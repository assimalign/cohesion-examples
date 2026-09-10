# `k8s/` — one cluster, one architecture

Every subfolder is a **domain** — a landing-zone area: one application whose services/resources make up its architecture; every domain declares its own `CohesionApplication`
(= Kubernetes namespace) once in its `Directory.Build.props`, and every zone app under `Zones/` is its own application.

| Area | Application / namespace | Projects | Gateway |
| --- | --- | --- | --- |
| `Platform/` | `platform` — deployed first, root of trust | `Example.Platform.SecretStore`, `Example.Platform.ConfigurationStore`, `Example.Platform.LogSpace` | `Example.Platform.Gateway` |
| `Identity/` | `identity` | `Example.Identity.IdentityHub` (user flows in `Flows/`) | `Example.Identity.Gateway` |
| `Networking/` | `networking` | `Example.Networking.Rezolvr` (DNS), `Example.Networking.VpnGateway` | `Example.Networking.Gateway` |
| `Zones/AppA` | `appa` | `Example.AppA.Api`, `Example.AppA.Spa`, `Example.AppA.Database` (schema in C#), `Example.AppA.SecretStore` (+ `Example.AppA.Worker.Manifest` from another repo) | `Example.AppA.Gateway` |
| `Zones/AppB` | `appb` | same shape (billing) | `Example.AppB.Gateway` |
| `Zones/AppC` | `appc` | same shape (inventory) | `Example.AppC.Gateway` |
| `Gateway/` | `cohesion-system` | — | `Example.Gateway` — the **application set**: one gateway instance over all six applications, the sole production owner |

Every project is an ordinary executable with a `Program.cs`: the database defines its tables, triggers, functions and
custom types in C#, the identity hub composes its user-flow pipelines, the DNS server declares its zone, the secret
stores their CAs and policies. Orchestration is the opt-in `<CohesionApplicationModel>enabled</CohesionApplicationModel>`
in each csproj (every resource here is referenced, so every one is enabled): it generates the manifest, the typed
`Resource.*` accessors, and the area-owned default control plane a gateway talks to. The csproj otherwise holds only
what the gateway needs to know.

All areas share one Kubernetes gateway in production: `Gateway/Example.Gateway` obtains every area gateway's model
through its control plane and reconciles them in one process (one client, one informer, one embedded registry),
applying namespaces `platform`, `identity`, `networking`, `appa`, `appb`, `appc`. The per-area gateways exist so an
area can be **pulled down and run alone** — locally, in one process, in Docker, or against a development namespace —
while referencing the remote parts. In this single-cluster tree those references bind through the cluster's namespaces
(`remote.Kubernetes("identity")`, the operator kubeconfig channel); `k8s-federated/` binds the same externals through
peer control planes (`remote.Gateway(url)`).

Folder nesting never expresses ownership: the earlier `Zones/AppB/Example.AppB.Gateway/Example.AppB.*` nesting and the
copy-pasted `AppC` tree are gone; a gateway owns exactly what it references.

Each area owns its entire dependency tree: a zone declares what it needs from Identity (its token audience and clients),
Platform (its configuration namespace) and, in development, Networking (a DNS record) as **commands** on those references
in its gateway's `Program.cs`; the zone's gateway delivers them to each target's default control plane once it is
running, and they leave with the zone. Identity and Platform list no zones anywhere.

```bash
# production: the gateway runs in-cluster (publish images, bootstrap once as cluster-admin, then it reconciles on its own)
dotnet publish examples/k8s/Gateway/Example.Gateway -t:CohesionPublishApplication
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --gateway kubernetes --context cluster-01 --mode bootstrap | kubectl apply -f -

# CI / GitOps alternative: reconcile from a kubeconfig and exit. A later in-cluster gateway has a different owner
# identity (cohesion.io/owner) and is refused until it is started with --adopt.
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --gateway kubernetes --context cluster-01 --mode apply

# one zone, locally, referencing identity and platform running in the cluster (Development → local gateway)
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway
# the same zone as one process, or as containers
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway docker
# run Identity locally too, from source. --realize is transitive for identity's own resources, but identity's crossing
# into platform stays external and needs a binding of its own:
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --realize identity-hub --external platform-secretstore=kubernetes:cluster-01/platform
# what a gateway would deploy, as a document (this is also how the root set reads it)
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --mode describe
```
