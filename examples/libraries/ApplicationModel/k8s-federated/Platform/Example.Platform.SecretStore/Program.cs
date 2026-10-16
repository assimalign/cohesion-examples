using Assimalign.Cohesion.SecretStore;
using Assimalign.Cohesion.SecretStore.Hosting;

// Owns the organization's root trust, intermediate enrollment, trusted issuers, and shared secret policy.
SecretStoreApplicationBuilder builder = SecretStoreApplication.CreateBuilder(args);
// SelfSeedWhenNoPlatform defaults to true.
builder.AddCertificateAuthority(ca => ca.CommonName = "Example Root CA");

await using SecretStoreApplication application = builder.Build();
await application.RunAsync();
