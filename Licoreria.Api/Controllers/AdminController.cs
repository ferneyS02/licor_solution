using System;
using System.Linq;
using System.Threading.Tasks;
using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Sistema")]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public AdminController(AppDbContext ctx) => _ctx = ctx;

    // =========================================
    // PURGA DE VENTAS
    // DELETE: /api/admin/purge-ventas?years=5
    // =========================================
    [HttpDelete("purge-ventas")]
    public async Task<IActionResult> Purge([FromQuery] int years = 5)
    {
        if (years <= 0) return BadRequest("years debe ser mayor a 0.");

        // ✅ FechaHoraInicio la guardas en UTC => compara en UTC
        var limiteUtc = DateTime.UtcNow.AddYears(-years);

        var ordenes = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .Include(o => o.Pagos)
            .Where(o => o.FechaHoraInicio < limiteUtc)
            .ToListAsync();

        if (ordenes.Count == 0) return Ok(new { deleted = 0 });

        var detalles = ordenes.SelectMany(o => o.Detalles ?? Enumerable.Empty<DetalleOrden>()).ToList();
        var pagos = ordenes.SelectMany(o => o.Pagos ?? Enumerable.Empty<Pago>()).ToList();

        if (detalles.Count > 0) _ctx.DetallesOrden.RemoveRange(detalles);
        if (pagos.Count > 0) _ctx.Pagos.RemoveRange(pagos);

        _ctx.OrdenesMesa.RemoveRange(ordenes);

        await _ctx.SaveChangesAsync();
        return Ok(new { deleted = ordenes.Count });
    }

    // =========================================
    // RESET PASSWORD (Admin/Sistema)
    // POST: /api/admin/reset-password
    // Body:
    // { "nombre": "vendedor", "newPassword": "123456" }
    // (o por idUsuario)
    // { "idUsuario": 2, "newPassword": "123456" }
    // =========================================
    public record ResetPasswordReq(int? IdUsuario, string? Nombre, string NewPassword);

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordReq req)
    {
        if (string.IsNullOrWhiteSpace(req.NewPassword))
            return BadRequest("NewPassword es obligatorio.");

        var newPass = req.NewPassword.Trim();

        if (newPass.Length < 6)
            return BadRequest("La nueva contraseña debe tener mínimo 6 caracteres.");

        Usuario? user = null;

        if (req.IdUsuario.HasValue && req.IdUsuario.Value > 0)
        {
            user = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.IdUsuario == req.IdUsuario.Value);
        }
        else if (!string.IsNullOrWhiteSpace(req.Nombre))
        {
            var nombre = req.Nombre.Trim();
            user = await _ctx.Usuarios.FirstOrDefaultAsync(u => u.Nombre.ToLower() == nombre.ToLower());
        }
        else
        {
            return BadRequest("Debes enviar IdUsuario o Nombre.");
        }

        if (user == null)
            return NotFound("Usuario no encontrado.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
        await _ctx.SaveChangesAsync();

        return Ok(new { ok = true, user.IdUsuario, user.Nombre, user.Rol });
    }
}
