// Web.Compression, Example 2: tune compression and accept compressed uploads.
//
// Responses: gzip only, the Optimal level, any body over 256 bytes, and NDJSON added to the
// compressible content types. Requests: UseRequestDecompression decompresses a gzip, Brotli or
// deflate request body before the endpoint reads it, and refuses one that expands past 1 MiB
// (413), which guards against compression bombs.
//
//   dotnet run
//   curl -s -D - -o /dev/null -H "Accept-Encoding: br, gzip" http://localhost:5000/events    (gzip, since Brotli is off)
//   seq 1 10000 | gzip | curl --data-binary @- -H "Content-Encoding: gzip" -H "Content-Type: text/plain" http://localhost:5000/upload
//   head -c 2000000 /dev/zero | gzip | curl -i --data-binary @- -H "Content-Encoding: gzip" http://localhost:5000/upload   (413)

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Compression;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseResponseCompression(options =>
{
    options.EnableBrotli = false;
    options.Level = CompressionLevel.Optimal;
    options.MinimumResponseSizeBytes = 256;
    options.MimeTypes.Add("application/x-ndjson");
});
application.UseRequestDecompression(options => options.MaxDecompressedSizeBytes = 1024 * 1024);
application.UseRouting();

application.MapGet("/events", async (IHttpContext context) =>
{
    string events = string.Join('\n', Enumerable.Range(1, 50).Select(id => $$"""{"id":{{id}},"type":"order.created"}"""));

    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "application/x-ndjson";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(events), context.RequestCancelled);
});

// The endpoint reads plain bytes; decompression already happened.
application.MapPost("/upload", async (IHttpContext context) =>
{
    using MemoryStream body = new();
    await context.Request.Body.CopyToAsync(body, context.RequestCancelled);

    return $"Received {body.Length} bytes after decompression.";
});

await application.RunAsync();
