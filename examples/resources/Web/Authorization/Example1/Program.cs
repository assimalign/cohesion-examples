// Web.Authorization, Example 1: require a signed-in user.
//
// RequireAuthorization() applies the default policy, which requires an authenticated user.
// UseAuthorization enforces it once routing has picked the endpoint, so an anonymous request is
// challenged before the handler runs. Here the cookie scheme answers a challenge by redirecting to
// its login path.
//
//   dotnet run
//   curl http://localhost:5000/                                       (public)
//   curl -i http://localhost:5000/profile                             (302 to /login)
//   curl -c jar.txt -X POST "http://localhost:5000/login?user=ada"
//   curl -b jar.txt http://localhost:5000/profile                     (200)

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
        authentication.AddCookie(options => options.LoginPath = "/login");
        authentication.Options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddAuthorization();

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();
application.UseAuthorization();

application.MapGet("/", () => "A public page.");

application.MapPost("/login", async (string user, IHttpContext context) =>
{
    ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, user)], CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(new ClaimsPrincipal(identity), cancellationToken: context.RequestCancelled);
    return $"Signed in as {user}.";
});

application.MapGet("/profile", (IHttpContext context) => $"Profile of {context.User.Identity!.Name}.")
    .RequireAuthorization();

await application.RunAsync();
