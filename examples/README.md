# Overview

The idea of cohesion is to be able to deploy a cloud-like platform with built-in resources to a hosting platform like
Kubernetes, Docker, etc.

The concept is a referenced architecture that can be deployed regardless of the hosting model, which is where the
ApplicationModel implementation comes into play: think of the Azure Landing Zone design areas, created in code, so an
area can eventually be pulled down while remote aspects of the architecture stay in place. The current package set runs
the Web/Database zone subset; generic area resources can describe their models but still await runtime control planes.

| Folder | Topology | Gateways | Namespaces / clusters |
| --- | --- | --- | --- |
| `single-app/` | Local → InProcess today; Docker is a later provider | `Acme.Gateway` | N supervised processes or one process |
| `k8s/` | one target cluster; zone subset runs locally today | root `Example.Gateway` application-set shape + one gateway per area/zone | target namespaces `identity`, `networking`, `platform`, `appa`, `appb`, `appc` |
| `k8s-federated/` | one target cluster per area; zone subset runs locally today | one gateway per area, one per zone app | target mapping: Platform → `cluster-03`, Identity → `cluster-01`, Networking → `cluster-02`, Zones → `cluster-04` |

The rejected shape is explicit:

> folder nesting never expresses ownership; the nested `Example.AppB.Gateway/Example.AppB.*` and copy-pasted `AppC` trees are removed; per-area gateways were added for Identity/Networking/Platform.

A gateway owns what it references. Run `pwsh ../setup.ps1` from this directory after changing project membership, or
`pwsh ../setup.ps1 -Check` to verify the generated solutions.
