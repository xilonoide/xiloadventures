using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class TestModeOptionsWindow : Window
{
    public TestModeOptionsWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Inicializar labels
        MusicVolumeLabel.Text = MusicVolumeSlider.Value.ToString("0");
        EffectsVolumeLabel.Text = EffectsVolumeSlider.Value.ToString("0");
        VoiceVolumeLabel.Text = VoiceVolumeSlider.Value.ToString("0");
        MasterVolumeLabel.Text = MasterVolumeSlider.Value.ToString("0");

        // Habilitar/deshabilitar sliders según el estado del sonido
        UpdateSlidersEnabled();
    }

    public bool SoundEnabled
    {
        get => SoundCheckBox.IsChecked == true;
        set => SoundCheckBox.IsChecked = value;
    }

    public double MusicVolume
    {
        get => MusicVolumeSlider.Value;
        set => MusicVolumeSlider.Value = value;
    }

    public double EffectsVolume
    {
        get => EffectsVolumeSlider.Value;
        set => EffectsVolumeSlider.Value = value;
    }

    public double VoiceVolume
    {
        get => VoiceVolumeSlider.Value;
        set => VoiceVolumeSlider.Value = value;
    }

    public double MasterVolume
    {
        get => MasterVolumeSlider.Value;
        set => MasterVolumeSlider.Value = value;
    }

    public bool AiEnabled
    {
        get => AiCheckBox.IsChecked == true;
        set => AiCheckBox.IsChecked = value;
    }

    public bool UseLinuxMode
    {
        get => LinuxRadio.IsChecked == true;
        set
        {
            if (value)
            {
                LinuxRadio.IsChecked = true;
                WindowsRadio.IsChecked = false;
                WindowsTerminalRadio.IsChecked = false;
            }
            UpdateSoundControlsForPlatform();
        }
    }

    public bool UseWindowsTerminalMode
    {
        get => WindowsTerminalRadio.IsChecked == true;
        set
        {
            if (value)
            {
                WindowsTerminalRadio.IsChecked = true;
                WindowsRadio.IsChecked = false;
                LinuxRadio.IsChecked = false;
            }
            else if (!LinuxRadio.IsChecked == true)
            {
                WindowsRadio.IsChecked = true;
            }
            UpdateSoundControlsForPlatform();
        }
    }

    private void SoundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdateSlidersEnabled();
    }

    private void UpdateSlidersEnabled()
    {
        var enabled = SoundCheckBox.IsChecked == true && SoundCheckBox.IsEnabled;
        MusicVolumeSlider.IsEnabled = enabled;
        EffectsVolumeSlider.IsEnabled = enabled;
        VoiceVolumeSlider.IsEnabled = enabled;
        MasterVolumeSlider.IsEnabled = enabled;
    }

    private void UpdateSoundControlsForPlatform()
    {
        var isLinux = LinuxRadio.IsChecked == true;
        var isWindowsTerminal = WindowsTerminalRadio.IsChecked == true;

        // En Linux (Docker) no hay sonido disponible
        // En Windows Terminal sí hay sonido
        SoundCheckBox.IsEnabled = !isLinux;
        SoundDisabledNote.Visibility = isLinux ? Visibility.Visible : Visibility.Collapsed;
        WindowsTerminalNote.Visibility = isWindowsTerminal ? Visibility.Visible : Visibility.Collapsed;
        LinuxNote.Visibility = isLinux ? Visibility.Visible : Visibility.Collapsed;

        if (isLinux)
        {
            SoundCheckBox.IsChecked = false;
        }
        UpdateSlidersEnabled();
    }

    private async void PlatformRadio_Checked(object sender, RoutedEventArgs e)
    {
        // Evitar ejecución durante la inicialización del XAML
        if (!IsLoaded) return;

        UpdateSoundControlsForPlatform();

        // Verificar Docker cuando se selecciona Linux
        if (LinuxRadio.IsChecked == true)
        {
            var dockerAvailable = await IsDockerAvailableAsync();
            if (!dockerAvailable)
            {
                // Volver a Windows
                WindowsRadio.IsChecked = true;
                UpdateSoundControlsForPlatform();

                new AlertWindow(
                    "Docker Desktop es necesario para el modo pruebas Linux.\n\n" +
                    "Por favor, instala Docker Desktop y asegúrate de que esté en ejecución.",
                    "Docker no disponible") { Owner = this }.ShowDialog();
            }
        }
    }

    private async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(startInfo);
            if (process == null) return false;

            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MusicVolumeLabel != null)
        {
            MusicVolumeLabel.Text = e.NewValue.ToString("0");
        }
    }

    private void EffectsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EffectsVolumeLabel != null)
        {
            EffectsVolumeLabel.Text = e.NewValue.ToString("0");
        }
    }

    private void VoiceVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VoiceVolumeLabel != null)
        {
            VoiceVolumeLabel.Text = e.NewValue.ToString("0");
        }
    }

    private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MasterVolumeLabel != null)
        {
            MasterVolumeLabel.Text = e.NewValue.ToString("0");
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        DialogResult = true;
    }

    private void AiInfoIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Evitar que la ventana inicie el arrastre

        var message = @"Si activas la IA, el juego intentará entender mejor comandos complejos o mal escritos. Además, si subes el volumen de voz en las opciones, oirás las descripciones de las salas.

Para usarla debes tener Docker Desktop instalado. La primera vez que se use se descargarán algunos componentes y puede tardar unos minutos. Después funcionará muy rápido.

📋 REQUISITOS DEL SISTEMA

Mínimo:
• RAM: 8 GB
• GPU NVIDIA: No obligatoria (funciona con CPU)
• Espacio en disco: ~10 GB

Recomendado:
• RAM: 16 GB
• GPU NVIDIA RTX con 6+ GB VRAM
• Espacio en disco: ~15 GB

Componentes de IA:
• Comprensión de comandos (llama3): ~5 GB RAM
• Voz (Coqui TTS): ~2 GB RAM

⚡ Con GPU NVIDIA todo funciona más rápido, pero no es obligatorio.";

        var link = new System.Windows.Controls.TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        var hyperlink = new System.Windows.Documents.Hyperlink
        {
            NavigateUri = new System.Uri("https://docs.docker.com/desktop/setup/install/windows-install/")
        };
        hyperlink.Inlines.Add("Instala Docker Desktop");
        hyperlink.RequestNavigate += AiHelpLink_RequestNavigate;
        link.Inlines.Add(new System.Windows.Documents.Run(""));
        link.Inlines.Add(hyperlink);

        var dlg = new AlertWindow(message, "Ayuda sobre la IA")
        {
            Owner = this
        };
        dlg.SetCustomContent(link);
        dlg.HideOkButton();
        dlg.ShowDialog();
    }

    private void AiHelpLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            });
            e.Handled = true;
        }
        catch
        {
            // Ignoramos errores al abrir el navegador
        }
    }
}
