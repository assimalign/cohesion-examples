using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.LogSpace;
using Assimalign.Cohesion.LogSpace.Hosting;

// The LogSpace host builder exposes AddService and Build only; no domain-composition verbs yet.
LogSpaceApplicationBuilder builder = LogSpaceApplication.CreateBuilder(args);

await using LogSpaceApplication application = builder.Build();
await application.RunAsync();
