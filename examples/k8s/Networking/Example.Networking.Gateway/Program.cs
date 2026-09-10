using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);

// Command-line and environment bindings override these standalone-development fallbacks.
builder.RemoteReference(
    Externals.PlatformSecretStore,
    remote => remote.Endpoint("api", "https://localhost:18444"));
builder.RemoteReference(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
// The generic Networking SDKs currently support model description only; their runtime entries/control planes are upstream.
builder.AddAllResources();
builder.UseGateway(args);

await builder.Build().RunAsync();
