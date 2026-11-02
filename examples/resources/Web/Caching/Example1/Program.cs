// Web.Caching, Example 1: output caching.
//
// UseOutputCache stores successful GET responses and replays them until they expire. A base
// policy caches every GET endpoint, so /time keeps answering with the same time for 10 seconds.
// A replayed response carries an Age header that says how old it is.
//
//   dotnet run
//   curl -i http://localhost:5000/time      (run it twice within 10 seconds: same body, plus Age)

using System;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Caching;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();
builder.Services.AddOutputCache(cache => cache.Options.AddBasePolicy(policy => policy.Duration = TimeSpan.FromSeconds(10)));

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseOutputCache();

application.MapGet("/time", () => $"The time is {DateTime.UtcNow:HH:mm:ss.fff}.");

await application.RunAsync();
