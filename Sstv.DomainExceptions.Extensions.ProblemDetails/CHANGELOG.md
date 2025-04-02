Sstv.DomainExceptions.Extensions.ProblemDetails
=============

[<- root readme](./../README.md)

[<- readme](./README.md)

## Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.1.0] - 2025-04-02

### Added

- Added Microsoft.CodeAnalysis.PublicApiAnalyzers to control shipped API
- Allow to filter additional data from domain exception that will be added to ProblemDetails. Specify DomainExceptionSettings.AdditionalDataResponseIncludingFilter.

### Fixed
- DetailedMessage from DomainException not passed to ProblemDetails response


## [3.0.0] - 2024-12-05

### Changes

- Update to .NET 9

BREAKING CHANGES:
- Dropped support of any .NET lower that .NET 9
- No more replace Microsoft.AspNetCore.Http.DefaultProblemDetailsWriter by Rfc7231ProblemDetailsWriter. Bug https://github.com/dotnet/aspnetcore/issues/52577 was fixed 

## [2.2.0] - 2024-02-18

### Added

- Initial version