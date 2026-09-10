using System.Net;
using Assimalign.Cohesion.ConfigurationStore.Client;
using Assimalign.Cohesion.Dns;
using Assimalign.Cohesion.Rezolvr;
using Assimalign.Cohesion.Rezolvr.Hosting;
using Example.Networking.Rezolvr;

// The networking-rezolvr resource: the organization's authoritative DNS. Zones are C#; records claimed by other
// applications (dns.AddARecord commands) are added to the same zone at run time and owned by the claimer. The Rezolvr builder API used here is illustrative — the Rezolvr area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
RezolvrApplicationBuilder builder = RezolvrApplication.CreateBuilder(args);

// Forwarders can be promoted without a rebuild through the Platform ConfigurationStore (namespace `networking`).
builder.Configuration.AddConfigurationStore(Resource.References.PlatformConfigurationStore.Api.Endpoint, @namespace: "networking");

builder.AddZone("example.com", zone =>
{
    zone.Soa(primary: "ns1.example.com", hostmaster: "hostmaster@example.com", serial: 2026090601);
    zone.Ns("ns1.example.com");
    zone.A("ns1",        IPAddress.Parse("203.0.113.53"));
    // Public names for the organization's applications: the LB / Ingress allocations each gateway publishes in its
    // export (203.0.113.0/24 is documentation space).
    zone.A("platform",   IPAddress.Parse("203.0.113.10"));
    zone.A("identity",   IPAddress.Parse("203.0.113.20"));
    zone.A("networking", IPAddress.Parse("203.0.113.30"));
    zone.A("appa",       IPAddress.Parse("203.0.113.41"));
    zone.A("appb",       IPAddress.Parse("203.0.113.42"));
    zone.A("appc",       IPAddress.Parse("203.0.113.43"));
    zone.A("vpn",        IPAddress.Parse("203.0.113.60"));
    zone.AcceptCommands();                                   // rezolvr.record claims from trusted applications land here
});

builder.AddForwarders(builder.Configuration.Get<string[]>("Rezolvr:Forwarders") ?? ["1.1.1.1", "9.9.9.9"]);
builder.AddStorage(Resource.Mounts.Data);                    // zone state + claimed records survive restarts
builder.Listen(Resource.Endpoints.Dns);

await builder.Build().RunAsync();
