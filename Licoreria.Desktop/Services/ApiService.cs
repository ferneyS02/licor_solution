using System.Net.Http;
using System.Net.Http.Json;
using Licoreria.Desktop.Models;

namespace Licoreria.Desktop.Services;

public class ApiService
{
    private readonly HttpClient _http;

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri(ApiConfig.API) };
    }

    public Task<List<Categoria>?> GetCategoriasAsync()
        => _http.GetFromJsonAsync<List<Categoria>>("categorias");

    public Task<List<Producto>?> GetProductosPorCategoriaAsync(int idCategoria)
        => _http.GetFromJsonAsync<List<Producto>>($"productos/{idCategoria}");

    public Task<List<Mesa>?> GetMesasAsync()
        => _http.GetFromJsonAsync<List<Mesa>>("mesas");

    public Task<OrdenAbierta?> GetOrdenAbiertaPorMesaAsync(int idMesa)
        => _http.GetFromJsonAsync<OrdenAbierta?>($"ordenes/abierta/mesa/{idMesa}");

    public async Task<OrdenAbierta?> AbrirOrdenAsync(int idMesa, int idUsuario = 2)
    {
        var res = await _http.PostAsync($"ordenes/abrir/{idMesa}?idUsuario={idUsuario}", null);
        res.EnsureSuccessStatusCode();
        return await res.Content.ReadFromJsonAsync<OrdenAbierta>();
    }

    public async Task<bool> AgregarProductoAsync(int idOrden, int idProducto, int cantidad)
    {
        var body = new { IdProducto = idProducto, Cantidad = cantidad };
        var res = await _http.PostAsJsonAsync($"ordenes/{idOrden}/agregar", body);
        return res.IsSuccessStatusCode;
    }

    public Task<DetalleOrdenDto?> GetDetalleOrdenAsync(int idOrden)
        => _http.GetFromJsonAsync<DetalleOrdenDto>($"ordenes/{idOrden}/detalle");

    public async Task<(decimal baseMonto, decimal recargo, decimal final)> PagarAsync(int idOrden, string tipoPago)
    {
        var res = await _http.PostAsJsonAsync($"ordenes/{idOrden}/pagar", new { TipoPago = tipoPago });
        res.EnsureSuccessStatusCode();
        var data = await res.Content.ReadFromJsonAsync<dynamic>();
        decimal mb = (decimal)data!.MontoBase;
        decimal rc = (decimal)data!.Recargo;
        decimal mf = (decimal)data!.MontoFinal;
        return (mb, rc, mf);
    }

    public async Task<bool> CerrarOrdenAsync(int idOrden)
    {
        var res = await _http.PostAsync($"ordenes/{idOrden}/cerrar", null);
        return res.IsSuccessStatusCode;
    }
}
