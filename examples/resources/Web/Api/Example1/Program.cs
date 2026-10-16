// Web.Api, Example 1: typed endpoints.
//
// A typed endpoint is an ordinary lambda. The Cohesion source generator binds its parameters at
// compile time: a route value by name, a simple type from the query string, a complex type from
// the JSON body. It also writes what the lambda returns: a string as text, null as 204 No
// Content, and any other value as JSON. Nothing is reflected over at request time, and a value
// that does not bind is answered 400 before the lambda runs.
//
//   dotnet run
//   curl http://localhost:5000/hello/ada                     (route value)
//   curl "http://localhost:5000/add?a=2&b=3"                 (query string: 5)
//   curl -i -H "Content-Type: application/json" -d '{"name":"Pen","price":2.5}' http://localhost:5000/products   (201 with JSON)
//   curl -i "http://localhost:5000/add?a=2&b=x"              (400: b is not a number)

using System.Text.Json.Serialization;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddJsonSerialization(AppJsonContext.Default);

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/hello/{name}", (string name) =>
{
    return $"Hello, {name}!";
});
application.MapGet("/add", (int a, int b) => a + b);
application.MapPost("/products", (NewProduct product, IHttpContext context) =>
{
    context.Response.StatusCode = HttpStatusCode.Created;
    return new Product(1, product.Name, product.Price);
});

await application.RunAsync();

record NewProduct(string Name, decimal Price);
record Product(int Id, string Name, decimal Price);

[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(NewProduct))]
[JsonSerializable(typeof(Product))]
partial class AppJsonContext : JsonSerializerContext;
