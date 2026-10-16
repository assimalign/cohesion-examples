using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);

// AppA consumes this boundary as Externals.PlatformConfigurationStore.
// Command-line and environment bindings override the standalone Local endpoint fallback.
IConfigurationStoreResourceDescriptor config = builder.RemoteReferenceConfigurationStore(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
builder.RemoteReference(Externals.IdentityHub, remote => { });

using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "Configuration", "appa.json")));
var seed = new Dictionary<string, string?>();
foreach (JsonProperty section in configuration.RootElement.EnumerateObject())
{
    foreach (JsonProperty setting in section.Value.EnumerateObject())
    {
        seed.Add($"{section.Name}:{setting.Name}", setting.Value.ValueKind == JsonValueKind.String
            ? setting.Value.GetString() : setting.Value.GetRawText());
    }
}
config.AddNamespace("appa", seed);

ISecretStoreResourceDescriptor secrets = builder.AddSecretStore(Manifests.AppASecretStore)
    .IssueCertificate("appa-api", subject: "CN=appa-api", subjectAlternativeNames: ["localhost", "127.0.0.1"]);
// Providers are explicit. The zone SecretStore resolves appa-secretstore:<key> mounts (the API's
// TLS leaf), issues the leaves of endpoints whose certificate mount has no source, and persists
// peer trust grants. The platform ConfigurationStore stays a remote reference, never a source.
builder.UseSecretStore(secrets)
    .AsCertificateAuthority()
    .AsTrustStore();
// The schema-owned orders database cannot be claimed by a command; provision a separate archive.
IDatabaseResourceDescriptor database = builder.AddDatabase(
        Manifests.AppADatabase,
        new DatabaseResourceOptions { Storage = { Size = "20Gi" } })
    .AddDatabase("orders-archive", engine: "appa-sql")
    .DependsOn(secrets);
IWebResourceDescriptor api = builder.AddWeb(Manifests.AppAApi).DependsOn(database, secrets);
IWebResourceDescriptor spa = builder.AddWeb(Manifests.AppASpa).DependsOn(api);

// §4.3 assigns IdentityHub and Rezolvr commands to this consumer. Their ApplicationModel
// packages ship no typed external binders, so their owning gateways carry those declarations.
builder.UseGateway(args);
await builder.Build().RunAsync();
