// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141
#pragma warning disable CA1852

using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Hellang.Middleware.ProblemDetails;
using Sstv.DomainExceptions;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
using Sstv.Host;
using System.Collections;
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
    b.ConfigureSettings = (sp, settings) =>
    {
        settings.GenerateExceptionIdAutomatically = true;     // default value
        settings.CollectErrorCodesMetricAutomatically = true; // default value
        settings.ThrowIfHasNoErrorCodeDescription = true;     // default value
        settings.ErrorCodesDescriptionSource = null;          // manually set your own error description source
        settings.DefaultErrorDescriptionProvider =            // override default error description func
            errorCode => new ErrorDescription(errorCode, "N/A"); // default func
    };
});

builder.Services.AddDomainExceptions(b =>
{
    b.WithErrorCodesDescriptionFromConfiguration();
});

// Configure problem details
builder.Services.AddProblemDetails(options =>
{
    options.Map<DomainException>(ex =>
    {
        var pb = new ProblemDetails
        {
            Title = ex.Message,
            Status = StatusCodes.Status500InternalServerError,
            Detail = ex.DetailedMessage,
            Type = ex.HelpLink,
            Extensions =
            {
                ["code"] = ex.ErrorCode
            }
        };

        foreach (DictionaryEntry e in ex.Data)
        {
            pb.Extensions.TryAdd(e.Key.ToString()!, e.Value);
        }

        return pb;
    });

    // Because exceptions are handled polymorphically, this will act as a "catch all" mapping, which is why it's added last.
    // If an exception other than NotImplementedException and HttpRequestException is thrown, this will handle it.
    options.MapToStatusCode<Exception>(StatusCodes.Status500InternalServerError);
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
    });

var app = builder.Build();

// регистрируем middleware
app.UseProblemDetails();

app.MapExampleEndpoints();

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.UseErrorCodesDebugView();

app.Run();


