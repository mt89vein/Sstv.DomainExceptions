using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        builder.Services.TryAddProblemDetailsRfc7231Compliance();

        return builder;
    }

    // TODO: Remove in .NET 9.0 or .NET 8.0 patch
    // BUG: https://github.com/dotnet/aspnetcore/issues/52577
    private static void TryAddProblemDetailsRfc7231Compliance(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(IsDefaultProblemDetailsWriter);

        if (descriptor == null)
        {
            return;
        }

        var decoratedType = descriptor.ImplementationType!;
        var lifetime = descriptor.Lifetime;

        services.Add(ServiceDescriptor.Describe(decoratedType, decoratedType, lifetime));
        services.Replace(ServiceDescriptor.Describe(typeof(IProblemDetailsWriter),
            sp => NewProblemDetailsWriter(sp, decoratedType),
            lifetime));

        static bool IsDefaultProblemDetailsWriter(ServiceDescriptor serviceDescriptor)
        {
            return serviceDescriptor.ServiceType == typeof(IProblemDetailsWriter) &&
            serviceDescriptor.ImplementationType?.FullName == "Microsoft.AspNetCore.Http.DefaultProblemDetailsWriter";
        }

        static Rfc7231ProblemDetailsWriter
            NewProblemDetailsWriter(IServiceProvider serviceProvider, Type decoratedType)
        {
            return new((IProblemDetailsWriter)serviceProvider.GetRequiredService(decoratedType));
        }
    }
}