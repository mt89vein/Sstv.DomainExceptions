using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sstv.DomainExceptions.DebugViewer;
using System.Diagnostics;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Builder of domain exception.
/// </summary>
[DebuggerDisplay("ErrorCodesDescriptionSource = {ErrorCodesSourceAddedType.Name}")]
public class DomainExceptionBuilder
{
    /// <summary>
    /// Services registrator.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Configure settings lambda.
    /// </summary>
    public Action<IServiceProvider, DomainExceptionSettings>? ConfigureSettings { get; set; }

    /// <summary>
    /// Error codes source type that should be used.
    /// </summary>
    internal Type? ErrorCodesSourceAddedType { get; set; }

    /// <summary>
    /// Initiates new instance <see cref="DomainExceptionBuilder"/>.
    /// </summary>
    /// <param name="services">Services registrator.</param>
    public DomainExceptionBuilder(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        Services = services;
    }

    /// <summary>
    /// Adds custom error code description source.
    /// </summary>
    /// <typeparam name="TErrorCodesDescriptionSource">Custom error codes description source.</typeparam>
    public DomainExceptionBuilder WithErrorCodesDescriptionSource<TErrorCodesDescriptionSource>()
        where TErrorCodesDescriptionSource : class, IErrorCodesDescriptionSource
    {
        EnsureErrorCodesDescriptionSourceNotAddedYet();
        Services.RemoveAll(typeof(IErrorCodesDescriptionSource));
        Services.AddSingleton<IErrorCodesDescriptionSource, TErrorCodesDescriptionSource>();
        ErrorCodesSourceAddedType = typeof(TErrorCodesDescriptionSource);

        return this;
    }

    /// <summary>
    /// Read error code description from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="sectionName">Configuration section name.</param>
    public DomainExceptionBuilder WithErrorCodesDescriptionFromConfiguration(
        string sectionName = "DomainExceptionSettings:ErrorCodes"
    )
    {
        ArgumentNullException.ThrowIfNull(sectionName);

        EnsureErrorCodesDescriptionSourceNotAddedYet();

        ErrorCodesDescriptionFromConfigurationSource.SectionName = sectionName;
        Services.RemoveAll(typeof(IErrorCodesDescriptionSource));
        Services.AddSingleton<IErrorCodesDescriptionSource, ErrorCodesDescriptionFromConfigurationSource>();
        ErrorCodesSourceAddedType = typeof(ErrorCodesDescriptionFromConfigurationSource);

        return this;
    }

    /// <summary>
    /// Use user provided dictionary as an error code description source.
    /// </summary>
    /// <param name="errorDescriptions">Dictionary of errors.</param>
    public DomainExceptionBuilder WithErrorCodesDescriptionFromMemory(
        IDictionary<string, ErrorDescription> errorDescriptions
    )
    {
        ArgumentNullException.ThrowIfNull(errorDescriptions);

        EnsureErrorCodesDescriptionSourceNotAddedYet();

        Services.RemoveAll(typeof(IErrorCodesDescriptionSource));
        Services.AddSingleton<IErrorCodesDescriptionSource>(
            new ErrorCodesDescriptionInMemorySource(errorDescriptions)
        );
        ErrorCodesSourceAddedType = typeof(ErrorCodesDescriptionInMemorySource);

        return this;
    }

    /// <summary>
    /// Finalize builder.
    /// </summary>
    internal void Build()
    {
        Services.TryAddSingleton<DomainExceptionDebugViewer>();
        Services.AddHostedService<InitHostedService>(sp => new InitHostedService(sp, ConfigureSettings));

        // default registration
        if (ErrorCodesSourceAddedType is null)
        {
            WithErrorCodesDescriptionFromConfiguration();
        }
    }

    /// <summary>
    /// Throw if already registered error codes description source.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// If error codes description source already registered.
    /// </exception>
    private void EnsureErrorCodesDescriptionSourceNotAddedYet()
    {
        if (ErrorCodesSourceAddedType is not null)
        {
            throw new InvalidOperationException(
                $"IErrorCodesDescriptionSource already added with implementation type {ErrorCodesSourceAddedType.FullName}"
            );
        }
    }
}