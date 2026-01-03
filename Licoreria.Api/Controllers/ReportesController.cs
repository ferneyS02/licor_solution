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
    // También acepta: desde=2025-12-18&hasta=2025-12-18
    [HttpGet("rango/pdf")]
    public async Task<IActionResult> RangoPdf([FromQuery] string desde, [FromQuery] string hasta)
    {
        // ✅ No hace daño dejarlo aquí (aunque ya esté en Program.cs)
        QuestPDF.Settings.License = LicenseType.Community;

        if (!TryParseFecha(desde, out var desdeDt))
            return BadRequest("Fecha 'desde' inválida. Usa yyyy-MM-dd o dd/MM/yyyy. Ej: 2025-12-18 o 18/12/2025");

        if (!TryParseFecha(hasta, out var hastaDt))
            return BadRequest("Fecha 'hasta' inválida. Usa yyyy-MM-dd o dd/MM/yyyy. Ej: 2025-12-18 o 18/12/2025");

        if (hastaDt.Date < desdeDt.Date)
            return BadRequest("Rango inválido (hasta < desde).");

        var desdeFecha = desdeDt.Date;
        var hastaFecha = hastaDt.Date;

        // Rango "día local" -> UTC para Postgres timestamptz
        var desdeLocal = DateTime.SpecifyKind(desdeFecha, DateTimeKind.Local);
        var hastaLocal = DateTime.SpecifyKind(hastaFecha.AddDays(1).AddTicks(-1), DateTimeKind.Local);

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

        // (opcional) cultura CO para moneda más “bonita” en PDF
        var co = CultureInfo.GetCultureInfo("es-CO");

        byte[] pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(20);

                page.Header()
                    .Text($"Licorería 45° - Reporte {desdeFecha:yyyy-MM-dd} a {hastaFecha:yyyy-MM-dd}")
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
                            col.Item().Text(
                                $"Pago: {p.TipoPago} | Base: {p.MontoBase.ToString("C0", co)} | Recargo: {p.Recargo.ToString("C0", co)} | Final: {p.MontoFinal.ToString("C0", co)}"
                            );
                        }

                        col.Item().Text("--------------------------------------------------");
                    }
                });

                page.Footer().AlignRight().Text($"Generado: {DateTime.Now:g}");
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_{desdeFecha:yyyyMMdd}_{hastaFecha:yyyyMMdd}.pdf");
    }

    private static bool TryParseFecha(string input, out DateTime dt)
    {
        dt = default;
        if (string.IsNullOrWhiteSpace(input)) return false;

        input = input.Trim();

        // 1) yyyy-MM-dd
        if (DateTime.TryParseExact(input, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out dt))
            return true;

        // 2) dd/MM/yyyy
        if (DateTime.TryParseExact(input, "dd/MM/yyyy", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out dt))
            return true;

        // 3) ISO date-time (2025-12-18T00:00:00 o con Z)
        if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out var dto))
        {
            dt = dto.DateTime;
            return true;
        }

        return false;
    }
}
