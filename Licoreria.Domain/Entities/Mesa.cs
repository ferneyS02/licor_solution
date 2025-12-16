namespace Licoreria.Domain.Entities;

public class Mesa
{
    public int IdMesa { get; set; }
    public string Nombre { get; set; } = null!; // Mesa1..Mesa8
    public string Estado { get; set; } = "Disponible"; // Disponible | Ocupada
}
