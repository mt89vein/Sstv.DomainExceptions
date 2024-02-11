using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Sstv.DomainExceptions.DebugViewer;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Builder of domain exception.
/// </summary>
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
    /// Is manually added error codes source?
    /// </summary>
    internal bool ErrorCodesSourceManuallyAdded { get; set; }

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
        Services.AddSingleton<IErrorCodesDescriptionSource, TErrorCodesDescriptionSource>();
        ErrorCodesSourceManuallyAdded = true;

        return this;
    }

    /// <summary>
    /// Adds custom error code description source.
    /// </summary>
    /// <typeparam name="TErrorCodesDescriptionSource">Custom error codes description source.</typeparam>
    public DomainExceptionBuilder WithErrorCodesDescriptionSource<TErrorCodesDescriptionSource>(TErrorCodesDescriptionSource instance)
        where TErrorCodesDescriptionSource : class, IErrorCodesDescriptionSource
    {
        Services.AddSingleton<IErrorCodesDescriptionSource>(instance);
        ErrorCodesSourceManuallyAdded = true;

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

        Services.AddSingleton<IErrorCodesDescriptionSource>(sp =>
            new ErrorCodesDescriptionFromConfigurationSource(sp.GetRequiredService<IConfiguration>().GetSection(sectionName))
        );
        ErrorCodesSourceManuallyAdded = true;

        return this;
    }

    /// <summary>
    /// Use user provided dictionary as an error code description source.
    /// </summary>
    /// <param name="errorDescriptions">Dictionary of errors.</param>
    public DomainExceptionBuilder WithErrorCodesDescriptionFromMemory(
        IReadOnlyDictionary<string, ErrorDescription> errorDescriptions
    )
    {
        ArgumentNullException.ThrowIfNull(errorDescriptions);

        Services.AddSingleton<IErrorCodesDescriptionSource>(
            new ErrorCodesDescriptionInMemorySource(errorDescriptions)
        );
        ErrorCodesSourceManuallyAdded = true;

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
        if (!ErrorCodesSourceManuallyAdded)
        {
            WithErrorCodesDescriptionFromConfiguration();
        }
    }
}