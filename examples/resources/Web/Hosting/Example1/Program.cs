// Web.Hosting, Example 1: the smallest web application.
//
// WebApplication.CreateBuilder(args) reads configuration (appsettings.json, environment variables
// and the command line) and, unless configuration declares listeners, listens on
// http://localhost:5000. Build() creates the application, and RunAsync() serves until the process
// ends. Run adds the last step of the request pipeline; this one answers every request.
//
//   dotnet run
//   curl http://localhost:5000/anything
//   dotnet run -- --Http:Endpoints:Main:Port=8080         (listen on port 8080 instead)

using System.Text;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await using WebApplication application = builder.Build();

application.Run(async context =>
{
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/plain; charset=utf-8";
    await context.Response.Body.WriteAsync(
        Encoding.UTF8.GetBytes($"Hello from Cohesion! You asked for {context.Request.Method} {context.Request.Path.Value}."),
        context.RequestCancelled);
});

await application.RunAsync();
