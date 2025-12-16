using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Licoreria.Api.Controllers;

[ApiController]
[Route("api/ordenes")]
public class OrdenesController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public OrdenesController(AppDbContext ctx) => _ctx = ctx;

    // GET: /api/ordenes/abierta/{idMesa}
    [HttpGet("abierta/{idMesa:int}")]
    public async Task<IActionResult> Abierta(int idMesa)
    {
        var orden = await _ctx.OrdenesMesa.AsNoTracking()
            .Where(o => o.IdMesa == idMesa && o.Estado == "Abierta")
            .Select(o => new { o.IdOrden })
            .FirstOrDefaultAsync();

        if (orden == null) return NotFound();
        return Ok(orden);
    }

    // POST: /api/ordenes/abrir/{idMesa}?idUsuario=2
    [HttpPost("abrir/{idMesa:int}")]
    public async Task<IActionResult> Abrir(int idMesa, [FromQuery] int idUsuario = 2)
    {
        // si ya hay una abierta, devuelve esa (evita errores “mesa ocupada”)
        var ya = await _ctx.OrdenesMesa
            .Where(o => o.IdMesa == idMesa && o.Estado == "Abierta")
            .Select(o => new { o.IdOrden })
            .FirstOrDefaultAsync();
        if (ya != null)
        {
            var mesaYa = await _ctx.Mesas.FindAsync(idMesa);
            return Ok(new { IdOrden = ya.IdOrden, Mesa = mesaYa?.Nombre ?? $"Mesa{idMesa}" });
        }

        // Jornada abierta (si no existe, crear)
        var jornada = await _ctx.Jornadas.FirstOrDefaultAsync(j => j.Estado == "Abierta");
        if (jornada == null)
        {
            jornada = new Jornada
            {
                FechaJornada = DateTime.Today,
                FechaHoraInicio = DateTime.Now,
                Estado = "Abierta"
            };
            _ctx.Jornadas.Add(jornada);
            await _ctx.SaveChangesAsync();
        }

        var mesa = await _ctx.Mesas.FindAsync(idMesa);
        if (mesa == null) return NotFound("Mesa no existe");

        mesa.Estado = "Ocupada";

        var orden = new OrdenMesa
        {
            IdMesa = mesa.IdMesa,
            IdJornada = jornada.IdJornada,
            FechaHoraInicio = DateTime.Now,
            Estado = "Abierta"
        };

        _ctx.OrdenesMesa.Add(orden);
        await _ctx.SaveChangesAsync();

        return Ok(new { orden.IdOrden, Mesa = mesa.Nombre });
    }

    // GET: /api/ordenes/{idOrden} (detalle + total)
    [HttpGet("{idOrden:int}")]
    public async Task<IActionResult> Detalle(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa.AsNoTracking()
            .Include(o => o.Detalles)
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound();

        var total = orden.Detalles.Sum(d => d.Total);

        return Ok(new
        {
            orden.IdOrden,
            Mesa = orden.Mesa.Nombre,
            orden.Estado,
            Total = total,
            Lineas = orden.Detalles
                .OrderByDescending(d => d.IdDetalle)
                .Select(d => new {
                    d.IdDetalle,
                    d.NombreProducto,
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Total
                })
        });
    }

    public record AgregarReq(int IdProducto, int Cantidad);

    // POST: /api/ordenes/{idOrden}/agregar
    [HttpPost("{idOrden:int}/agregar")]
    public async Task<IActionResult> Agregar(int idOrden, [FromBody] AgregarReq req)
    {
        if (req.Cantidad <= 0) return BadRequest("Cantidad inválida");

        var orden = await _ctx.OrdenesMesa.Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null || orden.Estado != "Abierta") return BadRequest("Orden no válida");

        var prod = await _ctx.Productos.FindAsync(req.IdProducto);
        if (prod == null) return NotFound("Producto no existe");

        // (inventario) resta stock si lo manejas
        if (prod.Stock > 0)
        {
            if (prod.Stock < req.Cantidad) return BadRequest("Stock insuficiente");
            prod.Stock -= req.Cantidad;
        }

        var det = new DetalleOrden
        {
            IdOrden = orden.IdOrden,
            IdProducto = prod.IdProducto,
            NombreProducto = prod.Nombre,        // snapshot
            PrecioUnitario = prod.PrecioActual,  // snapshot
            Cantidad = req.Cantidad,
            Total = prod.PrecioActual * req.Cantidad
        };

        orden.Detalles.Add(det);
        await _ctx.SaveChangesAsync();
        return Ok("Agregado");
    }

    public record PagoReq(string TipoPago);

    // POST: /api/ordenes/{idOrden}/pagar  (monto se calcula desde detalles)
    [HttpPost("{idOrden:int}/pagar")]
    public async Task<IActionResult> Pagar(int idOrden, [FromBody] PagoReq req)
    {
        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .Include(o => o.Pagos)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no encontrada");
        if (orden.Estado != "Abierta") return BadRequest("Orden ya cerrada");

        var baseMonto = orden.Detalles.Sum(d => d.Total);

        decimal recargo = 0;
        decimal final = baseMonto;

        if (req.TipoPago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
        {
            recargo = Math.Round(baseMonto * 0.05m, 0) + 300m;
            final = baseMonto + recargo;
        }

        _ctx.Pagos.Add(new Pago
        {
            IdOrden = idOrden,
            TipoPago = req.TipoPago,
            MontoBase = baseMonto,
            Recargo = recargo,
            MontoFinal = final,
            FechaHora = DateTime.Now
        });

        await _ctx.SaveChangesAsync();
        return Ok(new { MontoBase = baseMonto, Recargo = recargo, MontoFinal = final });
    }

    // POST: /api/ordenes/{idOrden}/cerrar
    [HttpPost("{idOrden:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa.Include(o => o.Mesa).FirstOrDefaultAsync(o => o.IdOrden == idOrden);
        if (orden == null) return NotFound();

        orden.Estado = "Cerrada";
        orden.FechaHoraCierre = DateTime.Now;

        if (orden.Mesa != null) orden.Mesa.Estado = "Disponible";

        await _ctx.SaveChangesAsync();
        return Ok("Orden cerrada");
    }
}
