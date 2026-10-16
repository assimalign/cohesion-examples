// Web.Authorization, Example 2: named policies on a route group.
//
// Three named policies: "admins" requires a role, "sales" requires a claim value, and "owner"
// runs code against the request (the {owner} route value must be the caller). The /api group
// requires a signed-in user for every route, a route adds its own policy on top, and /api/status
// opts out with AllowAnonymous. ApiEndpointMetadata makes the cookie scheme answer 401 and 403
// instead of redirecting, as an API should.
//
//   dotnet run
//   curl http://localhost:5000/api/status                                              (200)
//   curl -i http://localhost:5000/api/reports                                          (401)
//   curl -c jar.txt -X POST "http://localhost:5000/login?user=ada&department=sales"
//   curl -b jar.txt http://localhost:5000/api/reports                                  (200)
//   curl -b jar.txt http://localhost:5000/api/documents/ada                            (200)
//   curl -i -b jar.txt http://localhost:5000/api/documents/linus                       (403)
//   curl -i -b jar.txt -X DELETE http://localhost:5000/api/users/linus                 (403)
//   curl -c jar.txt -X POST "http://localhost:5000/login?user=root&role=admin"
//   curl -b jar.txt -X DELETE http://localhost:5000/api/users/linus                    (200)

using System.Collections.Generic;
using System.Security.Claims;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Cookie;
using Assimalign.Cohesion.Web.Authorization;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddCookie();
        authentication.Options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddAuthorization(authorization =>
    {
        authorization.Options.AddPolicy("admins", policy => policy.RequireRole("admin"));
        authorization.Options.AddPolicy("sales", policy => policy.RequireClaim("department", "sales"));
        authorization.Options.AddPolicy("owner", policy => policy.RequireAssertion(context =>
            context.HttpContext.TryGetRouteValues(out RouteValueDictionary? values)
            && values?["owner"] as string == context.User.Identity?.Name));
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();
application.UseAuthorization();

application.MapPost("/login", async (string user, string? role, string? department, IHttpContext context) =>
{
    List<Claim> claims = [new Claim(ClaimTypes.Name, user)];
    if (role is not null)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }
    if (department is not null)
    {
        claims.Add(new Claim("department", department));
    }

    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity), cancellationToken: context.RequestCancelled);
    return $"Signed in as {user}.";
});

IRouterGroupBuilder api = application.MapGroup("/api")
    .RequireAuthorization()
    .WithMetadata(ApiEndpointMetadata.Instance);

api.MapGet("status", () => "All systems go.")
    .AllowAnonymous();

api.MapGet("reports", () => "Quarterly sales report.")
    .RequireAuthorization("sales");

api.MapGet("documents/{owner}", (string owner) => $"Documents of {owner}.")
    .RequireAuthorization("owner");

api.MapDelete("users/{name}", (string name) => $"Deleted user {name}.")
    .RequireAuthorization("admins");

await application.RunAsync();
