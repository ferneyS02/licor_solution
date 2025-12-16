namespace Licoreria.Domain.Entities;

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PrecioActual { get; set; }
    public int Stock { get; set; }
    public string? Imagen { get; set; } // "aguila.png"
    public int IdCategoria { get; set; }
    public Categoria Categoria { get; set; } = null!;
}
