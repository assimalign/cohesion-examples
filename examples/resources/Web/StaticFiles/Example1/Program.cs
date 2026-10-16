// Web.StaticFiles, Example 1: serve a folder of files.
//
// UseStaticFiles serves the files under wwwroot, which Sdk.Web copies next to the application.
// A request for / gets index.html, the default document. Every file response carries an ETag, so
// a client that already has the file gets 304 Not Modified, and Range requests get 206.
//
//   dotnet run
//   Open http://localhost:5000, or:
//   curl -i http://localhost:5000/css/site.css
//   curl -i -H 'If-None-Match: "<the ETag from the previous response>"' http://localhost:5000/css/site.css   (304)
//   curl -i -r 0-9 http://localhost:5000/css/site.css                                                        (206)

using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.StaticFiles;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

await using WebApplication application = builder.Build();

application.UseStaticFiles();

await application.RunAsync();
