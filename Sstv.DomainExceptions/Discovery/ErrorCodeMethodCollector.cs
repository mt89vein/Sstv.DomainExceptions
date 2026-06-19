using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

namespace Sstv.DomainExceptions.Discovery;

[Generator(LanguageNames.CSharp)]
internal partial class ErrorCodeMethodCollector : IIncrementalGenerator
{
    private static readonly Regex _toExceptionPattern = new(@"([\w.]+)\.ToException\(\)", RegexOptions.Compiled);
    private static readonly Regex _memberAccessPattern = new(@"(\w+)\.(\w+)", RegexOptions.Compiled);

    private static readonly HashSet<string> _fluentChainMethods = new(StringComparer.Ordinal)
    {
        "ToException", "WithErrorId", "WithDetailedMessage", "WithAdditionalData"
    };

    private static readonly HashSet<string> _mapMethodNames = new(StringComparer.Ordinal)
    {
        "Map", "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapQuery",
        "MapGroup", "MapMethods"
    };

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

        return new MethodInfo
        {
            TypeName = typeName,
            MethodName = symbol.Name,
            Namespace = namespaceName!,
            SyntaxNode = methodDecl,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static bool IsMinimalApiEndpoint(SyntaxNode node)
    {
        if (node is not InvocationExpressionSyntax invocation)
        {
            return false;
        }

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        return _mapMethodNames.Contains(memberAccess.Name.Identifier.Text);
    }

    private static EndpointInfo? GetEndpointInfo(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        var routePattern = GetRoutePattern(invocation);
        if (routePattern is null)
        {
            return null;
        }

        var endpointName = GetEndpointName(invocation);
        var key = endpointName ?? routePattern;

        var body = GetEndpointBody(invocation);
        if (body is null)
        {
            return null;
        }

        return new EndpointInfo
        {
            Key = key,
            Namespace = ctx.SemanticModel.Compilation.AssemblyName ?? "MinimalApi",
            Body = body,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static string? GetRoutePattern(InvocationExpressionSyntax invocation)
    {
        if (invocation.ArgumentList.Arguments.Count < 2)
        {
            return null;
        }

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
        {
            return null;
        }

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

        var compilation = allMethods.Count > 0
            ? allMethods[0].SemanticModel?.Compilation
            : allEndpoints.FirstOrDefault()?.SemanticModel.Compilation;
        var namespaceName = compilation?.AssemblyName ?? "Sstv.DomainExceptions";

        GenerateSource(context, methodErrorCodes, namespaceName);
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

        foreach (var node in body.DescendantNodes())
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            switch (node)
            {
                case ReturnStatementSyntax { Expression: not null } returnStmt:
                    ExtractErrorCodesFromExpression(returnStmt.Expression, errorCodes, semanticModel);
                    break;
                case ThrowStatementSyntax { Expression: not null } throwStmt:
                    CollectErrorCodesFromThrowExpression(throwStmt.Expression, errorCodes, semanticModel);
                    ResolveThrowFromVariable(throwStmt.Expression, errorCodes, semanticModel);
                    break;
                case ThrowExpressionSyntax throwExpr:
                    CollectErrorCodesFromThrowExpression(throwExpr, errorCodes, semanticModel);
                    ResolveThrowFromVariable(throwExpr, errorCodes, semanticModel);
                    break;
                default:
                    break;
            }
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

            if (_fluentChainMethods.Contains(errorCode))
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
        {
            return;
        }

        if (throwExpression is not IdentifierNameSyntax identifier)
        {
            return;
        }

        if (semanticModel.GetSymbolInfo(identifier).Symbol is not ILocalSymbol localSymbol)
        {
            return;
        }

        foreach (var syntaxRef in localSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is not VariableDeclaratorSyntax declarator)
            {
                continue;
            }

            var initializer = declarator.Initializer?.Value;
            if (initializer is null)
            {
                continue;
            }

            var initializerModel = semanticModel.Compilation.GetSemanticModel(syntaxRef.SyntaxTree);
            CollectErrorCodesFromThrowExpression(initializer, errorCodes, initializerModel);
            ExtractErrorCodesFromExpression(initializer, errorCodes, initializerModel);
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

        foreach (var invocation in body.DescendantNodes())
        {
            if (invocation is not InvocationExpressionSyntax invocationExpr)
            {
                continue;
            }

            if (semanticModel.GetSymbolInfo(invocationExpr).Symbol is IMethodSymbol methodSymbol)
            {
                var receiverType = methodSymbol.ContainingType?.ToDisplayString();
                if (!string.IsNullOrEmpty(receiverType))
                {
                    var key = receiverType + "." + methodSymbol.Name;
                    if (!calledKeys.Contains(key))
                    {
                        calledKeys.Add(key);
                    }
                }
            }
        }

        return calledKeys;
    }

    private static void GenerateSource(SourceProductionContext context,
        Dictionary<string, List<ErrorCodeInfo>> methodErrorCodes,
        string namespaceName)
    {
        var keysToEmit = methodErrorCodes.Where(k => k.Value.Count > 0).Select(k => k.Key).ToList();

        if (keysToEmit.Count == 0)
        {
            return;
        }

        var sb = new StringBuilder();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Collections.Frozen;");
        sb.AppendLine("using Sstv.DomainExceptions;");

        var allTypeNames = methodErrorCodes.Values
            .SelectMany(c => c)
            .Select(c => c.SourceTypeName)
            .Where(n => n != null)
            .Distinct()
            .Select(n =>
            {
                var name = n!;
                if (!name.StartsWith("global::", StringComparison.Ordinal) && name.Contains('.'))
                    return "global::" + name;
                return name;
            })
            .OrderBy(n => n)
            .ToArray();

        var aliasMap = new Dictionary<string, string>(StringComparer.Ordinal);
        var aliasCounts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var typeName in allTypeNames)
        {
            var simpleName = typeName.Contains('.')
                ? typeName.Substring(typeName.LastIndexOf('.') + 1)
                : typeName;

            aliasCounts.TryGetValue(simpleName, out var count);
            aliasCounts[simpleName] = count + 1;

            var alias = count > 0 ? $"{simpleName}_{count}" : simpleName;
            aliasMap[typeName] = alias;
        }

        if (aliasMap.Count > 0)
        {
            sb.AppendLine();
        }

        foreach (var kvp in aliasMap)
        {
            sb.AppendLine($"using {kvp.Value} = {kvp.Key};");
        }

        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
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
                var typeName = c.SourceTypeName;
                if (typeName != null && !typeName.StartsWith("global::", StringComparison.Ordinal) && typeName.Contains('.'))
                {
                    typeName = "global::" + typeName;
                }

                var typeAlias = typeName != null && aliasMap.TryGetValue(typeName, out var alias) ? alias : null;

                var fullEnumExpression = c.FullEnumExpression;
                if (fullEnumExpression != null && typeAlias != null && fullEnumExpression.Contains('.'))
                {
                    var memberPart = fullEnumExpression.Substring(fullEnumExpression.LastIndexOf('.') + 1);
                    fullEnumExpression = $"{typeAlias}.{memberPart}";
                }

                var typeArg = typeAlias != null ? $", typeof({typeAlias})" : "";
                return c.SourceType == ErrorCodeSourceType.Enum
                    ? $"new ErrorCodeSource({fullEnumExpression ?? c.Code}.GetErrorCode(), ErrorCodeSourceType.Enum{typeArg})"
                    : $"new ErrorCodeSource(\"{c.Code}\", ErrorCodeSourceType.Constant{typeArg})";
            }));
            sb.AppendLine($"                [\"{key}\"] = [{codes}],");
        }

        sb.AppendLine("            }.ToFrozenDictionary();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ErrorCodeMethodCollector.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static ErrorCodeInfo? ExtractErrorCodeFromToException(ExpressionSyntax expression,
        SemanticModel? semanticModel)
    {
        var match = _toExceptionPattern.Match(expression.ToString());
        if (!match.Success)
        {
            return null;
        }

        var fullExpression = match.Groups[1].Value;
        var memberName = fullExpression.Split('.').Last();
        string? typeName = null;
        if (semanticModel != null)
        {
            InvocationExpressionSyntax? toExceptionInvoke = null;

            if (expression is InvocationExpressionSyntax invokeExpr &&
                invokeExpr.Expression.ToString().EndsWith(".ToException", StringComparison.OrdinalIgnoreCase))
            {
                toExceptionInvoke = invokeExpr;
            }

            toExceptionInvoke ??= expression.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(i => i.Expression.ToString().EndsWith(".ToException", StringComparison.OrdinalIgnoreCase));
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

    private static bool IsDerivedFromDomainException(
        ObjectCreationExpressionSyntax objectCreation,
        SemanticModel semanticModel)
    {
        var typeSymbol = semanticModel.GetTypeInfo(objectCreation).Type;
        if (typeSymbol is null)
        {
            return false;
        }

        var current = typeSymbol;
        while (current is not null)
        {
            if (current.ToDisplayString() == "Sstv.DomainExceptions.DomainException")
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
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

                if (invocation.Expression is MemberAccessExpressionSyntax ma)
                {
                    ExtractErrorCodesFromExpression(ma.Expression, errorCodes, semanticModel);
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
            var match = _memberAccessPattern.Match(argText);

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

internal sealed class MethodInfo
{
    public string TypeName { get; set; } = "";
    public string MethodName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public MethodDeclarationSyntax SyntaxNode { get; set; } = null!;
    public SemanticModel? SemanticModel { get; set; }
}

internal sealed class EndpointInfo
{
    public string Key { get; set; } = "";
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