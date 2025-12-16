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

    // 1) Obtener orden abierta por mesa (para no abrir 2 veces)
    [HttpGet("abierta/mesa/{idMesa:int}")]
    public async Task<IActionResult> GetAbiertaPorMesa(int idMesa)
    {
        var orden = await _ctx.OrdenesMesa
            .AsNoTracking()
            .Where(o => o.IdMesa == idMesa && o.Estado == "Abierta")
            .Select(o => new { o.IdOrden, Mesa = o.Mesa.Nombre })
            .FirstOrDefaultAsync();

        if (orden == null) return Ok(null);
        return Ok(orden);
    }

    // 2) Abrir orden (si ya hay abierta, devolverla)
    [HttpPost("abrir/{idMesa:int}")]
    public async Task<IActionResult> Abrir(int idMesa, [FromQuery] int idUsuario = 2)
    {
        // Si ya hay una orden abierta para esa mesa: devolverla
        var ya = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdMesa == idMesa && o.Estado == "Abierta");
        if (ya != null)
            return Ok(new { ya.IdOrden, Mesa = ya.Mesa.Nombre });

        // Jornada abierta o crear
        var jornada = await _ctx.Jornadas.FirstOrDefaultAsync(j => j.Estado == "Abierta");
        if (jornada == null)
        {
            jornada = new Jornada
            {
                FechaJornada = DateTime.Today,
                FechaHoraInicio = DateTime.Now,
                UsuarioInicio = idUsuario,
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
            Usuario = idUsuario,
            Estado = "Abierta",
            TipoPago = "Normal"
        };

        _ctx.OrdenesMesa.Add(orden);
        await _ctx.SaveChangesAsync();

        return Ok(new { orden.IdOrden, Mesa = mesa.Nombre });
    }

    public record AgregarReq(int IdProducto, int Cantidad);

    // 3) Agregar producto: SI YA EXISTE EN LA ORDEN, SUMA CANTIDAD
    [HttpPost("{idOrden:int}/agregar")]
    public async Task<IActionResult> Agregar(int idOrden, [FromBody] AgregarReq req)
    {
        if (req.Cantidad <= 0) return BadRequest("Cantidad inválida");

        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null || orden.Estado != "Abierta")
            return BadRequest("Orden no válida");

        var prod = await _ctx.Productos.FindAsync(req.IdProducto);
        if (prod == null) return NotFound("Producto no existe");

        // Inventario (si quieres controlar stock real)
        if (prod.Stock > 0 && prod.Stock < req.Cantidad)
            return BadRequest("Stock insuficiente");

        // Si ya existe línea del mismo producto, sumamos
        var linea = orden.Detalles.FirstOrDefault(d => d.IdProducto == req.IdProducto);
        if (linea == null)
        {
            linea = new DetalleOrden
            {
                IdOrden = orden.IdOrden,
                IdProducto = prod.IdProducto,
                NombreProducto = prod.Nombre,
                PrecioUnitario = prod.PrecioActual,
                Cantidad = req.Cantidad,
                Total = prod.PrecioActual * req.Cantidad
            };
            orden.Detalles.Add(linea);
        }
        else
        {
            linea.Cantidad += req.Cantidad;
            linea.Total = linea.PrecioUnitario * linea.Cantidad;
        }

        // Descontar stock si lo usas
        if (prod.Stock > 0) prod.Stock -= req.Cantidad;

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // 4) Detalle + Total calculado (esto es lo que tu WPF necesita)
    [HttpGet("{idOrden:int}/detalle")]
    public async Task<IActionResult> Detalle(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa
            .AsNoTracking()
            .Include(o => o.Mesa)
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound();

        var lineas = orden.Detalles
            .OrderBy(d => d.NombreProducto)
            .Select(d => new
            {
                d.IdProducto,
                d.NombreProducto,
                d.PrecioUnitario,
                d.Cantidad,
                d.Total
            })
            .ToList();

        var total = lineas.Sum(x => x.Total);

        return Ok(new
        {
            orden.IdOrden,
            Mesa = orden.Mesa.Nombre,
            orden.Estado,
            Lineas = lineas,
            Total = total
        });
    }

    public record PagarReq(string TipoPago);

    // 5) Pagar: NO PIDAS MONTO, se calcula SOLO con detalle
    [HttpPost("{idOrden:int}/pagar")]
    public async Task<IActionResult> Pagar(int idOrden, [FromBody] PagarReq req)
    {
        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no existe");
        if (orden.Estado != "Abierta") return BadRequest("Orden no está abierta");

        var montoBase = orden.Detalles.Sum(d => d.Total);

        if (montoBase <= 0)
            return BadRequest("No hay productos en la orden");

        decimal recargo = 0;
        decimal final = montoBase;

        if (req.TipoPago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
        {
            recargo = Math.Round(montoBase * 0.05m, 0) + 300m;
            final = montoBase + recargo;
        }

        _ctx.Pagos.Add(new Pago
        {
            IdOrden = idOrden,
            MontoBase = montoBase,
            TipoPago = req.TipoPago,
            Recargo = recargo,
            MontoFinal = final,
            FechaHora = DateTime.Now
        });

        await _ctx.SaveChangesAsync();

        return Ok(new { MontoBase = montoBase, Recargo = recargo, MontoFinal = final });
    }

    // 6) Cerrar mesa
    [HttpPost("{idOrden:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound();

        orden.Estado = "Cerrada";
        orden.FechaHoraCierre = DateTime.Now;

        if (orden.Mesa != null)
            orden.Mesa.Estado = "Disponible";

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
