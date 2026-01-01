using System.Diagnostics;
using System.Windows;
using XiloAdventures.Wpf.Common.Services;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Windows;
using XiloAdventures.Wpf.Windows;

namespace XiloAdventures.Wpf;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        // Manejo de errores global para diagnóstico
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Dispatcher.Invoke(() => ErrorWindow.ShowFatalError(ex?.Message ?? "Error desconocido", ex?.StackTrace));
        };
        DispatcherUnhandledException += (s, args) =>
        {
            args.Handled = true;
            ErrorWindow.ShowFatalError(args.Exception);
        };
        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            args.SetObserved();
            Dispatcher.Invoke(() => ErrorWindow.ShowFatalError(args.Exception));
        };

        base.OnStartup(e);

        // Mostrar splash screen
        var splash = new SplashWindow();
        splash.Show();

        var stopwatch = Stopwatch.StartNew();

        // Preparar la aplicación en background
        await System.Threading.Tasks.Task.Run(() =>
        {
            AppPaths.EnsureDirectories();
            UiSettingsManager.LoadGlobal();
        });

        // Crear la ventana principal
        var startup = new StartupWindow();

        // Asegurar mínimo 2 segundos de splash
        var elapsed = stopwatch.ElapsedMilliseconds;
        if (elapsed < 2000)
        {
            await System.Threading.Tasks.Task.Delay((int)(2000 - elapsed));
        }

        // Cerrar splash y mostrar ventana principal
        splash.Close();
        startup.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Al cerrar la aplicación intentamos cerrar Docker Desktop por completo.
        try
        {
            DockerShutdownHelper.TryShutdownDockerDesktop();
        }
        catch
        {
            // Ignoramos cualquier error; no queremos bloquear el cierre de la app.
        }

        base.OnExit(e);
    }
}
