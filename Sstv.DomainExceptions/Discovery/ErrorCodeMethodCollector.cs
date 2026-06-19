using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Sstv.DomainExceptions.Discovery;

[Generator]
internal partial class ErrorCodeMethodCollector : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => node is MethodDeclarationSyntax,
                transform: (ctx, _) => GetMethodInfoFromSyntax(ctx))
            .Where(m => m != null)
            .Collect();

        var endpoints = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (node, _) => IsMinimalApiEndpoint(node),
                transform: (ctx, _) => GetEndpointInfo(ctx))
            .Where(e => e != null)
            .Collect();

        context.RegisterSourceOutput(methods.Combine(endpoints), GenerateErrorCodesDictionary);
    }

    private static MethodInfo? GetMethodInfoFromSyntax(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not MethodDeclarationSyntax methodDecl)
        {
            return null;
        }

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl);
        if (symbol is null)
        {
            return null;
        }

        var typeSymbol = symbol.ContainingType;
        if (typeSymbol == null)
        {
            return null;
        }

        var typeName = typeSymbol.ToDisplayString();
        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrEmpty(namespaceName))
        {
            namespaceName = ctx.SemanticModel.Compilation.AssemblyName ?? "Global";
        }

        var isEntryPoint = methodDecl.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("CollectErrorCodes"))
            || IsControllerAction(typeSymbol, symbol);

        return new MethodInfo
        {
            TypeName = typeName,
            MethodName = symbol.Name,
            Namespace = namespaceName!,
            IsEntryPoint = isEntryPoint,
            SyntaxNode = methodDecl,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static readonly HashSet<string> _mapMethodNames = new(StringComparer.Ordinal)
    {
        "Map", "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch",
        "MapGroup", "MapMethods"
    };

    private static bool IsControllerAction(INamedTypeSymbol? typeSymbol, IMethodSymbol methodSymbol)
    {
        if (typeSymbol is null)
            return false;

        var current = typeSymbol;
        while (current is not null)
        {
            if (current.ToDisplayString() is "Microsoft.AspNetCore.Mvc.ControllerBase"
                or "Microsoft.AspNetCore.Mvc.Controller")
            {
                var hasNonAction = methodSymbol.GetAttributes().Any(a =>
                    a.AttributeClass?.ToDisplayString() == "Microsoft.AspNetCore.Mvc.NonActionAttribute");

                return !hasNonAction;
            }
            current = current.BaseType;
        }

        return false;
    }

    private static bool IsMinimalApiEndpoint(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        return _mapMethodNames.Contains(memberAccess.Name.Identifier.Text);
    }

    private static EndpointInfo? GetEndpointInfo(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not InvocationExpressionSyntax invocation)
            return null;

        var routePattern = GetRoutePattern(invocation);
        if (routePattern is null)
            return null;

        var endpointName = GetEndpointName(invocation);
        var key = endpointName ?? routePattern;

        var body = GetEndpointBody(invocation);
        if (body is null)
            return null;

        return new EndpointInfo
        {
            Key = key,
            RoutePattern = routePattern,
            Namespace = ctx.SemanticModel.Compilation.AssemblyName ?? "MinimalApi",
            Body = body,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static string? GetRoutePattern(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count < 2)
            return null;

        var firstArg = invocation.ArgumentList.Arguments[0].Expression;
        if (firstArg is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static string? GetEndpointName(InvocationExpressionSyntax mapInvocation)
    {
        if (mapInvocation.Parent is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Parent is InvocationExpressionSyntax outerInvocation &&
            memberAccess.Name.Identifier.Text == "WithName" &&
            outerInvocation.ArgumentList.Arguments.Count >= 1 &&
            outerInvocation.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal &&
            literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            return literal.Token.ValueText;
        }

        return null;
    }

    private static SyntaxNode? GetEndpointBody(InvocationExpressionSyntax invocation)
    {
        var args = invocation.ArgumentList.Arguments;
        if (args.Count < 2)
            return null;

        var handlerArg = args[args.Count - 1].Expression;

        return handlerArg switch
        {
            LambdaExpressionSyntax lambda => (SyntaxNode?)lambda.Body,
            AnonymousMethodExpressionSyntax anonymous => anonymous.Block,
            _ => null
        };
    }

    private static void GenerateErrorCodesDictionary(
        SourceProductionContext context,
        (ImmutableArray<MethodInfo?> Methods, ImmutableArray<EndpointInfo?> Endpoints) input
    )
    {
        var allMethods = input.Methods.Where(m => m != null).Cast<MethodInfo>().ToList();
        var allEndpoints = input.Endpoints.Where(e => e != null).Cast<EndpointInfo>().ToList();

        if (allMethods.Count == 0 && allEndpoints.Count == 0)
        {
            return;
        }

        var methodErrorCodes = new Dictionary<string, List<ErrorCodeInfo>>();
        var methodCalls = new Dictionary<string, List<string>>();

        foreach (var methodInfo in allMethods)
        {
            var key = methodInfo.TypeName + "." + methodInfo.MethodName;
            if (methodErrorCodes.ContainsKey(key))
            {
                continue;
            }

            var errorCodes = CollectErrorCodes(context, methodInfo);
            var calledKeys = CollectCalledMethods(methodInfo);

            methodErrorCodes[key] = errorCodes;
            methodCalls[key] = calledKeys;
        }

        foreach (var endpointInfo in allEndpoints)
        {
            var key = endpointInfo.Key;
            if (methodErrorCodes.ContainsKey(key))
            {
                continue;
            }

            var errorCodes = AnalyzeErrorCodesFromNode(context, endpointInfo.Body, endpointInfo.SemanticModel);
            var calledKeys = AnalyzeCalledMethodsFromNode(endpointInfo.Body, endpointInfo.SemanticModel);

            methodErrorCodes[key] = errorCodes;
            methodCalls[key] = calledKeys;
        }

        for (var iteration = 0; iteration < 10; iteration++)
        {
            var anyAdded = false;
            foreach (var kvp in methodCalls)
            {
                var key = kvp.Key;
                var calledKeys = kvp.Value;

                foreach (var calledKey in calledKeys)
                {
                    if (methodErrorCodes.TryGetValue(calledKey, out var calledCodes) && calledCodes.Count > 0)
                    {
                        foreach (var code in calledCodes)
                        {
                            if (methodErrorCodes[key].All(e => e.Code != code.Code))
                            {
                                methodErrorCodes[key].Add(code);
                                anyAdded = true;
                            }
                        }
                    }
                }
            }

            if (!anyAdded)
            {
                break;
            }
        }

        if (methodErrorCodes.All(kvp => kvp.Value.Count == 0))
        {
            return;
        }

        var firstMethod = allMethods.Count > 0 ? allMethods.First() : null;
        var namespaceName = firstMethod is not null
            ? firstMethod.TypeName.Contains('.')
                ? firstMethod.TypeName.Substring(0, firstMethod.TypeName.LastIndexOf('.'))
                : firstMethod.Namespace
            : allEndpoints.FirstOrDefault()?.Namespace ?? "Sstv.DomainExceptions";

        GenerateSource(context, methodErrorCodes, allMethods, allEndpoints, namespaceName);
    }

    private static List<ErrorCodeInfo> CollectErrorCodes(SourceProductionContext context, MethodInfo methodInfo)
    {
        return AnalyzeErrorCodesFromNode(context, methodInfo.SyntaxNode, methodInfo.SemanticModel);
    }

    private static List<ErrorCodeInfo> AnalyzeErrorCodesFromNode(
        SourceProductionContext context,
        SyntaxNode body,
        SemanticModel? semanticModel)
    {
        var errorCodes = new List<ErrorCodeInfo>();

        foreach (var returnStmt in body.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (returnStmt.Expression != null)
            {
                ExtractErrorCodesFromExpression(returnStmt.Expression, errorCodes, semanticModel);
            }
        }

        foreach (var throwStmt in body.DescendantNodes().OfType<ThrowStatementSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (throwStmt.Expression != null)
            {
                CollectErrorCodesFromThrowExpression(throwStmt.Expression, errorCodes, semanticModel);
                ResolveThrowFromVariable(throwStmt.Expression, errorCodes, semanticModel);
            }
        }

        foreach (var throwExpr in body.DescendantNodes().OfType<ThrowExpressionSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            CollectErrorCodesFromThrowExpression(throwExpr, errorCodes, semanticModel);
            ResolveThrowFromVariable(throwExpr, errorCodes, semanticModel);
        }

        return errorCodes;
    }

    private static void CollectErrorCodesFromThrowExpression(
        ExpressionSyntax throwExpression,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel)
    {
        var thrownText = throwExpression.ToString();

        if (thrownText.Contains(".ToException()"))
        {
            var errorCodeInfo = ExtractErrorCodeFromToException(throwExpression, semanticModel);
            if (errorCodeInfo != null && errorCodes.All(e => e.Code != errorCodeInfo.Code))
            {
                errorCodes.Add(errorCodeInfo);
            }
        }

        foreach (var memberAccess in throwExpression.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Parent is InvocationExpressionSyntax)
            {
                continue;
            }

            var fullMatch = memberAccess.ToString();
            var errorCode = memberAccess.Name.Identifier.Text;

            var chainMethods = new HashSet<string>(StringComparer.Ordinal)
            {
                "ToException", "WithErrorId", "WithDetailedMessage", "WithAdditionalData"
            };
            if (chainMethods.Contains(errorCode))
            {
                continue;
            }

            var sourceType = DetermineSourceType(fullMatch, semanticModel);

            if (errorCodes.All(e => e.Code != errorCode) && char.IsUpper(errorCode.FirstOrDefault()))
            {
                var fullEnumExpression = sourceType == ErrorCodeSourceType.Enum ? fullMatch : null;
                string? typeName = null;
                string? constantValue = null;
                var symbol = semanticModel?.GetSymbolInfo(memberAccess).Symbol;
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat
                        .FullyQualifiedFormat);
                    if (fieldSymbol is { IsConst: true, ConstantValue: not null })
                    {
                        constantValue = fieldSymbol.ConstantValue.ToString();
                    }
                }

                typeName ??= fullMatch.Contains('.')
                    ? fullMatch.Substring(0, fullMatch.LastIndexOf('.'))
                    : errorCode;
                var codeValue = constantValue ?? errorCode;
                errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName));
            }
        }
    }

    private static void ResolveThrowFromVariable(
        ExpressionSyntax throwExpression,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel)
    {
        if (semanticModel is null)
            return;

        if (throwExpression is not IdentifierNameSyntax identifier)
            return;

        if (semanticModel.GetSymbolInfo(identifier).Symbol is not ILocalSymbol localSymbol)
            return;

        foreach (var syntaxRef in localSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not VariableDeclaratorSyntax declarator)
                continue;

            var initializer = declarator.Initializer?.Value;
            if (initializer is null)
                continue;

            CollectErrorCodesFromThrowExpression(initializer, errorCodes, semanticModel);
            ExtractErrorCodesFromExpression(initializer, errorCodes, semanticModel);
        }
    }

    private static List<string> CollectCalledMethods(MethodInfo methodInfo)
    {
        return AnalyzeCalledMethodsFromNode(methodInfo.SyntaxNode, methodInfo.SemanticModel);
    }

    private static List<string> AnalyzeCalledMethodsFromNode(SyntaxNode body, SemanticModel? semanticModel)
    {
        var calledKeys = new List<string>();
        if (semanticModel == null)
        {
            return calledKeys;
        }

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
            {
                var receiverType = methodSymbol.ContainingType?.ToDisplayString();
                if (!string.IsNullOrEmpty(receiverType) && !calledKeys.Contains(receiverType + "." + methodSymbol.Name))
                {
                    calledKeys.Add(receiverType + "." + methodSymbol.Name);
                }
            }
        }

        return calledKeys;
    }

    private static void GenerateSource(SourceProductionContext context,
        Dictionary<string, List<ErrorCodeInfo>> methodErrorCodes,
        List<MethodInfo> allMethods,
        List<EndpointInfo> allEndpoints,
        string namespaceName)
    {
        var keysToEmit = new HashSet<string>(methodErrorCodes.Keys.Where(k => methodErrorCodes[k].Count > 0));

        if (keysToEmit.Count == 0)
            return;

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine();
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Collections.Frozen;");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        sb.AppendLine("    public enum ErrorCodeSourceType");
        sb.AppendLine("    {");
        sb.AppendLine("        Enum,");
        sb.AppendLine("        Constant");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public sealed class ErrorCodeSource : IEquatable<ErrorCodeSource>");
        sb.AppendLine("    {");
        sb.AppendLine("        public string Code { get; }");
        sb.AppendLine("        public ErrorCodeSourceType SourceType { get; }");
        sb.AppendLine("        public Type? ErrorType { get; }");
        sb.AppendLine(
            "        public ErrorCodeSource(string code, ErrorCodeSourceType sourceType, Type? errorType = null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Code = code;");
        sb.AppendLine("            SourceType = sourceType;");
        sb.AppendLine("            ErrorType = errorType;");
        sb.AppendLine("        }");
        sb.AppendLine(
            "        public bool Equals(ErrorCodeSource? other) => other != null && Code == other.Code && SourceType == other.SourceType;");
        sb.AppendLine("        public override bool Equals(object? obj) => Equals(obj as ErrorCodeSource);");
        sb.AppendLine("        public override int GetHashCode() => HashCode.Combine(Code, SourceType);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static partial class ErrorCodeMethodCollector");
        sb.AppendLine("    {");
        sb.AppendLine(
            "        public static readonly FrozenDictionary<string, HashSet<ErrorCodeSource>> ErrorCodesByMethod =");
        sb.AppendLine("            new Dictionary<string, HashSet<ErrorCodeSource>>");
        sb.AppendLine("            {");

        foreach (var key in keysToEmit)
        {
            var codes = string.Join(", ", methodErrorCodes[key].Select(c =>
            {
                var typeArg = c.SourceTypeName != null ? $", typeof({c.SourceTypeName})" : "";
                return c.SourceType == ErrorCodeSourceType.Enum
                    ? $"new ErrorCodeSource({c.FullEnumExpression ?? c.Code}.GetErrorCode(), ErrorCodeSourceType.{c.SourceType}{typeArg})"
                    : $"new ErrorCodeSource(\"{c.Code}\", ErrorCodeSourceType.{c.SourceType}{typeArg})";
            }));
            sb.AppendLine($"                [\"{key}\"] = new HashSet<ErrorCodeSource> {{ {codes} }},");
        }

        sb.AppendLine("            }.ToFrozenDictionary();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ErrorCodeMethodCollector.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static ErrorCodeInfo? ExtractErrorCodeFromToException(ExpressionSyntax expression,
        SemanticModel? semanticModel)
    {
        var match = Regex.Match(expression.ToString(), @"([\w.]+)\.ToException\(\)");
        if (!match.Success)
        {
            return null;
        }

        var fullExpression = match.Groups[1].Value;
        var memberName = fullExpression.Split('.').Last();
        string? typeName = null;
        if (semanticModel != null)
        {
            var toExceptionInvoke = expression.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(i => i.Expression.ToString().EndsWith(".ToException", StringComparison.InvariantCultureIgnoreCase));
            if (toExceptionInvoke?.Expression is MemberAccessExpressionSyntax mae)
            {
                var typeInfo = semanticModel.GetTypeInfo(mae.Expression);
                typeName = typeInfo.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            }
        }

        typeName ??= fullExpression.Contains('.') ? fullExpression.Substring(0, fullExpression.LastIndexOf('.')) : null;

        return new ErrorCodeInfo(memberName, ErrorCodeSourceType.Enum, fullExpression, typeName);
    }

    private static ErrorCodeSourceType DetermineSourceType(string fullExpression, SemanticModel? semanticModel)
    {
        if (!fullExpression.Contains('.') || semanticModel == null)
        {
            return ErrorCodeSourceType.Constant;
        }

        var parts = fullExpression.Split('.');
        var memberName = parts.Last();
        var fullTypeName = string.Join(".", parts.Take(parts.Length - 1));

        var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(fullTypeName);
        if (typeSymbol == null)
        {
            return EnumFinder.FindEnumWithErrorDescriptionAttribute(semanticModel.Compilation, memberName).HasValue
                ? ErrorCodeSourceType.Enum
                : ErrorCodeSourceType.Constant;
        }

        return ErrorCodeSourceType.Constant;
    }

    private static void ExtractErrorCodesFromExpression(
        ExpressionSyntax expr,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel
    )
    {
        switch (expr)
        {
            case ObjectCreationExpressionSyntax objectCreation:
                ExtractErrorCodesFromObjectCreation(objectCreation, errorCodes, semanticModel);
                break;
            case InvocationExpressionSyntax invocation:
                foreach (var arg in invocation.ArgumentList.Arguments)
                {
                    ExtractErrorCodesFromExpression(arg.Expression, errorCodes, semanticModel);
                }

                break;
            default:
                break;
        }
    }

    private static void ExtractErrorCodesFromObjectCreation(
        ObjectCreationExpressionSyntax objectCreation,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel
    )
    {
        if (objectCreation.ArgumentList == null)
        {
            return;
        }

        foreach (var arg in objectCreation.ArgumentList.Arguments)
        {
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                semanticModel != null &&
                IsDerivedFromDomainException(objectCreation, semanticModel))
            {
                var stringValue = literal.Token.ValueText;
                if (errorCodes.All(e => e.Code != stringValue))
                {
                    errorCodes.Add(new ErrorCodeInfo(stringValue, ErrorCodeSourceType.Constant, null, null));
                }
                continue;
            }

            var argText = arg.Expression.ToString();
            var match = Regex.Match(argText, @"(\w+)\.(\w+)");

            if (!match.Success)
            {
                continue;
            }

            var potentialCode = match.Groups[2].Value;
            var sourceType = DetermineSourceType(argText, semanticModel);

            if (char.IsUpper(potentialCode.FirstOrDefault()) && errorCodes.All(e => e.Code != potentialCode))
            {
                var fullEnumExpression = sourceType == ErrorCodeSourceType.Enum ? argText : null;
                string? typeName = null;
                string? constantValue = null;
                if (semanticModel != null && arg.Expression is MemberAccessExpressionSyntax maes)
                {
                    var symbol = semanticModel.GetSymbolInfo(maes).Symbol;
                    if (symbol is IFieldSymbol fieldSymbol)
                    {
                        typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        if (fieldSymbol is { IsConst: true, ConstantValue: not null })
                        {
                            constantValue = fieldSymbol.ConstantValue.ToString();
                        }
                    }
                }

                typeName ??= argText.Contains('.') ? argText.Substring(0, argText.LastIndexOf('.')) : null;
                var codeValue = constantValue ?? potentialCode;
                errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName));
            }
            else if (char.IsUpper(argText.FirstOrDefault()) && !argText.Contains('.'))
            {
                if (errorCodes.All(e => e.Code != argText))
                {
                    errorCodes.Add(new ErrorCodeInfo(argText, ErrorCodeSourceType.Constant, null, argText));
                }
            }
        }
    }
}

internal class MethodInfo
{
    public string TypeName { get; set; } = "";
    public string MethodName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public bool IsEntryPoint { get; set; }
    public MethodDeclarationSyntax SyntaxNode { get; set; } = null!;
    public SemanticModel? SemanticModel { get; set; }
}

internal sealed class EndpointInfo
{
    public string Key { get; set; } = "";
    public string RoutePattern { get; set; } = "";
    public string Namespace { get; set; } = "";
    public SyntaxNode Body { get; set; } = null!;
    public SemanticModel SemanticModel { get; set; } = null!;
}

internal enum ErrorCodeSourceType
{
    Enum,
    Constant
}

internal sealed class ErrorCodeInfo
{
    public string Code { get; }
    public ErrorCodeSourceType SourceType { get; }
    public string? FullEnumExpression { get; }
    public string? SourceTypeName { get; }

    public ErrorCodeInfo(string code, ErrorCodeSourceType sourceType, string? fullEnumExpression = null,
        string? sourceTypeName = null)
    {
        Code = code;
        SourceType = sourceType;
        FullEnumExpression = fullEnumExpression;
        SourceTypeName = sourceTypeName;
    }
}

internal static class EnumFinder
{
    public static (string EnumTypeName, string FieldName)? FindEnumWithErrorDescriptionAttribute(
        Compilation compilation, string fieldName)
    {
        foreach (var symbol in compilation.GetSymbolsWithName(fieldName, SymbolFilter.Member))
        {
            if (symbol.ContainingType is { TypeKind: TypeKind.Enum } enumType &&
                enumType.GetAttributes().Any(a =>
                    (a.AttributeClass?.Name ?? "") is nameof(ErrorDescriptionAttribute) or "ErrorDescription"))
            {
                return (enumType.Name, fieldName);
            }
        }

        return null;
    }
}