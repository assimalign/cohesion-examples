// The users application: users-db and users-api, on the public gateway under /api/users.
// README.md says how to read it. [proposed], beyond the v2 design: stand-ins, the WebApplication type, a route on
// another application's gateway, the disruption budget and AllowBind by application name.
//
// This is the one AppHost of the example written in the callback overload, `options => { ... }`. The other five use
// the object overload, `new() { ... }`. Both declare the same resources: this file prints the same run in either.
// A callback is code, so statements may stand between its assignments; this file uses none.

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("users", Platforms.InProcess, Platforms.Kubernetes);

var publicHost = app.Environment.Parameter("publicHost");

var identityApp = app.Environment.Application("identity", localStandIn: Projects.BookStore_Identity_AppHost);
var platformApp = app.Environment.Application("platform", localStandIn: Projects.BookStore_Platform_AppHost);
var networkingApp = app.Environment.Application("networking", localStandIn: Projects.BookStore_Networking_AppHost);

// Admission admits a Get only if the owner lists "users" in AllowBind on that resource.
var identity = identityApp.IdentityHub.Get("identity");
var logs = platformApp.LogSpace.Get("logs");
var publicGateway = networkingApp.Networking.GetGateway("public");

var usersDb = app.Database.Add<Projects.BookStore_Users_Database>("users-db", options =>
{
    options.Storage = "10Gi";                       // the type's default, written anyway: a disk size is data
    options.BackupBeforeUpgrade = BackupPolicy.Required;
    options.Lock = ResourceLock.CannotDelete;
    options.RoleAssignments = [new(Roles.Database.Operator, Principal.Group("users-dba"))];
});

var usersApi = app.Web.Add<Projects.BookStore_Users_Api>("users-api", options =>
{
    options.Scale = 2;
    options.Settings = new()
    {
        ["ConnectionStrings:Users"] = usersDb.ConnectionSecret,
        ["Authentication:Authority"] = identity.Issuer,
        ["Telemetry:Endpoint"] = logs.Url,
    };
    // orders-api calls users-api.
    options.AllowBind = ["orders"];
});

// What an exposure requires, its target, stands before the callback as an argument of the verb. [proposed]
app.Networking.AddExposure("users-public", usersApi, options =>
{
    options.Gateway = publicGateway;
    options.Host = publicHost;
    options.Path = "/api/users";
    options.StripPrefix = true;
});

app.Kubernetes.AddPodDisruptionBudget("users-api", usersApi, options => options.MinAvailable = 1);

app.Authorization.AddRoleAssignment(Roles.Operator, Principal.Group("users-oncall"));

return await app.RunAsync(args);
