using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions.DebugViewer;

/// <summary>
/// <see cref="DomainException"/> debug viewer.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DomainExceptionDebugViewer
{
    /// <summary>
    /// Debug view model enrichers.
    /// </summary>
    private readonly IEnumerable<IDomainExceptionDebugEnricher> _enrichers;

    /// <summary>
    /// Initiates new instance of <see cref="DomainExceptionDebugViewer"/>.
    /// </summary>
    /// <param name="enrichers">Debug view model enrichers.</param>
    public DomainExceptionDebugViewer(IEnumerable<IDomainExceptionDebugEnricher> enrichers)
    {
        _enrichers = enrichers;
    }

    /// <summary>
    /// Returns error codes.
    /// </summary>
    /// <returns>Found error codes.</returns>
    public DomainExceptionDebugVm DebugView()
    {
        if (DomainExceptionSettings.Instance.ErrorCodesDescriptionSource is null)
        {
            throw new InvalidOperationException("There is no ErrorCodesDescriptionSource");
        }

        var dic = new Dictionary<string, DomainExceptionCodeDebugVm>();

        foreach (var errorDescription in DomainExceptionSettings.Instance.ErrorCodesDescriptionSource!.Enumerate())
        {
            if (!dic.ContainsKey(errorDescription.ErrorCode))
            {
                var vm = new DomainExceptionCodeDebugVm
                {
                    Code = errorDescription.ErrorCode,
                    HelpLink = errorDescription.HelpLink,
                    AdditionalData = errorDescription.AdditionalData is not null &&
                                     errorDescription.AdditionalData.Count > 0
                        ? errorDescription.AdditionalData.ToDictionary(x => x.Key, x => x.Value)
                        : null,
                    Message = errorDescription.Description,
                    IsObsolete = errorDescription.IsObsolete
                };

                dic.Add(errorDescription.ErrorCode, vm);

                foreach (var enricher in _enrichers)
                {
                    enricher.Enrich(vm);
                }
            }
        }

        return new DomainExceptionDebugVm
        {
            ErrorCodes = dic.Values.ToArray()
        };
    }
}