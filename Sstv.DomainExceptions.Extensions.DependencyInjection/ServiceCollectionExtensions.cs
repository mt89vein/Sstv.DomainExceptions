using Microsoft.Extensions.DependencyInjection;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds domain exception services to dependency injection container.
    /// </summary>
    /// <param name="services">Services registrator.</param>
    /// <param name="configure">Configure function.</param>
    /// <returns>Services registrator.</returns>
    public static IServiceCollection AddDomainExceptions(
        this IServiceCollection services,
        Action<DomainExceptionBuilder>? configure = null
    )
    {
        var builder = new DomainExceptionBuilder(services);

        configure?.Invoke(builder);

        builder.Build();

        return services;
    }
}