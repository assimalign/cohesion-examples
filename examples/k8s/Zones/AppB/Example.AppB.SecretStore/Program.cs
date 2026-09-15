using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.SecretStore;
using Assimalign.Cohesion.SecretStore.Hosting;

// Owns AppB secrets and leaf certificates under the platform trust hierarchy.
SecretStoreApplicationBuilder builder = SecretStoreApplication.CreateBuilder(args);
// SelfSeedWhenNoPlatform defaults to true for a standalone Local application.
builder.AddCertificateAuthority(ca => ca.CommonName = "Example AppB CA");

await using SecretStoreApplication application = builder.Build();
await application.RunAsync();
