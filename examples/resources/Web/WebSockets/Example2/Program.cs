// Web.WebSockets, Example 2: chat rooms.
//
// Every socket connected to /chat/{room} receives what any member of that room sends. The client
// must offer the "chat.v1" subprotocol, and UseWebSockets pings idle sockets to keep them alive.
// UseWebSockets also refuses a handshake from another site's page (403), so the chat page is
// served by this application.
//
//   dotnet run
//   Open http://localhost:5000 in two browser tabs and chat between them, or use
//   websocat --protocol chat.v1 ws://localhost:5000/chat/lobby

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;
using Assimalign.Cohesion.Web.WebSockets;

// Each connected socket and the room it joined.
ConcurrentDictionary<WebSocket, string> members = new();

// A WebSocket allows one send at a time, so broadcasts take turns.
SemaphoreSlim sendGate = new(1, 1);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

await using WebApplication application = builder.Build();

application.UseRouting();
application.UseWebSockets(options => options.KeepAliveInterval = TimeSpan.FromSeconds(15));

application.MapGet("/", async (IHttpContext context) =>
{
    context.Response.StatusCode = HttpStatusCode.Ok;
    context.Response.Headers[HttpHeaderKey.ContentType] = "text/html; charset=utf-8";
    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(ChatPage.Html), context.RequestCancelled);
});

application.MapWebSocket(
    "/chat/{room}",
    async (context, socket) =>
    {
        if (socket.SubProtocol != "chat.v1")
        {
            await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Offer the chat.v1 subprotocol.", CancellationToken.None);
            return;
        }

        string room = context.GetRouteMatch()?.Values?["room"] as string ?? "lobby";
        members[socket] = room;

        try
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

                // Send the message to everyone in the same room, the sender included.
                ReadOnlyMemory<byte> message = buffer.AsMemory(0, received.Count).ToArray();

                await sendGate.WaitAsync(context.RequestCancelled);
                try
                {
                    foreach (WebSocket member in members.Where(entry => entry.Value == room).Select(entry => entry.Key))
                    {
                        await member.SendAsync(message, WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
                finally
                {
                    sendGate.Release();
                }
            }
        }
        finally
        {
            members.TryRemove(socket, out _);
        }
    },
    // Pick the subprotocol the server speaks, when the client offered it.
    context => new HttpWebSocketAcceptOptions
    {
        SubProtocol = context.WebSockets.RequestedProtocols.Contains("chat.v1") ? "chat.v1" : null,
    });

await application.RunAsync();

static class ChatPage
{
    public const string Html = """
        <!doctype html>
        <meta charset="utf-8">
        <title>Cohesion chat</title>
        <input id="text" placeholder="Say something" autofocus>
        <button id="send">Send</button>
        <ul id="log"></ul>
        <script>
          const socket = new WebSocket(`ws://${location.host}/chat/lobby`, "chat.v1");
          socket.onmessage = event => {
            const item = document.createElement("li");
            item.textContent = event.data;
            log.append(item);
          };
          send.onclick = () => { socket.send(text.value); text.value = ""; };
        </script>
        """;
}
