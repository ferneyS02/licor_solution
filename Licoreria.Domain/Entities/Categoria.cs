namespace Licoreria.Domain.Entities;

public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = null!;
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
