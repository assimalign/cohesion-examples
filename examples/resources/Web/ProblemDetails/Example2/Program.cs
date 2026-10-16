// Web.ProblemDetails, Example 2: domain errors as problem types.
//
// Each business rule a transfer can break has its own problem type: a stable URI that clients
// switch on, instead of parsing messages. The problems carry the data a client needs to react
// (the balance, the limit), and the same problem is logged to the console as JSON with
// ProblemDetailsWriter.
//
//   dotnet run
//   curl -i -X POST "http://localhost:5000/transfers?from=checking&amount=50"      (200)
//   curl -i -X POST "http://localhost:5000/transfers?from=checking&amount=500"     (422, insufficient-funds)
//   curl -i -X POST "http://localhost:5000/transfers?from=savings&amount=5000"     (422, limit-exceeded)
//   curl -i -X POST "http://localhost:5000/transfers?from=nowhere&amount=1"        (404)

using System;
using System.Collections.Generic;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

const decimal DailyLimit = 1000m;
Dictionary<string, decimal> balances = new() { ["checking"] = 120m, ["savings"] = 9000m };

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

application.MapPost("/transfers", async (string from, decimal amount, IHttpContext context) =>
{
    ProblemDetails? problem = null;

    if (!balances.TryGetValue(from, out decimal balance))
    {
        problem = ProblemDetails.FromStatus(HttpStatusCode.NotFound, $"There is no account '{from}'.");
    }
    else if (amount > DailyLimit)
    {
        problem = new ProblemDetails
        {
            Type = "https://bank.example/problems/limit-exceeded",
            Title = "The transfer exceeds the daily limit.",
            Status = 422,
            Detail = $"Transfers are limited to {DailyLimit} a day.",
        };
        problem.Extensions["limit"] = DailyLimit;
    }
    else if (amount > balance)
    {
        problem = new ProblemDetails
        {
            Type = "https://bank.example/problems/insufficient-funds",
            Title = "The account does not have enough funds.",
            Status = 422,
            Detail = $"The balance of '{from}' is {balance}, but the transfer is {amount}.",
        };
        problem.Extensions["balance"] = balance;
        problem.Extensions["account"] = from;
    }

    if (problem is not null)
    {
        Console.WriteLine($"Rejected transfer: {ProblemDetailsWriter.Default.WriteToString(problem)}");
        await context.Response.WriteProblemDetailsAsync(problem, context.RequestCancelled);
        return null;
    }

    balances[from] = balance - amount;
    return $"Transferred {amount} from '{from}'. New balance: {balances[from]}.";
});

await application.RunAsync();
