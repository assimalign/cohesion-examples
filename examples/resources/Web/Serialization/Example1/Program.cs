// Web.Serialization, Example 1: read and write JSON.
//
// AddJsonSerialization registers System.Text.Json with a source-generated context, so no
// reflection is involved and the application stays NativeAOT-compatible. A handler reads a typed
// body with ReadContentAsync and writes a typed value with WriteContentAsync. Property names are
// camelCase on the wire.
//
//   dotnet run
//   curl -i -H "Content-Type: application/json" -d '{"product":"Pen","quantity":3}' http://localhost:5000/orders

using System;
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

application.MapPost("/orders", async (IHttpContext context) =>
{
    Order? order = await context.Request.ReadContentAsync<Order>(context.RequestCancelled);

    Receipt receipt = new(Guid.NewGuid(), order!.Product, order.Quantity, order.Quantity * 2.50m);

    context.Response.StatusCode = HttpStatusCode.Created;
    await context.Response.WriteContentAsync(receipt, context.RequestCancelled);
});

await application.RunAsync();

record Order(string Product, int Quantity);
record Receipt(Guid OrderId, string Product, int Quantity, decimal Total);

// Every type the application reads or writes is listed here.
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(Receipt))]
partial class AppJsonContext : JsonSerializerContext;
