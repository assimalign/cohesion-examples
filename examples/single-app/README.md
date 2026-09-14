# Single application

Acme has two resources and one gateway. Both resources are ordinary `Program.cs` executables with explicit orchestration opt-in. The SDK supplies their build/runtime defaults and generated control planes.

```powershell
dotnet run --project Acme.Gateway -- --gateway local --mode run
dotnet run --project Acme.Gateway -- --gateway inprocess --mode run
dotnet run --project Acme.Gateway -- --gateway docker --mode render
```

Local supervises two child processes; InProcess invokes both entries in one gateway process. Acme.Api deliberately keeps plain HTTP as the scale-down story. Its `/bindings` response reports the observed database endpoint and the generated Customers:PageSize default.

The verified package revision builds and describes this model, but both run modes stop at database readiness: the SQL DDL executor cannot migrate the schema's principals and grants yet. The explicit Customers table name now matches its grant; the example retains the intended permissions. No successful endpoint probe was established in this pass.

Docker and Kubernetes are selected provider packages at `10.0.1-preview.3`. Both renderers require published resource images in the manifests before they can produce YAML. Kubernetes rendering also requires `--cohesion-system-image <digest-pinned-image>` and `--cohesion-system-storage 1Gi`; see the [root README](../../README.md). Neither offline render needs a daemon or cluster. Kubernetes selects JIT for the gateway.

Every project has a Local launch profile. Use `dotnet run --no-launch-profile` when selecting the environment through shell variables. The `cohesion` CLI can run the same gateway and report its status. Topology 0, an API with an embedded database and no gateway, is not materialized here.
