Sstv.DomainExceptions
=============

[<- root readme](./../README.md)

[<- changelog](./CHANGELOG.md)

The main library in this repository.

### Install

You can install using Nuget Package Manager:

```bash
Install-Package Sstv.DomainExceptions -Version 3.1.0
```

via the .NET CLI:

```bash
dotnet add package Sstv.DomainExceptions --version 3.1.0
```

or you can add package reference manually:

```xml
<PackageReference Include="Sstv.DomainExceptions" Version="3.1.0" />
```

### How to use?

First of all you should decide, how you want to work with error codes.

Enum that decorates with ErrorDescriptionAttribute that can hold error code description, help link, error prefix.

```csharp
// this attribute is common for all values
[ErrorDescription(Prefix = "SSTV", HelpLink = "https://help.myproject.ru/error-codes/{0}", Level = Level.Critical)]
[ExceptionConfig(ClassName = "FirstException")]
public enum ErrorCodes
{
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.myproject.ru/error-codes/nothing-here",
        Level = Level.Fatal)]
    Default = 0,

    [ErrorDescription(
        Description = "You have not enough money",
        HelpLink = "https://help.myproject.ru/error-codes/not-enough-money",
        Level = Level.NotError)]
    NotEnoughMoney = 10001,

    [ErrorDescription(Prefix = "DIF", Description = "Another prefix example")]
    SomethingBadHappen = 10002,

    [ErrorDescription(
        Description = "Help link with template in enum member attribute",
        HelpLink = "https://help.myproject.ru/{0}/error-code"
    )]
    WhateverElse = 10003,
}
```

Thats all that you need to do to start work with.
After compile, source generator creates exception class and class with extensions methods for you:

```csharp

    public sealed partial class FirstException : DomainException
    {
        public static readonly IReadOnlyDictionary<ErrorCodes, ErrorDescription> ErrorDescriptions = new Dictionary<ErrorCodes, ErrorDescription>
        {
            [ErrorCodes.Default] = new ErrorDescription("SSTV00000", "Unhandled error code", Level.Fatal, "https://help.myproject.ru/error-codes/nothing-here"),
            [ErrorCodes.NotEnoughMoney] = new ErrorDescription("SSTV10001", "You have not enough money", Level.NotError, "https://help.myproject.ru/error-codes/not-enough-money"),
            [ErrorCodes.SomethingBadHappen] = new ErrorDescription("DIF10002", "Another prefix example", Level.Critical, "https://help.myproject.ru/error-codes/DIF10002"),
            [ErrorCodes.WhateverElse] = new ErrorDescription("SSTV10003", "Help link with template in enum member attribute", Level.Critical, "https://help.myproject.ru/SSTV10003/error-code"),
        }.ToFrozenDictionary();

        public static IErrorCodesDescriptionSource ErrorCodesDescriptionSource { get; } = new ErrorCodesDescriptionInMemorySource(ErrorDescriptions.Values.ToFrozenDictionary(x => x.ErrorCode, x => x));

        public FirstException(ErrorCodes errorCodes, Exception? innerException = null)
            : base(ErrorDescriptions[errorCodes], innerException)
        {
        }
    }

  public static class ErrorCodesExtensions
  {
      public static ErrorDescription GetDescription(this ErrorCodes errorCodes)
      {
          return FirstException.ErrorDescriptions[errorCodes];
      }

      public static string GetErrorCode(this ErrorCodes errorCodes)
      {
          return FirstException.ErrorDescriptions[errorCodes].ErrorCode;
      }

      public static FirstException ToException(this ErrorCodes errorCodes, Exception? innerException = null)
      {
          return new FirstException(errorCodes, innerException);
      }
  }

```

Here usage example:

```csharp

throw new FirstException(ErrorCodes.NotEnoughMoney)
    .WithDetailedMessage("DetailedError")
    .WithAdditionalData("123", 2);

// or more fluent api way:

throw ErrorCodes.NotEnoughMoney
    .ToException()
    .WithDetailedMessage("DetailedError")
    .WithAdditionalData("123", 2);
```


If you don't like enums, you can also use simple class with constants in it and with separate additional dictionary (that can be loaded from appsettings.json, in memory dictionary, database etc) with error code description, help link, additional context data.

```csharp
public static class DomainErrorCodes
{
    public const string DEFAULT = "SSTV.10000";
    public const string NOT_ENOUGH_MONEY = "SSTV.10004";
    public const string SOMETHING_BAD_HAPPEN = "SSTV.10005";
}

public sealed class MyException : DomainException
{
    public MyException(string errorCode, Exception? innerException = null)
        : base(errorCode, innerException)
    {
    }
}
```

```json
{
  "DomainExceptionSettings": {
    "ErrorCodes": {
      "SSTV.10004": {
        "Description": "You have not enough money",
        "Level": "Low",
        "HelpLink": "https://help.myproject.ru/error-codes/not-enough-money"
      }
    }
  }
}
```

```csharp
// somewhere in startup.cs
services.AddDomainException();

// or if you don't want to or can't use DI container
DomainExceptionSettings.Instance.ErrorCodesDescriptionSource = new ErrorCodesDescriptionFromConfigurationSource(configuration);

// and the usage
throw new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)
  .WithDetailedMessage("DetailedError")
  .WithAdditionalData("UserId", 2);
```

### When to choose enums?
* If you want to keep error codes, description, help link on the same file
* Just want to use an enum :)

Pros:
* All the things on the same file
* Library can be extended with static analyzer, or unit test that can validate enum and it's ErrorDescriptionAttribute for completeness. Snapshot testing can save to git all the error codes, so you and track changes.
* Default code static analyzer checks enum values overlapping
* Zero reflection usage (thanks for source generators)
* Source generators generates code and precompute all values that it needed, so and you can check them. Also it very fast because it ready to run.

Cons:
* All the changes should go through compile and release, even only description or help link was changed. Also you can't load this descriptions from external sources or appsettings.
* Can be challenging to change error code length at the future. Recommended length is 5, e.g. SSTV10000, so you have 9999 error codes per app or bounded context.

### When to choose constants?

Pros:
* We see the full error code right when declaring the constant.
* Zero reflection usage.
* Easy to change description, link, make error code obsolete at runtime without release - just change configuration.
* Partial class with constants can help to reduce the number of Merge Conflicts while adding error codes in parallel.

Cons:
* Cant see all the things about on the same file (except output of ErrorDebugView), cause we need additional store for error description, help link etc.
* You can forgot to add error description. Need additional checks or validators.

### Error code discovery (source generator)

Starting from version 4.0.0, the library includes a source generator (`ErrorCodeMethodCollector`) that automatically
discovers all error codes used across your application's call stack. At build time, it produces a
`FrozenDictionary<string, ErrorCodeSource[]>` mapping each method/endpoint to the error codes it can produce.

The generator is useful for enriching Swagger/OpenAPI docs with error codes, creating monitoring dashboards, or
validating that all code paths produce known error codes.

#### Enabling

Add the assembly-level attribute to activate the generator:

```csharp
[assembly: CollectErrorCodes]
```

Without this attribute, the generator produces no output.

#### Configuration

The `[CollectErrorCodes]` attribute supports optional named arguments:

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `MaxPropagationDepth` | `int` | `10` | Maximum call chain depth for error code propagation through method calls. |
| `ClassName` | `string?` | `"ErrorCodeMethodCollector"` | Name of the generated partial class. Useful for avoiding conflicts. |

Example with custom settings:

```csharp
[assembly: CollectErrorCodes(MaxPropagationDepth = 5, ClassName = "AppErrorCodes")]
```

This generates `AppErrorCodes.ErrorCodesByMethod` dictionary and limits call chain propagation to 5 levels deep.

#### Generated output

```csharp
using ErrorCodes = global::Sstv.Host.ErrorCodes;
using DomainErrorCodes = global::Sstv.Host.DomainErrorCodes;

namespace Sstv.Host
{
    public static partial class ErrorCodeMethodCollector
    {
        public static readonly FrozenDictionary<string, ErrorCodeSource[]> ErrorCodesByMethod =
            new Dictionary<string, ErrorCodeSource[]>
            {
                ["Sstv.Host.Controllers.OrderController.CreateOrder"] = [
                    new ErrorCodeSource(ErrorCodes.InvalidData.GetErrorCode(), ErrorCodeSourceType.Enum, typeof(ErrorCodes)),
                    new ErrorCodeSource("SSTV.10005", ErrorCodeSourceType.Constant, typeof(DomainErrorCodes)),
                ],
                ["minimal-api-example-1"] = [
                    new ErrorCodeSource("SSTV.10004", ErrorCodeSourceType.Constant, typeof(DomainErrorCodes)),
                ],
            }.ToFrozenDictionary();
    }
}
```

The namespace is derived from the assembly name. The dictionary is keyed by `FullTypeName.MethodName` for
controllers/services, or by the `WithName()` value (falling back to route pattern) for minimal API endpoints.
Each entry lists all error codes that can originate from that method, including those from methods it calls.

#### Supported detection patterns

The generator detects error codes from these sources:

| Pattern | Example | Source type |
|---------|---------|-------------|
| `DomainException` constructor with string literal | `throw new MyException("SSTV.10004")` | `Constant` |
| `DomainException` constructor with `const` field | `throw new MyException(DomainErrorCodes.NOT_ENOUGH_MONEY)` | `Constant` |
| `DomainException` constructor with enum member | `throw new MyException(ErrorCodes.InvalidData)` | `Enum` |
| `.ToException()` on enum value | `throw ErrorCodes.SomethingBadHappen.ToException()` | `Enum` |
| Fluent chain with `WithXxx()` | `throw ErrorCodes.Default.ToException().WithDetailedMessage("msg")` | `Enum` |
| Variable-declared exception then thrown | `var x = new MyException("CODE"); throw x;` | `Constant` |
| Return with error code object (Result pattern) | `return Result.Fail(new ErrorCodeResult(ErrorCodes.InvalidData))` | `Enum` |
| Named constructor argument (literal) | `throw new MyException(errorCode: "SSTV.10004")` | `Constant` |
| Named constructor argument (const/enum) | `throw new MyException(errorCode: ErrorCodes.InvalidData)` | `Enum` / `Constant` |

Error codes from string literals and `const` fields are stored as-is. Enum-based codes are stored as the member
name (e.g. `"InvalidData"`, `"SomethingBadHappen"`), and the generated dictionary references the extension class
to resolve them to their full string value at runtime via `GetErrorCode()`.

Enum members and const fields are accepted regardless of casing (e.g. `ErrorCodes.myLowercaseMember`).
Non-resolved identifiers (not backed by a symbol) still require an uppercase first letter to reduce noise.

#### Supported call-chain propagation

The generator traces error codes across method boundaries:

- **Direct calls**: error codes from `ValidateOrder()` propagate to `ProcessOrder()` that calls it.
- **Cross-class calls**: calls to methods on other service classes are tracked by declaring type.
- **Interface calls**: calls through `IOrderService` are resolved to concrete implementations in the same
  assembly (`OrderService`, `OrderAlternativeService`). Codes from all implementations are merged.
- **Method overloads**: multiple overloads with the same name are merged under one key. Error codes and
  call chains from all overloads are combined.
- **Generic methods**: generic methods and their overloads with different arity are supported. The key is
  the simple method name (without backtick suffix); overloads are merged.
- **Transitive closure**: up to 10 iterations of propagation across the call graph.

#### Supported endpoint types

| Endpoint type | Detection | Naming |
|---------------|-----------|--------|
| Controller action methods | `MethodDeclarationSyntax` | `FullTypeName.MethodName` |
| Minimal API `MapGet/Post/Put/Delete/Patch` | `InvocationExpressionSyntax` | `.WithName(...)` value or route pattern |
| Minimal API `MapGroup` / `MapMethods` | Same as above | Same as above |

#### What is NOT supported (limitations)

| Limitation | Details |
|------------|---------|
| **Runtime-computed codes** | Error codes from variables, string interpolation (`$"PREFIX_{id}"`), or method calls (`GetErrorCode()`) — only compile-time constants, literals, and enum members work |
| **Catch-and-re-throw** | `catch (Exception ex) { throw ex; }` — the variable initializer is not in the same method |
| **Object initializers** | `new MyException { ErrorCode = "X" }` |
| **`with` expressions** on records | Not handled |
| **Bare `throw;`** | Silent no-op |
| **Ternary/conditional inside throw** | Only the outermost expression is analyzed |
| **Call chain depth > 10** | Propagation loop hardcoded to 10 iterations |
| **Delegates / lambdas / `Func<>`/`Action<>`** | Inlined lambdas in Map methods are analyzed, but passing a method as a delegate is not traced |
| **Virtual dispatch / polymorphism** | Tracked by declaring type, not runtime type |
| **Interface implementations in external assemblies** | Cache only scans source types (same assembly) |
| **Reflection / `dynamic` calls** | Not resolvable via Roslyn semantic model |
| **Extension method calls on interfaces** | Resolved to the static extension class, not the interface |
| **Structs / records / abstract classes** | Excluded from interface implementation cache |
| **`Results.BadRequest()`, `Results.Ok()` etc.** | Not analyzed — explicit `IResult` returns must be handled manually |
