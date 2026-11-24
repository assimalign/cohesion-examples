// identity: BookStore's identity provider for its own APIs and users. A plain IdentityHub program; the same program
// serves every placement of the store (v2 §8.3).
//
// What is no longer here: the zone gateways' clients and their grants (routes.write on ingress, rules.write on egress).
// Those were control-plane permissions, and the control plane is now the ResourceManager, with its own IdentityHub and
// RBAC. This program lists data-plane clients only: who may call BookStore's APIs.

using Assimalign.Cohesion.IdentityHub.Hosting;

IdentityHubApplicationBuilder builder = IdentityHubApplication.CreateBuilder(args);

// BookStore's APIs: the audiences this IdentityHub issues tokens for.
builder.AddAudience("api://bookstore/users");
builder.AddAudience("api://bookstore/orders");

// [proposed] Accept the tokens the environment's ResourceManager issues to its own workloads, as the client assertion
// in a client-credentials request. The RM already proves each workload's identity from its platform token (a projected
// ServiceAccount token on Kubernetes, checked with TokenReview and the pod's uid label, v2 §5.5); issuing that
// identity for another audience lets a workload call another application's API with no secret anywhere. The issuer
// comes from the runtime contract (COHESION_CONTROL_PLANE_ENDPOINT + /identity), so nothing here names an environment.
builder.TrustWorkloadIdentities();

// orders-api calls users-api as itself. The workload is named by application and resource name; the RM resolves it to
// the resource's uid per environment, so recreating orders-api under the same name keeps the client, and a different
// application's `orders-api` never matches.
builder.AddClient("orders-api", client =>
{
    client.Workload = WorkloadSubject.Of(application: "orders", name: "orders-api");          // [proposed]
    client.Audiences.Add("api://bookstore/users");
    client.AccessTokenLifetime = TimeSpan.FromMinutes(15);
});

// storefront signs BookStore's customers in, and the browser calls /api/users and /api/orders with their token. User
// sign-in (authorization code with PKCE) is IdentityHub work that does not exist yet (v2 §10.1).
builder.AddClient("storefront", client =>
{
    client.Audiences.Add("api://bookstore/users");
    client.Audiences.Add("api://bookstore/orders");
});

// How IdentityHub uses its store: client-side behaviour, which only this process reads, in every placement. No
// Database type is visible, and nothing here creates, removes or places the database. Where the data lives is said in
// the identity AppHost. It declares identity-db and names it in the hub's Store block, so the database's settings are
// spec on that declaration, and its program is BookStore.Identity.Database. With no database named there, the hub
// would get a private one, part of the IdentityHub type, with its settings in that Store block (v2 §8.5 and §8.6).
builder.UseSqlDatabase(store =>
{
    store.StatementTimeout = TimeSpan.FromSeconds(15);
    store.PoolSize = 32;
    store.DeletedUserRetention = TimeSpan.FromDays(30);
});

await using IdentityHubApplication application = builder.Build();
await application.RunAsync();
