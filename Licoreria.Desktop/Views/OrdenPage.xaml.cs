using Licoreria.Desktop;              // Session
using Licoreria.Desktop.Models;        // LineaOrden
using Licoreria.Desktop.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Licoreria.Desktop.Views;

public partial class OrdenPage : Page
{
    private readonly ApiService _api = new();
    private readonly int _idOrden;
    private readonly string _mesa;

    public OrdenPage(int idOrden, string mesa)
    {
        InitializeComponent();
        _idOrden = idOrden;
        _mesa = mesa;
        Titulo.Text = $"Orden #{_idOrden} - {_mesa}";
        Loaded += async (_, __) => await RefrescarAsync();
    }

    private async Task RefrescarAsync()
    {
        var det = await _api.GetDetalleOrdenAsync(_idOrden);

        Lista.ItemsSource = det?.Lineas ?? new List<LineaOrden>();
        TxtTotal.Text = det != null ? $"${det.Total:N0}" : "$0";

        BtnQuitar1.IsEnabled = Lista.SelectedItem != null;
        BtnEliminarLinea.IsEnabled = Lista.SelectedItem != null;
    }

    private void Lista_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnQuitar1.IsEnabled = Lista.SelectedItem != null;
        BtnEliminarLinea.IsEnabled = Lista.SelectedItem != null;
    }

    private async void Agregar_Click(object sender, RoutedEventArgs e)
    {
        var win = new ProductosWindow(_idOrden);
        win.Owner = Window.GetWindow(this);
        win.ShowDialog();
        await RefrescarAsync();
    }

    private async void Quitar1_Click(object sender, RoutedEventArgs e)
    {
        if (Lista.SelectedItem is not LineaOrden linea)
        {
            MessageBox.Show("Selecciona un producto.");
            return;
        }

        var (ok, error) = await _api.QuitarProductoAsync(_idOrden, linea.IdProducto, 1);
        if (!ok)
        {
            MessageBox.Show(error ?? "No se pudo quitar.", "Quitar");
            return;
        }

        await RefrescarAsync();
    }

    private async void EliminarLinea_Click(object sender, RoutedEventArgs e)
    {
        if (Lista.SelectedItem is not LineaOrden linea)
        {
            MessageBox.Show("Selecciona un producto.");
            return;
        }

        var confirm = MessageBox.Show(
            $"¿Eliminar completamente '{linea.NombreProducto}' de la orden?",
            "Confirmar",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        var (ok, error) = await _api.QuitarProductoAsync(_idOrden, linea.IdProducto, linea.Cantidad);
        if (!ok)
        {
            MessageBox.Show(error ?? "No se pudo eliminar la línea.", "Eliminar línea");
            return;
        }

        await RefrescarAsync();
    }

    private async void CancelarOrden_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "¿Cancelar esta orden?\n\n- Si NO está pagada: se borra y se libera la mesa.\n- Si YA está pagada: solo Admin/Sistema puede ANULAR (devuelve stock).",
            "Confirmar cancelación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        // 1) Intento normal (sirve para NO pagadas)
        var (ok, error) = await _api.CancelarOrdenAsync(_idOrden, anularPagada: false);

        // 2) Si la API pide confirm=ANULAR, significa que ya está pagada
        if (!ok && (error?.Contains("confirm=ANULAR", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            var esAdmin = Session.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
                          Session.Rol.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

            if (!esAdmin)
            {
                MessageBox.Show("Esta orden ya está pagada. Solo Admin/Sistema puede anularla.", "No permitido");
                return;
            }

            var confirm2 = MessageBox.Show(
                "La orden YA está pagada.\n\n¿Deseas ANULAR la venta?\nEsto devolverá stock y eliminará el pago.",
                "ANULAR venta pagada",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm2 != MessageBoxResult.Yes) return;

            (ok, error) = await _api.CancelarOrdenAsync(_idOrden, anularPagada: true);
        }

        if (!ok)
        {
            MessageBox.Show(error ?? "No se pudo cancelar/anular.", "Cancelar orden");
            return;
        }

        MessageBox.Show("Orden cancelada/anulada correctamente.");
        NavigationService?.GoBack();
    }

    private async void Pagar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var item = CbPago.SelectedItem as ComboBoxItem;
            var tipo = item?.Content?.ToString() ?? "Efectivo";

            var pago = await _api.PagarAsync(_idOrden, tipo);
            if (pago == null)
            {
                MessageBox.Show("No se pudo pagar. Verifica que la orden tenga productos y stock.");
                return;
            }

            MessageBox.Show($"Pago {tipo}\nBase: {pago.Value.baseMonto:C0}\nRecargo: {pago.Value.recargo:C0}\nFinal: {pago.Value.final:C0}");
            await RefrescarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error pagando: " + ex.Message);
        }
    }

    private async void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        if (await _api.CerrarOrdenAsync(_idOrden))
        {
            MessageBox.Show("Mesa cerrada");
            NavigationService?.GoBack();
        }
        else
        {
            MessageBox.Show("No se pudo cerrar");
        }
    }
}
