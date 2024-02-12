namespace Sstv.DomainExceptions;

/// <summary>
/// Attribute on enum for configuring exception class generation.
/// </summary>
[AttributeUsage(AttributeTargets.Enum)]
public sealed class ExceptionConfigAttribute : Attribute
{
    /// <summary>
    /// The name of generated exception class.
    /// </summary>
    public string? ClassName { get; set; }
}