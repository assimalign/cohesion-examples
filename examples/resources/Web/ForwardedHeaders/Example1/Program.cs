// Web.ForwardedHeaders, Example 1: run behind a reverse proxy.
//
// A reverse proxy (nginx, a load balancer, an ingress) opens its own connection to the
// application, so the connection reports the proxy as the client. The proxy passes the original
// client address, scheme and host in X-Forwarded-For, X-Forwarded-Proto and X-Forwarded-Host.
// UseForwardedHeaders applies them as the request's effective values, but only when the request
// comes from a trusted proxy (loopback by default). The raw request is never changed.
//
//   dotnet run
//   curl http://localhost:5000/whoami
//   curl -H "X-Forwarded-For: 203.0.113.7" -H "X-Forwarded-Proto: https" -H "X-Forwarded-Host: shop.example" http://localhost:5000/whoami

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ForwardedHeaders;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

// Ahead of everything that reads the client address, scheme or host.
application.UseForwardedHeaders(options => options.Headers = ForwardedHeaderNames.XForwarded);
application.UseRouting();

application.MapGet("/whoami", (IHttpContext context) => $"""
    Connection: {context.Request.Scheme} {context.Request.Host.Value} from {context.ConnectionInfo.RemoteIp}
    Effective:  {context.EffectiveScheme} {context.EffectiveHost.Value} from {context.EffectiveRemoteIp}
    """);

await application.RunAsync();
