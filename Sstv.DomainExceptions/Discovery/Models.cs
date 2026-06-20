using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sstv.DomainExceptions.Discovery;

internal sealed class MethodInfo
{
    public string TypeName { get; set; } = "";
    public string MethodName { get; set; } = "";
    public string Namespace { get; set; } = "";
    public MethodDeclarationSyntax? SyntaxNode { get; set; }
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
    public string? ExtensionClassName { get; }

    public ErrorCodeInfo(string code, ErrorCodeSourceType sourceType, string? fullEnumExpression = null,
        string? sourceTypeName = null, string? extensionClassName = null)
    {
        Code = code;
        SourceType = sourceType;
        FullEnumExpression = fullEnumExpression;
        SourceTypeName = sourceTypeName;
        ExtensionClassName = extensionClassName;
    }
}

internal sealed class GeneratorSettings
{
    public bool IsEnabled { get; set; }
    public int? MaxPropagationDepth { get; set; }
    public string? ClassName { get; set; }
}

internal static class ErrorCodeAnalysis
{
    public static string? GetExtensionClassName(ITypeSymbol? enumType)
    {
        if (enumType is not INamedTypeSymbol { TypeKind: TypeKind.Enum } namedType)
        {
            return null;
        }

        try
        {
            var exceptionConfigAttr = namedType.GetAttributes().FirstOrDefault(a =>
                a.AttributeClass?.Name is "ExceptionConfigAttribute" or "ExceptionConfig");

            string? exceptionClassName = null;
            if (exceptionConfigAttr is not null)
            {
                if (exceptionConfigAttr.ConstructorArguments.Length > 0 &&
                    exceptionConfigAttr.ConstructorArguments[0].Value is string ctorValue)
                {
                    exceptionClassName = ctorValue;
                }
                else
                {
                    foreach (var namedArg in exceptionConfigAttr.NamedArguments)
                    {
                        if (namedArg.Key == "ClassName" && namedArg.Value.Value is string namedValue)
                        {
                            exceptionClassName = namedValue;
                            break;
                        }
                    }
                }
            }

            exceptionClassName ??= namedType.Name + "Exception";

            var ns = namedType.ContainingNamespace?.ToDisplayString() ?? "";
            return string.IsNullOrEmpty(ns)
                ? exceptionClassName + "Extensions"
                : ns + "." + exceptionClassName + "Extensions";
        }
        catch
        {
            return null;
        }
    }
}
