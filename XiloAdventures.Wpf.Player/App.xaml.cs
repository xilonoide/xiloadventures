using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media.Imaging;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Common.Services;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Player;

public partial class App : Application
{
    private static BitmapSource? _customIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Cargar icono personalizado embebido (si existe)
        LoadCustomIcon();

        // Aplicar icono personalizado a todas las ventanas que se abran
        if (_customIcon != null)
        {
            EventManager.RegisterClassHandler(typeof(Window), Window.LoadedEvent, new RoutedEventHandler(OnWindowLoaded));
        }

        // Capturar excepciones no manejadas
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Dispatcher.Invoke(() => ErrorWindow.ShowFatalError(ex?.Message ?? "Error desconocido", ex?.StackTrace));
        };

        DispatcherUnhandledException += (sender, args) =>
        {
            args.Handled = true;
            ErrorWindow.ShowFatalError(args.Exception);
        };

        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            args.SetObserved();
            Dispatcher.Invoke(() => ErrorWindow.ShowFatalError(args.Exception));
        };

        // Mostrar splash screen
        var splash = new SplashWindow();
        if (_customIcon != null)
        {
            splash.Icon = _customIcon;
            splash.CustomLogo = _customIcon;
        }
        splash.Show();

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Cargar el mundo embebido en background
            WorldModel? world = null;
            await System.Threading.Tasks.Task.Run(() =>
            {
                world = LoadEmbeddedWorld();
            });

            if (world == null)
            {
                splash.Close();
                ErrorWindow.ShowFatalError("No se pudo cargar el mundo del juego.");
                return;
            }

            // Configurar el parser
            Parser.SetWorldDictionary(world.Game.ParserDictionaryJson);

            // Intentar cargar autoguardado, si no existe crear estado inicial
            // Ruta: Documentos/xiloadventures/{worldId}/autosave.xas
            var savesFolder = AppPaths.PlayerSavesFolder(world.Game.Id);
            AppPaths.EnsurePlayerSavesDirectory(world.Game.Id);

            GameState state;
            bool isNewGame = false;
            try
            {
                var autosavePath = Path.Combine(savesFolder, "autosave.xas");

                if (File.Exists(autosavePath))
                {
                    state = SaveManager.LoadFromPath(autosavePath, world);

                    // Validar que el autoguardado pertenece al mundo actual
                    if (!string.Equals(state.WorldId, world.Game.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        state = WorldLoader.CreateInitialState(world);
                        isNewGame = true;
                    }
                }
                else
                {
                    state = WorldLoader.CreateInitialState(world);
                    isNewGame = true;
                }
            }
            catch
            {
                state = WorldLoader.CreateInitialState(world);
                isNewGame = true;
            }

            // Si es partida nueva, inicializar configuración en el GameState
            if (isNewGame)
            {
                state.FontFamily = world.Game.DefaultFontFamily;
                state.FontSize = 18.0;
                state.SoundEnabled = true;
                state.MusicVolume = 10.0;
                state.EffectsVolume = 10.0;
                state.MasterVolume = 10.0;
                state.VoiceVolume = 10.0;
                state.MapEnabled = true;
                state.UseLlmForUnknownCommands = false;
            }

            // Configuración de UI: cargar desde el GameState
            var uiSettings = new UiSettings
            {
                SoundEnabled = state.SoundEnabled,
                FontSize = state.FontSize,
                FontFamily = state.FontFamily,
                MusicVolume = state.MusicVolume,
                EffectsVolume = state.EffectsVolume,
                MasterVolume = state.MasterVolume,
                VoiceVolume = state.VoiceVolume,
                MapEnabled = state.MapEnabled,
                UseLlmForUnknownCommands = state.UseLlmForUnknownCommands
            };

            // Crear el SoundManager y aplicar configuración desde uiSettings
            var soundManager = new SoundManager
            {
                SoundEnabled = uiSettings.SoundEnabled,
                MusicVolume = (float)(uiSettings.MusicVolume / 10.0),
                EffectsVolume = (float)(uiSettings.EffectsVolume / 10.0),
                MasterVolume = (float)(uiSettings.MasterVolume / 10.0),
                VoiceVolume = (float)(uiSettings.VoiceVolume / 10.0)
            };
            soundManager.RefreshVolumes();

            // Suprimir voz durante la inicialización si es partida cargada (no nueva)
            if (!isNewGame)
            {
                soundManager.SuppressVoicePlayback = true;
            }

            // Crear la ventana de juego
            var window = new MainWindow(world, state, soundManager, uiSettings, isRunningFromEditor: false);

            // Restaurar voz tras crear la ventana
            soundManager.SuppressVoicePlayback = false;

            // Precargar la voz de la sala inicial antes de mostrar la ventana,
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
                    // Si algo falla al precargar la voz, continuamos sin interrumpir el inicio del juego.
                }
            }

            // Asegurar mínimo 2 segundos de splash
            var elapsed = stopwatch.ElapsedMilliseconds;
            if (elapsed < 2000)
            {
                await System.Threading.Tasks.Task.Delay((int)(2000 - elapsed));
            }

            // Cerrar splash y mostrar ventana principal
            splash.Close();

            // Mostrar ventana de introducción si es nueva partida y hay texto de intro configurado
            if (isNewGame && !string.IsNullOrWhiteSpace(world.Game.IntroText))
            {
                var introWindow = new IntroWindow
                {
                    IntroText = world.Game.IntroText,
                    LogoBase64 = null,
                    CustomLogo = _customIcon
                };
                if (_customIcon != null)
                {
                    introWindow.Icon = _customIcon;
                }
                introWindow.ShowDialog();
            }

            window.Show();
        }
        catch (Exception ex)
        {
            splash.Close();
            ErrorWindow.ShowFatalError(ex);
        }
    }

    private WorldModel? LoadEmbeddedWorld()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "XiloAdventures.Wpf.Player.world.xaw";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return null;
            }

            // Extraer el recurso a un archivo temporal para poder usar WorldLoader
            var tempPath = Path.Combine(Path.GetTempPath(), "temp_world.xaw");

            using (var fileStream = File.Create(tempPath))
            {
                stream.CopyTo(fileStream);
            }

            try
            {
                // Usar WorldLoader para cargar correctamente el archivo (maneja Base64/ZIP)
                return WorldLoader.LoadWorldModel(tempPath);
            }
            finally
            {
                // Limpiar el archivo temporal
                try { File.Delete(tempPath); } catch { }
            }
        }
        catch
        {
            return null;
        }
    }

    private static void LoadCustomIcon()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "XiloAdventures.Wpf.Player.custom_icon.ico";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return;
            }

            // Cargar el icono desde el stream
            var decoder = new IconBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count > 0)
            {
                // Buscar el frame más grande (mejor calidad)
                _customIcon = decoder.Frames.OrderByDescending(f => f.PixelWidth).First();
            }
        }
        catch
        {
            // Si falla, se usará el icono por defecto
            _customIcon = null;
        }
    }

    private static void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is Window window && _customIcon != null)
        {
            window.Icon = _customIcon;
        }
    }
}
