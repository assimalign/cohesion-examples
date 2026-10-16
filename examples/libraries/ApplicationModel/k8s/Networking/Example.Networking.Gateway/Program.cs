using System.Net;

using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
builder.RemoteReference(
    Externals.PlatformSecretStore,
    remote => remote.Endpoint("api", "https://localhost:18444"));
builder.RemoteReference(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
// Networking owns no store: the VPN keys are a parameter: mount (cross-application store sources
// are not supported yet), so no provider is registered.
IRezolvrResourceDescriptor dns = builder.AddRezolvr(Manifests.NetworkingRezolvr);
IVpnGatewayResourceDescriptor vpn = builder.AddVpnGateway(Manifests.NetworkingVpnGateway);

// §4.3 assigns this declaration to the consuming application; Rezolvr.ApplicationModel
// ships no RemoteReferenceRezolvr binder, so it is declared by the owning gateway until that gap closes.
if (builder.Environment.IsLocal)
{
    dns.AddARecord("appa-dev", IPAddress.Loopback)
        .AddARecord("appb-dev", IPAddress.Loopback)
        .AddARecord("appc-dev", IPAddress.Loopback);
}

builder.UseGateway(args);
await builder.Build().RunAsync();
