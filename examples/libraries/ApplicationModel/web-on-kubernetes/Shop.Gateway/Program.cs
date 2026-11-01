// The shop's gateway: it declares the application, two Web resources, and runs them on a Kubernetes
// cluster through the cluster's API server.
//
// orders-api runs two instances; storefront runs one, and its Orders:Url setting is orders-api's
// endpoint, which the platform's Service gives it. The gateway deploys orders-api first, waits for an
// instance to publish, reads its endpoint, then deploys storefront with it. Each resource's image is
// one of this example's Web programs, published to the registry the cluster pulls from
// (publish-images.ps1).
//
//   dotnet run --project Shop.Gateway                                  (the current kubeconfig context)
//   dotnet run --project Shop.Gateway -- --context kind-cohesion --registry localhost:5001
//
// Ctrl+C stops the gateway. What it deployed keeps running; running it again adopts it. The gateway
// compares image references, not the images they name, so after republishing the same :latest tags
// it leaves the running pods alone: restart them (README.md).

using System;

using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ApplicationModel.Gateway;
using Assimalign.Cohesion.ApplicationModel.Gateway.Kubernetes;

string? context = GetOption(args, "--context");
string registry = GetOption(args, "--registry") ?? "localhost:5001";
string ns = GetOption(args, "--namespace") ?? "shop";

await using KubernetesInfrastructure kubernetes = KubernetesInfrastructure.Create(new KubernetesInfrastructureOptions
{
    Context = context,
    Namespace = ns,
    // The example republishes its images under one tag, so a new pod pulls the latest push. Only a new
    // pod pulls: an unchanged tag never makes the gateway replace the pods it adopts.
    ImagePullPolicy = "Always",
});
kubernetes
    .AddImage("Web", "orders-api", $"{registry}/shop-orders:latest")
    .AddImage("Web", "storefront", $"{registry}/shop-storefront:latest");

ApplicationGatewayBuilder builder = ApplicationGateway.CreateBuilder("shop");
builder.StartupTimeout = TimeSpan.FromMinutes(3);
builder.UseKubernetes(kubernetes);

IApplicationResourceDefinition orders = builder.AddResource("Web", "orders-api").WithReplicas(2);
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

Console.WriteLine($"The shop runs in namespace '{kubernetes.Namespace}':");
foreach (IApplicationGatewayDeployment resource in gateway.Resources)
{
    string endpoint = resource.Outputs.TryGetValue(OutputName.Endpoint, out string? value) ? value : "(no endpoint)";
    Console.WriteLine($"  {resource.Name,-12} {endpoint}  {resource.Instances.Count} instance(s) published");
}

Console.WriteLine($"Try: kubectl --namespace {kubernetes.Namespace} port-forward service/storefront 8080:80, then curl http://localhost:8080");
Console.WriteLine("Ctrl+C stops the gateway; the shop keeps running.");
await gateway.Completion;

static string? GetOption(string[] args, string name)
{
    int index = Array.IndexOf(args, name);
    return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
