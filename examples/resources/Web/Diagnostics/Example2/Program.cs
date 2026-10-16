// Web.Diagnostics, Example 2: an access log file.
//
// The same request log goes to the console and to an access log file in the Combined log
// format, the one most log analyzers read. The entries add the query string, and the
// X-Correlation-Id header joins the headers whose values are logged; Authorization and any other
// header off that list still show as [Redacted]. /healthz is polled constantly by monitoring, so
// it is not logged at all.
//
//   dotnet run
//   curl -H "X-Correlation-Id: abc-123" -H "Authorization: Bearer secret" "http://localhost:5000/search?q=pens"
//   curl http://localhost:5000/healthz                  (not logged)
//   (the access log path is printed at startup)

using System;
using System.IO;

using Assimalign.Cohesion.Logging;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Diagnostics;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

string logDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
Console.WriteLine($"Access log directory: {logDirectory}");

using ILoggerFactory loggerFactory = new LoggerFactoryBuilder()
    .AddProvider(new ConsoleLoggerProvider())
    .AddProvider(new W3CAccessLogProvider(new W3CAccessLogOptions
    {
        Directory = logDirectory,
        Format = AccessLogFormat.Combined,
    }))
    .Build();

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseHttpLogging(loggerFactory, options =>
{
    options.Fields = HttpLoggingFields.Default | HttpLoggingFields.RequestQuery;
    options.AllowedRequestHeaders.Add("X-Correlation-Id");
});
application.UseRouting();

application.MapGet("/search", (string q) => $"Results for '{q}'.");

application.MapGet("/healthz", () => "Healthy")
    .WithHttpLogging(HttpLoggingFields.None);

await application.RunAsync();
