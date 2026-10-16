// Web.OpenApi, Example 1: an OpenAPI document for your endpoints.
//
// AddOpenApi and MapOpenApi describe the typed endpoints in an OpenAPI 3.1 document at
// /openapi/v1.json: their routes, parameters, request bodies and responses, with JSON schemas
// built from the same source-generated JSON contracts the endpoints use. Tools such as Swagger UI,
// Scalar or a client generator read it.
//
//   dotnet run
//   curl http://localhost:5000/openapi/v1.json

using System.Text.Json.Serialization;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.OpenApi;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddJsonSerialization(AppJsonContext.Default)
    .AddOpenApi();

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/books/{id:int}", (int id) => new Book(id, "The Pragmatic Programmer", 1999));
application.MapPost("/books", (Book book) => book);

application.MapOpenApi();

await application.RunAsync();

record Book(int Id, string Title, int Year);

[JsonSerializable(typeof(Book))]
partial class AppJsonContext : JsonSerializerContext;
