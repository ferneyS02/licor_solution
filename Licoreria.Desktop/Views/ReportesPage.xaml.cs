using Licoreria.Desktop;
using Licoreria.Desktop.Services;
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

        InitTimePickers();

        if (Session.Rol.Equals("Vendedor", StringComparison.OrdinalIgnoreCase))
            PurgePanel.Visibility = Visibility.Collapsed;
    }

    private void InitTimePickers()
    {
        for (var h = 0; h < 24; h++)
        {
            var hh = h.ToString("00");
            CbDesdeHora.Items.Add(hh);
            CbHastaHora.Items.Add(hh);
        }

        for (var m = 0; m < 60; m++)
        {
            var mm = m.ToString("00");
            CbDesdeMin.Items.Add(mm);
            CbHastaMin.Items.Add(mm);
        }

        CbDesdeHora.SelectedItem = "00";
        CbDesdeMin.SelectedItem = "00";

        CbHastaHora.SelectedItem = "23";
        CbHastaMin.SelectedItem = "59";
    }

    private async void Generar_Click(object sender, RoutedEventArgs e)
    {
        if (DpDesde.SelectedDate == null || DpHasta.SelectedDate == null)
        {
            MessageBox.Show("Selecciona fechas");
            return;
        }

        int hDesde = int.TryParse(CbDesdeHora.SelectedItem?.ToString(), out var hd) ? hd : 0;
        int mDesde = int.TryParse(CbDesdeMin.SelectedItem?.ToString(), out var md) ? md : 0;
        int hHasta = int.TryParse(CbHastaHora.SelectedItem?.ToString(), out var hh) ? hh : 0;
        int mHasta = int.TryParse(CbHastaMin.SelectedItem?.ToString(), out var mh) ? mh : 0;

        var usarSoloFecha = hDesde == 0 && mDesde == 0 && hHasta == 23 && mHasta == 59;

        DateTime desdeDt;
        DateTime hastaDt;
        string desde;
        string hasta;

        if (usarSoloFecha)
        {
            desdeDt = DpDesde.SelectedDate.Value.Date;
            hastaDt = DpHasta.SelectedDate.Value.Date;

            desde = Uri.EscapeDataString(desdeDt.ToString("yyyy-MM-dd"));
            hasta = Uri.EscapeDataString(hastaDt.ToString("yyyy-MM-dd"));
        }
        else
        {
            desdeDt = DateTime.SpecifyKind(
                DpDesde.SelectedDate.Value.Date.AddHours(hDesde).AddMinutes(mDesde),
                DateTimeKind.Local);

            var hastaBase = DpHasta.SelectedDate.Value.Date.AddHours(hHasta).AddMinutes(mHasta);
            hastaDt = DateTime.SpecifyKind(hastaBase.AddMinutes(1).AddTicks(-1), DateTimeKind.Local);

            desde = Uri.EscapeDataString(desdeDt.ToString("yyyy-MM-dd'T'HH:mm:ss"));
            hasta = Uri.EscapeDataString(hastaDt.ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff"));
        }

        if (hastaDt < desdeDt)
        {
            MessageBox.Show("Rango inválido: 'Hasta' es menor que 'Desde'.");
            return;
        }

        var url = $"{ApiConfig.HOST}/api/reportes/rango/pdf?desde={desde}&hasta={hasta}";

        try
        {
            using var http = new HttpClient();

            if (!string.IsNullOrWhiteSpace(Session.Token))
                http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Session.Token);

            var bytes = await http.GetByteArrayAsync(url);

            var outDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ReportesLicoreria45");
            Directory.CreateDirectory(outDir);

            var fileName = usarSoloFecha
                ? $"reporte_{desdeDt:yyyy-MM-dd}_{hastaDt:yyyy-MM-dd}.pdf"
                : $"reporte_{desdeDt:yyyyMMdd_HHmm}_{hastaDt:yyyyMMdd_HHmm}.pdf";

            var path = Path.Combine(outDir, fileName);
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
