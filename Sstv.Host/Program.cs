// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141
#pragma warning disable CA1852

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
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

builder.Services.AddDomainException();
builder.Services.AddProblemDetail();

builder.Services
    .AddControllers()
    .AddValidationProblemDetails()
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
