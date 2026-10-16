// Web.Testing, Example 2 (the application under test).
//
// An ordinary Sdk.Web application. Its greeting comes from configuration, so a test can override
// it with a command-line argument.
//
// The csproj enables orchestration, which FromProgram<Program>() needs. An orchestrated app binds
// the endpoints its host assigns (a gateway, or the test factory) instead of localhost:5000, so a
// bare `dotnet run` opens no port. Run it through its tests:
//
//   cd ../Tests
//   dotnet test

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

string greeting = builder.Configuration["Greeting"] ?? "Hello";

await using WebApplication application = builder.Build();

application.UseRouting();
application.MapGet("/hello/{name}", (string name) => $"{greeting}, {name}!");

await application.RunAsync();

// Top-level statements compile to an internal Program class. Declaring it public lets the test
// project name it: WebApplicationTestFactory.FromProgram<Program>().
public partial class Program;
