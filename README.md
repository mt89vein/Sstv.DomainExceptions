Sstv.DomainExceptions
========

### What is DomainExceptions?

DomainExceptions is a simple little library build to provide convenient way to markup code with error codes,
to add an error description, enrich logs with error code, context data, collect error code metrics and so on.

### What is an error code?

An error code is a number (or a combination of letters and numbers) that corresponds to a specific problem in the
operation of the program.  
Example of error code: SSTV10321, where SSTV is the prefix of app
or [bounded context](https://martinfowler.com/bliki/BoundedContext.html).

### Purpose

Applications often encounter exceptions and developers usually spend too little time to do exceptions properly.  
Exceptions can help us to notify about error, stop processing and avoid potentially corrupting data, provide some
context to it for further research and fix.  
If we designate all the code with unique error codes, it will be faster to determine the root of the problem when it
arises.  
When user occurs some error, we can show error code to him, and provide link to the helpful wiki page or user
documentation for this error code, Using so user can fix the problem by himself or share it to the technical support.  
Technical support can look his own wiki, find article by error code, read the solution or recommendations how to solve
the problem or avoid this concrete error, and maybe do automation scripts and help to user. Error codes can save a lot
of time in investigation problems.

### Getting started

This repository contains three NuGet packages:

| Package                                                                                                                  | Version                                                                                                                                                                                                            | Description                                                               |
|--------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------|
| [Sstv.DomainExceptions](./Sstv.DomainExceptions/README.md)                                                               | [![NuGet version](https://img.shields.io/nuget/v/Sstv.DomainExceptions.svg?style=flat-square)](https://www.nuget.org/packages/Sstv.DomainExceptions)                                                               | Core lib that meant to be referenced in your domain layer                 |
| [Sstv.DomainExceptions.Extensions.DependencyInjection](./Sstv.DomainExceptions.Extensions.DependencyInjection/README.md) | [![NuGet version](https://img.shields.io/nuget/v/Sstv.DomainExceptions.Extensions.DependencyInjection.svg?style=flat-square)](https://www.nuget.org/packages/Sstv.DomainExceptions.Extensions.DependencyInjection) | Dependency injection integration lib, for configuring at composition root |
| [Sstv.DomainExceptions.Extensions.ProblemDetails](./Sstv.DomainExceptions.Extensions.ProblemDetails/README.md)           | [![NuGet version](https://img.shields.io/nuget/v/Sstv.DomainExceptions.Extensions.ProblemDetails.svg?style=flat-square)](https://www.nuget.org/packages/Sstv.DomainExceptions.Extensions.ProblemDetails)           | Problem details integration lib                                           |
| [Sstv.DomainExceptions.Extensions.SerilogEnricher](./Sstv.DomainExceptions.Extensions.SerilogEnricher/README.md)         | [![NuGet version](https://img.shields.io/nuget/v/Sstv.DomainExceptions.Extensions.SerilogEnricher.svg?style=flat-square)](https://www.nuget.org/packages/Sstv.DomainExceptions.Extensions.SerilogEnricher)         | Serilog integration lib                                                   |

How to install and use, you can read at theirs readme files.

For usage example, you can look the sample [here](./Sstv.Host).

### Contribute

Feel free for creation issues, or PR :)

### License

Sstv.DomainExceptions is licensed under the [MIT License](./License.md) 
