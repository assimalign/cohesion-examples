// Web.ProblemDetails, Example 1: write error responses in a standard format.
//
// ProblemDetails is the RFC 9457 error body (application/problem+json), and
// WriteProblemDetailsAsync writes one with its status and content type. FromStatus fills in the
// standard title for a status code. A problem can also name its own type, the instance it is
// about, and extension members.
//
//   dotnet run
//   curl -i http://localhost:5000/books/99         (404 with the standard title)
//   curl -i http://localhost:5000/books/1/loan     (409 with a custom type, instance and extensions)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapGet("/books/{id:int}", async (int id, IHttpContext context) =>
{
    if (id != 1)
    {
        await context.Response.WriteProblemDetailsAsync(
            ProblemDetails.FromStatus(HttpStatusCode.NotFound, $"There is no book {id}."),
            context.RequestCancelled);
        return null;
    }

    return "The Pragmatic Programmer";
});

application.MapGet("/books/{id:int}/loan", async (int id, IHttpContext context) =>
{
    ProblemDetails problem = new()
    {
        Type = "https://library.example/problems/already-on-loan",
        Title = "The book is already on loan.",
        Status = 409,
        Detail = $"Book {id} is due back in 5 days.",
        Instance = $"/books/{id}/loan",
    };
    problem.Extensions["dueInDays"] = 5;
    problem.Extensions["waitlist"] = new[] { "ada", "linus" };

    await context.Response.WriteProblemDetailsAsync(problem, context.RequestCancelled);
});

await application.RunAsync();
