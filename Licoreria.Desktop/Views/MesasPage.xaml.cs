using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Licoreria.Desktop;
using Licoreria.Desktop.Services;
using Licoreria.Desktop.Models;

namespace Licoreria.Desktop.Views;

public partial class MesasPage : Page
{
    private readonly ApiService _api = new();

    public MesasPage()
    {
        InitializeComponent();
        Loaded += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            var mesas = await _api.GetMesasAsync() ?? new();
            GridMesas.Children.Clear();

            foreach (var m in mesas)
            {
                var btn = new Button
                {
                    Content = $"{m.Nombre}\n{m.Estado}",
                    Margin = new Thickness(10),
                    Padding = new Thickness(10),
                    Foreground = Brushes.White,
                    Height = 120,
                    Background = (m.Estado == "Disponible")
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"))
                        : Brushes.DarkRed
                };

                btn.Click += async (_, __) =>
                {
                    try
                    {
                        // si ya hay orden abierta, entra
                        var idAbierta = await _api.GetOrdenAbiertaAsync(m.IdMesa);
                        if (idAbierta.HasValue)
                        {
                            NavigationService?.Navigate(new OrdenPage(idAbierta.Value, m.Nombre));
                            return;
                        }

                        // si no, crea una
                        var orden = await _api.AbrirOrdenAsync(m.IdMesa);
                        if (orden != null)
                            NavigationService?.Navigate(new OrdenPage(orden.IdOrden, orden.Mesa));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error");
                    }
                };

                GridMesas.Children.Add(btn);
            }
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show(
                $"No pude conectarme a la API en: {ApiConfig.API}\n\n" +
                "1) Verifica que Licoreria.Api esté ejecutándose.\n" +
                "2) Verifica que esté escuchando en el puerto 5128.\n\n" +
                $"Detalle: {ex.Message}",
                "API no disponible");
        }
        catch (TaskCanceledException)
        {
            MessageBox.Show(
                $"Se agotó el tiempo intentando conectar a: {ApiConfig.API}\n\n" +
                "Asegúrate de iniciar Licoreria.Api.",
                "API no disponible");
        }
    }
}
