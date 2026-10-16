// Web.Sessions, Example 1: a visit counter.
//
// UseSessions gives each visitor a session, identified by a cookie and stored in memory. The
// session is loaded on first use, so a request that never touches it costs nothing.
//
//   dotnet run
//   curl -c jar.txt -b jar.txt http://localhost:5000/      (run it a few times)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Sessions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseSessions();
application.UseRouting();

application.MapGet("/", async (IHttpContext context) =>
{
    IHttpSession session = await context.LoadSessionAsync(context.RequestCancelled);

    int visits = (session.GetInt32("visits") ?? 0) + 1;
    session.SetInt32("visits", visits);

    return $"Visits in this session: {visits}";
});

await application.RunAsync();
