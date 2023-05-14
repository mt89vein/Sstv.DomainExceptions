using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
    /// Source of error codes description.
    /// </summary>
    private readonly IErrorCodesDescriptionSource _errorCodesDescriptionSource;

    /// <summary>
    /// Initiates new instance of <see cref="DomainExceptionDebugViewer"/>.
    /// </summary>
    /// <param name="enrichers">Debug view model enrichers.</param>
    /// <param name="errorCodesDescriptionSource">Source of error codes description.</param>
    public DomainExceptionDebugViewer(
        IEnumerable<IDomainExceptionDebugEnricher> enrichers,
        IErrorCodesDescriptionSource errorCodesDescriptionSource
    )
    {
        _enrichers = enrichers;
        _errorCodesDescriptionSource = errorCodesDescriptionSource;
    }

    /// <summary>
    /// Returns error codes defined in <paramref name="assemblies"/>.
    /// </summary>
    /// <returns>Found error codes.</returns>
    public DomainExceptionDebugVm DebugView(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var dic = new Dictionary<string, DomainExceptionCodeDebugVm>();

        var domainExceptionTypes = ReflectionHelper.GetDomainExceptionTypes(assemblies);

        foreach (var domainExceptionType in domainExceptionTypes)
        {
            if (!domainExceptionType.BaseType!.IsGenericType)
            {
                continue;
            }

            if (domainExceptionType.BaseType!.GetGenericTypeDefinition() != typeof(DomainException<>))
            {
                continue;
            }

            var domainErrorCodeEnumType = domainExceptionType.GetDomainErrorCodeEnumType();
            var domainErrorCodeEnumValues = Enum.GetValues(domainErrorCodeEnumType);

            foreach (Enum enumValue in domainErrorCodeEnumValues)
            {
                var errorDescription = ErrorCodeEnumExtensions.GetErrorDescription(enumValue);

                var vm = Map(errorDescription);

                vm.ExceptionType = domainExceptionType.FullName!;
                vm.AssemblyName = domainExceptionType.Assembly.FullName!;
                vm.ErrorCodesEnumType = domainErrorCodeEnumType.FullName!;

                dic.TryAdd(errorDescription.ErrorCode, vm);

                foreach (var enricher in _enrichers)
                {
                    enricher.Enrich(vm);
                }
            }
        }

        var errorDescriptions = _errorCodesDescriptionSource.Enumerate();

        foreach (var errorDescription in errorDescriptions)
        {
            if (!dic.ContainsKey(errorDescription.ErrorCode))
            {
                var vm = Map(errorDescription);
                dic.TryAdd(errorDescription.ErrorCode, vm);

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

    private static DomainExceptionCodeDebugVm Map(ErrorDescription errorDescription)
    {
        return new DomainExceptionCodeDebugVm
        {
            Code = errorDescription.ErrorCode,
            HelpLink = errorDescription.HelpLink,
            AdditionalData = errorDescription.AdditionalData is not null && errorDescription.AdditionalData.Count > 0
                ? new Dictionary<string, object>(errorDescription.AdditionalData)
                : null,
            Message = errorDescription.Description,
            IsObsolete = errorDescription.IsObsolete
        };
    }
}