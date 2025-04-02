namespace Sstv.DomainExceptions;

/// <summary>
/// Arguments for filtering additional data.
/// </summary>
public readonly struct AdditionalDataPropertyFilterArgs : IEquatable<AdditionalDataPropertyFilterArgs>
{
    /// <summary>
    /// Domain exception that processing.
    /// </summary>
    public DomainException DomainException { get; }

    /// <summary>
    /// The key.
    /// </summary>
    public string? Key { get; }

    /// <summary>
    /// value
    /// </summary>
    public object? Value { get; }

    /// <summary>
    /// Arguments for filtering additional data.
    /// </summary>
    /// <param name="domainException">Domain exception that processing.</param>
    /// <param name="key">The key.</param>
    /// <param name="value">It's value.</param>
    public AdditionalDataPropertyFilterArgs(DomainException domainException, string? key, object? value)
    {
        DomainException = domainException;
        Key = key;
        Value = value;
    }

    /// <inheritdoc />
    public bool Equals(AdditionalDataPropertyFilterArgs other)
    {
        return DomainException.Equals(other.DomainException) &&
               string.Equals(Key, other.Key, StringComparison.OrdinalIgnoreCase) &&
               Equals(Value, other.Value);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is AdditionalDataPropertyFilterArgs other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = DomainException.GetHashCode();
            hashCode = (hashCode * 397) ^ (Key != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Key) : 0);
            hashCode = (hashCode * 397) ^ (Value != null ? Value.GetHashCode() : 0);
            return hashCode;
        }
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(AdditionalDataPropertyFilterArgs left, AdditionalDataPropertyFilterArgs right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Unequality operator.
    /// </summary>
    public static bool operator !=(AdditionalDataPropertyFilterArgs left, AdditionalDataPropertyFilterArgs right)
    {
        return !left.Equals(right);
    }
}