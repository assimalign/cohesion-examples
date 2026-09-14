# cohesion-examples

Three consumer scaffolds implement [the Cohesion developer-experience design](https://github.com/assimalign/cohesion/blob/main/docs/DEVELOPER_EXPERIENCE_DESIGN.md): 52 existing executable projects, each with its own `Program.cs`.

| Scaffold | Projects | Composition |
| --- | ---: | --- |
| [single-app](examples/single-app/) | 3 | Acme API + database, one gateway |
| [k8s](examples/k8s/) | 25 | Platform, Identity, Networking and three zones, plus a root application set |
| [k8s-federated](examples/k8s-federated/) | 24 | The same six applications with independent gateways |

The SDK supplies executable defaults and Debug runtime settings. Root `Directory.Build.props` carries organization identity; each area sets `CohesionApplication` once. Every resource explicitly enables `CohesionApplicationModel`; gateways are always enabled by `Sdk.Gateway`. Resources have generated manifests, `Resource.*` accessors and their area's default control plane. There are no hand-written resource/gateway wrapper classes or SDK bridges.

Gateway generation returns typed descriptors for Web, Database, ConfigurationStore, SecretStore, IdentityHub, Rezolvr and LogSpace. The five command-bearing areas used here have declarative verbs; Web and LogSpace have none. VpnGateway and Scheduler still use the generic descriptor. Only Web, Database and ConfigurationStore have typed external binders: zones declare configuration namespaces themselves, while Identity and Networking gateways temporarily carry the zone audience/client and DNS commands.

The rejected ownership shape is recorded verbatim:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

## Local environment

Every project has `Properties/launchSettings.json` selecting environment `Local` for developer-machine runs. The six zone APIs keep their overrides in `appsettings.Local.json`. Development uses strict deployed security. To supply an environment through shell variables, use `dotnet run --no-launch-profile` so launch settings do not override those values.

`Development` is still a real, deployable environment name; `Local` is the developer-machine name. `--realize` is refused outside Local. A standalone process with neither environment variable still defaults to `Production`.

`cohesion run` injects `--environment Local` only when no explicit argument or shell environment is supplied and the effective gateway is absent, `local` or `inprocess`. The CLI suppresses launch profiles when `COHESION_ENVIRONMENT` or `DOTNET_ENVIRONMENT` is supplied in the shell; a direct `dotnet run` requires `--no-launch-profile` explicitly.

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --mode describe
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --mode describe
dotnet run --project examples/single-app/Acme.Gateway -- --gateway local --mode run
dotnet run --project examples/single-app/Acme.Gateway -- --gateway inprocess --mode run
```

The full zone declares four local resources: SecretStore, Database, API and SPA. Zone APIs declare HTTPS using their SecretStore's requested certificate; Acme and the SPAs retain HTTP. External ConfigurationStore commands require a reachable, authorized Platform gateway; a static localhost endpoint fallback supplies bindings but cannot substitute for that command channel. See the [landing-zone notes](examples/k8s/README.md) for runtime prerequisites and remaining gaps.

The final verification against Cohesion `5dfa1e3c` builds all 52 projects and describes the individual and root application models. Live runs remain blocked by SecretStore's ambient content-root mismatch and SQL's unsupported principal/grant migrations. No healthy HTTPS probe or delivered telemetry record was established. The full evidence and remaining SDK issues are in [VERIFICATION.md](VERIFICATION.md).

## Packages and portable sources

`global.json` pins all 20 Cohesion SDKs to `10.0.1-preview.3.local`. The checked-in `nuget.config` retains the verification machine's two local feeds:

- `C:\Source\repos\assimalign\cohesion\_out\packages`
- `C:\Source\repos\assimalign\cohesion-platforms\_out\packages`

The three exact platform mappings are `Assimalign.Cohesion.ApplicationModel.Gateway.Containers`, `.Docker`, and `.Kubernetes`; they resolve from the platforms feed at `10.0.1-preview.3`. The seven gateways selecting these providers pin `CohesionPlatformsVersion` themselves so root props remain identity-only. InProcess and ControlPlane continue to resolve from the Cohesion feed.

For another machine, copy `nuget.config` to ignored `nuget.config.user` and update the two source paths with `dotnet nuget update source <name> --source <path> --configfile nuget.config.user`. Use `dotnet restore --configfile nuget.config.user`, then build with `--no-restore`. A developer may also use `dotnet nuget add source` in their user configuration; this repository's `<clear />` means such a source must also be listed in the selected config. Keep credentials outside tracked files. During SDK resolution, make the developer config the active repository config locally, without committing its path changes. Use a fresh `NUGET_PACKAGES` when the same `.local` version is repacked.

CI removes **both** local sources and mappings, switches SDK pins to `10.0.1-preview.3`, and rejects leftover local source entries. `.github/workflows/build.yml` is structurally complete and blocked on the nuget.org promotion of `10.0.1-preview.3`. The credential guard and ignored `.cohesion/` and `parameters.json` remain in place.

## Offline platform rendering and CLI

Docker and Kubernetes providers are selected by the zone gateways and Acme. Networking stays Local because VpnGateway is non-composable; the root set also stays Local to demonstrate application-set model resolution. Identity and Platform retain Local/InProcess.

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway docker --mode render
# Illustrative digest for OFFLINE rendering only; replace it with a real published digest before deployment.
$image = 'example/gateway@sha256:' + ('0' * 64)
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway kubernetes --mode render --environment Development --cohesion-system-image $image --cohesion-system-storage 1Gi --control-plane-expose loadbalancer

dotnet tool install Assimalign.Cohesion.Cli --tool-path .cohesion/tools --version 10.0.1-preview.3.local --configfile nuget.config
# Run cohesion status from a gateway project directory; cohesion new --help lists template usage.
```

Render contacts neither a Docker daemon nor a cluster. Both providers first require each resource manifest to carry a published `artifact.image`; the checked-in examples have no published resource images, so the commands above currently stop at that validation gate. The supplied local feed has Windows runtime packs only, preventing the required Linux image publication in this pass.

After publication, Docker's renderer produces Compose services/networks/volumes and `x-cohesion` metadata; its control plane stays in the gateway process. Kubernetes also requires the digest-pinned system image and storage size, and its renderer produces control-plane Services and an intentionally empty trust Secret. Those YAML objects were not observed in this verification because resource-image validation stopped rendering. Kubernetes requires JIT, so SDK auto-selection makes `PublishAot=false` for gateways selecting it; `IsAotCompatible` remains enabled.

Run `pwsh ./setup.ps1` to regenerate the four solutions and `pwsh ./setup.ps1 -Check` to verify them. No solution includes `_old/` or `.vs/`; both owner-retained folders remain untouched.
