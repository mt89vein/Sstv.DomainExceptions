Sstv.DomainExceptions
=============

[<- root readme](./../README.md)

[<- readme](./README.md)

## Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

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
