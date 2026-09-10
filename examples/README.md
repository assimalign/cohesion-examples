# Overview

The idea of cohesion is to be able to deploy a cloud-like platform with built-in resources to a hosting platform like
Kubernetes, Docker, etc.

The concept is a referenced architecture that can be deployed regardless of the hosting model, which is where the
ApplicationModel implementation comes into play: think of the Azure Landing Zone design areas, created in code, so entire
areas can be pulled down and run locally while referencing remote aspects of a given architecture.

| Folder | Topology | Gateways | Namespaces / clusters |
| --- | --- | --- | --- |
| `single-app/` | 1–3 (in-process → local → Docker; topology 0 is `Acme.Api` alone with `EmbeddedDatabase`, not scaffolded) | `Acme.Gateway` | one process or one Docker network |
| `k8s/` | 4 (one cluster) | root `Example.Gateway` (application set, `cohesion-system`) + one gateway per area/zone for development | `identity`, `networking`, `platform`, `appa`, `appb`, `appc` in `cluster-01` |
| `k8s-federated/` | 5 (federated) | one gateway per area, one per zone app | Platform → `cluster-03`, Identity → `cluster-01`, Networking → `cluster-02`, Zones → `cluster-04` |

Folder nesting never expresses ownership; a gateway owns what it references. The earlier nested
`Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees were removed.
