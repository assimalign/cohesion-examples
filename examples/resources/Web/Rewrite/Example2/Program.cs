// Web.Rewrite, Example 2: canonical URLs, named captures and custom rules.
//
// - Canonical URLs: every page has one address, lowercase and without a trailing slash, and the
//   other spellings are redirected to it (308).
// - Named captures: old blog URLs move to the new scheme, ${name} inserting a named group.
// - A rewrite turns a readable product URL into the query string the endpoint expects.
// - A delegate rule can do anything. This one answers scanners probing /wp-admin with 404 before
//   the application sees the request.
// - IRewriteFeature tells an endpoint the URL the client actually asked for.
//
//   dotnet run
//   curl -i http://localhost:5000/About                    (308, Location: /about)
//   curl -i http://localhost:5000/about/                   (308, Location: /about)
//   curl -i http://localhost:5000/blog/2026/hello-world    (308, Location: /posts/hello-world?year=2026)
//   curl http://localhost:5000/products/42                 (Product 42, requested as /products/42)
//   curl -i http://localhost:5000/wp-admin/login.php       (404)

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Rewrite;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRewrite(rules =>
{
    rules.AddRedirectToLowercase();
    rules.AddRedirectToNoTrailingSlash();
    rules.AddRedirect(@"^/blog/(?<year>\d{4})/(?<slug>[a-z0-9-]+)$", "/posts/${slug}?year=${year}", HttpStatusCode.PermanentRedirect);
    rules.AddRewrite(@"^/products/(\d+)$", "/product?id=$1");
    rules.Add(context =>
    {
        if (context.Path.Value.StartsWith("/wp-admin"))
        {
            context.HttpContext.Response.StatusCode = HttpStatusCode.NotFound;
            context.EndResponse();
        }
    });
});
application.UseRouting();

application.MapGet("/about", () => "About us.");
application.MapGet("/posts/{slug}", (string slug, int year) => $"Post '{slug}' from {year}.");

application.MapGet("/product", (int id, IHttpContext context) =>
{
    IRewriteFeature? rewrite = context.Features.Get<IRewriteFeature>();
    return $"Product {id}, requested as {rewrite?.OriginalPath.Value ?? context.Request.Path.Value}.";
});

await application.RunAsync();
