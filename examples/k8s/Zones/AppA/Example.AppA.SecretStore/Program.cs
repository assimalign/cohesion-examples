using Assimalign.Cohesion.Hosting;
using Assimalign.Cohesion.SecretStore;
using Assimalign.Cohesion.SecretStore.Hosting;

// Owns AppA secrets and leaf certificates under the platform trust hierarchy.
ISecretStoreApplicationBuilder builder = SecretStoreApplication.CreateBuilder(args);
// SelfSeedWhenNoPlatform defaults to true for a standalone Local application.
builder.AddCertificateAuthority(ca => ca.CommonName = "Example AppA CA");

await builder.Build().RunAsync();
