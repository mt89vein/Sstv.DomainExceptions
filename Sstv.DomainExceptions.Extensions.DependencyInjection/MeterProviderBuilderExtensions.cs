using OpenTelemetry.Metrics;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Extends <see cref="MeterProviderBuilder"/>.
/// </summary>
public static class MeterProviderBuilderExtensions
{
    /// <summary>
    /// Adds domain exception metric collection.
    /// </summary>
    /// <param name="builder">Meter provider builder.</param>
    /// <returns>Meter provider builder.</returns>
    public static MeterProviderBuilder AddDomainExceptionInstrumentation(this MeterProviderBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.AddMeter(ErrorCodesMeter.METER_NAME);
    }
}