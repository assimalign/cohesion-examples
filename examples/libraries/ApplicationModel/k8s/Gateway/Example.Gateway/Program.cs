using System;

using Assimalign.Cohesion;
using Assimalign.Cohesion.ApplicationModel;
using Assimalign.Cohesion.ApplicationModel.Gateway;

// The generated Applications members resolve each area model by invoking that gateway's
// describe mode in Local, then one Local gateway owns the combined lifecycle.
bool hasEnvironmentArgument = Array.Exists(
    args,
    argument => string.Equals(argument, "--environment", StringComparison.OrdinalIgnoreCase)
        || argument.StartsWith("--environment=", StringComparison.OrdinalIgnoreCase));
bool hasEnvironmentVariable =
    !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AppEnvironment.Keys.EnvironmentKey))
    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(AppEnvironment.Keys.DotNetEnvironmentKey));
string[] applicationSetArgs = hasEnvironmentArgument || hasEnvironmentVariable
    ? args
    : [.. args, "--environment", AppEnvironment.Keys.Local];

var gatewayOptions = new LocalGatewayOptions();
ApplicationGatewayCommandLine.Apply(gatewayOptions, applicationSetArgs);

// A member's model arrives from its gateway's describe output without provider registrations:
// providers are code. Each member registers its own, by resource name, and never inherits another
// member's. Identity and Networking own no store and read only parameter: mounts.
IApplicationSet set = Application.CreateSet(new LocalGateway(gatewayOptions), applicationSetArgs)
    .AddApplication(Applications.Platform, platform =>
    {
        platform.UseSecretStore("platform-secretstore")
            .AsCertificateAuthority()
            .AsTrustStore();
        platform.UseConfigurationStore("platform-configuration-store");
        platform.Providers.Telemetry = ResourceTelemetrySink.FromResource("platform-logspace");
    })
    .AddApplication(Applications.Identity)
    .AddApplication(Applications.Networking)
    .AddApplication(Applications.AppA, appa => appa
        .UseSecretStore("appa-secretstore")
        .AsCertificateAuthority()
        .AsTrustStore())
    .AddApplication(Applications.AppB, appb => appb
        .UseSecretStore("appb-secretstore")
        .AsCertificateAuthority()
        .AsTrustStore())
    .AddApplication(Applications.AppC, appc => appc
        .UseSecretStore("appc-secretstore")
        .AsCertificateAuthority()
        .AsTrustStore());

await set.RunAsync();
