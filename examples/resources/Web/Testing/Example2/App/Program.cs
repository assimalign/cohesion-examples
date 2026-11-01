// Web.Testing, Example 2 (the application under test).
//
// An ordinary Sdk.Web application whose composition lives in GreetingApplication, so its tests
// compose the very same application in memory. Its greeting comes from configuration:
//
//   dotnet run
//   dotnet run -- --Greeting=Howdy
//   curl http://localhost:5000/hello/cohesion
//
// Run its tests:
//
//   cd ../Tests
//   dotnet test

using Assimalign.Cohesion.Web.Hosting;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
GreetingApplication.AddServices(builder);

string greeting = builder.Configuration["Greeting"] ?? "Hello";

await using WebApplication application = builder.Build();
GreetingApplication.MapEndpoints(application, greeting);

await application.RunAsync();
