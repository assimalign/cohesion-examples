using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
ISecretStoreResourceDescriptor secrets = builder.AddSecretStore(Manifests.PlatformSecretStore)
    .IssueCertificate("platform-configuration-store", "CN=platform-configuration-store",
        subjectAlternativeNames: ["localhost", "127.0.0.1"]);
// Providers are explicit. The SecretStore resolves platform-secretstore:<key> mounts (the
// ConfigurationStore's TLS leaf), issues the leaves of endpoints whose certificate mount has no
// source (LogSpace's), and persists peer trust grants.
builder.UseSecretStore(secrets)
    .AsCertificateAuthority()
    .AsTrustStore();
// LogSpace's DependsOn returns the base descriptor. ConfigurationStore starts after the sink;
// SecretStore bootstraps it first and therefore retains console logging.
IApplicationResourceDescriptor logs = builder.AddLogSpace(Manifests.PlatformLogSpace).DependsOn(secrets);
builder.Providers.Telemetry = ResourceTelemetrySink.FromResource(logs);
IConfigurationStoreResourceDescriptor config = builder.AddConfigurationStore(Manifests.PlatformConfigurationStore)
    .DependsOn(secrets, logs);
// Resolves platform-configuration-store:<namespace> Configuration mounts of platform resources.
builder.UseConfigurationStore(config);

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
