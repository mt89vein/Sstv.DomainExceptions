namespace Sstv.DomainExceptions.SourceGenerators.Models;

/// <summary>
/// Infromation from [ErrorDescriptionAttribute]
/// </summary>
internal sealed record ErrorDescriptionAttributeInfo
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
    /// Uri for help link.
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// Length of error code.
    /// </summary>
    public int ErrorCodeLength { get; set; } = 5;
}