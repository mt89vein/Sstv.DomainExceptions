namespace Sstv.DomainExceptions;

/// <summary>
/// Settings.
/// </summary>
public sealed class DomainExceptionSettings
{
    /// <summary>
    /// Backing field of <see cref="ErrorCodesDescriptionSource"/>.
    /// </summary>
    private IErrorCodesDescriptionSource? _errorCodesDescriptionSource;

    /// <summary>
    /// Lazy initializer of singleton.
    /// </summary>
    private static readonly Lazy<DomainExceptionSettings> _lazy = new(() => new DomainExceptionSettings());

    /// <summary>
    /// Lazy initializer of <see cref="ErrorCodesDescriptionSource"/>.
    /// </summary>
    internal static Lazy<IErrorCodesDescriptionSource?>? ErrorDescriptionSourceGetter { get; set; }

    /// <summary>
    /// Instance of settings.
    /// </summary>
    public static DomainExceptionSettings Instance => _lazy.Value;

    /// <summary>
    /// Exception should be thrown, when <see cref="IErrorCodesDescriptionSource"/> returns null.
    /// </summary>
    public bool ThrowIfHasNoErrorCodeDescription { get; set; } = true;

    /// <summary>
    /// Collect error codes metric automatically.
    /// </summary>
    public bool CollectErrorCodesMetricAutomatically { get; set; } = true;

    /// <summary>
    /// Generate exception id automatically.
    /// </summary>
    public bool GenerateExceptionIdAutomatically { get; set; } = true;

    /// <summary>
    /// Allows to filter additonal data from DomainException to ProblemDetails response.
    /// </summary>
    public Func<AdditionalDataPropertyFilterArgs, bool>? AdditionalDataResponseIncludingFilter { get; set; }

    /// <summary>
    /// Add criticality level to additional data.
    /// </summary>
    public bool AddCriticalityLevel { get; set; } = true;

    /// <summary>
    /// Source of error codes description.
    /// </summary>
    public IErrorCodesDescriptionSource? ErrorCodesDescriptionSource
    {
        get
        {
            if (_errorCodesDescriptionSource is not null)
            {
                return _errorCodesDescriptionSource;
            }

            return _errorCodesDescriptionSource = ErrorDescriptionSourceGetter?.Value;
        }
        set => _errorCodesDescriptionSource = value;
    }

    /// <summary>
    /// Invoked, when <see cref="IErrorCodesDescriptionSource"/> returns null.
    /// </summary>
    public Func<string, ErrorDescription>? DefaultErrorDescriptionProvider { get; set; }

    /// <summary>
    /// Invoked, when exception created.
    /// </summary>
    [Obsolete("Use OnErrorCreated instead.", error: true)]
    public Action<DomainException>? OnExceptionCreated { get; set; }

    /// <summary>
    /// Invoked, when error created.
    /// </summary>
    public Action<ErrorDescription, object?>? OnErrorCreated { get; set; }

    /// <summary>
    /// Hides public constructor.
    /// </summary>
    private DomainExceptionSettings()
    {
    }
}