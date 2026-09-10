# `single-app/` — one repo, one gateway, two resources

The two-person-company story: an API and a database, run as **one process** by default, as containers when wanted, and
later on Kubernetes without touching application code. Both resources are ordinary executables with a `Program.cs`;
each opts in to orchestration with one csproj line so `Acme.Gateway` can reference them.

```bash
dotnet run --project Acme.Gateway                        # one process (InProcess is the default here): Acme.Api + Acme.Database under one gateway
dotnet run --project Acme.Gateway -- --gateway local     # two supervised processes
dotnet run --project Acme.Gateway -- --gateway docker    # two containers on network `acme`
dotnet run --project Acme.Api                            # the API alone, as a plain executable (no gateway; Resource.* falls back to DevPort and appsettings)
dotnet publish Acme.Gateway -t:CohesionPublishApplication   # images + application.images.json (OCI archives on disk until CohesionContainerRegistry is set)
```

To move to Kubernetes later: add `Kubernetes` to `CohesionGateways` in `Acme.Gateway.csproj` (the gateway executable
becomes JIT; the resources stay NativeAOT), set `CohesionContainerRegistry` in `Directory.Build.props`, then
`--gateway kubernetes --mode apply`. Topology 0 — `Acme.Api` alone with `EmbeddedDatabase` and no gateway — is the step
below this one and is not scaffolded here.
