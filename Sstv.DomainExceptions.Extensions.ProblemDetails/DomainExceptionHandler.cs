using JetBrains.Annotations;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Sstv.DomainExceptions.Extensions.ProblemDetails;

/// <summary>
/// Domain exception handler.
/// </summary>
[UsedImplicitly]
internal sealed class DomainExceptionHandler : IExceptionHandler
{
    /// <summary>
    /// Problem details response service.
    /// </summary>
    private readonly IProblemDetailsService _problemDetailsService;

    /// <summary>
    /// Creates new instance of <see cref="DomainExceptionHandler"/>
    /// </summary>
    /// <param name="problemDetailsService">Problem details response service.</param>
    public DomainExceptionHandler(IProblemDetailsService problemDetailsService)
    {
        _problemDetailsService = problemDetailsService;
    }

    /// <summary>
    /// Handles domain exception.
    /// </summary>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <param name="exception">The unhandled exception.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true" /> if the exception was handled successfully; otherwise <see langword="false" />.</returns>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DomainException de)
        {
            return false;
        }

        var ctx = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ErrorCodeProblemDetails(de)
            {
                Status = httpContext.Response.StatusCode
            }
        };

        await _problemDetailsService.WriteAsync(ctx);

        return true;
    }
}