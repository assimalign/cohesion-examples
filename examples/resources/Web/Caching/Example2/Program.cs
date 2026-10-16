// Web.Caching, Example 2: named policies, tags and eviction.
//
// Without a base policy nothing is cached until an endpoint opts in. The /catalog group opts into
// the "catalog" policy: five minutes, a separate entry for each ?page= value, and the "catalog"
// tag. /catalog/stock opts out because stock levels must be live. POST /catalog/refresh evicts
// every entry tagged "catalog", for example after the catalog changed.
//
//   dotnet run
//   curl http://localhost:5000/catalog?page=1          (repeat: same render time)
//   curl http://localhost:5000/catalog?page=2          (a separate entry)
//   curl http://localhost:5000/catalog/stock           (never cached)
//   curl -X POST http://localhost:5000/catalog/refresh
//   curl http://localhost:5000/catalog?page=1          (rendered again)

using System;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Caching;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

// The application keeps the store so it can evict from it.
using InMemoryOutputCacheStore store = new();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseOutputCache(store, options => options.AddPolicy("catalog", policy =>
{
    policy.Duration = TimeSpan.FromMinutes(5);
    policy.VaryByQuery("page");
    policy.Tag("catalog");
}));

IRouterGroupBuilder catalog = application.MapGroup("/catalog").CacheOutput("catalog");

catalog.MapGet("", (int? page) => $"Catalog page {page ?? 1}, rendered at {DateTime.UtcNow:HH:mm:ss.fff}.");

catalog.MapGet("stock", () => $"Stock levels at {DateTime.UtcNow:HH:mm:ss.fff}.")
    .DisableOutputCache();

application.MapPost("/catalog/refresh", async () =>
{
    await store.EvictByTagAsync("catalog");
    return "Catalog cache cleared.";
});

await application.RunAsync();
