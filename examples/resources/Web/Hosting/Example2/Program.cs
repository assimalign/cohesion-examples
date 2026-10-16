// Web.Hosting, Example 2: background work, configuration and shutdown.
//
// A background service runs next to the web server: a ticker that counts every interval.
// AddService registers it, and the host starts it before the server and stops it after the
// server has stopped. The interval comes from configuration: appsettings.json, overridden by the
// COHESION_CONFIG__Ticker__IntervalSeconds environment variable or the command line.
//
// A plain application does not stop on Ctrl+C by itself, so the example turns Ctrl+C into the
// shutdown token RunAsync stops on. The ticker is then stopped and the application disposed.
//
//   dotnet run
//   curl http://localhost:5000/ticks
//   dotnet run -- --Ticker:IntervalSeconds=5
//   (press Ctrl+C: "Stopped after N ticks.")

using System;
using System.Threading;
using System.Threading.Tasks;

using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services.AddRouting();

int seconds = int.TryParse(builder.Configuration["Ticker:IntervalSeconds"], out int configured) ? configured : 1;
TickerService ticker = new(TimeSpan.FromSeconds(seconds));
builder.AddService(ticker);

await using WebApplication application = builder.Build();

application.UseRouting();
application.MapGet("/ticks", () => $"{ticker.Ticks} ticks so far, one every {seconds} second(s).");

using CancellationTokenSource shutdown = new();
Console.CancelKeyPress += (_, keyPress) =>
{
    keyPress.Cancel = true; // keep the process alive until the application has stopped
    shutdown.Cancel();
};

await application.RunAsync(shutdown.Token);
Console.WriteLine($"Stopped after {ticker.Ticks} ticks.");

// BackgroundService runs ExecuteAsync from StartAsync and cancels it on StopAsync.
sealed class TickerService : BackgroundService
{
    private readonly TimeSpan _interval;
    private long _ticks;

    public TickerService(TimeSpan interval)
    {
        _interval = interval;
    }

    public long Ticks => Interlocked.Read(ref _ticks);

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using PeriodicTimer timer = new(_interval);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            Interlocked.Increment(ref _ticks);
        }
    }
}
