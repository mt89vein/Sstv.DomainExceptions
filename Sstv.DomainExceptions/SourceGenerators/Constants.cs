namespace Sstv.DomainExceptions.SourceGenerators;

/// <summary>
/// Constants.
/// </summary>
internal sealed class Constants
{
    /// <summary>
    /// Library main namespace.
    /// </summary>
    public const string NAMESPACE = "Sstv.DomainExceptions";

    /// <summary>
    /// Attribute class name, that should be placed on top of enum with error codes.
    /// </summary>
    public const string ATTRIBUTE_CLASS_NAME = "ErrorDescription";

    /// <summary>
    /// Full name of <see cref="ATTRIBUTE_CLASS_NAME"/>.
    /// </summary>
    public const string ERROR_DESCRIPTION_ATTRIBUTE_FULL_NAME = NAMESPACE + "." + ATTRIBUTE_CLASS_NAME + "Attribute";

    /// <summary>
    /// Error description holder class name.
    /// </summary>
    public const string ERROR_DESCRIPTION_CLASS_NAME = "ErrorDescription";

    /// <summary>
    /// Exception config attribute.
    /// </summary>
    public const string EXCEPTION_CONFIG_ATTRIBUTE_FULL_NAME = NAMESPACE + "." + "ExceptionConfigAttribute";

    public static class NamedArguments
    {
        public const string HELP_LINK = "HelpLink";
        public const string PREFIX = "Prefix";
        public const string DESCRIPTION = "Description";
        public const string ERROR_CODE_LENGTH = "ErrorCodeLength";
        public const string CLASS_NAME = "ClassName";
    }
}