using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.VpnGateway;
using Assimalign.Cohesion.VpnGateway.Hosting;

// The VpnGateway host builder exposes AddService and Build only; no domain-composition verbs yet.
IVpnGatewayApplicationBuilder builder = VpnGatewayApplication.CreateBuilder(args);

await builder.Build().RunAsync();
