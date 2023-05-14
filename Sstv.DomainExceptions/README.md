Sstv.DomainExceptions
=============

[<- root readme](./../README.md)

[<- changelog](./CHANGELOG.md)

The main library in this repository.

### Install

You can install using Nuget Package Manager:

```bash
Install-Package Sstv.DomainExceptions -Version 1.0.0
```

via the .NET CLI:

```bash
dotnet add package Sstv.DomainExceptions --version 1.0.0
```

or you can add package reference manually:

```xml
<PackageReference Include="Sstv.DomainExceptions" Version="1.0.0" />
```

### How to use?

First of all you should decide, how you want to work with error codes.

Enum that decorates with ErrorDescriptionAttribute that can hold error code description, help link, error prefix. Before DomainException<TEnum> instantiated, ErrorDescriptionAttribute read from enum by it's value using cached reflection.

```csharp
// this attribute is common for all values
[ErrorDescription(Prefix = "SSTV.", HelpLink = "https://help.myproject.ru/error-codes/{0}")]
public enum DomainErrorCodesEnum
{
    // this attribute override common attribute on enum type
    [ErrorDescription(
        Description = "Unhandled error code",
        HelpLink = "https://help.myproject.ru/error-codes/nothing-here")]
    Default = 0,

    [ErrorDescription(
        Description = "You have not enough money",
        HelpLink = "https://help.myproject.ru/error-codes/not-enough-money")]
    NotEnoughMoney = 10001,

    [ErrorDescription(Prefix = "DIF.", Description = "Another prefix example")]
    SomethingBadHappen = 10002,

    [Obsolete("Don't use this error code because it obsolete :)", true)]
    [ErrorDescription(Prefix = "DIF.", Description = "Obsolete error code in enum")]
    ObsoleteErrorCode = 10003,
}

public sealed class MyGenericException : DomainException<DomainErrorCodesEnum>
{
    public MyGenericException(
      DomainErrorCodesEnum errorCode, 
      Exception? innerException = null
    ) : base(errorCode, innerException)
    {
    }
}

// usage
throw new MyGenericException(DomainErrorCodesEnum.NotEnoughMoney)
    .WithDetailedMessage("DetailedError")
    .WithAdditionalData("123", 2);
```

or class with simple constants in it and with separate additional dictionary (that can be loaded from appsettings.json, in memory dictionary, database etc) with error code description, help link, additional context data.

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
        "HelpLink": "https://help.myproject.ru/error-codes/not-enough-money"
      }
    }
  }
}
```

```csharp
// somewhere in startup.cs
services.AddDomainException();

// or if you don't want to use DI
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
* Library can be extended with static analyzer, or unit test that can validate
enum and it's ErrorDescriptionAttribute for completeness. Snapshot testing can save to git all the error codes, so you and track changes.
* Default code static analyzer checks enum values overlapping
* ErrorCodesDebugView can show you the source of error code (exception and enum assemblies)

Cons:
* All the changes should go through compile and release, even only changed description or help link,
also you can't load this descriptions from external sources or appsettings.
* Reflection (with cache) is used to get ErrorDescriptionAttribute at runtime.
* Can be challenging to change error code length at the future. Recommended length is 5, e.g. SSTV.10000, so you have 9999 error codes per app or bounded context.

### When to choose constants?

Pros:
* We see the full error code right when declaring the constant.
* Zero reflection usage.
* Easy to change description, link, make error code obsolete at runtime without release - just change configuration.
* Partial class with constants can help to reduce the number of Merge Conflicts while adding error codes in parallel.

Cons:
* Cant see all the things about on the same file (except output of ErrorDebugView), cause we need additional store for error description, help link etc.
* You can forgot to add error description. Need additional checks or validators.