using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using NAudio.Wave;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class EndingWindow : Window
{
    private const string GitHubUrl = "https://github.com/xilonoide/XiloAdventures";

    private const string DefaultEndingText =
        "¡Enhorabuena, aventurero!\n\n" +
        "Has completado esta aventura con éxito.\n" +
        "Gracias por jugar a esta XiloAdventure.\n\n" +
        "Esperamos que hayas disfrutado del viaje.";

    private WaveOutEvent? _waveOut;
    private Mp3FileReader? _mp3Reader;

    public EndingWindow()
    {
        InitializeComponent();
        Loaded += EndingWindow_Loaded;
    }

    public string? EndingText { get; set; }
    public string? LogoBase64 { get; set; }
    public string? MusicBase64 { get; set; }

    /// <summary>
    /// Si es true, cierra la aplicación al cerrar la ventana. Si es false, solo cierra el diálogo.
    /// </summary>
    public bool CloseApplicationOnExit { get; set; } = true;

    private void EndingWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Texto de finalización (personalizado o por defecto)
        EndingTextBlock.Text = string.IsNullOrWhiteSpace(EndingText)
            ? DefaultEndingText
            : EndingText;

        // Logo
        if (!string.IsNullOrEmpty(LogoBase64))
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

        // Música
        if (!string.IsNullOrEmpty(MusicBase64))
        {
            try
            {
                var musicBytes = Convert.FromBase64String(MusicBase64);
                var tempFile = Path.GetTempFileName() + ".mp3";
                File.WriteAllBytes(tempFile, musicBytes);

                _mp3Reader = new Mp3FileReader(tempFile);
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_mp3Reader);
                _waveOut.Play();
            }
            catch
            {
                // Si falla, continuar sin música
            }
        }

        // Focus para capturar teclas
        Focus();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        CloseApplication();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        CloseApplication();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        StopMusic();
    }

    private void StopMusic()
    {
        _waveOut?.Stop();
        _waveOut?.Dispose();
        _mp3Reader?.Dispose();
        _waveOut = null;
        _mp3Reader = null;
    }

    private void CloseApplication()
    {
        StopMusic();
        if (CloseApplicationOnExit)
        {
            Application.Current.Shutdown();
        }
        else
        {
            Close();
        }
    }

    private void GitHubLink_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Evitar que cierre la aplicación
        Process.Start(new ProcessStartInfo
        {
            FileName = GitHubUrl,
            UseShellExecute = true
        });
    }
}
