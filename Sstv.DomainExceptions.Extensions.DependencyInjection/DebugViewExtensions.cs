using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for error codes debug view.
/// </summary>
public static class DebugViewExtensions
{
    /// <summary>
    /// Default path for middleware.
    /// </summary>
    private const string DEFAULT_PATH = "/error-codes";

    /// <summary>
    /// Adds a middleware for debug view.
    /// </summary>
    /// <param name="builder">Domain exception builder.</param>
    /// <param name="path">Path. If not provided, would be used <see cref="DEFAULT_PATH"/>.</param>
    /// <param name="port">On which server port we show debug view.</param>
    /// <returns>Domain exception builder.</returns>
    public static DomainExceptionBuilder UseErrorCodesDebugView(
        this DomainExceptionBuilder builder,
        PathString? path = null,
        int port = 8082
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        path ??= new PathString(DEFAULT_PATH);

        builder.Services.AddTransient<IStartupFilter>(sp => new DebugViewerStartupFilter(path.Value, port));

        return builder;
    }

    /// <summary>
    /// Adds a middleware for debug view.
    /// </summary>
    /// <param name="applicationBuilder">Application http pipeline builder.</param>
    /// <param name="path">Path. If not provided, would be used <see cref="DEFAULT_PATH"/>.</param>
    /// <returns>Application http pipeline builder.</returns>
    public static IApplicationBuilder UseErrorCodesDebugView(this IApplicationBuilder applicationBuilder, PathString? path = null)
    {
        ArgumentNullException.ThrowIfNull(applicationBuilder);

        path ??= new PathString(DEFAULT_PATH);

        applicationBuilder.UseMiddleware<DebugViewerMiddleware>(path.Value);

        return applicationBuilder;
    }
}