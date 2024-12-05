// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141
#pragma warning disable CA1852

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Sstv.DomainExceptions;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
using Sstv.DomainExceptions.Extensions.ProblemDetails;
using Sstv.Host;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .ConfigureResource(b => b.AddService(serviceName: "MyService"))
    .WithMetrics(o =>
    {
        o.AddDomainExceptionInstrumentation();
        o.AddAspNetCoreInstrumentation();
        o.AddPrometheusExporter();
    });

builder.Services.AddDomainExceptions(b =>
{
    b.WithErrorCodesDescriptionSource(FirstException.ErrorCodesDescriptionSource);
    b.WithErrorCodesDescriptionSource(SecondErrorCodesException.ErrorCodesDescriptionSource);
    b.WithErrorCodesDescriptionFromConfiguration();
    b.UseDomainExceptionHandler();

    b.ConfigureSettings = (sp, settings) =>
    {
        settings.GenerateExceptionIdAutomatically = true;     // default value
        settings.CollectErrorCodesMetricAutomatically = true; // default value
        settings.ThrowIfHasNoErrorCodeDescription = true;     // default value
        settings.ErrorCodesDescriptionSource = null;          // manually set your own error description source instance
        settings.DefaultErrorDescriptionProvider =            // override default error description func
            errorCode => new ErrorDescription(errorCode, "N/A"); // default func

        settings.OnErrorCreated += (error, _) =>
        {
            Console.WriteLine(error.ToString());
        };
    };
});

builder.Services
    .AddProblemDetails(x =>
    {
        x.CustomizeProblemDetails = context =>
        {
            if (context.Exception is DomainException de)
            {
                context.ProblemDetails.Status = context.HttpContext.Response.StatusCode = ErrorCodeMapping.MapToStatusCode(de.GetDescription());
            }
            else
            {
                context.ProblemDetails = new ErrorCodeProblemDetails(ErrorCodes.Default.GetDescription())
                {
                    Status = context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError
                };
            }
        };
    })
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

var app = builder.Build();

app.UseExceptionHandler();

app.MapExampleEndpoints();

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.UseErrorCodesDebugView();

app.Run();


