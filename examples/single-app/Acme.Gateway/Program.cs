using Assimalign.Cohesion.ApplicationModel;

IApplicationBuilder builder = Gateway.CreateBuilder(args);   // generated: Application.CreateBuilder("acme", args)
builder.AddAllResources();                                   // AddAcmeDatabase(); AddAcmeApi(); edges inferred from the manifests
builder.UseGateway(args);                                    // default InProcess here; --gateway local|docker to fan out
// Platform-specific exceptions (rare) go here and nowhere else: builder.UseGateway(args, gateways => gateways.Kubernetes(k => ...)); the callback runs only when that gateway is selected.
await builder.Build().RunAsync();
