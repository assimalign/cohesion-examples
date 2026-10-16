using Assimalign.Cohesion.VpnGateway;
using Assimalign.Cohesion.VpnGateway.Hosting;

// The VpnGateway host builder exposes AddService and Build only; no domain-composition verbs yet.
VpnGatewayApplicationBuilder builder = VpnGatewayApplication.CreateBuilder(args);

await using VpnGatewayApplication application = builder.Build();
await application.RunAsync();
