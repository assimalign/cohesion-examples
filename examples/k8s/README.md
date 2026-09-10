# `k8s/` — one cluster, one architecture

Every subfolder is a **domain** — a landing-zone area: one application whose services/resources make up its architecture; every domain declares its own `CohesionApplication`
(= Kubernetes namespace) once in its `Directory.Build.props`, and every zone app under `Zones/` is its own application.

| Area | Application / namespace | Projects | Gateway |
| --- | --- | --- | --- |
| `Platform/` | `platform` — deployed first, root of trust | `Example.Platform.SecretStore`, `Example.Platform.ConfigurationStore`, `Example.Platform.LogSpace` | `Example.Platform.Gateway` |
| `Identity/` | `identity` | `Example.Identity.IdentityHub` | `Example.Identity.Gateway` |
| `Networking/` | `networking` | `Example.Networking.Rezolvr` (DNS), `Example.Networking.VpnGateway` | `Example.Networking.Gateway` |
| `Zones/AppA` | `appa` | `Example.AppA.Api`, `Example.AppA.Spa`, `Example.AppA.Database` (schema in C#), `Example.AppA.SecretStore` | `Example.AppA.Gateway` |
| `Zones/AppB` | `appb` | same shape (billing) | `Example.AppB.Gateway` |
| `Zones/AppC` | `appc` | same shape (inventory) | `Example.AppC.Gateway` |
| `Gateway/` | `cohesion-system` | — | `Example.Gateway` — the **application set**: one gateway instance over all six applications, the sole production owner |

Every project is an ordinary executable with a `Program.cs`. Database programs define tables and relationships in C#.
The generic Identity, Networking, Platform, and SecretStore programs retain their intended ownership boundaries while
their typed configuration surfaces remain upstream work. Orchestration is the opt-in
`<CohesionApplicationModel>enabled</CohesionApplicationModel>`
in each csproj (every resource here is referenced, so every one is enabled): it generates the manifest and the typed
`Resource.*` accessors. Web and Database currently add a registered runtime entry/control plane; the generic area SDKs
retain the same intended boundary but await that upstream implementation. The csproj otherwise holds only what the
gateway needs to know.

The checked-in `10.0.1-preview.3.local` projects select Local and InProcess where the model is composable. The current
runnable path is the zone Database/API/SPA subset (or the Acme pair); generic area resources do not yet register the
runtime entries/control planes those gateways require. Networking includes a non-composable VPN data plane and therefore
selects Local only; the root application-set gateway also selects Local. Kubernetes execution is deferred until its
provider is released.

In the target production topology, all areas share one Kubernetes gateway: `Gateway/Example.Gateway` obtains every area
gateway's model through its control plane and reconciles them in one process (one client, one informer, one embedded registry),
applying namespaces `platform`, `identity`, `networking`, `appa`, `appb`, `appc`. The per-area gateways exist so an
area can be **pulled down and run alone** once its resource control planes are implemented. In this single-cluster tree,
those references will bind through the cluster's namespaces and operator channel; `k8s-federated/` targets peer
control-plane bindings. Those provider/control-plane paths are not present in the pinned local package set.

The rejected shape is explicit:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

A gateway owns exactly what it references.

Each area owns its entire dependency tree. In the direction-of-record API, a zone declares what it needs from Identity,
Platform, and Networking as commands on those references in its gateway's `Program.cs`; the zone's gateway delivers them
after the target is running and removes them on teardown. The typed command packages are not in this pinned release, so
the current programs preserve the ownership boundary without fabricating those future verbs.

```bash
# implemented loop: one zone subset as supervised processes, then as one process
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode run
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess --mode run

# generic area models can be inspected without starting their unimplemented runtime control planes
dotnet run --project examples/k8s/Networking/Example.Networking.Gateway -- --gateway local --mode describe

# what a gateway would deploy, as a document (this is also how the root set reads it)
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode describe
```
