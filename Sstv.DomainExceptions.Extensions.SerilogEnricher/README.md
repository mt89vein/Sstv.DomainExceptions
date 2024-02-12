Sstv.DomainExceptions.Extensions.SerilogEnricher
=============

[<- root readme](./../README.md)

[<- changelog](./CHANGELOG.md)

This library integrates Sstv.DomainExceptions with Serilog using it's [enrich feature](https://github.com/serilog/serilog/wiki/Enrichment).
When you write to log, serilog can attach some extra data to you logging message. So when exception logged, we can add an error code and user provided additional data from exception instance to the current logging scope.

### Install

You can install using Nuget Package Manager:

```bash
Install-Package Sstv.DomainExceptions.Extensions.SerilogEnricher -Version 2.1.0
```

via the .NET CLI:

```bash
dotnet add package Sstv.DomainExceptions.Extensions.SerilogEnricher --version 2.1.0
```

or you can add package reference manually:

```xml
<PackageReference Include="Sstv.DomainExceptions.Extensions.SerilogEnricher" Version="2.1.0" />
```

### How to use?

When you configuring your Serilog logger, add enricher via method `WithDomainException` e.g.:

```diff
 public static class HostBuilderExtensions
 {
     public static IHostBuilder UseSerilog(this IHostBuilder hostBuilder)
     {
         return hostBuilder.UseSerilog((hostingContext, loggerConfiguration) =>
         {
             var serviceName = hostingContext.Configuration.GetValue<string>("ServiceName");
             var hostName = hostingContext.Configuration.GetValue<string>("HOSTNAME");

             loggerConfiguration
                 .MinimumLevel.Information()
                 .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                 .MinimumLevel.Override("System", LogEventLevel.Warning)
                 .Enrich.WithProperty("Service", serviceName)
                 .Enrich.WithProperty("Host", hostName)
+                .Enrich.WithDomainException()
                 .WriteTo.Console();
         });
     }
 }
```