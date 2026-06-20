using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace Sstv.DomainExceptions.Discovery;

internal partial class ErrorCodeMethodCollector
{
    private static readonly Regex _toExceptionPattern = new(@"([\w.]+)\.ToException\(\)", RegexOptions.Compiled);
    private static readonly Regex _memberAccessPattern = new(@"(\w+)\.(\w+)", RegexOptions.Compiled);

    private static readonly HashSet<string> _fluentChainMethods = new(StringComparer.Ordinal)
    {
        "ToException", "WithErrorId", "WithDetailedMessage", "WithAdditionalData"
    };

    private static List<ErrorCodeInfo> CollectErrorCodes(SourceProductionContext context, MethodInfo methodInfo)
    {
        if (methodInfo.SyntaxNode is null || methodInfo.SemanticModel is null)
        {
            return [];
        }

        return AnalyzeErrorCodesFromNode(context, methodInfo.SyntaxNode, methodInfo.SemanticModel);
    }

    private static List<ErrorCodeInfo> AnalyzeErrorCodesFromNode(
        SourceProductionContext context,
        SyntaxNode body,
        SemanticModel? semanticModel)
    {
        var errorCodes = new List<ErrorCodeInfo>();

        try
        {
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
        }
        catch
        {
            // partial results are acceptable
        }

        return errorCodes;
    }

    private static void CollectErrorCodesFromThrowExpression(
        ExpressionSyntax throwExpression,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel)
    {
        try
        {
            if (throwExpression is ObjectCreationExpressionSyntax objectCreation)
            {
                ExtractErrorCodesFromObjectCreation(objectCreation, errorCodes, semanticModel);
            }

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

                var sourceType = ErrorCodeSourceType.Constant;
                string? typeName = null;
                string? extensionClassName = null;
                string? constantValue = null;

                var symbol = semanticModel?.GetSymbolInfo(memberAccess).Symbol;
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat
                        .FullyQualifiedFormat);
                    extensionClassName = ErrorCodeAnalysis.GetExtensionClassName(fieldSymbol.ContainingType);
                    if (fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
                    {
                        sourceType = ErrorCodeSourceType.Enum;
                    }
                    else if (fieldSymbol is { IsConst: true, ConstantValue: not null })
                    {
                        constantValue = fieldSymbol.ConstantValue.ToString();
                    }
                }

                typeName ??= ExtractTypeName(fullMatch, errorCode);
                var codeValue = constantValue ?? errorCode;
                var fullEnumExpression = sourceType == ErrorCodeSourceType.Enum ? fullMatch : null;

                if (errorCodes.All(e => e.Code != codeValue) && (symbol is IFieldSymbol || IsPotentialCode(errorCode)))
                {
                    errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName, extensionClassName));
                }
            }
        }
        catch
        {
            // skip problematic expressions
        }
    }

    private static bool IsPotentialCode(string errorCode)
    {
        return errorCode.Length > 0 && char.IsUpper(errorCode[0]);
    }

    private static string? ExtractTypeName(string fullMatch, string errorCode)
    {
        var lastDot = fullMatch.LastIndexOf('.');
        return lastDot > 0 ? fullMatch.Substring(0, lastDot) : null;
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

            try
            {
                var initializerModel = semanticModel.Compilation.GetSemanticModel(syntaxRef.SyntaxTree);
                CollectErrorCodesFromThrowExpression(initializer, errorCodes, initializerModel);
                ExtractErrorCodesFromExpression(initializer, errorCodes, initializerModel);
            }
            catch
            {
                // skip if semantic model is unavailable
            }
        }
    }

    private static ErrorCodeInfo? ExtractErrorCodeFromToException(ExpressionSyntax expression,
        SemanticModel? semanticModel)
    {
        var text = expression.ToString();
        var match = _toExceptionPattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        var fullExpression = match.Groups[1].Value;
        var lastDot = fullExpression.LastIndexOf('.');
        var memberName = lastDot > 0 ? fullExpression.Substring(lastDot + 1) : fullExpression;
        string? typeName = null;
        string? extensionClassName = null;
        if (semanticModel is not null)
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
                try
                {
                    var typeInfo = semanticModel.GetTypeInfo(mae.Expression);
                    typeName = typeInfo.Type?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    extensionClassName = ErrorCodeAnalysis.GetExtensionClassName(typeInfo.Type);
                }
                catch
                {
                    // skip if type resolution fails
                }
            }
        }

        typeName ??= lastDot > 0 ? fullExpression.Substring(0, lastDot) : null;

        return new ErrorCodeInfo(memberName, ErrorCodeSourceType.Enum, fullExpression, typeName, extensionClassName);
    }

    private static bool IsDerivedFromDomainException(
        ObjectCreationExpressionSyntax objectCreation,
        SemanticModel semanticModel)
    {
        try
        {
            var typeSymbol = semanticModel.GetTypeInfo(objectCreation).Type;

            var current = typeSymbol;
            while (current is not null)
            {
                if (current.ToDisplayString() == "Sstv.DomainExceptions.DomainException")
                {
                    return true;
                }

                current = current.BaseType;
            }
        }
        catch
        {
            // fallback
        }

        return false;
    }

    private static void ExtractErrorCodesFromExpression(
        ExpressionSyntax expr,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel)
    {
        try
        {
            switch (expr)
            {
                case ObjectCreationExpressionSyntax objectCreation:
                    ExtractErrorCodesFromObjectCreation(objectCreation, errorCodes, semanticModel);
                    break;
                case InvocationExpressionSyntax invocation:
                    if (invocation.ArgumentList is not null)
                    {
                        foreach (var arg in invocation.ArgumentList.Arguments)
                        {
                            ExtractErrorCodesFromExpression(arg.Expression, errorCodes, semanticModel);
                        }
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
        catch
        {
            // skip problematic expressions
        }
    }

    private static void ExtractErrorCodesFromObjectCreation(
        ObjectCreationExpressionSyntax objectCreation,
        List<ErrorCodeInfo> errorCodes,
        SemanticModel? semanticModel)
    {
        if (objectCreation.ArgumentList is null)
        {
            return;
        }

        foreach (var arg in objectCreation.ArgumentList.Arguments)
        {
            if (arg.Expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                semanticModel is not null &&
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

            var sourceType = ErrorCodeSourceType.Constant;
            string? typeName = null;
            string? extensionClassName = null;
            string? constantValue = null;
            var hasResolvedField = false;

            if (semanticModel is not null && arg.Expression is MemberAccessExpressionSyntax maes)
            {
                try
                {
                    var symbol = semanticModel.GetSymbolInfo(maes).Symbol;
                    if (symbol is IFieldSymbol fieldSymbol)
                    {
                        hasResolvedField = true;
                        typeName = fieldSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        extensionClassName = ErrorCodeAnalysis.GetExtensionClassName(fieldSymbol.ContainingType);
                        if (fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
                        {
                            sourceType = ErrorCodeSourceType.Enum;
                        }
                        else if (fieldSymbol is { IsConst: true, ConstantValue: not null })
                        {
                            constantValue = fieldSymbol.ConstantValue.ToString();
                        }
                    }
                }
                catch
                {
                    // skip if symbol resolution fails
                }
            }

            typeName ??= ExtractTypeName(argText, potentialCode);
            var codeValue = constantValue ?? potentialCode;
            var fullEnumExpression = sourceType == ErrorCodeSourceType.Enum ? argText : null;

            if ((hasResolvedField || IsPotentialCode(potentialCode)) && errorCodes.All(e => e.Code != codeValue))
            {
                errorCodes.Add(new ErrorCodeInfo(codeValue, sourceType, fullEnumExpression, typeName, extensionClassName));
            }
            else if (IsPotentialCode(argText) && !argText.Contains('.'))
            {
                if (errorCodes.All(e => e.Code != argText))
                {
                    errorCodes.Add(new ErrorCodeInfo(argText, ErrorCodeSourceType.Constant, null, argText));
                }
            }
        }
    }
}