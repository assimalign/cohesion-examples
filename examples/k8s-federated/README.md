# `k8s-federated/` — one cluster per area

The same projects as `k8s/`, minus the root `Gateway/`: each area owns its application boundary. In the target topology,
areas resolve one another through their gateways' control planes. Nothing is listed twice: a resource's csproj references
the remote resource's project (or its `*.Manifest` package), and the build generates `Externals.<Name>` for the crossing.
The pinned package set has no gateway control-plane host yet, so current Programs use static localhost development
bindings where an endpoint is required and leave optional Identity unbound.

The table is the target multi-cluster placement. The checked-in `10.0.1-preview.3.local` gateway projects select Local,
plus InProcess for composable applications; Networking remains Local because its VPN data plane is not composable. The
currently runnable subset is each zone's Database/API/SPA chain. Kubernetes deployment and the `cohesion trust` CLI
commands arrive with their separate provider and tooling deliverables.

| Area | Cluster | Application / namespace | Gateway | Control plane | Binds to |
| --- | --- | --- | --- | --- | --- |
| Platform | `cluster-03` | `platform` | `Example.Platform.Gateway` | `https://platform.example.com/cohesion` | — (deployed first; root of trust; holds trust grants) |
| Identity | `cluster-01` | `identity` | `Example.Identity.Gateway` | `https://identity.example.com/cohesion` | platform |
| Networking | `cluster-02` | `networking` | `Example.Networking.Gateway` | `https://networking.example.com/cohesion` | platform |
| Zones | `cluster-04` | `appa`, `appb`, `appc` | `Example.AppA.Gateway`, `Example.AppB.Gateway`, `Example.AppC.Gateway` | `https://appa.example.com/cohesion`, … | identity, platform |

In that target topology, trust is explicit and per direction: the application that must **verify** a peer registers that peer's public trust key,
together with the **command kinds** that peer may issue against it (`--allow`); commands from a peer are otherwise refused.
Platform verifies everyone (mount sources, intermediate-CA enrollment); Identity verifies the zones (client and token
requests); every application verifies Platform (bootstrap material). Inter-application traffic uses the peers' public
endpoints over TLS from the org CA.

```bash
# generic area manifests can be inspected while their runtime control planes remain upstream work
dotnet run --project examples/k8s-federated/Platform/Example.Platform.Gateway -- --gateway local --mode describe
dotnet run --project examples/k8s-federated/Networking/Example.Networking.Gateway -- --gateway local --mode describe

# implemented zone subset
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode run
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess --mode run
```
