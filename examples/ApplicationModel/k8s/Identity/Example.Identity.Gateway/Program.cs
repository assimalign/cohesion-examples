using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
builder.RemoteReference(
    Externals.PlatformSecretStore,
    remote => remote.Endpoint("api", "https://localhost:18444"));
// Identity owns no store: IdentityHub's TLS leaf, signing keys, and client secrets are parameter:
// mounts (cross-application store sources are not supported yet), so no provider is registered.
IIdentityHubResourceDescriptor identity = builder.AddIdentityHub(Manifests.IdentityHub);

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
