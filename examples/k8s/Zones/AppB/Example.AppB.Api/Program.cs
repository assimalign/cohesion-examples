using Assimalign.Cohesion.ConfigurationStore.Client;
using Assimalign.Cohesion.Database.Sql.Client;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Api;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Bearer;
using Assimalign.Cohesion.Web.Health;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Example.AppB.Api;

// The appb-api resource — an ordinary executable. Because CohesionApplicationModel is enabled, Sdk.Web generated
// Resource.g.cs (Resource.Name / Endpoints / Mounts / Settings / References, backed by the ambient ResourceContext that the
// gateway supplies — environment variables out-of-process, a per-invocation scope in-process) and ResourceControlPlane.g.cs
// (the Web default control plane on `http`). Locally, in-process, in Docker and on Kubernetes this file is identical.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);   // appsettings.json → appsettings.{Environment}.json → COHESION_CONFIG__* → args

// Application `platform` (non-optional crossing): promoted configuration for this zone; the zone's gateway claims the
// `appb` namespace with config.AddNamespace(...). Layered after the manifest default so a promoted value wins.
builder.Configuration.AddConfigurationStore(Resource.References.PlatformConfigurationStore.Api.Endpoint, @namespace: "appb");

// Same-application dependency: the observed endpoint of appb-database (127.0.0.1:<port> locally, the container name
// on Docker, appb-database.appb.svc on Kubernetes) behind one generated accessor.
ISqlClient billing = SqlClient.Create(new SqlClientOptions
{
    Settings = DatabaseConnectionSettings.For(Resource.References.AppBDatabase.Db.Endpoint, database: "billing", principal: Resource.Name),
    ConnectionFactory = Resource.References.AppBDatabase.Db.ConnectionFactory(),
});
builder.Services.AddSingleton(billing);

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
builder.AddHealthCheck("billing-db", async cancellation => await Health.PingAsync(Resource.References.AppBDatabase.Db, cancellation));

int pageSize = Resource.Settings.BillingPageSize;

WebApplication app = builder.Build();
app.UseRouting();
if (authority is not null)
{
    app.UseAuthentication();
}

app.MapGet("/invoices", () => billing.Query<Invoice>("SELECT * FROM Invoices ORDER BY 1 LIMIT @take", new { take = pageSize }));
app.MapGet("/invoices/{id}", (long id) => billing.QuerySingle<Invoice>("SELECT * FROM Invoices WHERE Id = @id", new { id }));
app.MapPost("/invoices", (Invoice invoice) => billing.Execute("INSERT INTO Invoices (Id, AccountId, IssuedAt, DueAt, Amount, Paid) VALUES (@Id, @AccountId, @IssuedAt, @DueAt, @Amount, @Paid)", invoice));

await app.RunAsync();

namespace Example.AppB.Api
{
    public sealed record Invoice(long Id, long AccountId, DateTime IssuedAt, DateTime DueAt, decimal Amount, bool Paid);
}
