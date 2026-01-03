using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ordenes")]
public class OrdenesController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public OrdenesController(AppDbContext ctx) => _ctx = ctx;

    private static DateTime UtcNow() => DateTime.UtcNow;
    private static DateTime UtcToday() => DateTime.UtcNow.Date;

    private int? GetUserIdFromToken()
    {
        var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(idStr, out var id) ? id : null;
    }

    private async Task<int> GetSistemaUserIdAsync()
    {
        var id = await _ctx.Usuarios.AsNoTracking()
            .Where(u => u.Nombre == "sistema")
            .Select(u => u.IdUsuario)
            .FirstOrDefaultAsync();

        if (id > 0) return id;

        id = await _ctx.Usuarios.AsNoTracking()
            .Select(u => u.IdUsuario)
            .FirstOrDefaultAsync();

        return id;
    }

    [HttpGet("abierta/{idMesa:int}")]
    public async Task<IActionResult> GetOrdenAbierta(int idMesa)
    {
        var orden = await _ctx.OrdenesMesa
            .AsNoTracking()
            .Include(o => o.Mesa)
            .Where(o => o.IdMesa == idMesa && o.Estado == "Abierta")
            .Select(o => new { o.IdOrden, Mesa = o.Mesa.Nombre })
            .FirstOrDefaultAsync();

        return new JsonResult(orden);
    }

    [HttpGet("{idOrden:int}/detalle")]
    public async Task<IActionResult> Detalle(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa
            .AsNoTracking()
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no existe");

        var lineas = (orden.Detalles ?? new List<DetalleOrden>())
            .OrderBy(d => d.NombreProducto)
            .Select(d => new
            {
                d.IdDetalle,
                d.IdProducto,
                d.NombreProducto,
                d.PrecioUnitario,
                d.Cantidad,
                d.Total
            })
            .ToList();

        var total = lineas.Sum(x => x.Total);
        return Ok(new { Lineas = lineas, Total = total });
    }

    [HttpPost("abrir/{idMesa:int}")]
    public async Task<IActionResult> Abrir(int idMesa)
    {
        var mesa = await _ctx.Mesas.FindAsync(idMesa);
        if (mesa == null) return NotFound("Mesa no existe");

        var existente = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdMesa == idMesa && o.Estado == "Abierta");

        if (existente != null)
            return Ok(new { existente.IdOrden, Mesa = existente.Mesa!.Nombre });

        var idUsuario = GetUserIdFromToken() ?? await GetSistemaUserIdAsync();
        if (idUsuario <= 0) return StatusCode(500, "No hay usuario base en BD (Seed).");

        var jornada = await _ctx.Jornadas.FirstOrDefaultAsync(j => j.Estado == "Abierta");
        if (jornada == null)
        {
            jornada = new Jornada
            {
                FechaJornada = UtcToday(),
                FechaHoraInicio = UtcNow(),
                UsuarioInicio = idUsuario,
                Estado = "Abierta"
            };
            _ctx.Jornadas.Add(jornada);
            await _ctx.SaveChangesAsync();
        }

        var orden = new OrdenMesa
        {
            IdMesa = mesa.IdMesa,
            IdJornada = jornada.IdJornada,
            FechaHoraInicio = UtcNow(),
            IdUsuario = idUsuario,
            Estado = "Abierta",
            TipoPago = "Normal"
        };

        mesa.Estado = "Ocupada";
        _ctx.OrdenesMesa.Add(orden);
        await _ctx.SaveChangesAsync();

        return Ok(new { orden.IdOrden, Mesa = mesa.Nombre });
    }

    public record AgregarReq(int IdProducto, int Cantidad);

    [HttpPost("{idOrden:int}/agregar")]
    public async Task<IActionResult> Agregar(int idOrden, [FromBody] AgregarReq req)
    {
        if (req.Cantidad <= 0) return BadRequest("Cantidad inválida");

        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null || orden.Estado != "Abierta") return BadRequest("Orden no válida");

        // ✅ Si ya se pagó, no se puede modificar
        var yaPagada = await _ctx.Pagos.AsNoTracking().AnyAsync(p => p.IdOrden == idOrden);
        if (yaPagada) return BadRequest("Orden ya pagada. No se puede modificar.");

        var prod = await _ctx.Productos.FindAsync(req.IdProducto);
        if (prod == null) return NotFound("Producto no existe");

        orden.Detalles ??= new List<DetalleOrden>();

        var existente = orden.Detalles.FirstOrDefault(d => d.IdProducto == prod.IdProducto);
        var enOrden = existente?.Cantidad ?? 0;

        // ✅ validar stock
        if (prod.Stock < (enOrden + req.Cantidad))
            return Conflict($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.Stock}, en orden: {enOrden}.");

        if (existente != null)
        {
            existente.Cantidad += req.Cantidad;
            existente.Total = existente.PrecioUnitario * existente.Cantidad;
        }
        else
        {
            orden.Detalles.Add(new DetalleOrden
            {
                IdOrden = orden.IdOrden,
                IdProducto = prod.IdProducto,
                NombreProducto = prod.Nombre,
                PrecioUnitario = prod.PrecioActual,
                Cantidad = req.Cantidad,
                Total = prod.PrecioActual * req.Cantidad
            });
        }

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    // ✅ NUEVO: QUITAR PRODUCTOS (antes de pagar)
    public record QuitarReq(int IdProducto, int Cantidad);

    [HttpPost("{idOrden:int}/quitar")]
    public async Task<IActionResult> Quitar(int idOrden, [FromBody] QuitarReq req)
    {
        if (req.Cantidad <= 0) return BadRequest("Cantidad inválida");

        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null || orden.Estado != "Abierta") return BadRequest("Orden no válida");

        // ✅ Si ya se pagó, no se puede modificar por aquí
        var yaPagada = await _ctx.Pagos.AsNoTracking().AnyAsync(p => p.IdOrden == idOrden);
        if (yaPagada) return BadRequest("Orden ya pagada. No se puede modificar. Usa /cancelar para anular (Admin/Sistema).");

        orden.Detalles ??= new List<DetalleOrden>();

        var detalle = orden.Detalles.FirstOrDefault(d => d.IdProducto == req.IdProducto);
        if (detalle == null) return NotFound("El producto no está en la orden.");

        if (req.Cantidad >= detalle.Cantidad)
        {
            // elimina la línea completa
            orden.Detalles.Remove(detalle);
            _ctx.DetallesOrden.Remove(detalle);
        }
        else
        {
            detalle.Cantidad -= req.Cantidad;
            detalle.Total = detalle.PrecioUnitario * detalle.Cantidad;
        }

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }

    public record PagoReq(string TipoPago);

    [HttpPost("{idOrden:int}/pagar")]
    public async Task<IActionResult> Pagar(int idOrden, [FromBody] PagoReq req)
    {
        // ✅ Evita pagar 2 veces
        var yaPagada = await _ctx.Pagos.AsNoTracking().AnyAsync(p => p.IdOrden == idOrden);
        if (yaPagada) return BadRequest("Esta orden ya fue pagada.");

        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no existe");
        if (orden.Estado != "Abierta") return BadRequest("La orden no está abierta.");

        var detalles = orden.Detalles ?? new List<DetalleOrden>();
        if (detalles.Count == 0) return BadRequest("La orden no tiene productos.");

        var montoBase = detalles.Sum(d => d.Total);
        if (montoBase <= 0) return BadRequest("La orden no tiene productos.");

        decimal recargo = 0;
        decimal final = montoBase;

        if (req.TipoPago.Equals("Tarjeta", StringComparison.OrdinalIgnoreCase))
        {
            recargo = Math.Round(montoBase * 0.05m, 0) + 300m;
            final = montoBase + recargo;
        }

        await using var tx = await _ctx.Database.BeginTransactionAsync();

        // ✅ Descontar stock al pagar
        foreach (var d in detalles)
        {
            var prod = await _ctx.Productos.FirstOrDefaultAsync(p => p.IdProducto == d.IdProducto);
            if (prod == null)
            {
                await tx.RollbackAsync();
                return Conflict($"Producto {d.IdProducto} ya no existe.");
            }

            if (prod.Stock < d.Cantidad)
            {
                await tx.RollbackAsync();
                return Conflict($"Stock insuficiente para {prod.Nombre}. Disponible: {prod.Stock}, requerido: {d.Cantidad}.");
            }

            prod.Stock -= d.Cantidad;
        }

        _ctx.Pagos.Add(new Pago
        {
            IdOrden = idOrden,
            MontoBase = montoBase,
            TipoPago = req.TipoPago,
            Recargo = recargo,
            MontoFinal = final,
            FechaHora = UtcNow()
        });

        orden.TipoPago = req.TipoPago;

        await _ctx.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { MontoBase = montoBase, Recargo = recargo, MontoFinal = final });
    }

    // ✅ NUEVO: CANCELAR ORDEN / ANULAR VENTA (devuelve stock si estaba pagada)
    // - Si NO está pagada: cualquier usuario puede cancelar (borra orden y libera mesa)
    // - Si SÍ está pagada: solo Admin/Sistema y requiere ?confirm=ANULAR (devuelve stock y borra pago + orden)
    [HttpDelete("{idOrden:int}/cancelar")]
    public async Task<IActionResult> Cancelar(int idOrden, [FromQuery] string? confirm = null)
    {
        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .Include(o => o.Detalles)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no existe");

        var pagos = await _ctx.Pagos.Where(p => p.IdOrden == idOrden).ToListAsync();
        var pagada = pagos.Count > 0;

        if (!pagada)
        {
            // Cancelación simple (sin devolver stock porque aún no se descontó)
            if (orden.Mesa != null)
                orden.Mesa.Estado = "Disponible";

            _ctx.OrdenesMesa.Remove(orden); // cascada borra detalles
            await _ctx.SaveChangesAsync();

            return Ok(new { ok = true, tipo = "cancelada_sin_pago" });
        }

        // ✅ Si está pagada: solo Admin/Sistema
        if (!(User.IsInRole("Admin") || User.IsInRole("Sistema")))
            return Forbid();

        // ✅ Confirmación obligatoria para evitar accidentes
        if (!string.Equals(confirm, "ANULAR", StringComparison.Ordinal))
            return BadRequest("Para anular una venta pagada usa: DELETE /api/ordenes/{id}/cancelar?confirm=ANULAR");

        await using var tx = await _ctx.Database.BeginTransactionAsync();

        // Devolver stock
        foreach (var d in (orden.Detalles ?? new List<DetalleOrden>()))
        {
            var prod = await _ctx.Productos.FirstOrDefaultAsync(p => p.IdProducto == d.IdProducto);
            if (prod != null)
                prod.Stock += d.Cantidad;
        }

        // Borrar pagos y orden (cascada borra detalles)
        _ctx.Pagos.RemoveRange(pagos);

        if (orden.Mesa != null)
            orden.Mesa.Estado = "Disponible";

        _ctx.OrdenesMesa.Remove(orden);

        await _ctx.SaveChangesAsync();
        await tx.CommitAsync();

        return Ok(new { ok = true, tipo = "anulada_con_devolucion_stock" });
    }

    [HttpPost("{idOrden:int}/cerrar")]
    public async Task<IActionResult> Cerrar(int idOrden)
    {
        var orden = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .FirstOrDefaultAsync(o => o.IdOrden == idOrden);

        if (orden == null) return NotFound("Orden no existe");

        orden.Estado = "Cerrada";
        orden.FechaHoraCierre = UtcNow();

        if (orden.Mesa != null)
            orden.Mesa.Estado = "Disponible";

        await _ctx.SaveChangesAsync();
        return Ok(new { ok = true });
    }
}
