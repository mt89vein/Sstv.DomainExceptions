namespace Sstv.DomainExceptions;

/// <summary>
/// Marker attribute to exclude class or method from error code analysis.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
public sealed class ExcludeFromErrorCodeAnalysisAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the justification for excluding the member from error code analysis.
    /// </summary>
    public string? Justification { get; set; }

    /// <summary>
    /// Default ctor.
    /// </summary>
    public ExcludeFromErrorCodeAnalysisAttribute()
    {
    }
}
