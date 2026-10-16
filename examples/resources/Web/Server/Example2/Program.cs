// Web.Server, Example 2: listeners, protocols and limits.
//
// The server is configured in code instead of the default http://localhost:5000:
// - one HTTPS listener on port 5001 that speaks HTTP/2 and HTTP/1.1, chosen per connection (ALPN),
//   with a self-signed certificate created at startup;
// - request bodies limited to 1 MiB on both protocols, so a larger upload is refused;
// - HTTP/2 connections limited to 50 concurrent streams;
// - at most 100 open connections across the server.
//
//   dotnet run
//   curl -k --http2 https://localhost:5001/protocol                       (Http20, with a curl built with HTTP/2 support)
//   curl -k --http1.1 https://localhost:5001/protocol                     (Http11)
//   head -c 2000000 /dev/zero | curl -k -i --data-binary @- https://localhost:5001/upload    (413)

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Assimalign.Cohesion.Connections.Security;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

const long MaxBodySize = 1024 * 1024;

using X509Certificate2 certificate = CreateCertificate();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Server
    .LimitConcurrentConnections(100)
    .UseServer(options => options.UseHttps(
        tcp => tcp.EndPoint = new IPEndPoint(IPAddress.Loopback, 5001),
        new TlsServerOptions { AuthenticationOptions = { ServerCertificate = certificate } },
        http1 => http1.Limits.MaxRequestBodySize = MaxBodySize,
        http2 =>
        {
            http2.Limits.MaxRequestBodySize = MaxBodySize;
            http2.Limits.MaxStreamsPerConnection = 50;
        }));
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/protocol", (IHttpContext context) => $"You are speaking {context.Version}.");

application.MapPost("/upload", async (IHttpContext context) =>
{
    using MemoryStream body = new();
    await context.Request.Body.CopyToAsync(body, context.RequestCancelled);
    return $"Received {body.Length} bytes.";
});

await application.RunAsync();

// A self-signed certificate for localhost.
static X509Certificate2 CreateCertificate()
{
    using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256);

    SubjectAlternativeNameBuilder names = new();
    names.AddDnsName("localhost");
    names.AddIpAddress(IPAddress.Loopback);
    request.CertificateExtensions.Add(names.Build());

    using X509Certificate2 created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));

    // On Windows, TLS can only use the private key after a PKCS#12 round trip.
    return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx), password: null);
}
