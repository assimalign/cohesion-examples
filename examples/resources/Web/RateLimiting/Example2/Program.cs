// Web.RateLimiting, Example 2: a named policy per API key.
//
// The "per-key" policy gives each X-Api-Key value its own token bucket: a burst of 5 requests,
// then one more every 2 seconds. Requests without a key share the "anonymous" bucket. The /api
// group applies the policy to every route, and /api/health opts out so monitoring is never
// throttled. A rejected request gets a JSON body that says why and when to retry.
//
//   dotnet run
//   for i in $(seq 1 7); do curl -s -H "X-Api-Key: alpha" http://localhost:5000/api/orders; echo; done   (5 answers, then 429s)
//   curl -H "X-Api-Key: beta" http://localhost:5000/api/orders                                          (a separate bucket)
//   curl http://localhost:5000/api/health                                                              (never limited)

using System;
using System.Text;
using System.Threading.RateLimiting;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.RateLimiting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseRateLimiting(options =>
{
    options.AddPolicy("per-key", RateLimitingPolicy.Create(
        static (IHttpContext context) => RateLimitPartitionKeys.Header(context, "X-Api-Key", fallback: "anonymous"),
        static (string _) => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 5,
            TokensPerPeriod = 1,
            ReplenishmentPeriod = TimeSpan.FromSeconds(2),
            AutoReplenishment = true,
        })));

    options.OnRejected = async (rejection, cancellationToken) =>
    {
        double retryAfter = Math.Ceiling(rejection.RetryAfter?.TotalSeconds ?? 1);
        string body = $$"""{"error":"rate_limited","policy":"{{rejection.PolicyName}}","retryAfterSeconds":{{retryAfter}}}""";

        rejection.Context.Response.Headers[HttpHeaderKey.ContentType] = "application/json";
        await rejection.Context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(body), cancellationToken);
    };
});

IRouterGroupBuilder api = application.MapGroup("/api").RequireRateLimiting("per-key");

api.MapGet("orders", () => "Orders: 1001, 1002, 1003");

api.MapGet("health", () => "Healthy")
    .DisableRateLimiting();

await application.RunAsync();
