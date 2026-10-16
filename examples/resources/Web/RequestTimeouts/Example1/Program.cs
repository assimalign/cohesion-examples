// Web.RequestTimeouts, Example 1: a time limit for every request.
//
// UseRequestTimeouts gives every request 2 seconds. When the time is up it cancels the request's
// token and answers 504 Gateway Timeout. The limit is cooperative: the handler has to pass the
// token to the work it awaits, so that the work stops. (Limits are suspended while a debugger is
// attached.)
//
//   dotnet run
//   curl -i http://localhost:5000/fast       (200)
//   curl -i http://localhost:5000/slow       (504 after 2 seconds)

using System;
using System.Threading;
using System.Threading.Tasks;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.RequestTimeouts;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseRequestTimeouts(TimeSpan.FromSeconds(2));

application.MapGet("/fast", () => "Done quickly.");

// A CancellationToken parameter is bound to the request's token, which the timeout cancels.
application.MapGet("/slow", async (CancellationToken cancellationToken) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    return "Done slowly.";
});

await application.RunAsync();
