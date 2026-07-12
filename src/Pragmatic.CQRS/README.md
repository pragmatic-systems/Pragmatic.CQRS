## Pragmatic.CQRS
A super lightweight CQRS implementation that replicates the MediatR interfaces.

## Features
* Request/Response Handlers
* Request/Void Handlers
* Pipeline Support

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