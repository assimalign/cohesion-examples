using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
// The gateway names what it composes: each area's hand-written verb over the manifest the build
// captured for that referenced resource. Acme has no store, certificate authority, or telemetry
// sink, so it registers no providers.
builder.AddDatabase(Manifests.AcmeDatabase);
builder.AddWeb(Manifests.AcmeApi);
builder.UseGateway(args);

await builder.Build().RunAsync();
