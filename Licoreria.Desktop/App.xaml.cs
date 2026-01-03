using System.Windows;
using Licoreria.Desktop.Services;

namespace Licoreria.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        // ✅ Evita que WPF cierre la app cuando se cierre la primera ventana (Login)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        base.OnStartup(e);

        await ApiBootstrapper.EnsureApiRunningAsync();

        var login = new Views.LoginWindow();
        var ok = login.ShowDialog();

        if (ok == true)
        {
            var main = new Views.MainWindow();

            // ✅ Esta debe ser la ventana principal real
            MainWindow = main;

            // ✅ Ahora sí, cerrar MainWindow apaga la app
            ShutdownMode = ShutdownMode.OnMainWindowClose;

            main.Show();
        }
        else
        {
            Shutdown();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        ApiBootstrapper.TryStopApi();
        base.OnExit(e);
    }
}
