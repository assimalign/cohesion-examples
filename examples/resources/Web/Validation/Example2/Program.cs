// Web.Validation, Example 2: nested rules and opting out.
//
// The order profile validates the order, its shipping address (ChildRules), and every line
// (RuleForEach). Errors are keyed by the member's JSON path, such as shippingAddress.postalCode or
// lines[0].quantity. POST /orders requires a complete order. POST /drafts saves orders in
// progress, so it turns validation off with DisableValidation and accepts anything that parses.
//
//   dotnet run
//   curl -H "Content-Type: application/json" -d '{"customer":"Ada","shippingAddress":{"street":"1 Main St","postalCode":"12345"},"lines":[{"sku":"PEN","quantity":2}]}' http://localhost:5000/orders
//   curl -i -H "Content-Type: application/json" -d '{"customer":"Ada","shippingAddress":{"street":"","postalCode":"1"},"lines":[{"sku":"PEN","quantity":0}]}' http://localhost:5000/orders   (400)
//   curl -H "Content-Type: application/json" -d '{"customer":"","shippingAddress":{"street":"","postalCode":""},"lines":[]}' http://localhost:5000/drafts

using System.Text.Json.Serialization;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.ObjectValidation;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;
using Assimalign.Cohesion.Web.Validation;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddJsonSerialization(AppJsonContext.Default)
    .AddValidation(validation => validation.Options.AddProfile(new OrderProfile()));

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapPost("/orders", (Order order, IHttpContext context) =>
{
    context.Response.StatusCode = HttpStatusCode.Created;
    return new OrderReceipt(order.Customer, order.Lines.Length, "accepted");
});

application.MapPost("/drafts", (Order order) => new OrderReceipt(order.Customer, order.Lines.Length, "draft saved"))
    .DisableValidation();

await application.RunAsync();

record Address(string Street, string PostalCode);
record OrderLine(string Sku, int Quantity);
record Order(string Customer, Address ShippingAddress, OrderLine[] Lines);
record OrderReceipt(string Customer, int Lines, string Status);

sealed class OrderProfile : ValidationProfile<Order>
{
    public override void Configure(IValidationRuleDescriptor<Order> descriptor)
    {
        descriptor.RuleFor(order => order.Customer).NotEmpty();
        descriptor.RuleFor(order => order.Lines).NotEmpty();
        descriptor.RuleFor(order => order.ShippingAddress).ChildRules(address =>
        {
            address.RuleFor(a => a.Street).NotEmpty();
            address.RuleFor(a => a.PostalCode).MinLength(5);
        });
        descriptor.RuleForEach(order => order.Lines).ChildRules(line => line.RuleFor(l => l.Quantity).GreaterThan(0));
    }
}

[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(OrderReceipt))]
partial class AppJsonContext : JsonSerializerContext;
