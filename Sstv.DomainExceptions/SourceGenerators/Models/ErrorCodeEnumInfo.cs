using System.Globalization;

namespace Sstv.DomainExceptions.SourceGenerators.Models;

/// <summary>
/// Contains all information from enum that decorated with [ErrorDescription] attribute.
/// </summary>
internal sealed class ErrorCodeEnumInfo
{
    private string? _cachedVariableNameNoSuffix;

    /// <summary>
    /// Enum name.
    /// </summary>
    public string EnumName { get; }

    /// <summary>
    /// Target namespace for generated source files.
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    /// Members of enum.
    /// </summary>
    public ErrorCodeEnumMemberInfo[] Members { get; }

    /// <summary>
    /// Error description attribute values on enum.
    /// </summary>
    public ErrorDescriptionAttributeInfo ErrorDescription { get; }

    /// <summary>
    /// Exception generation config.
    /// </summary>
    public ExceptionConfigAttributeInfo ExceptionConfigAttributeInfo { get; set; }

    internal ErrorCodeEnumInfo(
        string enumEnumName,
        string @namespace,
        ErrorDescriptionAttributeInfo errorDescription,
        ExceptionConfigAttributeInfo exceptionConfigAttributeInfo,
        ErrorCodeEnumMemberInfo[] members
    )
    {
        EnumName = enumEnumName;
        Namespace = @namespace;
        Members = members;
        ErrorDescription = errorDescription;
        ExceptionConfigAttributeInfo = exceptionConfigAttributeInfo;
    }

    /// <summary>
    /// Returns variable name from enum name.
    /// </summary>
    /// <returns>Variable name.</returns>
    public string GetVariableName()
    {
        return _cachedVariableNameNoSuffix ??= char.ToLower(EnumName[0], CultureInfo.InvariantCulture) + EnumName.Substring(1);
    }
}