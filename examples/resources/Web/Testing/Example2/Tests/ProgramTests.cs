// Web.Testing, Example 2 (the tests).
//
// The application's own composition, GreetingApplication, applied to the builder and the application
// of a WebApplicationTestFactory: the tests run the pipeline Program.cs runs, on the factory's
// in-memory transport, over HTTP/1.1 and HTTP/2.
//
//   dotnet test

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Xunit;

using Assimalign.Cohesion.Web.Testing;

public sealed class ProgramTests
{
    [Fact]
    public async Task Hello_UsesTheGreeting()
    {
        await using WebApplicationTestFactory factory = Compose(new WebApplicationTestFactoryOptions(), "Hello");
        using HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync("/hello/cohesion");

        Assert.Equal("Hello, cohesion!", body);
    }

    [Fact]
    public async Task Hello_UsesAnotherGreeting()
    {
        await using WebApplicationTestFactory factory = Compose(new WebApplicationTestFactoryOptions(), "Howdy");
        using HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync("/hello/cohesion");

        Assert.Equal("Howdy, cohesion!", body);
    }

    [Fact]
    public async Task Hello_AnswersOverHttp2()
    {
        await using WebApplicationTestFactory factory = Compose(
            new WebApplicationTestFactoryOptions { Protocol = WebApplicationTestProtocol.Http2 },
            "Hello");
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/hello/cohesion");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(new System.Version(2, 0), response.Version);
    }

    private static WebApplicationTestFactory Compose(WebApplicationTestFactoryOptions options, string greeting)
    {
        WebApplicationTestFactory factory = new(options);
        GreetingApplication.AddServices(factory.Builder);
        GreetingApplication.MapEndpoints(factory.Application, greeting);
        return factory;
    }
}
