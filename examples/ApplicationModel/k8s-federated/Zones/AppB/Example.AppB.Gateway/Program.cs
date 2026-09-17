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

// AppB consumes this boundary as Externals.PlatformConfigurationStore.
// Command-line and environment bindings override the standalone Local endpoint fallback.
IConfigurationStoreResourceDescriptor config = builder.RemoteReferenceConfigurationStore(
    Externals.PlatformConfigurationStore,
    remote => remote.Endpoint("api", "https://localhost:18443"));
builder.RemoteReference(Externals.IdentityHub, remote => { });

using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "Configuration", "appb.json")));
var seed = new Dictionary<string, string?>();
foreach (JsonProperty section in configuration.RootElement.EnumerateObject())
{
    foreach (JsonProperty setting in section.Value.EnumerateObject())
    {
        seed.Add($"{section.Name}:{setting.Name}", setting.Value.ValueKind == JsonValueKind.String
            ? setting.Value.GetString() : setting.Value.GetRawText());
    }
}
config.AddNamespace("appb", seed);

ISecretStoreResourceDescriptor secrets = builder.AddAppBSecretStore()
    .IssueCertificate("appb-api", subject: "CN=appb-api", subjectAlternativeNames: ["localhost", "127.0.0.1"]);
// The schema-owned billing database cannot be claimed by a command; provision a separate archive.
IDatabaseResourceDescriptor database = builder.AddAppBDatabase(options => options.Storage.Size = "20Gi")
    .AddDatabase("billing-archive", engine: "appb-sql")
    .DependsOn(secrets);
IWebResourceDescriptor api = builder.AddAppBApi().DependsOn(database, secrets);
IWebResourceDescriptor spa = builder.AddAppBSpa().DependsOn(api);

// §4.3 assigns IdentityHub and Rezolvr commands to this consumer. Their ApplicationModel
// packages ship no typed external binders, so their owning gateways carry those declarations.
builder.UseGateway(args);
await builder.Build().RunAsync();
