namespace Sstv.DomainExceptions.SourceGenerators.Models;

/// <summary>
/// Information from attribute [ExceptionConfig].
/// </summary>
internal sealed record ExceptionConfigAttributeInfo
{
    /// <summary>
    /// The name of generated exception class.
    /// </summary>
    public string? ClassName { get; set; }
}