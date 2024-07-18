Sstv.DomainExceptions
========

### What is DomainExceptions?

DomainExceptions is a simple little library built to provide a convenient way to markup code with error codes,
to add an error description, enrich logs with error code and context data, collect error code metrics, and so on.

### What is an error code?

An error code is a number (or a combination of letters and numbers) that corresponds to a specific problem in the
operation of the program.  
Example of error code: SSTV10321, where SSTV is the prefix of app
or [bounded context](https://martinfowler.com/bliki/BoundedContext.html).

### Purpose

Applications often encounter exceptions, and developers usually spend too little time handling them properly.
Exceptions can help us to notify errors, stop processing, avoid potentially corrupting data, and provide some
context to it for further research and fixing. 
If we designate all the code with unique error codes, it will be faster to determine the root of the problem when it
arises.
When a user occurs an error, we can show the error code to him and provide a link to the helpful wiki page or user
documentation for this error code, Using this, the user can fix the problem by himself or share it with technical support. 
Technical support can look at his own wiki, find articles by error code, read the solution or recommendations on how to solve
the problem or avoid this concrete error, and maybe do automation scripts and help the user. Error codes can save a lot 
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
