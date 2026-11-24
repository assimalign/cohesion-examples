// The ResourceManager for BookStore's environments: one per environment, staging and prod, both on Kubernetes.
//
// Why a program of its own rather than the stock image (v2 decision 6): the stock Kubernetes image carries every
// Cohesion provider. BookStore builds this one so the catalog is exactly what its AppHosts use: the eight area
// providers below, on top of the always-on Workloads, Networking and Storage. Fewer types to admit, fewer apiVersions
// to carry through upgrades. Everything else would be identical, so a team without that need runs the stock image and
// has no project here.
//
// Two ways this program runs, from the same image:
//
//   serve   The RM workload, a singleton the system layer deploys: management routes, the agent routes, IdentityHub
//           under /identity, the store, the reconcilers. It never deploys, scales or upgrades itself, and opens its
//           store in Verify mode, refusing a schema it did not expect (v2 §9.7).
//
//   system  On an operator's machine or in CI, once per command. CreateBuilder sees args[0] == "system" and returns a
//           system runner that reads only the composition below; Build() never runs, so nothing touches files or needs
//           secrets that exist only on the target (v2 §9.3). The `cohesion system ...` CLI forwards here:
//
//             dotnet run --project ResourceManager/BookStore.ResourceManager/src -- \
//                 system install --environment prod --file environments/prod.system.jsonc
//             dotnet run --project ResourceManager/BookStore.ResourceManager/src -- \
//                 system upgrade --environment prod --file environments/prod.system.jsonc --image <digest>
//             dotnet run --project ResourceManager/BookStore.ResourceManager/src -- \
//                 system status --environment prod
//
//           pipelines/system.yml runs the second; status works with the RM down, because the system record lives on
//           the platform, outside the RM's store (v2 §9.4).

using Assimalign.Cohesion.ConfigurationStore.ResourceProvider;
using Assimalign.Cohesion.Database.ResourceProvider;
using Assimalign.Cohesion.Fabric.Kubernetes.Drivers;
using Assimalign.Cohesion.IdentityHub.ResourceProvider;
using Assimalign.Cohesion.LogSpace.ResourceProvider;
using Assimalign.Cohesion.MessageHub.ResourceProvider;
using Assimalign.Cohesion.ResourceManager.Hosting;
using Assimalign.Cohesion.Rezolvr.ResourceProvider;
using Assimalign.Cohesion.VpnGateway.ResourceProvider;
using Assimalign.Cohesion.Web.ResourceProvider;

ResourceManagerApplicationBuilder builder = ResourceManagerApplication.CreateBuilder(args);

// One platform, a compile-time reference: a component integration that registers the Kubernetes FabricPlatform. Its
// drivers apply with one field manager per resource (cohesion/<uid>), select pods by the uid label, and impersonate
// each application's ServiceAccount for raw manifests, so the cluster's own RBAC bounds app.Kubernetes.AddManifest
// too (v2 §7.6).
builder.Services.AddKubernetesPlatform(kubernetes => kubernetes.InCluster());

// The area providers this RM serves, one for every section BookStore's AppHosts declare through. Each one is spec
// types, validators and a pure Plan, the same code the AppHosts run at emit, so a document that emits cleanly is very
// likely to admit.
//
// [proposed] The last six. v2 as it stood had providers for IdentityHub and Database only and ran every other program
// as a plain workload. The sections surface gives every area a resource type that predefines its workload, so an RM
// must be built with the provider of each type it admits. These six packages and their Add verbs do not exist yet.
builder.Providers
    .AddIdentityHub()
    .AddDatabase()
    .AddWeb()
    .AddLogSpace()
    .AddConfigurationStore()
    .AddMessageHub()
    .AddRezolvr()
    .AddVpnGateway();

// [proposed] No System(...) call. v2 §5.1 sets the endpoint, TLS, store size and backups here; in this example they
// come from environments/<env>.system.jsonc at install and upgrade, because one image serves two installations. A
// program may still set defaults here, and the file wins.

await using ResourceManagerApplication application = builder.Build();
await application.RunAsync();
