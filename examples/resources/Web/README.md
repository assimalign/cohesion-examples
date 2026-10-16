# Web examples

One folder per package in the Cohesion Web area, each with two small applications built on
`Assimalign.Cohesion.Sdk.Web`. Each example is a single `Program.cs`, and its opening comment
says what it shows and gives the commands to try it.

## Before you run

- **.NET SDK** 10.0.300 or later.
- **Package feed.** The Cohesion SDKs and packages come from the Assimalign GitHub Packages feed
  (`nuget.config` at the repository root), and `global.json` pins which build to use. GitHub
  Packages needs a token with `read:packages`, even to read. Supply it through the environment,
  never in a tracked file:

  ```bash
  export NuGetPackageSourceCredentials_assimalign="Username=<github-user>;Password=$(gh auth token)"
  ```

- **Run one:** `cd Routing/Example1` then `dotnet run`. Applications listen on
  `http://localhost:5000`. The certificate, HTTPS and server examples listen on
  `https://localhost:5001`.
- **Build them all:** `dotnet build Web.slnx`.

## Environments and listeners

Every application has `Properties/launchSettings.json`, which sets `COHESION_ENVIRONMENT` to
`Local` for `dotnet run`, and one settings file per environment. The host reads `appsettings.json`,
then `appsettings.{environment}.json`, so the file for the current environment declares the
listeners under `Http:Endpoints`:

| Environment | Listener | Limits |
| --- | --- | --- |
| `Local` | `http://localhost:5000` | defaults |
| `Development` | `http://0.0.0.0:8080` | defaults |
| `Staging`, `Production` | `http://0.0.0.0:8080` | 10 MiB request bodies, 1-minute keep-alive, 1000 connections |

Without a launch profile (`dotnet run --no-launch-profile`, or the built executable started
directly) the environment is `Production`. Override any value with a `COHESION_CONFIG__` variable
(`COHESION_CONFIG__Http__Endpoints__Main__Port=9000`) or the command line
(`--Http:Endpoints:Main:Port=9000`).

The Authentication.Certificate examples, HttpsPolicy/Example2 and Server/Example2 configure their
listeners in code with `builder.Server.UseServer(...)`, which turns configuration binding off.
Their settings files are empty on purpose, and their code is the server configuration.

## Examples

| Package | Example 1 | Example 2 |
| --- | --- | --- |
| [Antiforgery](Antiforgery) | A token in a header protects a POST | A server-rendered form, a protected route group, a persistent key ring |
| [Api](Api) | Typed endpoints: route, query and body binding | A CRUD API on a route group, with status codes and endpoint metadata |
| [Authentication](Authentication) | A custom API-key scheme | Challenge and forbid from an endpoint |
| [Authentication.Bearer](Authentication.Bearer) | JWT bearer tokens | Roles in tokens, enforced by policy |
| [Authentication.Certificate](Authentication.Certificate) | Client certificates over TLS | Certificates mapped to users and roles |
| [Authentication.Cookie](Authentication.Cookie) | Sign in and out with a cookie | Login pages, roles, and API responses |
| [Authorization](Authorization) | Require a signed-in user | Role, claim and assertion policies on a route group |
| [Caching](Caching) | Output caching with a base policy | Named policies, vary-by-query, eviction by tag |
| [Compression](Compression) | Brotli and gzip responses | Tuned compression and compressed uploads |
| [CookiePolicy](CookiePolicy) | Cookie consent | Enforced cookie attributes and rejections |
| [Cors](Cors) | One allowed origin | Named policies and preflight requests |
| [Diagnostics](Diagnostics) | Request logging to the console | An access log file with extra fields |
| [ErrorHandling](ErrorHandling) | Exceptions become problem details | Exceptions mapped to statuses, status-code pages |
| [Forms](Forms) | Read a submitted form | File uploads with limits |
| [ForwardedHeaders](ForwardedHeaders) | Run behind a reverse proxy | A chain of trusted proxies |
| [Health](Health) | A health endpoint | Readiness and liveness probes |
| [HostFiltering](HostFiltering) | Allowed host names | Host names behind a proxy |
| [Hosting](Hosting) | The smallest web application | A background service, configuration and shutdown |
| [HttpsPolicy](HttpsPolicy) | HTTPS behind a TLS-terminating proxy | The application terminates TLS itself |
| [OpenApi](OpenApi) | An OpenAPI document | A documented API: tags, security scheme, YAML and 3.0 |
| [ProblemDetails](ProblemDetails) | Standard error responses | Domain errors as problem types |
| [Query](Query) | The HTTP QUERY method | Conditional queries and stored results |
| [RateLimiting](RateLimiting) | Requests per client | A token bucket per API key |
| [RequestTimeouts](RequestTimeouts) | A time limit for every request | Limits per endpoint |
| [Rewrite](Rewrite) | Redirects and rewrites | Canonical URLs, named captures, custom rules |
| [Routing](Routing) | Match requests to endpoints | Templates, constraints, groups and links |
| [SecurityHeaders](SecurityHeaders) | Safe defaults on every response | A Content Security Policy with nonces |
| [Serialization](Serialization) | Read and write JSON | Content negotiation with a custom CSV writer |
| [Server](Server) | Features the server gives every request | Listeners, protocols and limits |
| [Sessions](Sessions) | A visit counter | Sign in with a session |
| [StaticFiles](StaticFiles) | Serve a folder of files | A single-page application |
| [Testing](Testing) | Test a pipeline in memory | Test the application's real `Program.cs` |
| [Validation](Validation) | Validate a request body | Nested rules and opting out |
| [WebSockets](WebSockets) | An echo endpoint | Chat rooms with a subprotocol |

## Packages outside the App.Web framework

`Sdk.Web` delivers every Web package through the `App.Web` shared framework, with two
exceptions that these examples reference explicitly: `Assimalign.Cohesion.Web.Testing` and
`Assimalign.Cohesion.Web.OpenApi` ship on NuGet only. They are referenced with
`Version="$(CohesionVersion)"`, the version `global.json` pins.
