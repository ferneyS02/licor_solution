using Licoreria.Desktop.Services;
using System;
using System.Windows;

namespace Licoreria.Desktop.Views;

public partial class ResetPasswordWindow : Window
{
    private readonly ApiService _api = new();

    public ResetPasswordWindow()
    {
        InitializeComponent();
        TxtUsuario.Focus();
    }

    private async void Reset_Click(object sender, RoutedEventArgs e)
    {
        TxtInfo.Text = "";
        BtnReset.IsEnabled = false;

        try
        {
            var usuario = (TxtUsuario.Text ?? "").Trim();
            var nueva = TxtNueva.Password ?? "";
            var confirmar = TxtConfirmar.Password ?? "";

            if (string.IsNullOrWhiteSpace(usuario))
            {
                TxtInfo.Text = "Escribe el nombre del usuario (ej: vendedor).";
                return;
            }

            if (string.IsNullOrWhiteSpace(nueva) || nueva.Trim().Length < 6)
            {
                TxtInfo.Text = "La nueva contraseña debe tener mínimo 6 caracteres.";
                return;
            }

            if (!string.Equals(nueva, confirmar, StringComparison.Ordinal))
            {
                TxtInfo.Text = "La confirmación no coincide.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Vas a resetear la contraseña del usuario: {usuario}\n\n¿Deseas continuar?",
                "Confirmar reset",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes) return;

            var (ok, error) = await _api.ResetPasswordAsync(usuario, nueva);

            if (!ok)
            {
                MessageBox.Show(error ?? "No se pudo resetear la contraseña.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Opcional: copiar al portapapeles (muy útil)
            var copy = MessageBox.Show(
                "Contraseña reseteada correctamente.\n\n¿Copiar la nueva contraseña al portapapeles?",
                "OK",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (copy == MessageBoxResult.Yes)
                Clipboard.SetText(nueva);

            DialogResult = true;
            Close();
        }
        finally
        {
            BtnReset.IsEnabled = true;
        }
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
