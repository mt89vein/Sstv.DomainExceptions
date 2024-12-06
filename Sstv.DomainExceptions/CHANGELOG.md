Sstv.DomainExceptions
=============

[<- root readme](./../README.md)

[<- readme](./README.md)

## Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.3.0] - 2024-12-06

- CriticalityLevel added to [ErrorDescription], also added to metric label, logs, api response. So you can use it to differ codes by its error criticality level.

BREAKING CHANGES: 
- Dropped field "IsObsolete" on [ErrorDescription]
- DomainExceptionSettings.OnExceptionCreated replaced by DomainExceptionSettings.OnErrorCreated callback

## [2.1.0] - 2024-02-12

- Adds [ExceptionConfig] attribute, for configuring the name of generated exception.
- Exception now generated as partial class
- AsException extensions method renamed to ToException

## [2.0.0] - 2024-02-11

- Source gen release
- Remove all reflection usage
- Downgrade to netstandard2.0 for source generators


## [1.0.0] - 2023-10-04

### Added

- DomainException and it's generic version
- DomainExceptionSettings for configuring
- ErrorDescription and ErrorDescriptionAttribute
- ErrorCodesMeter, now we can collect metrics
- IErrorCodesDescriptionSource, so we can provide external configuration
