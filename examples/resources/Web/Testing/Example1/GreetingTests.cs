// Web.Testing, Example 1: test a pipeline in memory.
//
// WebApplicationTestFactory hosts a WebApplication on an in-memory transport. The test composes
// the application through factory.Builder and factory.Application, then sends real HTTP requests
// through the HttpClient the factory creates. No port is opened.
//
//   dotnet test

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Testing;

public sealed class GreetingTests
{
    [Fact]
    public async Task Hello_ReturnsGreeting()
    {
        await using WebApplicationTestFactory factory = new();
        factory.Builder.Services.AddRouting();
        factory.Application.UseRouting();
        factory.Application.MapGet("/hello/{name}", (string name) => $"Hello, {name}!");

        using HttpClient client = factory.CreateClient();
        string body = await client.GetStringAsync("/hello/cohesion");

        Assert.Equal("Hello, cohesion!", body);
    }

    [Fact]
    public async Task UnknownPath_Returns404()
    {
        await using WebApplicationTestFactory factory = new();
        factory.Builder.Services.AddRouting();
        factory.Application.UseRouting();
        factory.Application.MapGet("/hello/{name}", (string name) => $"Hello, {name}!");

        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/goodbye");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
