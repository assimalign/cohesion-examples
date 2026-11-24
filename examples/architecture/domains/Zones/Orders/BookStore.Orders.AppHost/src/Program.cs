// The orders application: orders-db and orders-api, reached through the networking application's gateways.
// README.md says how to read it. [proposed], beyond the v2 design: stand-ins, the WebApplication type, classes and
// sizes, one instance in process, a route on another application's gateway, the egress gateway and AllowBind by
// application name. Roles.Operator is v2's own and decided (v2 §15, decision 29).

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("orders", Platforms.InProcess, Platforms.Kubernetes);

var paymentsHost = app.Environment.Parameter("paymentsHost");
var publicHost = app.Environment.Parameter("publicHost");

var identityApp = app.Environment.Application("identity", localStandIn: Projects.BookStore_Identity_AppHost);
var platformApp = app.Environment.Application("platform", localStandIn: Projects.BookStore_Platform_AppHost);
var usersApp = app.Environment.Application("users", localStandIn: Projects.BookStore_Users_AppHost);
var networkingApp = app.Environment.Application("networking", localStandIn: Projects.BookStore_Networking_AppHost);

// Admission admits a Get only if the owner lists "orders" in AllowBind on that resource.
var identity = identityApp.IdentityHub.Get("identity");
var logs = platformApp.LogSpace.Get("logs");
var messages = platformApp.MessageHub.Get("messages");
var usersApi = usersApp.Web.Get("users-api");
var publicGateway = networkingApp.Networking.GetGateway("public");
var egressGateway = networkingApp.Networking.GetEgressGateway("egress");

// A role of this application's own (v2 §10.6): Database's Operator plus the one right to grow a volume, which no
// predefined operations role holds. It only adds to its base, it follows that base at every ResourceManager upgrade,
// and only this document can assign it. A deploy that adds or changes it is refused (RoleEscalation) unless its
// sender holds everything the role holds, or is an Owner of the environment who sends --escalate. Its spelling and
// that of OperableSettings below are [proposed] in v2.
var ordersDbKeeper = app.Authorization.AddRoleDefinition("orders-db-keeper", new()
{
    Description = "Operate orders-db and grow its volume.",
    BasedOn = Roles.Database.Operator,
    Permissions = [Permissions.Database.StorageWrite],
});

// Database predefines port sql 5432, one writer, a volume kept on delete and the stateful class. The environment
// maps a class to node pool and storage class, and a size to requests and limits.
// RoleAssignments gives the DBAs the role above, on this database only. With it they can grow the volume through the
// API, which costs money and cannot be undone: the next deploy keeps the grown size and flags it until Storage here
// says the same.
var ordersDb = app.Database.Add<Projects.BookStore_Orders_Database>("orders-db", new()
{
    Storage = "50Gi",
    Size = Size.Large,                              // the type's default is medium
    BackupBeforeUpgrade = BackupPolicy.Required,
    Lock = ResourceLock.CannotDelete,
    RoleAssignments = [new(ordersDbKeeper, Principal.Group("orders-dba"))],
});

// WebApplication predefines port http 8080, the stateless class and size small.
// OperableSettings lists the keys that a holder of Operator on orders-api may change through the API without write:
// here the Checkout section, for orders-oncall and for everyone else who holds the role, whoever gave it to them. So
// a deploy of orders that changes the list changes what all of them may write on orders-api. A key set to a reference
// is wiring and stays closed, and the next deploy puts a changed key back to what Settings says.
var ordersApi = app.Web.Add<Projects.BookStore_Orders_Api>("orders-api", new()
{
    Scale = Scale.Between(3, 10),                   // in process: one instance, by rule of the platform
    Settings = new()
    {
        ["ConnectionStrings:Orders"] = ordersDb.ConnectionSecret,
        ["Users:Url"] = usersApi.Url,
        ["Messages:Endpoint"] = messages.Url,
        ["Payments:Host"] = paymentsHost,
        ["Authentication:Authority"] = identity.Issuer,
        ["Telemetry:Endpoint"] = logs.Url,
        ["Checkout:MaxLines"] = "50",
    },
    OperableSettings = ["Checkout:*"],
    WorkloadHatch = new()
    {
        Kubernetes = new()
        {
            AutoscaleCpuPercent = 70,
            // A patch only adds, where policy.patches allows, and throws where a typed member exists (HostNetwork).
            // In a JSON pointer ~1 is "/": the annotation is prometheus.io/scrape.
            AddToPodTemplate = new() { ["/metadata/annotations/prometheus.io~1scrape"] = "true" },
        },
    },
});

app.Networking.AddExposure("orders-public", new()
{
    Target = ordersApi,
    Gateway = publicGateway,
    Host = publicHost,
    Path = "/api/orders",
    StripPrefix = true,
});

app.Networking.AddEgressRule("payments", new()
{
    From = ordersApi,
    Through = egressGateway,
    Host = paymentsHost,
    Port = 443,
});

// Ingress is refused from everything not listed, and an exposure does not open the policy: orders-public needs its
// gateway here. With no EgressOnlyTo, egress is not limited.
app.Kubernetes.AddNetworkPolicy("orders-api-ingress", new()
{
    AppliesTo = ordersApi,
    IngressOnlyFrom = [publicGateway],
});

app.Authorization.AddRoleAssignment(Roles.Operator, Principal.Group("orders-oncall"));

return await app.RunAsync(args);
