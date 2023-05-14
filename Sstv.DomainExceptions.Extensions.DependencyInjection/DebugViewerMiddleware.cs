using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sstv.DomainExceptions.DebugViewer;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Debug viewer middleware.
/// </summary>
[SuppressMessage("Design", "CA1812: Avoid uninstantiated internal classes")]
internal sealed class DebugViewerMiddleware
{
    /// <summary>
    /// Next middleware.
    /// </summary>
    private readonly RequestDelegate _next;

    /// <summary>
    /// Path, where we show debug view.
    /// </summary>
    private readonly PathString _path;

    /// <summary>
    /// Initiates new instance of <see cref="DebugViewerMiddleware"/>.
    /// </summary>
    /// <param name="next">Next middleware.</param>
    /// <param name="path">Path, where we show debug view.</param>
    public DebugViewerMiddleware(RequestDelegate next, PathString path)
    {
        _next = next;
        _path = path;
    }

    /// <summary>
    /// Invokes middleware.
    /// </summary>
    /// <param name="httpContext">Current HTTP request.</param>
    public Task InvokeAsync(HttpContext httpContext)
    {
        if (httpContext.Request.Path == _path)
        {
            var debugViewer = httpContext.RequestServices.GetRequiredService<DomainExceptionDebugViewer>();
            return httpContext.Response.WriteAsJsonAsync(debugViewer.DebugView(), httpContext.RequestAborted);
        }

        return _next(httpContext);
    }
}