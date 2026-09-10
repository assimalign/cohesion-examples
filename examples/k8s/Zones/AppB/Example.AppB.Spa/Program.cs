using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Api;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.StaticFiles;
using Example.AppB.Spa;

// The appb-spa resource: static assets plus the one endpoint the browser needs to find the API.
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
WebApplication app = builder.Build();
app.UseRouting();

// The browser learns endpoints from the resource, never from a hard-coded host: the observed URL of appb-api.
app.MapGet("/cohesion.config.json", () => new
{
    references = new { appbApi = new { https = Resource.References.AppBApi.Https.Url } },
});

app.UseStaticFiles(files =>
{
    files.FileSystem = app.ContentRoot.Subdirectory("wwwroot");
    files.DefaultDocument = "index.html";
});

await app.RunAsync();
