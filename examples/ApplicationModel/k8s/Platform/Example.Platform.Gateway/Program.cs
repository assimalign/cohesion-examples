using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ConfigurationStore.ApplicationModel;
using Assimalign.Cohesion.SecretStore.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
ISecretStoreResourceDescriptor secrets = builder.AddPlatformSecretStore()
    .IssueCertificate("platform-configuration-store", "CN=platform-configuration-store",
        subjectAlternativeNames: ["localhost", "127.0.0.1"]);
// LogSpace's DependsOn returns the base descriptor. ConfigurationStore starts after the sink;
// SecretStore bootstraps it first and therefore retains console logging.
IApplicationResourceDescriptor logs = builder.AddPlatformLogSpace().DependsOn(secrets);
IConfigurationStoreResourceDescriptor config = builder.AddPlatformConfigurationStore().DependsOn(secrets, logs);

using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "Configuration", "shared.json")));
var seed = new Dictionary<string, string?>();
foreach (JsonProperty setting in configuration.RootElement.EnumerateObject())
{
    if (setting.Value.ValueKind == JsonValueKind.Object)
    {
        foreach (JsonProperty child in setting.Value.EnumerateObject())
            seed.Add($"{setting.Name}:{child.Name}", child.Value.ToString());
    }
    else
    {
        seed.Add(setting.Name, setting.Value.GetString());
    }
}
// Declare shared exactly once. The resource hosts networking; zones own their namespaces.
config.AddNamespace("shared", seed);

builder.UseGateway(args);
await builder.Build().RunAsync();
