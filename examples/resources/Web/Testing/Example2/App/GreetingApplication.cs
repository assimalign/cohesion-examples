using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

/// <summary>
/// The application's composition, kept out of Program.cs so that its tests compose the same
/// application: Program.cs applies it to the builder it runs, and the tests to the builder of a
/// WebApplicationTestFactory.
/// </summary>
public static class GreetingApplication
{
    /// <summary>
    /// Registers the application's services.
    /// </summary>
    public static void AddServices(WebApplicationBuilder builder)
    {
        builder.Services.AddRouting();
    }

    /// <summary>
    /// Composes the application's pipeline: GET /hello/{name} answers "{greeting}, {name}!".
    /// </summary>
    public static void MapEndpoints(WebApplication application, string greeting)
    {
        application.UseRouting();
        application.MapGet("/hello/{name}", (string name) => $"{greeting}, {name}!");
    }
}
