// Web.Forms, Example 1: read a submitted form.
//
// UseForms parses every urlencoded or multipart request body before the endpoint runs, so a
// handler reads context.Request.Form without awaiting anything. A malformed form is answered with
// 400 before it reaches the handler.
//
//   dotnet run
//   curl -d "name=Ada&language=C#" http://localhost:5000/signup

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseForms();

application.MapPost("/signup", (IHttpContext context) =>
{
    IHttpFormCollection form = context.Request.Form;
    return $"Welcome, {form["name"].Value}! You write {form["language"].Value}.";
});

await application.RunAsync();
