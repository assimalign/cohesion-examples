// Web.Query, Example 2: conditional queries and stored results.
//
// QUERY /products searches a catalog that rarely changes. UseQueryConditionals compares the
// client's If-None-Match with the catalog's current ETag and answers 304 before the endpoint runs,
// so a client that already holds the result is not sent it again. An Accept header the
// application cannot produce is answered 406.
//
// QUERY /reports runs a query, saves the result, and answers 303 See Other with a GET URL for it,
// which the client can bookmark or share.
//
//   dotnet run
//   curl -i -X QUERY -H "Content-Type: application/json" -d '{"maxPrice":20}' http://localhost:5000/products
//   curl -i -X QUERY -H "Content-Type: application/json" -H 'If-None-Match: "catalog-v1"' -d '{"maxPrice":20}' http://localhost:5000/products   (304)
//   curl -i -X QUERY -H "Content-Type: application/json" -H "Accept: text/html" -d '{}' http://localhost:5000/products                          (406)
//   curl -i -X QUERY -H "Content-Type: application/json" -d '{"year":2026}' http://localhost:5000/reports      (303, Location: /reports/<id>)
//   curl http://localhost:5000/reports/<id>

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Query;
using Assimalign.Cohesion.Web.Routing;

HttpEntityTag catalogVersion = HttpEntityTag.Strong("catalog-v1");
ConcurrentDictionary<string, string> reports = new();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseQueryValidation(options =>
{
    options.AcceptedMediaTypes.Add(HttpMediaType.ApplicationJson);
    options.SupportedResponseMediaTypes.Add(HttpMediaType.ApplicationJson);
});

// Tells the conditional middleware which version of the catalog a /products query would read.
application.UseQueryConditionals(context => ValueTask.FromResult<WebQueryResourceValidators?>(
    context.Request.Path.Value == "/products"
        ? new WebQueryResourceValidators { ETag = catalogVersion, HasCurrentRepresentation = true }
        : null));

application.Map(HttpMethod.Query, "/products", async (IHttpContext context) =>
{
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "application/json";
    context.Response.Headers[HttpHeaderKey.ETag] = catalogVersion.ToString();
    await context.Response.Body.WriteAsync("""[{"name":"Pen","price":3},{"name":"Notebook","price":12}]"""u8.ToArray(), context.RequestCancelled);
});

application.Map(HttpMethod.Query, "/reports", async (IHttpContext context) =>
{
    using StreamReader reader = new(context.Request.Body);
    string query = await reader.ReadToEndAsync(context.RequestCancelled);

    string id = Guid.NewGuid().ToString("N")[..8];
    reports[id] = $$"""{"report":"{{id}}","query":{{query}},"rows":42}""";

    context.Response.RedirectQueryToGet($"/reports/{id}");
});

application.MapGet("/reports/{id}", async (string id, IHttpContext context) =>
{
    if (!reports.TryGetValue(id, out string? report))
    {
        context.Response.StatusCode = HttpStatusCode.NotFound;
        return;
    }

    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "application/json";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(report), context.RequestCancelled);
});

await application.RunAsync();
