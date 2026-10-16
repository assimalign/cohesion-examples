// Web.Query, Example 1: the HTTP QUERY method.
//
// QUERY (RFC 10008) is a safe, idempotent request that carries its query in the body: a GET with
// room for a structured search. UseQueryValidation checks every QUERY request before the endpoint
// runs. This application accepts JSON queries only, so any other Content-Type is answered 415 with
// an Accept-Query header that tells the client what to send instead.
//
//   dotnet run
//   curl -X QUERY -H "Content-Type: application/json" -d '{"name":"ada"}' http://localhost:5000/people
//   curl -i -X QUERY -H "Content-Type: text/plain" -d "name=ada" http://localhost:5000/people     (415)

using System.IO;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Query;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseQueryValidation(options => options.AcceptedMediaTypes.Add(HttpMediaType.ApplicationJson));

application.Map(HttpMethod.Query, "/people", async (IHttpContext context) =>
{
    using StreamReader reader = new(context.Request.Body);
    string query = await reader.ReadToEndAsync(context.RequestCancelled);

    return $"Searching people matching {query}";
});

await application.RunAsync();
