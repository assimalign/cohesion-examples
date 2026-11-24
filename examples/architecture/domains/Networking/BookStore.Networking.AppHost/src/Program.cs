// The networking application: the public gateway, the egress gateway, public DNS and the site-to-site tunnel.
// README.md says how to read it. [proposed], beyond the v2 design: the secret, a manifest's Values and Outputs, both
// gateways, AllowBind by application name, the development certificate, the Rezolvr and VpnGateway types and
// policy.privileged.allow.

using Assimalign.Cohesion.Fabric.Providers;

var app = CohesionApplication.CreateBuilder("networking", Platforms.InProcess, Platforms.Kubernetes);

var publicDomain = app.Environment.Parameter("publicDomain");
var publicListenerHostname = app.Environment.Parameter("publicListenerHostname");
var publicAddress = app.Environment.Parameter("publicAddress");
var dnsAddress = app.Environment.Parameter("dnsAddress");
var onPremPeer = app.Environment.Parameter("onPremPeer");
var sitePresharedKey = app.Environment.Secret("sitePresharedKey");      // only a Settings entry takes a secret

// In Outputs, From is group/version/Kind/name of the object the file holds, and Path is a JSON pointer into it.
var publicTls = app.Kubernetes.AddManifest("public-tls", new()
{
    File = "deploy/public-certificate.yaml",
    Values = new()
    {
        ["/spec/dnsNames/0"] = publicListenerHostname,
        ["/spec/dnsNames/1"] = publicDomain,
    },
    Outputs = new()
    {
        ["secretName"] = new() { From = "cert-manager.io/v1/Certificate/public-tls", Path = "/spec/secretName" },
    },
    ReadyCondition = "Ready",
});

// No project: Cohesion runs no program for a gateway. In process the Kubernetes block is not read, and the gateway
// gets a development certificate by rule of the platform.
var publicGateway = app.Networking.AddGateway("public", new()
{
    Listeners = new() { ["https"] = new() { Port = 443, Host = publicListenerHostname } },
    AllowBind = ["identity", "users", "orders", "storefront"],
    Kubernetes = new()
    {
        TlsSecrets = new() { ["https"] = publicTls.Output("secretName") },      // a manifest's output, never a string
        StaticAddress = publicAddress,
    },
});

app.Networking.AddEgressGateway("egress", new() { AllowBind = ["orders"] });

var publicDns = app.Rezolvr.Add<Projects.BookStore_Networking_Rezolvr>("public-dns", new()
{
    Scale = 2,
    Settings = new()
    {
        ["Zones:0:Name"] = publicDomain,
        ["Zones:0:Records:www:A"] = publicGateway.Address,
        ["Zones:0:Records:id:A"] = publicGateway.Address,
    },
});

// No Ports: the exposure takes the ports Rezolvr marks as exposable, 53 on UDP and on TCP.
// No Gateway: the platform gives the target an address of its own, and Visibility says who reaches it.
app.Networking.AddExposure("dns", new()
{
    Target = publicDns,
    Visibility = ExposureVisibility.Public,
    Kubernetes = new() { StaticAddress = dnsAddress },
});

// VpnGateway needs the host's network and NET_ADMIN. The in-process platform cannot give them, so it is declared
// through app.Kubernetes, and the environment's policy.privileged.allow must list this application.
app.Kubernetes.VpnGateway.Add<Projects.BookStore_Networking_VpnGateway>("site-vpn", new()
{
    Settings = new()
    {
        ["Peers:0:Endpoint"] = onPremPeer,
        ["Peers:0:Routes"] = "10.20.0.0/16",
        ["Peers:0:PresharedKey"] = sitePresharedKey,
    },
});

app.Authorization.AddRoleAssignment(Roles.Operator, Principal.Group("network-oncall"));

return await app.RunAsync(args);
