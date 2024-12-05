using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Configures <see cref="DomainExceptionSettings"/>.
/// </summary>
internal sealed class InitHostedService : BackgroundService
{
    /// <summary>
    /// Creates new instance of <see cref="InitHostedService"/>.
    /// </summary>
    /// <param name="sp">Service provider.</param>
    /// <param name="configure">User provided configure action.</param>
    public InitHostedService(IServiceProvider sp, Action<IServiceProvider, DomainExceptionSettings>? configure)
    {
        configure?.Invoke(sp, DomainExceptionSettings.Instance);

        if (DomainExceptionSettings.Instance.CollectErrorCodesMetricAutomatically)
        {
            DomainExceptionSettings.Instance.OnErrorCreated += ErrorCodesMeter.Measure;
        }

        DomainExceptionSettings.ErrorDescriptionSourceGetter =
            new Lazy<IErrorCodesDescriptionSource?>(() =>
            {
                var dict = new Dictionary<string, ErrorDescription>();

                foreach (var s in sp.GetServices<IErrorCodesDescriptionSource>())
                {
                    foreach (var errorDescription in s.Enumerate())
                    {
                        dict.TryAdd(errorDescription.ErrorCode, errorDescription);
                    }
                }

                return new ErrorCodesDescriptionInMemorySource(dict);
            });
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}