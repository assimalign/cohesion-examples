using Assimalign.Cohesion.ConfigurationStore;
using Assimalign.Cohesion.ConfigurationStore.Hosting;
using Example.Platform.ConfigurationStore;

// The platform-configurationstore resource: namespaces, seeds and promotion policy as C#. The ConfigurationStore builder API used here is illustrative — the ConfigurationStore area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
ConfigurationStoreApplicationBuilder builder = ConfigurationStoreApplication.CreateBuilder(args);

builder.AddStore(store => store.UseEmbeddedDatabase(Resource.Mounts.Data));

builder.AddPromotion(stages => stages.Add("development").Add("staging").Add("production", requiresApproval: true));

// Platform-owned namespaces. Zone namespaces (appa, appb, appc) arrive as configurationstore.namespace commands from the
// zones' gateways, through this resource's default control plane, and are owned by them.
builder.AddNamespace("shared",     ns => ns.SeedFromFile(builder.ContentRoot.File("Configuration/shared.json")));
builder.AddNamespace("networking", ns => ns.SeedFromFile(builder.ContentRoot.File("Configuration/networking.json")));

builder.AddApi(Resource.Endpoints.Api);   // read/watch for clients, write for promotions
await builder.Build().RunAsync();
