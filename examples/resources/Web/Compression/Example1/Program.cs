// Web.Compression, Example 1: compress responses.
//
// UseResponseCompression compresses a response with Brotli or gzip when the client's
// Accept-Encoding allows it, the content type is text-like, and the body is larger than 1 KiB.
// Small responses are sent as they are, because compressing them saves nothing.
//
//   dotnet run
//   curl -s -D - -o /dev/null -H "Accept-Encoding: br, gzip" http://localhost:5000/report   (Content-Encoding: br)
//   curl -s -D - -o /dev/null -H "Accept-Encoding: gzip" http://localhost:5000/report       (Content-Encoding: gzip)
//   curl --compressed http://localhost:5000/report                                          (curl decompresses it)
//   curl -s -D - -H "Accept-Encoding: gzip" http://localhost:5000/ping                      (too small: not compressed)

using System.Linq;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Compression;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseResponseCompression();
application.UseRouting();

// About 6 KiB of very repetitive text.
application.MapGet("/report", () =>
    string.Join('\n', Enumerable.Range(1, 200).Select(day => $"Day {day}: 42 orders, 0 returns")));

application.MapGet("/ping", () => "pong");

await application.RunAsync();
