using System.Text.RegularExpressions;
using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Sistema")]
[Route("api/admin/productos")]
public class AdminProductosController : ControllerBase
{
    private readonly AppDbContext _ctx;
    private readonly IWebHostEnvironment _env;

    public AdminProductosController(AppDbContext ctx, IWebHostEnvironment env)
    {
        _ctx = ctx;
        _env = env;
    }

    public record ProductoCreateReq(string Nombre, decimal PrecioActual, int Stock, int IdCategoria, string? Imagen);
    public record ProductoUpdateReq(string Nombre, decimal PrecioActual, int Stock, int IdCategoria, string? Imagen);
    public record PrecioReq(decimal PrecioActual);

    // ✅ READ 1: Listar (con filtros opcionales)
    // GET: /api/admin/productos?q=poker&idCategoria=2
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? q = null, [FromQuery] int? idCategoria = null)
    {
        var query = _ctx.Productos.AsNoTracking().AsQueryable();

        if (idCategoria.HasValue)
            query = query.Where(p => p.IdCategoria == idCategoria.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim().ToLower();
            query = query.Where(p => p.Nombre.ToLower().Contains(term));
        }

        var items = await query
            .OrderBy(p => p.Nombre)
            .Select(p => new
            {
                p.IdProducto,
                p.Nombre,
                p.PrecioActual,
                p.Stock,
                p.IdCategoria,
                p.Imagen
            })
            .ToListAsync();

        return Ok(items);
    }

    // ✅ READ 2: Obtener uno
    // GET: /api/admin/productos/10
    [HttpGet("{idProducto:int}")]
    public async Task<IActionResult> GetById(int idProducto)
    {
        var p = await _ctx.Productos.AsNoTracking()
            .Where(x => x.IdProducto == idProducto)
            .Select(x => new
            {
                x.IdProducto,
                x.Nombre,
                x.PrecioActual,
                x.Stock,
                x.IdCategoria,
                x.Imagen
            })
            .FirstOrDefaultAsync();

        return p == null ? NotFound("Producto no existe.") : Ok(p);
    }

    // ✅ CREATE
    // POST: /api/admin/productos
    [HttpPost]
    public async Task<IActionResult> Crear([FromBody] ProductoCreateReq req)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre))
            return BadRequest("Nombre es obligatorio.");

        if (req.PrecioActual < 0)
            return BadRequest("PrecioActual no puede ser negativo.");

        if (req.Stock < 0)
            return BadRequest("Stock no puede ser negativo.");

        var catExiste = await _ctx.Categorias.AnyAsync(c => c.IdCategoria == req.IdCategoria);
        if (!catExiste)
            return BadRequest("IdCategoria no existe.");

        var p = new Producto
        {
            Nombre = req.Nombre.Trim(),
            PrecioActual = req.PrecioActual,
            Stock = req.Stock,
            IdCategoria = req.IdCategoria,
            Imagen = string.IsNullOrWhiteSpace(req.Imagen) ? null : req.Imagen.Trim()
        };

        _ctx.Productos.Add(p);
        await _ctx.SaveChangesAsync();

        return Ok(new { p.IdProducto, p.Nombre, p.PrecioActual, p.Stock, p.IdCategoria, p.Imagen });
    }

    // ✅ UPDATE (todo)
    // PUT: /api/admin/productos/10
    [HttpPut("{idProducto:int}")]
    public async Task<IActionResult> Actualizar(int idProducto, [FromBody] ProductoUpdateReq req)
    {
        var p = await _ctx.Productos.FirstOrDefaultAsync(x => x.IdProducto == idProducto);
        if (p == null) return NotFound("Producto no existe.");

        if (string.IsNullOrWhiteSpace(req.Nombre))
            return BadRequest("Nombre es obligatorio.");

        if (req.PrecioActual < 0)
            return BadRequest("PrecioActual no puede ser negativo.");

        if (req.Stock < 0)
            return BadRequest("Stock no puede ser negativo.");

        var catExiste = await _ctx.Categorias.AnyAsync(c => c.IdCategoria == req.IdCategoria);
        if (!catExiste)
            return BadRequest("IdCategoria no existe.");

        p.Nombre = req.Nombre.Trim();
        p.PrecioActual = req.PrecioActual;
        p.Stock = req.Stock;
        p.IdCategoria = req.IdCategoria;
        p.Imagen = string.IsNullOrWhiteSpace(req.Imagen) ? null : req.Imagen.Trim();

        await _ctx.SaveChangesAsync();
        return Ok(new { p.IdProducto, p.Nombre, p.PrecioActual, p.Stock, p.IdCategoria, p.Imagen });
    }

    // ✅ UPDATE SOLO PRECIO
    // PATCH: /api/admin/productos/10/precio
    [HttpPatch("{idProducto:int}/precio")]
    public async Task<IActionResult> CambiarPrecio(int idProducto, [FromBody] PrecioReq req)
    {
        if (req.PrecioActual < 0)
            return BadRequest("PrecioActual no puede ser negativo.");

        var p = await _ctx.Productos.FirstOrDefaultAsync(x => x.IdProducto == idProducto);
        if (p == null) return NotFound("Producto no existe.");

        p.PrecioActual = req.PrecioActual;
        await _ctx.SaveChangesAsync();

        return Ok(new { p.IdProducto, p.Nombre, p.PrecioActual });
    }

    // ✅ DELETE (solo si nunca se vendió)
    // DELETE: /api/admin/productos/10
    [HttpDelete("{idProducto:int}")]
    public async Task<IActionResult> Eliminar(int idProducto)
    {
        var p = await _ctx.Productos.FirstOrDefaultAsync(x => x.IdProducto == idProducto);
        if (p == null) return NotFound("Producto no existe.");

        var usadoEnVentas = await _ctx.DetallesOrden.AnyAsync(d => d.IdProducto == idProducto);
        if (usadoEnVentas)
            return Conflict("No se puede eliminar: el producto ya fue usado en ventas. (Recomendación: inactivar en vez de borrar)");

        _ctx.Productos.Remove(p);
        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true, eliminado = idProducto });
    }

    // ✅ UPLOAD IMAGEN (desde el Desktop)
    // POST: /api/admin/productos/imagen  (multipart/form-data, campo: "file")
    [HttpPost("imagen")]
    [RequestSizeLimit(10_000_000)] // 10MB
    public async Task<IActionResult> SubirImagen([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Archivo requerido.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new HashSet<string> { ".png", ".jpg", ".jpeg", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest("Formato no permitido. Solo: png, jpg, jpeg, webp.");

        var baseName = Path.GetFileNameWithoutExtension(file.FileName);
        baseName = Regex.Replace(baseName, @"[^a-zA-Z0-9_\-]+", "_").Trim('_');
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "img";

        var finalName = $"{baseName}_{DateTime.UtcNow:yyyyMMddHHmmssfff}{ext}";

        var webRoot = _env.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRoot))
            webRoot = Path.Combine(AppContext.BaseDirectory, "wwwroot");

        var dir = Path.Combine(webRoot, "imagenes");
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, finalName);
        using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        return Ok(new { FileName = finalName, Url = $"/imagenes/{finalName}" });
    }
}
