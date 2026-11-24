// users-api: the Users application's API. A plain Web program.
//
// It reads three settings: ConnectionStrings:Users, Authentication:Authority and Telemetry:Endpoint. Run by the
// ResourceManager, the users AppHost's references fill them in: a secret the driver mounts for users-db, the identity
// application's issuer, the platform application's logs. Run with `dotnet run`, web-settings.local.json does.
//
// There is no AddResource(...) and no UseKubernetes()/UseInProcess(): a program does not know where it runs. When the
// environment sets COHESION_CONTROL_PLANE_ENDPOINT, the App kernel's agent reports started, ready and revision in the
// background, pulls action invocations over its own outbound connection, and never delays start (v2 §5.5). Unset, this
// is an unmanaged program.
//
// Its public path is not here either: /api/users on the public gateway is an exposure in the users AppHost.

using System;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;

using BookStore.Users;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(authentication => authentication.AddJwtBearer(jwt =>
{
    jwt.ValidIssuers.Add(builder.Configuration["Authentication:Authority"]!);
    jwt.ValidAudiences.Add("api://bookstore/users");
}));

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseAuthorization();
application.UseRouting();

// The gateway strips /api/users, so this is GET /api/users/{id} from outside.
application.MapGet("/{id:guid}", async (Guid id, IHttpContext context) =>
{
    User user = new() { Id = new UserId(id), Email = "(read from users-db)", CreatedAt = DateTimeOffset.UtcNow };
    await context.Response.WriteContentAsync(user, context.RequestCancelled);
});

await application.RunAsync();
