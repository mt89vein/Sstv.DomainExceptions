using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;

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

internal sealed record ErrorCodeInfo(
    string Code,
    ErrorCodeSourceType SourceType,
    string? FullEnumExpression = null,
    string? SourceTypeName = null,
    string? ExtensionClassName = null);

internal sealed record MethodAnalysis(
    string Key,
    ImmutableArray<ErrorCodeInfo> ErrorCodes,
    ImmutableArray<string> CalledKeys);

internal sealed class GeneratorSettings
{
    public bool IsEnabled { get; set; }
    public int? MaxPropagationDepth { get; set; }
    public string? ClassName { get; set; }
    public string? AssemblyName { get; set; }
    public ImmutableHashSet<string>? AllowedTypes { get; set; }
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