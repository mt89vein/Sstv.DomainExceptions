using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Sstv.DomainExceptions;

/// <summary>
/// Errors metric collector.
/// </summary>
public static class ErrorCodesMeter
{
    /// <summary>
    /// Meter name.
    /// </summary>
    public const string METER_NAME = "Sstv.DomainExceptions";

    /// <summary>
    /// Meter instance.
    /// </summary>
    private static readonly Meter _meter = new(METER_NAME);

    /// <summary>
    /// Counts exceptions with error code.
    /// </summary>
    private static readonly Counter<long> _counter = _meter.CreateCounter<long>(name: "error.codes", description: "Error codes count");

    /// <summary>
    /// Counts exception.
    /// </summary>
    /// <param name="domainException">An exception.</param>
    public static void Measure(DomainException? domainException)
    {
        if (!_counter.Enabled)
        {
            return;
        }

        if (domainException is null)
        {
            return;
        }

        var tagList = new TagList
        {
            { "code", domainException.ErrorCode },
            { "message", domainException.Message }
        };

        _counter.Add(1, tagList);
    }
}