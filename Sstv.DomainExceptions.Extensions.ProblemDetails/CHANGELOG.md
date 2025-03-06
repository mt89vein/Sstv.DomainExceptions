Sstv.DomainExceptions.Extensions.ProblemDetails
=============

[<- root readme](./../README.md)

[<- readme](./README.md)

## Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [3.1.0] - 2025-03-06

### Added

- Added Microsoft.CodeAnalysis.PublicApiAnalyzers to control shipped API


## [3.0.0] - 2024-12-05

### Changes

- Update to .NET 9

BREAKING CHANGES:
- Dropped support of any .NET lower that .NET 9
- No more replace Microsoft.AspNetCore.Http.DefaultProblemDetailsWriter by Rfc7231ProblemDetailsWriter. Bug https://github.com/dotnet/aspnetcore/issues/52577 was fixed 

## [2.2.0] - 2024-02-18

### Added

- Initial version