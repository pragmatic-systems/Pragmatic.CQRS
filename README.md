# Pragsys.CQRS
A Simple CQRS implementation that mimics the MediatR interfaces.

## Status
[![Bugs](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=bugs)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)
[![Code Smells](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=code_smells)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)
[![Reliability Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=reliability_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=security_rating)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=vulnerabilities)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=pragmatic-systems_Pragsys.CQRS&metric=coverage)](https://sonarcloud.io/summary/new_code?id=pragmatic-systems_Pragsys.CQRS)

## Features
* Request/Response Handlers
* Request/Void Handlers
* Pipeline Support

## Pending
* May look at adding notification/broadcast fan out.

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