// Web.Authentication.Certificate, Example 1: client certificates over TLS.
//
// The certificate scheme authenticates the client certificate a TLS connection presented. The
// HTTPS listener asks every client for one (AllowClientCertificate), accepts only the certificate
// this application trusts, and still lets clients without a certificate connect anonymously.
//
// To stay self-contained, the example creates a self-signed server certificate and a client
// certificate at startup, and saves the client's as client.pfx. Present it from PowerShell 7
// (Windows' own curl cannot present a self-signed .pfx):
//
//   dotnet run
//   (Invoke-WebRequest https://localhost:5001/whoami -Certificate (Get-PfxCertificate client.pfx) -SkipCertificateCheck).Content
//                                                       (Hello, CN=example-client!)
//   curl -k https://localhost:5001/whoami               (Hello, anonymous!)

using System;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Assimalign.Cohesion.Connections.Security;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Certificate;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

using X509Certificate2 serverCertificate = CreateCertificate("localhost", server: true);
using X509Certificate2 clientCertificate = CreateCertificate("example-client", server: false);
File.WriteAllBytes("client.pfx", clientCertificate.Export(X509ContentType.Pfx));

TlsServerOptions tls = new TlsServerOptions { AuthenticationOptions = { ServerCertificate = serverCertificate } }
    .AllowClientCertificate((certificate, chain, errors) => certificate.Thumbprint == clientCertificate.Thumbprint);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Declaring a listener replaces the default http://localhost:5000 one.
builder.Server.UseServer(options => options.UseHttps(tcp => tcp.EndPoint = new IPEndPoint(IPAddress.Loopback, 5001), tls));

builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddCertificate();
        authentication.Options.DefaultScheme = CertificateAuthenticationDefaults.AuthenticationScheme;
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();

application.MapGet("/whoami", (IHttpContext context) =>
    context.User.Identity is { IsAuthenticated: true } identity
        ? $"Hello, {identity.Name}!"
        : "Hello, anonymous!");

await application.RunAsync();

// A self-signed certificate for a server (localhost) or a client.
static X509Certificate2 CreateCertificate(string name, bool server)
{
    using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    CertificateRequest request = new($"CN={name}", key, HashAlgorithmName.SHA256);
    request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
        [new Oid(server ? "1.3.6.1.5.5.7.3.1" : "1.3.6.1.5.5.7.3.2")], critical: false)); // server or client authentication

    if (server)
    {
        SubjectAlternativeNameBuilder names = new();
        names.AddDnsName("localhost");
        names.AddIpAddress(IPAddress.Loopback);
        request.CertificateExtensions.Add(names.Build());
    }

    using X509Certificate2 certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(30));

    // On Windows, TLS can only use the private key after a PKCS#12 round trip. Exportable lets
    // the example save the client certificate to client.pfx.
    return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null, X509KeyStorageFlags.Exportable);
}
