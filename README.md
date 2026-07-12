# Pragmatic.CQRS
A Simple CQRS implementation that mimics the MediatR interfaces.

## Status

[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=bugs)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=coverage)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)

[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragmatic.CQRS&metric=sqale_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragmatic.CQRS)

## Download
Available on NuGet - https://www.nuget.org/packages/Pragmatic.CQRS/

## Features
* Request/Response Handlers
* Request/Void Handlers
* Pipeline Support

## Pending
* May look at adding notification/broadcast fan out.

## Usage

Auto registers IMediator instance and all handlers in associated assemblies as Transient.

```
  services.AddCqrs(cfg =>
  {
      cfg.RegisterServicesFromAssemblies(
          new[] { typeof(Program).Assembly });
  });
```

`IPipelineBehaviour` to be registered seperately, applies in reverse order - LIFO pattern.

## Building Locally
You can use the cake file to build, test and publish:

Run: `dotnet cake --Target=LocalNugetPackAndPush --NuGetSource="{source}" --NuGetApiKey="{key}"`

To write to a local folder:

Run: `dotnet cake --Target=LocalNugetPackAndPush --NuGetSource="c:\package-source" --NuGetApiKey="key"`

Note - We are using LocalNugetPackAndPush as the full NugetPackAndPush runs SonarScan and requires additional variables.

## Operations

Build and test: `dotnet cake --Target=BuildAndTest`

Build and benchmark: `dotnet cake --Target=BuildAndBenchmark`

Build and sonar: `dotnet cake --Target=BuildAndSonarScan`