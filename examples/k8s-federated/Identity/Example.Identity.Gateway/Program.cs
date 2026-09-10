using Assimalign.Cohesion.ApplicationModel;
using Example.Identity.Gateway;

// The identity application.
IApplicationBuilder builder = Gateway.CreateBuilder(args);   // generated: Application.CreateBuilder("identity", args)

// Platform is the root of trust: its SecretStore holds identity's signing keys and TLS material (the peer gateway's control plane).
var platformSecrets = builder.RemoteReference(Externals.PlatformSecretStore, remote => remote.Gateway("https://platform.example.com/cohesion"));

var hub = builder.AddIdentityHub();   // DependsOn(platformSecrets) inferred from identity-hub's manifest

builder.UseGateway(args);             // --gateway local|inprocess|docker|kubernetes
// Platform-specific exceptions (rare) go here and nowhere else: builder.UseGateway(args, gateways => gateways.Kubernetes(k => ...)); the callback runs only when that gateway is selected.
await builder.Build().RunAsync();     // --mode run|apply|teardown|bootstrap|describe · --context cluster-0N · --external · --realize
