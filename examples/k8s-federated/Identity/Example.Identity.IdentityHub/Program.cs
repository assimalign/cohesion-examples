using Assimalign.Cohesion.IdentityHub;
using Assimalign.Cohesion.IdentityHub.Hosting;

// The host declares organization-owned audiences and clients.
// Zone audiences and clients are commands in the owning Identity gateway until typed external binders exist.
IdentityHubApplicationBuilder builder = IdentityHubApplication.CreateBuilder(args);
builder.AddAudience("example-operations");
builder.AddClient("example-cli", options =>
{
    options.AllowDeviceAuthorization = true;
    options.Audiences.Add("example-operations");
});

await using IdentityHubApplication application = builder.Build();
await application.RunAsync();
