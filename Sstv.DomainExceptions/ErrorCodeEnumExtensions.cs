using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Sstv.DomainExceptions;

/// <summary>
/// Extensions methods for <see cref="DomainException{T}.ErrorCode"/>
/// </summary>
public static class ErrorCodeEnumExtensions
{
    /// <summary>
    /// ErrorDescription cache for enum value.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<int, ErrorDescription>>
        _errorDescriptionAttributesCache = new();

    /// <summary>
    /// Returns fully prefixed error code for enum value.
    /// </summary>
    /// <param name="errorCodeEnumValue">Error code.</param>
    /// <typeparam name="TErrorCode">Enum that describes error codes.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="ErrorDescriptionAttribute"/> not set on <paramref name="errorCodeEnumValue"/>.
    /// </exception>
    public static string GetPrefixedErrorCode<TErrorCode>(this TErrorCode errorCodeEnumValue)
        where TErrorCode : unmanaged, Enum
    {
        return errorCodeEnumValue.GetErrorDescription().ErrorCode;
    }

    /// <summary>
    /// Returns error description by error code.
    /// </summary>
    /// <param name="errorCodeEnumValue">Error code.</param>
    /// <typeparam name="TErrorCode">Enum that describes error codes.</typeparam>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="ErrorDescriptionAttribute"/> not set on <paramref name="errorCodeEnumValue"/>.
    /// </exception>
    public static ErrorDescription GetErrorDescription<TErrorCode>(this TErrorCode errorCodeEnumValue)
        where TErrorCode : unmanaged, Enum
    {
        return GetErrorDescriptionInternal(
            typeof(TErrorCode),
            enumIntValue: Unsafe.As<TErrorCode, int>(ref errorCodeEnumValue),
            enumStringValue: Enum.GetName(errorCodeEnumValue)!
        );
    }

    /// <summary>
    /// Returns error description by error code.
    /// </summary>
    /// <param name="errorCodeEnumValue">Error code.</param>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="ErrorDescriptionAttribute"/> not set on <paramref name="errorCodeEnumValue"/>.
    /// </exception>
    internal static ErrorDescription GetErrorDescription(Enum errorCodeEnumValue)
    {
        var enumIntValue = ((IConvertible)errorCodeEnumValue).ToInt32(CultureInfo.InvariantCulture.NumberFormat);

        return GetErrorDescriptionInternal(
            errorCodeEnumValue.GetType(),
            enumIntValue,
            errorCodeEnumValue.ToString()
        );
    }

    /// <summary>
    /// Returns error description by enum values with caching.
    /// </summary>
    /// <param name="enumType">Enum type.</param>
    /// <param name="enumIntValue">Enum int backed value.</param>
    /// <param name="enumStringValue">Enum string value.</param>
    /// <exception cref="InvalidOperationException">
    /// When <see cref="ErrorDescriptionAttribute"/> not set on <paramref name="enumType"/>.
    /// </exception>
    private static ErrorDescription GetErrorDescriptionInternal(Type enumType, int enumIntValue, string enumStringValue)
    {
        var dict =
            _errorDescriptionAttributesCache.GetOrAdd(enumType, _ => new ConcurrentDictionary<int, ErrorDescription>());

        return dict.GetOrAdd(enumIntValue, static (enumIntValue, state) =>
        {
            var errorCodeEnumType = state.enumType;
            var field = errorCodeEnumType.GetField(state.enumStringValue);

            var attributeOnEnumValue = field
                ?.GetCustomAttributes(typeof(ErrorDescriptionAttribute), false)
                .Cast<ErrorDescriptionAttribute>()
                .FirstOrDefault();

            var hasObsoleteAttribute = field
                ?.GetCustomAttributes(typeof(ObsoleteAttribute), false)
                .Cast<ObsoleteAttribute>()
                .Any();

            var attributeOnEnum = errorCodeEnumType.GetErrorDescriptionAttributeFromEnum();

            var errorCode = GetPrefixedErrorCodeInternal(attributeOnEnum, attributeOnEnumValue, enumIntValue);
            var helpLink = GetHelpLink(attributeOnEnum, attributeOnEnumValue, errorCode);

            return new ErrorDescription(
                errorCode,
                attributeOnEnumValue?.Description ?? "N/A",
                helpLink,
                isObsolete: hasObsoleteAttribute == true
            );
        }, (enumType, enumStringValue));

        static string? GetHelpLink(
            ErrorDescriptionAttribute? attributeOnEnum,
            ErrorDescriptionAttribute? attributeOnEnumValue,
            string errorCode
        )
        {
            var helpLink = attributeOnEnumValue?.HelpLink;

            if (string.IsNullOrWhiteSpace(helpLink) && !string.IsNullOrWhiteSpace(attributeOnEnum?.HelpLink))
            {
                helpLink = string.Format(CultureInfo.InvariantCulture, attributeOnEnum.HelpLink, errorCode);
            }

            return helpLink;
        }

        static string GetPrefixedErrorCodeInternal(
            ErrorDescriptionAttribute? attributeOnEnum,
            ErrorDescriptionAttribute? attributeOnEnumValue,
            int errorCodeEnumValue
        )
        {
            if (string.IsNullOrWhiteSpace(attributeOnEnum?.Prefix) &&
                string.IsNullOrWhiteSpace(attributeOnEnumValue?.Prefix))
            {
                throw new InvalidOperationException(
                    "No error prefix set. Provide ErrorDescriptionAttribute on enum or its value with Prefix property.");
            }

            var prefix = attributeOnEnumValue?.Prefix ?? attributeOnEnum?.Prefix;
            var errorCodeLength = attributeOnEnumValue?.ErrorCodeLength ?? attributeOnEnum?.ErrorCodeLength;

            var code = errorCodeEnumValue
                .ToString(CultureInfo.InvariantCulture)
                .PadLeft(errorCodeLength.GetValueOrDefault(5), '0');

            return $"{prefix}{code}";
        }
    }
}