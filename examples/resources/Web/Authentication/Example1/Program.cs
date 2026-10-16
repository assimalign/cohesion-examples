// Web.Authentication, Example 1: a custom authentication scheme.
//
// A scheme turns request data into a user. This one reads the X-Api-Key header and looks the key
// up in a table. UseAuthentication runs the default scheme on every request and sets context.User
// when it succeeds. A request without a key stays anonymous.
//
//   dotnet run
//   curl http://localhost:5000/whoami                             (anonymous)
//   curl -H "X-Api-Key: key-ada" http://localhost:5000/whoami     (ada)

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddScheme(new AuthenticationScheme("ApiKey", "API key", () => new ApiKeyHandler()));

        // No scheme is picked for you, even when only one is registered.
        authentication.Options.DefaultScheme = "ApiKey";
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();

application.MapGet("/whoami", (IHttpContext context) =>
    context.User.Identity is { IsAuthenticated: true } identity
        ? $"Hello, {identity.Name}!"
        : "Hello, anonymous!");

await application.RunAsync();

// A new handler is created for every request that uses the scheme.
sealed class ApiKeyHandler : IAuthenticationHandler
{
    private static readonly Dictionary<string, string> _users = new()
    {
        ["key-ada"] = "ada",
        ["key-linus"] = "linus",
    };

    private AuthenticationScheme _scheme = null!;
    private IHttpContext _context = null!;

    public Task InitializeAsync(AuthenticationScheme scheme, IHttpContext context, CancellationToken cancellationToken = default)
    {
        _scheme = scheme;
        _context = context;
        return Task.CompletedTask;
    }

    public Task<AuthenticateResult> AuthenticateAsync(CancellationToken cancellationToken = default)
    {
        string? key = _context.Request.Headers.GetValue("X-Api-Key");

        if (key is null)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (!_users.TryGetValue(key, out string? user))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown API key."));
        }

        // The authentication type (the scheme name) is what makes the identity authenticated.
        ClaimsIdentity identity = new([new Claim(ClaimTypes.Name, user)], _scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), null, _scheme.Name)));
    }

    public Task ChallengeAsync(AuthenticationProperties? properties, CancellationToken cancellationToken = default)
    {
        _context.Response.StatusCode = HttpStatusCode.Unauthorized;
        _context.Response.Headers[HttpHeaderKey.WWWAuthenticate] = _scheme.Name;
        return Task.CompletedTask;
    }

    public Task ForbidAsync(AuthenticationProperties? properties, CancellationToken cancellationToken = default)
    {
        _context.Response.StatusCode = HttpStatusCode.Forbidden;
        return Task.CompletedTask;
    }
}
