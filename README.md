# cohesion-examples

Reference scaffolds for organizations building on Cohesion, materialized from
[`docs/DEVELOPER_EXPERIENCE_DESIGN.md`](https://github.com/assimalign/cohesion/blob/main/docs/DEVELOPER_EXPERIENCE_DESIGN.md)
(the direction of record). Every deployable thing is an ordinary .NET executable that picks a Cohesion SDK, and every
relationship is an ordinary project or package reference. Local validation pins `10.0.1-preview.3.local` and exercises
the implemented Local and InProcess gateways; CI switches the pins to `10.0.1-preview.3` for the released-feed gate.
Docker, Kubernetes, and federated clusters are the target deployment shapes and do not require resource-code changes.

| Scaffold | Shape | What it shows |
| --- | --- | --- |
| [`examples/single-app`](examples/single-app/) | one repo, one gateway, two resources | the two-person-company story: explicit Local or one-process InProcess execution |
| [`examples/k8s`](examples/k8s/) | mono-repo, one target cluster | the landing-zone taxonomy, a runnable zone subset, and the root application-set composition shape |
| [`examples/k8s-federated`](examples/k8s-federated/) | mono-repo, one target cluster per area | the same projects, no root gateway; current Local/InProcess gateways retain the future control-plane boundaries |

Conventions shared by all three: `global.json` pins the .NET SDK and all 20 Cohesion SDKs; `nuget.config` carries no
credentials (guarded by CI); the root `Directory.Build.props` carries shared identity and temporary packaged-SDK build
defaults; each area folder sets `CohesionApplication` once. **Every project is a `Program.cs`-only executable.**
Database programs define their current schema in C#; generic area programs mark the intended ownership and host boundary
until their typed configuration APIs land. **Orchestration is an opt-in**:
`<CohesionApplicationModel>enabled</CohesionApplicationModel>`
in a csproj generates the manifest and `Resource.g.cs` (typed accessors for the resource's own endpoints, mounts, settings,
and references). Web and Database also generate the registered runtime entry/control plane needed by today's gateways;
the generic area SDKs still await that implementation. A plain executable without the opt-in cannot be referenced. Every
resource in these scaffolds is enabled because every one is referenced.
The csproj otherwise carries only what the gateway needs to know: name, endpoints, probes, mounts, settings, references.
The direction of record also assigns **declarative commands** (`identity.AddAudience(...)`,
`config.AddNamespace(...)`, `dns.AddARecord(...)`) to the referencing application. Those typed command packages are a
later deliverable, so this preview records that ownership in comments instead of inventing APIs in the examples repo.

The rejected ownership shape is recorded verbatim:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

Run `pwsh ./setup.ps1` after adding or removing a project. The script deterministically regenerates the root solution and
all three per-example `.slnx` files; `pwsh ./setup.ps1 -Check` verifies them without writing.

```bash
# run the implemented zone subset: N supervised processes, then one process
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode run
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess --mode run

# the small-business scaffold follows the same Local/InProcess progression
dotnet run --project examples/single-app/Acme.Gateway -- --gateway local --mode run
dotnet run --project examples/single-app/Acme.Gateway -- --gateway inprocess --mode run
```
