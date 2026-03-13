using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using B2BSpareParts.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace B2BSpareParts.Infrastructure.Auth;

public class JwtTokenGenerator
{
    private readonly IConfiguration _configuration;

    public JwtTokenGenerator(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string Generate(AppUser user)
    {
        var key = _configuration["Jwt:Key"] ?? "super-secret-dev-key-change-me";
        var issuer = _configuration["Jwt:Issuer"] ?? "B2BSpareParts";
        var audience = _configuration["Jwt:Audience"] ?? "B2BSparePartsClient";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("tenantId", user.TenantId.ToString())
        };

        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, expires: DateTime.UtcNow.AddDays(7), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
