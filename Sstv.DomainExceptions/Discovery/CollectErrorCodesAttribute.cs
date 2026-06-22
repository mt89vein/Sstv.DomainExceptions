namespace Sstv.DomainExceptions;

/// <summary>
/// Marker attribute to enable ErrorCodesCollector source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class CollectErrorCodesAttribute : Attribute
{
    /// <summary>
    /// Maximum depth of call chain propagation. Default is 10.
    /// </summary>
    public int MaxPropagationDepth { get; set; } = 10;

    /// <summary>
    /// Name of the generated partial class. Default is "ErrorCodeMethodCollector".
    /// </summary>
    public string? ClassName { get; set; }

    /// <summary>
    /// Types to scan for error codes. If null or empty, all types are scanned.
    /// </summary>
    public Type[]? Types { get; set; }
}