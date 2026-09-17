using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.IdentityHub.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
builder.RemoteReference(
    Externals.PlatformSecretStore,
    remote => remote.Endpoint("api", "https://localhost:18444"));
IIdentityHubResourceDescriptor identity = builder.AddIdentityHub();

// §4.3 assigns this declaration to the consuming application; IdentityHub.ApplicationModel
// ships no RemoteReferenceIdentityHub binder, so it is declared by the owning gateway until that gap closes.
// These are confidential service clients, not the design's browser authorization-code clients.
foreach (string application in new[] { "appa", "appb", "appc" })
{
    identity.AddAudience($"{application}-api")
        .AddClient($"{application}-service", [$"{application}-api"], credentialSource: $"{application}-client");
}

builder.UseGateway(args);
await builder.Build().RunAsync();
