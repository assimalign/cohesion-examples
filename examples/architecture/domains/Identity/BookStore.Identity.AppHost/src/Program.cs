// The identity application: BookStore's one IdentityHub, and identity-db, the database the hub keeps its data in.
// README.md says how to read it. [proposed], beyond the v2 design: the stand-in, a route on another application's
// gateway, classes and sizes, and AllowBind by application name. [proposed] in v2 (§8.5; §15, decision 46): how a hub
// names its database, Store = new() { Database = ... }. Decided by the owner on 2026-11-22: a hub with no database
// declared gets a private one, and declaring a database replaces it.

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("identity", Platforms.InProcess, Platforms.Kubernetes);

var corporateAuthority = app.Environment.Parameter("corporateAuthority");
var identityHost = app.Environment.Parameter("identityHost");

// Admission admits the Get only if networking lists "identity" in AllowBind on its gateway.
var networkingApp = app.Environment.Application("networking", localStandIn: Projects.BookStore_Networking_AppHost);
var publicGateway = networkingApp.Networking.GetGateway("public");

// The database the identity team owns: a resource of its own, like orders-db, with its own backup policy, its own
// lock and its own administrators. Its program is BookStore.Identity.Database, one line that calls the hub's database
// package. It stands above the hub because the hub names it (CS0841). Durability and buffer pool have no member on a
// database declaration: that program would set them.
var identityDb = app.Database.Add<Projects.BookStore_Identity_Database>("identity-db", new()
{
    Storage = "20Gi",
    Size = Size.Large,                              // the type's default is medium
    BackupBeforeUpgrade = BackupPolicy.Required,
    Lock = ResourceLock.CannotDelete,
    RoleAssignments = [new(Roles.Database.Operator, Principal.Group("identity-dba"))],
});

// Store names the database and holds nothing else: any other member beside Database is refused when this file runs.
// Without that line the hub would get a private database, its store tier, which the Store block would configure.
// identity-db would then run with nothing reading it, and nothing would report that. The line has to be there at the
// hub's first deploy. Decided by the owner on 2026-11-22: once deployed, a hub cannot switch between a private and a
// declared database, even while empty. The code of the refusal, PlacementImmutable, is [proposed] in v2 (§8.11).
app.IdentityHub.Add<Projects.BookStore_Identity_IdentityHub>("identity", new()
{
    Settings = new() { ["Federation:Upstream:Authority"] = corporateAuthority },
    Web = new()
    {
        Scale = 2,                          // admission answers PrimaryNotScalable while the type is not yet stateless
        Exposure = new() { Gateway = publicGateway, Host = identityHost },
    },
    Store = new() { Database = identityDb },        // the whole statement: this hub has no store tier
    Lock = ResourceLock.CannotDelete,
    AllowBind = ["platform", "users", "orders", "storefront"],
});

app.Authorization.AddRoleAssignment(Roles.IdentityHub.Operator, Principal.Group("identity-oncall"));

return await app.RunAsync(args);
