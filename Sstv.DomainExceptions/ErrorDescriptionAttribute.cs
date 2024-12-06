namespace Sstv.DomainExceptions;

/// <summary>
/// Attribute for enum and enum value for adding error code description.
/// </summary>
[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
public sealed class ErrorDescriptionAttribute : Attribute
{
    /// <summary>
    /// Prefix for use.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Description of error.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Criticality level.
    /// </summary>
    public Level Level { get; set; }

    /// <summary>
    /// Uri for help link.
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Length of error code.
    /// </summary>
    public int ErrorCodeLength { get; set; } = 5;
}