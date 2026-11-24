// The storefront application: the browser application, on the public gateway's catch-all path.
// README.md says how to read it. [proposed], beyond the v2 design: stand-ins, the WebApplication type, sizes, a route
// on another application's gateway and AllowBind by application name.

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("storefront", Platforms.InProcess, Platforms.Kubernetes);

var publicHost = app.Environment.Parameter("publicHost");

var identityApp = app.Environment.Application("identity", localStandIn: Projects.BookStore_Identity_AppHost);
var platformApp = app.Environment.Application("platform", localStandIn: Projects.BookStore_Platform_AppHost);
var networkingApp = app.Environment.Application("networking", localStandIn: Projects.BookStore_Networking_AppHost);

// Admission admits a Get only if the owner lists "storefront" in AllowBind on that resource. No Get reads users-api
// or orders-api: the browser calls /api/users and /api/orders on its own origin.
var identity = identityApp.IdentityHub.Get("identity");
var logs = platformApp.LogSpace.Get("logs");
var config = platformApp.ConfigurationStore.Get("config");
var publicGateway = networkingApp.Networking.GetGateway("public");

var storefront = app.Web.Add<Projects.BookStore_Storefront_Host>("storefront", new()
{
    Scale = 2,  // no Size: the default, small, is 150m CPU and 128Mi more per instance than it had before
    Settings = new()
    {
        ["Authentication:Authority"] = identity.Issuer,
        ["Configuration:Endpoint"] = config.Url,
        ["Telemetry:Endpoint"] = logs.Url,
    },
});

app.Networking.AddExposure("storefront-public", new()
{
    Target = storefront,
    Gateway = publicGateway,
    Host = publicHost,
    Path = "/",
});

app.Authorization.AddRoleAssignment(Roles.Contributor, Principal.Group("storefront-team"));

return await app.RunAsync(args);
