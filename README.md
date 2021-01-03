# ServiceModel.Grpc

`ServiceModel.Grpc` enables applications to communicate with gRPC services using code-first approach (no .proto files), helps to get around some limitations of gRPC protocol like "only reference types", "exact one input", "no nulls". Provides exception handling. Helps to migrate existing WCF solution to gRPC with minimum effort.

The library supports lightweight runtime proxy generation via Reflection.Emit and C# source code generation.

The solution is built on top of [gRPC C#](https://github.com/grpc/grpc/tree/master/src/csharp).

## Links

- [docs](https://max-ieremenko.github.io/ServiceModel.Grpc/)
  - [service and operation names](https://max-ieremenko.github.io/ServiceModel.Grpc/ServiceAndOperationName.html)
  - [service and operation bindings](https://max-ieremenko.github.io/ServiceModel.Grpc/ServiceAndOperationBinding.html)
  - [client configuration](https://max-ieremenko.github.io/ServiceModel.Grpc/ClientConfiguration.html)
  - [client code generation](https://max-ieremenko.github.io/ServiceModel.Grpc/client-code-generation.html)
  - [server code generation](https://max-ieremenko.github.io/ServiceModel.Grpc/server-code-generation.html)
  - operations
    - [unary](https://max-ieremenko.github.io/ServiceModel.Grpc/operation-unary.html)
    - [client streaming](https://max-ieremenko.github.io/ServiceModel.Grpc/operation-client-streaming.html)
    - [server streaming](https://max-ieremenko.github.io/ServiceModel.Grpc/operation-server-streaming.html)
    - [duplex streaming](https://max-ieremenko.github.io/ServiceModel.Grpc/operation-duplex-streaming.html)
  - [ASP.NET Core server configuration](https://max-ieremenko.github.io/ServiceModel.Grpc/ASPNETCoreServerConfiguration.html)
  - [Grpc.Core server configuration](https://max-ieremenko.github.io/ServiceModel.Grpc/GrpcCoreServerConfiguration.html)
  - error handler general [information](https://max-ieremenko.github.io/ServiceModel.Grpc/error-handling-general.html)
  - global error handling [tutorial](https://max-ieremenko.github.io/ServiceModel.Grpc/global-error-handling.html)
  - getting started [tutorial](https://max-ieremenko.github.io/ServiceModel.Grpc/CreateClientAndServerASPNETCore.html) create a gRPC client and server
  - [compatibility with native gRPC](https://max-ieremenko.github.io/ServiceModel.Grpc/CompatibilityWithNativegRPC.html)
  - migrate from WCF to a gRPC [tutorial](https://max-ieremenko.github.io/ServiceModel.Grpc/MigrateWCFServiceTogRPC.html)
  - migrate from WCF FaultContract to a gRPC global error handling [tutorial](https://max-ieremenko.github.io/ServiceModel.Grpc/migrate-wcf-faultcontract-to-global-error-handling.html)
  - [example](Examples/ProtobufMarshaller) protobuf marshaller
- [examples](Examples)

## ServiceModel.Grpc at a glance

### Declare a service contract

``` c#
[ServiceContract]
public interface ICalculator
{
    [OperationContract]
    Task<long> Sum(long x, int y, int z, CancellationToken token = default);

    [OperationContract]
    ValueTask<(int Multiplier, IAsyncEnumerable<int> Values)> MultiplyBy(IAsyncEnumerable<int> values, int multiplier, CancellationToken token = default);
}
```

### Client call (Reflection.Emit)

A proxy for ICalculator service will be generated on demand via `Reflection.Emit`.

```powershell
PS> Install-Package ServiceModel.Grpc
```

``` c#
// create a channel
var channel = new Channel("localhost", 5000, ...);

// create a client factory
var clientFactory = new ClientFactory();

// request the factory to generate a proxy for ICalculator service
var calculator = clientFactory.CreateClient<ICalculator>(channel);

// call Sum: sum == 6
var sum = await calculator.Sum(1, 2, 3);

// call MultiplyBy: multiplier == 2, values == [] {2, 4, 6}
var (multiplier, values) = await calculator.MultiplyBy(new[] {1, 2, 3}, 2);
```

### Client call (source code generation)

A proxy for ICalculator service will be generated in the source code.

```powershell
PS> Install-Package ServiceModel.Grpc.DesignTime
```

``` c#
// request ServiceModel.Grpc to generate a source code for ICalculator service proxy
[ImportGrpcService(typeof(ICalculator))]
internal static partial class MyGrpcServices
{
    // generated code ...
    public static IClientFactory AddCalculatorClient(this IClientFactory clientFactory, Action<ServiceModelGrpcClientOptions> configure = null) {}
}

// create a channel
var channel = new Channel("localhost", 5000, ...);

// create a client factory
var clientFactory = new ClientFactory();

// register ICalculator proxy generated by ServiceModel.Grpc.DesignTime
clientFactory.AddCalculatorClient();

// create a new instance of the proxy
var calculator = clientFactory.CreateClient<ICalculator>(channel);

// call Sum: sum == 6
var sum = await calculator.Sum(1, 2, 3);

// call MultiplyBy: multiplier == 2, values == [] {2, 4, 6}
var (multiplier, values) = await calculator.MultiplyBy(new[] {1, 2, 3}, 2);
```

> ServiceModel.Grpc.DesignTime uses roslyn [source generators](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md), which requires [net5.0 sdk](https://dotnet.microsoft.com/download/dotnet/5.0).

### Implement a service

``` c#
internal sealed class Calculator : ICalculator
{
    public Task<long> Sum(long x, int y, int z, CancellationToken token) => x + y + z;

    public ValueTask<(int Multiplier, IAsyncEnumerable<int> Values)> MultiplyBy(IAsyncEnumerable<int> values, int multiplier, CancellationToken token)
    {
        var multiplicationResult = DoMultiplication(values, multiplier, token);
        return new ValueTask<(int, IAsyncEnumerable<int>)>((multiplier, multiplicationResult));
    };

    private static async IAsyncEnumerable<int> DoMultiplication(IAsyncEnumerable<int> values, int multiplier, [EnumeratorCancellation] CancellationToken token)
    {
        await foreach (var value in values.WithCancellation(token))
        {
            yield return value * multiplier;
        }
    }
}
```

### Host the service in asp.net core application

```powershell
PS> Install-Package ServiceModel.Grpc.AspNetCore
```

``` c#
internal sealed class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // enable ServiceModel.Grpc
        services.AddServiceModelGrpc();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseEndpoints(endpoints =>
        {
            // bind Calculator service
            endpoints.MapGrpcService<Calculator>();
        });
    }
}
```

### Host the service in Grpc.Core.Server

```powershell
PS> Install-Package ServiceModel.Grpc.SelfHost
```

``` c#
var server = new Grpc.Core.Server
{
    Ports = { new ServerPort("localhost", 5000, ...) }
};

// bind Calculator service
server.Services.AddServiceModelTransient(() => new Calculator());
```

## NuGet feed

-----
Name | Package | Supported platforms | Description
-----| :------ |:------------------- | :----------
ServiceModel.Grpc | [![Version](https://img.shields.io/nuget/vpre/ServiceModel.Grpc.svg)](https://www.nuget.org/packages/ServiceModel.Grpc) | netstandard2.0/2.1, net461+ | main functionality, basic Grpc.Core.Api extensions, ClientFactory
ServiceModel.Grpc.AspNetCore | [![Version](https://img.shields.io/nuget/vpre/ServiceModel.Grpc.AspNetCore.svg)](https://www.nuget.org/packages/ServiceModel.Grpc.AspNetCore) | net5.0, .net core 3.0/3.1+ | Grpc.AspNetCore.Server extensions
ServiceModel.Grpc.SelfHost | [![Version](https://img.shields.io/nuget/vpre/ServiceModel.Grpc.SelfHost.svg)](https://www.nuget.org/packages/ServiceModel.Grpc.SelfHost) | netstandard2.0/2.1, net461+ | Grpc.Core extensions for self-hosted Grpc.Core.Server
ServiceModel.Grpc.DesignTime | [![Version](https://img.shields.io/nuget/vpre/ServiceModel.Grpc.DesignTime.svg)](https://www.nuget.org/packages/ServiceModel.Grpc.DesignTime) | netstandard2.0/2.1, net461+ | C# code generator
ServiceModel.Grpc.ProtoBufMarshaller | [![Version](https://img.shields.io/nuget/vpre/ServiceModel.Grpc.ProtoBufMarshaller.svg)](https://www.nuget.org/packages/ServiceModel.Grpc.ProtoBufMarshaller) | netstandard2.0/2.1, net461+ | marshaller factory based on protobuf-net