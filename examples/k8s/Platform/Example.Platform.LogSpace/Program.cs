using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.LogSpace;
using Assimalign.Cohesion.LogSpace.Hosting;

// The LogSpace host builder exposes AddService and Build only; no domain-composition verbs yet.
ILogSpaceApplicationBuilder builder = LogSpaceApplication.CreateBuilder(args);

await builder.Build().RunAsync();
