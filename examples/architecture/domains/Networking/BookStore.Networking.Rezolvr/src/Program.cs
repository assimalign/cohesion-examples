// public-dns: authoritative DNS for BookStore's public domain. A plain Rezolvr program.
//
// Its zone and records are settings (Zones:0:Name, Zones:0:Records:*). Run by the ResourceManager, the networking AppHost
// fills them in: the domain from the environment, the A records from the public gateway's address. Run with
// `dotnet run`, it reads them from its own settings file. Nothing here knows which platform runs it, or that a
// ResourceManager exists: when COHESION_CONTROL_PLANE_ENDPOINT is set, the App kernel's agent reports started, ready and
// revision in the background, and never delays start (v2 §5.5).

using Assimalign.Cohesion.Rezolvr.Hosting;

RezolvrApplicationBuilder builder = RezolvrApplication.CreateBuilder(args);

await using RezolvrApplication application = builder.Build();
await application.RunAsync();
