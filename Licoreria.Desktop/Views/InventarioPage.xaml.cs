using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Licoreria.Desktop.Models;
using Licoreria.Desktop.Services;
using Microsoft.Win32;

namespace Licoreria.Desktop.Views;

public partial class InventarioPage : Page
{
    private readonly ApiService _api = new();
    private ObservableCollection<ProductoInventario> _items = new();

    // 🔎 Auto-búsqueda con debounce
    private readonly DispatcherTimer _searchTimer = new();
    private bool _initDone = false;

    public InventarioPage()
    {
        InitializeComponent();

        _searchTimer.Interval = TimeSpan.FromMilliseconds(400);
        _searchTimer.Tick += async (_, __) =>
        {
            _searchTimer.Stop();
            if (_initDone)
                await LoadProductosAsync();
        };

        Loaded += async (_, __) => await InitAsync();
    }

    private bool RolPermitido =>
        Session.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        Session.Rol.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

    private async Task InitAsync()
    {
        if (!RolPermitido)
        {
            MessageBox.Show("No tienes permisos para Inventario.\n\nSolo Admin o Sistema.", "Acceso denegado");
            NavigationService?.GoBack();
            return;
        }

        try
        {
            var cats = await _api.GetCategoriasAsync() ?? new();

            CboCategoria.ItemsSource = cats;
            CboCategoriaEdit.ItemsSource = cats;

            if (cats.Count > 0)
            {
                CboCategoria.SelectedIndex = 0;
                CboCategoriaEdit.SelectedIndex = 0;
            }

            GridProductos.ItemsSource = _items;

            await LoadProductosAsync();
            NuevoFormulario();

            _initDone = true; // ✅ ya puede disparar búsquedas automáticas
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error Inventario");
        }
    }

    private async Task LoadProductosAsync()
    {
        TxtEstado.Text = "";

        var q = string.IsNullOrWhiteSpace(TxtBuscar.Text) ? null : TxtBuscar.Text.Trim();
        var idCat = CboCategoria.SelectedValue is int v ? v : (int?)null;

        var data = await _api.GetInventarioAsync(q, idCat) ?? new();

        _items = new ObservableCollection<ProductoInventario>(data);
        GridProductos.ItemsSource = _items;
    }

    private void NuevoFormulario()
    {
        TxtId.Text = "";
        TxtNombre.Text = "";
        TxtPrecio.Text = "";
        TxtStock.Text = "0";
        TxtImagen.Text = "";

        if (CboCategoria.SelectedValue is int idCat)
            CboCategoriaEdit.SelectedValue = idCat;

        GridProductos.SelectedItem = null;
        ActualizarPreview();
    }

    private ProductoInventario? LeerFormulario(out string error)
    {
        error = "";

        var nombre = (TxtNombre.Text ?? "").Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            error = "Nombre es obligatorio.";
            return null;
        }

        if (CboCategoriaEdit.SelectedValue is not int idCategoria)
        {
            error = "Selecciona una categoría.";
            return null;
        }

        if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio))
        {
            if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio))
            {
                error = "Precio inválido.";
                return null;
            }
        }

        if (!int.TryParse(TxtStock.Text, out var stock) || stock < 0)
        {
            error = "Stock inválido.";
            return null;
        }

        var imagen = (TxtImagen.Text ?? "").Trim();
        if (imagen.Length == 0) imagen = null;

        var id = 0;
        int.TryParse(TxtId.Text, out id);

        return new ProductoInventario
        {
            IdProducto = id,
            Nombre = nombre,
            PrecioActual = precio,
            Stock = stock,
            IdCategoria = idCategoria,
            Imagen = imagen
        };
    }

    // ===== Preview =====
    private void ActualizarPreview()
    {
        try
        {
            var fileName = (TxtImagen.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(fileName))
            {
                ImgPreview.Source = null;
                return;
            }

            var url = ApiConfig.Img(fileName);

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(url);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bmp.EndInit();

            ImgPreview.Source = bmp;
        }
        catch
        {
            ImgPreview.Source = null;
        }
    }

    // ===== EVENTS =====
    private async void Categoria_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded)
        {
            _searchTimer.Stop(); // ✅ evita que dispare una búsqueda pendiente

            if (CboCategoria.SelectedValue is int idCat)
                CboCategoriaEdit.SelectedValue = idCat;

            await LoadProductosAsync();
        }
    }

    private async void Refrescar_Click(object sender, RoutedEventArgs e)
    {
        _searchTimer.Stop();
        await LoadProductosAsync();
        TxtEstado.Text = "Lista actualizada.";
    }

    private void Nuevo_Click(object sender, RoutedEventArgs e)
    {
        NuevoFormulario();
        TxtEstado.Text = "Formulario listo para nuevo producto.";
    }

    private void GridProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GridProductos.SelectedItem is not ProductoInventario p) return;

        TxtId.Text = p.IdProducto.ToString();
        TxtNombre.Text = p.Nombre;
        TxtPrecio.Text = p.PrecioActual.ToString(CultureInfo.InvariantCulture);
        TxtStock.Text = p.Stock.ToString();
        TxtImagen.Text = p.Imagen ?? "";
        CboCategoriaEdit.SelectedValue = p.IdCategoria;

        ActualizarPreview();
    }

    private async void GuardarNuevo_Click(object sender, RoutedEventArgs e)
    {
        TxtEstado.Text = "";

        var p = LeerFormulario(out var err);
        if (p == null)
        {
            TxtEstado.Text = err;
            return;
        }

        try
        {
            var creado = await _api.CrearProductoAsync(p);
            if (creado == null)
            {
                TxtEstado.Text = "No se pudo crear. Revisa permisos/datos.";
                return;
            }

            TxtEstado.Text = $"Producto creado (ID {creado.IdProducto}).";
            await LoadProductosAsync();

            var match = _items.FirstOrDefault(x => x.IdProducto == creado.IdProducto);
            if (match != null)
                GridProductos.SelectedItem = match;
        }
        catch (Exception ex)
        {
            TxtEstado.Text = ex.Message;
        }
    }

    private async void Actualizar_Click(object sender, RoutedEventArgs e)
    {
        TxtEstado.Text = "";

        var p = LeerFormulario(out var err);
        if (p == null)
        {
            TxtEstado.Text = err;
            return;
        }

        if (p.IdProducto <= 0)
        {
            TxtEstado.Text = "Selecciona un producto para actualizar.";
            return;
        }

        try
        {
            var upd = await _api.ActualizarProductoAsync(p);
            if (upd == null)
            {
                TxtEstado.Text = "No se pudo actualizar. Revisa permisos/datos.";
                return;
            }

            TxtEstado.Text = "Producto actualizado.";
            await LoadProductosAsync();

            var match = _items.FirstOrDefault(x => x.IdProducto == p.IdProducto);
            if (match != null)
                GridProductos.SelectedItem = match;
        }
        catch (Exception ex)
        {
            TxtEstado.Text = ex.Message;
        }
    }

    private async void SoloPrecio_Click(object sender, RoutedEventArgs e)
    {
        TxtEstado.Text = "";

        if (!int.TryParse(TxtId.Text, out var id) || id <= 0)
        {
            TxtEstado.Text = "Selecciona un producto.";
            return;
        }

        if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var precio))
        {
            if (!decimal.TryParse(TxtPrecio.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out precio))
            {
                TxtEstado.Text = "Precio inválido.";
                return;
            }
        }

        try
        {
            var ok = await _api.CambiarPrecioAsync(id, precio);
            TxtEstado.Text = ok ? "Precio actualizado." : "No se pudo actualizar el precio.";

            if (ok)
                await LoadProductosAsync();
        }
        catch (Exception ex)
        {
            TxtEstado.Text = ex.Message;
        }
    }

    private async void Eliminar_Click(object sender, RoutedEventArgs e)
    {
        TxtEstado.Text = "";

        if (!int.TryParse(TxtId.Text, out var id) || id <= 0)
        {
            TxtEstado.Text = "Selecciona un producto.";
            return;
        }

        var confirmar = MessageBox.Show(
            $"¿Eliminar el producto ID {id}?\n\n" +
            "Si ya fue vendido, la API no permitirá eliminarlo (protege el histórico).",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmar != MessageBoxResult.Yes) return;

        try
        {
            var ok = await _api.EliminarProductoAsync(id);
            TxtEstado.Text = ok
                ? "Producto eliminado."
                : "No se pudo eliminar (posiblemente ya se vendió).";

            if (ok)
            {
                await LoadProductosAsync();
                NuevoFormulario();
            }
        }
        catch (Exception ex)
        {
            TxtEstado.Text = ex.Message;
        }
    }

    // ✅ Enter = buscar inmediato
    private async void Buscar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _searchTimer.Stop();
            await LoadProductosAsync();
        }
    }

    // ✅ Escribiendo = buscar automático (debounce 400ms)
    private void Buscar_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_initDone) return;

        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void TxtImagen_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (IsLoaded) ActualizarPreview();
    }

    private async void CargarImagen_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Selecciona una imagen",
            Filter = "Imágenes (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp"
        };

        if (dlg.ShowDialog() != true) return;

        TxtEstado.Text = "Subiendo imagen...";

        var (ok, fileName, _, error) = await _api.SubirImagenAsync(dlg.FileName);
        if (!ok)
        {
            TxtEstado.Text = $"Error subiendo imagen: {error}";
            return;
        }

        TxtImagen.Text = fileName!;
        TxtEstado.Text = $"Imagen subida OK: {fileName}";
        ActualizarPreview();
    }
}
