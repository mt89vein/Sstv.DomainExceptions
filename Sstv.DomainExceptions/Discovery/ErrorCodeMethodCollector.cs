using Microsoft.CodeAnalysis;
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

        context.RegisterSourceOutput(methods, GenerateErrorCodesDictionary);
    }

    private static MethodInfo? GetMethodInfoFromSyntax(GeneratorSyntaxContext ctx)
    {
        if (ctx.Node is not MethodDeclarationSyntax methodDecl)
            return null;

        var hasCollectAttribute = methodDecl.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a => a.Name.ToString().Contains("CollectErrorCodes"));

        if (!hasCollectAttribute)
            return null;

        var symbol = ctx.SemanticModel.GetDeclaredSymbol(methodDecl);
        if (symbol == null)
            return null;

        var typeSymbol = symbol.ContainingType;
        if (typeSymbol == null)
            return null;

        var typeName = typeSymbol.ToDisplayString();
        var namespaceName = typeSymbol.ContainingNamespace?.ToDisplayString();
        if (string.IsNullOrEmpty(namespaceName))
            namespaceName = ctx.SemanticModel.Compilation.AssemblyName ?? "Global";

        return new MethodInfo
        {
            TypeName = typeName,
            MethodName = symbol.Name,
            Namespace = namespaceName ?? "",
            SyntaxNode = methodDecl,
            SemanticModel = ctx.SemanticModel
        };
    }

    private static void GenerateErrorCodesDictionary(
        SourceProductionContext context,
        ImmutableArray<MethodInfo?> methods)
    {
        var allMethods = methods.Where(m => m != null).Cast<MethodInfo>().ToList();
        if (allMethods.Count == 0)
            return;

        var methodErrorCodes = new Dictionary<string, List<ErrorCodeInfo>>();
        var methodCalls = new Dictionary<string, List<string>>();

        foreach (var methodInfo in allMethods)
        {
            var key = methodInfo.TypeName + "." + methodInfo.MethodName;
            if (methodErrorCodes.ContainsKey(key))
                continue;

            var errorCodes = CollectErrorCodes(context, methodInfo);
            var calledKeys = CollectCalledMethods(methodInfo);

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
                            if (!methodErrorCodes[key].Any(e => e.Code == code.Code))
                            {
                                methodErrorCodes[key].Add(code);
                                anyAdded = true;
                            }
                        }
                    }
                }
            }
            if (!anyAdded)
                break;
        }

        if (methodErrorCodes.All(kvp => kvp.Value.Count == 0))
            return;

        var firstMethod = allMethods.First();
        var lastDot = firstMethod.TypeName.LastIndexOf('.');
        var namespaceName = lastDot > 0 ? firstMethod.TypeName.Substring(0, lastDot) : firstMethod.Namespace;

        GenerateSource(context, methodErrorCodes, namespaceName);
    }

    private static List<ErrorCodeInfo> CollectErrorCodes(SourceProductionContext context, MethodInfo methodInfo)
    {
        var errorCodes = new List<ErrorCodeInfo>();
        var semanticModel = methodInfo.SemanticModel;

        foreach (var returnStmt in methodInfo.SyntaxNode.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (returnStmt.Expression != null)
                ExtractErrorCodesFromExpression(returnStmt.Expression, errorCodes, semanticModel);
        }

        foreach (var throwStmt in methodInfo.SyntaxNode.DescendantNodes().OfType<ThrowStatementSyntax>())
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            if (throwStmt.Expression == null)
                continue;

            var thrownText = throwStmt.Expression.ToString();

            if (thrownText.Contains(".ToException()"))
            {
                var errorCodeInfo = ExtractErrorCodeFromToException(throwStmt.Expression, semanticModel);
                if (errorCodeInfo != null && !errorCodes.Any(e => e.Code == errorCodeInfo.Code))
                    errorCodes.Add(errorCodeInfo);
            }

            if (throwStmt.Expression is SyntaxNode thrownExpr)
            {
                foreach (var memberAccess in thrownExpr.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                {
                    if (memberAccess.Parent is InvocationExpressionSyntax)
                        continue;

                    var fullMatch = memberAccess.ToString();
                    var errorCode = memberAccess.Name.Identifier.Text;

                    var chainMethods = new HashSet<string>(StringComparer.Ordinal)
                    {
                        "ToException", "WithErrorId", "WithDetailedMessage", "WithAdditionalData"
                    };
                    if (chainMethods.Contains(errorCode))
                        continue;

                    var sourceType = DetermineSourceType(fullMatch, semanticModel);

                    if (!errorCodes.Any(e => e.Code == errorCode) && char.IsUpper(errorCode.FirstOrDefault()))
                    {
                        var fullEnumExpression = sourceType == ErrorCodeSourceType.Enum ? fullMatch : null;
                        string? typeName = null;
                        string? constantValue = null;
                        if (semanticModel != null)
                        {
                            var symbol = semanticModel.GetSymbolInfo(memberAccess).Symbol;
                            if (symbol is IFieldSymbol fieldSymbol)
                            {
                                typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                if (fieldSymbol.IsConst && fieldSymbol.ConstantValue != null)
                                    constantValue = fieldSymbol.ConstantValue.ToString();
                            }
                        }
                        typeName ??= fullMatch.Contains('.') ? fullMatch.Substring(0, fullMatch.LastIndexOf('.')) : errorCode;
                        var codeValue = constantValue ?? errorCode;
                        errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName));
                    }
                }
            }
        }

        return errorCodes;
    }

    private static List<string> CollectCalledMethods(MethodInfo methodInfo)
    {
        var calledKeys = new List<string>();
        var semanticModel = methodInfo.SemanticModel;
        if (semanticModel == null)
            return calledKeys;

        foreach (var invocation in methodInfo.SyntaxNode.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol)
            {
                var receiverType = methodSymbol.ContainingType?.ToDisplayString();
                if (!string.IsNullOrEmpty(receiverType) && !calledKeys.Contains(receiverType + "." + methodSymbol.Name))
                    calledKeys.Add(receiverType + "." + methodSymbol.Name);
            }
        }

        return calledKeys;
    }

    private static void GenerateSource(SourceProductionContext context, Dictionary<string, List<ErrorCodeInfo>> methodErrorCodes, string namespaceName)
    {
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
        sb.AppendLine("        public ErrorCodeSource(string code, ErrorCodeSourceType sourceType, Type? errorType = null)");
        sb.AppendLine("        {");
        sb.AppendLine("            Code = code;");
        sb.AppendLine("            SourceType = sourceType;");
        sb.AppendLine("            ErrorType = errorType;");
        sb.AppendLine("        }");
        sb.AppendLine("        public bool Equals(ErrorCodeSource? other) => other != null && Code == other.Code && SourceType == other.SourceType;");
        sb.AppendLine("        public override bool Equals(object? obj) => Equals(obj as ErrorCodeSource);");
        sb.AppendLine("        public override int GetHashCode() => HashCode.Combine(Code, SourceType);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    public static partial class ErrorCodeMethodCollector");
        sb.AppendLine("    {");
        sb.AppendLine("        public static readonly FrozenDictionary<string, HashSet<ErrorCodeSource>> ErrorCodesByMethod =");
        sb.AppendLine("            new Dictionary<string, HashSet<ErrorCodeSource>>");
        sb.AppendLine("            {");

        foreach (var kvp in methodErrorCodes.Where(k => k.Value.Count > 0))
        {
            var codes = string.Join(", ", kvp.Value.Select(c =>
            {
                var typeArg = c.SourceTypeName != null ? $", typeof({c.SourceTypeName})" : "";
                return c.SourceType == ErrorCodeSourceType.Enum
                    ? $"new ErrorCodeSource({c.FullEnumExpression ?? c.Code}.GetErrorCode(), ErrorCodeSourceType.{c.SourceType}{typeArg})"
                    : $"new ErrorCodeSource(\"{c.Code}\", ErrorCodeSourceType.{c.SourceType}{typeArg})";
            }));
            sb.AppendLine($"                [\"{kvp.Key}\"] = new HashSet<ErrorCodeSource> {{ {codes} }},");
        }

        sb.AppendLine("            }.ToFrozenDictionary();");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource("ErrorCodeMethodCollector.g.cs", SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static ErrorCodeInfo? ExtractErrorCodeFromToException(ExpressionSyntax expression, SemanticModel? semanticModel)
    {
        var match = Regex.Match(expression.ToString(), @"([\w.]+)\.ToException\(\)");
        if (!match.Success)
            return null;

        var fullExpression = match.Groups[1].Value;
        var memberName = fullExpression.Split('.').Last();
        string? typeName = null;
        if (semanticModel != null)
        {
            var toExceptionInvoke = expression.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(i => i.Expression.ToString().EndsWith(".ToException"));
            if (toExceptionInvoke?.Expression is MemberAccessExpressionSyntax mae && mae.Expression != null)
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
            return ErrorCodeSourceType.Constant;

        var parts = fullExpression.Split('.');
        var memberName = parts.Last();
        var fullTypeName = string.Join(".", parts.Take(parts.Length - 1));

        var typeSymbol = semanticModel.Compilation.GetTypeByMetadataName(fullTypeName);
        if (typeSymbol == null)
            return EnumFinder.FindEnumWithErrorDescriptionAttribute(semanticModel.Compilation, memberName).HasValue
                ? ErrorCodeSourceType.Enum
                : ErrorCodeSourceType.Constant;

        if (typeSymbol.TypeKind == TypeKind.Enum)
            return ErrorCodeSourceType.Constant;

        if (typeSymbol.GetMembers().Any(m => m.Name == memberName && m is IFieldSymbol f && f.IsConst))
            return ErrorCodeSourceType.Constant;

        return ErrorCodeSourceType.Constant;
    }

    private static void ExtractErrorCodesFromExpression(ExpressionSyntax expr, List<ErrorCodeInfo> errorCodes, SemanticModel? semanticModel)
    {
        switch (expr)
        {
            case ObjectCreationExpressionSyntax objectCreation:
                ExtractErrorCodesFromObjectCreation(objectCreation, errorCodes, semanticModel);
                break;
            case InvocationExpressionSyntax invocation:
                foreach (var arg in invocation.ArgumentList.Arguments)
                    ExtractErrorCodesFromExpression(arg.Expression, errorCodes, semanticModel);
                break;
            default:
                break;
        }
    }

    private static void ExtractErrorCodesFromObjectCreation(ObjectCreationExpressionSyntax objectCreation, List<ErrorCodeInfo> errorCodes, SemanticModel? semanticModel)
    {
        if (objectCreation.ArgumentList == null)
            return;

        foreach (var arg in objectCreation.ArgumentList.Arguments)
        {
            var argText = arg.Expression.ToString();
            var match = Regex.Match(argText, @"(\w+)\.(\w+)");

            if (match.Success)
            {
                var potentialCode = match.Groups[2].Value;
                var sourceType = DetermineSourceType(argText, semanticModel);

                if (char.IsUpper(potentialCode.FirstOrDefault()) && !errorCodes.Any(e => e.Code == potentialCode))
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
                            if (fieldSymbol.IsConst && fieldSymbol.ConstantValue != null)
                                constantValue = fieldSymbol.ConstantValue.ToString();
                        }
                    }
                    typeName ??= argText.Contains('.') ? argText.Substring(0, argText.LastIndexOf('.')) : null;
                    var codeValue = constantValue ?? potentialCode;
                    errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName));
                }
                else if (char.IsUpper(argText.FirstOrDefault()) && !argText.Contains('.'))
                {
                    if (!errorCodes.Any(e => e.Code == argText))
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
    public MethodDeclarationSyntax SyntaxNode { get; set; } = null!;
    public SemanticModel? SemanticModel { get; set; }
}

internal enum ErrorCodeSourceType { Enum, Constant }

internal sealed class ErrorCodeInfo
{
    public string Code { get; }
    public ErrorCodeSourceType SourceType { get; }
    public string? FullEnumExpression { get; }
    public string? SourceTypeName { get; }
    public ErrorCodeInfo(string code, ErrorCodeSourceType sourceType, string? fullEnumExpression = null, string? sourceTypeName = null)
    {
        Code = code;
        SourceType = sourceType;
        FullEnumExpression = fullEnumExpression;
        SourceTypeName = sourceTypeName;
    }
}

internal static class EnumFinder
{
    public static (string EnumTypeName, string FieldName)? FindEnumWithErrorDescriptionAttribute(Compilation compilation, string fieldName)
    {
        foreach (var symbol in compilation.GetSymbolsWithName(fieldName, SymbolFilter.Member))
        {
            if (symbol.ContainingType is INamedTypeSymbol enumType &&
                enumType.TypeKind == TypeKind.Enum &&
                enumType.GetAttributes().Any(a => (a.AttributeClass?.Name ?? "") is "ErrorDescriptionAttribute" or "ErrorDescription"))
            {
                return (enumType.Name, fieldName);
            }
        }
        return null;
    }
}