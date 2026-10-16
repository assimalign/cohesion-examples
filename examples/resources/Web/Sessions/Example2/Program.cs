// Web.Sessions, Example 2: sign in with a session.
//
// /login stores the user name in the session and then regenerates the session id, the standard
// defense against session fixation. /logout clears the session. The store and the cookie are
// configured explicitly: an in-memory store, a custom cookie name, and a five-minute idle timeout.
//
//   dotnet run
//   curl -c jar.txt -b jar.txt -X POST "http://localhost:5000/login?user=ada"
//   curl -c jar.txt -b jar.txt http://localhost:5000/me
//   curl -c jar.txt -b jar.txt -X POST http://localhost:5000/logout

using System;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Sessions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseSessions(new InMemoryHttpSessionStore(), options =>
{
    options.CookieName = "example-session";
    options.IdleTimeout = TimeSpan.FromMinutes(5);
});
application.UseRouting();

application.MapPost("/login", async (string user, IHttpContext context) =>
{
    IHttpSession session = await context.LoadSessionAsync(context.RequestCancelled);
    session.SetString("user", user);

    // A privilege change: issue a new session id before the response starts.
    await context.RegenerateSessionIdAsync(context.RequestCancelled);

    return $"Signed in as {user}.";
});

application.MapGet("/me", async (IHttpContext context) =>
{
    IHttpSession session = await context.LoadSessionAsync(context.RequestCancelled);
    string? user = session.GetString("user");

    return user is null ? "Not signed in." : $"Signed in as {user}.";
});

application.MapPost("/logout", async (IHttpContext context) =>
{
    IHttpSession session = await context.LoadSessionAsync(context.RequestCancelled);
    session.Clear();

    return "Signed out.";
});

await application.RunAsync();
