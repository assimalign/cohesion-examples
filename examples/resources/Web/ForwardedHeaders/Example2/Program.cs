// Web.ForwardedHeaders, Example 2: a chain of trusted proxies.
//
// Here requests pass through two proxies, a CDN and then a load balancer on the 10.0.0.0/8
// network, and each one appends to X-Forwarded-For. ForwardLimit = 2 walks back through at most
// two hops, and only addresses in the known networks are trusted, so a client cannot spoof its
// address by adding entries of its own. The standard Forwarded header (RFC 7239) is accepted too.
// IHttpForwardedFeature reports what was applied, and the original values.
//
//   dotnet run
//   curl -H "X-Forwarded-For: 198.51.100.7, 10.0.0.5" -H "X-Forwarded-Proto: https" http://localhost:5000/whoami
//   curl -H "X-Forwarded-For: 6.6.6.6, 198.51.100.7, 10.0.0.5" http://localhost:5000/whoami     (stops after two hops)
//   curl -H "Forwarded: for=198.51.100.7;proto=https;host=shop.example" http://localhost:5000/whoami

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ForwardedHeaders;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseForwardedHeaders(options =>
{
    options.Headers = ForwardedHeaderNames.All;
    options.ForwardLimit = 2;

    // Trust networks instead of individual proxy addresses.
    options.KnownProxies.Clear();
    options.KnownNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
    options.KnownNetworks.Add(System.Net.IPNetwork.Parse("10.0.0.0/8"));
});
application.UseRouting();

application.MapGet("/whoami", (IHttpContext context) =>
{
    IHttpForwardedFeature? forwarded = context.Features.Get<IHttpForwardedFeature>();
    if (forwarded is null)
    {
        return $"No forwarded headers applied. Client: {context.ConnectionInfo.RemoteIp}";
    }

    return $"""
        Client:   {forwarded.RemoteIp} over {forwarded.Scheme} to {forwarded.Host.Value}
        Original: {forwarded.OriginalRemoteEndPoint} over {forwarded.OriginalScheme} to {forwarded.OriginalHost.Value}
        Trusted hops: {forwarded.TrustedHopCount}
        """;
});

await application.RunAsync();
