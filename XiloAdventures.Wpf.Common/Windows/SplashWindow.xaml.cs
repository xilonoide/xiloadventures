using System.Windows;
using System.Windows.Media;

namespace XiloAdventures.Wpf.Common.Windows;

/// <summary>
/// Ventana de splash screen que muestra el logo de la aplicación
/// mientras se carga la ventana principal.
/// </summary>
public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Establece una imagen personalizada para el logo.
    /// </summary>
    public ImageSource? CustomLogo
    {
        set
        {
            if (value != null)
            {
                LogoImage.Source = value;
            }
        }
    }
}
