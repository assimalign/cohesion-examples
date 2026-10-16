// Web.Cors, Example 2: named policies and preflight requests.
//
// Before a cross-origin DELETE, or any request with custom headers, the browser asks first with an
// OPTIONS "preflight". UseCors answers the preflight itself (204) and the endpoint never runs.
//
// - The default policy lets https://app.example read the catalog.
// - The "partners" policy lets https://partner.example delete products with credentials (cookies)
//   and an X-Request-Id header, exposes X-Total-Count to its scripts, and lets the browser cache
//   the preflight answer for 10 minutes.
// - /internal/metrics opts out of CORS, so no origin is ever allowed to read it.
//
//   dotnet run
//   curl -i -X OPTIONS -H "Origin: https://partner.example" -H "Access-Control-Request-Method: DELETE" -H "Access-Control-Request-Headers: X-Request-Id" http://localhost:5000/products/7
//   curl -i -X DELETE -H "Origin: https://partner.example" http://localhost:5000/products/7
//   curl -i -X OPTIONS -H "Origin: https://app.example" -H "Access-Control-Request-Method: DELETE" http://localhost:5000/products/7   (no allow headers)
//   curl -i -H "Origin: https://app.example" http://localhost:5000/internal/metrics                                                 (no allow headers)

using System;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Cors;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseCors(options => options
    .AddDefaultPolicy(policy => policy.WithOrigins("https://app.example"))
    .AddPolicy("partners", policy => policy
        .WithOrigins("https://partner.example")
        .WithMethods("GET", "DELETE")
        .WithHeaders("X-Request-Id")
        .WithExposedHeaders("X-Total-Count")
        .AllowCredentials()
        .SetPreflightMaxAge(TimeSpan.FromMinutes(10))));

application.MapGet("/products", (IHttpContext context) =>
{
    context.Response.Headers["X-Total-Count"] = "3";
    return "Pen, Notebook, Stapler";
});

application.MapDelete("/products/{id:int}", (int id) => $"Deleted product {id}.")
    .RequireCors("partners");

application.MapGet("/internal/metrics", () => "requests_total 1024")
    .DisableCors();

await application.RunAsync();
