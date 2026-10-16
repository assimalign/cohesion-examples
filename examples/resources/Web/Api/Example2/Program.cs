// Web.Api, Example 2: a small CRUD API.
//
// A to-do API on a route group. Every route shares the /todos prefix and the "todos" tag, each
// route has a name and a summary (which an OpenAPI document picks up), and the handlers set
// status codes themselves: 201 with a Location header on create, 404 for a missing item, 204
// after a delete. The X-Request-Id header binds to a parameter through [FromHeader].
//
//   dotnet run
//   curl -i -H "Content-Type: application/json" -d '{"title":"Write examples"}' http://localhost:5000/todos   (201)
//   curl http://localhost:5000/todos
//   curl -X PUT http://localhost:5000/todos/1/done
//   curl -i http://localhost:5000/todos/42                                                                (404)
//   curl -i -X DELETE -H "X-Request-Id: abc-123" http://localhost:5000/todos/1                           (204)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.Serialization;

ConcurrentDictionary<int, Todo> todos = new();
int lastId = 0;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddJsonSerialization(AppJsonContext.Default);

await using WebApplication application = builder.Build();

application.UseRouting();

IRouterGroupBuilder api = application.MapGroup("/todos").WithTags("todos");

api.MapGet("", () => todos.Values.OrderBy(todo => todo.Id).ToArray())
    .WithName("list-todos")
    .WithSummary("Lists every to-do.");

api.MapGet("{id:int}", (int id, IHttpContext context) =>
{
    if (todos.TryGetValue(id, out Todo? todo))
    {
        return todo;
    }

    context.Response.StatusCode = HttpStatusCode.NotFound;
    return null;
})
    .WithName("get-todo")
    .WithSummary("Gets one to-do.");

api.MapPost("", (NewTodo input, IHttpContext context) =>
{
    Todo todo = new(Interlocked.Increment(ref lastId), input.Title, Done: false);
    todos[todo.Id] = todo;

    context.Response.StatusCode = HttpStatusCode.Created;
    context.Response.Headers[HttpHeaderKey.Location] = $"/todos/{todo.Id}";
    return todo;
})
    .WithName("create-todo")
    .WithSummary("Creates a to-do.");

api.MapPut("{id:int}/done", (int id, IHttpContext context) =>
{
    if (!todos.TryGetValue(id, out Todo? todo))
    {
        context.Response.StatusCode = HttpStatusCode.NotFound;
        return null;
    }

    return todos[id] = todo with { Done = true };
})
    .WithName("complete-todo")
    .WithSummary("Marks a to-do as done.");

api.MapDelete("{id:int}", (int id, [FromHeader(Name = "X-Request-Id")] string? requestId, IHttpContext context) =>
{
    if (!todos.TryRemove(id, out _))
    {
        context.Response.StatusCode = HttpStatusCode.NotFound;
        return;
    }

    Console.WriteLine($"Deleted to-do {id} (request {requestId ?? "without an id"}).");
    context.Response.StatusCode = HttpStatusCode.NoContent;
})
    .WithName("delete-todo")
    .WithSummary("Deletes a to-do.");

await application.RunAsync();

record NewTodo(string Title);
record Todo(int Id, string Title, bool Done);

[JsonSerializable(typeof(NewTodo))]
[JsonSerializable(typeof(Todo))]
[JsonSerializable(typeof(Todo[]))]
partial class AppJsonContext : JsonSerializerContext;
