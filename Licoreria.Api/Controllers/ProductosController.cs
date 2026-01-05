using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public ProductosController(AppDbContext ctx) => _ctx = ctx;

    [HttpGet("productos/{idCategoria:int}")]
    public async Task<IActionResult> PorCategoria(int idCategoria) =>
        Ok(await _ctx.Productos
            .AsNoTracking()
            .Where(p => p.IdCategoria == idCategoria)
            .OrderBy(p => p.Orden)
            .ThenBy(p => p.IdProducto)
            .Select(p => new { p.IdProducto, p.Nombre, p.PrecioActual, p.Imagen, p.Orden })
            .ToListAsync());

    // ✅ Guardar orden (solo Admin/Sistema)
    public record OrdenReq(List<int> Ids);

    [HttpPut("productos/{idCategoria:int}/orden")]
    [Authorize(Roles = "Admin,Sistema")]
    public async Task<IActionResult> GuardarOrden(int idCategoria, [FromBody] OrdenReq req)
    {
        if (req?.Ids == null || req.Ids.Count == 0)
            return BadRequest("Ids es obligatorio.");

        var ids = req.Ids.Where(x => x > 0).Distinct().ToList();
        if (ids.Count == 0) return BadRequest("Ids inválidos.");

        var productos = await _ctx.Productos
            .Where(p => p.IdCategoria == idCategoria && ids.Contains(p.IdProducto))
            .ToListAsync();

        if (productos.Count != ids.Count)
            return BadRequest("Algunos productos no existen o no pertenecen a la categoría.");

        // asigna 1..N según el orden recibido
        for (int i = 0; i < ids.Count; i++)
        {
            var id = ids[i];
            var prod = productos.First(p => p.IdProducto == id);
            prod.Orden = i + 1;
        }

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
