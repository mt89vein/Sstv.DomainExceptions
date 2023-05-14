using Serilog.Core;
using Serilog.Events;
using System.Collections;

namespace Sstv.DomainExceptions.Extensions.SerilogEnricher;

/// <summary>
/// Logs enricher with <see cref="DomainException"/> error code.
/// </summary>
internal sealed class DomainExceptionEnricher : ILogEventEnricher
{
    /// <summary>Enrich the log event.</summary>
    /// <param name="logEvent">The log event to enrich.</param>
    /// <param name="propertyFactory">Factory for creating new properties to add to the event.</param>
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Exception is not DomainException e)
        {
            return;
        }

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("ErrorCode", e.ErrorCode));

        foreach (DictionaryEntry entry in e.Data)
        {
            var key = entry.Key.ToString();

            if (!string.IsNullOrWhiteSpace(key))
            {
                logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(key, entry.Value));
            }
        }
    }
}