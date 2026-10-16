// Web.StaticFiles, Example 2: a single-page application.
//
// wwwroot holds the app's shell and is served with a Cache-Control header. A second folder,
// downloads, is mounted at /downloads. /api/ping is an ordinary endpoint. Any other path that does
// not name a file falls back to index.html, so client-side routes such as /settings/profile
// load the app instead of returning 404.
//
//   dotnet run
//   Open http://localhost:5000/settings/profile, or:
//   curl -i http://localhost:5000/                                (index.html, Cache-Control: public, max-age=3600)
//   curl http://localhost:5000/downloads/release-notes.txt
//   curl http://localhost:5000/api/ping
//   curl http://localhost:5000/settings/profile                  (index.html: a client-side route)
//   curl -i http://localhost:5000/missing.png                     (404: a path that names a file)

using System;
using System.IO;

using Assimalign.Cohesion.FileSystem;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.StaticFiles;

using PhysicalFileSystem downloads = new(Path.Combine(AppContext.BaseDirectory, "downloads"));

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseStaticFiles(options => options.CacheControl = "public, max-age=3600");
application.UseStaticFiles(downloads, options => options.RequestPath = "/downloads");
application.UseRouting();

application.MapGet("/api/ping", () => "pong");

// Matches only paths without a file extension.
application.MapFallbackToFile("index.html");

await application.RunAsync();
