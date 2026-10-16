// Web.OpenApi, Example 2: a documented API.
//
// The document gets a title, version and description, a tag description, and a bearer security
// scheme. The orders routes carry their tag, names, summaries and descriptions into the document.
// An internal route is left out with ExcludeFromDescription. The same document is also served
// as YAML, and as OpenAPI 3.0 for tools that do not read 3.1 yet.
//
//   dotnet run
//   curl http://localhost:5000/openapi/v1.json
//   curl http://localhost:5000/openapi/v1.yaml
//   curl http://localhost:5000/openapi/v1-3.0.json

using System.Text.Json.Serialization;

using Assimalign.Cohesion.OpenApi;
using Assimalign.Cohesion.OpenApi.Attributes;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.OpenApi;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddJsonSerialization(AppJsonContext.Default)
    .AddOpenApi(openApi =>
    {
        openApi.Options.Title = "Orders API";
        openApi.Options.ApiVersion = "2.1.0";
        openApi.Options.Description = "Create and track customer orders.";
        openApi.Options.AddTag(new OpenApiTagMetadata { Name = "orders", Description = "Customer orders." });
        openApi.Options.AddSecurityScheme(new OpenApiSecuritySchemeMetadata
        {
            Name = "Bearer",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
    });

await using WebApplication application = builder.Build();

application.UseRouting();

IRouterGroupBuilder orders = application.MapGroup("/orders").WithTags("orders");

orders.MapGet("{id:int}", (int id) => new Order(id, "Pen", 3, OrderStatus.Shipped))
    .WithName("getOrder")
    .WithSummary("Gets an order.")
    .WithDescription("Returns the order with the given id, including its current status.");

orders.MapPost("", (NewOrder order) => new Order(1, order.Product, order.Quantity, OrderStatus.Received))
    .WithName("createOrder")
    .WithSummary("Places an order.");

application.MapPost("/internal/cache/clear", () => "Cache cleared.")
    .ExcludeFromDescription();

application.MapOpenApi();
application.MapOpenApi("/openapi/v1.yaml");
application.MapOpenApi("/openapi/v1-3.0.json", OpenApiSpecVersion.V3_0);

await application.RunAsync();

record NewOrder(string Product, int Quantity);
record Order(int Id, string Product, int Quantity, OrderStatus Status);

[JsonConverter(typeof(JsonStringEnumConverter<OrderStatus>))]
enum OrderStatus { Received, Shipped, Delivered }

[JsonSerializable(typeof(NewOrder))]
[JsonSerializable(typeof(Order))]
partial class AppJsonContext : JsonSerializerContext;
