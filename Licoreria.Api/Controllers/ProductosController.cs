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
            .Select(p => new { p.IdProducto, p.Nombre, p.PrecioActual, p.Imagen })
            .ToListAsync());
}
