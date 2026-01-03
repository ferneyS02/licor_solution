using Licoreria.Desktop.Services;
using System;
using System.Windows;

namespace Licoreria.Desktop.Views;

public partial class ChangePasswordWindow : Window
{
    private readonly ApiService _api = new();

    public ChangePasswordWindow()
    {
        InitializeComponent();
        TxtActual.Focus();
    }

    private async void Guardar_Click(object sender, RoutedEventArgs e)
    {
        TxtInfo.Text = "";
        BtnGuardar.IsEnabled = false;

        try
        {
            var actual = TxtActual.Password ?? "";
            var nueva = TxtNueva.Password ?? "";
            var confirmar = TxtConfirmar.Password ?? "";

            if (string.IsNullOrWhiteSpace(actual) || string.IsNullOrWhiteSpace(nueva) || string.IsNullOrWhiteSpace(confirmar))
            {
                TxtInfo.Text = "Completa todos los campos.";
                return;
            }

            if (nueva.Length < 6)
            {
                TxtInfo.Text = "La nueva contraseña debe tener mínimo 6 caracteres.";
                return;
            }

            if (!string.Equals(nueva, confirmar, StringComparison.Ordinal))
            {
                TxtInfo.Text = "La confirmación no coincide.";
                return;
            }

            var (ok, error) = await _api.CambiarPasswordAsync(actual, nueva);
            if (!ok)
            {
                MessageBox.Show(error ?? "No se pudo cambiar la contraseña.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show("Contraseña actualizada correctamente.", "OK",
                MessageBoxButton.OK, MessageBoxImage.Information);

            DialogResult = true;
            Close();
        }
        finally
        {
            BtnGuardar.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
