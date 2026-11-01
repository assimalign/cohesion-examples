// The shop's gateway: it declares the application, two Web resources, and runs them as containers on
// a Docker-compatible engine, Podman's included, on this machine.
//
// orders-api runs two instances; storefront runs one, and its Orders:Url setting is orders-api's
// endpoint, its name on the gateway's network. The gateway deploys orders-api first, waits for an
// instance to publish, reads its endpoint, then deploys storefront with it. Each resource's image is
// one of this example's Web programs, loaded into the engine's image store (publish-images.ps1).
//
//   dotnet run --project Shop.Gateway                                        (DOCKER_HOST, else the platform's default engine)
//   dotnet run --project Shop.Gateway -- --endpoint npipe:////./pipe/docker_engine --network shop
//
// Ctrl+C stops the gateway. What it deployed keeps running; running it again adopts it. The gateway
// compares image references, not the images they name, so after republishing the same :latest tags
// remove the shop's containers before running it again (README.md).

using System;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ApplicationModel.Gateway;
using Assimalign.Cohesion.ApplicationModel.Gateway.Docker;

string? endpoint = GetOption(args, "--endpoint");
string network = GetOption(args, "--network") ?? "shop";

await using DockerInfrastructure docker = DockerInfrastructure.Create(new DockerInfrastructureOptions
{
    Endpoint = endpoint,
    Network = network,
    // The images exist only in the local engine's store, so the engine never pulls them.
    ImagePullPolicy = "Never",
});
docker
    .AddImage("Web", "orders-api", "shop-orders:latest")
    .AddImage("Web", "storefront", "shop-storefront:latest");

ApplicationGatewayBuilder builder = ApplicationGateway.CreateBuilder("shop");
builder.StartupTimeout = TimeSpan.FromMinutes(2);
builder.UseDocker(docker);

IApplicationResourceDefinition orders = builder.AddResource("Web", "orders-api").WithDockerReplicas(2);
builder.AddResource("Web", "storefront").WithSetting("Orders:Url", orders.Endpoint);

// StartAsync completes once every resource has published; Completion completes when the run ends,
// on Ctrl+C, and faults with the run's failure. Ctrl+C during startup cancels StartAsync and ends the
// run quietly.
ApplicationGateway gateway = builder.Build();
try
{
    await gateway.StartAsync();
}
catch (OperationCanceledException)
{
    await gateway.Completion;
    return;
}

Console.WriteLine($"The shop runs on network '{docker.Network}' of the engine at {docker.Endpoint}:");
foreach (IApplicationGatewayDeployment resource in gateway.Resources)
{
    string address = resource.Outputs.TryGetValue(OutputName.Endpoint, out string? value) ? value : "(no endpoint)";
    Console.WriteLine($"  {resource.Name,-12} {address}  {resource.Instances.Count} instance(s) published");
}

// Each endpoint is the resource's name on the network. The engine also publishes every container's
// port on a loopback port of this machine, which the engine's ps command shows (podman's and docker's
// take the same arguments).
Console.WriteLine($"Try: podman ps --filter label=com.assimalign.cohesion.network={docker.Network} --format \"{{{{.ID}}}} {{{{.Names}}}} {{{{.Ports}}}}\"  (or docker ps, the same arguments)");
Console.WriteLine("     then curl the 127.0.0.1 port the storefront container's 8080 is published on.");
Console.WriteLine("Ctrl+C stops the gateway; the shop keeps running.");
await gateway.Completion;

static string? GetOption(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
