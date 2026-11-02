# Web on Docker

A gateway and two Web resources on the restarted Application Model ([assimalign/cohesion#1759](https://github.com/assimalign/cohesion/pull/1759)), running as containers on a Docker-compatible engine on this machine, Docker or a Podman machine, or, with no engine at all, inside the gateway's own process.

| Project | What it is |
| --- | --- |
| [Shop.Gateway](Shop.Gateway/Program.cs) | The gateway: declares the application and runs it on the engine with `builder.UseDocker(docker => ...)`, or in its own process with `builder.UseInProcess()` when it is started with `--in-process`. An `Assimalign.Cohesion.Sdk.Gateway` executable that references both programs. |
| [Shop.Orders](Shop.Orders/Program.cs) | `orders-api`: a Web program that registers itself as a resource with `builder.Services.AddResource("orders-api", resource => resource.UseDocker().UseInProcess())`. Two instances on the engine (`WithDockerReplicas(2)`). |
| [Shop.Storefront](Shop.Storefront/Program.cs) | `storefront`: a Web program whose `Orders:Url` setting is `orders-api`'s endpoint, which it calls. |

## Packages

Everything restores from the Assimalign GitHub Packages feed, at two CI builds:

- [`global.json`](global.json) pins the Cohesion SDKs to a `main` CI build of cohesion, taken after [#1759](https://github.com/assimalign/cohesion/pull/1759) and [#1784](https://github.com/assimalign/cohesion/pull/1784) merged. The in-process platform's packages, `Assimalign.Cohesion.ApplicationModel.Gateway.InProcess` and `Assimalign.Cohesion.ApplicationModel.Hosting.InProcess`, come from the same build: the projects reference them at the SDK's version, `$(CohesionVersion)`.
- [`Directory.Build.props`](Directory.Build.props) pins `CohesionPlatformsVersion` to a CI build of cohesion-platforms' [#36](https://github.com/assimalign/cohesion-platforms/pull/36), which stacks on [#35](https://github.com/assimalign/cohesion-platforms/pull/35), until those pull requests merge: cohesion-platforms has no `main` build of this code yet. That build depends on exactly the cohesion build above, so the two pins move together.

A pull request's CI packages are deleted when it closes, so move the platforms pin to a cohesion-platforms `main` build once #36 and #35 merge. Cohesion keeps only its newest 20 `main` builds, so move the cohesion pin to a newer one before it ages out; the two pins still move together. This folder has its own `global.json`, so it is not part of the root solution.

The feed needs a token with `read:packages`, even for reads, and `gh auth login` does not grant that scope by default. Add it once, then hand the token to the root `nuget.config` through the environment:

```powershell
gh auth refresh -h github.com -s read:packages
$env:GITHUB_USERNAME = '<github-user>'
$env:GITHUB_PASSWORD = (gh auth token)
```

## Run it in process

This needs only the .NET SDK `10.0.300` or later, which [`global.json`](global.json) requires. From this folder:

```powershell
dotnet run --project Shop.Gateway -- --in-process
```

The gateway runs both Web programs inside its own process, each on a thread of its own and a loopback port of its own. It deploys `orders-api`, waits for it to publish, deploys `storefront` with `orders-api`'s endpoint as its `Orders:Url` setting, and prints each resource's endpoint:

```text
The shop runs in this process:
  orders-api   http://127.0.0.1:61625  1 instance(s) published
  storefront   http://127.0.0.1:61628  1 instance(s) published
Try: curl http://127.0.0.1:61628
Ctrl+C stops the gateway and both programs.
```

The ports change from run to run. In another shell, request the storefront at the address the gateway printed:

```powershell
curl http://127.0.0.1:61628
```

The answer is the storefront's line, followed by what `orders-api` returned to it. Both programs run in one process, so both name this machine:

```text
storefront (<machine>) -> orders-api: {"served-by":"<machine>","orders":[{"id":1,"item":"coffee"},{"id":2,"item":"tea"}]}
```

In process, every resource runs once: `WithDockerReplicas(2)` is the Docker platform's setting, and no other platform reads it. Ctrl+C stops the gateway, and disposing it stops both programs, `storefront` first.

In `Program.cs` that is the gateway's lifecycle: `ApplicationGateway.CreateBuilder("shop")` returns an `ApplicationGatewayBuilder`, `await gateway.StartAsync()` returns once every resource has published, each `IApplicationGatewayDeployment` in `gateway.Resources` carries its outputs and instances, and `await gateway.Completion` waits until the run ends, faulting with its failure. The gateway is `await using`, so leaving the program disposes it, and with it the platform it built.

Nothing in either program changes for this. [`Shop.Gateway.csproj`](Shop.Gateway/Shop.Gateway.csproj) references the in-process platform's gateway half and both programs, and names the resource each one is:

```xml
<ProjectReference Include="..\Shop.Orders\Shop.Orders.csproj" CohesionResource="orders-api" />
<ProjectReference Include="..\Shop.Storefront\Shop.Storefront.csproj" CohesionResource="storefront" />
```

Each program references the platform's instance half and opts into it beside Docker, with `resource.UseDocker().UseInProcess()`. The build binds each program's top-level statements to its resource, so the gateway's bare `builder.UseInProcess()` runs both. The platform is the gateway's choice when it runs: the same build runs on the engine without `--in-process`.

## Run it on Podman or Docker

```mermaid
flowchart LR
    Gateway["Shop.Gateway"] -->|"containers, network"| Engine["Docker engine API"]
    Gateway -->|"polls and calls each control-plane port"| Orders["orders-api containers"]
    Gateway -->|"polls and calls each control-plane port"| Storefront["storefront container"]
    Storefront -->|"Orders:Url = http://orders-api:8080"| Orders
```

The gateway runs on the engine's host and pulls. Each resource is one container per replica on the gateway's network, `shop`, under the resource's name, so `orders-api`'s endpoint is `http://orders-api:8080` on that network. The engine publishes every container's `http` port (8080) and control-plane port (9090) on loopback ports of this machine. The gateway polls each container's control-plane port for its instance's publication and renewals, and calls its control plane there; a container needs no address of its gateway. A program serves on its `http` endpoint only after it has published.

Prerequisites:

- The .NET SDK `10.0.300` or later, which [`global.json`](global.json) requires.
- Podman with a running machine (`podman machine init`, then `podman machine start`), or Docker.

The gateway reaches the engine at `--endpoint`, else `DOCKER_HOST`, else the platform's default: on Windows the `docker_engine` pipe, elsewhere the Docker socket. Pass `--endpoint` in the docker CLI's form (`unix:///run/podman/podman.sock`, for example) for any other engine.

The images and the gateway must use the same engine. `publish-images.ps1` uses Podman when it is on `PATH`, else Docker; `-Engine Docker` or `-Engine Podman` picks one. A Podman machine on Windows serves the `docker_engine` pipe only when no other engine holds it, so with Docker Desktop also installed, either publish with `-Engine Docker`, or point the gateway at the Podman machine's own pipe, which `podman machine inspect --format "{{.ConnectionInfo.PodmanPipe.Path}}"` prints:

```powershell
dotnet run --project Shop.Gateway -- --endpoint npipe:////./pipe/podman-machine-default
```

From this folder:

```powershell
./publish-images.ps1               # build both programs as images for the engine's architecture and load them into its store
dotnet run --project Shop.Gateway
```

`builder.UseDocker(docker => ...)` sets the engine's options on `docker.Options` (the endpoint, the network `shop`, the image pull policy) and binds each resource to its image with `docker.AddImage(...)`. The gateway builds that infrastructure when it is built, and disposing the gateway disposes it, which stops polling the network and leaves what it deployed running.

The gateway deploys `orders-api`, waits for an instance to publish, deploys `storefront` with `orders-api`'s endpoint as its setting, and prints each resource's endpoint and published instances. Then, in another shell (`docker` takes the same arguments):

```powershell
podman ps --filter label=com.assimalign.cohesion.network=shop --format "{{.ID}} {{.Names}} {{.Ports}}"
curl http://127.0.0.1:<port>      # the loopback port the shop-storefront container's 8080 is published on
```

The answer names the storefront container and the `orders-api` container that served it by their host names, which are the container ids in the first column. Ctrl+C stops the gateway, and what it deployed keeps running. Running the gateway again adopts it, and replaces only a resource whose image reference, ports, replica count, or settings changed.

The image reference is the tag, `shop-orders:latest`, not the image it names, so republishing after a code change does not replace the running containers. Remove the shop before running the gateway again:

```powershell
$containers = podman ps -aq --filter label=com.assimalign.cohesion.network=shop
if ($containers) { podman rm -f $containers }
podman network rm shop
```

To remove the images as well:

```powershell
podman rmi shop-orders:latest shop-storefront:latest
```

## Each program on its own

Each Web program is also a plain application outside a gateway:

```powershell
dotnet run --project Shop.Orders
```
