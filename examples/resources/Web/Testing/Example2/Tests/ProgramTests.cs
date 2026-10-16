// Web.Testing, Example 2 (the tests).
//
// FromProgram<Program>() runs the application's real Program.cs in the test process, waits until
// it reports ready, and hands back an HttpClient aimed at it. Arguments reach the program as its
// command line, so a test can override configuration.
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
    public async Task Hello_UsesDefaultGreeting()
    {
        await using IWebApplicationProgramTestFactory factory = WebApplicationTestFactory.FromProgram<Program>();
        using HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync("/hello/cohesion");

        Assert.Equal("Hello, cohesion!", body);
    }

    [Fact]
    public async Task Hello_UsesGreetingFromArguments()
    {
        await using IWebApplicationProgramTestFactory factory = WebApplicationTestFactory.FromProgram<Program>(
            new WebApplicationProgramTestFactoryOptions { Arguments = ["--Greeting=Howdy"] });
        using HttpClient client = factory.CreateClient();

        string body = await client.GetStringAsync("/hello/cohesion");

        Assert.Equal("Howdy, cohesion!", body);
    }

    [Fact]
    public async Task Readiness_ReportsReady()
    {
        await using IWebApplicationProgramTestFactory factory = WebApplicationTestFactory.FromProgram<Program>();
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync("/readyz");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
