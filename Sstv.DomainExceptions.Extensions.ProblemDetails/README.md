Sstv.DomainExceptions.Extensions.ProblemDetails
=============

[<- root readme](./../README.md)

[<- changelog](./CHANGELOG.md)

This library brings to Sstv.DomainExceptions additional capabilities to create response in [ProblemDetails format](https://datatracker.ietf.org/doc/rfc9457/)

## Install

You can install using Nuget Package Manager:

```bash
Install-Package Sstv.DomainExceptions.Extensions.ProblemDetails -Version 2.3.0
```

via the .NET CLI:

```bash
dotnet add package Sstv.DomainExceptions.Extensions.ProblemDetails --version 2.3.0
```

or you can add package reference manually:

```xml
<PackageReference Include="Sstv.DomainExceptions.Extensions.ProblemDetails" Version="2.3.0" />
```

## How to use?

### Register to Dependency injection:
Call `UseDomainExceptionHandler` extension method on DomainExceptionBuilder:

```csharp
services.AddDomainExceptions(builder =>
{
    builder.UseDomainExceptionHandler();

    // other config
});
```

### Add AspNetCore built-in ProblemDetails

```csharp
services.AddProblemDetails(x =>
{
    x.CustomizeProblemDetails = context =>
    {
        // when domain exception occurs, we can grab error code and map HTTP status code for him
        if (context.Exception is DomainException de)
        {
            context.ProblemDetails.Status = context.HttpContext.Response.StatusCode = ErrorCodeMapping.MapToStatusCode(de.ErrorCode);
        }
        else
        {
            // for other exceptions we can add default error code for consistent behavior
            context.ProblemDetails = new ErrorCodeProblemDetails(ErrorCodes.Default.GetDescription())
            {
                Status = context.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    };
})
```

### Add exception handler middlware

```csharp
app.UseExceptionHandler();
```

after that, we can throw exception and see results

```csharp
throw ErrorCodes.NotEnoughMoney.ToException()
                .WithDetailedMessage("You want 500, but your account balance is 300.");
```

```json
{
    "type": "https://help.myproject.ru/error-codes/not-enough-money",
    "title": "You have not enough money",
    "status": 200,
    "code": "SSTV10001",
    "criticalityLevel": "Low",
    "errorId": "ad3f064c-1254-41dd-82e4-891507937cf6"
}
```