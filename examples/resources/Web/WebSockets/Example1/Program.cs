// Web.WebSockets, Example 1: an echo endpoint.
//
// MapWebSocket accepts the handshake and hands the handler a plain System.Net.WebSockets.WebSocket.
// The handler sends every message back until the client closes the socket.
//
//   dotnet run
//   websocat ws://localhost:5000/echo        (type a line; the server sends it back)

using System;
using System.Net.WebSockets;
using System.Threading;

using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.WebSockets;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseWebSockets();

application.MapWebSocket("/echo", async (context, socket) =>
{
    byte[] buffer = new byte[4096];

    while (true)
    {
        ValueWebSocketReceiveResult received = await socket.ReceiveAsync(buffer.AsMemory(), context.RequestCancelled);

        if (received.MessageType == WebSocketMessageType.Close)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            return;
        }

        await socket.SendAsync(buffer.AsMemory(0, received.Count), received.MessageType, received.EndOfMessage, context.RequestCancelled);
    }
});

await application.RunAsync();
