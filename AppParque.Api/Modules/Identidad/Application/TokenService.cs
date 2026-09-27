using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AppParque.Api.Modules.Identidad.Domain;
using Microsoft.IdentityModel.Tokens;

namespace AppParque.Api.Modules.Identidad.Application;

public class TokenService(IConfiguration config) : ITokenService
{
    public string CreateAccessToken(Usuario usuario)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var credenciales = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(ClaimTypes.Name, usuario.Username),
            new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var minutos = config.GetValue("Jwt:AccessTokenMinutes", 20);

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(minutos),
            signingCredentials: credenciales);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, RefreshToken Entity) CreateRefreshToken(int usuarioId)
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var dias = config.GetValue("Jwt:RefreshTokenDays", 10);

        var entity = new RefreshToken
        {
            UsuarioId = usuarioId,
            TokenHash = HashToken(rawToken),
            ExpiraEn = DateTime.UtcNow.AddDays(dias),
        };

        return (rawToken, entity);
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
