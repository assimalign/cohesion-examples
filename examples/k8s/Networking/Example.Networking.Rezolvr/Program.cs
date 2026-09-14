using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.Rezolvr;
using Assimalign.Cohesion.Rezolvr.Hosting;

// The Rezolvr host builder exposes AddService and Build only; no domain-composition verbs yet.
IRezolvrApplicationBuilder builder = RezolvrApplication.CreateBuilder(args);

await builder.Build().RunAsync();
