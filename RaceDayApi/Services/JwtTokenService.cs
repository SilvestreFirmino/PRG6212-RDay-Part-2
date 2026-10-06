using Microsoft.IdentityModel.Tokens;
using RaceDayApi.DTOs;
using RaceDayApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RaceDayApi.Services;

public class JwtTokenService
{
    private readonly IConfiguration _configuration;
    public JwtTokenService(IConfiguration configuration) => _configuration = configuration;

    public AuthResponse Create(User user)
    {
        DateTime expires = DateTime.UtcNow.AddHours(4);
        string key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is missing.");
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"], _configuration["Jwt:Audience"], claims, expires: expires, signingCredentials: credentials);
        return new AuthResponse(new JwtSecurityTokenHandler().WriteToken(token), expires,
            new UserSummary(user.UserId, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.Role, user.IsActive));
    }
}
