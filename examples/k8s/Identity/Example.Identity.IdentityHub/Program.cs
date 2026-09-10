using Assimalign.Cohesion.IdentityHub;
using Assimalign.Cohesion.IdentityHub.Flows;
using Assimalign.Cohesion.IdentityHub.Hosting;
using Assimalign.Cohesion.Web.Health;
using Example.Identity.IdentityHub;
using Example.Identity.IdentityHub.Flows;

// The identity-hub resource: directory + token service + OIDC issuer for the whole organization, with the organization's
// own user flows composed as pipelines. The IdentityHub builder API used here is illustrative — the IdentityHub area owns it. What this scaffold fixes is only that the resource is an ordinary executable (Program.cs), that orchestration is the opt-in in the csproj, and that its orchestration-facing facts live there.
IdentityHubApplicationBuilder builder = IdentityHubApplication.CreateBuilder(args);

builder.AddDirectory(directory => directory.UseEmbeddedDatabase(Resource.Mounts.Data));   // Database.Embedded (R10)
builder.AddTokens(tokens => tokens.UseSigningKeys(Resource.Mounts.Signing));               // ES256 keys held by the Platform SecretStore
builder.AddOpenIdConnect(oidc => oidc.Issuer = Resource.Settings.IdentityHubIssuer);

// User flows are pipelines the organization writes itself: each step is a class in Flows/; the hub runs them on its own
// endpoints and persists flow state in the directory.
builder.AddUserFlow("signup", flow => flow
    .Step<CollectEmailStep>()
    .Step<VerifyEmailStep>()
    .Step<CreateAccountStep>()
    .Then<IssueTokensStep>());
builder.AddUserFlow("signin", flow => flow
    .Step<PasswordStep>()
    .Branch(user => user.MfaEnrolled, then => then.Step<TotpStep>())
    .Then<IssueTokensStep>());
builder.AddUserFlow("recover", flow => flow
    .Step<CollectEmailStep>()
    .Step<VerifyEmailStep>()
    .Step<ResetPasswordStep>());

// No zone is listed here. Each zone declares its own token audience and clients as commands on identity-hub
// (identity.AddAudience / identity.AddClient in Zones/*/Example.*.Gateway/Program.cs); the zone's gateway delivers them to
// this resource's default control plane once identity-hub is Running, and they are owned by that zone.

builder.AddHealthCheck("signing-keys", cancellation => Health.FileExistsAsync(Resource.Mounts.Signing, cancellation));

await builder.Build().RunAsync();
