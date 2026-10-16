// Web.Validation, Example 1: validate a request body.
//
// A validation profile states the rules for a model. Every typed endpoint that binds a Customer
// body runs the profile before the handler, and an invalid body is answered 400 with the errors by
// member (application/problem+json). The handler only ever sees valid customers.
//
//   dotnet run
//   curl -H "Content-Type: application/json" -d '{"name":"Ada","age":36}' http://localhost:5000/customers
//   curl -i -H "Content-Type: application/json" -d '{"name":"","age":12}' http://localhost:5000/customers   (400)

using System.Text.Json.Serialization;

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
    .AddValidation(validation => validation.Options.AddProfile(new CustomerProfile()));

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapPost("/customers", (Customer customer) => $"Created customer {customer.Name}.");

await application.RunAsync();

record Customer(string Name, int Age);

sealed class CustomerProfile : ValidationProfile<Customer>
{
    public override void Configure(IValidationRuleDescriptor<Customer> descriptor)
    {
        descriptor.RuleFor(customer => customer.Name).NotEmpty();
        descriptor.RuleFor(customer => customer.Age).GreaterThanOrEqualTo(18);
    }
}

// System.Text.Json source generation: the serializer needs no reflection, so the app stays
// NativeAOT-compatible.
[JsonSerializable(typeof(Customer))]
partial class AppJsonContext : JsonSerializerContext;
