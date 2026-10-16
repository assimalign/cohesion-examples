// Web.Health, Example 1: a health endpoint.
//
// A health check reports whether one part of the application works. MapHealthChecks serves the
// combined report as JSON at /healthz: 200 while every check is healthy (or degraded), 503 as soon
// as one is unhealthy. Load balancers and monitors poll it.
//
//   dotnet run
//   curl -i http://localhost:5000/healthz

using Assimalign.Cohesion.Web.Health;
using Assimalign.Cohesion.Web.Hosting;

IHealthCheckService health = HealthChecks.CreateBuilder()
    .AddCheck("self", () => HealthCheckResult.Healthy("The application is running."))
    .Build();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await using WebApplication application = builder.Build();

application.MapHealthChecks(health);

await application.RunAsync();
