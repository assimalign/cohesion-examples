// The platform application: logs, configuration and messaging, shared by every BookStore application.
// README.md says how to read it. [proposed], beyond the v2 design: the stand-in, the LogSpace, ConfigurationStore
// and MessageHub types, classes and sizes, and AllowBind by application name.

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("platform", Platforms.InProcess, Platforms.Kubernetes);

// Admission admits the Get only if identity lists "platform" in AllowBind on the hub.
var identityApp = app.Environment.Application("identity", localStandIn: Projects.BookStore_Identity_AppHost);
var identity = identityApp.IdentityHub.Get("identity");

// Each type predefines its port, one writer, a volume kept on delete, the stateful class and its size.
var logs = app.LogSpace.Add<Projects.BookStore_Platform_LogSpace>("logs", new()
{
    Storage = "100Gi",
    Authority = identity.Issuer,
    Lock = ResourceLock.CannotDelete,
    AllowBind = ["users", "orders", "storefront"],
});

app.ConfigurationStore.Add<Projects.BookStore_Platform_ConfigurationStore>("config", new()
{
    Storage = "5Gi",
    Authority = identity.Issuer,
    TelemetryEndpoint = logs.Url,
    Lock = ResourceLock.CannotDelete,
    AllowBind = ["storefront"],
});

app.MessageHub.Add<Projects.BookStore_Platform_MessageHub>("messages", new()
{
    Storage = "50Gi",
    Authority = identity.Issuer,
    TelemetryEndpoint = logs.Url,
    Lock = ResourceLock.CannotDelete,
    AllowBind = ["orders"],
});

app.Authorization.AddRoleAssignment(Roles.Operator, Principal.Group("platform-oncall"));

return await app.RunAsync(args);
