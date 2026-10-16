// Web.RequestTimeouts, Example 2: limits per endpoint.
//
// The default policy gives every request 2 seconds and answers a timeout with problem details.
// /report is allowed 10 seconds. /export has a policy of its own that writes the whole timeout
// response itself: 503 with a message. /backup can run as long as it needs, so it turns the
// limit off. /search sets its limit at run time from the ?budget= query value.
//
//   dotnet run
//   curl -i http://localhost:5000/report             (200 after 3 seconds: inside its 10-second limit)
//   curl -i http://localhost:5000/search             (504 problem details after 2 seconds)
//   curl -i "http://localhost:5000/search?budget=5"  (200 after 3 seconds)
//   curl -i http://localhost:5000/export             (503 after 1 second)
//   curl -i http://localhost:5000/backup             (200 after 4 seconds: no limit)

using System;
using System.Threading;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.RequestTimeouts;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseRequestTimeouts(options => options.DefaultPolicy = new RequestTimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(2),
    WriteProblemDetails = true,
});

application.MapGet("/report", async (CancellationToken cancellationToken) =>
{
    await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
    return "Report ready.";
})
    .WithRequestTimeout(TimeSpan.FromSeconds(10));

application.MapGet("/search", async (int? budget, IHttpContext context) =>
{
    if (budget is not null)
    {
        context.Features.Get<IRequestTimeoutFeature>()?.SetTimeout(TimeSpan.FromSeconds(budget.Value));
    }

    await Task.Delay(TimeSpan.FromSeconds(3), context.RequestCancelled);
    return "Search results.";
});

application.MapGet("/export", async (CancellationToken cancellationToken) =>
{
    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
    return "Export finished.";
})
    .WithRequestTimeout(new RequestTimeoutPolicy
    {
        Timeout = TimeSpan.FromSeconds(1),

        // When set, WriteResponse owns the response, status code included.
        WriteResponse = async context =>
        {
            context.Response.StatusCode = HttpStatusCode.ServiceUnavailable;
            context.Response.Headers[HttpHeaderKey.ContentType] = "text/plain; charset=utf-8";
            await context.Response.Body.WriteAsync("The export is taking too long. Try a smaller date range."u8.ToArray());
        },
    });

application.MapGet("/backup", async (CancellationToken cancellationToken) =>
{
    await Task.Delay(TimeSpan.FromSeconds(4), cancellationToken);
    return "Backup complete.";
})
    .DisableRequestTimeout();

await application.RunAsync();
