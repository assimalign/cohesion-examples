// The storefront resource: a Web application that reads the address of orders-api from its
// configuration, where the gateway delivers its Orders:Url setting (the COHESION_CONFIG__Orders__Url
// variable on its container), and answers with what orders-api returns.
//
//   dotnet run --project Shop.Storefront -- --Orders:Url=http://localhost:5000 --Http:Endpoints:Main:Port=5002
//   curl http://localhost:5002

using System;
using System.Net.Http;
using System.Text;

using Assimalign.Cohesion.ApplicationModel.Hosting.Docker;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Hosting.ApplicationModel;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
string orders = builder.Configuration["Orders:Url"] ?? throw new InvalidOperationException("The Orders:Url setting did not arrive.");
builder.Services.AddResource("storefront", resource => resource.UseDocker());

await using WebApplication application = builder.Build();
using var client = new HttpClient { BaseAddress = new Uri(orders) };

application.Run(async context =>
{
    string answer = await client.GetStringAsync("/");
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/plain; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes($"storefront ({Environment.MachineName}) -> orders-api: {answer}"));
});

await application.RunAsync();
