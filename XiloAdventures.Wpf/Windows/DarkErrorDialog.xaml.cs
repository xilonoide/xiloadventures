using System.Windows;
using System.Windows.Input;

namespace XiloAdventures.Wpf.Windows;

/// <summary>
/// Diálogo de error con tema oscuro que muestra mensajes detallados.
/// </summary>
public partial class DarkErrorDialog : Window
{
    public DarkErrorDialog(string title, string message, Window? owner = null)
    {
        InitializeComponent();

        TitleText.Text = title;
        MessageText.Text = message;

        if (owner != null)
        {
            Owner = owner;
        }
    }

    /// <summary>
    /// Muestra un diálogo de error con tema oscuro.
    /// </summary>
    /// <param name="title">Título del diálogo.</param>
    /// <param name="message">Mensaje de error detallado.</param>
    /// <param name="owner">Ventana padre opcional.</param>
    public static void Show(string title, string message, Window? owner = null)
    {
        var dialog = new DarkErrorDialog(title, message, owner);
        dialog.ShowDialog();
    }

    /// <summary>
    /// Muestra un diálogo de error para una excepción.
    /// </summary>
    /// <param name="title">Título del diálogo.</param>
    /// <param name="ex">Excepción a mostrar.</param>
    /// <param name="owner">Ventana padre opcional.</param>
    public static void ShowException(string title, Exception ex, Window? owner = null)
    {
        var message = ex.Message;
        if (ex.InnerException != null)
        {
            message += "\n\nDetalles internos:\n" + ex.InnerException.Message;
        }
        var dialog = new DarkErrorDialog(title, message, owner);
        dialog.ShowDialog();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            // Doble clic para maximizar/restaurar
            WindowState = WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
        else
        {
            DragMove();
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(MessageText.Text);
        }
        catch
        {
            // Ignorar errores del portapapeles
        }

        Close();
    }
}
