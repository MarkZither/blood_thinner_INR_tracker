using System.Security.Claims;
using BloodThinnerTracker.Api.Services.Authentication;
using Xunit;

namespace BloodThinnerTracker.Api.Tests.Services.Authentication;

public class IdTokenClaimResolverTests
{
    [Fact]
    public void ResolveExternalUserId_PrefersOid()
    {
        var claims = new[] { new Claim("oid", "OID-123"), new Claim("sub", "SUB-1") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = IdTokenClaimResolver.ResolveExternalUserId(principal);

        Assert.Equal("OID-123", result);
    }

    [Fact]
    public void ResolveExternalUserId_UsesObjectIdentifierSchema()
    {
        var claims = new[] { new Claim("http://schemas.microsoft.com/identity/claims/objectidentifier", "OBJ-456") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = IdTokenClaimResolver.ResolveExternalUserId(principal);

        Assert.Equal("OBJ-456", result);
    }

    [Fact]
    public void ResolveExternalUserId_FallsBackToSub()
    {
        var claims = new[] { new Claim("sub", "SUB-ONLY") };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var result = IdTokenClaimResolver.ResolveExternalUserId(principal);

        Assert.Equal("SUB-ONLY", result);
    }

    [Fact]
    public void ResolveExternalUserId_ReturnsNullWhenNoClaims()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var result = IdTokenClaimResolver.ResolveExternalUserId(principal);

        Assert.Null(result);
    }
}
