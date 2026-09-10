using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);
// The generic Platform SDKs currently support model description only; their runtime entries/control planes are upstream.
builder.AddAllResources();
builder.UseGateway(args);

await builder.Build().RunAsync();
