// Web.ErrorHandling, Example 1: turn exceptions into problem details.
//
// UseErrorHandling is the exception boundary. An exception thrown anywhere after it becomes a 500
// response with an application/problem+json body (RFC 9457), and the exception's message is not
// sent to the client. Without the boundary the client gets an empty 500.
//
//   dotnet run
//   curl http://localhost:5000/ok
//   curl -i http://localhost:5000/boom      (500, {"type":"about:blank","title":"Internal Server Error","status":500})

using System;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ErrorHandling;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddErrorHandling();

await using WebApplication application = builder.Build();

// Near the front, so it catches what everything after it throws.
application.UseErrorHandling();
application.UseRouting();

application.MapGet("/ok", () => "All good.");
application.MapGet("/boom", context => throw new InvalidOperationException("The connection string is Server=db;Password=hunter2."));

await application.RunAsync();
