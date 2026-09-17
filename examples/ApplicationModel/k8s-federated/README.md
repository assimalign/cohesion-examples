# Federated landing zone

These 24 projects mirror k8s without its root application-set gateway. Every resource has a `Program.cs`, explicit orchestration opt-in and the generated default control plane. Every executable's launch profile selects Local. The area gateways own their own applications.

| Area | Target cluster | Application | Target peer control plane |
| --- | --- | --- | --- |
| Platform | cluster-03 | platform | https://platform.example.com/cohesion |
| Identity | cluster-01 | identity | https://identity.example.com/cohesion |
| Networking | cluster-02 | networking | https://networking.example.com/cohesion |
| Zones | cluster-04 | appa, appb, appc | https://appa.example.com/cohesion and peers |

These are deployment destinations, not verified live clusters. Project references crossing an application boundary generate Externals; each consumer binds them through its peer gateway. The Local endpoint fallbacks remain useful for inspecting bindings but do not replace an authorized remote command channel.

Zones declare ConfigurationStore namespaces through the typed external binder. IdentityHub and Rezolvr have no equivalent binder, so Identity's gateway temporarily declares zone audiences/confidential service clients and Networking's gateway declares Local loopback A records. Certificates are requested on each zone's own SecretStore. API endpoint names stay http while their scheme is HTTPS; SPAs retain HTTP. See the [k8s composition notes](../k8s/README.md) for seed ownership, certificate sources and named-secret prerequisites.

Platform ConfigurationStore is ordered after LogSpace for same-application telemetry. Zone APIs retain console logging because remote LogSpace injection is unavailable. Rezolvr, VpnGateway and LogSpace resource builders expose no domain verbs yet. The shared SecretStore content-root and SQL migration blockers prevented live readiness and telemetry verification; the [verification report](../../VERIFICATION.md) records the exact failures.

```powershell
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway local --mode describe
dotnet run --project examples/k8s-federated/Platform/Example.Platform.Gateway -- --gateway local --mode describe
dotnet run --project examples/k8s-federated/Zones/AppA/Example.AppA.Gateway -- --gateway docker --mode render
```

Zone gateways select both platform providers; both renderers require published resource images, and Kubernetes additionally needs a digest-pinned system image and storage size. The current manifests lack those resource images, so render stops before YAML output. Neither renderer contacts a daemon or cluster. Identity and Platform select Local/InProcess; Networking stays Local because VPN is non-composable. Use `dotnet run --no-launch-profile` with shell environment overrides. The [root README](../../README.md) covers portable feeds, deployed Development behavior and the released-feed CI publication dependency.

Federation trust grants remain explicit and directional. Use the shipped `cohesion trust` CLI with the peer gateway and allowed command kinds; a model description or offline render proves neither trust nor remote mutation delivery.
