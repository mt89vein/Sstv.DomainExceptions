using Microsoft.AspNetCore.Mvc;
using Sstv.DomainExceptions;
using Sstv.DomainExceptions.DebugViewer;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
using Sstv.DomainExceptions.Extensions.ProblemDetails;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Sstv.Host;

/// <summary>
/// Extensions methods for <see cref="IServiceCollection"/>.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// TraceId problem details extensions name.
    /// </summary>
    private const string TRACE_ID_KEY = "traceId";

    /// <summary>
    /// Default error code.
    /// </summary>
    private static readonly ErrorDescription _defaultErrorCodeDescription =
        ErrorCodes.Default.GetDescription();

    /// <summary>
    /// Default problem details response.
    /// </summary>
    private static readonly ErrorCodeProblemDetails _defaultErrorResponse = new(_defaultErrorCodeDescription);

    /// <summary>
    /// Registeres <see cref="FirstException"/> and <see cref="SecondErrorCodesException"/>.
    /// </summary>
    /// <param name="services">Service registrator.</param>
    /// <returns>Service registrator.</returns>
    public static IServiceCollection AddDomainException(this IServiceCollection services)
    {
        return services.AddDomainExceptions(b =>
        {
            b.WithErrorCodesDescriptionSource(FirstException.ErrorCodesDescriptionSource);
            b.WithErrorCodesDescriptionSource(SecondErrorCodesException.ErrorCodesDescriptionSource);
            b.WithErrorCodesDescriptionFromConfiguration();
            b.UseDomainExceptionHandler();

            b.ConfigureSettings = (IServiceProvider sp, DomainExceptionSettings settings) =>
            {
                settings.GenerateExceptionIdAutomatically = true;     // default value
                settings.CollectErrorCodesMetricAutomatically = true; // default value
                settings.ThrowIfHasNoErrorCodeDescription = true;     // default value
                settings.ErrorCodesDescriptionSource = null;          // manually set your own error description source instance
                settings.DefaultErrorDescriptionProvider =            // override default error description func
                    errorCode => new ErrorDescription(errorCode, "N/A"); // default func
                settings.AdditionalDataResponseIncludingFilter = null; // If specified allowes to filter additional data from domain exception

                var exceptionLogger = sp.GetRequiredService<ILogger<DomainException>>();

                settings.OnErrorCreated += (errorDescription, exception) =>
                {
                    var loglevel = errorDescription.Level switch
                    {
                        Level.Undefined => LogLevel.None,
                        Level.NotError => LogLevel.Information,
                        Level.Low => LogLevel.Warning,
                        Level.Medium => LogLevel.Error,
                        Level.High => LogLevel.Error,
                        Level.Critical => LogLevel.Critical,
                        Level.Fatal => LogLevel.Critical,
                        _ => LogLevel.Error
                    };

                    if (exception is DomainException domainException)
                    {
                        exceptionLogger.LogDomainException(loglevel, domainException, errorDescription.ErrorCode, domainException.Message);
                    }
                };
            };
        });
    }

    /// <summary>
    /// Register problem details.
    /// </summary>
    /// <param name="services">Service registrator.</param>
    /// <returns>Service registrator.</returns>
    public static IServiceCollection AddProblemDetail(this IServiceCollection services)
    {
        return services
            .AddSingleton<IDomainExceptionDebugEnricher, StatusCodeEnricher>()
            .AddProblemDetails(o =>
            {
                o.CustomizeProblemDetails = ctx =>
                {
                    if (ctx.Exception is DomainException de)
                    {
                        ctx.ProblemDetails.Status =
                            ctx.HttpContext.Response.StatusCode =
                                ErrorCodeMapping.MapToStatusCode(de.GetDescription());
                    }
                    else
                    {
                        ctx.ProblemDetails = _defaultErrorResponse;
                        ctx.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        ErrorCodesMeter.Measure(_defaultErrorCodeDescription, null);
                    }

                    var addExceptionDetails = !ctx.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>()
                        .IsProduction();

                    if (addExceptionDetails && ctx.Exception is not null)
                    {
                        ctx.ProblemDetails.Extensions["exceptionDetails"] = ctx.Exception.ToString();
                    }

                    if (!ctx.ProblemDetails.Extensions.ContainsKey(TRACE_ID_KEY))
                    {
                        var traceId = Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;

                        if (!string.IsNullOrWhiteSpace(traceId))
                        {
                            ctx.ProblemDetails.Extensions[TRACE_ID_KEY] = traceId;
                        }
                    }
                };
            });
    }

    /// <summary>
    /// Configures API behavior to return ProblemDetails on validation.
    /// </summary>
    /// <param name="builder">MVC builder.</param>
    /// <returns>MVC builder.</returns>
    public static IMvcBuilder AddValidationProblemDetails(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(o =>
        {
            o.InvalidModelStateResponseFactory = static (ActionContext context) =>
            {
                var errorDescription = ErrorCodes.InvalidData.GetDescription();
                ErrorCodesMeter.Measure(errorDescription, null);
                var statusCode = ErrorCodeMapping.MapToStatusCode(errorDescription);

                var problemDetails = new ValidationProblemDetails(context.ModelState)
                {
                    Title = errorDescription.Description,
                    Type = !string.IsNullOrWhiteSpace(errorDescription.HelpLink)
                        ? errorDescription.HelpLink
                        : $"https://httpstatuses.io/{statusCode}",
                    Extensions =
                    {
                        ["code"] = errorDescription.ErrorCode,
                        [TRACE_ID_KEY] = Activity.Current?.Id ?? context.HttpContext.TraceIdentifier
                    }
                };

                return new ObjectResult(problemDetails)
                {
                    StatusCode = statusCode
                };
            };
        });
    }
}