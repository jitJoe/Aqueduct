# Aqueduct

Aqueduct is a code-first bi-directional RPC system for Blazor WASM and ASP.Net Core that sits atop SignalR.

## Aims
 - Expedite development speed by reducing message handling/sending boilerplate and instead dealing entirely in C# concepts (return values and exceptions rather than messages)
 - Reduce mistakes and cost-of-change by using a shared strongly-typed interface as a contract between client and server rather than implicit contracts - catch mistakes caused by a contract change at compile time rather than runtime

## Example Flow (Client->Server)
 - Define an interface in a shared assembly
   - Every method must return a `Task` or `Task<T>`
   - For methods returning `Task<T>`, `T` must be a type that is available to both the Client and Server assemblies (and present in an allow-list)
   - All method parameters must be types that are available to both the Client and Server assemblies (and present in an allow-list)
 - Implement the interface in your ASP.Net server project
   - Any exceptions that are thrown must be of a type that is available to both the Client and Server assemblies, if it is not then it will be substituted for the base Exception type
 - In your Client project, request an instance of the interface from a service provider
 - Call methods on the instance
   - If method arguments are of a derived type, that type must also be available to the Server assembly (and present in an allow-list)
 - Receive the result or exception as if it were a local call
   - If no response is received within a timeout window (30s by default), the Task will be cancelled (however, this does not cancel execution on the Server side if it has started)
 
## NuGet
 - https://www.nuget.org/packages/Aqueduct.Client
 - https://www.nuget.org/packages/Aqueduct.Server
 - https://www.nuget.org/packages/Aqueduct.Shared

## Example Project
 - https://github.com/jitJoe/AqueductExample

## Concepts

### Client
The Client is the browser tab running your Blazor WASM application.  Each Client is given a Connection ID by the Server (this is not provided to the Client unless your Server code does so explicitly.)

### Server
The Server is your ASP.Net Core application to which the Clients connect.

### Client Services
A Client Service is a class that is implemented on the Client side that the Server can obtain a proxy for via one of the methods on `Aqueduct.Server.ServiceProvider.IServerServiceProvider`.  Each proxy to a Client Service is specific to a given Client and will invoke methods on that Client.

Client Services must derive from `Aqueduct.Client.ClientService`, this provides a ClientServiceProvider which can be used to fetch other Aqueduct services.

#### Client Local Services
A Client Service may want to expose methods to other components on the Client, without exposing them to the Server.  For example, a method that allows Blazor Components to subscribe to updates by providing an Action.  For this purpose, an interface can be defined in the Client assembly and added to the list of interfaces the Client Service implements; the `Aqueduct.Client.ServiceProvider.IClientServiceProvider` can then be used to obtain the Client Service as the local interface.

Server Services must derive from `Aqueduct.Server.ServerService`, this provides a ServerServiceProvider which can be used to fetch other Aqueduct services and provides the Aqueduct Connection ID for the Client.

### Server Services
A Server Service is a class that is implemented on the Server side that a Client can obtain a proxy for via one of the methods on `Aqueduct.Client.ServiceProvider.IClientServiceProvider`.

#### Server Local Services
A Server Service may want to expose methods to other components on the Server, without exposing them to the Client.  For example, a method that allows a BackgroundService to hand over work to a method that Clients should not be allowed to invoke.  For this purpose, an interface can be defined in the Server assembly and added to the list of interfaces the Server Service implements; the `Aqueduct.Server.ServiceProvider.IServerServiceProvider` can then be used to obtain the Server Service as the local interface.

### Service Providers
The `Aqueduct.Client.ServiceProvider.IClientServiceProvider` and `Aqueduct.Server.ServiceProvider.IServerServiceProvider` interfaces can be obtained via the standard ASP.Net/Blazor WASM built-in dependency injection methods.  However, Client and Server services must be obtained via these interfaces.  The first constructor will be used for instantiation, if it has parameters these will be resolved using the framework's dependency injection support.

### Allowed Type Lists
There are two types of allowed type list: `Serialisation` and `Services`.  These lists are intended to prevent a malicious actor from tricking the Server into instantiating types that it shouldn't and to prevent either side from sending an assignment compatible serialisation of a class the other has no knowledge of.

The types in a serialisation list are allowed to be used as Client Service/Server Service interface method return types (the generic parameter T in `Task<T>` specifically), as parameters on those methods and as assignment compatible instances for their members (e.g. sending a `Dog` for a member typed as `Animal`).

The types in a services list are allowed to be used as Client Service/Server Service interfaces and implementations.

These lists are provided to the Client and Server independently via their Startup/Program extension methods (`Aqueduct.Server.Extensions.AddAqueductExtensions::AddAqueduct` and `Aqueduct.Client.Extensions.AddAqueductExtensions::AddAqueduct`).  As the Serialisation lists should match, it makes sense to define one list in a shared assembly and use this on both sides.

These lists support adding an entire assembly, the simplest approach is to add everything in a shared assembly (along with specifically adding other types you use (string, Exception for example)) for both then not to worry about it again.  However, you should exercise caution if taking this approach (see below.)

For generic types, you can add a fully constructed type (e.g. `List<string>`), in which case only that specific type will be allowed.  Alternatively, you can add an open type (e.g. `List<>` or `Dictionary<,>`) in which case, at runtime, the type parameters can be any other allowed type (e.g. if you have added `string` and `decimal` then `Dictionary<string, decimal>` would be permitted at runtime).

There are some peculiarities with serialising Exceptions, namely that it uses an internal type `System.Collections.ListDictionaryInternal`, you can add this (and other non-public classes) to the allowed list like such:

```
typeof(List<>).Assembly.GetType("System.Collections.ListDictionaryInternal")
```

It is important that you understand the security implications of adding types to these lists:
 - You should be happy with an instance of any type that is included in the Serialisation list being instantiated at will by a third party.
 - Types in the Services list will also need to derive from the relevant base class (ClientService or ServerService) or they will never be instantiated.  However, for any types this applies to you should be happy with methods being invoked on an instance of that type at will by a third party.
   - Do not rely on how your Client calls these methods for security, a malicious actor can easily provide arbitrary arguments that conform to the method parameter types (e.g. do not expose a ReadFile method that accepts a file path under the assumption that your Client only ever calls it with a safe file name)

## Limitations
 - Generic Client Service/Server Service interfaces and methods are not supported
 - No retry mechanism for failed or timed-out invocations/callbacks
 - No horizontal scaling (backplane) support (yet)
 - No out-of-the-box wider integration into Asp.Net core, e.g. authentication/authorisation
 - No support for non-Blazor WASM SignalR clients
 - Only supports .Net 5 and later, earlier versions of SignalR did not support parallel invocations which in turn meant Server methods that called back to the Client could never complete

## Disclaimer
This project and its source code are provided without any warranty or guarantee.  No liability is accepted for its use.
