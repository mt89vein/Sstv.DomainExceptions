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

        DomainExceptionSettings.ErrorDescriptionSourceGetter =
            new Lazy<IErrorCodesDescriptionSource?>(() => sp.GetService<IErrorCodesDescriptionSource>());
    }
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}