using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Licoreria.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IConfiguration _cfg;

    public AuthController(AppDbContext ctx, IConfiguration cfg)
    {
        _ctx = ctx;
        _cfg = cfg;
    }

    public record LoginReq(string Nombre, string Password);
    public record LoginResp(int IdUsuario, string Nombre, string Rol, string Token, DateTime ExpiresAtUtc);

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest("Nombre y Password son obligatorios");

        var nombre = req.Nombre.Trim();

        // ✅ Login tolerante: ignora mayúsculas/minúsculas
        var user = await _ctx.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Nombre.ToLower() == nombre.ToLower());

        if (user == null)
            return Unauthorized("Usuario o contraseña inválidos");

        var ok = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!ok)
            return Unauthorized("Usuario o contraseña inválidos");

        var (token, expiresAtUtc) = CreateToken(user.IdUsuario, user.Nombre, user.Rol);

        return Ok(new LoginResp(user.IdUsuario, user.Nombre, user.Rol, token, expiresAtUtc));
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var nombre = User.FindFirstValue(ClaimTypes.Name);
        var rol = User.FindFirstValue(ClaimTypes.Role);
        return Ok(new { IdUsuario = id, Nombre = nombre, Rol = rol });
    }

    // ========= CAMBIAR CONTRASEÑA (Admin/Sistema) =========
    public record ChangePasswordReq(string CurrentPassword, string NewPassword);

    [HttpPost("change-password")]
    [Authorize(Roles = "Admin,Sistema")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordReq req)
    {
        if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest("Contraseña actual y nueva son obligatorias.");

        var newPass = req.NewPassword.Trim();
        if (newPass.Length < 6)
            return BadRequest("La nueva contraseña debe tener mínimo 6 caracteres.");

        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(idStr, out var idUsuario))
            return Unauthorized();

        var user = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == idUsuario);
        if (user == null)
            return Unauthorized();

        var ok = BCrypt.Net.BCrypt.Verify(req.CurrentPassword, user.PasswordHash);
        if (!ok)
            return BadRequest("La contraseña actual no es correcta.");

        // (opcional) evitar misma contraseña
        if (BCrypt.Net.BCrypt.Verify(newPass, user.PasswordHash))
            return BadRequest("La nueva contraseña no puede ser igual a la actual.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
        await _ctx.SaveChangesAsync();

        return Ok(new { ok = true });
    }

    private (string token, DateTime expiresAtUtc) CreateToken(int idUsuario, string nombre, string rol)
    {
        var jwt = _cfg.GetSection("Jwt");
        var key = jwt["Key"] ?? throw new InvalidOperationException("Falta Jwt:Key");
        var issuer = jwt["Issuer"] ?? "Licoreria45";
        var audience = jwt["Audience"] ?? "Licoreria45Desktop";
        var minutes = int.TryParse(jwt["ExpiresMinutes"], out var m) ? m : 720;

        var expiresAtUtc = DateTime.UtcNow.AddMinutes(minutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, idUsuario.ToString()),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Role, rol)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: creds
        );

        var jwtString = new JwtSecurityTokenHandler().WriteToken(token);
        return (jwtString, expiresAtUtc);
    }
}
