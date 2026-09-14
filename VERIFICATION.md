# Item 38d verification — 2026-09-14

Refs: assimalign/cohesion#979

## Result

All 52 projects build, both clean Debug solution builds and the Release build compile, and the root application set describes all six applications. The examples now use the landed typed descriptors, Local launch profiles, SDK defaults, corrected resource names, TLS declarations and platform providers.

This is **not a green end-to-end pass**. SecretStore startup, SQL schema migration, missing published resource images, one SDK warning and the platform dependency advisory prevent the requested acceptance bar. No successful HTTP/HTTPS probe or telemetry delivery is claimed. This report records every recipe step and each remaining substitution.

## Preflight and feed provenance

- Examples: `C:\Source\repos\assimalign\cohesion-examples`, actual branch **main**, original HEAD `f43e6fa68b63f287abb59da970fa3ce02947b5e9`. The final run note instead names `feature/dx-design-buildout`. No branch was switched. Commit clarification was requested; absent an answer, changes remain staged and uncommitted.
- Source snapshot: `C:\Source\repos\assimalign\_head-ro`, HEAD `5dfa1e3c15d96af5943802ca12a396138a998cfc`. This was the only Cohesion source tree read.
- FIRST, before any dotnet invocation: opened `C:\Source\repos\assimalign\cohesion\_out\packages\Assimalign.Cohesion.Sdk.Gateway.10.0.1-preview.3.local.nupkg` read-only. Its bundled props contain **7** resource-kind rows and `SecretStore`. Its nuspec repository commit is **`5dfa1e3c15d96af5943802ca12a396138a998cfc`**, carrying both 16b and C1.
- Cohesion feed: **833 files**, **244** `*.10.0.1-preview.3.local.nupkg`, all **20 SDKs**. All required packages are present: ApplicationModel, ApplicationModel.Gateway, Gateway.InProcess, Gateway.ControlPlane, Hosting.Resources, Hosting.Telemetry, OpenTelemetry, Connections; Web/Database/ConfigurationStore/SecretStore/IdentityHub/Rezolvr/LogSpace/VpnGateway/Scheduler.ApplicationModel; SecretStore.Client, ConfigurationStore.Client, Database.Client, Cli and Templates. No required preflight package was missing.
- Runtime coverage at `.local`: **19 win-arm64**, **1 win-x64** (the old Database pack); no Linux runtime packs. Nineteen App ref packs are present. Host: .NET SDK **10.0.401**, RID **win-arm64**.
- Platforms source and all three package nuspecs report **`726bff72832b108cc57063ed842ee1e7b4b98c25`**, the Local follow-up on `6910b9e`, as anticipated by the final run note. Feed: `C:\Source\repos\assimalign\cohesion-platforms\_out\packages`. Exactly these three packages are present at **10.0.1-preview.3**: `Assimalign.Cohesion.ApplicationModel.Gateway.Containers`, `Assimalign.Cohesion.ApplicationModel.Gateway.Docker`, `Assimalign.Cohesion.ApplicationModel.Gateway.Kubernetes`. Both gateway classes implement `IApplicationGatewayRenderer`.
- Fresh cache throughout: **`C:\Source\repos\assimalign\cohesion-examples\.cohesion\38d\nuget`**. CLI home, HTTP cache, temporary files, tools, logs and runtime state also reside under `.cohesion/38d`. The user-wide NuGet cache was not deleted. No upstream build, pack, installer, GitHub command, push or deployment was run.
- The first dotnet invocation's first-use setup reported `Installed an ASP.NET Core HTTPS development certificate.` This incidental SDK side effect was not requested by the implementation. No trust command was run; subsequent commands set `DOTNET_GENERATE_ASPNET_CERTIFICATE=false`. No existing certificate was removed.

## Implementation and ownership decisions

### Defaults, scaffolds and Local

Removed exactly **7** SDK-owned root properties, the **1** `Directory.Build.targets` file, **11** runtime-reference properties, and **14** redundant gateway model opt-ins. These are **33** distinct removals, not the brief's arithmetical total of 26. Each gateway has the requested always-enabled comment. The root props retain the three identity/version properties; all **38 resources** retain explicit `CohesionApplicationModel=enabled`. **No project loses Debug runtime posture.** The immediate Acme describe smoke passed without any SelfContained/RuntimeIdentifier command-line workaround.

Removed the deleted targets file from `setup.ps1`'s RootFiles and regenerated the root solution. The three scaffold solutions were already current and therefore have no artificial diff. Counts: root **52 projects + 6 files**; single-app **3**, k8s **25**, k8s-federated **24**. No `Directory.Build.targets` solution entry remains.

Deleted both untracked `Identity/Example.Identity.IdentityHub/Flows/UserFlowSteps.cs` files and their now-empty folders in k8s and k8s-federated. They referenced nonexistent `IdentityHub.Flows`, `IUserFlowStep`, `FlowContext`, `FlowResult` and `VerificationCode` APIs and lacked explicit threading usings. Both IdentityHub projects subsequently build with **0 warnings / 0 errors**. Their removal produces no tracked diff; `_old/` and `.vs/` were preserved.

All **52** new `Properties/launchSettings.json` files were structurally verified: one profile named exactly for its project, `commandName: Project`, only `COHESION_ENVIRONMENT: Local`, no extra options, two-space indent and trailing newline. The six API settings files were moved with `git mv`; the staged diff reports **six R100 renames** to `appsettings.Local.json`. There are zero tracked `appsettings.Development.json` files under examples. All **22** plain `appsettings.json` files are unchanged.

The root keeps the shipped template's environment guard and uses **`AppEnvironment.Keys.Local`**. This keeps the example/template behavior aligned while preserving explicit arguments and shell variables. It still hand-writes `Application.CreateSet(new LocalGateway(...), args)` and its six `AddApplication` calls. `Applications.AppA`, `AppB` and `AppC` casing is corrected.

The word `Development` remains in `README.md` (the deployed environment explanation and Kubernetes example), `examples/README.md` and `examples/k8s-federated/README.md` (links to that deployed behavior), and this verification report (migration evidence and design drift). It no longer appears in executable example code, project comments or environment settings filenames. `Local` is the developer-machine name; unset standalone Core still defaults to Production. Direct shell-environment overrides require `dotnet run --no-launch-profile`; the CLI suppresses launch profiles itself.

### Typed descriptors: the 16b result

The generated declarations and explicitly typed consuming code compile **without any consumer `CohesionGatewayResourceKind` row or SDK bridge**:

| Generated call | Returned descriptor | Used command/ordering |
| --- | --- | --- |
| `AddAppASecretStore`, AppB/AppC twins, `AddPlatformSecretStore` | `ISecretStoreResourceDescriptor` | `IssueCertificate` |
| `AddAppADatabase`, AppB/AppC twins | `IDatabaseResourceDescriptor` | `AddDatabase`, typed `DependsOn` |
| `AddAppAApi/Spa`, AppB/AppC twins | `IWebResourceDescriptor` | typed `DependsOn`; no built-in command verbs |
| `AddPlatformConfigurationStore` and `RemoteReferenceConfigurationStore` | `IConfigurationStoreResourceDescriptor` | `AddNamespace`, typed `DependsOn` |
| `AddIdentityHub` | `IIdentityHubResourceDescriptor` | `AddAudience`, `AddClient` |
| `AddNetworkingRezolvr` | `IRezolvrResourceDescriptor` | `AddARecord` |
| `AddPlatformLogSpace` | `ILogSpaceResourceDescriptor` | commandless; `DependsOn` returns `IApplicationResourceDescriptor`, used for `logs` |
| `AddNetworkingVpnGateway` | `IApplicationResourceDescriptor` | generic and commandless |

Scheduler also lacks a resource-kind row and a typed descriptor; there is no Scheduler example to build. The commands actually exercised by compilation are IssueCertificate, AddDatabase, AddNamespace, AddAudience, AddClient and AddARecord. Other command signatures were read and documented, not falsely claimed as exercised here.

Every zone now adds its SecretStore, Database, API and SPA. Existing orders/billing/inventory schemas remain resource-owned. The typed gateway database commands create **orders-archive / billing-archive / inventory-archive**, using the corresponding named SQL engine, because `DatabaseResourceCommandHandler` rejects taking ownership of an existing code-owned database. Explicit table names now match existing grants in all seven Database programs (Customers, Orders/OrderLines, Invoices/Payments, Items/Movements). The permissions and foreign-key declarations remain intact; upstream migrations still block startup.

Each zone gateway reads its existing Configuration JSON into a string dictionary and owns exactly its `appa`, `appb` or `appc` namespace through the typed external ConfigurationStore binder. The existing PageSize seed is consumed; the generated CohesionSetting remains the API's local default. No live configuration client was invented.

Identity's owning gateway declares all three zone audiences and confidential service clients. Each client has a named, parameter-backed Secret mount on IdentityHub. These demonstrate the shipped `AddClient(clientId, audiences, credentialSource)` API; they do not implement the design's browser authorization-code/redirect-URI example. Networking's owning gateway declares three Local-only IPv4 loopback A records (`appa-dev`, `appb-dev`, `appc-dev`). Both relocated sites explicitly say that §4.3 assigns the declaration to the consuming application, but the area's missing typed external binder requires ownership-side declaration until that gap closes. The READMEs name the same deviations.

Platform's gateway owns `shared` and consumes the existing shared.json through a linked Content item. ConfigurationStore's host owns `networking` and consumes networking.json, flattening keys with colons. This avoids declaring `shared` both in the host and as a gateway command: `ConfigurationStoreRepository` rejects that ownership collision. Platform SecretStore demonstrates `AddCertificateAuthority` with `Example Root CA`; `SelfSeedWhenNoPlatform` defaults true. IdentityHub's host declares `example-operations` and the device-enabled `example-cli` client. Rezolvr, VpnGateway and LogSpace retain minimal host programs because their builders expose no domain verbs.

### Resource naming and SDK facts

Both ConfigurationStore csprojs now declare `platform-configuration-store`; their comment and certificate source key were changed together. All six AppA/AppB/AppC API Programs use `Resource.References.PlatformConfigurationStore`. Both generated spellings were inspected: `Externals.PlatformConfigurationStore` and `Resource.References.PlatformConfigurationStore`. The two Platform gateway Programs additionally use the new name for the issued certificate. Documentation records the rename. Existing correctly spelled external accessors were retained.

The exact source-bearing files for this rename are the two `examples/{k8s,k8s-federated}/Platform/Example.Platform.ConfigurationStore/Example.Platform.ConfigurationStore.csproj`, the two `Platform/Example.Platform.Gateway/Program.cs`, and the six `Zones/App{A,B,C}/Example.App{A,B,C}.Api/Program.cs`. The full path inventory is below.

The sibling names `platform-secretstore`, `platform-logspace`, and `appa-secretstore`/`appb-secretstore`/`appc-secretstore` remain unchanged: no example resource-side C# accessor consumes them, and the ruling scoped the rename to ConfigurationStore. Their potential `Secretstore`/`Logspace` casing deserves an owner decision.

Removed the VpnGateway `Update="wireguard"` no-op; there is no SDK endpoint by that name, and declaring an unsupported data-plane listener would misrepresent the shipped host. Its SDK endpoint remains http:8080 and composable=false. Corrected LogSpace to HTTPS OTLP 4318 with tls, Rezolvr to separate dns UDP/TCP endpoints with no data mount, the verb-noun command lists, and the false runtime/control-plane and domain-composition comments. The gateway generated filename is actually `Gateway.g.cs`, so the six older `Resources.g.cs` comments were corrected too.

### HTTPS and telemetry evidence

For each of six zone APIs, the existing Web endpoint is updated and one Secret mount is included (AppA shown; AppB/AppC names substituted):

```xml
<CohesionEndpoint Update="http" Scheme="https" Certificate="tls" ContainerPort="8443" Public="true" />
<CohesionMount Include="tls" Kind="Secret" Source="appa-secretstore:certs/appa-api" />
```

This deliberately preserves the endpoint **name** http for the ambient binder/control plane and changes its **scheme** to https. There is no second plain-HTTP zone API listener in the model. SPAs and Acme deliberately retain plain HTTP. The four duplicate SDK tls declarations on IdentityHub/ConfigurationStore now use `Update` with only `Source`; the legitimate signing and VPN keys Includes remain. There are exactly six remaining tls Includes, all the newly configured Web APIs. Certificate commands request localhost and 127.0.0.1 SANs.

Describe confirms the API's sole declared endpoint is http/https/8443/certificate=tls and its explicit source is the zone SecretStore. **The actual API listener and SecretStore-issued API certificate were not verified**, because its SecretStore never became Running. A diagnostic inspection DPAPI-unprotected the generated SecretStore bootstrap leaf **in memory**, using `ProtectedData.Unprotect(..., null, CurrentUser)` and `X509Certificate2.CreateFromPem`. Its subject was `CN=appa-secretstore-api`, issuer **`CN=Cohesion appa development root`**. This establishes the **gateway development issuer** branch for bootstrap only. No private-key material was printed or written unprotected.

No `trust.pem` was materialized in any of the five run cases, and no healthy resource listener was established. Therefore **zero HTTP status codes and zero trusted HTTPS probes** are reported. The requested curl trust-bundle probe was **blocked, not replaced by disabled certificate validation**. No validation bypass was used. In-memory bootstrap-leaf inspection is not a substitute for an endpoint trust check. The requested out-of-process trust-export API still does not exist; manual DPAPI unprotection would be necessary for a successful Windows curl probe.

Platform uses `logs = AddPlatformLogSpace().DependsOn(secrets)` and `AddPlatformConfigurationStore().DependsOn(secrets, logs)`. ConfigurationStore is the producer selected to start after LogSpace. SecretStore must bootstrap the sink and retains console logging. Zone APIs retain console logging because same-application sink discovery cannot discover a remote LogSpace and there is no cross-application ordering API.

Platform startup failed at SecretStore before LogSpace or ConfigurationStore ran. Observed **0 telemetry.headers files**, **0 LogSpace record files**, and no producer process in which to verify injected endpoint/protocol/headers variables. **No OTLP arrival or successful telemetry injection is claimed.** The environment and delivery checks are blocked by startup, not silently omitted. The source contract reserves three `COHESION_TELEMETRY_*` names plus `COHESION_LOG_FORMAT`; only OTLP/HTTP JSON is implemented, and otlp-grpc throws.

### Platform packages and rendering

The six zone gateway csprojs and Acme select `Local;InProcess;Docker;Kubernetes` and explicitly pin **`CohesionPlatformsVersion=10.0.1-preview.3`**. This confines the platform version to its consumers and keeps root props identity-only. Networking and the root set deliberately stay Local; Identity and Platform stay Local/InProcess. The SDK reports `CohesionGatewayRequiresJit=true, PublishAot=false` for Kubernetes consumers, without disabling `IsAotCompatible` or adding overrides.

`nuget.config` adds the platforms feed with **exact** package mappings:

```xml
<packageSource key="cohesion-platforms-local">
  <package pattern="Assimalign.Cohesion.ApplicationModel.Gateway.Containers" />
  <package pattern="Assimalign.Cohesion.ApplicationModel.Gateway.Docker" />
  <package pattern="Assimalign.Cohesion.ApplicationModel.Gateway.Kubernetes" />
</packageSource>
```

There is no gateway-prefix wildcard, so InProcess and ControlPlane still resolve from cohesion-local. The workflow now removes both local sources and both mappings. Its exact PowerShell rewrite block was run on scratch copies: **20 release pins, all 10.0.1-preview.3; 0 local sources and 0 local mappings**. The released-feed restore/build itself was not run: the orchestrator identifies publication as pending. The READMEs state this publication dependency and document per-developer config overrides, including the repository's source-clear behavior.

Both required offline render commands were attempted with the provider packages actually restored (full commands below). Docker stopped with:

```text
Unhandled exception. System.InvalidOperationException: Resource 'appa-secretstore' cannot be realized by Docker because artifact.image is absent.
```

Kubernetes, despite supplying both required system options and a digest-pinned image, stopped with:

```text
Unhandled exception. System.InvalidOperationException: Resource 'appa-secretstore' cannot be realized by the Kubernetes gateway because its manifest does not declare artifact.image.
```

Neither produced YAML: **0 Compose documents, 0 Kubernetes objects**. Services/networks/volumes/x-cohesion, control-plane Services, control port 8080, trust Secret and read-only trust mount could not be confirmed from generated output. No daemon or cluster was contacted. Source confirms both renderer interfaces; this is a **resource-image prerequisite**, not a missing-renderer failure. A second Kubernetes attempt with explicit Development hit the same gate.

`CohesionResolveImagePublish` supports linux-x64 or linux-musl-x64 image artifacts; all supplied runtime packs are Windows. Specifically absent at `.local` are `Assimalign.Cohesion.App.Web.Runtime.linux-x64`, `.App.Database.Runtime.linux-x64`, and `.App.SecretStore.Runtime.linux-x64`, which the AppA image publication would require (ConfigurationStore, IdentityHub, LogSpace, Rezolvr and VpnGateway Linux packs are absent too). Image publication was **not attempted** against these missing packs, and no placeholder resource image was forged. The owner/orchestrator must provide Linux packs and published resource images for the requested render proof.

By design, Docker's renderer would not emit a control-plane service or certificate Secret; secret inputs have empty sensitive content. Kubernetes' offline trust Secret would be empty. Those expected absences are not reported as defects.

## Commands and verification results

All dotnet commands below used the same fresh cache. Complete local logs are in `.cohesion/38d`; ignored state is retained for review. No test project exists in these scaffolds, none was added, and **0 tests were run**. The consumer repository has no build/Tasks project to bootstrap. **No pack commands or pack outputs** were produced, as explicitly prohibited for this item.

### Builds and static checks

| Actual command/check | Result |
| --- | --- |
| `dotnet run --project examples/single-app/Acme.Gateway -- --gateway local --mode describe` immediately after bridge removal | exit 0; no runtime override; `bridge-smoke.log` |
| `dotnet build examples/k8s/Zones/AppA/Example.AppA.Gateway/Example.AppA.Gateway.csproj --no-incremental` | 2 warnings, 0 errors; typed command smoke |
| `git clean -xdn` | preview only; never executed as unrestricted deletion |
| `git clean -xdn -- <validated examples bin/obj paths>` then `git clean -xdf -- <those exact paths>` before each final Debug build | 104 bin/obj directories removed per final pass; no _old, .vs, source or cache deletion |
| `dotnet build Assimalign.Cohesion.Examples.slnx --no-incremental -m` — final clean pass 1 | exit 0; **12 warnings, 0 errors**, 19.34 s |
| same command — final clean pass 2 | exit 0; **12 warnings, 0 errors**, 19.91 s |
| `dotnet build Assimalign.Cohesion.Examples.slnx --configuration Release --no-incremental -m` | exit 0; **12 warnings, 0 errors**, 10.38 s |
| `dotnet build <csproj>` for each of the **52** tracked projects, individually | **52 exit 0**, aggregate **21 warnings, 0 errors**; `restored-project-builds.json` lists each exact path/command operand and result |
| `pwsh ./setup.ps1` and `pwsh ./setup.ps1 -Check` | all four solutions current; counts 52/3/25/24 and root 6 files |
| XML/JSON checks over every project/profile | 14 SDK-enabled gateways, 38 explicitly enabled resources, 52 valid Local profiles; no missing opt-in |
| `git diff --cached --name-status` | six settings renames are R100 |
| `git diff --quiet -- 'examples/**/appsettings.json'` | exit 0; all 22 neutral settings unchanged |
| CI rewrite block on scratch copies | 20 released pins; both local sources and mappings removed |

Earlier full Debug passes before the final schema fix also compiled with 14 warnings each; the earlier Release build had 12 warnings, and the incremental schema-fix solution build had 15. Their logs are retained. The final clean results above supersede those source snapshots.

An extra individual-project attempt used `dotnet build <csproj> --no-restore` immediately after Release: **52 failures, 66 aggregate NETSDK1047 errors**, because Release's shared assets lacked the Debug win-arm64 target. This was a verification-command mistake, not a demonstrated SDK restore failure. All 52 were rerun with normal restore and passed. No tracked workaround was added.

No generated nullable warning remains. However, each final solution build still has **one SDK warning** (the zero-warning bar fails):

```text
Sdk.Gateway found no resource manifest owned by application 'cohesion-system'; Build() will reject a zero-resource application.
```

It comes from packed `Targets/Sdk.Gateway.targets(57,3)` for the root IApplicationSet, whose describe actually succeeds. The other **11 warnings** in each final solution build are restore/build repetitions of:

```text
warning NU1902: Package 'KubernetesClient' 17.0.4 has a known moderate severity vulnerability, https://github.com/advisories/GHSA-w7r3-mgwf-4mqq
```

The platforms dependency was neither replaced nor suppressed.

### Describe, environments and render

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode describe
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --mode describe
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode describe
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --mode describe
```

All four exit **0**. Both AppA descriptions contain four local resources (`appa-secretstore`, `appa-database`, `appa-api`, `appa-spa`) and two externals (`platform-configuration-store`, `identity-hub`). The root resolves **platform, identity, networking, appa, appb, appc** through the six generated application declarations.

The last command has no gateway or environment argument. Its verbatim model fields are:

```text
  "application": "appa",
  "environment": "Local",
  "gateway": "local",
```

With `COHESION_ENVIRONMENT=Development` in the shell:

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway --no-launch-profile -- --mode describe
```

It exits **-532462766**, with this verbatim output:

```text
Unhandled exception. System.InvalidOperationException: No Cohesion gateway was selected. Pass --gateway, set COHESION_GATEWAY, or use Local for the local default.
```

Together these verify C1 in the restored feed, rather than merely in source. The shell override was scoped to that verification process.

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway docker --mode render
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway kubernetes --mode render --cohesion-system-image example/gateway@sha256:0000000000000000000000000000000000000000000000000000000000000000 --cohesion-system-storage 1Gi --control-plane-expose loadbalancer
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway kubernetes --mode render --environment Development --cohesion-system-image example/gateway@sha256:0000000000000000000000000000000000000000000000000000000000000000 --cohesion-system-storage 1Gi --control-plane-expose loadbalancer
```

All three exit **-532462766**, blocked by missing resource artifact.image as quoted above. The illustrative digest was used only as an offline system option; it is not a claimed published image.

### Live runs, endpoint checks, telemetry and CLI

To isolate state, the live commands ran from separate `.cohesion/38d/<case>` working directories, using an absolute project path and `--no-build --no-restore` after successful builds. This is an explicit recipe adjustment. Commands were:

```powershell
dotnet run --no-build --no-restore --project C:\Source\repos\assimalign\cohesion-examples\examples\k8s\Zones\AppA\Example.AppA.Gateway -- --gateway local --mode run
dotnet run --no-build --no-restore --project C:\Source\repos\assimalign\cohesion-examples\examples\k8s\Zones\AppA\Example.AppA.Gateway -- --gateway inprocess --mode run
dotnet run --no-build --no-restore --project C:\Source\repos\assimalign\cohesion-examples\examples\k8s\Platform\Example.Platform.Gateway -- --gateway local --mode run
dotnet run --no-build --no-restore --project C:\Source\repos\assimalign\cohesion-examples\examples\single-app\Acme.Gateway -- --gateway local --mode run
dotnet run --no-build --no-restore --project C:\Source\repos\assimalign\cohesion-examples\examples\single-app\Acme.Gateway -- --gateway inprocess --mode run
```

All five terminate at the StatefulSet readiness gate with the relevant SecretStore/database **Failed**. Acme was repeated from fresh state after fixing table/grant name mismatches. Its second attempts still fail for the upstream principal migration limitation. No test-started gateway or resource process remains running.

Representative live failure:

```text
Resource 'appa-secretstore' did not reach Running or another state satisfying the StatefulSet readiness gate (observed 'Failed') in gateway 'local'. Reconcile aborted.
```

The normal error hides the initiating host failure. A scratch-only startup hook, built without a new project, observed first-chance exceptions during separate diagnostic runs:

```text
System.InvalidOperationException: The host content root must match the ambient resource content root.
Assimalign.Cohesion.Database.DatabaseSchemaMigrationException: SQL schema 'customers' declares principals and grants, but the SQL DDL executor cannot migrate them yet.
```

The hook was not added to example code, and its environment setting was removed after launching those diagnostics. Endpoint probes (recipe 5/6/10) and telemetry injection/delivery checks (recipe 7) are **blocked before healthy startup**, with zero successful probes/records. Root and federated describe (recipe 8/11) passed. No requested live failure is counted as success.

```powershell
dotnet tool install Assimalign.Cohesion.Cli --tool-path .cohesion/38d/tools --version 10.0.1-preview.3.local --configfile nuget.config
.cohesion/38d/tools/cohesion.exe new --help
```

Both exit **0**. The tool was also invoked by its absolute path with `status` from `examples/k8s/Zones/AppA/Example.AppA.Gateway`: exit **2**, verbatim ``no local gateway state; run `cohesion run` first``. The isolated failed runs did not establish healthy state in that project directory. This is a successful tool installation/help check, **not a passing live status check**. `--prerelease` was not passed with `--version`.

## Blocked / needs decision: exact upstream gaps

All Cohesion paths in this section are relative to `_head-ro` at the verified commit; platforms paths are relative to its read-only checkout.

1. **SecretStore startup:** `resources/SecretStore/Assimalign.Cohesion.SecretStore.Hosting/src/SecretStoreApplicationContext.cs:16` constructs `HostEnvironment` without ContentRootPath. `libraries/Hosting/Assimalign.Cohesion.Hosting/src/Implementation/HostEnvironment.cs:25` leaves that init property unset. `Assimalign.Cohesion.Hosting.Resources/src/Internal/ResourceHost.cs:51-58`, `AssertContentRoot`, rejects it. Fix/pack the host context upstream; no examples-side hosting shim was introduced. This blocks AppA Local/InProcess, Platform, API TLS and telemetry.
2. **Database startup:** `resources/Database/Assimalign.Cohesion.Database.Sql/src/Internal/SqlSchemaProvisioner.cs:294,322`, `RejectUnsupported`, rejects schema principals/grants; lines 299-303 also reject foreign-key migrations. The examples' schema permissions should not silently be removed to obtain a green run. Implement the migration support upstream or explicitly approve a reduced schema demonstration. The concrete Acme diagnostic is quoted above.
3. **Published images/render prerequisite:** `platforms/Docker/Assimalign.Cohesion.ApplicationModel.Gateway.Docker/src/DockerGateway.cs:101-123`, `ValidateResource`, and the Kubernetes project's `src/KubernetesGateway.cs:115-139`, `ValidateResource`, reject absent resource artifact.image. `sdks/Assimalign.Cohesion.Sdk/Tasks/Tasks/Resources/CohesionResolveImagePublish.cs:24,73-75` requires a Linux RID. Supply the missing runtime packs and published resource images. No image bridge or upstream pack was allowed.
4. **SDK set warning:** `sdks/Assimalign.Cohesion.Sdk.Gateway/Tasks/Tasks/CohesionCreateResourceVerbs.cs`, the zero-owned-manifests warning invoked at `Targets/Sdk.Gateway.targets:57`, does not exempt this valid IApplicationSet. Describe succeeds, but the zero-SDK-warning acceptance bar fails.
5. **Dependency advisory:** the Kubernetes provider brings `KubernetesClient 17.0.4`, producing NU1902. Updating/repacking that provider is outside this examples item.
6. **Typed external binders:** each of `Assimalign.Cohesion.{SecretStore,IdentityHub,Rezolvr,LogSpace,VpnGateway,Scheduler}.ApplicationModel` lacks `RemoteReference<Area>`. Only Web, Database and ConfigurationStore provide one in their `src/Extensions/*ResourceExtensions.cs`. Thus the zone cannot express the design's typed IdentityHub/Rezolvr mutations directly. The owning gateways carry them with explicit deviation comments.
7. **Static external fallback is insufficient for commands:** `libraries/ApplicationModel/Assimalign.Cohesion.ApplicationModel.Gateway/src/ApplicationGateway.Commands.cs:213-217` rejects non-peer command bindings with `Remote commands require an authenticated peer gateway control-plane binding.` The typed configuration seed compiles, but an authorized reachable Platform gateway remains required for execution. This would remain after fixing local SecretStore startup.
8. **Telemetry boundaries:** `Assimalign.Cohesion.ApplicationModel/src/Abstractions/IApplicationResourceDescriptor.cs`, `DependsOn`, is same-application only. `Assimalign.Cohesion.ApplicationModel.Gateway/src/ApplicationGateway.Telemetry.cs:31-69`, `TryGetOwnLogSpaceEndpoint`, only discovers the same application's sink; Local alone permits the declared DevPort fallback. `tests/GatewayTelemetryTests.cs:27,37` explicitly verifies no inferred edge. Remote LogSpace is not a zone sink at this version. Explicit Platform ordering is present, but runtime delivery is blocked.
9. **Host domain APIs:** `resources/{Rezolvr,VpnGateway,LogSpace}/Assimalign.Cohesion.<Area>.Hosting/src/Abstractions/I<Area>ApplicationBuilder.cs` offers AddService overloads and Build only, with no zone, peer or ingestion verbs. These resource Programs remain minimal by necessity.
10. **Remaining descriptor gaps:** `sdks/Assimalign.Cohesion.Sdk.Gateway/Targets/Sdk.Gateway.props` has no VpnGateway or Scheduler resource-kind row. `Scheduler.ApplicationModel/src/Extensions/SchedulerResourceExtensions.cs:19`, `AddScheduler`, returns the base descriptor and there is no typed descriptor. `LogSpace.ApplicationModel/src/Abstractions/ILogSpaceResourceDescriptor.cs:8` and VpnGateway's equivalent add no command or typed DependsOn members.
11. **Web/VPN defaults:** `Sdk.Web/Targets/Sdk.Web.props:19` supplies http only, no certificate/mount. A TLS Web example must declare these. `Sdk.VpnGateway/Targets/Sdk.VpnGateway.props:12,19` forces non-composability and declares only http; there is no wireguard endpoint. Decide upstream defaults separately.
12. **Trust export:** `ApplicationModel.Gateway/src/Internal/LocalMountMaterializer.cs:159-168` and WindowsLocalFileProtector DPAPI-protect trust.pem; `Hosting.Resources/src/ResourceMount.cs:108-122` reads it through DPAPI. `Assimalign.Cohesion.Cli` has no `cohesion trust export`. There is no supported out-of-process export command for a Windows curl probe; this run used no trust bypass.
13. **Root convenience API:** `GatewaySourceWriter.cs:87-100` emits CreateBuilder only. `ApplicationModel/src/Application.cs:27`, `Application.CreateSet`, plus the hand-maintained `AddApplication` list remains required. No generated Gateway.CreateSet exists.
14. **Kubernetes render options:** `KubernetesGateway.System.cs:83` calls `Internal/KubernetesSystemInstallation.cs:31-38`, requiring a digest-pinned SystemImage and SystemStorageSize even offline. Both were supplied here, but resource-image validation still blocked output.
15. **Release promotion / branch:** nuget.org promotion of 10.0.1-preview.3 remains an orchestrator prerequisite; released-feed CI was not run. Actual examples branch main conflicts with the last run note's feature branch. Owner confirmation is needed before committing on main; no branch switch is allowed.

## Design/brief disagreements and explicit substitutions

- Design §4.3:257 / §4.5(c):321 retains `platform-configurationstore`; the ruling-required resource rename yields the intended accessor. Other sibling names remain an owner decision.
- `SecretStoreResourceCommandExtensions.AddSecret` takes a string source, rejects literal sources; `IssueCertificate` takes a slash-free name/subject/SANs. These differ from illustrative shorthand; only IssueCertificate was used.
- `ConfigurationStoreResourceCommandExtensions.AddNamespace` takes an `IReadOnlyDictionary<string,string?>`, not a seed filename. Existing JSON was read into dictionaries. `SetValue` accepts string/null only, including its JsonElement overload; no arbitrary structured values were sent.
- `IdentityHubResourceCommandExtensions.AddClient` takes clientId, a nonempty audiences sequence and credentialSource, not fluent AuthorizationCode/RedirectUri options. Confidential service clients replace the illustration and require supplied secrets. `RezolvrResourceCommandExtensions.AddARecord` takes an IPv4 IPAddress and DNS-valid name, not a resource endpoint; Local loopback records were used. These signatures live in each area's ApplicationModel `src/Extensions` project.
- The brief suggests AddDatabase("orders") on an already host-owned database and shared namespace declarations both in the host and gateway. `Assimalign.Cohesion.Database.Hosting/src/Internal/DatabaseResourceCommandHandler.cs:61` and `Assimalign.Cohesion.ConfigurationStore.Hosting/src/Internal/ConfigurationStoreRepository.cs:205-207` reject those ownership claims. Separate archive databases and host-owned networking/gateway-owned shared avoid duplicate ownership.
- `IApplicationEnvironment.IsLocal` and IsDevelopment are properties (`ApplicationModel/src/Abstractions/IApplicationEnvironment.cs:17,22`). Design line 276 still calls IsLocal(). Core constants and the landed property were used.
- Design line **354** still says `LogSpace now fails closed outside loopback Development`; the gate is now IsLocal. The carried-forward Development-era prose at lines 44, 70, 281, 295, 354 and 382 belongs in an item-27 documentation follow-up, not an examples workaround.
- Confirmed the corrected anchors: §4.4 heading at **299**, `Application.CreateSet` at **27**. The other brief paragraph still mentioning Application.cs:23 is superseded by its own C1 correction.
- Design/brief calls the gateway file Resources.g.cs; the actual target writes **Gateway.g.cs** (`Sdk.Gateway/Targets/Sdk.Gateway.targets:3,7`). Example comments now match actual output.
- Describe includes empty `certificate` strings on compiled non-TLS endpoints. This matches the 5dfa1e3c golden/serializer behavior and is not an undeclared-certificate defect.
- The brief expects runnable Local fallbacks and successful render from source-only manifests. Actual authenticated command and artifact.image gates prevent those claims; neither was bypassed.
- The requested literal all-tracked credential-pattern scan necessarily matches the guard workflow's own detection expression. The actual workflow intentionally excludes itself. Both scans were inspected; the workflow-equivalent scan has no matches. The guard was preserved.
- Root-only manual edits were maintained; the SDK's first-use development-certificate side effect is disclosed above. The attempted automatic parallel-agent facility was unavailable, so verification ran in this one workspace without subagents.

## Scope creep candidates not implemented

- Upstream SecretStore content-root initialization and SQL principal/foreign-key migrations.
- Typed external binders, same-application telemetry ordering ergonomics and cross-application telemetry support.
- Windows CLI trust export and generated application-set conveniences.
- Linux runtime-pack publication, resource image publication, platforms advisory remediation and released-feed promotion.
- The remaining SecretStore/LogSpace resource-name casing and stale signed-design API illustrations.

## Final file inventory and repository checks

The staged inventory below records only this item's files, including the six pure renames and 52 new profiles. Untracked Flows removals cannot appear in that diff. No owner-retained `_old/` or `.vs/` entry is staged. Git whitespace and credential checks are recorded with the final inventory.

```text
.github/workflows/build.yml
Assimalign.Cohesion.Examples.slnx
Directory.Build.props
Directory.Build.targets (deleted)
README.md
VERIFICATION.md (new)
examples/README.md
examples/k8s-federated/Identity/Example.Identity.Gateway/Example.Identity.Gateway.csproj
examples/k8s-federated/Identity/Example.Identity.Gateway/Program.cs
examples/k8s-federated/Identity/Example.Identity.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Identity/Example.Identity.IdentityHub/Example.Identity.IdentityHub.csproj
examples/k8s-federated/Identity/Example.Identity.IdentityHub/Program.cs
examples/k8s-federated/Identity/Example.Identity.IdentityHub/Properties/launchSettings.json (new)
examples/k8s-federated/Networking/Example.Networking.Gateway/Example.Networking.Gateway.csproj
examples/k8s-federated/Networking/Example.Networking.Gateway/Program.cs
examples/k8s-federated/Networking/Example.Networking.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Networking/Example.Networking.Rezolvr/Example.Networking.Rezolvr.csproj
examples/k8s-federated/Networking/Example.Networking.Rezolvr/Program.cs
examples/k8s-federated/Networking/Example.Networking.Rezolvr/Properties/launchSettings.json (new)
examples/k8s-federated/Networking/Example.Networking.VpnGateway/Example.Networking.VpnGateway.csproj
examples/k8s-federated/Networking/Example.Networking.VpnGateway/Program.cs
examples/k8s-federated/Networking/Example.Networking.VpnGateway/Properties/launchSettings.json (new)
examples/k8s-federated/Platform/Example.Platform.ConfigurationStore/Example.Platform.ConfigurationStore.csproj
examples/k8s-federated/Platform/Example.Platform.ConfigurationStore/Program.cs
examples/k8s-federated/Platform/Example.Platform.ConfigurationStore/Properties/launchSettings.json (new)
examples/k8s-federated/Platform/Example.Platform.Gateway/Example.Platform.Gateway.csproj
examples/k8s-federated/Platform/Example.Platform.Gateway/Program.cs
examples/k8s-federated/Platform/Example.Platform.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Platform/Example.Platform.LogSpace/Example.Platform.LogSpace.csproj
examples/k8s-federated/Platform/Example.Platform.LogSpace/Program.cs
examples/k8s-federated/Platform/Example.Platform.LogSpace/Properties/launchSettings.json (new)
examples/k8s-federated/Platform/Example.Platform.SecretStore/Example.Platform.SecretStore.csproj
examples/k8s-federated/Platform/Example.Platform.SecretStore/Program.cs
examples/k8s-federated/Platform/Example.Platform.SecretStore/Properties/launchSettings.json (new)
examples/k8s-federated/README.md
examples/k8s-federated/Zones/AppA/Example.AppA.Api/Example.AppA.Api.csproj
examples/k8s-federated/Zones/AppA/Example.AppA.Api/Program.cs
examples/k8s-federated/Zones/AppA/Example.AppA.Api/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppA/Example.AppA.Api/appsettings.Local.json (R100 rename)
examples/k8s-federated/Zones/AppA/Example.AppA.Database/Program.cs
examples/k8s-federated/Zones/AppA/Example.AppA.Database/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppA/Example.AppA.Gateway/Example.AppA.Gateway.csproj
examples/k8s-federated/Zones/AppA/Example.AppA.Gateway/Program.cs
examples/k8s-federated/Zones/AppA/Example.AppA.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppA/Example.AppA.SecretStore/Example.AppA.SecretStore.csproj
examples/k8s-federated/Zones/AppA/Example.AppA.SecretStore/Program.cs
examples/k8s-federated/Zones/AppA/Example.AppA.SecretStore/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppA/Example.AppA.Spa/Example.AppA.Spa.csproj
examples/k8s-federated/Zones/AppA/Example.AppA.Spa/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppB/Example.AppB.Api/Example.AppB.Api.csproj
examples/k8s-federated/Zones/AppB/Example.AppB.Api/Program.cs
examples/k8s-federated/Zones/AppB/Example.AppB.Api/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppB/Example.AppB.Api/appsettings.Local.json (R100 rename)
examples/k8s-federated/Zones/AppB/Example.AppB.Database/Program.cs
examples/k8s-federated/Zones/AppB/Example.AppB.Database/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppB/Example.AppB.Gateway/Example.AppB.Gateway.csproj
examples/k8s-federated/Zones/AppB/Example.AppB.Gateway/Program.cs
examples/k8s-federated/Zones/AppB/Example.AppB.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppB/Example.AppB.SecretStore/Example.AppB.SecretStore.csproj
examples/k8s-federated/Zones/AppB/Example.AppB.SecretStore/Program.cs
examples/k8s-federated/Zones/AppB/Example.AppB.SecretStore/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppB/Example.AppB.Spa/Example.AppB.Spa.csproj
examples/k8s-federated/Zones/AppB/Example.AppB.Spa/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppC/Example.AppC.Api/Example.AppC.Api.csproj
examples/k8s-federated/Zones/AppC/Example.AppC.Api/Program.cs
examples/k8s-federated/Zones/AppC/Example.AppC.Api/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppC/Example.AppC.Api/appsettings.Local.json (R100 rename)
examples/k8s-federated/Zones/AppC/Example.AppC.Database/Program.cs
examples/k8s-federated/Zones/AppC/Example.AppC.Database/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppC/Example.AppC.Gateway/Example.AppC.Gateway.csproj
examples/k8s-federated/Zones/AppC/Example.AppC.Gateway/Program.cs
examples/k8s-federated/Zones/AppC/Example.AppC.Gateway/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppC/Example.AppC.SecretStore/Example.AppC.SecretStore.csproj
examples/k8s-federated/Zones/AppC/Example.AppC.SecretStore/Program.cs
examples/k8s-federated/Zones/AppC/Example.AppC.SecretStore/Properties/launchSettings.json (new)
examples/k8s-federated/Zones/AppC/Example.AppC.Spa/Example.AppC.Spa.csproj
examples/k8s-federated/Zones/AppC/Example.AppC.Spa/Properties/launchSettings.json (new)
examples/k8s/Gateway/Example.Gateway/Example.Gateway.csproj
examples/k8s/Gateway/Example.Gateway/Program.cs
examples/k8s/Gateway/Example.Gateway/Properties/launchSettings.json (new)
examples/k8s/Identity/Example.Identity.Gateway/Example.Identity.Gateway.csproj
examples/k8s/Identity/Example.Identity.Gateway/Program.cs
examples/k8s/Identity/Example.Identity.Gateway/Properties/launchSettings.json (new)
examples/k8s/Identity/Example.Identity.IdentityHub/Example.Identity.IdentityHub.csproj
examples/k8s/Identity/Example.Identity.IdentityHub/Program.cs
examples/k8s/Identity/Example.Identity.IdentityHub/Properties/launchSettings.json (new)
examples/k8s/Networking/Example.Networking.Gateway/Example.Networking.Gateway.csproj
examples/k8s/Networking/Example.Networking.Gateway/Program.cs
examples/k8s/Networking/Example.Networking.Gateway/Properties/launchSettings.json (new)
examples/k8s/Networking/Example.Networking.Rezolvr/Example.Networking.Rezolvr.csproj
examples/k8s/Networking/Example.Networking.Rezolvr/Program.cs
examples/k8s/Networking/Example.Networking.Rezolvr/Properties/launchSettings.json (new)
examples/k8s/Networking/Example.Networking.VpnGateway/Example.Networking.VpnGateway.csproj
examples/k8s/Networking/Example.Networking.VpnGateway/Program.cs
examples/k8s/Networking/Example.Networking.VpnGateway/Properties/launchSettings.json (new)
examples/k8s/Platform/Example.Platform.ConfigurationStore/Example.Platform.ConfigurationStore.csproj
examples/k8s/Platform/Example.Platform.ConfigurationStore/Program.cs
examples/k8s/Platform/Example.Platform.ConfigurationStore/Properties/launchSettings.json (new)
examples/k8s/Platform/Example.Platform.Gateway/Example.Platform.Gateway.csproj
examples/k8s/Platform/Example.Platform.Gateway/Program.cs
examples/k8s/Platform/Example.Platform.Gateway/Properties/launchSettings.json (new)
examples/k8s/Platform/Example.Platform.LogSpace/Example.Platform.LogSpace.csproj
examples/k8s/Platform/Example.Platform.LogSpace/Program.cs
examples/k8s/Platform/Example.Platform.LogSpace/Properties/launchSettings.json (new)
examples/k8s/Platform/Example.Platform.SecretStore/Example.Platform.SecretStore.csproj
examples/k8s/Platform/Example.Platform.SecretStore/Program.cs
examples/k8s/Platform/Example.Platform.SecretStore/Properties/launchSettings.json (new)
examples/k8s/README.md
examples/k8s/Zones/AppA/Example.AppA.Api/Example.AppA.Api.csproj
examples/k8s/Zones/AppA/Example.AppA.Api/Program.cs
examples/k8s/Zones/AppA/Example.AppA.Api/Properties/launchSettings.json (new)
examples/k8s/Zones/AppA/Example.AppA.Api/appsettings.Local.json (R100 rename)
examples/k8s/Zones/AppA/Example.AppA.Database/Program.cs
examples/k8s/Zones/AppA/Example.AppA.Database/Properties/launchSettings.json (new)
examples/k8s/Zones/AppA/Example.AppA.Gateway/Example.AppA.Gateway.csproj
examples/k8s/Zones/AppA/Example.AppA.Gateway/Program.cs
examples/k8s/Zones/AppA/Example.AppA.Gateway/Properties/launchSettings.json (new)
examples/k8s/Zones/AppA/Example.AppA.SecretStore/Example.AppA.SecretStore.csproj
examples/k8s/Zones/AppA/Example.AppA.SecretStore/Program.cs
examples/k8s/Zones/AppA/Example.AppA.SecretStore/Properties/launchSettings.json (new)
examples/k8s/Zones/AppA/Example.AppA.Spa/Example.AppA.Spa.csproj
examples/k8s/Zones/AppA/Example.AppA.Spa/Properties/launchSettings.json (new)
examples/k8s/Zones/AppB/Example.AppB.Api/Example.AppB.Api.csproj
examples/k8s/Zones/AppB/Example.AppB.Api/Program.cs
examples/k8s/Zones/AppB/Example.AppB.Api/Properties/launchSettings.json (new)
examples/k8s/Zones/AppB/Example.AppB.Api/appsettings.Local.json (R100 rename)
examples/k8s/Zones/AppB/Example.AppB.Database/Program.cs
examples/k8s/Zones/AppB/Example.AppB.Database/Properties/launchSettings.json (new)
examples/k8s/Zones/AppB/Example.AppB.Gateway/Example.AppB.Gateway.csproj
examples/k8s/Zones/AppB/Example.AppB.Gateway/Program.cs
examples/k8s/Zones/AppB/Example.AppB.Gateway/Properties/launchSettings.json (new)
examples/k8s/Zones/AppB/Example.AppB.SecretStore/Example.AppB.SecretStore.csproj
examples/k8s/Zones/AppB/Example.AppB.SecretStore/Program.cs
examples/k8s/Zones/AppB/Example.AppB.SecretStore/Properties/launchSettings.json (new)
examples/k8s/Zones/AppB/Example.AppB.Spa/Example.AppB.Spa.csproj
examples/k8s/Zones/AppB/Example.AppB.Spa/Properties/launchSettings.json (new)
examples/k8s/Zones/AppC/Example.AppC.Api/Example.AppC.Api.csproj
examples/k8s/Zones/AppC/Example.AppC.Api/Program.cs
examples/k8s/Zones/AppC/Example.AppC.Api/Properties/launchSettings.json (new)
examples/k8s/Zones/AppC/Example.AppC.Api/appsettings.Local.json (R100 rename)
examples/k8s/Zones/AppC/Example.AppC.Database/Program.cs
examples/k8s/Zones/AppC/Example.AppC.Database/Properties/launchSettings.json (new)
examples/k8s/Zones/AppC/Example.AppC.Gateway/Example.AppC.Gateway.csproj
examples/k8s/Zones/AppC/Example.AppC.Gateway/Program.cs
examples/k8s/Zones/AppC/Example.AppC.Gateway/Properties/launchSettings.json (new)
examples/k8s/Zones/AppC/Example.AppC.SecretStore/Example.AppC.SecretStore.csproj
examples/k8s/Zones/AppC/Example.AppC.SecretStore/Program.cs
examples/k8s/Zones/AppC/Example.AppC.SecretStore/Properties/launchSettings.json (new)
examples/k8s/Zones/AppC/Example.AppC.Spa/Example.AppC.Spa.csproj
examples/k8s/Zones/AppC/Example.AppC.Spa/Properties/launchSettings.json (new)
examples/single-app/Acme.Api/Properties/launchSettings.json (new)
examples/single-app/Acme.Database/Program.cs
examples/single-app/Acme.Database/Properties/launchSettings.json (new)
examples/single-app/Acme.Gateway/Acme.Gateway.csproj
examples/single-app/Acme.Gateway/Properties/launchSettings.json (new)
examples/single-app/README.md
nuget.config
setup.ps1
```

Repository checks: `git diff --check` and `git diff --cached --check` pass. The workflow-equivalent credential scan returns no matches (exit 1); the literal all-tracked scan matches only the unchanged guard workflow itself. No stale bridge/casing/no-op endpoint patterns remain in example code. The two deleted Flows folders are absent. All 48 shared Program/csproj twins still match across k8s and k8s-federated.

Commit: **not committed**. The actual branch remains main; confirmation to commit there is pending because the final run note instead specified feature/dx-design-buildout and forbade switching branches. All item files are staged; no unrelated untracked files remain.
