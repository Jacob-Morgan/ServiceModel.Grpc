# ServiceModel.Grpc

`ServiceModel.Grpc` enables applications to communicate with gRPC services using a code-first approach (no .proto files), helps to get around limitations of gRPC protocol like "only reference types", "exact one input", "no nulls", "no value-types". Provides exception handling. Helps to migrate existing WCF solution to gRPC with minimum effort.

The library supports lightweight runtime proxy generation via Reflection.Emit and C# source code generation.

The solution is built on top of [gRPC C#](https://github.com/grpc/grpc/tree/master/src/csharp) and [grpc-dotnet](https://github.com/grpc/grpc-dotnet).

## Links

- [service and operation names](ServiceAndOperationName.md)
- [service and operation bindings](ServiceAndOperationBinding.md)
- [client configuration](ClientConfiguration.md)
- [client code generation](client-code-generation.md)
- [server code generation](server-code-generation.md)
- operations
  - [unary](operation-unary.md)
  - [client streaming](operation-client-streaming.md)
  - [server streaming](operation-server-streaming.md)
  - [duplex streaming](operation-duplex-streaming.md)
- [ASP.NET Core server configuration](ASPNETCoreServerConfiguration.md)
- [Grpc.Core server configuration](GrpcCoreServerConfiguration.md)
- [exception handling general information](error-handling-general.md)
- [global exception handling](global-error-handling.md)
- [client filters](client-filters.md)
- [server filters](server-filters.md)
- [getting started](CreateClientAndServerASPNETCore.md) create a gRPC client and server
- [compatibility with native gRPC](CompatibilityWithNativegRPC.md)
- [migrate from WCF to a gRPC](MigrateWCFServiceTogRPC.md)
- [migrate from WCF FaultContract to a gRPC global error handling](migrate-wcf-faultcontract-to-global-error-handling.md)
- [examples](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples)
  - [swagger integration](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/Swagger)
  - [MessagePack marshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/MessagePackMarshaller)
  - [protobuf marshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/ProtobufMarshaller)
  - [custom marshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/CustomMarshaller)

## Usage

### Declare a service contract

``` c#
[DataContract]
public class Person
{
    [DataMember]
    public string FirstName { get; set; }

    [DataMember]
    public string SecondName { get; set; }
}

[ServiceContract]
public interface IGreeter
{
    [OperationContract]
    Task<string> SayHello(Person person, CancellationToken token = default);

    [OperationContract]
    ValueTask<(string Greeting, IAsyncEnumerable<string> Greetings)> Greet(IAsyncEnumerable<Person> persons, string greeting, CancellationToken token = default);
}
```

### Client call (Reflection.Emit)

A proxy for IGreeter service will be generated on demand via `Reflection.Emit`.

```powershell
PS> Install-Package ServiceModel.Grpc
```

``` c#
// create a channel
var channel = new Channel("localhost", 5000, ...);

// create a client factory
var clientFactory = new ClientFactory();

// request the factory to generate a proxy for IGreeter service
var greeter = clientFactory.CreateClient<IGreeter>(channel);

// call SayHello
var greet = await greeter.SayHello(new Person { FirstName = "John", SecondName = "X" });

// call Greet
var (greeting, greetings) = await greeter.Greet(new[] { new Person { FirstName = "John", SecondName = "X" } }, "hello");
```

### Client call (source code generation)

A proxy for IGreeter service will be generated in the source code.

```powershell
PS> Install-Package ServiceModel.Grpc.DesignTime
```

``` c#
// request ServiceModel.Grpc to generate a source code for IGreeter service proxy
[ImportGrpcService(typeof(IGreeter))]
internal static partial class MyGrpcServices
{
    // generated code ...
    public static IClientFactory AddGreeterClient(this IClientFactory clientFactory, Action<ServiceModelGrpcClientOptions> configure = null) {}
}

// create a channel
var channel = new Channel("localhost", 5000, ...);

// create a client factory
var clientFactory = new ClientFactory();

// register IGreeter proxy generated by ServiceModel.Grpc.DesignTime
clientFactory.AddGreeterClient();

// create a new instance of the proxy
var greeter = clientFactory.CreateClient<IGreeter>(channel);

// call SayHello
var greet = await greeter.SayHello(new Person { FirstName = "John", SecondName = "X" });

// call Greet
var (greeting, greetings) = await greeter.Greet(new[] { new Person { FirstName = "John", SecondName = "X" } }, "hello");
```

> ServiceModel.Grpc.DesignTime uses roslyn [source generators](https://github.com/dotnet/roslyn/blob/master/docs/features/source-generators.md), which requires [net5.0 sdk](https://dotnet.microsoft.com/download/dotnet/5.0).

### Implement a service

``` c#
internal sealed class Greeter : IGreeter
{
    public Task<string> SayHello(Person person, CancellationToken token = default)
    {
        return string.Format("Hello {0} {1}", person.FirstName, person.SecondName);
    }

    public ValueTask<(string Greeting, IAsyncEnumerable<string> Greetings)> Greet(IAsyncEnumerable<Person> persons, string greeting, CancellationToken token)
    {
        var greetings = DoGreet(persons, greeting, token);
        return new ValueTask<(string, IAsyncEnumerable<string>)>((greeting, greetings));
    }

    private static async IAsyncEnumerable<string> DoGreet(IAsyncEnumerable<Person> persons, string greeting, [EnumeratorCancellation] CancellationToken token)
    {
        await foreach (var person in persons.WithCancellation(token))
        {
            yield return string.Format("{0} {1} {2}", greeting, person.FirstName, person.SecondName);
        }
    }
}
```

### Host the service in the asp.net core application

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
            // bind Greeter service
            endpoints.MapGrpcService<Greeter>();
        });
    }
}
```

Integrate with Swagger, see [example](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/Swagger)

![UI demo](https://raw.githubusercontent.com/max-ieremenko/ServiceModel.Grpc/master/Examples/Swagger/greeter-swagger-ui.png)

### Host the service in Grpc.Core.Server

```powershell
PS> Install-Package ServiceModel.Grpc.SelfHost
```

``` c#
var server = new Grpc.Core.Server
{
    Ports = { new ServerPort("localhost", 5000, ...) }
};

// bind Greeter service
server.Services.AddServiceModelTransient(() => new Greeter());
```

### Server filters

see [example](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/ServerFilters)

``` c#
// Startup.cs
public void ConfigureServices(IServiceCollection services)
{
    // setup filter life time
    services.AddSingleton<LoggingServerFilter>();

    // attach the filter globally
    services.AddServiceModelGrpc(options =>
    {
        options.Filters.Add(1, provider => provider.GetRequiredService<LoggingServerFilter>());
    });
}

internal sealed class LoggingServerFilter : IServerFilter
{
    private readonly ILoggerFactory _loggerFactory;

    public LoggingServerFilter(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
    }

    public async ValueTask InvokeAsync(IServerFilterContext context, Func<ValueTask> next)
    {
        // create logger with a service name
        var logger = _loggerFactory.CreateLogger(context.ServiceInstance.GetType().Name);

        // log input
        logger.LogInformation("begin {0}", context.ContractMethodInfo.Name);
        foreach (var entry in context.Request)
        {
            logger.LogInformation("input {0} = {1}", entry.Key, entry.Value);
        }

        try
        {
            // invoke all other filters in the stack and the service method
            await next().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // log exception
            logger.LogError("error {0}: {1}", context.ContractMethodInfo.Name, ex);
            throw;
        }

        // log output
        logger.LogInformation("end {0}", context.ContractMethodInfo.Name);
        foreach (var entry in context.Response)
        {
            logger.LogInformation("output {0} = {1}", entry.Key, entry.Value);
        }
    }
}
```

## Data contracts

By default, the DataContractSerializer is used for marshaling data between the server and the client. This behavior is configurable, see examples [ProtobufMarshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/ProtobufMarshaller), [MessagePackMarshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/MessagePackMarshaller), [CustomMarshaller](https://github.com/max-ieremenko/ServiceModel.Grpc/tree/master/Examples/CustomMarshaller).

``` c#
[DataContract]
public class Person
{
    [DataMember]
    public string Name { get; set;}

    [DataMember]
    public DateTime BirthDay { get; set;}
}
```

## Service contracts

A service contract is a public interface marked with ServiceContractAttribute.
Methods marked with OperationContractAttribute are gRPC calls.

> for net461 System.ServiceModel.dll, for netstandard package System.ServiceModel.Primitives

``` c#
[ServiceContract]
public interface IPersonService
{
    // gRPC call
    [OperationContract]
    Task Ping();

    // method is not gRPC call
    Task Ping();
}
```

## Operation contracts

Any operation in a service contract is one of gRPC method: [Unary](operation-unary.md), [ClientStreaming](operation-client-streaming.md), [ServerStreaming](operation-server-streaming.md) or [DuplexStreaming](operation-duplex-streaming.md).

#### Context parameters

ServiceModel.Grpc.CallContext

``` c#
// contract
[OperationContract]
Task Ping(CallContext context = default);

// client
await client.Ping(new CallOptions(....));

// server
Task Ping(CallContext context)
{
    // take ServerCallContext
    Grpc.Core.ServerCallContext serverContext = context;
    var token = serverContext.CancellationToken;
    var requestHeaders = serverContext.RequestHeaders;
}
```

Grpc.Core.CallOptions

``` c#
// contract
[OperationContract]
Task Ping(CallOptions context = default);
[OperationContract]
Task Ping(CallOptions? context = default);

// client
await client.Ping(new CallOptions(....));

// server
Task Ping(CallOptions context)
{
    // the following properties are copied from the current Grpc.Core.ServerCallContext
    var token = context.CancellationToken;
    var requestHeaders = context.RequestHeaders;
    var deadline = context.Deadline;
    var writeOptions = context.WriteOptions;
}
```

Grpc.Core.ServerCallContext

``` c#
// contract
[OperationContract]
Task Ping(ServerCallContext context = default);

// client
await client.Ping();

// server
Task Ping(ServerCallContext context)
{
    var token = context.CancellationToken;
    var requestHeaders = context.RequestHeaders;
}
```

System.Threading.CancellationToken

``` c#
// contract
[OperationContract]
Task Ping(CancellationToken token = default);
[OperationContract]
Task Ping(CancellationToken? token = default);

// client
var tokenSource = new CancellationTokenSource();
await client.Ping(tokenSource.Token);

// server
Task Ping(CancellationToken token)
{
    // the token was copied from the current Grpc.Core.ServerCallContext
    if (!token.IsCancellationRequested)
    {
        // ...
    }
}
```

## Limitations

- generic methods are not supported
- ref and out parameters are not supported
