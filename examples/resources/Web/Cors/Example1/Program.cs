// Web.Cors, Example 1: allow one front-end origin.
//
// A browser lets a page read a response from another origin only when the server allows it. The
// default policy allows https://app.example. A request from that origin gets an
// Access-Control-Allow-Origin header. A request from any other origin is still answered, but
// without the header, so the browser keeps the response from the page.
//
//   dotnet run
//   curl -i -H "Origin: https://app.example" http://localhost:5000/products     (Access-Control-Allow-Origin: https://app.example)
//   curl -i -H "Origin: https://evil.example" http://localhost:5000/products    (no Access-Control-Allow-Origin)

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Cors;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins("https://app.example")));

application.MapGet("/products", () => "Pen, Notebook, Stapler");

await application.RunAsync();
