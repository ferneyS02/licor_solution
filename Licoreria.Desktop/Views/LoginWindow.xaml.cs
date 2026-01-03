using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Licoreria.Desktop.Services;

namespace Licoreria.Desktop.Views;

public partial class LoginWindow : Window
{
    private readonly ApiService _api = new();

    public LoginWindow()
    {
        InitializeComponent();

        // ✅ asegura foco real
        ContentRendered += (_, __) => EnsureForegroundAndFocus();

#if DEBUG
        // Diagnóstico opcional
        this.AddHandler(Keyboard.PreviewKeyDownEvent, new KeyEventHandler((s, e) =>
        {
            if (Keyboard.FocusedElement is TextBoxBase || Keyboard.FocusedElement is PasswordBox)
                return;

            TxtError.Text = $"(DEBUG) Tecla: {e.Key}";
        }), true);

        this.AddHandler(TextCompositionManager.PreviewTextInputEvent, new TextCompositionEventHandler((s, e) =>
        {
            if (Keyboard.FocusedElement is TextBoxBase || Keyboard.FocusedElement is PasswordBox)
                return;

            TxtError.Text = $"(DEBUG) Texto: '{e.Text}'";
        }), true);
#endif
    }

    private void EnsureForegroundAndFocus()
    {
        try
        {
            Activate();

            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                ShowWindow(hwnd, SW_SHOWNORMAL);
                SetForegroundWindow(hwnd);
            }

            Dispatcher.BeginInvoke(() =>
            {
                if (string.IsNullOrWhiteSpace(TxtUsuario.Text))
                    TxtUsuario.Text = "vendedor";

                TxtUsuario.SelectAll();
                TxtUsuario.Focus();
                Keyboard.Focus(TxtUsuario);
            }, DispatcherPriority.Input);
        }
        catch
        {
            TxtUsuario.Focus();
            Keyboard.Focus(TxtUsuario);
        }
    }

    // ✅ si en XAML lo conectas a PreviewMouseDown de los inputs, evita “primer click perdido”
    private void Input_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is Control ctrl && !ctrl.IsKeyboardFocusWithin)
        {
            e.Handled = true;
            ctrl.Focus();
            Keyboard.Focus(ctrl);
        }
    }

    private async void Login_Click(object sender, RoutedEventArgs e)
    {
        TxtError.Text = "";
        BtnLogin.IsEnabled = false;

        try
        {
            var nombre = (TxtUsuario.Text ?? string.Empty).Trim();
            var pass = TxtPassword.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(nombre) || string.IsNullOrWhiteSpace(pass))
            {
                TxtError.Text = "Escribe usuario y contraseña.";
                return;
            }

            var ok = await _api.LoginAsync(nombre, pass);
            if (!ok)
            {
                TxtError.Text = "Usuario o contraseña inválidos.";
                return;
            }

            // ✅ esto cierra el diálogo y devuelve true a ShowDialog()
            DialogResult = true;
        }
        catch (Exception ex)
        {
            TxtError.Text = ex.Message;
        }
        finally
        {
            BtnLogin.IsEnabled = true;
        }
    }

    private void Password_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            Login_Click(BtnLogin, new RoutedEventArgs());
        }
    }

    private void Salir_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false; // ✅ esto cierra el diálogo
    }

    // Win32: asegurar ventana al frente
    private const int SW_SHOWNORMAL = 1;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
