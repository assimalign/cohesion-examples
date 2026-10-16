// Web.HttpsPolicy, Example 2: the application terminates TLS itself.
//
// The application listens on http://localhost:5000 and https://localhost:5001, with a self-signed
// certificate it creates at startup. UseHttpsRedirection sends http requests to the https port,
// and UseHsts sends a Strict-Transport-Security header with a 180-day lifetime that also covers
// subdomains. HSTS is never sent for localhost, which keeps it out of your browser while you
// develop, so the last request reaches the https listener under a public name.
//
//   dotnet run
//   curl -i http://localhost:5000/                                                         (307 to https://localhost:5001/)
//   curl -k https://localhost:5001/                                                        (200)
//   curl -k -i --resolve shop.example:5001:127.0.0.1 https://shop.example:5001/            (200, Strict-Transport-Security)

using System;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Assimalign.Cohesion.Connections.Security;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.HttpsPolicy;
using Assimalign.Cohesion.Web.Routing;

using X509Certificate2 certificate = CreateCertificate();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Server.UseServer(options =>
{
    options.UseHttp1(tcp => tcp.EndPoint = new IPEndPoint(IPAddress.Loopback, 5000));
    options.UseHttps(
        tcp => tcp.EndPoint = new IPEndPoint(IPAddress.Loopback, 5001),
        new TlsServerOptions { AuthenticationOptions = { ServerCertificate = certificate } });
});
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseHttpsRedirection(options => options.HttpsPort = 5001);
application.UseHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(180);
    options.IncludeSubDomains = true;
});
application.UseRouting();

application.MapGet("/", () => "Served over https.");

await application.RunAsync();

// A self-signed certificate for localhost and shop.example.
static X509Certificate2 CreateCertificate()
{
    using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    CertificateRequest request = new("CN=localhost", key, HashAlgorithmName.SHA256);

    SubjectAlternativeNameBuilder names = new();
    names.AddDnsName("localhost");
    names.AddDnsName("shop.example");
    names.AddIpAddress(IPAddress.Loopback);
    request.CertificateExtensions.Add(names.Build());

    using X509Certificate2 created = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));

    // On Windows, TLS can only use the private key after a PKCS#12 round trip.
    return X509CertificateLoader.LoadPkcs12(created.Export(X509ContentType.Pfx), password: null);
}
