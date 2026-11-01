# cohesion-examples

The examples mirror the cohesion repository's layout: `examples/libraries/<Area>` for library areas and `examples/resources/<Area>` for resource areas.

- **[Web](examples/resources/Web/)**: two small applications for each Web package, 69 projects in all. Each one is a single `Program.cs`; its [README](examples/resources/Web/README.md) lists them.
- **[ApplicationModel](examples/libraries/ApplicationModel/)**: one shop, a gateway and two Web resources, run by a gateway on a platform: [web-on-docker](examples/libraries/ApplicationModel/web-on-docker/) on a Docker-compatible engine, Podman's included, and [web-on-kubernetes](examples/libraries/ApplicationModel/web-on-kubernetes/) on a Kubernetes cluster. Both also run inside the gateway's own process, with no engine or cluster.

## Packages

Every `Assimalign.Cohesion.*` package, the SDKs included, comes from the Assimalign GitHub Packages feed (`https://nuget.pkg.github.com/assimalign/index.json`), which `nuget.config` maps them to. Cohesion's CI publishes each `main` commit there as `10.0.0-preview.0.ci.<run>` and each same-repository pull request commit as `10.0.0-preview.0.pr.<pr>.<run>`. The root `global.json` pins the Cohesion SDKs the Web examples build on, and moving the pin moves every one of them to another build. Only the newest 20 `main` builds are kept, and a pull request's builds are deleted when it closes, so an old pin stops restoring.

The root pin is a build of cohesion's [#1759](https://github.com/assimalign/cohesion/pull/1759), the application-model restart, whose Web area places every feature with a parameterless `Use<Feature>()` and configures it on its `builder.Services.Add<Feature>(...)` registration. Move it to a `main` build once that pull request merges.

Each ApplicationModel example has its own `global.json` and `Directory.Build.props`, which pin the cohesion and cohesion-platforms CI builds it runs on, and its own solution, so it is not part of the root solution.

GitHub Packages needs a token with `read:packages` even to read public packages, and `gh auth login` does not grant that scope by default. Supply the token through the environment, never in a tracked file: `nuget.config`'s credential block names only the variables `GITHUB_USERNAME` and `GITHUB_PASSWORD`, and the credential guard rejects any other password value or token.

```powershell
gh auth refresh -h github.com -s read:packages
$env:GITHUB_USERNAME = '<github-user>'
$env:GITHUB_PASSWORD = (gh auth token)
```

or, equivalently, from NuGet's own variable:

```bash
export NuGetPackageSourceCredentials_assimalign="Username=<github-user>;Password=$(gh auth token)"
```

CI does the same with the repository secret `COHESION_PACKAGES_TOKEN` when it is defined, and with the workflow's own token otherwise. It verifies the generated solutions, restores, builds in Release, and runs the Web.Testing examples' tests, then builds each ApplicationModel example against its own pins.

## Solutions

`Assimalign.Cohesion.Examples.slnx` and `examples/resources/Web/Web.slnx` are generated: run `pwsh ./setup.ps1` after adding or removing a project, and `pwsh ./setup.ps1 -Check` to verify them. The ApplicationModel examples keep their own hand-written solutions. No solution includes `_old/` or `.vs/`.
