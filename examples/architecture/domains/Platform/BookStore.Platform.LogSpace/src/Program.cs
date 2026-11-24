// logs: BookStore's logs and telemetry, which every application sends to. A plain LogSpace program.
//
// It reads one setting, Authentication:Authority, which the platform AppHost sets with Authority = identity.Issuer.
// Its data directory is a volume the LogSpace resource type predefines and mounts; the platform AppHost writes only
// its size, and the program only sees a path. Nothing here names a platform or a ResourceManager: when
// COHESION_CONTROL_PLANE_ENDPOINT is set, the App kernel's agent reports started, ready and revision in the background,
// and never delays start (v2 §5.5).

using Assimalign.Cohesion.LogSpace.Hosting;

LogSpaceApplicationBuilder builder = LogSpaceApplication.CreateBuilder(args);

await using LogSpaceApplication application = builder.Build();
await application.RunAsync();
