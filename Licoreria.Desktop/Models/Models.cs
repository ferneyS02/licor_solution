namespace Licoreria.Desktop.Models;

public class Categoria
{
    public int IdCategoria { get; set; }
    public string Nombre { get; set; } = "";
}

public class Producto
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = "";
    public decimal PrecioActual { get; set; }
    public string? Imagen { get; set; }
}

public class Mesa
{
    public int IdMesa { get; set; }
    public string Nombre { get; set; } = "";
    public string Estado { get; set; } = "";
}

public class OrdenAbierta
{
    public int IdOrden { get; set; }
    public string Mesa { get; set; } = "";
}

public class LineaOrden
{
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = "";
    public decimal PrecioUnitario { get; set; }
    public int Cantidad { get; set; }
    public decimal Total { get; set; }
}

public class DetalleOrdenDto
{
    public int IdOrden { get; set; }
    public string Mesa { get; set; } = "";
    public string Estado { get; set; } = "";
    public List<LineaOrden> Lineas { get; set; } = new();
    public decimal Total { get; set; }
}
