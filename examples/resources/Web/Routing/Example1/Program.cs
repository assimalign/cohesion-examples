// Web.Routing, Example 1: match requests to endpoints.
//
// AddRouting and UseRouting turn the router on, and each Map call adds an endpoint for an HTTP
// method and a route template. {name} captures a path segment and binds to the lambda parameter
// of the same name. A path no route matches gets 404. A path that matches, but with the wrong
// method, gets 405 and an Allow header listing the methods that would work.
//
//   dotnet run
//   curl http://localhost:5000/
//   curl http://localhost:5000/hello/ada
//   curl -i http://localhost:5000/nowhere                 (404)
//   curl -i -X DELETE http://localhost:5000/hello/ada     (405, Allow: GET, HEAD)

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/", () => "Welcome! Try /hello/{your name}.");
application.MapGet("/hello/{name}", (string name) => $"Hello, {name}!");

await application.RunAsync();
