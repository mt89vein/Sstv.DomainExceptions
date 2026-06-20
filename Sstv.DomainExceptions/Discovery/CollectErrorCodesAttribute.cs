namespace Sstv.DomainExceptions;

/// <summary>
/// Marker attribute to enable ErrorCodesCollector source generator.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly)]
public sealed class CollectErrorCodesAttribute : Attribute;