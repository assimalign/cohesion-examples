// The orders-api resource: an ordinary Web application that makes itself a resource on the Kubernetes
// platform. builder.Services.AddResource registers it like any other service. When the gateway runs it,
// the instance publishes its manifest through a Lease before the application serves, and the gateway
// manages it through the cluster's API server. Run with dotnet run, it is a plain Web application.
//
//   dotnet run --project Shop.Orders
//   curl http://localhost:5000

using System.Text;

using Assimalign.Cohesion.ApplicationModel.Hosting.Kubernetes;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Hosting.ApplicationModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddResource("orders-api", resource => resource.UseKubernetes());

await using WebApplication application = builder.Build();

application.Run(async context =>
{
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "application/json; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(
        $$"""{"served-by":"{{System.Environment.MachineName}}","orders":[{"id":1,"item":"coffee"},{"id":2,"item":"tea"}]}"""));
});

await application.RunAsync();
