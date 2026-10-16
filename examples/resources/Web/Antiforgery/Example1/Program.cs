// Web.Antiforgery, Example 1: protect a POST endpoint from cross-site request forgery.
//
// GET /token issues a token pair: a cookie and a request token. A POST to /transfer must send the
// cookie back together with the request token in the X-CSRF-TOKEN header, or it is rejected with
// 400. GET, HEAD, OPTIONS and TRACE never need a token.
//
//   dotnet run
//   TOKEN=$(curl -s -c cookies.txt http://localhost:5000/token)
//   curl -i -b cookies.txt -X POST -H "X-CSRF-TOKEN: $TOKEN" http://localhost:5000/transfer   (200)
//   curl -i -b cookies.txt -X POST http://localhost:5000/transfer                             (400)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Antiforgery;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAntiforgery();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseAntiforgery();

// Mints the token pair: the cookie goes on the response, the request token is the body.
application.MapGet("/token", (IHttpContext context) =>
{
    HttpAntiforgeryTokenSet tokens = context.RequireAntiforgery.GetAndStoreTokens(context);
    return tokens.RequestToken ?? string.Empty;
});

// UseAntiforgery validates the token before this handler runs.
application.MapPost("/transfer", () => "Transfer accepted.")
    .RequireAntiforgery();

await application.RunAsync();
