// Web.Authentication.Cookie, Example 1: sign in with a cookie.
//
// POST /login signs the user in: the cookie scheme writes the user into an encrypted cookie.
// Later requests send the cookie back, and UseAuthentication turns it into context.User again.
// POST /logout deletes the cookie. Anonymous requests to /me are challenged, which for the cookie
// scheme means a redirect to its login page.
//
//   dotnet run
//   curl -c jar.txt -b jar.txt -X POST "http://localhost:5000/login?user=ada"
//   curl -b jar.txt http://localhost:5000/me
//   curl -c jar.txt -b jar.txt -X POST http://localhost:5000/logout
//   curl -i -b jar.txt http://localhost:5000/me          (302 to /Account/Login)

using System.Security.Claims;

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
        authentication.AddCookie();
        authentication.Options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();

application.MapPost("/login", async (string user, IHttpContext context) =>
{
    ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, user)], CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity), cancellationToken: context.RequestCancelled);

    return $"Signed in as {user}.";
});

application.MapGet("/me", async (IHttpContext context) =>
{
    if (context.User.Identity is not { IsAuthenticated: true } identity)
    {
        await context.ChallengeAsync(cancellationToken: context.RequestCancelled);
        return null;
    }

    return $"You are {identity.Name}.";
});

application.MapPost("/logout", async (IHttpContext context) =>
{
    await context.SignOutAsync(cancellationToken: context.RequestCancelled);
    return "Signed out.";
});

await application.RunAsync();
