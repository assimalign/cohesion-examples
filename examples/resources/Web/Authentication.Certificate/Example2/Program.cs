// Web.Authentication.Certificate, Example 2: map certificates to users and roles.
//
// The HTTPS listener requires a client certificate from a known client, so a connection without
// one fails the TLS handshake. OnCertificateValidated replaces the default identity (the
// certificate's subject) with the user and role the application registered for that certificate,
// and an authorization policy uses the role.
//
// The example creates a server certificate and two client certificates at startup and saves the
// clients' as admin.pfx and reader.pfx. Present them from PowerShell 7:
//
//   dotnet run
//   $admin = Get-PfxCertificate admin.pfx; $reader = Get-PfxCertificate reader.pfx
//   (Invoke-WebRequest https://localhost:5001/admin -Certificate $admin -SkipCertificateCheck).Content       (200)
//   (Invoke-WebRequest https://localhost:5001/reports -Certificate $reader -SkipCertificateCheck).Content    (200)
//   (Invoke-WebRequest https://localhost:5001/admin -Certificate $reader -SkipCertificateCheck -SkipHttpErrorCheck).StatusCode   (403)
//   curl -k https://localhost:5001/reports                                    (fails: no client certificate)

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

using Assimalign.Cohesion.Connections.Security;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Certificate;
using Assimalign.Cohesion.Web.Authorization;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

using X509Certificate2 serverCertificate = CreateCertificate("localhost", server: true);
using X509Certificate2 adminCertificate = CreateCertificate("admin-client", server: false);
using X509Certificate2 readerCertificate = CreateCertificate("reader-client", server: false);
File.WriteAllBytes("admin.pfx", adminCertificate.Export(X509ContentType.Pfx));
File.WriteAllBytes("reader.pfx", readerCertificate.Export(X509ContentType.Pfx));

// The registered clients, by certificate thumbprint.
Dictionary<string, (string User, string Role)> clients = new()
{
    [adminCertificate.Thumbprint] = ("ada", "admin"),
    [readerCertificate.Thumbprint] = ("linus", "reader"),
};

TlsServerOptions tls = new TlsServerOptions { AuthenticationOptions = { ServerCertificate = serverCertificate } }
    .RequireClientCertificate((certificate, chain, errors) => clients.ContainsKey(certificate.Thumbprint));

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Server.UseServer(options => options.UseHttps(tcp => tcp.EndPoint = new IPEndPoint(IPAddress.Loopback, 5001), tls));

builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddCertificate(options => options.OnCertificateValidated = (context, cancellationToken) =>
        {
            (string user, string role) = clients[context.ClientCertificate.Thumbprint];
            ClaimsIdentity identity = new(
                [new Claim(ClaimTypes.Name, user), new Claim(ClaimTypes.Role, role)],
                CertificateAuthenticationDefaults.AuthenticationScheme);

            context.Principal = new ClaimsPrincipal(identity);
            return ValueTask.CompletedTask;
        });
        authentication.Options.DefaultScheme = CertificateAuthenticationDefaults.AuthenticationScheme;
    })
    .AddAuthorization(authorization => authorization.Options.AddPolicy("admins", policy => policy.RequireRole("admin")));

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();
application.UseAuthorization();

application.MapGet("/reports", (IHttpContext context) => $"Reports for {context.User.Identity!.Name}.")
    .RequireAuthorization();

application.MapGet("/admin", (IHttpContext context) => $"Admin console for {context.User.Identity!.Name}.")
    .RequireAuthorization("admins");

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
    // the example save the client certificates to .pfx files.
    return X509CertificateLoader.LoadPkcs12(certificate.Export(X509ContentType.Pfx), password: null, X509KeyStorageFlags.Exportable);
}
