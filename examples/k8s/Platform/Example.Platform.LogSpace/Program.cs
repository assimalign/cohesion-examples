using Assimalign.Cohesion.LogSpace;
using Assimalign.Cohesion.LogSpace.Hosting;
using Example.Platform.LogSpace;

// The platform-logspace resource: OTLP ingestion, retention and query. The LogSpace builder API used here is illustrative — the LogSpace area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
LogSpaceApplicationBuilder builder = LogSpaceApplication.CreateBuilder(args);

builder.AddStorage(storage => storage.UseEmbeddedDatabase(Resource.Mounts.Data).Retain(Resource.Settings.LogSpaceRetention));
builder.AddIngest(Resource.Endpoints.Otlp, ingest => ingest.Otlp().RequireApplicationCredential());   // every resource's bootstrap credential authenticates its telemetry
builder.AddQuery(Resource.Endpoints.Query);

await builder.Build().RunAsync();
