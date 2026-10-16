// Web.Diagnostics, Example 1: log every request.
//
// UseHttpLogging writes one log entry per request once it completes: the request line, status,
// duration, client address and trace id, plus the values of well-known headers such as Host and
// User-Agent. Any other header value is written as [Redacted], and query strings and bodies are
// left out, because they often carry secrets. Here the entries go to the console.
//
//   dotnet run
//   curl http://localhost:5000/hello
//   curl http://localhost:5000/missing
//   (the console shows an entry for each, such as "GET /hello -> 200 in 3 ms")

using Assimalign.Cohesion.Logging;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Diagnostics;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

using ILoggerFactory loggerFactory = new LoggerFactoryBuilder()
    .AddProvider(new ConsoleLoggerProvider())
    .Build();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

// First, so requests that later middleware rejects are logged too.
application.UseHttpLogging(loggerFactory);
application.UseRouting();

application.MapGet("/hello", () => "Hello!");

await application.RunAsync();
