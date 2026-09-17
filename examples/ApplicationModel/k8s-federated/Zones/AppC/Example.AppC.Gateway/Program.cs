using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ConfigurationStore.ApplicationModel;
using Assimalign.Cohesion.Database.ApplicationModel;
using Assimalign.Cohesion.SecretStore.ApplicationModel;
using Assimalign.Cohesion.Web.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);

// AppC consumes this boundary as Externals.PlatformConfigurationStore.
// Command-line and environment bindings override the standalone Local endpoint fallback.
IConfigurationStoreResourceDescriptor config = builder.RemoteReferenceConfigurationStore(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
builder.RemoteReference(Externals.IdentityHub, remote => { });

using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "Configuration", "appc.json")));
var seed = new Dictionary<string, string?>();
foreach (JsonProperty section in configuration.RootElement.EnumerateObject())
{
    foreach (JsonProperty setting in section.Value.EnumerateObject())
    {
        seed.Add($"{section.Name}:{setting.Name}", setting.Value.ValueKind == JsonValueKind.String
            ? setting.Value.GetString() : setting.Value.GetRawText());
    }
}
config.AddNamespace("appc", seed);

ISecretStoreResourceDescriptor secrets = builder.AddAppCSecretStore()
    .IssueCertificate("appc-api", subject: "CN=appc-api", subjectAlternativeNames: ["localhost", "127.0.0.1"]);
// The schema-owned inventory database cannot be claimed by a command; provision a separate archive.
IDatabaseResourceDescriptor database = builder.AddAppCDatabase(options => options.Storage.Size = "20Gi")
    .AddDatabase("inventory-archive", engine: "appc-sql")
    .DependsOn(secrets);
IWebResourceDescriptor api = builder.AddAppCApi().DependsOn(database, secrets);
IWebResourceDescriptor spa = builder.AddAppCSpa().DependsOn(api);

// §4.3 assigns IdentityHub and Rezolvr commands to this consumer. Their ApplicationModel
// packages ship no typed external binders, so their owning gateways carry those declarations.
builder.UseGateway(args);
await builder.Build().RunAsync();
