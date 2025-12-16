using Licoreria.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace Licoreria.Api.Controllers;

[ApiController]
[Route("api/reportes")]
public class ReportesController : ControllerBase
{
    private readonly AppDbContext _ctx;
    public ReportesController(AppDbContext ctx) => _ctx = ctx;

    // GET: /api/reportes/rango/pdf?desde=2025-01-01&hasta=2025-01-31
    [HttpGet("rango/pdf")]
    public async Task<IActionResult> RangoPdf([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        var d0 = desde.Date;
        var d1 = hasta.Date.AddDays(1).AddTicks(-1);

        var ordenes = await _ctx.OrdenesMesa
            .Include(o => o.Mesa)
            .Include(o => o.Detalles)
            .Include(o => o.Pagos)
            .Where(o => o.FechaHoraInicio >= d0 && o.FechaHoraInicio <= d1)
            .OrderBy(o => o.FechaHoraInicio)
            .ToListAsync();

        var pagos = ordenes.SelectMany(o => o.Pagos).ToList();
        var totalEfectivo = pagos.Where(p => p.TipoPago == "Efectivo").Sum(p => p.MontoFinal);
        var totalVirtual = pagos.Where(p => p.TipoPago == "Virtual").Sum(p => p.MontoFinal);
        var totalTarjeta = pagos.Where(p => p.TipoPago == "Tarjeta").Sum(p => p.MontoFinal);

        byte[] pdf = Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Margin(20);
                page.Header().Text($"Licorería 45° - Reporte {d0:yyyy-MM-dd} a {hasta:yyyy-MM-dd}").Bold().FontSize(16);

                page.Content().Column(col =>
                {
                    col.Item().Text($"Total Efectivo: {totalEfectivo:C0}");
                    col.Item().Text($"Total Virtual:  {totalVirtual:C0}");
                    col.Item().Text($"Total Tarjeta:  {totalTarjeta:C0}");
                    col.Item().Text("");

                    foreach (var o in ordenes)
                    {
                        col.Item().Text($"Orden #{o.IdOrden} - {o.Mesa.Nombre} - {o.FechaHoraInicio:g}").Bold();

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

                            foreach (var d in o.Detalles)
                            {
                                t.Cell().Text(d.NombreProducto);
                                t.Cell().Text(d.Cantidad.ToString());
                                t.Cell().Text($"{d.PrecioUnitario:C0}");
                                t.Cell().Text($"{d.Total:C0}");
                            }
                        });

                        var p = o.Pagos.LastOrDefault();
                        if (p != null)
                            col.Item().Text($"Pago: {p.TipoPago} | Base: {p.MontoBase:C0} | Recargo: {p.Recargo:C0} | Final: {p.MontoFinal:C0}");

                        col.Item().Text("--------------------------------------------------");
                    }
                });

                page.Footer().AlignRight().Text($"Generado: {DateTime.Now:g}");
            });
        }).GeneratePdf();

        return File(pdf, "application/pdf", $"reporte_{d0:yyyyMMdd}_{hasta:yyyyMMdd}.pdf");
    }
}
