// Web.Hosting, Example 1: the smallest web application.
//
// WebApplication.CreateBuilder(args) reads configuration: appsettings.json, then
// appsettings.{environment}.json, then COHESION_CONFIG__ environment variables, then the command
// line. The listeners come from Http:Endpoints. dotnet run uses the Local environment
// (Properties/launchSettings.json), whose file listens on http://localhost:5000; Development,
// Staging and Production listen on port 8080 on every interface. Build() creates the application,
// and RunAsync() serves until the process ends. Run adds the last step of the request pipeline;
// this one answers every request.
//
//   dotnet run
//   curl http://localhost:5000/anything
//   dotnet run -- --Http:Endpoints:Main:Port=8081         (override the port from the command line)
//   dotnet run --no-launch-profile                        (Production: http://0.0.0.0:8080)

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
