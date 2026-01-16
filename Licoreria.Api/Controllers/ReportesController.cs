using System.Globalization;
using Licoreria.Domain.Entities;
using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Licoreria.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Sistema")]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public ReportesController(AppDbContext ctx) => _ctx = ctx;

    // GET: /api/reportes/rango/pdf?desde=18/12/2025&hasta=18/12/2025
    // También acepta:
    //  - desde=2025-12-18&hasta=2025-12-18
    //  - desde=2025-12-18T09:30&hasta=2025-12-18T18:15
    //  - desde=18/12/2025 09:30&hasta=18/12/2025 18:15
    // Si NO se envía hora, se asume el día completo.
    [HttpGet("rango/pdf")]
    public async Task<IActionResult> RangoPdf([FromQuery] string desde, [FromQuery] string hasta)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        if (!TryParseFechaHora(desde, out var desdeDt, out var desdeTieneHora))
            return BadRequest("'desde' inválido. Usa yyyy-MM-dd, dd/MM/yyyy, o fecha+hora (yyyy-MM-ddTHH:mm / dd/MM/yyyy HH:mm). Ej: 2025-12-18T09:30");

        if (!TryParseFechaHora(hasta, out var hastaDt, out var hastaTieneHora))
            return BadRequest("'hasta' inválido. Usa yyyy-MM-dd, dd/MM/yyyy, o fecha+hora (yyyy-MM-ddTHH:mm / dd/MM/yyyy HH:mm). Ej: 2025-12-18T18:15");

        var desdeLocal = DateTime.SpecifyKind(desdeTieneHora ? desdeDt : desdeDt.Date, DateTimeKind.Local);
        var hastaLocal = DateTime.SpecifyKind(
            hastaTieneHora ? hastaDt : hastaDt.Date.AddDays(1).AddTicks(-1),
            DateTimeKind.Local);

        if (hastaLocal < desdeLocal)
            return BadRequest("Rango inválido (hasta < desde).");

        var d0 = desdeLocal.ToUniversalTime();
        var d1 = hastaLocal.ToUniversalTime();

        var ordenes = await _ctx.OrdenesMesa
            .AsNoTracking()
            .AsSplitQuery()
            .Include(o => o.Mesa)
            .Include(o => o.Detalles)
            .Include(o => o.Pagos)
            .Where(o => o.FechaHoraInicio >= d0 && o.FechaHoraInicio <= d1)
            .OrderBy(o => o.FechaHoraInicio)
            .ToListAsync();

        var pagos = ordenes.SelectMany(o => o.Pagos ?? Enumerable.Empty<Pago>()).ToList();

        var totalEfectivo = pagos.Where(p => p.TipoPago == "Efectivo").Sum(p => p.MontoFinal);
        var totalVirtual = pagos.Where(p => p.TipoPago == "Virtual").Sum(p => p.MontoFinal);
        var totalTarjeta = pagos.Where(p => p.TipoPago == "Tarjeta").Sum(p => p.MontoFinal);

        static DateTime ToLocal(DateTime dt) =>
            dt.Kind == DateTimeKind.Utc ? dt.ToLocalTime() : dt;

        var co = CultureInfo.GetCultureInfo("es-CO");

        byte[] pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(20);

                page.Header()
                    .Text($"Licorería 45° - Reporte {desdeLocal:yyyy-MM-dd HH:mm} a {hastaLocal:yyyy-MM-dd HH:mm}")
                    .Bold().FontSize(16);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Total Efectivo: {totalEfectivo.ToString("C0", co)}");
                    col.Item().Text($"Total Virtual:  {totalVirtual.ToString("C0", co)}");
                    col.Item().Text($"Total Tarjeta:  {totalTarjeta.ToString("C0", co)}");
                    col.Item().Text("");

                    foreach (var o in ordenes)
                    {
                        var mesaNombre = o.Mesa?.Nombre ?? "(sin mesa)";
                        var fechaOrden = ToLocal(o.FechaHoraInicio);

                        col.Item().Text($"Orden #{o.IdOrden} - {mesaNombre} - {fechaOrden:g}").Bold();

                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.ConstantColumn(50);
                                c.ConstantColumn(80);
                                c.ConstantColumn(80);
                            });

                            t.Header(h =>
                            {
                                h.Cell().Text("Producto").Bold();
                                h.Cell().Text("Cant").Bold();
                                h.Cell().Text("P.Unit").Bold();
                                h.Cell().Text("Total").Bold();
                            });

                            foreach (var d in (o.Detalles ?? Enumerable.Empty<DetalleOrden>()))
                            {
                                t.Cell().Text(d.NombreProducto);
                                t.Cell().Text(d.Cantidad.ToString());
                                t.Cell().Text(d.PrecioUnitario.ToString("C0", co));
                                t.Cell().Text(d.Total.ToString("C0", co));
                            }
                        });

                        var p = (o.Pagos ?? Enumerable.Empty<Pago>())
                            .OrderBy(x => x.FechaHora)
                            .LastOrDefault();

                        if (p != null)
                        {
                            var baseStr = p.MontoBase.ToString("C0", co);
                            var recargoStr = p.Recargo.ToString("C0", co);
                            var finalStr = p.MontoFinal.ToString("C0", co);

                            col.Item().Text($"Pago: {p.TipoPago} | Base: {baseStr} | Recargo: {recargoStr} | Final: {finalStr}");
                        }

                        col.Item().Text("--------------------------------------------------");
                    }
                });

                page.Footer().AlignRight().Text($"Generado: {DateTime.Now:g}");
            });
        }).GeneratePdf();

        var fileName = (desdeTieneHora || hastaTieneHora)
            ? $"reporte_{desdeLocal:yyyyMMdd_HHmm}_{hastaLocal:yyyyMMdd_HHmm}.pdf"
            : $"reporte_{desdeLocal:yyyyMMdd}_{hastaLocal:yyyyMMdd}.pdf";

        return File(pdf, "application/pdf", fileName);
    }

    private static bool TryParseFechaHora(string input, out DateTime dt, out bool tieneHora)
    {
        dt = default;
        tieneHora = false;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();

        var formatosConHora = new[]
        {
            "yyyy-MM-dd'T'HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd'T'HH:mm:ss",
            "yyyy-MM-dd'T'HH:mm",
            "yyyy-MM-dd HH:mm:ss.FFFFFFF",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "dd/MM/yyyy HH:mm:ss.FFFFFFF",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
        };

        if (DateTime.TryParseExact(input, formatosConHora, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt))
        {
            tieneHora = true;
            return true;
        }

        var formatosSoloFecha = new[]
        {
            "yyyy-MM-dd",
            "dd/MM/yyyy",
        };

        if (DateTime.TryParseExact(input, formatosSoloFecha, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt))
        {
            tieneHora = false;
            return true;
        }

        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out var dto))
        {
            dt = dto.LocalDateTime;
            tieneHora = input.Contains(':') || input.Contains('T') || input.Contains('t');
            return true;
        }

        return false;
    }
}
