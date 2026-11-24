// site-vpn: the site-to-site tunnel to the on-premises network. A plain VpnGateway program.
//
// The peer, its routes and the pre-shared key are settings. Run by the ResourceManager, the networking AppHost supplies
// them; the key arrives as a file the driver mounts from a platform secret, never as a value in any document. Host
// network and NET_ADMIN are not here, and not in the AppHost either: the VpnGateway resource type needs them from its
// platform, and the environment's policy.privileged.allow must list the networking application ([proposed]).

using Assimalign.Cohesion.VpnGateway.Hosting;

VpnGatewayApplicationBuilder builder = VpnGatewayApplication.CreateBuilder(args);

await using VpnGatewayApplication application = builder.Build();
await application.RunAsync();
