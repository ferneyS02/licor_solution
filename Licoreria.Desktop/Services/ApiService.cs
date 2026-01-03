using Licoreria.Desktop;                 // ✅ Session
using Licoreria.Desktop.Models;
using System;
using System.Collections.Generic;         // ✅ List<>
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Licoreria.Desktop.Services;

public class ApiService
{
    private readonly HttpClient _http;
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public ApiService()
    {
        _http = new HttpClient
        {
            BaseAddress = new Uri($"{ApiConfig.API.TrimEnd('/')}/"),
            Timeout = TimeSpan.FromSeconds(10)
        };

        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // 🔐 Si ya existe sesión (token), lo reutiliza.
        if (!string.IsNullOrWhiteSpace(Session.Token))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
    }

    // ========= AUTH =========
    private record LoginReq(string Nombre, string Password);
    private record LoginResp(int IdUsuario, string Nombre, string Rol, string Token, DateTime ExpiresAtUtc);

    public async Task<bool> LoginAsync(string nombre, string password)
    {
        var res = await _http.PostAsJsonAsync("auth/login", new LoginReq(nombre, password), _jsonOptions);
        if (!res.IsSuccessStatusCode) return false;

        var data = await res.Content.ReadFromJsonAsync<LoginResp>(_jsonOptions);
        if (data == null || string.IsNullOrWhiteSpace(data.Token)) return false;

        Session.IdUsuario = data.IdUsuario;
        Session.Nombre = data.Nombre;
        Session.Rol = data.Rol;
        Session.Token = data.Token;

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);
        return true;
    }

    // ========= DATA =========
    public Task<List<Categoria>?> GetCategoriasAsync() =>
        _http.GetFromJsonAsync<List<Categoria>>("categorias", _jsonOptions);

    public Task<List<Producto>?> GetProductosPorCategoriaAsync(int idCategoria) =>
        _http.GetFromJsonAsync<List<Producto>>($"productos/{idCategoria}", _jsonOptions);

    public Task<List<Mesa>?> GetMesasAsync() =>
        _http.GetFromJsonAsync<List<Mesa>>("mesas", _jsonOptions);

    public Task<OrdenAbierta?> GetOrdenAbiertaPorMesaAsync(int idMesa) =>
        _http.GetFromJsonAsync<OrdenAbierta?>($"ordenes/abierta/{idMesa}", _jsonOptions);

    public async Task<int?> GetOrdenAbiertaAsync(int idMesa)
    {
        var ord = await GetOrdenAbiertaPorMesaAsync(idMesa);
        return ord?.IdOrden;
    }

    public async Task<OrdenAbierta?> AbrirOrdenAsync(int idMesa)
    {
        var res = await _http.PostAsync($"ordenes/abrir/{idMesa}", null);
        if (!res.IsSuccessStatusCode) return null;

        return await res.Content.ReadFromJsonAsync<OrdenAbierta>(_jsonOptions);
    }

    public async Task<bool> AgregarProductoAsync(int idOrden, int idProducto, int cantidad)
    {
        var res = await _http.PostAsJsonAsync(
            $"ordenes/{idOrden}/agregar",
            new { IdProducto = idProducto, Cantidad = cantidad },
            _jsonOptions
        );

        return res.IsSuccessStatusCode;
    }

    public Task<DetalleOrdenDto?> GetDetalleOrdenAsync(int idOrden) =>
        _http.GetFromJsonAsync<DetalleOrdenDto>($"ordenes/{idOrden}/detalle", _jsonOptions);

    private record PagoResp(decimal MontoBase, decimal Recargo, decimal MontoFinal);

    public async Task<(decimal baseMonto, decimal recargo, decimal final)?> PagarAsync(int idOrden, string tipoPago)
    {
        var res = await _http.PostAsJsonAsync(
            $"ordenes/{idOrden}/pagar",
            new { TipoPago = tipoPago },
            _jsonOptions
        );

        if (!res.IsSuccessStatusCode) return null;

        var data = await res.Content.ReadFromJsonAsync<PagoResp>(_jsonOptions);
        if (data == null) return null;

        return (data.MontoBase, data.Recargo, data.MontoFinal);
    }

    public async Task<bool> CerrarOrdenAsync(int idOrden)
    {
        var res = await _http.PostAsync($"ordenes/{idOrden}/cerrar", null);
        return res.IsSuccessStatusCode;
    }

    // ========= ORDEN: QUITAR / CANCELAR =========
    public async Task<(bool ok, string? error)> QuitarProductoAsync(int idOrden, int idProducto, int cantidad)
    {
        if (cantidad <= 0) return (false, "Cantidad inválida");

        var res = await _http.PostAsJsonAsync(
            $"ordenes/{idOrden}/quitar",
            new { IdProducto = idProducto, Cantidad = cantidad },
            _jsonOptions
        );

        if (res.IsSuccessStatusCode) return (true, null);

        var msg = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error";
        return (false, msg);
    }

    public async Task<(bool ok, string? error)> CancelarOrdenAsync(int idOrden, bool anularPagada = false)
    {
        var url = $"ordenes/{idOrden}/cancelar";
        if (anularPagada) url += "?confirm=ANULAR";

        var res = await _http.DeleteAsync(url);

        if (res.IsSuccessStatusCode) return (true, null);

        var msg = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error";
        return (false, msg);
    }

    // ========= ADMIN (Inventario) =========
    private record ProductoCreateReq(string Nombre, decimal PrecioActual, int Stock, int IdCategoria, string? Imagen);
    private record ProductoUpdateReq(string Nombre, decimal PrecioActual, int Stock, int IdCategoria, string? Imagen);
    private record PrecioReq(decimal PrecioActual);

    public Task<List<ProductoInventario>?> GetInventarioAsync(string? q = null, int? idCategoria = null)
    {
        var url = "admin/productos";
        var first = true;

        if (!string.IsNullOrWhiteSpace(q))
        {
            url += first ? "?" : "&";
            first = false;
            url += "q=" + Uri.EscapeDataString(q.Trim());
        }

        if (idCategoria.HasValue)
        {
            url += first ? "?" : "&";
            first = false;
            url += "idCategoria=" + idCategoria.Value;
        }

        return _http.GetFromJsonAsync<List<ProductoInventario>>(url, _jsonOptions);
    }

    public async Task<ProductoInventario?> CrearProductoAsync(ProductoInventario p)
    {
        var res = await _http.PostAsJsonAsync(
            "admin/productos",
            new ProductoCreateReq(p.Nombre, p.PrecioActual, p.Stock, p.IdCategoria, p.Imagen),
            _jsonOptions
        );

        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ProductoInventario>(_jsonOptions);
    }

    public async Task<ProductoInventario?> ActualizarProductoAsync(ProductoInventario p)
    {
        var res = await _http.PutAsJsonAsync(
            $"admin/productos/{p.IdProducto}",
            new ProductoUpdateReq(p.Nombre, p.PrecioActual, p.Stock, p.IdCategoria, p.Imagen),
            _jsonOptions
        );

        if (!res.IsSuccessStatusCode) return null;
        return await res.Content.ReadFromJsonAsync<ProductoInventario>(_jsonOptions);
    }

    public async Task<bool> CambiarPrecioAsync(int idProducto, decimal precioNuevo)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), $"admin/productos/{idProducto}/precio")
        {
            Content = JsonContent.Create(new PrecioReq(precioNuevo), options: _jsonOptions)
        };

        var res = await _http.SendAsync(req);
        return res.IsSuccessStatusCode;
    }

    public async Task<bool> EliminarProductoAsync(int idProducto)
    {
        var res = await _http.DeleteAsync($"admin/productos/{idProducto}");
        return res.IsSuccessStatusCode;
    }

    // ========= ADMIN: SUBIR IMAGEN =========
    private record UploadResp(string FileName, string Url);

    public async Task<(bool ok, string? fileName, string? url, string? error)> SubirImagenAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return (false, null, null, "Archivo no existe.");

        try
        {
            using var form = new MultipartFormDataContent();

            var originalName = Path.GetFileName(filePath);
            var bytes = await File.ReadAllBytesAsync(filePath);

            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            // IMPORTANTE: el campo se llama "file" (igual que en la API)
            form.Add(fileContent, "file", originalName);

            var res = await _http.PostAsync("admin/productos/imagen", form);

            if (!res.IsSuccessStatusCode)
            {
                var msg = await res.Content.ReadAsStringAsync();
                return (false, null, null, string.IsNullOrWhiteSpace(msg) ? res.ReasonPhrase : msg);
            }

            var data = await res.Content.ReadFromJsonAsync<UploadResp>(_jsonOptions);
            if (data == null || string.IsNullOrWhiteSpace(data.FileName))
                return (false, null, null, "Respuesta inválida del servidor.");

            return (true, data.FileName, data.Url, null);
        }
        catch (Exception ex)
        {
            return (false, null, null, ex.Message);
        }
    }

    // ========= ADMIN: PURGA DE VENTAS =========
    private record PurgeResp(int Deleted);

    public async Task<(bool ok, int deleted, string? error)> PurgarVentasAsync(int years = 5)
    {
        if (years <= 0) return (false, 0, "years debe ser mayor a 0.");

        var res = await _http.DeleteAsync($"admin/purge-ventas?years={years}");

        if (!res.IsSuccessStatusCode)
        {
            var msg = await res.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error desconocido";
            return (false, 0, msg);
        }

        var data = await res.Content.ReadFromJsonAsync<PurgeResp>(_jsonOptions);
        return (true, data?.Deleted ?? 0, null);
    }

    // ========= AUTH: CAMBIAR CONTRASEÑA (Admin/Sistema) =========
    private record ChangePasswordReq(string CurrentPassword, string NewPassword);

    public async Task<(bool ok, string? error)> CambiarPasswordAsync(string currentPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(currentPassword) || string.IsNullOrWhiteSpace(newPassword))
            return (false, "Debes escribir la contraseña actual y la nueva.");

        var res = await _http.PostAsJsonAsync(
            "auth/change-password",
            new ChangePasswordReq(currentPassword, newPassword),
            _jsonOptions
        );

        if (res.IsSuccessStatusCode) return (true, null);

        var msg = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error desconocido";
        return (false, msg);
    }

    // ========= ADMIN: RESET PASSWORD (Admin/Sistema) =========
    private record ResetPasswordReq(int? IdUsuario, string? Nombre, string NewPassword);

    /// <summary>
    /// Resetea la contraseña de un usuario por NOMBRE (Admin/Sistema).
    /// Llama a POST /api/admin/reset-password
    /// Body: { "idUsuario": null, "nombre": "vendedor", "newPassword": "123456" }
    /// </summary>
    public async Task<(bool ok, string? error)> ResetPasswordAsync(string nombreUsuario, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(nombreUsuario))
            return (false, "Debes escribir el nombre del usuario.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
            return (false, "La nueva contraseña debe tener mínimo 6 caracteres.");

        var res = await _http.PostAsJsonAsync(
            "admin/reset-password",
            new ResetPasswordReq(null, nombreUsuario.Trim(), newPassword.Trim()),
            _jsonOptions
        );

        if (res.IsSuccessStatusCode) return (true, null);

        var msg = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error desconocido";
        return (false, msg);
    }

    /// <summary>
    /// Resetea la contraseña de un usuario por ID (Admin/Sistema).
    /// Body: { "idUsuario": 2, "nombre": null, "newPassword": "123456" }
    /// </summary>
    public async Task<(bool ok, string? error)> ResetPasswordByIdAsync(int idUsuario, string newPassword)
    {
        if (idUsuario <= 0)
            return (false, "IdUsuario inválido.");

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
            return (false, "La nueva contraseña debe tener mínimo 6 caracteres.");

        var res = await _http.PostAsJsonAsync(
            "admin/reset-password",
            new ResetPasswordReq(idUsuario, null, newPassword.Trim()),
            _jsonOptions
        );

        if (res.IsSuccessStatusCode) return (true, null);

        var msg = await res.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(msg)) msg = res.ReasonPhrase ?? "Error desconocido";
        return (false, msg);
    }
}
