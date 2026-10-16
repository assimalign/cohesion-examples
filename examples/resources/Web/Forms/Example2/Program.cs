// Web.Forms, Example 2: file uploads with limits.
//
// UseForms parses with the limits of the form feature on the request. A middleware ahead of it
// installs a feature with tighter limits than the defaults, so a form over them is answered with
// 413 (application/problem+json) and never reaches the handler. /upload lists every file it got.
//
//   dotnet run
//   curl -F "title=Report" -F "file=@Program.cs" http://localhost:5000/upload
//   curl -i -F "file=@some-file-over-1MiB" http://localhost:5000/upload                  (413)

using System.Text;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

HttpFormOptions limits = new()
{
    MultipartBodyLengthLimit = 1024 * 1024, // at most 1 MiB of multipart body
    ValueCountLimit = 10,                   // at most 10 fields and files
};

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();

// A form feature installed ahead of UseForms wins over the default one.
application.Use((context, next) =>
{
    context.Features.Set<IHttpFormFeature>(new HttpFormFeature(context.Request, limits));
    return next(context);
});
application.UseForms();

application.MapPost("/upload", (IHttpContext context) =>
{
    IHttpFormCollection form = context.Request.Form;
    StringBuilder summary = new();
    summary.AppendLine($"Title: {form["title"].Value}");

    foreach (IHttpFormFile file in form.Files)
    {
        summary.AppendLine($"{file.Name}: {file.FileName}, {file.Length} bytes, {file.ContentType}");
    }

    return summary.ToString();
});

await application.RunAsync();
