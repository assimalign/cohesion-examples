// messages: messaging between BookStore's applications. A plain MessageHub program.
//
// It reads Authentication:Authority and Telemetry:Endpoint, which the platform AppHost sets with Authority and
// TelemetryEndpoint: identity's issuer and logs. Its data directory is a volume the MessageHub resource type
// predefines and mounts; the platform AppHost writes only its size, and the program only sees a path. Nothing here
// names a platform or a ResourceManager: when COHESION_CONTROL_PLANE_ENDPOINT is set, the App kernel's agent reports
// started, ready and revision in the background, and never delays start (v2 §5.5).

using Assimalign.Cohesion.MessageHub.Hosting;

MessageHubApplicationBuilder builder = MessageHubApplication.CreateBuilder(args);

await using MessageHubApplication application = builder.Build();
await application.RunAsync();
