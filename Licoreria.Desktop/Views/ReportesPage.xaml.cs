using Licoreria.Desktop;                 // ✅ para Session y ApiConfig
using Licoreria.Desktop.Services;         // ✅ para ApiService
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Windows;
using System.Windows.Controls;

namespace Licoreria.Desktop.Views;

public partial class ReportesPage : Page
{
    private readonly ApiService _api = new();

    public ReportesPage()
    {
        InitializeComponent();
        DpDesde.SelectedDate = DateTime.Today;
        DpHasta.SelectedDate = DateTime.Today;

        // UI: por seguridad, ocultar purga para vendedor (API igual lo bloquea)
        if (Session.Rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase))
            PurgePanel.Visibility = Visibility.Collapsed;
    }

    private async void Generar_Click(object sender, RoutedEventArgs e)
    {
        if (DpDesde.SelectedDate == null || DpHasta.SelectedDate == null)
        {
            MessageBox.Show("Selecciona fechas");
            return;
        }

        var desde = DpDesde.SelectedDate.Value.ToString("yyyy-MM-dd");
        var hasta = DpHasta.SelectedDate.Value.ToString("yyyy-MM-dd");

        var url = $"{ApiConfig.HOST}/api/reportes/rango/pdf?desde={desde}&hasta={hasta}";

        try
        {
            using var http = new HttpClient();

            if (!string.IsNullOrWhiteSpace(Session.Token))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var bytes = await http.GetByteArrayAsync(url);

            var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ReportesLicoreria45");
            Directory.CreateDirectory(outDir);

            var path = Path.Combine(outDir, $"reporte_{desde}_{hasta}.pdf");
            await File.WriteAllBytesAsync(path, bytes);

            TxtInfo.Text = $"PDF guardado en: {path}";
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error PDF");
        }
    }

    private async void Purgar_Click(object sender, RoutedEventArgs e)
    {
        TxtPurgeInfo.Text = "";

        if (!int.TryParse(TxtYears.Text?.Trim(), out var years) || years <= 0)
        {
            MessageBox.Show("Years debe ser un número mayor a 0.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Esto eliminará ventas con más de {years} años.\n\n¿Deseas continuar?",
            "Confirmar purga",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        BtnPurgar.IsEnabled = false;

        try
        {
            var (ok, deleted, error) = await _api.PurgarVentasAsync(years);

            if (!ok)
            {
                MessageBox.Show(error ?? "No se pudo ejecutar la purga.", "Error purga");
                return;
            }

            TxtPurgeInfo.Text = $"Purga OK. Órdenes eliminadas: {deleted}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error purga");
        }
        finally
        {
            BtnPurgar.IsEnabled = true;
        }
    }
}
