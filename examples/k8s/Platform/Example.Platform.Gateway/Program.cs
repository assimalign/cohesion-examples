using Assimalign.Cohesion.ApplicationModel;
using Example.Platform.Gateway;

// The platform application — deployed first, the root of trust; the root Example.Gateway application set (k8s/Gateway) obtains this same model through this gateway's control plane (`--mode describe`).
IApplicationBuilder builder = Gateway.CreateBuilder(args);   // generated: Application.CreateBuilder("platform", args)

var secrets = builder.AddPlatformSecretStore();                        // self-seeds the org CA; holds TrustedIssuers
var config  = builder.AddPlatformConfigurationStore();                 // DependsOn(secrets) inferred (tls mount)
var logs    = builder.AddPlatformLogSpace();                           // DependsOn(secrets) inferred

builder.UseTelemetry(logs);   // every resource in this application — and, hosted by the root application set, every application in the cluster — gets COHESION_TELEMETRY_ENDPOINT

builder.UseGateway(args);     // --gateway local|inprocess|docker|kubernetes
// Platform-specific exceptions (rare) go here and nowhere else: builder.UseGateway(args, gateways => gateways.Kubernetes(k => ...)); the callback runs only when that gateway is selected.
await builder.Build().RunAsync();   // --mode run|apply|teardown|bootstrap|describe · --context cluster-0N · --external · --realize
