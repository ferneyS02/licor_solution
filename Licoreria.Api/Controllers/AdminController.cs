using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public AdminController(AppDbContext ctx) => _ctx = ctx;

    // DELETE: /api/admin/purge-ventas?years=5
    [HttpDelete("purge-ventas")]
    public async Task<IActionResult> Purge([FromQuery] int years = 5)
    {
        var limite = DateTime.Now.AddYears(-years);

        var ordenes = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .Include(o => o.Pagos)
            .Where(o => o.FechaHoraInicio < limite)
            .ToListAsync();

        if (ordenes.Count == 0) return Ok(new { deleted = 0 });

        _ctx.DetallesOrden.RemoveRange(ordenes.SelectMany(o => o.Detalles));
        _ctx.Pagos.RemoveRange(ordenes.SelectMany(o => o.Pagos));
        _ctx.OrdenesMesa.RemoveRange(ordenes);

        await _ctx.SaveChangesAsync();
        return Ok(new { deleted = ordenes.Count });
    }
}
