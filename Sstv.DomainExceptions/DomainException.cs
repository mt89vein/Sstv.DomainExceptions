using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions;

/// <summary>
/// Domain exception.
/// </summary>
[DebuggerDisplay("{ErrorCode}: {Message}, {DetailedMessage}")]
[SuppressMessage("Design", "CA1032:Implement standard exception constructors")]
public abstract class DomainException : Exception
{
    /// <summary>
    /// Error description.
    /// </summary>
    private readonly ErrorDescription _errorDescription;

    /// <summary>
    /// Additional data.
    /// </summary>
    private readonly Dictionary<string, object> _additionalData = new();

    /// <summary>
    /// Error code.
    /// </summary>
    public virtual string ErrorCode => _errorDescription.ErrorCode;

    /// <summary>
    /// Error message for user, without user-specific data.
    /// </summary>
    public override string Message => _errorDescription.Description;

    /// <summary>
    /// Help link.
    /// </summary>
    public override string? HelpLink => _errorDescription.HelpLink;

    /// <summary>
    /// Gets a collection of key/value pairs that provide additional user-defined information about the exception.
    /// </summary>
    public override IDictionary Data => _additionalData;

    /// <summary>
    /// Detailed message with user-specific data.
    /// </summary>
    public virtual string? DetailedMessage { get; internal set; }

    /// <summary>
    /// Creates new instance of <see cref="DomainException"/>.
    /// </summary>
    /// <param name="errorDescription">Error description.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <exception cref="ArgumentNullException">
    /// When <paramref name="errorDescription"/> is null.
    /// </exception>
    protected DomainException(
        ErrorDescription errorDescription,
        Exception? innerException = null
    ) : base(message: null, innerException)
    {
        _errorDescription = errorDescription ??
                            throw new ArgumentNullException(nameof(errorDescription), "errorDescription cannot be null");

        if (errorDescription.AdditionalData is not null)
        {
            foreach (var kv in errorDescription.AdditionalData)
            {
                if (!_additionalData.ContainsKey(kv.Key))
                {
                    _additionalData.Add(kv.Key, kv.Value);
                }
            }
        }

        if (DomainExceptionSettings.Instance.GenerateExceptionIdAutomatically)
        {
            WithErrorId();
        }

        if (DomainExceptionSettings.Instance.AddCriticalityLevel)
        {
            const string CRITICALITY_LEVEL = "CriticalityLevel";
            if (!_additionalData.ContainsKey(CRITICALITY_LEVEL))
            {
                _additionalData.Add(CRITICALITY_LEVEL, _errorDescription.Level.ToString());
            }
        }

        DomainExceptionSettings.Instance.OnErrorCreated?.Invoke(_errorDescription, this);
    }

    /// <summary>
    /// Creates new instance of <see cref="DomainException"/>.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    /// <param name="innerException">Inner exception.</param>
    /// <exception cref="ArgumentNullException">
    /// When <paramref name="errorCode"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// If <see cref="IErrorCodesDescriptionSource"/> returns null for error code.
    /// </exception>
    protected DomainException(string errorCode, Exception? innerException = null)
        : this(GetErrorDescription(errorCode), innerException)
    {
    }

    /// <summary>
    /// Sets <see cref="DetailedMessage"/>.
    /// </summary>
    /// <param name="detailedMessage">Detailed message with user-specific data.</param>
    public DomainException WithDetailedMessage(string? detailedMessage)
    {
        DetailedMessage = detailedMessage;

        return this;
    }

    /// <summary>
    /// Adds dictionary to additional data.
    /// </summary>
    /// <param name="additionalData">Additional data.</param>
    public DomainException WithAdditionalData(Dictionary<string, object> additionalData)
    {
        _ = additionalData ?? throw new ArgumentNullException(nameof(additionalData), "additionalData cannot be null");

        foreach (var kv in additionalData)
        {
            if (!_additionalData.ContainsKey(kv.Key))
            {
                _additionalData.Add(kv.Key, kv.Value);
            }
        }

        return this;
    }

    /// <summary>
    /// Adds key value to additional data.
    /// </summary>
    /// <param name="key">Key of data</param>
    /// <param name="value">Value of data.</param>
    public DomainException WithAdditionalData(string key, object value)
    {
        _ = key ?? throw new ArgumentNullException(nameof(key), "key cannot be null");
        _ = value ?? throw new ArgumentNullException(nameof(value), "value cannot be null");

        if (!_additionalData.ContainsKey(key))
        {
            _additionalData.Add(key, value);
        }

        return this;
    }

    /// <summary>
    /// Adds KeyValuePair to additional data.
    /// </summary>
    /// <param name="additionalData">Key value pair.</param>
    public DomainException WithAdditionalData(KeyValuePair<string, object> additionalData)
    {
        _ = additionalData.Key ?? throw new ArgumentNullException(nameof(additionalData), "key cannot be null");
        _ = additionalData.Value ?? throw new ArgumentNullException(nameof(additionalData), "value cannot be null");

        if (!_additionalData.ContainsKey(additionalData.Key))
        {
            _additionalData.Add(additionalData.Key, additionalData.Value);
        }

        return this;
    }

    /// <summary>
    /// Marks exception with unique id.
    /// </summary>
    /// <param name="errorId">Unique id. If not provided, Guid.NewGuid() would be used.</param>
    public DomainException WithErrorId(string? errorId = null)
    {
        const string KEY = "ErrorId";

        if (!_additionalData.ContainsKey(KEY))
        {
            _additionalData.Add(KEY, errorId ?? Guid.NewGuid().ToString());
        }

        return this;
    }

    /// <summary>
    /// String representation of error for user.
    /// </summary>
    public string ToUserViewString()
    {
        return $"{ErrorCode}: {Message}";
    }

    /// <summary>
    /// String representation of error.
    /// </summary>
    public override string ToString()
    {
        var details = string.IsNullOrWhiteSpace(DetailedMessage)
            ? string.Empty
            : $", {DetailedMessage}";

        return ToUserViewString() + details + "\r\n" + base.ToString();
    }

    /// <summary>
    /// Returns current error description.
    /// </summary>
    /// <returns>Error description.</returns>
    public ErrorDescription GetDescription()
    {
        return _errorDescription;
    }

    /// <summary>
    /// Returns error description by error code.
    /// </summary>
    /// <param name="errorCode">Error code.</param>
    /// <returns>Error description.</returns>
    /// <exception cref="InvalidOperationException">
    /// If <see cref="IErrorCodesDescriptionSource"/> returns null for error code.
    /// </exception>
    private static ErrorDescription GetErrorDescription(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode))
        {
            throw new ArgumentNullException(nameof(errorCode), "errorcode cannot be null or empty");
        }

        var instance = DomainExceptionSettings.Instance;

        var errorDescription = instance.ErrorCodesDescriptionSource?.GetDescription(errorCode);

        if (errorDescription is not null)
        {
            return errorDescription;
        }

        if (instance.ThrowIfHasNoErrorCodeDescription)
        {
            throw new InvalidOperationException(
                $"There is no error code description from source for error code {errorCode}"
            );
        }

        return instance.DefaultErrorDescriptionProvider?.Invoke(errorCode) ??
               new ErrorDescription(errorCode, "N/A");
    }
}