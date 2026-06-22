// CA1852 Type 'Program' can be sealed because it has no subtypes in its containing assembly and is not externally visible
// https://github.com/dotnet/roslyn-analyzers/issues/6141

#pragma warning disable CA1852

using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using Sstv.Domain.Sample;
using Sstv.DomainExceptions.Discovery;
using Sstv.DomainExceptions.Extensions.DependencyInjection;
using Sstv.Host;
using System.Text.Json;
using System.Text.Json.Serialization;

ErrorCodeRegistry.Init(
    [
        Sstv.Host.ErrorCodeMethodCollector.ErrorCodesByMethod,
        Sstv.Domain.Sample.ErrorCodeMethodCollector.ErrorCodesByMethod
    ],
    [
        Sstv.Host.ErrorCodeMethodCollector.MethodCallGraph,
        Sstv.Domain.Sample.ErrorCodeMethodCollector.MethodCallGraph
    ]
);

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
builder.Services.AddSingleton<SampleService>();
builder.Services.AddSingleton<OrderService>();
builder.Services.AddSingleton<IOrderService>(sp => sp.GetRequiredService<OrderService>());

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

app.MapGet("/discovery",
    () => Results.Ok(ErrorCodeRegistry.Instance.AllErrorCodes.ToDictionary(
        x => x.Key,
        x => x.Value.Select(source =>
            new
            {
                source.Code,
                ErrorType = source.ErrorType?.ToString(),
                SourceType = Enum.GetName(source.SourceType)
            }))));

app.Run();
