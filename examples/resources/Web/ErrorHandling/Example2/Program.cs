// Web.ErrorHandling, Example 2: map exceptions to statuses.
//
// An OnError handler turns the application's own exceptions into the right problem details: a
// KeyNotFoundException becomes 404 and an ArgumentException 400, each with its message as the
// detail. Anything else falls through to the default 500, whose message is shown only in the
// Development environment. OnException logs every fault. UseStatusCodePages gives the bodyless
// 404 and 405 answers from routing a problem-details body too.
//
//   dotnet run                                       (or DOTNET_ENVIRONMENT=Development dotnet run)
//   curl -i http://localhost:5000/orders/7           (404, "Order 7 does not exist.")
//   curl -i "http://localhost:5000/orders?limit=0"   (400, "The limit must be at least 1.")
//   curl -i http://localhost:5000/crash              (500; details only in Development)
//   curl -i http://localhost:5000/nowhere            (404 from routing, with a body)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.ErrorHandling;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

Dictionary<int, string> orders = new() { [1] = "Pen", [2] = "Notebook", [3] = "Stapler" };

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddErrorHandling(errors => errors.OnError(async (context, exception, cancellationToken) =>
    {
        ProblemDetails? problem = exception switch
        {
            KeyNotFoundException => ProblemDetails.FromStatus(HttpStatusCode.NotFound, exception.Message),
            ArgumentException => ProblemDetails.FromStatus(HttpStatusCode.BadRequest, exception.Message),
            _ => null,
        };

        if (problem is null)
        {
            return false; // not handled here: the boundary writes its default 500
        }

        await context.Response.WriteProblemDetailsAsync(problem, cancellationToken);
        return true;
    }));

bool development = builder.Environment.IsDevelopment();

await using WebApplication application = builder.Build();

application.UseErrorHandling(options =>
{
    options.IncludeDeveloperDetails = development;
    options.OnException = (context, exception) =>
    {
        Console.Error.WriteLine($"{context.Request.Method} {context.Request.Path.Value} failed: {exception.GetType().Name}: {exception.Message}");
        return ValueTask.CompletedTask;
    };
});
application.UseStatusCodePages();
application.UseRouting();

application.MapGet("/orders/{id:int}", (int id) =>
    orders.TryGetValue(id, out string? order) ? order : throw new KeyNotFoundException($"Order {id} does not exist."));

application.MapGet("/orders", (int? limit) =>
    limit < 1
        ? throw new ArgumentException("The limit must be at least 1.")
        : string.Join(", ", orders.Values.Take(limit ?? orders.Count)));

application.MapGet("/crash", context => throw new InvalidOperationException("Something nobody expected."));

await application.RunAsync();
