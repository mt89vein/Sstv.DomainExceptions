Sstv.DomainExceptions.Extensions.DependencyInjection
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

### Changed

- Update to .NET 9
- error_codes_total metric now have level label from error code

BREAKING CHANGES:
- OpenTelemetry.Api dependency upgraded to 1.10.0
- Dropped support of any .NET lower that .NET 9
- ErrorCodesMeter now accept ErrorDescription and instance of error, instead of DomainException. This helps to use not only exceptions, but also Result pattern.

## [2.0.0] - 2024-02-11

- Source gen release

## [1.0.0] - 2023-10-04

### Added

- DebugViewerMiddleware
- ErrorCodesDescriptionInMemorySource
- ErrorCodesDescriptionFromConfigurationSource