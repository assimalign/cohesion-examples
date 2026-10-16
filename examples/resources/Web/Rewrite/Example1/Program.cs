// Web.Rewrite, Example 1: redirects and rewrites.
//
// A redirect tells the client to ask for another URL; 301 Moved Permanently lets browsers remember
// it. A rewrite changes the URL inside the server before routing sees it, and the client never
// knows. Both rules match the path with a regular expression, and $1 inserts the first capture.
//
//   dotnet run
//   curl -i http://localhost:5000/old-greeting          (301, Location: /greeting)
//   curl -L http://localhost:5000/old-greeting          (follows the redirect: Hello!)
//   curl http://localhost:5000/legacy/items/42          (served by /items/{id}: Item 42)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Rewrite;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

// Ahead of routing, so routing sees the rewritten path. Redirects go ahead of rewrites.
application.UseRewrite(rules => rules
    .AddRedirect("^/old-greeting$", "/greeting", HttpStatusCode.MovedPermanently)
    .AddRewrite(@"^/legacy/items/(\d+)$", "/items/$1"));
application.UseRouting();

application.MapGet("/greeting", () => "Hello!");
application.MapGet("/items/{id:int}", (int id) => $"Item {id}");

await application.RunAsync();
