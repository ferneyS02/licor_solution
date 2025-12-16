namespace Licoreria.Domain.Entities;

public class Jornada
{
    public int IdJornada { get; set; }
    public DateTime FechaJornada { get; set; }      // visible para reportes
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int UsuarioInicio { get; set; }
    public int? UsuarioCierre { get; set; }
    public string Estado { get; set; } = "Abierta"; // Abierta | Cerrada
}
