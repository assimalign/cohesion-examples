// Web.Authentication.Bearer, Example 1: JWT bearer tokens.
//
// The bearer scheme validates the token in the Authorization header: its signature, issuer,
// audience and lifetime. To keep the example self-contained, POST /token issues a 10-minute ES256
// token signed with a key generated at startup, which is also the key the scheme verifies with.
// A real application gets its tokens from an identity provider instead.
//
//   dotnet run
//   TOKEN=$(curl -s -X POST "http://localhost:5000/token?user=ada")
//   curl -H "Authorization: Bearer $TOKEN" http://localhost:5000/me
//   curl -i http://localhost:5000/me                     (401, WWW-Authenticate: Bearer)

using System;
using System.Security.Cryptography;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.IdentityModel;
using Assimalign.Cohesion.IdentityModel.Token.JsonWebToken;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Bearer;
using Assimalign.Cohesion.Web.Hosting;
using Assimalign.Cohesion.Web.Routing;

const string Issuer = "https://issuer.example";
const string Audience = "example-api";

using ECDsa signingKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddRouting()
    .AddAuthentication(authentication =>
    {
        authentication.AddJwtBearer(options =>
        {
            options.SigningKeys.Add(EcdsaJwtSignatureVerifier.Create(signingKey, "example-key"));
            options.ValidIssuers.Add(Issuer);
            options.ValidAudiences.Add(Audience);
        });
        authentication.Options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    });

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();

JsonWebTokenWriter tokens = JsonWebTokenWriter.CreateEs256(signingKey, "example-key");

application.MapPost("/token", (string user) =>
{
    DateTimeOffset now = DateTimeOffset.UtcNow;
    JsonWebTokenDescriptor token = new()
    {
        Issuer = Issuer,
        Subject = new SubjectIdentifier(user, issuer: Issuer),
        IssuedAt = now,
        NotBefore = now,
        ExpiresAt = now.AddMinutes(10),
    };
    token.Audiences.Add(Audience);
    token.Claims.Add(new IdentityClaim("name", user));

    return tokens.Write(token);
});

application.MapGet("/me", async (IHttpContext context) =>
{
    if (context.User.Identity is not { IsAuthenticated: true } identity)
    {
        await context.ChallengeAsync(cancellationToken: context.RequestCancelled);
        return null;
    }

    return $"Token accepted. You are {identity.Name}.";
});

await application.RunAsync();
