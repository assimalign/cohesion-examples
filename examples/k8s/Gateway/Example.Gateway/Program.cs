using Assimalign.Cohesion.ApplicationModel;

// One gateway over the whole cluster. Each member is an area gateway referenced above; Applications.<Name> is generated
// from its manifest, and its model is obtained through that gateway's own control plane (`--mode describe`) — every
// gateway is an ordinary executable, so nothing is named twice here. Crossings between member applications
// (appa → identity, identity → platform, …) resolve inside the set without a control-plane hop; the RemoteReference
// bindings in each area's Program.cs only apply when that area runs alone. Platform first: root of trust.
IApplicationSetBuilder set = Gateway.CreateSetBuilder(args);          // generated: application set "cohesion-system"

set.AddApplication(Applications.Platform);
set.AddApplication(Applications.Identity);
set.AddApplication(Applications.Networking);
set.AddApplication(Applications.AppA);
set.AddApplication(Applications.AppB);
set.AddApplication(Applications.AppC);

set.UseGateway(args);            // --gateway kubernetes --context cluster-01 (Local: every application becomes a process set)
await set.Build().RunAsync();    // --mode apply reconciles and exits (CI/GitOps); --mode bootstrap emits ClusterRole/ClusterRoleBinding/ServiceAccount + the cohesion-system Deployment
