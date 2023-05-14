using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Sstv.DomainExceptions.Extensions.DependencyInjection;

/// <summary>
/// Startup filter, that adds <see cref="DebugViewerMiddleware"/> to http pipeline.
/// </summary>
internal sealed class DebugViewerStartupFilter : IStartupFilter
{
    /// <summary>
    /// Path, where we show debug view.
    /// </summary>
    private readonly PathString _path;

    /// <summary>
    /// On which server port we show debug view.
    /// </summary>
    private readonly int _port;

    /// <summary>
    /// Initiates new instance of <see cref="DebugViewerStartupFilter"/>.
    /// </summary>
    /// <param name="path">Path, where we show debug view.</param>
    /// <param name="port">On which server port we show debug view.</param>
    public DebugViewerStartupFilter(PathString path, int port)
    {
        _path = path;
        _port = port;
    }

    /// <summary>
    /// Extends the provided <paramref name="next"/> and returns an <see cref="Action"/> of the same type.
    /// </summary>
    /// <param name="next">The Configure method to extend.</param>
    /// <returns>A modified <see cref="Action"/>.</returns>
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            builder.MapWhen(x => x.Connection.LocalPort == _port, b =>
            {
                b.UseMiddleware<DebugViewerMiddleware>(_path);

                next(builder);
            });
        };
    }
}