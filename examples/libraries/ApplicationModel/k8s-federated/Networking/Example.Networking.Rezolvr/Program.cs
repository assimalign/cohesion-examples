using Assimalign.Cohesion.Rezolvr;
using Assimalign.Cohesion.Rezolvr.Hosting;

// The Rezolvr host builder exposes AddService and Build only; no domain-composition verbs yet.
RezolvrApplicationBuilder builder = RezolvrApplication.CreateBuilder(args);

await using RezolvrApplication application = builder.Build();
await application.RunAsync();
