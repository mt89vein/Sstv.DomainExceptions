using Serilog;
using Serilog.Configuration;

namespace Sstv.DomainExceptions.Extensions.SerilogEnricher;

/// <summary>
/// Logging extensions for <see cref="LoggerEnrichmentConfiguration"/>.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Registeres domain exception enricher.
    /// </summary>
    /// <param name="enrich">Logger enrichment configurator.</param>
    /// <exception cref="ArgumentNullException">
    /// When <paramref name="enrich"/> was null.
    /// </exception>
    /// <returns>Logger configurator.</returns>
    public static LoggerConfiguration WithDomainException(this LoggerEnrichmentConfiguration enrich)
    {
        if (enrich == null)
        {
            throw new ArgumentNullException(nameof(enrich));
        }

        return enrich.With<DomainExceptionEnricher>();
    }
}