using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Route("api/productos")]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public ProductosController(AppDbContext ctx) => _ctx = ctx;

    [HttpGet("{idCategoria:int}")]
    public async Task<IActionResult> PorCategoria(int idCategoria) =>
        Ok(await _ctx.Productos.AsNoTracking()
            .Where(p => p.IdCategoria == idCategoria)
            .Select(p => new {
                p.IdProducto,
                p.Nombre,
                p.PrecioActual,
                p.Stock,
                p.Imagen
            })
            .OrderBy(x => x.Nombre)
            .ToListAsync());

    [HttpGet("todos")]
    public async Task<IActionResult> Todos() =>
        Ok(await _ctx.Productos.AsNoTracking()
            .Select(p => new {
                p.IdProducto,
                p.Nombre,
                p.PrecioActual,
                p.Stock,
                p.Imagen,
                p.IdCategoria
            })
            .OrderBy(x => x.Nombre)
            .ToListAsync());

    public record StockReq(int Stock);
    [HttpPut("{idProducto:int}/stock")]
    public async Task<IActionResult> SetStock(int idProducto, [FromBody] StockReq req)
    {
        var p = await _ctx.Productos.FindAsync(idProducto);
        if (p == null) return NotFound();
        p.Stock = req.Stock;
        await _ctx.SaveChangesAsync();
        return Ok(new { p.IdProducto, p.Stock });
    }

    public record PrecioReq(decimal PrecioActual);
    [HttpPut("{idProducto:int}/precio")]
    public async Task<IActionResult> SetPrecio(int idProducto, [FromBody] PrecioReq req)
    {
        var p = await _ctx.Productos.FindAsync(idProducto);
        if (p == null) return NotFound();
        p.PrecioActual = req.PrecioActual;
        await _ctx.SaveChangesAsync();
        return Ok(new { p.IdProducto, p.PrecioActual });
    }
}
