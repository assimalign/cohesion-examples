using Assimalign.Cohesion.ConfigurationStore.Client;
using Assimalign.Cohesion.Database.Sql.Client;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Api;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Bearer;
using Assimalign.Cohesion.Web.Health;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Example.AppC.Api;

// The appc-api resource — an ordinary executable. Because CohesionApplicationModel is enabled, Sdk.Web generated
// Resource.g.cs (Resource.Name / Endpoints / Mounts / Settings / References, backed by the ambient ResourceContext that the
// gateway supplies — environment variables out-of-process, a per-invocation scope in-process) and ResourceControlPlane.g.cs
// (the Web default control plane on `http`). Locally, in-process, in Docker and on Kubernetes this file is identical.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);   // appsettings.json → appsettings.{Environment}.json → COHESION_CONFIG__* → args

// Application `platform` (non-optional crossing): promoted configuration for this zone; the zone's gateway claims the
// `appc` namespace with config.AddNamespace(...). Layered after the manifest default so a promoted value wins.
builder.Configuration.AddConfigurationStore(Resource.References.PlatformConfigurationStore.Api.Endpoint, @namespace: "appc");

// Same-application dependency: the observed endpoint of appc-database (127.0.0.1:<port> locally, the container name
// on Docker, appc-database.appc.svc on Kubernetes) behind one generated accessor.
ISqlClient inventory = SqlClient.Create(new SqlClientOptions
{
    Settings = DatabaseConnectionSettings.For(Resource.References.AppCDatabase.Db.Endpoint, database: "inventory", principal: Resource.Name),
    ConnectionFactory = Resource.References.AppCDatabase.Db.ConnectionFactory(),
});
builder.Services.AddSingleton(inventory);

// Optional external (application `identity`): absent when the crossing is unresolved, present when the gateway bound it
// (RemoteReference, --external, configuration, or the same cluster's export). This API is a token audience — claimed by
// the zone's gateway with identity.AddAudience — and callers obtain tokens from identity-hub.
Uri? authority = null;
if (Resource.References.IdentityHub.Https.TryGetUrl(out authority))
{
    builder.AddAuthentication().AddJwtBearer(options =>
    {
        options.Authority = authority;
        options.Audience = Resource.Name;
    });
}

// Aggregated into /readyz by the Web default control plane, with every other health contributor in the process.
builder.AddHealthCheck("inventory-db", async cancellation => await Health.PingAsync(Resource.References.AppCDatabase.Db, cancellation));

int pageSize = Resource.Settings.InventoryPageSize;

WebApplication app = builder.Build();
app.UseRouting();
if (authority is not null)
{
    app.UseAuthentication();
}

app.MapGet("/items", () => inventory.Query<Item>("SELECT * FROM Items ORDER BY 1 LIMIT @take", new { take = pageSize }));
app.MapGet("/items/{sku}", (string sku) => inventory.QuerySingle<Item>("SELECT * FROM Items WHERE Sku = @sku", new { sku }));
app.MapPost("/items", (Item item) => inventory.Execute("INSERT INTO Items (Sku, Name, OnHand, Reserved, UpdatedAt) VALUES (@Sku, @Name, @OnHand, @Reserved, @UpdatedAt)", item));

await app.RunAsync();

namespace Example.AppC.Api
{
    public sealed record Item(string Sku, string Name, int OnHand, int Reserved, DateTime UpdatedAt);
}
