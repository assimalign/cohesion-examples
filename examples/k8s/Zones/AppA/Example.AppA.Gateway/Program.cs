using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);

// AppA consumes this resource across the platform boundary, so the SDK generates
// Externals.PlatformConfigurationStore instead of a local AddPlatformConfigurationStore verb.
builder.RemoteReference(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
builder.RemoteReference(Externals.IdentityHub, remote => { });

builder.AddAppADatabase();
builder.AddAppAApi();
builder.AddAppASpa();
// AppASecretStore stays referenced and generated, but its generic SDK does not yet register
// an entry/control plane; realizing it would prevent today's Local/InProcess run from becoming ready.
builder.UseGateway(args);

await builder.Build().RunAsync();
