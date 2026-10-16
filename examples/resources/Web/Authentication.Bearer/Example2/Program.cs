// Web.Authentication.Bearer, Example 2: roles in tokens, enforced by policy.
//
// The token's "roles" claim becomes role claims on the user, and Web.Authorization enforces them
// per endpoint. A request without a valid token is challenged (401). A valid token without the
// admin role is forbidden (403, WWW-Authenticate: Bearer error="insufficient_scope"). The realm
// names the protected API in every challenge.
//
//   dotnet run
//   USER_TOKEN=$(curl -s -X POST "http://localhost:5000/token?user=linus")
//   ADMIN_TOKEN=$(curl -s -X POST "http://localhost:5000/token?user=ada&role=admin")
//   curl -i http://localhost:5000/admin                                         (401)
//   curl -i -H "Authorization: Bearer $USER_TOKEN" http://localhost:5000/admin  (403)
//   curl -H "Authorization: Bearer $ADMIN_TOKEN" http://localhost:5000/admin    (200)

using System;
using System.Security.Cryptography;

using Assimalign.Cohesion.Http;
using Assimalign.Cohesion.IdentityModel;
using Assimalign.Cohesion.IdentityModel.Token.JsonWebToken;
using Assimalign.Cohesion.Web;
using Assimalign.Cohesion.Web.Authentication;
using Assimalign.Cohesion.Web.Authentication.Bearer;
using Assimalign.Cohesion.Web.Authorization;
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
            options.ClockSkew = TimeSpan.FromSeconds(30);
            options.Realm = "example-api";
        });
        authentication.Options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddAuthorization(authorization => authorization.Options.AddPolicy("admins", policy => policy.RequireRole("admin")));

await using WebApplication application = builder.Build();

application.UseAuthentication();
application.UseRouting();
application.UseAuthorization();

JsonWebTokenWriter tokens = JsonWebTokenWriter.CreateEs256(signingKey, "example-key");

application.MapPost("/token", (string user, string? role) =>
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

    if (role is not null)
    {
        token.Claims.Add(new IdentityClaim("roles", IdentityClaimValue.FromArray([role])));
    }

    return tokens.Write(token);
});

application.MapGet("/me", (IHttpContext context) => $"You are {context.User.Identity!.Name}.")
    .RequireAuthorization();

application.MapGet("/admin", (IHttpContext context) => $"Welcome, administrator {context.User.Identity!.Name}.")
    .RequireAuthorization("admins");

await application.RunAsync();
