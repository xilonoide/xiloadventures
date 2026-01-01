using System;
using System.Windows;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class ErrorWindow : Window
{
    private readonly string _errorMessage;

    public ErrorWindow(string message, string? stackTrace = null)
    {
        InitializeComponent();

        _errorMessage = string.IsNullOrEmpty(stackTrace)
            ? message
            : $"{message}\n\n--- Stack Trace ---\n{stackTrace}";

        ErrorText.Text = _errorMessage;
    }

    public ErrorWindow(Exception ex)
        : this(ex.Message, ex.StackTrace)
    {
    }

    private void CopyAndClose_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(_errorMessage);
        }
        catch
        {
            // Si falla el portapapeles, ignoramos
        }

        // Cerrar la aplicación
        Application.Current.Shutdown(1);
    }

    /// <summary>
    /// Muestra la ventana de error y cierra la aplicación después.
    /// </summary>
    public static void ShowFatalError(string message, string? stackTrace = null)
    {
        var window = new ErrorWindow(message, stackTrace);
        window.ShowDialog();
    }

    /// <summary>
    /// Muestra la ventana de error y cierra la aplicación después.
    /// </summary>
    public static void ShowFatalError(Exception ex)
    {
        var window = new ErrorWindow(ex);
        window.ShowDialog();
    }
}
