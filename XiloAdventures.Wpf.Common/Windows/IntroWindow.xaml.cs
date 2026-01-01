using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class IntroWindow : Window
{
    public IntroWindow()
    {
        InitializeComponent();
        Loaded += IntroWindow_Loaded;
    }

    public string? IntroText { get; set; }
    public string? LogoBase64 { get; set; }
    public ImageSource? CustomLogo { get; set; }

    private void IntroWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Texto de introducción
        if (!string.IsNullOrWhiteSpace(IntroText))
        {
            IntroTextBlock.Text = IntroText;
        }

        // Logo: prioridad CustomLogo > LogoBase64 > logo por defecto
        if (CustomLogo != null)
        {
            LogoImage.Source = CustomLogo;
        }
        else if (!string.IsNullOrEmpty(LogoBase64))
        {
            try
            {
                var imageBytes = Convert.FromBase64String(LogoBase64);
                var image = new BitmapImage();
                using var ms = new MemoryStream(imageBytes);
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.StreamSource = ms;
                image.EndInit();
                image.Freeze();
                LogoImage.Source = image;
            }
            catch
            {
                // Si falla, usar logo por defecto
                LogoImage.Source = new BitmapImage(new Uri("/XiloAdventures.Wpf.Common;component/Assets/logo.png", UriKind.Relative));
            }
        }
        else
        {
            // Logo por defecto
            LogoImage.Source = new BitmapImage(new Uri("/XiloAdventures.Wpf.Common;component/Assets/logo.png", UriKind.Relative));
        }

        // Focus para capturar teclas
        Focus();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        Close();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        Close();
    }
}
