using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;

namespace Licoreria.Desktop.Views;

public partial class ReportesPage : Page
{
    public ReportesPage()
    {
        InitializeComponent();
        DpDesde.SelectedDate = DateTime.Today;
        DpHasta.SelectedDate = DateTime.Today;
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

        var url = $"{Licoreria.Desktop.ApiConfig.HOST}/api/reportes/rango/pdf?desde={desde}&hasta={hasta}";

        try
        {
            using var http = new HttpClient();
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
}
