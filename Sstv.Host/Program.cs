// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141
#pragma warning disable CA1852

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
using Sstv.Host;
using Sstv.Host.Controllers;
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

var x = ErrorCodeMethodCollector.ErrorCodesByMethod[
    typeof(OrderController).FullName + "." + nameof(OrderController.CreateOrder)];

var deepNested = ErrorCodeMethodCollector.ErrorCodesByMethod[
    "Sstv.Host.Nested.Level1.Level2.DeepNestedService.ProcessDeep"];

Console.WriteLine($"Deep nested method has {deepNested.Count} error codes");

builder.Services.AddDomainException();
builder.Services.AddProblemDetail();
builder.Services.AddSingleton<OrderService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.OperationFilter<SwaggerErrorCodesFilter>();
});

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

app.UseSwagger();
app.UseSwaggerUI();

app.MapExampleEndpoints();

app.MapControllers();

app.MapPrometheusScrapingEndpoint();

app.UseErrorCodesDebugView();

app.Run();
