# ApplicationModel examples

One shop on the restarted Application Model ([assimalign/cohesion#1759](https://github.com/assimalign/cohesion/pull/1759)), on two platforms. In both, a gateway program declares the application, `orders-api` with two instances and a `storefront` whose `Orders:Url` setting is `orders-api`'s endpoint, and runs it on its platform. Each resource is an ordinary Web program that makes itself a resource with `builder.Services.AddResource(name, resource => resource.Use<Platform>())`, and runs as a plain application outside a gateway.

| Example | Platform | Gateway half | Instance half |
| --- | --- | --- | --- |
| [web-on-docker](web-on-docker/) | A Docker-compatible engine on this machine, Podman's included: one container per replica | `builder.UseDocker(docker)`, `WithDockerReplicas(2)` | `resource.UseDocker()` |
| [web-on-kubernetes](web-on-kubernetes/) | A Kubernetes cluster, through its API server: a Deployment and a Service per resource | `builder.UseKubernetes(kubernetes)`, `WithReplicas(2)` | `resource.UseKubernetes()` |

The platform packages come from cohesion-platforms. Each example pins its cohesion build in its own `global.json` and its cohesion-platforms build in its own `Directory.Build.props`, keeps its own `Shop.slnx`, and shares nothing with the other; neither is part of the root solution. Its README says how to build its images and run it.
