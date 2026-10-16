// Web.SecurityHeaders, Example 1: safe defaults on every response.
//
// UseSecurityHeaders adds the headers browsers use to block common attacks, on every response,
// errors and 404s included:
//   X-Content-Type-Options: nosniff                      (no guessing a content type)
//   Content-Security-Policy: frame-ancestors 'none'      (no framing, against clickjacking)
//   X-Frame-Options: DENY                                (the same, for older browsers)
//   Referrer-Policy: strict-origin-when-cross-origin     (no full URLs leaked to other sites)
//
//   dotnet run
//   curl -i http://localhost:5000/
//   curl -i http://localhost:5000/missing                (404, same headers)

using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.SecurityHeaders;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

// At the front of the pipeline, so every response passes through it.
application.UseSecurityHeaders();
application.UseRouting();

application.MapGet("/", () => "Check the response headers.");

await application.RunAsync();
