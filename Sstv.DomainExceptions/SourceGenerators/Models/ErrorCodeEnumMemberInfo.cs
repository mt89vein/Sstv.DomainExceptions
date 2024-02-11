namespace Sstv.DomainExceptions.SourceGenerators.Models;

internal sealed class ErrorCodeEnumMemberInfo
{
    /// <summary>
    /// Name of enum member with original enum name prefix.
    /// </summary>
    /// <example>PetKind.Cat</example>
    public string EnumMemberNameWithEnumName { get; }

    /// <summary>
    /// Integer value of enum.
    /// It can be int, byte, ulong etc.
    /// Thus named integral
    /// </summary>
    public string IntegralValue { get; }

    /// <summary>
    /// Infromation from [ErrorDescriptionAttribute]
    /// </summary>
    public ErrorDescriptionAttributeInfo ErrorDescription { get; }

    /// <summary>
    /// Information from [ObsoleteAttribute].
    /// </summary>
    public bool IsObsolete { get; }

    internal ErrorCodeEnumMemberInfo(
        string enumMemberNameWithEnumName,
        string integralValue,
        ErrorDescriptionAttributeInfo errorDescription,
        bool isObsolete
    )
    {
        EnumMemberNameWithEnumName = enumMemberNameWithEnumName;
        IntegralValue = integralValue;
        ErrorDescription = errorDescription;
        IsObsolete = isObsolete;
    }
}