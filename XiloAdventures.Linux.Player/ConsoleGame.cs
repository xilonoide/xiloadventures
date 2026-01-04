using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Linux.Player.Audio;
using XiloAdventures.Linux.Player.Screens;

namespace XiloAdventures.Linux.Player;

/// <summary>
/// Motor principal del juego de consola
/// </summary>
public class ConsoleGame
{
    private readonly GameOptions _options;
    private WorldModel? _world;
    private GameState? _state;
    private GameEngine? _engine;
    private LinuxSoundManager? _soundManager;
    private readonly ConsoleInput _input = new();
    private readonly GameScreen _gameScreen = new();

    private bool _isRunning;
    private bool _needsRedraw = true;
    private bool _isInCombat;
    private bool _isInTrade;
    private bool _isInCraft;
    private bool _isInConversation;
    private List<DialogueOption>? _currentDialogueOptions;

    // Para detectar cambios de tamaño
    private int _lastConsoleWidth;
    private int _lastConsoleHeight;

    // Cliente HTTP para consultas a Ollama LLM
    // En Docker (modo pruebas): acceder al host Windows via host.docker.internal
    // En Linux nativo: los servicios corren localmente en localhost
    private static string OllamaHost => File.Exists("/.dockerenv") ? "host.docker.internal" : "localhost";

    private HttpClient? _httpClient;
    private HttpClient HttpClient => _httpClient ??= new HttpClient
    {
        BaseAddress = new Uri($"http://{OllamaHost}:11434/"),
        // Timeout largo porque Ollama puede tardar ~30s en cargar el modelo la primera vez
        Timeout = TimeSpan.FromSeconds(120)
    };

    public ConsoleGame(GameOptions options)
    {
        _options = options;
        _lastConsoleWidth = Console.WindowWidth;
        _lastConsoleHeight = Console.WindowHeight;
    }

    /// <summary>
    /// Ejecuta el juego
    /// </summary>
    public async Task RunAsync()
    {
        try
        {
            // Cargar el mundo
            if (!LoadWorld())
            {
                ConsoleRenderer.WriteLine("Error: No se pudo cargar el mundo del juego.", Colors.Error);
                return;
            }

            // Inicializar sonido
            InitializeSound();

            // Mostrar intro si existe
            if (!string.IsNullOrWhiteSpace(_world!.Game.IntroText))
            {
                _gameScreen.ShowIntro(_world.Game.Title ?? "XiloAdventures", _world.Game.IntroText);
                _input.WaitForEnter();
            }

            // Configurar parser
            Parser.SetWorldDictionary(_world.Game.ParserDictionaryJson);

            // Crear estado inicial
            _state = WorldLoader.CreateInitialState(_world);

            // Configurar IA según opciones de línea de comandos
            _state.UseLlmForUnknownCommands = _options.IaEnabled;

            // Crear motor del juego
            _engine = new GameEngine(_world, _state, _soundManager!, isDebugMode: false);

            // Suscribirse a eventos
            SubscribeToEngineEvents();

            // Disparar scripts iniciales
            _engine.TriggerInitialScripts();

            // Configurar callback para detectar resize durante la entrada
            _input.OnResizeDetected = () =>
            {
                if (_state != null && _engine?.CurrentRoom != null && _world != null)
                {
                    _gameScreen.ClearAndRedraw(_state, _engine.CurrentRoom, _world);
                }
            };

            // Loop principal
            _isRunning = true;
            await GameLoopAsync();
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteLine($"Error fatal: {ex.Message}", Colors.Error);
        }
        finally
        {
            _soundManager?.Dispose();
            _httpClient?.Dispose();
        }
    }

    private bool LoadWorld()
    {
        try
        {
            // Intentar cargar mundo embebido
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "XiloAdventures.Linux.Player.world.xaw";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"temp_world_{Guid.NewGuid():N}.xaw");
                try
                {
                    using (var fileStream = File.Create(tempPath))
                    {
                        stream.CopyTo(fileStream);
                    }
                    _world = WorldLoader.LoadWorldModel(tempPath);
                    return _world != null;
                }
                finally
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }

            // Fallback: buscar world.xaw en el directorio actual (para desarrollo)
            var localPath = Path.Combine(AppContext.BaseDirectory, "world.xaw");
            if (File.Exists(localPath))
            {
                _world = WorldLoader.LoadWorldModel(localPath);
                return _world != null;
            }

            return false;
        }
        catch (Exception ex)
        {
            ConsoleRenderer.WriteLine($"Error al cargar el mundo: {ex.Message}", Colors.Error);
            return false;
        }
    }

    private void InitializeSound()
    {
        _soundManager = new LinuxSoundManager();

        // Si el sonido está desactivado por línea de comandos, no inicializar
        if (!_options.SoundEnabled)
        {
            _soundManager.SoundEnabled = false;
            return;
        }

        if (!_soundManager.Initialize())
        {
            // Mostrar aviso de que no hay sonido
            ConsoleRenderer.Clear();
            ConsoleRenderer.DrawTopBorder();
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawCenteredLine($"{Colors.Yellow}Aviso: Sonido no disponible{Colors.Reset}");
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawSeparator(thin: true);
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawWrappedText("El juego funcionara sin sonido.");
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawWrappedText("Para habilitar sonido en Linux, instala SDL2:", color: Colors.Gray);
            ConsoleRenderer.DrawLine("  Ubuntu/Debian: sudo apt install libsdl2-2.0-0", color: Colors.Cyan);
            ConsoleRenderer.DrawLine("  Fedora: sudo dnf install SDL2", color: Colors.Cyan);
            ConsoleRenderer.DrawLine("  Arch: sudo pacman -S sdl2", color: Colors.Cyan);
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawBottomBorder();
            _input.WaitForEnter();
        }
    }

    private void SubscribeToEngineEvents()
    {
        if (_engine == null) return;

        _engine.RoomChanged += OnRoomChanged;
        _engine.ScriptMessage += OnScriptMessage;
        _engine.CombatStarted += OnCombatStarted;
        _engine.TradeOpened += OnTradeOpened;
        _engine.CraftOpened += OnCraftOpened;
        _engine.ConversationDialogue += OnConversationDialogue;
        _engine.ConversationOptions += OnConversationOptions;
        _engine.ConversationEnded += OnConversationEnded;
        _engine.PlayerDied += OnPlayerDied;
        _engine.AdventureCompleted += OnAdventureCompleted;
        _engine.HelpRequested += OnHelpRequested;
    }

    private async Task GameLoopAsync()
    {
        while (_isRunning && _state != null && _engine != null)
        {
            // Detectar cambios de tamaño de la consola - hacer CLS completo
            if (HasConsoleSizeChanged())
            {
                _gameScreen.ClearAndRedraw(_state!, _engine!.CurrentRoom!, _world!);
            }

            // Redibujar pantalla si es necesario
            if (_needsRedraw && !_isInCombat && !_isInTrade && !_isInCraft)
            {
                var currentRoom = _engine.CurrentRoom;
                _gameScreen.Render(_state, currentRoom!, _world!);
                _needsRedraw = false;
            }

            // Leer entrada (el cursor ya está posicionado por el layout)
            var input = _input.ReadLineInPlace();

            if (string.IsNullOrWhiteSpace(input))
                continue;

            // Añadir comando al historial
            _gameScreen.AddCommandToHistory(input);

            // Procesar comandos especiales de la consola
            if (await ProcessSpecialCommandAsync(input))
                continue;

            // Si estamos en conversación, manejar opciones de diálogo
            if (_isInConversation && _currentDialogueOptions != null)
            {
                if (int.TryParse(input, out int optionIndex) &&
                    optionIndex >= 1 && optionIndex <= _currentDialogueOptions.Count)
                {
                    var result = _engine.ProcessCommand($"opcion {optionIndex}");
                    if (!string.IsNullOrWhiteSpace(result.Message))
                    {
                        _gameScreen.ShowMessage(result.Message);
                    }
                    continue;
                }
            }

            // Procesar comando normal del motor
            var commandResult = _engine.ProcessCommand(input);

            if (commandResult.ClearScreenBefore)
            {
                _needsRedraw = true;
            }

            // Si hubo error y la IA está activada, consultar al LLM
            if (commandResult.HasError && _state.UseLlmForUnknownCommands)
            {
                _gameScreen.ShowMessage($"{Colors.Gray}Consultando IA...{Colors.Reset}");

                var (llmCommand, connectionError) = await TryAskLlmForUnknownCommandAsync(input);

                if (connectionError)
                {
                    // No se pudo conectar con Ollama
                    _gameScreen.ShowError($"{Colors.Yellow}(IA no disponible){Colors.Reset} {commandResult.Message}");
                }
                else if (!string.IsNullOrWhiteSpace(llmCommand) &&
                    !llmCommand.Equals("NO_ENTIENDO", StringComparison.OrdinalIgnoreCase) &&
                    !llmCommand.Contains("NO_ENTIENDO"))
                {
                    // La IA sugirió un comando válido, ejecutarlo
                    var llmResult = _engine.ProcessCommand(llmCommand);

                    if (!llmResult.HasError)
                    {
                        if (llmResult.ClearScreenBefore)
                        {
                            _needsRedraw = true;
                        }

                        _gameScreen.ShowMessage($"{Colors.Gray}(Interpretado: {llmCommand}){Colors.Reset}");
                        if (!string.IsNullOrWhiteSpace(llmResult.Message))
                        {
                            _gameScreen.ShowMessage(llmResult.Message);
                        }
                        commandResult = llmResult;
                    }
                    else
                    {
                        // El comando sugerido tampoco funcionó
                        _gameScreen.ShowError(commandResult.Message);
                    }
                }
                else
                {
                    // La IA no pudo interpretar el comando
                    _gameScreen.ShowError(commandResult.Message);
                }
            }
            else if (!string.IsNullOrWhiteSpace(commandResult.Message))
            {
                if (commandResult.HasError)
                {
                    _gameScreen.ShowError(commandResult.Message);
                }
                else
                {
                    _gameScreen.ShowMessage(commandResult.Message);
                }
            }
            else
            {
                // Si no hay mensaje pero hubo cambio, redibujar
                _needsRedraw = true;
            }
        }
    }

    private async Task<bool> ProcessSpecialCommandAsync(string input)
    {
        var cmd = input.Trim().ToLowerInvariant();

        switch (cmd)
        {
            case "misiones":
            case "quests":
            case "q":
                ShowQuests();
                _input.WaitForEnter();
                _needsRedraw = true;
                return true;

            case "cls":
            case "clear":
            case "limpiar":
                _gameScreen.ClearAndRedraw(_state!, _engine!.CurrentRoom!, _world!);
                return true;

            case "salir":
            case "exit":
            case "quit":
                if (ConfirmExit())
                {
                    _isRunning = false;
                }
                else
                {
                    _needsRedraw = true;
                }
                return true;

            case "ayuda":
            case "help":
            case "?":
            case "comandos":
                ShowHelp();
                _input.WaitForEnter();
                _needsRedraw = true;
                return true;

            case "guardar":
            case "save":
                SaveGame();
                return true;

            case "cargar":
            case "load":
                LoadGame();
                return true;

            default:
                // Comandos de volumen
                if (cmd == "vol" || cmd == "volumen")
                {
                    ShowVolumes();
                    _input.WaitForEnter();
                    _needsRedraw = true;
                    return true;
                }

                if (cmd.StartsWith("vol ") || cmd.StartsWith("volumen "))
                {
                    var args = cmd.StartsWith("vol ") ? cmd[4..].Trim() : cmd[8..].Trim();
                    ProcessVolumeCommand(args);
                    return true;
                }

                // Comandos de sonido on/off
                if (cmd == "sonido" || cmd == "sound")
                {
                    ToggleSound();
                    return true;
                }

                if (cmd.StartsWith("sonido ") || cmd.StartsWith("sound "))
                {
                    var args = cmd.StartsWith("sonido ") ? cmd[7..].Trim() : cmd[6..].Trim();
                    ProcessSoundCommand(args);
                    return true;
                }

                // Comandos de IA on/off
                if (cmd == "ia" || cmd == "ai")
                {
                    ToggleAi();
                    return true;
                }

                if (cmd.StartsWith("ia ") || cmd.StartsWith("ai "))
                {
                    var args = cmd.StartsWith("ia ") ? cmd[3..].Trim() : cmd[3..].Trim();
                    await ProcessAiCommandAsync(args);
                    return true;
                }

                return false;
        }
    }

    private bool ConfirmExit()
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawConfirmDialog("Salir", "Seguro que quieres salir del juego?");
        return _input.ReadConfirmation();
    }

    private void ShowHelp()
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder();
        ConsoleRenderer.DrawTitle("AYUDA - COMANDOS");
        ConsoleRenderer.DrawSeparator();
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Movimiento:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  norte, sur, este, oeste, arriba, abajo");
        ConsoleRenderer.DrawLine("  (o abreviado: n, s, e, o, subir, bajar)");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Objetos:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  examinar [objeto], coger [objeto], soltar [objeto]");
        ConsoleRenderer.DrawLine("  usar [objeto], abrir [objeto], cerrar [objeto]");
        ConsoleRenderer.DrawLine("  meter [objeto] en [contenedor]");
        ConsoleRenderer.DrawLine("  sacar [objeto] de [contenedor]");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Personajes:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  hablar [personaje], dar [objeto] a [personaje]");
        ConsoleRenderer.DrawLine("  atacar [personaje]");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Equipamiento:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  equipar [objeto], desequipar [objeto]");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Sistema:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  misiones, guardar, cargar, ayuda, cls, salir");

        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Sonido:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  sonido           - Activar/desactivar sonido");
        ConsoleRenderer.DrawLine("  sonido on/off    - Activar o desactivar sonido");
        ConsoleRenderer.DrawLine("  vol              - Ver volumenes actuales");
        ConsoleRenderer.DrawLine("  vol m 50         - Musica al 50%");
        ConsoleRenderer.DrawLine("  vol e 50         - Efectos al 50%");
        ConsoleRenderer.DrawLine("  vol v 50         - Voz al 50%");
        ConsoleRenderer.DrawLine("  vol t 50         - Maestro al 50%");
        ConsoleRenderer.DrawLine("  vol m+ / vol m-  - Subir/bajar 10%");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawLine($"{Colors.Bold}Inteligencia Artificial:{Colors.Reset}");
        ConsoleRenderer.DrawLine("  ia               - Ver estado de la IA y voz");
        ConsoleRenderer.DrawLine("  ia on/off        - Activar/desactivar IA y voz TTS");
        ConsoleRenderer.DrawEmptyLine();

        ConsoleRenderer.DrawBottomBorder();
    }

    private void ShowVolumes()
    {
        if (_soundManager == null)
        {
            _gameScreen.ShowError("Sonido no disponible.");
            return;
        }

        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder();
        ConsoleRenderer.DrawTitle("VOLUMENES");
        ConsoleRenderer.DrawSeparator();
        ConsoleRenderer.DrawEmptyLine();

        var master = (int)(_soundManager.MasterVolume * 100);
        var music = (int)(_soundManager.MusicVolume * 100);
        var effects = (int)(_soundManager.EffectsVolume * 100);
        var voice = (int)(_soundManager.VoiceVolume * 100);

        DrawVolumeBar("Maestro (t)", master);
        DrawVolumeBar("Musica  (m)", music);
        DrawVolumeBar("Efectos (e)", effects);
        DrawVolumeBar("Voz     (v)", voice);

        ConsoleRenderer.DrawEmptyLine();
        ConsoleRenderer.DrawLine($"  {Colors.Gray}Sonido: {(_soundManager.SoundEnabled ? "Activado" : "Desactivado")}{Colors.Reset}");
        ConsoleRenderer.DrawEmptyLine();
        ConsoleRenderer.DrawBottomBorder();
    }

    private void DrawVolumeBar(string label, int percent)
    {
        const int barWidth = 20;
        var filled = (int)(percent / 100.0 * barWidth);
        var empty = barWidth - filled;
        var bar = new string('█', filled) + new string('░', empty);
        ConsoleRenderer.DrawLine($"  {label}: {Colors.Cyan}{bar}{Colors.Reset} {percent,3}%");
    }

    private void ProcessVolumeCommand(string args)
    {
        if (_soundManager == null)
        {
            _gameScreen.ShowError("Sonido no disponible.");
            return;
        }

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            ShowVolumes();
            _input.WaitForEnter();
            _needsRedraw = true;
            return;
        }

        var channel = parts[0].ToLowerInvariant();
        float currentVolume;
        string channelName;

        // Determinar canal
        switch (channel.TrimEnd('+', '-'))
        {
            case "m":
            case "musica":
            case "music":
                currentVolume = _soundManager.MusicVolume;
                channelName = "Música";
                break;
            case "e":
            case "efectos":
            case "effects":
            case "fx":
                currentVolume = _soundManager.EffectsVolume;
                channelName = "Efectos";
                break;
            case "v":
            case "voz":
            case "voice":
                currentVolume = _soundManager.VoiceVolume;
                channelName = "Voz";
                break;
            case "t":
            case "maestro":
            case "master":
            case "total":
                currentVolume = _soundManager.MasterVolume;
                channelName = "Maestro";
                break;
            default:
                _gameScreen.ShowError($"Canal desconocido: {channel}");
                return;
        }

        float newVolume;

        // Incremento/decremento rápido
        if (channel.EndsWith("++"))
        {
            newVolume = Math.Min(1.0f, currentVolume + 0.25f);
        }
        else if (channel.EndsWith("--"))
        {
            newVolume = Math.Max(0.0f, currentVolume - 0.25f);
        }
        else if (channel.EndsWith("+"))
        {
            newVolume = Math.Min(1.0f, currentVolume + 0.10f);
        }
        else if (channel.EndsWith("-"))
        {
            newVolume = Math.Max(0.0f, currentVolume - 0.10f);
        }
        else if (parts.Length >= 2 && int.TryParse(parts[1], out int percent))
        {
            // Valor absoluto
            newVolume = Math.Clamp(percent / 100.0f, 0.0f, 1.0f);
        }
        else
        {
            _gameScreen.ShowError("Uso: vol [m|e|v|t] [0-100] o vol [m|e|v|t][+|-]");
            return;
        }

        // Aplicar volumen
        switch (channel.TrimEnd('+', '-'))
        {
            case "m":
            case "musica":
            case "music":
                _soundManager.MusicVolume = newVolume;
                break;
            case "e":
            case "efectos":
            case "effects":
            case "fx":
                _soundManager.EffectsVolume = newVolume;
                break;
            case "v":
            case "voz":
            case "voice":
                _soundManager.VoiceVolume = newVolume;
                break;
            case "t":
            case "maestro":
            case "master":
            case "total":
                _soundManager.MasterVolume = newVolume;
                break;
        }

        _soundManager.RefreshVolumes();

        var newPercent = (int)(newVolume * 100);
        _gameScreen.ShowMessage($"{channelName}: {newPercent}%");
    }

    private void ToggleSound()
    {
        if (_soundManager == null)
        {
            _gameScreen.ShowError("Sonido no disponible.");
            return;
        }

        _soundManager.SoundEnabled = !_soundManager.SoundEnabled;

        if (_soundManager.SoundEnabled)
        {
            _soundManager.RefreshVolumes();
            _gameScreen.ShowMessage("Sonido activado.");
        }
        else
        {
            _soundManager.StopMusic();
            _gameScreen.ShowMessage("Sonido desactivado.");
        }
    }

    private void ProcessSoundCommand(string args)
    {
        if (_soundManager == null)
        {
            _gameScreen.ShowError("Sonido no disponible.");
            return;
        }

        var arg = args.ToLowerInvariant();

        switch (arg)
        {
            case "on":
            case "si":
            case "activar":
            case "1":
                _soundManager.SoundEnabled = true;
                _soundManager.RefreshVolumes();
                _gameScreen.ShowMessage("Sonido activado.");
                break;

            case "off":
            case "no":
            case "desactivar":
            case "0":
                _soundManager.SoundEnabled = false;
                _soundManager.StopMusic();
                _gameScreen.ShowMessage("Sonido desactivado.");
                break;

            default:
                _gameScreen.ShowError("Uso: sonido [on|off]");
                break;
        }
    }

    private void ToggleAi()
    {
        if (_state == null)
        {
            _gameScreen.ShowError("Estado del juego no disponible.");
            return;
        }

        var llmStatus = _state.UseLlmForUnknownCommands ? "activada" : "desactivada";
        var ttsStatus = (_soundManager?.VoiceVolume ?? 0) > 0 ? "activada" : "desactivada";
        _gameScreen.ShowMessage($"IA: {llmStatus}, Voz TTS: {ttsStatus}. Usa 'ia on' o 'ia off' para cambiar.");
    }

    private async Task ProcessAiCommandAsync(string args)
    {
        if (_state == null)
        {
            _gameScreen.ShowError("Estado del juego no disponible.");
            return;
        }

        var arg = args.ToLowerInvariant();

        switch (arg)
        {
            case "on":
            case "si":
            case "activar":
            case "1":
                // Instalar/iniciar servicios de Docker si es necesario
                ConsoleRenderer.Clear();
                var aiReady = await DockerServiceConsole.EnsureAllAsync();

                if (!aiReady)
                {
                    _gameScreen.ShowError("No se pudieron iniciar los servicios de IA.");
                    _needsRedraw = true;
                    return;
                }

                _state.UseLlmForUnknownCommands = true;
                if (_soundManager != null)
                {
                    _soundManager.VoiceVolume = 1.0f;
                    _soundManager.RefreshVolumes();
                }

                Console.WriteLine();
                Console.WriteLine($"  {Colors.Green}IA activada correctamente.{Colors.Reset}");
                Console.WriteLine($"  {Colors.Gray}Comandos con IA y descripciones con voz.{Colors.Reset}");
                Console.WriteLine();
                Console.Write($"  {Colors.Dim}Pulsa Enter para continuar...{Colors.Reset}");
                Console.ReadLine();
                _needsRedraw = true;
                break;

            case "off":
            case "no":
            case "desactivar":
            case "0":
                _state.UseLlmForUnknownCommands = false;
                if (_soundManager != null)
                {
                    _soundManager.VoiceVolume = 0f;
                    _soundManager.StopVoice();
                    _soundManager.RefreshVolumes();
                }
                _gameScreen.ShowMessage("IA desactivada. Sin IA ni voz.");
                break;

            default:
                _gameScreen.ShowError("Uso: ia [on|off]");
                break;
        }
    }

    /// <summary>
    /// Intenta interpretar un comando desconocido usando el LLM.
    /// Retorna (comando, errorConexion) donde errorConexion indica si falló la conexión.
    /// </summary>
    private async Task<(string? Command, bool ConnectionError)> TryAskLlmForUnknownCommandAsync(string originalCommand)
    {
        try
        {
            var prompt = BuildLlmPrompt(originalCommand);

            var payload = new
            {
                model = "llama3.2:3b",
                prompt,
                stream = false,
                options = new
                {
                    num_ctx = 1024,
                    num_predict = 128
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await HttpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                var answer = responseProp.GetString();
                if (!string.IsNullOrWhiteSpace(answer))
                {
                    return (answer.Trim(), false);
                }
            }

            return (null, false);
        }
        catch (HttpRequestException ex)
        {
            // No hay conexión con Ollama - mostrar diagnóstico
            Console.WriteLine($"\n{Colors.Dim}[Debug] Ollama host: {OllamaHost}, URL: {HttpClient.BaseAddress}{Colors.Reset}");
            Console.WriteLine($"{Colors.Dim}[Debug] Error: {ex.Message}{Colors.Reset}");
            return (null, true);
        }
        catch (TaskCanceledException)
        {
            // Timeout
            Console.WriteLine($"\n{Colors.Dim}[Debug] Timeout conectando a Ollama en {HttpClient.BaseAddress}{Colors.Reset}");
            return (null, true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n{Colors.Dim}[Debug] Error inesperado: {ex.GetType().Name}: {ex.Message}{Colors.Reset}");
            return (null, true);
        }
    }

    /// <summary>
    /// Construye el prompt para el LLM.
    /// </summary>
    private string BuildLlmPrompt(string originalCommand)
    {
        var roomDescription = _engine!.DescribeCurrentRoom();
        var inventory = _engine.DescribeInventory();
        var doors = _engine.DescribeDoorsInCurrentRoom();

        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Eres un intérprete de comandos para un juego de aventuras de texto en español.");
        promptBuilder.AppendLine("El parser interno del juego no ha entendido el comando del jugador.");
        promptBuilder.AppendLine("Tu trabajo es interpretar lo que el jugador quiso decir y devolver un comando válido.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("VERBOS VÁLIDOS que el juego entiende:");
        promptBuilder.AppendLine("- examinar <objeto> (examina un objeto específico)");
        promptBuilder.AppendLine("- ir <dirección> (norte, sur, este, oeste, arriba, abajo)");
        promptBuilder.AppendLine("- coger <objeto>");
        promptBuilder.AppendLine("- soltar <objeto>");
        promptBuilder.AppendLine("- abrir puerta <dirección> (ej: abrir puerta norte)");
        promptBuilder.AppendLine("- cerrar puerta <dirección>");
        promptBuilder.AppendLine("- hablar <personaje>");
        promptBuilder.AppendLine("- usar <objeto>");
        promptBuilder.AppendLine("- dar <objeto> a <personaje>");
        promptBuilder.AppendLine("- meter <objeto> en <contenedor>");
        promptBuilder.AppendLine("- sacar <objeto> de <contenedor>");
        promptBuilder.AppendLine("- inventario");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("CONTEXTO DE LA SALA ACTUAL:");
        promptBuilder.AppendLine(roomDescription);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("PUERTAS EN ESTA SALA:");
        promptBuilder.AppendLine(doors);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("INVENTARIO DEL JUGADOR:");
        promptBuilder.AppendLine(inventory);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine($"COMANDO DEL JUGADOR: \"{originalCommand}\"");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("INSTRUCCIONES:");
        promptBuilder.AppendLine("1. Interpreta lo que el jugador quiso decir");
        promptBuilder.AppendLine("2. Responde SOLO con el comando válido que crees que quiso escribir");
        promptBuilder.AppendLine("3. Usa EXACTAMENTE los verbos de la lista de arriba");
        promptBuilder.AppendLine("4. El objeto debe existir en la sala o inventario del jugador");
        promptBuilder.AppendLine("5. Para puertas, usa 'abrir puerta <dirección>' o 'cerrar puerta <dirección>'");
        promptBuilder.AppendLine("6. NO añadas explicaciones, solo el comando");
        promptBuilder.AppendLine("7. Si no puedes interpretar el comando, responde: NO_ENTIENDO");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("Respuesta (solo el comando o NO_ENTIENDO):");
        return promptBuilder.ToString();
    }

    private void ShowQuests()
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder();
        ConsoleRenderer.DrawTitle("MISIONES");
        ConsoleRenderer.DrawSeparator();

        var activeQuests = _state!.Quests
            .Where(q => q.Value.Status == QuestStatus.InProgress)
            .Select(q => _world!.Quests.FirstOrDefault(d =>
                d.Id.Equals(q.Key, StringComparison.OrdinalIgnoreCase)))
            .Where(d => d != null)
            .ToList();

        var completedQuests = _state.Quests
            .Where(q => q.Value.Status == QuestStatus.Completed)
            .Select(q => _world!.Quests.FirstOrDefault(d =>
                d.Id.Equals(q.Key, StringComparison.OrdinalIgnoreCase)))
            .Where(d => d != null)
            .ToList();

        if (activeQuests.Any())
        {
            ConsoleRenderer.DrawLine($"{Colors.Yellow}Activas:{Colors.Reset}");
            foreach (var quest in activeQuests)
            {
                ConsoleRenderer.DrawLine($"  * {quest!.Name}");
                if (!string.IsNullOrEmpty(quest.Description))
                {
                    ConsoleRenderer.DrawWrappedText(quest.Description, color: Colors.Gray, prefix: "    ");
                }
            }
        }

        if (completedQuests.Any())
        {
            ConsoleRenderer.DrawEmptyLine();
            ConsoleRenderer.DrawLine($"{Colors.Green}Completadas:{Colors.Reset}");
            foreach (var quest in completedQuests)
            {
                ConsoleRenderer.DrawLine($"  * {quest!.Name}", color: Colors.Gray);
            }
        }

        if (!activeQuests.Any() && !completedQuests.Any())
        {
            ConsoleRenderer.DrawLine("No tienes misiones.", color: Colors.Gray);
        }

        ConsoleRenderer.DrawBottomBorder();
    }

    private void SaveGame()
    {
        try
        {
            var savesDir = GetSavesDirectory();
            Directory.CreateDirectory(savesDir);

            var savePath = Path.Combine(savesDir, "autosave.xas");
            SaveManager.SaveToPath(_state!, savePath, _world?.Game.EncryptionKey);

            _gameScreen.ShowMessage("Partida guardada correctamente.");
        }
        catch (Exception ex)
        {
            _gameScreen.ShowError($"Error al guardar: {ex.Message}");
        }
    }

    private void LoadGame()
    {
        try
        {
            var savesDir = GetSavesDirectory();
            var savePath = Path.Combine(savesDir, "autosave.xas");

            if (!File.Exists(savePath))
            {
                _gameScreen.ShowError("No hay partida guardada.");
                return;
            }

            var loadedState = SaveManager.LoadFromPath(savePath, _world!);

            // Recrear el engine con el estado cargado
            _state = loadedState;
            _engine = new GameEngine(_world!, _state, _soundManager!, isDebugMode: false);
            SubscribeToEngineEvents();

            _gameScreen.ShowMessage("Partida cargada correctamente.");
            _needsRedraw = true;
        }
        catch (Exception ex)
        {
            _gameScreen.ShowError($"Error al cargar: {ex.Message}");
        }
    }

    private string GetSavesDirectory()
    {
        // ~/.local/share/xiloadventures/world_id
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Si LocalApplicationData está vacío, usar home
        if (string.IsNullOrEmpty(baseDir))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            baseDir = Path.Combine(home, ".local", "share");
        }

        return Path.Combine(baseDir, "xiloadventures", _world?.Game.Id ?? "unknown");
    }

    private bool HasConsoleSizeChanged()
    {
        try
        {
            var currentWidth = Console.WindowWidth;
            var currentHeight = Console.WindowHeight;

            if (currentWidth != _lastConsoleWidth || currentHeight != _lastConsoleHeight)
            {
                _lastConsoleWidth = currentWidth;
                _lastConsoleHeight = currentHeight;
                return true;
            }
        }
        catch
        {
            // Ignorar errores al leer el tamaño de la consola
        }
        return false;
    }

    #region Event Handlers

    private void OnRoomChanged(Room room)
    {
        // Limpiar historial y redibujar con la nueva sala (como en WPF)
        if (_state != null && _world != null)
        {
            _gameScreen.ClearAndRedraw(_state, room, _world);
            _needsRedraw = false; // Ya se redibujó
        }
        else
        {
            _needsRedraw = true;
        }
    }

    private void OnScriptMessage(string message)
    {
        _gameScreen.ShowScriptMessage(message);
        _input.WaitForEnter();
        _needsRedraw = true;
    }

    private void OnCombatStarted(string npcId)
    {
        if (_state == null || _engine == null || _world == null) return;

        var npc = _state.Npcs.FirstOrDefault(n =>
            n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));

        if (npc == null) return;

        _isInCombat = true;

        var combatScreen = new CombatScreen(_engine, _state, _world, npc, _input);
        var result = combatScreen.Run();

        _isInCombat = false;
        _needsRedraw = true;

        // Mostrar resultado
        switch (result)
        {
            case CombatResult.Victory:
                _gameScreen.ShowMessage($"Has derrotado a {npc.Name}!");
                break;
            case CombatResult.Fled:
                _gameScreen.ShowMessage("Has huido del combate.");
                break;
        }
    }

    private void OnTradeOpened(Npc merchant)
    {
        if (_state == null || _world == null) return;

        _isInTrade = true;

        var tradeScreen = new TradeScreen(_state, _world, merchant, _input);
        tradeScreen.Run();

        _isInTrade = false;
        _needsRedraw = true;
        _engine?.CloseShop();
    }

    private void OnCraftOpened()
    {
        if (_state == null || _engine == null || _world == null) return;

        _isInCraft = true;

        var craftScreen = new CraftScreen(_state, _world, _engine.CurrentRoom?.Id ?? "", _input);
        craftScreen.Run();

        _isInCraft = false;
        _needsRedraw = true;
    }

    private void OnConversationDialogue(ConversationMessage message)
    {
        _isInConversation = true;

        _gameScreen.ShowDialogue(message.SpeakerName, message.Text);
    }

    private void OnConversationOptions(List<DialogueOption> options)
    {
        _currentDialogueOptions = options;
        _gameScreen.ShowDialogueOptions(options);
    }

    private void OnConversationEnded()
    {
        _isInConversation = false;
        _currentDialogueOptions = null;
        _needsRedraw = true;
    }

    private void OnPlayerDied(DeathType deathType)
    {
        var reason = deathType switch
        {
            DeathType.Health => "Has muerto por falta de salud.",
            DeathType.Hunger => "Has muerto de hambre.",
            DeathType.Thirst => "Has muerto de sed.",
            DeathType.Sleep => "Has muerto de agotamiento.",
            DeathType.Sanity => "Has perdido la cordura.",
            _ => "Has muerto."
        };

        _gameScreen.ShowGameOver(reason);
        _input.WaitForEnter();
        _isRunning = false;
    }

    private void OnAdventureCompleted()
    {
        var endingText = _world?.Game.EndingText ?? "Has completado la aventura!";
        _gameScreen.ShowVictory(endingText);
        _input.WaitForEnter();
        _isRunning = false;
    }

    private void OnHelpRequested()
    {
        ShowHelp();
        _input.WaitForEnter();
        _needsRedraw = true;
    }

    #endregion
}
