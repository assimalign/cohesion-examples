// Web.Authentication.Cookie, Example 2: login pages, roles and APIs.
//
// The cookie scheme gets its own cookie name, login and access-denied pages, a SameSite=Strict
// cookie, and a 20-minute sliding expiration. A page challenges an anonymous visitor with a
// redirect to /login?ReturnUrl=..., and forbids a signed-in visitor without the admin role with a
// redirect to /denied. An API endpoint marked with ApiEndpointMetadata is answered 401 or 403
// instead, which is what a script expects.
//
//   dotnet run
//   curl -i http://localhost:5000/admin                                           (302 to /login)
//   curl -i http://localhost:5000/api/stats                                       (401)
//   curl -c jar.txt -b jar.txt -X POST "http://localhost:5000/login?user=linus"
//   curl -i -b jar.txt http://localhost:5000/admin                                (302 to /denied)
//   curl -c jar.txt -b jar.txt -X POST "http://localhost:5000/login?user=ada&role=admin"
//   curl -b jar.txt http://localhost:5000/admin                                   (200)

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Cookie;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddCookie(options =>
        {
            options.CookieName = "example-auth";
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/denied";
            options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
            options.SlidingExpiration = true;
            options.Cookie.SameSite = HttpCookieSameSiteMode.Strict;
        });
        authentication.Options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();

application.MapGet("/login", () => "Login page. Sign in with POST /login?user=<name>&role=<role>.");
application.MapGet("/denied", () => "Access denied.");

application.MapPost("/login", async (string user, string? role, IHttpContext context) =>
{
    List<Claim> claims = [new Claim(ClaimTypes.Name, user)];
    if (role is not null)
    {
        claims.Add(new Claim(ClaimTypes.Role, role));
    }

    ClaimsIdentity identity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity), cancellationToken: context.RequestCancelled);

    return $"Signed in as {user}.";
});

application.MapGet("/admin", async (IHttpContext context) =>
    await RequireAdminAsync(context) ? $"Welcome to the admin page, {context.User.Identity!.Name}." : null);

// Same check, but an API: 401 and 403 instead of redirects to pages.
application.MapGet("/api/stats", async (IHttpContext context) =>
    await RequireAdminAsync(context) ? "Orders today: 42" : null)
    .WithMetadata(ApiEndpointMetadata.Instance);

await application.RunAsync();

// Challenges an anonymous caller, forbids a caller who is not an admin, and admits the rest.
static async Task<bool> RequireAdminAsync(IHttpContext context)
{
    if (context.User.Identity is not { IsAuthenticated: true })
    {
        await context.ChallengeAsync(cancellationToken: context.RequestCancelled);
        return false;
    }

    if (!context.User.IsInRole("admin"))
    {
        await context.ForbidAsync(cancellationToken: context.RequestCancelled);
        return false;
    }

    return true;
}
