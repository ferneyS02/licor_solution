using Licoreria.Desktop; // Session
using System;
using System.IO;
using System.Windows;

namespace Licoreria.Desktop.Views;

public partial class MainWindow : Window
{
    public string LogoPath { get; }

    public MainWindow()
    {
        InitializeComponent();

        LogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "logo_licoreria.png");
        DataContext = this;

        TxtUsuario.Text = $"Usuario: {Session.Nombre} ({Session.Rol})";

        ApplyRoleUi();

        MainFrame.Navigate(new MesasPage());
    }

    private void ApplyRoleUi()
    {
        var canAdmin = Session.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                       || Session.Rol.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

        BtnInventario.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
        BtnReportes.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
        BtnCambiarPass.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
        BtnResetPass.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Mesas_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new MesasPage());

    private void Inventario_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new InventarioPage());

    private void Reportes_Click(object sender, RoutedEventArgs e) =>
        MainFrame.Navigate(new ReportesPage());

    private void CambiarPass_Click(object sender, RoutedEventArgs e)
    {
        var win = new ChangePasswordWindow { Owner = this };
        win.ShowDialog();
    }

    private void ResetPass_Click(object sender, RoutedEventArgs e)
    {
        var win = new ResetPasswordWindow { Owner = this };
        win.ShowDialog();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Session.Clear();

        var login = new LoginWindow();
        var ok = login.ShowDialog();
        if (ok == true)
        {
            TxtUsuario.Text = $"Usuario: {Session.Nombre} ({Session.Rol})";
            ApplyRoleUi();
            MainFrame.Navigate(new MesasPage());
        }
        else
        {
            Close();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
}
