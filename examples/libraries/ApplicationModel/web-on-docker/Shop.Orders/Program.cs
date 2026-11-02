// The orders-api resource: an ordinary Web application that makes itself a resource on the Docker
// platform. builder.Services.AddResource registers it like any other service. When the gateway runs it
// in a container, the instance publishes its manifest on its control-plane port, where the gateway
// polls it, before the application serves, and the gateway manages it on that port. Run with dotnet
// run, it is a plain Web application.
//
//   dotnet run --project Shop.Orders
//   curl http://localhost:5000

using System.Text;

using Assimalign.Cohesion.ApplicationModel.Hosting.Docker;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Hosting.ApplicationModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddResource("orders-api", resource => resource.UseDocker());

await using WebApplication application = builder.Build();

application.Run(async context =>
{
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "application/json; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
        $$"""{"served-by":"{{System.Environment.MachineName}}","orders":[{"id":1,"item":"coffee"},{"id":2,"item":"tea"}]}"""));
});

await application.RunAsync();
