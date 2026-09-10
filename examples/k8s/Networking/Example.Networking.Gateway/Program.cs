using Assimalign.Cohesion.ApplicationModel;
using Example.Networking.Gateway;

// The networking application; the root Example.Gateway application set (k8s/Gateway) obtains this same model through this gateway's control plane (`--mode describe`).
IApplicationBuilder builder = Gateway.CreateBuilder(args);   // generated: Application.CreateBuilder("networking", args)

// Two crossings into `platform` (the same cluster's namespace through the operator kubeconfig channel; context from Cohesion:Kubernetes:Context).
var platformSecrets = builder.RemoteReference(Externals.PlatformSecretStore, remote => remote.Kubernetes("platform"));
var platformConfig  = builder.RemoteReference(Externals.PlatformConfigurationStore, remote => remote.Kubernetes("platform"));

var dns = builder.AddRezolvr();      // DependsOn(platformConfig) inferred (forwarders)
var vpn = builder.AddVpnGateway();   // DependsOn(platformSecrets) inferred (its `keys` mount)

builder.UseGateway(args);            // --gateway local|docker|kubernetes (no inprocess: networking-vpn-gateway is composable=false)
// Platform-specific exceptions (rare) go here and nowhere else: builder.UseGateway(args, gateways => gateways.Kubernetes(k => ...)); the callback runs only when that gateway is selected.
await builder.Build().RunAsync();    // --mode run|apply|teardown|bootstrap|describe · --context cluster-0N · --external · --realize
