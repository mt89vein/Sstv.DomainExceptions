using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Sstv.DomainExceptions.Extensions.DependencyInjection;

namespace Sstv.DomainExceptions.Extensions.ProblemDetails;

/// <summary>
/// Provides extension methods for <see cref="DomainExceptionBuilder"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IExceptionHandler"/> for domain exception.
    /// </summary>
    /// <param name="builder">Builder of domain exception.</param>
    /// <returns>Builder of domain exception.</returns>
    public static DomainExceptionBuilder UseDomainExceptionHandler(this DomainExceptionBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddExceptionHandler<DomainExceptionHandler>();

        return builder;
    }
}