namespace Licoreria.Domain.Entities;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = null!;
    public string Rol { get; set; } = null!; // Admin | Vendedor
    public string PasswordHash { get; set; } = null!;
}
