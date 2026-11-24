// storefront: serves the BookStore browser application. A plain Web program, with no AddResource(...) and no platform verb.
//
// It reads Authentication:Authority (where the browser signs customers in), Configuration:Endpoint (feature flags from
// the platform application's config) and Telemetry:Endpoint. The storefront AppHost binds all three.
//
// It holds no address for users-api or orders-api. The browser calls /api/users and /api/orders on the same host this
// program is served from, and the public gateway routes each path to the application that attached it.

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();
builder.Services.AddStaticFiles();

await using WebApplication application = builder.Build();

application.UseStaticFiles();
application.UseRouting();

// What the browser application needs to start: the issuer to sign in against. The API paths are relative.
application.MapGet("/app-settings.json", async context =>
{
    await context.Response.WriteContentAsync(
        new StorefrontSettings(application.Configuration["Authentication:Authority"]!, "storefront"),
        context.RequestCancelled);
});

await application.RunAsync();

internal sealed record StorefrontSettings(string Authority, string ClientId);
