using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Demo.JwtAuth.Api.Services;

public sealed class JwtTokenService(string issuer, string audience, SymmetricSecurityKey signingKey)
{
    public int AccessTokenLifetimeSeconds { get; } = 300;

    public string CreateAccessToken(string username)
    {
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: [new Claim(ClaimTypes.Name, username)],
            expires: DateTime.UtcNow.AddSeconds(AccessTokenLifetimeSeconds),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
