// Web.Authentication, Example 2: challenge and forbid from an endpoint.
//
// An endpoint can drive authentication itself, without UseAuthentication or an authorization
// policy. /reports authenticates the request on demand, challenges an anonymous caller (401 with
// a WWW-Authenticate header), and forbids a caller without the "reports" role (403). The API-key
// scheme puts the key owner's roles on the identity as role claims.
//
//   dotnet run
//   curl -i http://localhost:5000/reports                             (401)
//   curl -i -H "X-Api-Key: key-linus" http://localhost:5000/reports   (403)
//   curl -i -H "X-Api-Key: key-ada" http://localhost:5000/reports     (200)

using System.Collections.Generic;
using System.Linq;
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
        authentication.Options.DefaultScheme = "ApiKey";
    });

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/reports", async (IHttpContext context) =>
{
    AuthenticateResult result = await context.AuthenticateAsync("ApiKey", context.RequestCancelled);

    if (!result.Succeeded)
    {
        await context.ChallengeAsync("ApiKey", cancellationToken: context.RequestCancelled);
        return;
    }

    if (!result.Principal!.IsInRole("reports"))
    {
        await context.ForbidAsync("ApiKey", cancellationToken: context.RequestCancelled);
        return;
    }

    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/plain; charset=utf-8";
    await context.Response.Body.WriteAsync("Q3 revenue: 1,234,567"u8.ToArray(), context.RequestCancelled);
});

await application.RunAsync();

sealed class ApiKeyHandler : IAuthenticationHandler
{
    // Each key's owner and roles.
    private static readonly Dictionary<string, (string User, string[] Roles)> _keys = new()
    {
        ["key-ada"] = ("ada", ["reports", "admin"]),
        ["key-linus"] = ("linus", []),
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

        if (!_keys.TryGetValue(key, out (string User, string[] Roles) owner))
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown API key."));
        }

        IEnumerable<Claim> claims = owner.Roles
            .Select(role => new Claim(ClaimTypes.Role, role))
            .Prepend(new Claim(ClaimTypes.Name, owner.User));

        ClaimsIdentity identity = new(claims, _scheme.Name);
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
