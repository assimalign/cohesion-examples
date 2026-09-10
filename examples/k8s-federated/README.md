# `k8s-federated/` — one cluster per area

The same projects as `k8s/`, minus the root `Gateway/`: each area owns its own Kubernetes gateway and cluster, and areas
reference each other through their gateways' control planes with `builder.RemoteReference(...)`. Nothing is listed
twice: a resource's csproj references the remote resource's project (or its `*.Manifest` package), the build generates
`Externals.<Name>` for the crossing, and the gateway binds it to the peer control plane. Each gateway publishes its own
control plane at `Cohesion:ControlPlane:PublicUrl` (see its `appsettings.json`).

| Area | Cluster | Application / namespace | Gateway | Control plane | Binds to |
| --- | --- | --- | --- | --- | --- |
| Platform | `cluster-03` | `platform` | `Example.Platform.Gateway` | `https://platform.example.com/cohesion` | — (deployed first; root of trust; holds trust grants) |
| Identity | `cluster-01` | `identity` | `Example.Identity.Gateway` | `https://identity.example.com/cohesion` | platform |
| Networking | `cluster-02` | `networking` | `Example.Networking.Gateway` | `https://networking.example.com/cohesion` | platform |
| Zones | `cluster-04` | `appa`, `appb`, `appc` | `Example.AppA.Gateway`, `Example.AppB.Gateway`, `Example.AppC.Gateway` | `https://appa.example.com/cohesion`, … | identity, platform |

Trust is explicit and per direction: the application that must **verify** a peer registers that peer's public trust key,
together with the **command kinds** that peer may issue against it (`--allow`); commands from a peer are otherwise refused.
Platform verifies everyone (mount sources, intermediate-CA enrollment); Identity verifies the zones (client and token
requests); every application verifies Platform (bootstrap material). Inter-application traffic uses the peers' public
endpoints over TLS from the org CA.

```bash
# 1. Platform — root of trust
dotnet run --project examples/k8s-federated/Platform/Example.Platform.Gateway -- --gateway kubernetes --context cluster-03 --mode apply

# 2. Identity (needs Platform to trust it, and to trust Platform)
cohesion trust add identity --from https://identity.example.com/cohesion --against https://platform.example.com/cohesion --allow secretstore.enroll
cohesion trust add platform --from https://platform.example.com/cohesion --against https://identity.example.com/cohesion
dotnet run --project examples/k8s-federated/Identity/Example.Identity.Gateway -- --gateway kubernetes --context cluster-01 --mode apply

# 3. Networking
cohesion trust add networking --from https://networking.example.com/cohesion --against https://platform.example.com/cohesion --allow secretstore.enroll
cohesion trust add platform   --from https://platform.example.com/cohesion   --against https://networking.example.com/cohesion
dotnet run --project examples/k8s-federated/Networking/Example.Networking.Gateway -- --gateway kubernetes --context cluster-02 --mode apply

# 4. A zone (Platform, Identity and Networking must trust it for the commands it issues; it must trust Platform)
cohesion trust add appa     --from https://appa.example.com/cohesion     --against https://platform.example.com/cohesion   --allow secretstore.enroll,configurationstore.namespace
cohesion trust add appa     --from https://appa.example.com/cohesion     --against https://identity.example.com/cohesion   --allow identityhub.audience,identityhub.client
cohesion trust add appa     --from https://appa.example.com/cohesion     --against https://networking.example.com/cohesion --allow rezolvr.record
cohesion trust add platform --from https://platform.example.com/cohesion --against https://appa.example.com/cohesion
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway kubernetes --context cluster-04 --mode apply

# pull the AppA zone down onto a laptop while identity and platform stay remote (the RemoteReference bindings in Program.cs apply)
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway
```
