namespace Sstv.DomainExceptions.Discovery;

/// <summary>
/// Attribute to mark methods that should have their error codes collected.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class CollectErrorCodesAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CollectErrorCodesAttribute"/> class.
    /// </summary>
    public CollectErrorCodesAttribute()
    {
    }
}