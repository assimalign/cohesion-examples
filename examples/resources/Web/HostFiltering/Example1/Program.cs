// Web.HostFiltering, Example 1: accept only your own host names.
//
// Anything can be sent as the Host header. An application that builds links, redirects or cache
// keys from it can be tricked into pointing at an attacker's site. UseHostFiltering answers 400
// to any request whose host is not on the allow list. The port is ignored.
//
//   dotnet run
//   curl http://localhost:5000/                            (200)
//   curl -i -H "Host: evil.example" http://localhost:5000/ (400)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.HostFiltering;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseHostFiltering(options =>
{
    options.AllowedHosts.Add("localhost");
    options.AllowedHosts.Add("127.0.0.1");
});
application.UseRouting();

application.MapGet("/", (IHttpContext context) => $"Hello from {context.Request.Host.Value}!");

await application.RunAsync();
