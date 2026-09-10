using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);

// Command-line and environment bindings override this standalone-development fallback.
builder.RemoteReference(
    Externals.PlatformSecretStore,
    remote => remote.Endpoint("api", "https://localhost:18444"));
// The generic IdentityHub SDK currently supports model description only; its runtime entry/control plane is upstream.
builder.AddAllResources();
builder.UseGateway(args);

await builder.Build().RunAsync();
