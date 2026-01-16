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

    private bool _pagada;

    public OrdenPage(int idOrden, string mesa)
    {
        InitializeComponent();
        _idOrden = idOrden;
        _mesa = mesa;

        Loaded += async (_, __) => await RefrescarAsync();
    }

    private async Task RefrescarAsync()
    {
        var det = await _api.GetDetalleOrdenAsync(_idOrden);

        var lineas = det?.Lineas ?? new List<LineaOrden>();
        var total = det?.Total ?? 0m;

        _pagada = det?.Pagada == true;
        var tieneProductos = lineas.Count > 0;

        // Título con estado
        Titulo.Text = $"Orden #{_idOrden} - {_mesa}" + (_pagada ? " (PAGADA)" : "");

        Lista.ItemsSource = lineas;
        TxtTotal.Text = det != null ? $"${total:N0}" : "$0";

        // Botones de línea (solo si NO está pagada)
        BtnQuitar1.IsEnabled = !_pagada && Lista.SelectedItem != null;
        BtnEliminarLinea.IsEnabled = !_pagada && Lista.SelectedItem != null;

        // Agregar producto (si está pagada, no deja)
        BtnAgregarProducto.IsEnabled = !_pagada;

        // Pago: solo si hay productos y NO está pagada
        BtnPagar.IsEnabled = tieneProductos && !_pagada;
        CbPago.IsEnabled = tieneProductos && !_pagada;

        // ✅ REGLA: cerrar solo si NO hay productos o si ya está pagada
        BtnCerrarMesa.IsEnabled = !tieneProductos || _pagada;
    }

    private void Lista_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        BtnQuitar1.IsEnabled = !_pagada && Lista.SelectedItem != null;
        BtnEliminarLinea.IsEnabled = !_pagada && Lista.SelectedItem != null;
    }

    private async void Agregar_Click(object sender, RoutedEventArgs e)
    {
        if (_pagada)
        {
            MessageBox.Show("La orden ya está pagada. No se puede modificar.");
            return;
        }

        var win = new ProductosWindow(_idOrden);
        win.Owner = Window.GetWindow(this);
        win.ShowDialog();
        await RefrescarAsync();
    }

    private async void Quitar1_Click(object sender, RoutedEventArgs e)
    {
        if (_pagada)
        {
            MessageBox.Show("La orden ya está pagada. No se puede modificar.");
            return;
        }

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
        if (_pagada)
        {
            MessageBox.Show("La orden ya está pagada. No se puede modificar.");
            return;
        }

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
        var (ok, error) = await _api.CerrarOrdenAsync(_idOrden);

        if (ok)
        {
            MessageBox.Show("Mesa cerrada");
            NavigationService?.GoBack();
        }
        else
        {
            MessageBox.Show(error ?? "No se pudo cerrar.", "Cerrar mesa");
        }
    }
}
