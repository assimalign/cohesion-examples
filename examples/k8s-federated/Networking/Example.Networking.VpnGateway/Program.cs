using System.Net;
using Assimalign.Cohesion.VpnGateway;
using Assimalign.Cohesion.VpnGateway.Hosting;
using Example.Networking.VpnGateway;

// The networking-vpn-gateway resource: WireGuard peers and routes as C#. The VpnGateway builder API used here is illustrative — the VpnGateway area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
VpnGatewayApplicationBuilder builder = VpnGatewayApplication.CreateBuilder(args);

builder.AddWireGuard(wireguard =>
{
    wireguard.Listen(Resource.Endpoints.Wireguard);
    wireguard.Keys(Resource.Mounts.Keys);                        // private key material never appears in code
    wireguard.Peer("office", peer =>
    {
        peer.PublicKey(Resource.Mounts.Keys.Read("office.pub"));
        peer.Endpoint("vpn.office.example.com", 51820);
        peer.AllowedIps(IPNetwork.Parse("10.10.0.0/16"));
        peer.PersistentKeepalive(TimeSpan.FromSeconds(25));
    });
});

await builder.Build().RunAsync();
