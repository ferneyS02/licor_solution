using System.Windows;
using System.Windows.Controls;
using Licoreria.Desktop.Services;
using Licoreria.Desktop.Models;

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
        Loaded += OrdenPage_Loaded;
    }

    private async void OrdenPage_Loaded(object sender, RoutedEventArgs e)
    {
        Titulo.Text = $"Orden #{_idOrden} - {_mesa}";
        await RefrescarDetalle();
    }

    private async Task RefrescarDetalle()
    {
        var dto = await _api.GetDetalleOrdenAsync(_idOrden);
        if (dto == null)
        {
            Lista.ItemsSource = null;
            TxtTotal.Text = "$0";
            return;
        }

        Lista.ItemsSource = dto.Lineas;
        TxtTotal.Text = $"${dto.Total:N0}";
    }

    private async void Agregar_Click(object sender, RoutedEventArgs e)
    {
        var win = new ProductosWindow(_idOrden);
        win.Owner = Window.GetWindow(this);
        win.ShowDialog();

        await RefrescarDetalle();
    }

    private async void Pagar_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var item = CbPago.SelectedItem as ComboBoxItem;
            var tipo = item?.Content?.ToString() ?? "Efectivo";

            var (baseMonto, recargo, final) = await _api.PagarAsync(_idOrden, tipo);

            MessageBox.Show(
                $"Pago: {tipo}\nBase: ${baseMonto:N0}\nRecargo: ${recargo:N0}\nFinal: ${final:N0}",
                "Pago registrado",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            await RefrescarDetalle();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error pagando", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Cerrar_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("¿Cerrar mesa?", "Confirmar", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
            return;

        var ok = await _api.CerrarOrdenAsync(_idOrden);
        if (ok)
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
