# One landing-zone architecture

Each area declares one `CohesionApplication` in `Directory.Build.props`. All 25 projects are executable and have Local launch profiles; every resource opts in to the SDK-generated manifest, accessors and default control plane.

| Area | Application | Resources | Gateway |
| --- | --- | --- | --- |
| Platform | platform | SecretStore, ConfigurationStore, LogSpace | Example.Platform.Gateway |
| Identity | identity | IdentityHub | Example.Identity.Gateway |
| Networking | networking | Rezolvr, VpnGateway | Example.Networking.Gateway |
| Zones/AppA | appa | API, SPA, Database, SecretStore | Example.AppA.Gateway |
| Zones/AppB | appb | Same shape, billing schema | Example.AppB.Gateway |
| Zones/AppC | appc | Same shape, inventory schema | Example.AppC.Gateway |
| Gateway | cohesion-system | Six application declarations | Example.Gateway |

The root hand-writes `Application.CreateSet(new LocalGateway(...), args)` and adds generated `Applications.Platform`, `Identity`, `Networking`, `AppA`, `AppB`, and `AppC`. There is no generated `Gateway.CreateSet`. Describe resolves the six area models through their gateway model/control-plane seam. The root selects Local deliberately; it does not claim a live Kubernetes deployment.

The rejected shape is explicit:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

## Commands and ownership

Zones use `RemoteReferenceConfigurationStore(Externals.PlatformConfigurationStore, ...)` and declare their own namespace from the existing Configuration JSON, flattened to string values. The PageSize entry is a configuration seed; the API's generated setting remains its local default, and this sample does not implement live configuration consumption. The resource name is `platform-configuration-store`, so both `Externals.PlatformConfigurationStore` and `Resource.References.PlatformConfigurationStore` have the intended spelling.

§4.3 assigns IdentityHub and Rezolvr declarations to the consuming application. Their ApplicationModel packages ship no `RemoteReferenceIdentityHub` or `RemoteReferenceRezolvr` binder, so those commands are declared by their owning gateways until that gap closes. Identity declares each zone's audience and a confidential service client using its own named secret mount. These are not browser authorization-code clients: the illustrated fluent client API is absent. Networking declares Local-only IPv4 loopback A records for appa-dev, appb-dev and appc-dev.

Each zone gateway adds its SecretStore and requests an API certificate, then orders Database → API → SPA with explicit `DependsOn` edges. The schema-owned orders/billing/inventory databases stay in the resource programs; typed `AddDatabase` commands provision separate archive databases because a command cannot claim a code-owned database. Platform's gateway alone declares shared from shared.json; the resource program seeds networking from networking.json, avoiding duplicate namespace ownership.

Full runs require a reachable and authorized Platform command channel. The localhost external fallbacks preserve standalone bindings but cannot provide a missing peer gateway. Identity additionally needs the existing Platform signing source and parameter mounts appa-client, appb-client and appc-client. VPN keys retain their Platform source. No credential material is checked in.

At the verified package revision, Local and InProcess runs stop before readiness: SecretStore's host content root disagrees with its ambient resource context, and the SQL migration executor rejects declared principals/grants. The examples keep those schema permissions. Explicit table names now match the grants, correcting a separate example-side validation error. These upstream failures prevented endpoint probes and telemetry delivery checks; HTTPS declarations and dependency ordering are verified in the model only.

## TLS and telemetry

Each zone API updates the SDK's existing endpoint with `Scheme="https" Certificate="tls" ContainerPort="8443"` and includes a Secret mount sourced from `<app>-secretstore:certs/<app>-api`. The endpoint name remains http because Web's ambient binder and default control plane select that name first; there is no second plain-HTTP API listener. Certificate requests include localhost and 127.0.0.1 SANs. SPAs and Acme remain HTTP.

Platform orders `LogSpace.DependsOn(secrets)` and `ConfigurationStore.DependsOn(secrets, logs)`. ConfigurationStore is the demonstrated telemetry producer. SecretStore must bootstrap the sink first, so it retains console logging. This ordering covers producers **inside the platform application only**. Zone APIs keep console logging: cross-application telemetry injection from a remote LogSpace and cross-application DependsOn do not exist at this version. Hosting configures OTLP/HTTP JSON automatically; there is no resource AddTelemetry call, and otlp-grpc is reserved but rejected.

SDK TLS mounts on IdentityHub and ConfigurationStore are updated, never duplicated. LogSpace defaults to HTTPS otlp:4318 and query:8443. Rezolvr declares separate dns/UDP and dns-tcp/TCP endpoints and has no data mount. VpnGateway exposes only the SDK's HTTP control plane; its old wireguard Update was a no-op and was removed. Rezolvr, VpnGateway and LogSpace builders currently have no domain-composition verbs.

```powershell
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --mode describe
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode run
dotnet run --project examples/k8s/Zones/AppA/Example.AppA.Gateway -- --gateway inprocess --mode run
dotnet run --project examples/k8s/Gateway/Example.Gateway -- --mode describe
```

Zone gateways select Local, InProcess, Docker and Kubernetes. Networking remains Local because VPN is non-composable. See the [root README](../../README.md) for exact offline rendering arguments, package setup, CLI usage and the shell-environment `--no-launch-profile` requirement. Both render attempts currently reject the missing resource `artifact.image`; published Linux images are a prerequisite, even for offline rendering.
