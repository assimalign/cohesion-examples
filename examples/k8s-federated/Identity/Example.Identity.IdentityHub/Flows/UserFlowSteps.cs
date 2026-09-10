using Assimalign.Cohesion.IdentityHub.Flows;

namespace Example.Identity.IdentityHub.Flows;

// The organization's own flow steps. A step receives the flow context (the subject so far, the request, the directory)
// and returns what happens next: continue, prompt the user, or fail. Illustrative — the IdentityHub area owns this API.

public sealed class CollectEmailStep : IUserFlowStep
{
    public ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
        => ValueTask.FromResult(context.Request.Has("email") ? FlowResult.Continue(subject: context.Subject.WithEmail(context.Request["email"])) : FlowResult.Prompt("email"));
}

public sealed class VerifyEmailStep : IUserFlowStep
{
    public async ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
    {
        if (!context.Request.Has("code"))
        {
            await context.Notifications.SendAsync(context.Subject.Email, new VerificationCode(context.Flow.Nonce), cancellation);
            return FlowResult.Prompt("code");
        }
        return context.Flow.Nonce.Matches(context.Request["code"]) ? FlowResult.Continue() : FlowResult.Fail("invalid-code");
    }
}

public sealed class CreateAccountStep : IUserFlowStep
{
    public async ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
    {
        var user = await context.Directory.Users.CreateAsync(context.Subject.Email, cancellation);
        return FlowResult.Continue(subject: context.Subject.For(user));
    }
}

public sealed class PasswordStep : IUserFlowStep
{
    public async ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
        => await context.Directory.Credentials.VerifyPasswordAsync(context.Request["email"], context.Request["password"], cancellation) is { } user
            ? FlowResult.Continue(subject: context.Subject.For(user))
            : FlowResult.Fail("invalid-credentials");
}

public sealed class TotpStep : IUserFlowStep
{
    public ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
        => ValueTask.FromResult(context.Request.Has("totp") ? (context.Subject.User.Totp.Verify(context.Request["totp"]) ? FlowResult.Continue() : FlowResult.Fail("invalid-totp")) : FlowResult.Prompt("totp"));
}

public sealed class ResetPasswordStep : IUserFlowStep
{
    public async ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
    {
        if (!context.Request.Has("password")) return FlowResult.Prompt("password");
        await context.Directory.Credentials.SetPasswordAsync(context.Subject.User, context.Request["password"], cancellation);
        return FlowResult.Continue();
    }
}

public sealed class IssueTokensStep : IUserFlowStep
{
    public async ValueTask<FlowResult> ExecuteAsync(FlowContext context, CancellationToken cancellation)
        => FlowResult.Complete(await context.Tokens.IssueAsync(context.Subject, audience: context.Request.Audience, cancellation));
}
