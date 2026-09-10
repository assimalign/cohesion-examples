# cohesion-examples

Reference scaffolds for organizations building on Cohesion, materialized from
[`docs/DEVELOPER_EXPERIENCE_DESIGN.md`](https://github.com/assimalign/cohesion/blob/main/docs/DEVELOPER_EXPERIENCE_DESIGN.md)
(the direction of record). Every deployable thing is an ordinary .NET executable that picks a Cohesion SDK; every
relationship is an ordinary project or package reference; a gateway project realizes the graph on one process, N
processes, Docker, Kubernetes or several clusters without changing application code.

| Scaffold | Shape | What it shows |
| --- | --- | --- |
| [`examples/single-app`](examples/single-app/) | one repo, one gateway, two resources | the two-person-company story: `dotnet run` = one process, `--gateway docker` = two containers |
| [`examples/k8s`](examples/k8s/) | mono-repo, one cluster | the landing-zone taxonomy (Identity / Networking / Platform / Zones) with one root application set as the production owner and per-area gateways for "pull one area down" |
| [`examples/k8s-federated`](examples/k8s-federated/) | mono-repo, one cluster per area | the same projects, no root gateway; areas reference each other through their gateways' control planes (`builder.RemoteReference(...)`) |

Conventions shared by all three: `global.json` pins the .NET SDK and all 20 Cohesion SDKs; `nuget.config` carries no
credentials (guarded by CI); the root `Directory.Build.props` carries identity only; each area folder sets
`CohesionApplication` once. **Every project is a `Program.cs`-only executable** — a database defines its schema,
triggers, functions and custom types in C#; the identity hub composes its user-flow pipelines; the DNS server declares
its zones; a secret store its policies — each through its area's builder (the builder APIs in these files are
illustrative; each area owns its own). **Orchestration is an opt-in**: `<CohesionApplicationModel>enabled</CohesionApplicationModel>`
in a csproj generates the manifest, `Resource.g.cs` (typed accessors for the resource's own endpoints, mounts, settings
and references) and `ResourceControlPlane.g.cs` (the area-owned default control plane a gateway talks to); a plain
executable without it cannot be referenced. Every resource in these scaffolds is enabled because every one is referenced.
The csproj otherwise carries only what the gateway needs to know: name, endpoints, probes, mounts, settings, references.
A reference also accepts **declarative commands** (`identity.AddAudience(...)`, `config.AddNamespace(...)`,
`dns.AddARecord(...)`) delivered to the target's control plane once it is running — each area owns its entire
dependency tree; a remote reference only has to be there.

```bash
# run a zone locally (N supervised processes), then as one process, then as containers
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway docker

# deploy the whole single-cluster architecture from the root application set (CI/GitOps form)
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --gateway kubernetes --context cluster-01 --mode apply
```
