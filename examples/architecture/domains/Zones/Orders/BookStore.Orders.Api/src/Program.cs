// orders-api: the Orders application's API. A plain Web program, with no AddResource(...) and no platform verb.
//
// Its settings come from the orders AppHost when the ResourceManager runs it: ConnectionStrings:Orders (a mounted
// secret), Users:Url (the users application's users-api), Messages:Endpoint (the platform application's messages),
// Payments:Host, Authentication:Authority and Telemetry:Endpoint.
//
// Two things it does not know: that /api/orders on the public gateway routes to it, and that its traffic to the payment
// provider leaves through the egress gateway's addresses. Both are resources in the orders AppHost.

using System;
using System.Net.Http;
using System.Net.Http.Headers;

using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;

using BookStore.Orders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddRouting();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(authentication => authentication.AddJwtBearer(jwt =>
{
    jwt.ValidIssuers.Add(builder.Configuration["Authentication:Authority"]!);
    jwt.ValidAudiences.Add("api://bookstore/orders");
}));

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseAuthorization();
application.UseRouting();

using HttpClient users = new() { BaseAddress = new Uri(application.Configuration["Users:Url"]!) };

// The gateway strips /api/orders, so this is GET /api/orders/{id} from outside.
application.MapGet("/{id:guid}", async (Guid id, IHttpContext context) =>
{
    Order order = new() { Id = new OrderId(id), CustomerId = Guid.Empty, Total = 42.50m, PlacedAt = DateTimeOffset.UtcNow };

    // orders-api calls users-api as itself. [proposed] WorkloadIdentity, from the App kernel: managed, it presents the
    // token the ResourceManager issues this workload to the identity application's IdentityHub, which declares the client
    // `orders-api` for exactly this resource and answers with a token for api://bookstore/users. No secret exists to
    // copy or leak. Unmanaged (dotnet run), it uses the developer's own `cohesion login`.
    string token = await WorkloadIdentity.Current.GetTokenAsync("api://bookstore/users", context.RequestCancelled);

    using HttpRequestMessage request = new(HttpMethod.Get, $"/{order.CustomerId:N}");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    using HttpResponseMessage customer = await users.SendAsync(request, context.RequestCancelled);
    customer.EnsureSuccessStatusCode();

    await context.Response.WriteContentAsync(order, context.RequestCancelled);
});

await application.RunAsync();
