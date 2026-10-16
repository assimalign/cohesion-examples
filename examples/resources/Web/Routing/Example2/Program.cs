// Web.Routing, Example 2: templates, constraints, groups and links.
//
// - A route group gives its routes a shared prefix, /api/v1.
// - Constraints pick the route: items/{id:int} takes numbers, items/{slug:alpha} takes letters,
//   and anything else does not match (404).
// - {month:int?} is optional, range(2000,2100) bounds a number, and {**path} catches the rest of
//   the path, slashes included.
// - Parameters that are not in the template bind from the query string.
// - A named route can build its own URL, so links never hard-code paths.
//
//   dotnet run
//   curl http://localhost:5000/api/v1/items/42                (by id)
//   curl http://localhost:5000/api/v1/items/pens              (by slug)
//   curl -i http://localhost:5000/api/v1/items/x-1            (404)
//   curl http://localhost:5000/api/v1/archive/2026            (a whole year)
//   curl http://localhost:5000/api/v1/archive/2026/10         (one month)
//   curl http://localhost:5000/api/v1/files/docs/2026/report.pdf
//   curl "http://localhost:5000/api/v1/search?q=pens&page=2"
//   curl http://localhost:5000/api/v1/items/42/link           (/api/v1/items/42)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

IRouterGroupBuilder api = application.MapGroup("/api/v1");

api.MapGet("items/{id:int}", (int id) => $"Item #{id}.")
    .WithName("item-by-id");

api.MapGet("items/{slug:alpha}", (string slug) => $"Item '{slug}'.");

api.MapGet("archive/{year:int:range(2000,2100)}/{month:int?}", (int year, int? month) =>
    month is null ? $"Everything from {year}." : $"Everything from {year}-{month:00}.");

api.MapGet("files/{**path}", (string path) => $"File: {path}");

api.MapGet("search", (string q, int? page) => $"Results for '{q}', page {page ?? 1}.");

api.MapGet("items/{id:int}/link", (int id, IHttpContext context) =>
    context.GetLinkGenerator().GetPathByName("item-by-id", new RouteValueDictionary { ["id"] = id }));

await application.RunAsync();
