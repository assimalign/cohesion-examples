// The shop's gateway: it declares the application, two Web resources, and runs them as containers on
// a Docker-compatible engine, Podman's included, on this machine, or, with --in-process, inside this
// process.
//
// orders-api runs two instances on the engine; storefront runs one, and its Orders:Url setting is
// orders-api's endpoint, its name on the gateway's network. The gateway deploys orders-api first,
// waits for an instance to publish, reads its endpoint, then deploys storefront with it. On the
// engine, each resource's image is one of this example's Web programs, loaded into the engine's image
// store (publish-images.ps1). In process, each resource is the program this project references
// (Shop.Gateway.csproj), and runs once, on a loopback port.
//
//   dotnet run --project Shop.Gateway -- --in-process                         (no engine: both programs run here)
//   dotnet run --project Shop.Gateway                                        (DOCKER_HOST, else the platform's default engine)
//   dotnet run --project Shop.Gateway -- --endpoint npipe:////./pipe/docker_engine --network shop
//
// Ctrl+C stops the gateway. In process, it stops both programs with it. On the engine, what it
// deployed keeps running, and running it again adopts it. The gateway compares image references, not
// the images they name, so after republishing the same :latest tags remove the shop's containers
// before running it again (README.md).

using System;
using System.Linq;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ApplicationModel.Gateway;
using Assimalign.Cohesion.ApplicationModel.Gateway.Docker;
using Assimalign.Cohesion.ApplicationModel.Gateway.InProcess;

bool inProcess = args.Contains("--in-process");
string? endpoint = GetOption(args, "--endpoint");
string network = GetOption(args, "--network") ?? "shop";

ApplicationGatewayBuilder builder = ApplicationGateway.CreateBuilder("shop");
builder.StartupTimeout = TimeSpan.FromMinutes(2);

// The gateway builds the platform when it is built, and disposing the gateway disposes it.
if (inProcess)
{
    builder.UseInProcess();
}
else
{
    builder.UseDocker(docker =>
    {
        docker.Options.Endpoint = endpoint;
        docker.Options.Network = network;
        // The images exist only in the local engine's store, so the engine never pulls them.
        docker.Options.ImagePullPolicy = "Never";
        docker
            .AddImage("Web", "orders-api", "shop-orders:latest")
            .AddImage("Web", "storefront", "shop-storefront:latest");
    });
}

// WithDockerReplicas is the Docker platform's setting; in process, every resource runs once.
IApplicationResourceDefinition orders = builder.AddResource("Web", "orders-api").WithDockerReplicas(2);
builder.AddResource("Web", "storefront").WithSetting("Orders:Url", orders.Endpoint);

// StartAsync completes once every resource has published; Completion completes when the run ends,
// on Ctrl+C, and faults with the run's failure. Ctrl+C during startup cancels StartAsync and ends the
// run quietly.
await using ApplicationGateway gateway = builder.Build();
try
{
    await gateway.StartAsync();
}
catch (OperationCanceledException)
{
    await gateway.Completion;
    return;
}

// Without --endpoint, the platform resolved the engine as the docker CLI does: DOCKER_HOST, then the
// platform's default.
string engine = endpoint ?? Environment.GetEnvironmentVariable("DOCKER_HOST") ?? "the platform's default endpoint";
Console.WriteLine(inProcess ? "The shop runs in this process:" : $"The shop runs on network '{network}' of the engine at {engine}:");
foreach (IApplicationGatewayDeployment resource in gateway.Resources)
{
    string address = resource.Outputs.TryGetValue(OutputName.Endpoint, out string? value) ? value : "(no endpoint)";
    Console.WriteLine($"  {resource.Name,-12} {address}  {resource.Instances.Count} instance(s) published");
}

if (inProcess)
{
    IApplicationGatewayDeployment storefront = gateway.Resources.Single(resource => resource.Name.Value == "storefront");
    Console.WriteLine($"Try: curl {storefront.Outputs[OutputName.Endpoint]}");
    Console.WriteLine("Ctrl+C stops the gateway and both programs.");
}
else
{
    // Each endpoint is the resource's name on the network. The engine also publishes every container's
    // port on a loopback port of this machine, which the engine's ps command shows (podman's and docker's
    // take the same arguments).
    Console.WriteLine($"Try: podman ps --filter label=com.assimalign.cohesion.network={network} --format \"{{{{.ID}}}} {{{{.Names}}}} {{{{.Ports}}}}\"  (or docker ps, the same arguments)");
    Console.WriteLine("     then curl the 127.0.0.1 port the storefront container's 8080 is published on.");
    Console.WriteLine("Ctrl+C stops the gateway; the shop keeps running.");
}

await gateway.Completion;

static string? GetOption(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
