using Assimalign.Cohesion.SecretStore;
using Assimalign.Cohesion.SecretStore.Hosting;
using Example.AppC.SecretStore;

// The appc-secretstore resource: this zone's secrets and leaf certificates. The SecretStore builder API used here is illustrative — the SecretStore area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
SecretStoreApplicationBuilder builder = SecretStoreApplication.CreateBuilder(args);

builder.AddStore(store => store.UseEmbeddedDatabase(Resource.Mounts.Data));

builder.AddCertificateAuthority(ca =>
{
    ca.IntermediateOf("platform-secretstore");   // enrolled on the first Kubernetes apply (cohesion trust add platform)
    if (builder.Environment.IsDevelopment())
    {
        ca.SelfSeed();                           // run alone: a development root, unless --external platform-secretstore=… is given
    }
});

builder.AddPolicy("certs/*", policy => policy.IssueLeaf(subject: key => $"{key}.appc.example.com", lifetime: TimeSpan.FromDays(30)));
builder.AddPolicy("*",       policy => policy.ReadableBy(application: "appc"));

builder.AddApi(Resource.Endpoints.Api);
await builder.Build().RunAsync();
