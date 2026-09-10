using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Api;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.StaticFiles;
using Example.AppA.Spa;

// The appa-spa resource: static assets plus the one endpoint the browser needs to find the API.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();
app.UseRouting();

// The browser learns endpoints from the resource, never from a hard-coded host: the observed URL of appa-api.
app.MapGet("/cohesion.config.json", () => new
{
    references = new { appaApi = new { https = Resource.References.AppAApi.Https.Url } },
});

app.UseStaticFiles(files =>
{
    files.FileSystem = app.ContentRoot.Subdirectory("wwwroot");
    files.DefaultDocument = "index.html";
});

await app.RunAsync();
