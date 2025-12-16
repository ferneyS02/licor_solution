namespace Licoreria.Domain.Entities;

public class DetalleOrden
{
    public int IdDetalle { get; set; }

    public int IdOrden { get; set; }
    public OrdenMesa Orden { get; set; } = null!;

    public int IdProducto { get; set; }
    public Producto Producto { get; set; } = null!;

    public string NombreProducto { get; set; } = null!; // snapshot
    public decimal PrecioUnitario { get; set; }         // snapshot
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}
