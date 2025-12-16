namespace Licoreria.Domain.Entities;

public class Pago
{
    public int IdPago { get; set; }

    public int IdOrden { get; set; }
    public OrdenMesa Orden { get; set; } = null!;

    public decimal MontoBase { get; set; }
    public string TipoPago { get; set; } = null!; // Efectivo | Virtual | Tarjeta
    public decimal Recargo { get; set; }          // Tarjeta: 5% + 300
    public decimal MontoFinal { get; set; }

    public DateTime FechaHora { get; set; } = DateTime.Now;
}
