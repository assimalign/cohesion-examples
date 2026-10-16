// Web.HttpsPolicy, Example 1: HTTPS behind a TLS-terminating proxy.
//
// In production TLS often ends at a proxy, which forwards plain HTTP and reports the original
// scheme in X-Forwarded-Proto. UseForwardedHeaders makes that the effective scheme. Then
// UseHttpsRedirection redirects every request that arrived over http to https (307), and UseHsts
// tells browsers to use https for this host from now on (Strict-Transport-Security). HSTS is never
// sent for localhost, so the requests below name a public host.
//
//   dotnet run
//   curl -i -H "X-Forwarded-Host: shop.example" "http://localhost:5000/cart?id=7"                                   (307 to https://shop.example/cart?id=7)
//   curl -i -H "X-Forwarded-Host: shop.example" -H "X-Forwarded-Proto: https" "http://localhost:5000/cart?id=7"     (200, Strict-Transport-Security)

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ForwardedHeaders;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.HttpsPolicy;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseForwardedHeaders(options => options.Headers = ForwardedHeaderNames.XForwarded);
application.UseHttpsRedirection();
application.UseHsts();
application.UseRouting();

application.MapGet("/cart", (int id) => $"Cart {id}, served over https.");

await application.RunAsync();
