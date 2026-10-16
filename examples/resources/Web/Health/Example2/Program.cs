// Web.Health, Example 2: readiness and liveness probes.
//
// Orchestrators such as Kubernetes ask two different questions. Liveness (/livez): is the process
// stuck and should it be restarted? Readiness (/readyz): can it take traffic right now? Tags put
// each check on the probes it belongs to, and /healthz reports every check.
//
// - "cache" is not ready until POST /warm-up, so /readyz answers 503 until then while /livez
//   stays 200: the process is fine, it just must not receive traffic yet.
// - "database" must answer within 2 seconds; a slow answer reports Degraded instead of failing,
//   and readiness treats Degraded as not ready.
//
//   dotnet run
//   curl -i http://localhost:5000/livez               (200)
//   curl -i http://localhost:5000/readyz              (503: the cache is warming up)
//   curl -X POST http://localhost:5000/warm-up
//   curl -i http://localhost:5000/readyz              (200)
//   curl http://localhost:5000/healthz                (every check, with descriptions)

using System;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Health;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

bool cacheWarm = false;

IHealthCheckService health = HealthChecks.CreateBuilder()
    .AddCheck(
        "process",
        () => HealthCheckResult.Healthy(),
        tags: [HealthTags.Live])
    .AddCheck(
        "cache",
        () => cacheWarm ? HealthCheckResult.Healthy("The cache is warm.") : HealthCheckResult.Unhealthy("The cache is warming up."),
        tags: [HealthTags.Ready])
    .AddCheck(
        "database",
        async (context, cancellationToken) =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(20), cancellationToken); // stands in for a real query
            return HealthCheckResult.Healthy("Answered in 20 ms.");
        },
        failureStatus: HealthStatus.Degraded,
        tags: [HealthTags.Ready],
        timeout: TimeSpan.FromSeconds(2))
    .Build();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.MapHealthChecks(health);
application.MapLivenessCheck(health);
application.MapReadinessCheck(health, configure: options => options.DegradedStatusCode = HttpStatusCode.ServiceUnavailable);
application.UseRouting();

application.MapPost("/warm-up", () =>
{
    cacheWarm = true;
    return "The cache is warm.";
});

await application.RunAsync();
