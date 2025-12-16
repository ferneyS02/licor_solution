using System.Windows;
using System.Windows.Controls;
using Licoreria.Desktop.Services;
using Licoreria.Desktop.Models;

namespace Licoreria.Desktop.Views;

public partial class InventarioPage : Page
{
    private readonly ApiService _api = new();

    public InventarioPage()
    {
        InitializeComponent();
        Loaded += (_, __) => Load();
    }

    private async void Load()
    {
        var items = await _api.GetInventarioAsync() ?? new();
        Grid.ItemsSource = items;
    }

    private async void GuardarStock_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not ProductoInventario p) return;
        var ok = await _api.SetStockAsync(p.IdProducto, p.Stock);
        MessageBox.Show(ok ? "Stock actualizado" : "Error stock");
    }

    private async void GuardarPrecio_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.DataContext is not ProductoInventario p) return;
        var ok = await _api.SetPrecioAsync(p.IdProducto, p.PrecioActual);
        MessageBox.Show(ok ? "Precio actualizado" : "Error precio");
    }
}
