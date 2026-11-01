// The shop's gateway: it declares the application, two Web resources, and runs them on a Kubernetes
// cluster through the cluster's API server, or, with --in-process, inside this process.
//
// orders-api runs two instances on the cluster; storefront runs one, and its Orders:Url setting is
// orders-api's endpoint, which the platform's Service gives it. The gateway deploys orders-api first,
// waits for an instance to publish, reads its endpoint, then deploys storefront with it. On the
// cluster, each resource's image is one of this example's Web programs, published to the registry the
// cluster pulls from (publish-images.ps1). In process, each resource is the program this project
// references (Shop.Gateway.csproj), and runs once, on a loopback port.
//
//   dotnet run --project Shop.Gateway -- --in-process                   (no cluster: both programs run here)
//   dotnet run --project Shop.Gateway                                  (the current kubeconfig context)
//   dotnet run --project Shop.Gateway -- --context kind-cohesion --registry localhost:5001
//
// Ctrl+C stops the gateway. In process, it stops both programs with it. On the cluster, what it
// deployed keeps running, and running it again adopts it. The gateway compares image references, not
// the images they name, so after republishing the same :latest tags it leaves the running pods alone:
// restart them (README.md).

using System;
using System.Linq;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ApplicationModel.Gateway;
using Assimalign.Cohesion.ApplicationModel.Gateway.InProcess;
using Assimalign.Cohesion.ApplicationModel.Gateway.Kubernetes;

bool inProcess = args.Contains("--in-process");
string? context = GetOption(args, "--context");
string registry = GetOption(args, "--registry") ?? "localhost:5001";
string ns = GetOption(args, "--namespace") ?? "shop";

ApplicationGatewayBuilder builder = ApplicationGateway.CreateBuilder("shop");
builder.StartupTimeout = TimeSpan.FromMinutes(3);

// The gateway builds the platform when it is built, and disposing the gateway disposes it.
if (inProcess)
{
    builder.UseInProcess();
}
else
{
    builder.UseKubernetes(kubernetes =>
    {
        kubernetes.Options.Context = context;
        kubernetes.Options.Namespace = ns;
        // The example republishes its images under one tag, so a new pod pulls the latest push. Only a
        // new pod pulls: an unchanged tag never makes the gateway replace the pods it adopts.
        kubernetes.Options.ImagePullPolicy = "Always";
        kubernetes
            .AddImage("Web", "orders-api", $"{registry}/shop-orders:latest")
            .AddImage("Web", "storefront", $"{registry}/shop-storefront:latest");
    });
}

// WithReplicas is the Kubernetes platform's setting; in process, every resource runs once.
IApplicationResourceDefinition orders = builder.AddResource("Web", "orders-api").WithReplicas(2);
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

Console.WriteLine(inProcess ? "The shop runs in this process:" : $"The shop runs in namespace '{ns}':");
foreach (IApplicationGatewayDeployment resource in gateway.Resources)
{
    string endpoint = resource.Outputs.TryGetValue(OutputName.Endpoint, out string? value) ? value : "(no endpoint)";
    Console.WriteLine($"  {resource.Name,-12} {endpoint}  {resource.Instances.Count} instance(s) published");
}

if (inProcess)
{
    IApplicationGatewayDeployment storefront = gateway.Resources.Single(resource => resource.Name.Value == "storefront");
    Console.WriteLine($"Try: curl {storefront.Outputs[OutputName.Endpoint]}");
    Console.WriteLine("Ctrl+C stops the gateway and both programs.");
}
else
{
    Console.WriteLine($"Try: kubectl --namespace {ns} port-forward service/storefront 8080:80, then curl http://localhost:8080");
    Console.WriteLine("Ctrl+C stops the gateway; the shop keeps running.");
}

await gateway.Completion;

static string? GetOption(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
