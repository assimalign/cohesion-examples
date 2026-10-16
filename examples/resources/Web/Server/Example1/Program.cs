// Web.Server, Example 1: features the server gives every request.
//
// - IWebRequestIdFeature carries the request's W3C trace id: the one in an incoming traceparent
//   header, or a new one.
// - IWebResponseStartingFeature runs a callback just before the response head is sent, the last
//   moment to add headers, whatever wrote the response.
// - IWebResponseCompletionFeature runs a callback after the response is sent: a place for timing
//   and cleanup that must not delay the client.
//
//   dotnet run
//   curl -i http://localhost:5000/                                                                  (X-Request-Id: a new id)
//   curl -i -H "traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01" http://localhost:5000/   (X-Request-Id: 4bf92f35...)
//   (the console shows one line per completed request)

using System;
using System.Diagnostics;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.Use(async (context, next) =>
{
    long started = Stopwatch.GetTimestamp();
    string requestId = context.Features.Get<IWebRequestIdFeature>()!.RequestId.ToHexString();
    string request = $"{context.Request.Method} {context.Request.Path.Value}";
    int status = 0;

    context.Features.Get<IWebResponseStartingFeature>()!.OnStarting(_ =>
    {
        context.Response.Headers["X-Request-Id"] = requestId;
        status = context.Response.StatusCode;
        return Task.CompletedTask;
    }, state: null);

    context.Features.Get<IWebResponseCompletionFeature>()!.Register(() =>
    {
        Console.WriteLine($"{requestId} {request} -> {status} in {Stopwatch.GetElapsedTime(started).TotalMilliseconds:F1} ms");
        return ValueTask.CompletedTask;
    });

    await next(context);
});
application.UseRouting();

application.MapGet("/", () => "Look at the X-Request-Id response header.");

await application.RunAsync();
