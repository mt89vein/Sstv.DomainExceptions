using System.Diagnostics;

namespace Sstv.DomainExceptions;

/// <summary>
/// Describes error with additional data.
/// </summary>
[DebuggerDisplay("[{Level} {ErrorCode}]: {Description}")]
public class ErrorDescription
{
    /// <summary>
    /// Error code.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Static error code description for user.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Help link for read about the error.
    /// </summary>
    public string? HelpLink { get; }

    /// <summary>
    /// Criticality level.
    /// </summary>
    public Level Level { get; }

    /// <summary>
    /// Additional data, that might be helpful for error handling.
    /// </summary>
    public IReadOnlyDictionary<string, object>? AdditionalData { get; }

    /// <summary>
    /// Creates new instance of <see cref="ErrorDescription"/>.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    /// <param name="description">Static error code description for user.</param>
    /// <param name="level">Criticality level.</param>
    /// <param name="helpLink">Help link for read about the error.</param>
    /// <param name="additionalData">Additional data, that might be helpful for error handling.</param>
    public ErrorDescription(
        string errorCode,
        string description,
        Level level = Level.Undefined,
        string? helpLink = null,
        IReadOnlyDictionary<string, object>? additionalData = null
    )
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentException("ErrorCode cannot be null or empty string", paramName: nameof(errorCode));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description cannot be null or empty string", paramName: nameof(description));
        }

        ErrorCode = errorCode;
        Description = description;
        HelpLink = helpLink;
        AdditionalData = additionalData;
        Level = level;
    }
}