using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Licoreria.Domain.Entities;

public class OrdenMesa
{
    [Key]
    public int IdOrden { get; set; }

    // FK Mesa
    public int IdMesa { get; set; }
    [ForeignKey(nameof(IdMesa))]
    public Mesa Mesa { get; set; } = null!;

    // FK Jornada
    public int IdJornada { get; set; }
    [ForeignKey(nameof(IdJornada))]
    public Jornada Jornada { get; set; } = null!;

    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraCierre { get; set; }

    // FK Usuario
    public int IdUsuario { get; set; }
    [ForeignKey(nameof(IdUsuario))]
    public Usuario Usuario { get; set; } = null!;

    [MaxLength(20)]
    public string Estado { get; set; } = "Abierta"; // Abierta/Cerrada

    [MaxLength(20)]
    public string TipoPago { get; set; } = "Normal"; // Normal/Prepago

    public ICollection<DetalleOrden> Detalles { get; set; } = new List<DetalleOrden>();
    public ICollection<Pago> Pagos { get; set; } = new List<Pago>();
}
