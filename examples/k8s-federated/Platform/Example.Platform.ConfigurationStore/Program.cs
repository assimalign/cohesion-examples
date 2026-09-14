using System.IO;
using System.Text.Json;

using Assimalign.Cohesion.ConfigurationStore;
using Assimalign.Cohesion.ConfigurationStore.Hosting;
using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.Hosting.Resources;

IConfigurationStoreApplicationBuilder builder = ConfigurationStoreApplication.CreateBuilder(args);
using JsonDocument configuration = JsonDocument.Parse(File.ReadAllText(
    Path.Combine(ResourceRuntime.Current.ContentRootPath, "Configuration", "networking.json")));
// The shared namespace is a gateway-owned command. Claiming it here too would reject that command.
// Host-owned networking entries come from the existing seed; configuration keys use ':' rather than '/'.
builder.AddNamespace("networking", ns =>
{
    foreach (JsonProperty setting in configuration.RootElement.GetProperty("Rezolvr").EnumerateObject())
    {
        if (setting.Value.ValueKind == JsonValueKind.Array)
        {
            int index = 0;
            foreach (JsonElement forwarder in setting.Value.EnumerateArray())
                ns.Set($"Rezolvr:{setting.Name}:{index++}", forwarder.GetString());
        }
        else
        {
            ns.Set($"Rezolvr:{setting.Name}", setting.Value.GetString());
        }
    }
});

await builder.Build().RunAsync();
