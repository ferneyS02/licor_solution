using System.IO;
using System.Windows;
using Licoreria.Desktop.Views;

namespace Licoreria.Desktop.Views;

public partial class MainWindow : Window
{
    public string LogoPath { get; }

    public MainWindow()
    {
        InitializeComponent();

        LogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_licoreria.png");
        DataContext = this;

        MainFrame.Navigate(new MesasPage());
    }

    private void Mesas_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new MesasPage());

    private void Inventario_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new InventarioPage());

    private void Reportes_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new ReportesPage());
}
