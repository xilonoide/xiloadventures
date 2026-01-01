using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Win32;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

/// <summary>
/// Representa un mundo en la lista con su ruta y título.
/// </summary>
public class WorldListItem
{
    public string FilePath { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;

    public override string ToString() => Title;
}

public partial class StartupWindow : Window
{
    private bool _isStartingNewGame;
    private bool _isLoadingVisible;
    private FileSystemWatcher? _worldsWatcher;
    private DispatcherTimer? _reloadDebounceTimer;

    public StartupWindow()
    {
        InitializeComponent();
        Loaded += StartupWindow_Loaded;
        Closed += StartupWindow_Closed;
    }

    private void StartupWindow_Loaded(object sender, RoutedEventArgs e)
    {
        ReloadWorlds();

        // Seleccionar automáticamente el primer mundo disponible al iniciar,
        // si hay alguno y nada está seleccionado todavía.
        if (WorldsList.Items.Count > 0 && WorldsList.SelectedIndex < 0)
        {
            WorldsList.SelectedIndex = 0;
        }

        WorldsList.SelectionChanged += WorldsList_SelectionChanged;
        UpdateButtonsEnabled();

        // Iniciar FileSystemWatcher para detectar cambios en la carpeta de mundos
        StartWorldsWatcher();
    }

    private void StartupWindow_Closed(object? sender, EventArgs e)
    {
        StopWorldsWatcher();
    }

    private void StartWorldsWatcher()
    {
        try
        {
            if (!Directory.Exists(AppPaths.WorldsFolder))
            {
                Directory.CreateDirectory(AppPaths.WorldsFolder);
            }

            _worldsWatcher = new FileSystemWatcher(AppPaths.WorldsFolder, "*.xaw")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                EnableRaisingEvents = true
            };

            _worldsWatcher.Created += OnWorldFileChanged;
            _worldsWatcher.Deleted += OnWorldFileChanged;
            _worldsWatcher.Renamed += OnWorldFileRenamed;
        }
        catch
        {
            // Ignorar errores al iniciar el watcher
        }
    }

    private void StopWorldsWatcher()
    {
        if (_worldsWatcher != null)
        {
            _worldsWatcher.EnableRaisingEvents = false;
            _worldsWatcher.Created -= OnWorldFileChanged;
            _worldsWatcher.Deleted -= OnWorldFileChanged;
            _worldsWatcher.Renamed -= OnWorldFileRenamed;
            _worldsWatcher.Dispose();
            _worldsWatcher = null;
        }

        if (_reloadDebounceTimer != null)
        {
            _reloadDebounceTimer.Stop();
            _reloadDebounceTimer = null;
        }
    }

    private void OnWorldFileChanged(object sender, FileSystemEventArgs e)
    {
        // Usar debounce para dar tiempo a que el archivo se escriba completamente
        Dispatcher.BeginInvoke(ScheduleReload);
    }

    private void OnWorldFileRenamed(object sender, RenamedEventArgs e)
    {
        // Usar debounce para dar tiempo a que el archivo se escriba completamente
        Dispatcher.BeginInvoke(ScheduleReload);
    }

    private void ScheduleReload()
    {
        // Reiniciar el timer si ya está corriendo (debounce)
        if (_reloadDebounceTimer != null)
        {
            _reloadDebounceTimer.Stop();
        }
        else
        {
            _reloadDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _reloadDebounceTimer.Tick += (_, _) =>
            {
                _reloadDebounceTimer?.Stop();
                ReloadWorlds();
            };
        }

        _reloadDebounceTimer.Start();
    }

    private void ReloadWorlds()
    {
        var previousSelection = WorldsList.SelectedItem as WorldListItem;
        var previousFileName = previousSelection?.FileName;

        WorldsList.Items.Clear();

        if (Directory.Exists(AppPaths.WorldsFolder))
        {
            var files = Directory.GetFiles(AppPaths.WorldsFolder, "*.xaw");
            foreach (var file in files.OrderBy(f => f))
            {
                var item = new WorldListItem
                {
                    FilePath = file,
                    FileName = Path.GetFileNameWithoutExtension(file),
                    Title = ReadWorldTitle(file) ?? Path.GetFileNameWithoutExtension(file)
                };
                WorldsList.Items.Add(item);
            }
        }

        // Restaurar selección anterior si es posible
        if (!string.IsNullOrEmpty(previousFileName))
        {
            for (int i = 0; i < WorldsList.Items.Count; i++)
            {
                if (WorldsList.Items[i] is WorldListItem item && item.FileName == previousFileName)
                {
                    WorldsList.SelectedIndex = i;
                    break;
                }
            }
        }

        if (WorldsList.Items.Count > 0 && WorldsList.SelectedIndex < 0)
        {
            WorldsList.SelectedIndex = 0;
        }

        UpdateButtonsEnabled();
    }

    private static string? ReadWorldTitle(string filePath)
    {
        // Intentar hasta 3 veces con pequeño delay (por si el archivo está bloqueado)
        for (int attempt = 0; attempt < 3; attempt++)
        {
            try
            {
                // Leer el contenido del archivo
                var content = File.ReadAllText(filePath);

                // 1. Intentar decodificar como base64+zip (formato actual del editor)
                if (TryDecodeZippedJson(content, out var json))
                {
                    var title = TryExtractTitleFromJson(json);
                    if (title != null)
                    {
                        return title;
                    }
                }

                // 2. Intentar parsear como JSON plano (formato legacy)
                var titleDirect = TryExtractTitleFromJson(content);
                if (titleDirect != null)
                {
                    return titleDirect;
                }

                // 3. Intentar descifrar (archivos cifrados)
                try
                {
                    var decrypted = CryptoUtil.DecryptFromFile(filePath);
                    // El contenido descifrado puede ser base64+zip o JSON plano
                    if (TryDecodeZippedJson(decrypted, out var decryptedJson))
                    {
                        var title = TryExtractTitleFromJson(decryptedJson);
                        if (title != null)
                        {
                            return title;
                        }
                    }
                    else
                    {
                        var title = TryExtractTitleFromJson(decrypted);
                        if (title != null)
                        {
                            return title;
                        }
                    }
                }
                catch
                {
                    // Si falla el descifrado, el archivo está corrupto o no está cifrado
                }

                // Si llegamos aquí, el archivo existe pero no pudimos leer el título
                break;
            }
            catch (IOException) when (attempt < 2)
            {
                // El archivo podría estar bloqueado, esperar un poco y reintentar
                System.Threading.Thread.Sleep(100);
            }
            catch
            {
                // Otros errores, no reintentar
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Intenta decodificar un texto como base64+zip que contiene world.json.
    /// </summary>
    private static bool TryDecodeZippedJson(string text, out string json)
    {
        json = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        try
        {
            var compressedBytes = Convert.FromBase64String(text);
            using var ms = new MemoryStream(compressedBytes);
            using var zip = new ZipArchive(ms, ZipArchiveMode.Read);

            var entry = zip.GetEntry("world.json") ?? zip.Entries.FirstOrDefault();
            if (entry == null)
                return false;

            using var entryStream = entry.Open();
            using var sr = new StreamReader(entryStream, Encoding.UTF8);
            json = sr.ReadToEnd();
            return !string.IsNullOrWhiteSpace(json);
        }
        catch
        {
            json = string.Empty;
            return false;
        }
    }

    private static string? TryExtractTitleFromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("Game", out var game) &&
                game.TryGetProperty("Title", out var title))
            {
                var titleStr = title.GetString();
                if (!string.IsNullOrWhiteSpace(titleStr))
                {
                    return titleStr;
                }
            }
        }
        catch
        {
            // JSON inválido
        }

        return null;
    }

    private void WorldsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateButtonsEnabled();
    }

    private void UpdateButtonsEnabled()
    {
        var hasSelection = WorldsList.SelectedItem is WorldListItem;

        // Botones que requieren un mundo seleccionado
        if (NewGameButton != null)
            NewGameButton.IsEnabled = hasSelection;

        if (EditorButton != null)
            EditorButton.IsEnabled = hasSelection;

        if (DeleteWorldIcon != null)
        {
            DeleteWorldIcon.IsEnabled = hasSelection;
            DeleteWorldIcon.Opacity = hasSelection ? 1.0 : 0.4;
        }

        // LoadGameButton siempre está habilitado (carga desde archivo)
    }

    private string? GetSelectedWorldFile()
    {
        if (WorldsList.SelectedItem is WorldListItem item)
        {
            return item.FilePath;
        }

        new AlertWindow("Selecciona un mundo primero.") { Owner = this }.ShowDialog();
        return null;
    }

    private async void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isStartingNewGame)
            return;

        var worldPath = GetSelectedWorldFile();
        if (worldPath is null)
            return;

        // Mostrar popup de opciones antes de cargar la partida
        var optionsWindow = new GameStartOptionsWindow { Owner = this };
        if (optionsWindow.ShowDialog() != true)
            return;

        _isStartingNewGame = true;
        NewGameButton.IsEnabled = false;
        ShowLoading("Iniciando partida...");

        try
        {
            WorldModel world;
            GameState state;
            try
            {
                world = WorldLoader.LoadWorldModel(worldPath, null, () => PromptForEncryptionKey("Introduce la clave usada para cifrar este mundo:"));
                Parser.SetWorldDictionary(world.Game.ParserDictionaryJson);
                state = WorldLoader.CreateInitialState(world);
            }
            catch (Exception)
            {
                new AlertWindow("Clave incorrecta", "Error") { Owner = this }.ShowDialog();
                return;
            }


            var uiSettings = UiSettingsManager.LoadForWorld(world.Game.Id, world.Game.DefaultFontFamily);
            // Usar opciones del popup
            uiSettings.SoundEnabled = optionsWindow.SoundEnabled == true;
            uiSettings.UseLlmForUnknownCommands = optionsWindow.LlmEnabled == true;

            var soundManager = new SoundManager()
            {
                SoundEnabled = uiSettings.SoundEnabled,
                MusicVolume = (float)(uiSettings.MusicVolume / 10.0),
                EffectsVolume = (float)(uiSettings.EffectsVolume / 10.0),
                MasterVolume = (float)(uiSettings.MasterVolume / 10.0),
                VoiceVolume = (float)(uiSettings.VoiceVolume / 10.0)
            };
            soundManager.RefreshVolumes();

            // Si la IA está activada para este mundo, preparar los contenedores Docker (IA + voz)
            if (uiSettings.UseLlmForUnknownCommands)
            {
                var dockerWindow = new DockerProgressWindow
                {
                    Owner = this,
                    OllamaModel = "llama3.2:3b"
                };

                var dockerResult = await dockerWindow.RunAsync();
                if (dockerResult.Canceled)
                {
                    uiSettings.UseLlmForUnknownCommands = false;
                    return;
                }

                if (!dockerResult.Success)
                {
                    uiSettings.UseLlmForUnknownCommands = false;

                    new AlertWindow(
                        "No se han podido iniciar los servicios de IA y voz.\n\n" +
                        "Comprueba que Docker Desktop está instalado y en ejecución.",
                        "Error")
                    {
                        Owner = this
                    }.ShowDialog();
                }
            }

            // Precargar la voz de la sala inicial antes de mostrar la partida,
            // para que se escuche nada más entrar.
            if (uiSettings.SoundEnabled && uiSettings.VoiceVolume > 0)
            {
                try
                {
                    var startRoom = state.Rooms
                        .FirstOrDefault(r => r.Id.Equals(state.CurrentRoomId, StringComparison.OrdinalIgnoreCase));
                    if (startRoom != null && !string.IsNullOrWhiteSpace(startRoom.Description))
                    {
                        await soundManager.PreloadRoomVoiceAsync(startRoom.Id, startRoom.Description);
                    }
                }
                catch
                {
                    // Si algo falla al precargar la voz, continuamos sin interrumpir el inicio de la partida.
                }
            }

            // Suprimir voz durante la inicialización para evitar que se lea al crear el GameEngine
            soundManager.SuppressVoicePlayback = true;
            var main = new MainWindow(world, state, soundManager, uiSettings);
            soundManager.SuppressVoicePlayback = false;

            main.Owner = this;
            Hide();

            // Mostrar ventana de introducción si hay texto de intro configurado
            if (!string.IsNullOrWhiteSpace(world.Game.IntroText))
            {
                var introWindow = new IntroWindow
                {
                    IntroText = world.Game.IntroText,
                    LogoBase64 = null
                };
                introWindow.ShowDialog();
            }

            main.ShowDialog();

            // Al cerrar la partida, volvemos a mostrar el inicio
            Show();
            ReloadWorlds();

            // Restaurar selección correcta
            if (WorldsList.Items.Count > 0)
            {
                var worldName = Path.GetFileNameWithoutExtension(worldPath);
                var index = FindWorldIndexByFileName(worldName);
                WorldsList.SelectedIndex = index >= 0 ? index : 0;
            }

        }
        finally
        {
            HideLoading();
            _isStartingNewGame = false;
            NewGameButton.IsEnabled = true;
        }
    }

    private int FindWorldIndexByFileName(string fileName)
    {
        for (int i = 0; i < WorldsList.Items.Count; i++)
        {
            if (WorldsList.Items[i] is WorldListItem item &&
                string.Equals(item.FileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }
        return -1;
    }

    private async void LoadGameButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Cargar partida",
            Filter = "Partidas guardadas (*.xas)|*.xas|Todos los archivos (*.*)|*.*",
            InitialDirectory = AppPaths.SavesFolder
        };

        if (dlg.ShowDialog(this) != true)
            return;

        ShowLoading("Cargando partida...");
        try
        {

            // Primero intentamos descifrar el archivo para obtener el WorldId
            // Usamos la clave por defecto para obtener el ID del mundo
            SaveData? save = null;
            try
            {
                var json = CryptoUtil.DecryptFromFile(dlg.FileName);
                save = System.Text.Json.JsonSerializer.Deserialize<SaveData>(json);
            }
            catch
            {
                // Si falla con la clave por defecto, podría ser que el mundo
                // tenga una clave personalizada. Intentaremos encontrar el mundo
                // de todas formas iterando por todos los mundos disponibles.
            }

            WorldModel? world = null;

            // Si pudimos leer el WorldId, buscamos el mundo correspondiente
            if (save != null && !string.IsNullOrWhiteSpace(save.WorldId))
            {
                if (Directory.Exists(AppPaths.WorldsFolder))
                {
                    foreach (var f in Directory.GetFiles(AppPaths.WorldsFolder, "*.xaw"))
                    {
                        try
                        {
                            var candidate = WorldLoader.LoadWorldModel(f, null, () => PromptForEncryptionKey("Introduce la clave usada para cifrar este mundo:"));
                            if (candidate.Game.Id == save.WorldId)
                            {
                                world = candidate;
                                break;
                            }
                        }
                        catch
                        {
                            // ignorar ficheros corruptos
                        }
                    }
                }
            }
            else
            {
                // Si no pudimos leer el WorldId (partida cifrada con clave personalizada),
                // intentamos cargar con cada mundo disponible hasta que uno funcione
                if (Directory.Exists(AppPaths.WorldsFolder))
                {
                    foreach (var f in Directory.GetFiles(AppPaths.WorldsFolder, "*.xaw"))
                    {
                        try
                        {
                            var candidate = WorldLoader.LoadWorldModel(f, null, () => PromptForEncryptionKey("Introduce la clave usada para cifrar este mundo:"));

                            // Intentamos cargar la partida con este mundo
                            try
                            {
                                var testState = SaveManager.LoadFromPath(dlg.FileName, candidate);
                                if (testState.WorldId == candidate.Game.Id)
                                {
                                    world = candidate;
                                    break;
                                }
                            }
                            catch
                            {
                                // Este no es el mundo correcto, continuar
                            }
                        }
                        catch
                        {
                            // ignorar ficheros corruptos
                        }
                    }
                }
            }

            if (world == null)
            {
                new AlertWindow("No se ha encontrado el mundo correspondiente a la partida, o la clave de cifrado es incorrecta.", "Error") { Owner = this }.ShowDialog();
                return;
            }

            GameState state;
            try
            {
                Parser.SetWorldDictionary(world.Game.ParserDictionaryJson);
                state = SaveManager.LoadFromPath(dlg.FileName, world);

                // Validación adicional: asegurar que el WorldId cargado coincide
                if (!string.Equals(state.WorldId, world.Game.Id, StringComparison.OrdinalIgnoreCase))
                {
                    new AlertWindow(
                        $"La partida cargada no coincide con el mundo seleccionado.\n\n" +
                        $"Partida: '{state.WorldId}'\nMundo: '{world.Game.Id}'",
                        "Error de validación")
                    {
                        Owner = this
                    }.ShowDialog();
                    return;
                }
            }
            catch (Exception)
            {
                new AlertWindow("Error al cargar la partida. Verifica que la clave de cifrado del mundo sea correcta.", "Error") { Owner = this }.ShowDialog();
                return;
            }

            var uiSettings = UiSettingsManager.LoadForWorld(world.Game.Id, world.Game.DefaultFontFamily);

            var soundManager = new SoundManager()
            {
                SoundEnabled = uiSettings.SoundEnabled,
                MusicVolume = (float)(uiSettings.MusicVolume / 10.0),
                EffectsVolume = (float)(uiSettings.EffectsVolume / 10.0),
                MasterVolume = (float)(uiSettings.MasterVolume / 10.0),
                VoiceVolume = (float)(uiSettings.VoiceVolume / 10.0)
            };
            soundManager.RefreshVolumes();

            if (uiSettings.UseLlmForUnknownCommands)
            {
                var dockerWindow = new DockerProgressWindow
                {
                    Owner = this,
                    OllamaModel = "llama3.2:3b"
                };

                var dockerResult = await dockerWindow.RunAsync();
                if (dockerResult.Canceled)
                {
                    uiSettings.UseLlmForUnknownCommands = false;
                    return;
                }

                if (!dockerResult.Success)
                {
                    uiSettings.UseLlmForUnknownCommands = false;

                    new AlertWindow(
                        "No se han podido iniciar los servicios de IA y voz.\n\n" +
                        "Comprueba que Docker Desktop está instalado y en ejecución.",
                        "Error")
                    {
                        Owner = this
                    }.ShowDialog();
                }
            }

            if (uiSettings.SoundEnabled && uiSettings.VoiceVolume > 0)
            {
                try
                {
                    var startRoom = state.Rooms
                        .FirstOrDefault(r => r.Id.Equals(state.CurrentRoomId, StringComparison.OrdinalIgnoreCase));
                    if (startRoom != null && !string.IsNullOrWhiteSpace(startRoom.Description))
                    {
                        await soundManager.PreloadRoomVoiceAsync(startRoom.Id, startRoom.Description);
                    }
                }
                catch
                {
                    // Si algo falla al precargar la voz, continuamos sin interrumpir el inicio de la partida.
                }
            }

            // Suprimir voz durante la inicialización para evitar que se lea al crear el GameEngine
            soundManager.SuppressVoicePlayback = true;
            var main = new MainWindow(world, state, soundManager, uiSettings);
            soundManager.SuppressVoicePlayback = false;

            main.Owner = this;
            Hide();
            main.ShowDialog();
            Show();
            ReloadWorlds();

            // Restaurar selección del mundo cargado
            if (WorldsList.Items.Count > 0)
            {
                var worldName = Path.GetFileNameWithoutExtension(world.Game.Id);
                var index = FindWorldIndexByFileName(worldName);
                WorldsList.SelectedIndex = index >= 0 ? index : 0;
            }
        }
        finally
        {
            HideLoading();
        }
    }
    private void EditorButton_Click(object sender, RoutedEventArgs e)
    {
        OpenSelectedWorldInEditor();
    }

    private void WorldsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WorldsList.SelectedItem is null)
        {
            return;
        }

        OpenSelectedWorldInEditor();
    }

    private void OpenSelectedWorldInEditor()
    {
        string? worldPath = null;
        string? selectedFileName = null;

        // Si hay un mundo seleccionado en la lista, intentamos abrir su fichero
        if (WorldsList.SelectedItem is WorldListItem selectedItem)
        {
            selectedFileName = selectedItem.FileName;
            if (File.Exists(selectedItem.FilePath))
            {
                worldPath = selectedItem.FilePath;
            }
        }

        // Si worldPath es null (fichero no existe), el editor creará un mundo nuevo
        var editor = new WorldEditorWindow(worldPath);
        if (editor.IsCanceled)
            return;
        editor.Owner = this;
        Hide();
        editor.ShowDialog();
        Show();
        ReloadWorlds();

        // Restaurar selección
        if (WorldsList.Items.Count > 0)
        {
            if (selectedFileName != null)
            {
                var index = FindWorldIndexByFileName(selectedFileName);
                WorldsList.SelectedIndex = index >= 0 ? index : 0;
            }
            else
            {
                WorldsList.SelectedIndex = 0;
            }
        }
    }


    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }


    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        var dlg = new ConfirmWindow("\u00bfSeguro que quieres salir de Xilo Adventures?", "Confirmar salida")
        {
            Owner = this
        };
        var result = dlg.ShowDialog() == true;

        if (!result)
        {
            e.Cancel = true;
        }

        base.OnClosing(e);
    }

    private void ShowLoading(string message)
    {
        _isLoadingVisible = true;
        LoadingText.Text = message;
        LoadingOverlay.Visibility = Visibility.Visible;
    }

    private void HideLoading()
    {
        if (!_isLoadingVisible)
            return;

        _isLoadingVisible = false;
        LoadingOverlay.Visibility = Visibility.Collapsed;
    }

    private string? PromptForEncryptionKey(string message)
    {
        var passwordBox = new PasswordBox
        {
            Margin = new Thickness(0, 12, 0, 0),
            MinWidth = 320
        };

        var dialog = new AlertWindow(message, "Clave de cifrado")
        {
            Owner = this
        };
        dialog.SetCustomContent(passwordBox);
        dialog.ShowCancelButton();
        dialog.Loaded += (_, _) => passwordBox.Focus();

        var result = dialog.ShowDialog();
        if (result == true)
        {
            return passwordBox.Password.Trim();
        }

        return null;
    }

    private void DonateLink_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://www.paypal.me/xmasmusicsoft",
            UseShellExecute = true
        });
    }

    private void OpenWorldsFolder_Click(object sender, MouseButtonEventArgs e)
    {
        try
        {
            if (!Directory.Exists(AppPaths.WorldsFolder))
            {
                Directory.CreateDirectory(AppPaths.WorldsFolder);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.WorldsFolder,
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignorar errores
        }
    }

    private void WorldGenerator_Click(object sender, MouseButtonEventArgs e)
    {
        var promptWindow = new PromptGeneratorWindow
        {
            Owner = this
        };
        promptWindow.ShowDialog();

        // Recargar la lista después de cerrar el generador (el archivo ya está guardado)
        ReloadWorlds();
    }

    private void CreateFromZones_Click(object sender, MouseButtonEventArgs e)
    {
        // Seleccionar múltiples archivos .json de zona
        var openDlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecciona los archivos de zona a fusionar",
            Filter = "Archivos de zona (*.json)|*.json|Todos los archivos (*.*)|*.*",
            Multiselect = true
        };

        if (openDlg.ShowDialog(this) != true || openDlg.FileNames.Length == 0)
            return;

        if (openDlg.FileNames.Length < 2)
        {
            new AlertWindow("Debes seleccionar al menos 2 archivos de zona para fusionar.", "Error")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        // Ordenar archivos por nombre para mantener orden consistente
        var sortedFiles = openDlg.FileNames.OrderBy(f => System.IO.Path.GetFileName(f)).ToArray();

        // Seleccionar donde guardar el .xaw fusionado
        var saveDlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Guardar mundo fusionado",
            Filter = "Mundos (*.xaw)|*.xaw",
            InitialDirectory = AppPaths.WorldsFolder,
            FileName = "mundo_fusionado.xaw"
        };

        if (saveDlg.ShowDialog(this) != true)
            return;

        try
        {
            WorldLoader.MergeAndSaveZoneFiles(sortedFiles, saveDlg.FileName);
            new AlertWindow($"Mundo fusionado correctamente:\n{System.IO.Path.GetFileName(saveDlg.FileName)}\n\n{sortedFiles.Length} zonas combinadas.", "Éxito")
            {
                Owner = this
            }.ShowDialog();

            // Recargar la lista de mundos
            ReloadWorlds();
        }
        catch (Exception ex)
        {
            new AlertWindow($"Error al fusionar los mundos:\n{ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void CreateNewWorld_Click(object sender, MouseButtonEventArgs e)
    {
        // Abrir el editor con null para crear un mundo nuevo
        var editor = new WorldEditorWindow(null);
        if (editor.IsCanceled)
            return;
        editor.Owner = this;
        Hide();
        editor.ShowDialog();
        Show();
        ReloadWorlds();

        // Seleccionar el primer mundo si hay alguno
        if (WorldsList.Items.Count > 0)
        {
            WorldsList.SelectedIndex = 0;
        }
    }

    private void DeleteWorldIcon_Click(object sender, MouseButtonEventArgs e)
    {
        if (WorldsList.SelectedItem is not WorldListItem selectedItem)
        {
            new AlertWindow("Selecciona un mundo primero.") { Owner = this }.ShowDialog();
            return;
        }

        var dlg = new ConfirmWindow(
            $"¿Seguro que quieres eliminar el mundo \"{selectedItem.Title}\"?\n\nEsta acción no se puede deshacer.",
            "Eliminar mundo")
        {
            Owner = this
        };

        var result = dlg.ShowDialog() == true;
        if (!result)
            return;

        try
        {
            if (File.Exists(selectedItem.FilePath))
            {
                File.Delete(selectedItem.FilePath);
            }
        }
        catch (Exception ex)
        {
            new AlertWindow($"No se ha podido eliminar el mundo:\n{ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        ReloadWorlds();
    }
}








