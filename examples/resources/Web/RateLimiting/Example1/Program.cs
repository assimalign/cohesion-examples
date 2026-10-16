// Web.RateLimiting, Example 1: limit requests per client.
//
// The global policy gives each client address 3 requests per 10-second window. A request over the
// limit is rejected with 429 Too Many Requests and a Retry-After header that says how many
// seconds remain until the window resets.
//
//   dotnet run
//   for i in 1 2 3 4; do curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5000/; done    (200 200 200 429)
//   curl -i http://localhost:5000/                                                             (429, Retry-After)

using System;
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

// One limiter per partition key; here the key is the client's address.
application.UseRateLimiting(options => options.GlobalPolicy = RateLimitingPolicy.Create(
    static (IHttpContext context) => RateLimitPartitionKeys.ClientAddress(context),
    static (string _) => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
    {
        PermitLimit = 3,
        Window = TimeSpan.FromSeconds(10),
    })));

application.MapGet("/", () => "Hello!");

await application.RunAsync();
