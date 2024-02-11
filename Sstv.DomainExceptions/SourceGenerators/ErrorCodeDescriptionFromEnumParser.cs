using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Sstv.DomainExceptions.SourceGenerators.Models;

namespace Sstv.DomainExceptions.SourceGenerators;

/// <summary>
/// Extract [ErrorCodeDescription] and other things from given enum symbol.
/// </summary>
internal static class ErrorCodeDescriptionFromEnumParser
{
    /// <summary>
    /// Extract [ErrorCodeDescription] from given <paramref name="enumSymbol"/>.
    /// </summary>
    /// <param name="enumSymbol">Symbol represents enum.</param>
    /// <param name="errorDescriptionAttributeSymbol">ErrorCodeDescriptionAttribute type symbol.</param>
    /// <returns>Extracted info from enum</returns>
    /// <remarks>
    /// Given <paramref name="enumSymbol"/> MUST be marked with [ErrorCodeDescription].
    /// </remarks>
    public static ErrorCodeEnumInfo ParseEnum(
        INamedTypeSymbol enumSymbol,
        INamedTypeSymbol errorDescriptionAttributeSymbol
    )
    {
        if (enumSymbol is null)
        {
            throw new ArgumentNullException(nameof(enumSymbol), "enumSymbol cannot be null");
        }
        if (errorDescriptionAttributeSymbol is null)
        {
            throw new ArgumentNullException(nameof(errorDescriptionAttributeSymbol), "errorDescriptionAttributeSymbol cannot be null");
        }

        var members = enumSymbol.GetMembers();

        var attributeInfo = ExtractErrorDescriptionAttributeInfo(enumSymbol, errorDescriptionAttributeSymbol);

        var @namespace = GetResultNamespace(enumSymbol);
        var enumName = SymbolDisplay.FormatLiteral(enumSymbol.Name, false);

        var memberInfos = members
            .OfType<IFieldSymbol>()
            // Skip all non enum fields declarations
            // Enum members are all const, according to docs
            .Where(static m => m is { IsConst: true, HasConstantValue: true })
            // Try to convert them into EnumMemberInfo
            .Select(p => ParseEnumMember(p, errorDescriptionAttributeSymbol)!)
            // And skip failed
            .Where(static i => i is not null)
            // Finally, create array of members
            .ToArray();


        return new ErrorCodeEnumInfo(
            enumName,
            @namespace,
            attributeInfo,
            memberInfos
        );
    }

    /// <summary>
    /// Returns <see cref="ErrorCodeEnumMemberInfo"/> from enum member.
    /// </summary>
    /// <param name="fieldSymbol">Enum member field.</param>
    /// <param name="errorDescriptionAttributeSymbol">ErrorCodeDescriptionAttribute type symbol.</param>
    private static ErrorCodeEnumMemberInfo? ParseEnumMember(
        IFieldSymbol fieldSymbol,
        INamedTypeSymbol errorDescriptionAttributeSymbol
    )
    {
        // For enum member this must be true
        if (!fieldSymbol.IsConst)
        {
            return null;
        }

        var enumMemberNameWithPrefix = $"{fieldSymbol.ContainingType.Name}.{fieldSymbol.Name}";

        var integralValue = fieldSymbol.ConstantValue?.ToString() ??
                            throw new ArgumentNullException(nameof(fieldSymbol), "field symbol has no value");

        var errorDescriptionAttributeInfo = ExtractErrorDescriptionAttributeInfo(fieldSymbol, errorDescriptionAttributeSymbol);

        return new ErrorCodeEnumMemberInfo(
            enumMemberNameWithPrefix,
            integralValue,
            errorDescriptionAttributeInfo,
            IsObsolete(fieldSymbol)
        );
    }

    /// <summary>
    /// Returns namespace, where we should place generated code.
    /// </summary>
    /// <param name="enumSymbol">Enum symbol.</param>
    /// <returns>Namespace.</returns>
    private static string GetResultNamespace(INamedTypeSymbol enumSymbol)
    {
        return enumSymbol.ContainingNamespace
            .ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", "");
    }

    /// <summary>
    /// Returns true, if enum member marked as obsolete.
    /// </summary>
    /// <param name="fieldSymbol">Enum member field.</param>
    private static bool IsObsolete(IFieldSymbol fieldSymbol)
    {
        return fieldSymbol
            .GetAttributes()
            .Any(x => x.AttributeClass?.Name.Contains("Obsolete") == true);
    }

    /// <summary>
    /// Method to extract information from [ErrorDescription] attribute: properties, arguments etc...
    /// </summary>
    private static ErrorDescriptionAttributeInfo ExtractErrorDescriptionAttributeInfo(
        ISymbol enumSymbol,
        INamedTypeSymbol errorDescriptionAttributeSymbol
    )
    {
        var info = new ErrorDescriptionAttributeInfo();

        // Search for [ErrorDescriptionAttribute] with at least 1 set property
        if (enumSymbol.GetAttributes() is { Length: > 0 } attributes &&
            attributes.FirstOrDefault(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, errorDescriptionAttributeSymbol)) is
            {
                NamedArguments.Length: > 0
            } attrInstance)
        {
            foreach (var namedArgument in attrInstance.NamedArguments)
            {
                // Prevent nulls
                if (namedArgument.Value is not
                    {
                        IsNull: false,
                        Kind: TypedConstantKind.Primitive,
                        Value: not null
                    } value)
                {
                    continue;
                }

                switch (namedArgument.Key)
                {
                    case Constants.NamedArguments.PREFIX:
                        info = info with { Prefix = GetConstantStringValue(value) };
                        break;
                    case Constants.NamedArguments.DESCRIPTION:
                        info = info with { Description = GetConstantStringValue(value) };
                        break;
                    case Constants.NamedArguments.HELP_LINK:
                        info = info with { HelpLink = GetConstantStringValue(value) };
                        break;
                    case Constants.NamedArguments.ERROR_CODE_LENGTH:
                        info = info with { ErrorCodeLength = value.Value is int intValue ? intValue : int.Parse(GetConstantStringValue(value)!) };
                        break;
                    default:
                        break;
                }
            }
        }

        return info;

        static string? GetConstantStringValue(TypedConstant constant)
        {
            return constant.Value?.ToString() is { Length: > 0 } notEmptyString &&
                   !string.IsNullOrWhiteSpace(notEmptyString)
                ? notEmptyString
                : null;
        }
    }
}