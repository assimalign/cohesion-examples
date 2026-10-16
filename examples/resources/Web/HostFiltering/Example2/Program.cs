// Web.HostFiltering, Example 2: host names behind a proxy.
//
// Behind a reverse proxy the host the client asked for arrives in X-Forwarded-Host.
// UseForwardedHeaders runs first, so host filtering checks that effective host. The allow list
// holds the public name and every subdomain of it (*.shop.example matches eu.shop.example but not
// shop.example itself). A request that does not come through the proxy has no public host and
// is refused too.
//
//   dotnet run
//   curl -H "X-Forwarded-Host: shop.example" http://localhost:5000/                (200)
//   curl -H "X-Forwarded-Host: eu.shop.example" http://localhost:5000/             (200)
//   curl -i -H "X-Forwarded-Host: evil.example" http://localhost:5000/             (400)
//   curl -i http://localhost:5000/                                                 (400)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ForwardedHeaders;
using Assimalign.Cohesion.Web.HostFiltering;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseForwardedHeaders(options => options.Headers = ForwardedHeaderNames.XForwarded);
application.UseHostFiltering(options =>
{
    options.AllowedHosts.Add("shop.example");
    options.AllowedHosts.Add("*.shop.example");
});
application.UseRouting();

application.MapGet("/", (IHttpContext context) => $"Welcome to {context.EffectiveHost.Value}!");

await application.RunAsync();
