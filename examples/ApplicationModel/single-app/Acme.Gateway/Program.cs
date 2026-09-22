using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
// One generated verb per referenced resource: the gateway names what it composes.
builder.AddAcmeDatabase();
builder.AddAcmeApi();
builder.UseGateway(args);

await builder.Build().RunAsync();
