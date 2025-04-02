namespace Sstv.DomainExceptions;

/// <summary>
/// Source of error codes description.
/// </summary>
public interface IErrorCodesDescriptionSource
{
    /// <summary>
    /// Returns <see cref="ErrorDescription"/> by <paramref name="errorCode"/>.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public ErrorDescription? GetDescription(string errorCode);

    /// <summary>
    /// Returns enumeration of all error descriptions.
    /// If source is not able to provide this method, then just return Enumerable.Empty.
    /// </summary>
    public IEnumerable<ErrorDescription> Enumerate();
}