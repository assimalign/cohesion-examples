using Assimalign.Cohesion.ApplicationModel;
using Example.Platform.Gateway;

// The platform application — deployed first, the root of trust.
IApplicationBuilder builder = Gateway.CreateBuilder(args);   // generated: Application.CreateBuilder("platform", args)

var secrets = builder.AddPlatformSecretStore();                        // self-seeds the org CA; holds TrustedIssuers
var config  = builder.AddPlatformConfigurationStore();                 // DependsOn(secrets) inferred (tls mount)
var logs    = builder.AddPlatformLogSpace();                           // DependsOn(secrets) inferred

builder.UseTelemetry(logs);   // Platform resources only; other applications bind LogSpace as an external over a public `otlp` endpoint (design item 31b)

builder.UseGateway(args);     // --gateway local|inprocess|docker|kubernetes
// Platform-specific exceptions (rare) go here and nowhere else: builder.UseGateway(args, gateways => gateways.Kubernetes(k => ...)); the callback runs only when that gateway is selected.
await builder.Build().RunAsync();   // --mode run|apply|teardown|bootstrap|describe · --context cluster-0N · --external · --realize
