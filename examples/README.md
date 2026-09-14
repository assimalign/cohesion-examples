# Examples

All three scaffolds use packaged Cohesion SDKs, executable `Program.cs` composition, generated manifests and default control planes. Folder-level `CohesionApplication` values express application boundaries.

| Folder | Projects | Topology |
| --- | ---: | --- |
| [single-app](single-app/) | 3 | Acme API/database; Local, InProcess, Docker and Kubernetes providers |
| [k8s](k8s/) | 25 | Six applications and one Local root application set |
| [k8s-federated](k8s-federated/) | 24 | The same applications, each independently owned |

Seven SDK resource kinds have typed descriptors. Zones own their ConfigurationStore namespace commands; IdentityHub and Rezolvr lack typed external binders, so their owning gateways carry zone declarations. Zone HTTPS certificates are requested on each zone's SecretStore. Platform ConfigurationStore starts after its local LogSpace sink; zone APIs retain console logging because remote LogSpace injection is unavailable.

The rejected shape is explicit:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

Every executable's launch profile selects Local. Use `--no-launch-profile` with shell environment overrides. See the [root README](../README.md) for package feeds, offline rendering, the CLI, and deployed Development behavior. The full graphs still require their external command channels and operator-supplied secrets; model inspection is independent of those running services.

Run `pwsh ../setup.ps1` from this directory after changing project membership, or `pwsh ../setup.ps1 -Check` to verify the generated solutions.
