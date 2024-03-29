﻿namespace Sstv.DomainExceptions;

/// <summary>
/// Dictionary as an error codes description source.
/// </summary>
public sealed class ErrorCodesDescriptionInMemorySource : IErrorCodesDescriptionSource
{
    /// <summary>
    /// User provided dictionary.
    /// </summary>
    private readonly IReadOnlyDictionary<string, ErrorDescription> _errorDescriptions;

    /// <summary>
    /// Initiates new instance of <see cref="ErrorCodesDescriptionInMemorySource"/>.
    /// </summary>
    /// <param name="errorDescriptions">User provided dictionary.</param>
    /// <exception cref="ArgumentNullException">
    /// When <paramref name="errorDescriptions"/> was null.
    /// </exception>
    public ErrorCodesDescriptionInMemorySource(
        IReadOnlyDictionary<string, ErrorDescription> errorDescriptions
    )
    {
        _errorDescriptions = errorDescriptions ??
                             throw new ArgumentNullException(nameof(errorDescriptions), "errorDescriptions cannot be null");
    }

    /// <summary>
    /// Returns <see cref="ErrorDescription"/> by <paramref name="errorCode"/>.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    public ErrorDescription? GetDescription(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            return null;
        }

        return _errorDescriptions.TryGetValue(errorCode, out var errorDescription)
            ? errorDescription
            : null;
    }

    /// <summary>
    /// Returns enumeration of all error descriptions.
    /// If source is not able to provide this method, then just return Enumerable.Empty.
    /// </summary>
    public IEnumerable<ErrorDescription> Enumerate()
    {
        return _errorDescriptions.Values;
    }
}