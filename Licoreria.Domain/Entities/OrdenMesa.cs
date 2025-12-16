namespace Licoreria.Domain.Entities;

public class OrdenMesa
{
    public int IdOrden { get; set; }
    public int IdMesa { get; set; }
    public Mesa Mesa { get; set; } = null!;

    public int IdJornada { get; set; }
    public Jornada Jornada { get; set; } = null!;

    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraCierre { get; set; }

    public int Usuario { get; set; }
    public string Estado { get; set; } = "Abierta";   // Abierta | Cerrada
    public string TipoOrden { get; set; } = "Mesa";   // Mesa | Prepago

    public ICollection<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
