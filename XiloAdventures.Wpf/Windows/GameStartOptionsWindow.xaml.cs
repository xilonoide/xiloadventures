using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Diagnostics;
using XiloAdventures.Wpf.Common.Windows;
using XiloAdventures.Wpf.Common.Ui;

namespace XiloAdventures.Wpf.Windows;

public partial class GameStartOptionsWindow : Window
{
    public GameStartOptionsWindow()
    {
        InitializeComponent();
        Loaded += GameStartOptionsWindow_Loaded;
    }

    private void GameStartOptionsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SoundCheckBox.IsChecked = UiSettingsManager.GlobalSettings.SoundEnabled;
        LlmCheckBox.IsChecked = UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands;
    }

    private void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void LlmInfoIcon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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
        hyperlink.RequestNavigate += LlmHelpLink_RequestNavigate;
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

    private void LlmHelpLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
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

    public bool? SoundEnabled => SoundCheckBox.IsChecked;
    public bool? LlmEnabled => LlmCheckBox.IsChecked;
}
