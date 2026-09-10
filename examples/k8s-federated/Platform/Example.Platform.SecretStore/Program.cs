using Assimalign.Cohesion.SecretStore;
using Assimalign.Cohesion.SecretStore.Hosting;
using Example.Platform.SecretStore;

// The platform-secretstore resource: the organization's root of trust. The SecretStore builder API used here is illustrative — the SecretStore area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
SecretStoreApplicationBuilder builder = SecretStoreApplication.CreateBuilder(args);

builder.AddStore(store => store.UseEmbeddedDatabase(Resource.Mounts.Data));

builder.AddCertificateAuthority(ca =>
{
    ca.Root("Example Org CA", lifetime: TimeSpan.FromDays(3650));   // self-seeded on first start; its own `api` leaf is issued from it
    ca.IssueIntermediates(to: application => application.TrustedFor("secretstore.enroll"));   // zone SecretStores enroll through the trust grant
});

builder.AddTrustedIssuers(issuers => issuers.PersistIn(store: "trust"));   // one grant per peer application (cohesion trust add), with allowed command kinds

builder.AddPolicy("certs/*",      policy => policy.IssueLeaf(subject: key => $"{key}.example.com", lifetime: TimeSpan.FromDays(90)));
builder.AddPolicy("identity/*",   policy => policy.ReadableBy(application: "identity").RotateEvery(TimeSpan.FromDays(30)));
builder.AddPolicy("networking/*", policy => policy.ReadableBy(application: "networking"));

builder.AddApi(Resource.Endpoints.Api);
await builder.Build().RunAsync();
