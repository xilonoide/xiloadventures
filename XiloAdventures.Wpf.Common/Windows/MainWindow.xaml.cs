using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Services;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class MainWindow : Window
{
    private WorldModel _world;
    private readonly GameEngine _engine;
    private readonly SoundManager _sound;
    private readonly UiSettings _uiSettings;
    private readonly bool _isRunningFromEditor;

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:11434/")
    };

    private readonly List<string> _commandHistory = new();
    private int _commandHistoryIndex = -1;
    private bool _isInitializingCheckbox;
    private bool _skipClosingConfirmation;
    private bool _gameEnded;
    private bool _isFirstLlmQuery = true;

    // Mapa de salas visitadas
    private readonly HashSet<string> _visitedRooms = new();
    private readonly Dictionary<string, (int X, int Y)> _roomCoordinates = new();
    private const int RoomWidth = 100;
    private const int RoomHeight = 60;
    private const int RoomSpacing = 20;
    private const double BaseWindowWidth = 1000;
    private const double MapPanelWidth = 600;

    // Zoom y pan del mapa
    private double _mapZoom = 1.0;
    private const double MinZoom = 0.3;
    private const double MaxZoom = 3.0;
    private const double ZoomStep = 0.1;
    private bool _isPanning;
    private Point _panStart;
    private double _panOffsetX;
    private double _panOffsetY;

    /// <summary>
    /// Carpeta de guardados según el contexto: editor usa SavesFolder, player usa PlayerSavesFolder.
    /// </summary>
    private string CurrentSavesFolder => _isRunningFromEditor
        ? AppPaths.SavesFolder
        : AppPaths.PlayerSavesFolder(_world.Game.Id);

    public MainWindow(WorldModel world, GameState state, SoundManager soundManager, UiSettings uiSettings, bool isRunningFromEditor = false)
    {
        _world = world;
        _sound = soundManager;
        _uiSettings = uiSettings;
        _isRunningFromEditor = isRunningFromEditor;

        // Asegurar que existe la carpeta de guardados
        if (!isRunningFromEditor)
        {
            AppPaths.EnsurePlayerSavesDirectory(world.Game.Id);
        }

        _engine = new GameEngine(world, state, _sound, _isRunningFromEditor);
        _engine.RoomChanged += Engine_RoomChanged;
        _engine.ScriptMessage += Engine_ScriptMessage;
        _engine.ConversationDialogue += Engine_ConversationDialogue;
        _engine.ConversationOptions += Engine_ConversationOptions;
        _engine.ConversationEnded += Engine_ConversationEnded;
        _engine.TradeOpened += Engine_TradeOpened;
        _engine.CraftOpened += Engine_CraftOpened;
        _engine.AdventureCompleted += Engine_AdventureCompleted;
        _engine.PlayerDied += Engine_PlayerDied;
        _engine.CombatStarted += Engine_CombatStarted;
        _engine.HelpRequested += Engine_HelpRequested;
        _engine.TriggerInitialScripts(); // Disparar scripts iniciales después de suscribir eventos

        InitializeComponent();

        // Establecer tamaño de ventana
        Height = SystemParameters.WorkArea.Height - 100;

        // Redibujar mapa cuando el canvas tenga su tamaño real
        MapCanvas.SizeChanged += (_, _) => { if (_uiSettings.MapEnabled) DrawMap(); };

        ApplyUiSettings();

        Loaded += MainWindow_Loaded;
        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    /// <summary>
    /// Recarga el mundo desde el editor, preservando la posición del jugador si es posible.
    /// </summary>
    public void ReloadWorld(WorldModel newWorld, GameState newState)
    {
        var currentRoomId = _engine.State.CurrentRoomId;
        var currentInventory = _engine.State.InventoryObjectIds.ToList();
        var currentPlayer = _engine.State.Player;

        // Actualizar el mundo
        _world = newWorld;

        // Intentar preservar posición del jugador si la sala existe
        if (newWorld.Rooms.Any(r => r.Id.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase)))
        {
            newState.CurrentRoomId = currentRoomId;
        }

        // Intentar preservar inventario (solo objetos que aún existen)
        var validInventory = currentInventory
            .Where(id => newWorld.Objects.Any(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        newState.InventoryObjectIds = validInventory;

        // Preservar estado del jugador (necesidades básicas, dinero, salud, etc.)
        newState.Player.DynamicStats.Hunger = currentPlayer.DynamicStats.Hunger;
        newState.Player.DynamicStats.Thirst = currentPlayer.DynamicStats.Thirst;
        newState.Player.DynamicStats.Sleep = currentPlayer.DynamicStats.Sleep;
        newState.Player.DynamicStats.Energy = currentPlayer.DynamicStats.Energy;
        newState.Player.DynamicStats.Health = currentPlayer.DynamicStats.Health;
        newState.Player.DynamicStats.Sanity = currentPlayer.DynamicStats.Sanity;
        newState.Player.DynamicStats.Mana = currentPlayer.DynamicStats.Mana;
        newState.Player.Money = currentPlayer.Money;

        // Preservar equipamiento (solo objetos que aún existen)
        if (currentPlayer.EquippedRightHandId != null &&
            newWorld.Objects.Any(o => o.Id.Equals(currentPlayer.EquippedRightHandId, StringComparison.OrdinalIgnoreCase)))
            newState.Player.EquippedRightHandId = currentPlayer.EquippedRightHandId;

        if (currentPlayer.EquippedLeftHandId != null &&
            newWorld.Objects.Any(o => o.Id.Equals(currentPlayer.EquippedLeftHandId, StringComparison.OrdinalIgnoreCase)))
            newState.Player.EquippedLeftHandId = currentPlayer.EquippedLeftHandId;

        if (currentPlayer.EquippedTorsoId != null &&
            newWorld.Objects.Any(o => o.Id.Equals(currentPlayer.EquippedTorsoId, StringComparison.OrdinalIgnoreCase)))
            newState.Player.EquippedTorsoId = currentPlayer.EquippedTorsoId;

        if (currentPlayer.EquippedHeadId != null &&
            newWorld.Objects.Any(o => o.Id.Equals(currentPlayer.EquippedHeadId, StringComparison.OrdinalIgnoreCase)))
            newState.Player.EquippedHeadId = currentPlayer.EquippedHeadId;

        // Recargar el motor
        _engine.LoadState(newState);

        // Mostrar mensaje de recarga
        AppendSystemMessage("⟳ Mundo recargado desde el editor.");

        // Actualizar visuales
        Title = $"Xilo Adventures - {_world.Game.Title}";
        UpdateStatusPanel();
        UpdateRoomVisuals();

        if (_uiSettings.MapEnabled)
        {
            _visitedRooms.Clear();
            _roomCoordinates.Clear();
            DrawMap();
        }
    }

    private System.Windows.Threading.DispatcherTimer? _systemMessageTimer;
    private System.Windows.Threading.DispatcherTimer? _npcMovementTimer;
    private Paragraph? _currentSystemMessage;
    private bool _lastRoomLitState = true;

    private void AppendSystemMessage(string text)
    {
        // Eliminar mensaje anterior si existe
        if (_currentSystemMessage != null && OutputTextBox.Document.Blocks.Contains(_currentSystemMessage))
        {
            OutputTextBox.Document.Blocks.Remove(_currentSystemMessage);
        }
        _systemMessageTimer?.Stop();

        var paragraph = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, 0, 0, 8),
            Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255))
        };
        OutputTextBox.Document.Blocks.Add(paragraph);
        OutputTextBox.ScrollToEnd();
        _currentSystemMessage = paragraph;

        // Configurar timer para eliminar el mensaje después de 2 segundos
        _systemMessageTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _systemMessageTimer.Tick += (_, _) =>
        {
            _systemMessageTimer.Stop();
            if (_currentSystemMessage != null && OutputTextBox.Document.Blocks.Contains(_currentSystemMessage))
            {
                OutputTextBox.Document.Blocks.Remove(_currentSystemMessage);
                _currentSystemMessage = null;
            }
        };
        _systemMessageTimer.Start();
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Title = $"Xilo Adventures - {_world.Game.Title}";
        InputTextBox.Focus();
        UpdateStatusPanel();
        UpdateRoomVisuals();

        // Leer descripción de la sala inicial con TTS (la voz fue suprimida durante el constructor)
        _engine.PlayCurrentRoomDescription();

        // Inicializar checkboxes sin disparar el evento
        _isInitializingCheckbox = true;
        UseLlmCheckBox.IsChecked = _uiSettings.UseLlmForUnknownCommands;
        MapEnabledCheckBox.IsChecked = _uiSettings.MapEnabled;
        _isInitializingCheckbox = false;

        // Inicializar timer para movimiento de NPCs basado en tiempo
        _npcMovementTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _npcMovementTimer.Tick += (_, _) =>
        {
            _engine.UpdateNpcTimedMovement();

            var currentLitState = _engine.IsCurrentRoomLit;

            // Actualizar si: sala iluminada, o el estado de iluminación cambió
            if (currentLitState || currentLitState != _lastRoomLitState)
            {
                UpdateRoomVisuals();
                _lastRoomLitState = currentLitState;
            }
        };
        _npcMovementTimer.Start();
    }

    private void ApplyUiSettings()
    {
        var size = _uiSettings.FontSize;
        var fontFamily = new System.Windows.Media.FontFamily(_uiSettings.FontFamily);

        OutputTextBox.FontSize = size;
        OutputTextBox.FontFamily = fontFamily;
        InputTextBox.FontSize = size;
        InputTextBox.FontFamily = fontFamily;
        RoomTitleText.FontSize = size + 2;
        RoomTitleText.FontFamily = fontFamily;
        RoomDescriptionText.FontSize = size;
        RoomDescriptionText.FontFamily = fontFamily;

        // Aplicar fuente a stats (solo familia, no tamaño)
        TextElement.SetFontFamily(StatsContentGrid, fontFamily);

        // Mostrar/ocultar mapa
        if (_uiSettings.MapEnabled)
        {
            MapPanel.Visibility = Visibility.Visible;
            MapColumn.Width = new GridLength(580);
            ExitsSection.Visibility = Visibility.Collapsed; // Las salidas se ven en el mapa

            // Layout de una columna para stats
            StatsColumn.Width = new GridLength(270);
            StatsScrollViewer.MaxWidth = double.PositiveInfinity;
            StatsScrollViewer.Margin = new Thickness(0, 0, 10, 0); // 10px margen derecho
            StatsContentCol1.Width = new GridLength(1, GridUnitType.Star);
            StatsContentCol2.Width = new GridLength(0);
            StatsRightColumn.Visibility = Visibility.Collapsed;
            EquipmentInventoryLeft.Visibility = Visibility.Visible;

            UpdateArrows();
            DrawMap();
        }
        else
        {
            MapPanel.Visibility = Visibility.Collapsed;
            MapColumn.Width = new GridLength(0);
            MapColumn.MinWidth = 0; // Quitar MinWidth para que sea realmente 0
            ExitsSection.Visibility = Visibility.Visible; // Salidas en columna izquierda

            // Stats fijo en 450px, texto (columna 0) toma el resto
            StatsColumn.Width = new GridLength(450);
            StatsScrollViewer.ClearValue(MaxWidthProperty);
            StatsScrollViewer.Margin = new Thickness(0); // Sin margen
            StatsContentCol1.Width = new GridLength(220);
            StatsContentCol2.Width = new GridLength(220);
            StatsRightColumn.Visibility = Visibility.Visible;
            EquipmentInventoryLeft.Visibility = Visibility.Collapsed;
        }
    }

    private void AppendText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var paragraph = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, 0, 0, 8)
        };
        OutputTextBox.Document.Blocks.Add(paragraph);
        OutputTextBox.ScrollToEnd();
    }

    private void AppendSeparator()
    {
        // Solo añadir separador si ya hay contenido
        if (OutputTextBox.Document.Blocks.Count == 0)
            return;

        // Salto de línea antes de la línea separadora
        var emptyParagraph = new Paragraph { Margin = new Thickness(0, 0, 0, 0) };
        OutputTextBox.Document.Blocks.Add(emptyParagraph);

        var line = new System.Windows.Shapes.Rectangle
        {
            Height = 1,
            Fill = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var container = new BlockUIContainer(line)
        {
            Margin = new Thickness(0, 4, 0, 12)
        };

        OutputTextBox.Document.Blocks.Add(container);
    }


    private async void InputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        // Historial de comandos con flechas arriba/abajo
        if (e.Key == Key.Enter)
        {
            var cmd = InputTextBox.Text.Trim();
            if (string.IsNullOrEmpty(cmd))
            {
                e.Handled = true;
                return;
            }

            // Comando de limpiar la salida a nivel de UI
            var lower = cmd.ToLowerInvariant();
            if (lower is "limpiar" or "cls" or "clear")
            {
                OutputTextBox.Document.Blocks.Clear();
                InputTextBox.Clear();
                e.Handled = true;
                return;
            }

            AppendText($"> {cmd}");
            AppendSeparator();
            InputTextBox.Clear();

            // Guardar en historial
            _commandHistory.Add(cmd);
            _commandHistoryIndex = _commandHistory.Count;

            // Enviar al motor
            var result = _engine.ProcessCommand(cmd);

            // Si el comando requiere limpiar la pantalla antes, hacerlo ahora
            if (result.ClearScreenBefore)
            {
                OutputTextBox.Document.Blocks.Clear();
            }

            // Si hubo error y la IA está activada, consultar a la IA
            if (_uiSettings.UseLlmForUnknownCommands && result.HasError)
            {
                ShowLlmProgress(cmd);
                try
                {
                    var llmCommand = await TryAskLlmForUnknownCommandAsync(cmd);

                    if (!string.IsNullOrWhiteSpace(llmCommand) &&
                        !llmCommand.Equals("NO_ENTIENDO", StringComparison.OrdinalIgnoreCase) &&
                        !llmCommand.Contains("NO_ENTIENDO"))
                    {
                        // La IA sugirió un comando válido, ejecutarlo
                        var llmResult = _engine.ProcessCommand(llmCommand);

                        if (!llmResult.HasError)
                        {
                            // Si el comando reinterpretado requiere limpiar la pantalla, hacerlo
                            if (llmResult.ClearScreenBefore)
                            {
                                OutputTextBox.Document.Blocks.Clear();
                            }

                            // El comando interpretado funcionó
                            AppendText($"(Interpretado como: {llmCommand})");
                            AppendText(llmResult.Message);
                            result = llmResult; // Usar este resultado para el resto del flujo
                        }
                        else
                        {
                            // El comando sugerido tampoco funcionó
                            AppendText(result.Message);
                        }
                    }
                    else
                    {
                        // La IA no pudo interpretar el comando
                        AppendText(result.Message);
                    }
                }
                catch
                {
                    // Error con la IA, mostrar el mensaje original
                    AppendText(result.Message);
                }
                finally
                {
                    HideLlmProgress();
                }
            }
            else if (!string.IsNullOrWhiteSpace(result.Message))
            {
                AppendText(result.Message);
            }

            UpdateStatusPanel();
            UpdateRoomVisuals();

            // Actualizar flechas por si se abrió/cerró una puerta
            if (_uiSettings.MapEnabled)
            {
                UpdateArrows();
            }

            // Autoguardar (en player exportado, incluir configuración)
            if (!_isRunningFromEditor)
            {
                SaveSettingsToGameState();
            }
            try
            {
                SaveManager.AutoSave(_engine.State, CurrentSavesFolder, _world.Game.EncryptionKey);
            }
            catch
            {
                // Ignoramos errores de autosave
            }

            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            if (_commandHistory.Count == 0)
            {
                e.Handled = true;
                return;
            }

            _commandHistoryIndex--;
            if (_commandHistoryIndex < 0)
                _commandHistoryIndex = 0;

            InputTextBox.Text = _commandHistory[_commandHistoryIndex];
            InputTextBox.CaretIndex = InputTextBox.Text.Length;
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            if (_commandHistory.Count == 0)
            {
                e.Handled = true;
                return;
            }

            _commandHistoryIndex++;
            if (_commandHistoryIndex >= _commandHistory.Count)
            {
                _commandHistoryIndex = _commandHistory.Count;
                InputTextBox.Clear();
            }
            else
            {
                InputTextBox.Text = _commandHistory[_commandHistoryIndex];
                InputTextBox.CaretIndex = InputTextBox.Text.Length;
            }

            e.Handled = true;
        }
        else if (e.Key == Key.PageUp || e.Key == Key.PageDown)
        {
            // Reenviamos PageUp/PageDown al RichTextBox para permitir scroll desde el input
            OutputTextBox.Focus();
            var routed = new KeyEventArgs(e.KeyboardDevice, e.InputSource, e.Timestamp, e.Key)
            {
                RoutedEvent = Keyboard.KeyDownEvent
            };
            InputManager.Current.ProcessInput(routed);
            e.Handled = true;
        }
    }



    private async System.Threading.Tasks.Task<string?> TryAskLlmForUnknownCommandAsync(string originalCommand)
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
                    num_ctx = 1024,   // Contexto pequeño: prompt ~750 tokens + respuesta ~30 tokens
                    num_predict = 128 // Respuesta corta con margen para comandos más complejos
                }
            };

            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var doc = await JsonDocument.ParseAsync(stream);

            if (doc.RootElement.TryGetProperty("response", out var responseProp))
            {
                var answer = responseProp.GetString();
                if (!string.IsNullOrWhiteSpace(answer))
                {
                    // Devolvemos la respuesta del modelo como un párrafo aparte.
                    return answer.Trim();
                }
            }

            return "Ni la IA te entiende...";
        }
        catch (HttpRequestException)
        {
            HandleLlmConnectionError();
            return null;
        }
        catch (Exception ex)
        {
            new AlertWindow($"Se produjo un error al consultar el modelo IA:\n{ex.Message}", "Error IA")
            {
                Owner = this
            }.ShowDialog();
            return null;
        }
    }

    private string BuildLlmPrompt(string originalCommand)
    {
        // Le damos algo de contexto al modelo, pero mantenemos todo muy ligero.
        var roomDescription = _engine.DescribeCurrentRoom();
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

    private void HandleLlmConnectionError()
    {
        var composePath = System.IO.Path.Combine(AppContext.BaseDirectory, "docker-compose.yml");
        bool composeStarted = false;

        if (System.IO.File.Exists(composePath))
        {
            composeStarted = TryStartDockerCompose(composePath);
        }

        string message;
        if (composeStarted)
        {
            message =
                "No se ha podido contactar con el modelo IA en http://localhost:11434.\n\n" +
                "Asegúrate de que Docker Desktop está instalado y ejecutándose y vuelve a probar el comando.";
        }
        else
        {
            message =
                "No se ha podido contactar con el modelo IA en http://localhost:11434.\n\n" +
                "Debes tener Docker Desktop instalado y en ejecución." +
                "para poder usar esta opción.";
        }

        new AlertWindow(message, "IA no disponible")
        {
            Owner = this
        }.ShowDialog();
    }

    private bool TryStartDockerCompose(string composeFilePath)
    {
        try
        {
            var candidates = new (string FileName, string Arguments)[]
            {
                ("docker", $"compose -f \"{composeFilePath}\" up -d"),
                ("docker-compose", $"-f \"{composeFilePath}\" up -d")
            };

            foreach (var candidate in candidates)
            {
                var psi = new ProcessStartInfo
                {
                    FileName = candidate.FileName,
                    Arguments = candidate.Arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null)
                    continue;

                if (!process.WaitForExit(10000))
                {
                    try { process.Kill(); } catch { }
                    continue;
                }

                if (process.ExitCode == 0)
                    return true;
            }
        }
        catch
        {
            // Ignoramos errores aquí; el método devolverá false.
        }

        return false;
    }

    private void ShowLlmProgress(string command)
    {
        LastCommandText.Text = $"> {command}";
        LastCommandText.Visibility = Visibility.Visible;
        LlmProgressBar.Visibility = Visibility.Visible;
        LlmStatusText.Text = _isFirstLlmQuery
            ? "(Consultando a la IA - La primera consulta es más lenta)"
            : "(Consultando a la IA)";
        LlmStatusText.Visibility = Visibility.Visible;
        _isFirstLlmQuery = false;
    }

    private void HideLlmProgress()
    {
        LlmProgressBar.Visibility = Visibility.Collapsed;
        LastCommandText.Visibility = Visibility.Collapsed;
        LastCommandText.Text = string.Empty;
        LlmStatusText.Visibility = Visibility.Collapsed;
    }

    private void UpdateStatusPanel()
    {
        StatsLabel.Text = _engine.DescribePlayerStats();
        MoneyLabel.Text = _engine.DescribePlayerMoney();

        // Actualizar ambas columnas (izquierda y derecha para layout expandido)
        var equipment = _engine.DescribeEquipmentSummary();
        var inventory = _engine.DescribeInventory();
        var exits = _engine.DescribeExits();

        EquipmentLabel.Text = equipment;
        InventoryLabel.Text = inventory;
        ExitsLabel.Text = exits;

        EquipmentLabelRight.Text = equipment;
        InventoryLabelRight.Text = inventory;

        // Actualizar turno en la parte superior
        TurnText.Text = $"Turno: {_engine.State.TurnCounter}";

        try
        {
            var gameTime = _engine.State.GameTime;
            var tod = gameTime.TimeOfDay;
            string periodo = (tod.Hours >= 21 || tod.Hours < 7) ? "Noche" : "Día";
            var weather = _engine.State.Weather.ToString().ToLowerInvariant();
            TimeLabel.Text = $"{gameTime:HH:mm} ({periodo}, {weather})";
        }
        catch
        {
            TimeLabel.Text = string.Empty;
        }

        // Actualizar estados de combate
        if (_world.Game.CombatEnabled)
        {
            CombatPanel.Visibility = Visibility.Visible;
            var stats = _engine.State.Player.DynamicStats;
            HealthBar.Value = stats.Health;
            HealthBar.Maximum = stats.MaxHealth;
            EnergyBar.Value = stats.Energy;
            SanityBar.Value = stats.Sanity;

            // Mana solo visible si MagicEnabled
            if (_world.Game.MagicEnabled)
            {
                ManaPanel.Visibility = Visibility.Visible;
                ManaBar.Value = stats.Mana;
                ManaBar.Maximum = stats.MaxMana;
            }
            else
            {
                ManaPanel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            CombatPanel.Visibility = Visibility.Collapsed;
        }

        // Actualizar necesidades básicas
        if (_world.Game.BasicNeedsEnabled)
        {
            BasicNeedsPanel.Visibility = Visibility.Visible;
            var stats = _engine.State.Player.DynamicStats;
            HungerBar.Value = stats.Hunger;
            ThirstBar.Value = stats.Thirst;
            SleepBar.Value = stats.Sleep;
        }
        else
        {
            BasicNeedsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void Engine_RoomChanged(Room obj)
    {
        // Limpiar el área de texto al entrar en una nueva sala
        OutputTextBox.Document.Blocks.Clear();

        UpdateRoomVisuals();
        TrackVisitedRoom(obj);
        if (_uiSettings.MapEnabled)
        {
            UpdateArrows();
            DrawMap();
        }
    }

    private void TrackVisitedRoom(Room room)
    {
        if (room == null) return;

        if (!_visitedRooms.Contains(room.Id))
        {
            _visitedRooms.Add(room.Id);

            // Asignar coordenadas si no tiene
            if (!_roomCoordinates.ContainsKey(room.Id))
            {
                // Si es la primera sala, ponerla en el centro
                if (_roomCoordinates.Count == 0)
                {
                    _roomCoordinates[room.Id] = (0, 0);
                }
            }
        }
    }

    private void Engine_ScriptMessage(string message)
    {
        // Asegurarse de ejecutar en el hilo de UI
        Dispatcher.Invoke(() =>
        {
            AppendText(message);
        });
    }

    private void Engine_ConversationDialogue(ConversationMessage message)
    {
        Dispatcher.Invoke(() =>
        {
            // Formato: [NPC (emoción)] "Texto del diálogo"
            var emotionStr = message.Emotion != "Neutral" ? $" ({message.Emotion})" : "";
            var speaker = message.IsNpc ? message.SpeakerName : "Tú";
            var formattedText = $"[{speaker}{emotionStr}]: \"{message.Text}\"";
            AppendText(formattedText);
        });
    }

    private void Engine_ConversationOptions(List<DialogueOption> options)
    {
        Dispatcher.Invoke(() =>
        {
            if (options == null || options.Count == 0)
                return;

            var sb = new StringBuilder();
            sb.AppendLine("\n¿Qué dices?");
            foreach (var option in options)
            {
                var prefix = option.IsEnabled ? $"  [{option.Index + 1}]" : $"  (×)";
                var suffix = !option.IsEnabled && !string.IsNullOrEmpty(option.DisabledReason)
                    ? $" - {option.DisabledReason}"
                    : "";
                sb.AppendLine($"{prefix} {option.Text}{suffix}");
            }
            sb.AppendLine("\n(Escribe el número de tu elección o 'salir' para terminar)");
            AppendText(sb.ToString());
        });
    }

    private void Engine_ConversationEnded()
    {
        Dispatcher.Invoke(() =>
        {
            AppendText("\n[Fin de la conversación]\n");
        });
    }

    private void Engine_TradeOpened(Npc merchant)
    {
        Dispatcher.Invoke(() =>
        {
            // Crear el motor de comercio
            var tradeEngine = new TradeEngine(_engine.State);

            // Crear y mostrar la ventana de comercio
            var tradeWindow = new TradeWindow(tradeEngine, _engine.State, merchant)
            {
                Owner = this
            };

            // Disparar evento de inicio de comercio
            _engine.TriggerTradeEvent(merchant.Id, "Event_OnTradeStart");

            tradeWindow.ItemBought += objectId =>
            {
                _engine.TriggerTradeEvent(merchant.Id, "Event_OnItemBought");
            };

            tradeWindow.ItemSold += objectId =>
            {
                _engine.TriggerTradeEvent(merchant.Id, "Event_OnItemSold");
            };

            tradeWindow.TradeClosed += () =>
            {
                // Usar BeginInvoke para asegurar que la actualizacion ocurra despues de cerrar el dialogo
                Dispatcher.BeginInvoke(() =>
                {
                    // Disparar evento de fin de comercio
                    _engine.TriggerTradeEvent(merchant.Id, "Event_OnTradeEnd");

                    // Notificar al ConversationEngine que la tienda cerro
                    _engine.CloseShop();

                    // Actualizar UI principal con cambios de comercio
                    UpdateStatusPanel();
                });
            };

            tradeWindow.ShowDialog();
        });
    }

    private void Engine_CraftOpened()
    {
        Dispatcher.Invoke(() =>
        {
            // Crear el motor de fabricacion
            var craftEngine = new CraftEngine(_engine.State);

            // Crear y mostrar la ventana de fabricacion
            var craftWindow = new CraftWindow(craftEngine, _engine.State, _engine.State.CurrentRoomId)
            {
                Owner = this
            };

            craftWindow.CraftClosed += () =>
            {
                // Usar BeginInvoke para asegurar que la actualizacion ocurra despues de cerrar el dialogo
                Dispatcher.BeginInvoke(() =>
                {
                    // Actualizar UI principal con cambios de fabricacion
                    UpdateStatusPanel();
                    UpdateRoomVisuals();
                });
            };

            craftWindow.ShowDialog();
        });
    }

    private void Engine_CombatStarted(string npcId)
    {
        Dispatcher.Invoke(() =>
        {
            // Buscar el NPC enemigo
            var enemy = _engine.State.Npcs.FirstOrDefault(n =>
                n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));

            if (enemy == null)
            {
                AppendSystemMessage("Error: No se encontró el enemigo para el combate.");
                return;
            }

            // Obtener el inventario del jugador para el combate
            var playerInventory = _engine.State.InventoryObjectIds
                .Select(id => _engine.State.Objects.FirstOrDefault(o => o.Id == id))
                .Where(o => o != null)
                .Cast<GameObject>()
                .ToList();

            // Crear el motor de combate
            var combatEngine = new CombatEngine(_engine.State);

            // Crear y mostrar la ventana de combate
            var combatWindow = new CombatWindow(
                combatEngine, _engine.State, enemy, playerInventory, _world.Game.MagicEnabled)
            {
                Owner = this
            };

            combatWindow.CombatEnded += reason =>
            {
                // Usar BeginInvoke para asegurar que la actualización ocurra después de cerrar el diálogo
                Dispatcher.BeginInvoke(() =>
                {
                    // Actualizar la UI después del combate
                    UpdateStatusPanel();
                    UpdateRoomVisuals();

                    // Disparar eventos de script de combate
                    _engine.TriggerCombatEndEvent(enemy.Id, reason);

                    switch (reason)
                    {
                        case CombatEndReason.Victory:
                            AppendText($"\n¡Has derrotado a {enemy.Name}!");
                            break;
                        case CombatEndReason.Defeat:
                            AppendText($"\n{enemy.Name} te ha derrotado...");
                            break;
                        case CombatEndReason.Fled:
                            AppendText($"\nHas huido del combate con {enemy.Name}.");
                            break;
                    }
                });
            };

            combatWindow.ShowDialog();
        });
    }

    private void Engine_HelpRequested()
    {
        Dispatcher.Invoke(() =>
        {
            var helpWindow = new HelpWindow
            {
                Owner = this
            };
            helpWindow.ShowDialog();
        });
    }

    private void Engine_AdventureCompleted()
    {
        Dispatcher.Invoke(() =>
        {
            // Evitar mostrar el EndingWindow más de una vez
            if (_gameEnded) return;
            _gameEnded = true;

            // Buscar la música de finalización en la biblioteca
            string? endingMusicBase64 = null;
            if (!string.IsNullOrEmpty(_world.Game.EndingMusicId))
            {
                var musicAsset = _world.Musics.FirstOrDefault(m =>
                    m.Id.Equals(_world.Game.EndingMusicId, StringComparison.OrdinalIgnoreCase));
                endingMusicBase64 = musicAsset?.Base64;
            }

            var endingWindow = new EndingWindow
            {
                EndingText = _world.Game.EndingText,
                LogoBase64 = null,
                MusicBase64 = endingMusicBase64,
                CloseApplicationOnExit = false // No cerrar la app desde aquí
            };

            _sound.StopMusic();
            endingWindow.ShowDialog();

            // Cerrar MainWindow si no estamos en modo editor
            if (!_isRunningFromEditor)
            {
                _skipClosingConfirmation = true;
                Close();
            }
            else
            {
                _gameEnded = false; // Permitir otro ciclo de juego en el editor
            }
        });
    }

    private void Engine_PlayerDied(DeathType deathType)
    {
        Dispatcher.Invoke(() =>
        {
            // Evitar mostrar el EndingWindow más de una vez
            if (_gameEnded) return;
            _gameEnded = true;

            var deathText = deathType switch
            {
                DeathType.Hunger => RandomMessages.HungerDeath,
                DeathType.Thirst => RandomMessages.ThirstDeath,
                DeathType.Sleep => RandomMessages.SleepDeath,
                DeathType.Health => RandomMessages.HealthDeath,
                DeathType.Sanity => RandomMessages.SanityDeath,
                _ => "Has muerto."
            };

            var endingWindow = new EndingWindow
            {
                EndingText = deathText,
                LogoBase64 = null,
                MusicBase64 = null,
                CloseApplicationOnExit = false // No cerrar la app desde aquí, lo haremos después
            };

            _sound.StopMusic();
            endingWindow.ShowDialog();

            // Si se ejecuta desde el editor, reiniciar el juego
            if (_isRunningFromEditor)
            {
                _gameEnded = false; // Permitir otro ciclo de juego
                var newState = WorldLoader.CreateInitialState(_world);
                _engine.LoadState(newState);
                UpdateStatusPanel();
                UpdateRoomVisuals();
                AppendSystemMessage("⟳ Has muerto. Reiniciando partida...");
            }
            else
            {
                // Cerrar MainWindow, lo que devolverá control a StartupWindow
                _skipClosingConfirmation = true;
                Close();
            }
        });
    }

    private void UpdateRoomVisuals()
    {
        var room = _engine.CurrentRoom;
        if (room == null)
            return;

        RoomTitleText.Text = room.Name;
        RoomDescriptionText.Text = _engine.DescribeCurrentRoom();

        // Solo mostrar la imagen si la sala está iluminada
        if (_engine.IsCurrentRoomLit)
        {
            RoomImage.Source = TryLoadRoomImage(room.ImageBase64) ?? DefaultRoomImage;
        }
        else
        {
            RoomImage.Source = DarkRoomImage ?? DefaultRoomImage;
        }
    }

    private static readonly System.Windows.Media.Imaging.BitmapSource? DarkRoomImage = CreateDarkRoomImage();

    private static System.Windows.Media.Imaging.BitmapSource? CreateDarkRoomImage()
    {
        try
        {
            // Crear una imagen negra de 256x256 píxeles
            const int width = 256;
            const int height = 256;
            var pixels = new byte[width * height * 4]; // BGRA format, all zeros = black
            var bmp = System.Windows.Media.Imaging.BitmapSource.Create(
                width, height, 96, 96,
                System.Windows.Media.PixelFormats.Bgra32, null,
                pixels, width * 4);
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static readonly System.Windows.Media.Imaging.BitmapImage? DefaultRoomImage = LoadDefaultRoomImage();

    private static System.Windows.Media.Imaging.BitmapImage? LoadDefaultRoomImage()
    {
        try
        {
            var uri = new Uri("pack://application:,,,/XiloAdventures.Wpf.Common;component/Assets/default_room.png", UriKind.Absolute);
            var bmp = new System.Windows.Media.Imaging.BitmapImage(uri);
            bmp.Freeze();
            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private static System.Windows.Media.Imaging.BitmapImage? TryLoadRoomImage(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        try
        {
            var bytes = Convert.FromBase64String(base64);
            using var ms = new MemoryStream(bytes);

            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = ms;
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();

            return bmp;
        }
        catch
        {
            return null;
        }
    }

    private void SaveMenu_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog
        {
            Title = "Guardar partida",
            Filter = "Partidas guardadas (*.xas)|*.xas|Todos los archivos (*.*)|*.*",
            InitialDirectory = CurrentSavesFolder,
            FileName = $"{_engine.State.WorldId}_partida.xas"
        };

        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                // En el player exportado, asegurar que la configuración está en el GameState
                if (!_isRunningFromEditor)
                {
                    SaveSettingsToGameState();
                }
                SaveManager.SaveToPath(_engine.State, dlg.FileName, _world.Game.EncryptionKey);
            }
            catch (Exception ex)
            {
                new AlertWindow($"Error al guardar partida:\n{ex.Message}", "Error") { Owner = this }.ShowDialog();
            }
        }
    }

    private async void LoadMenu_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Cargar partida",
            Filter = "Partidas guardadas (*.xas)|*.xas|Todos los archivos (*.*)|*.*",
            InitialDirectory = CurrentSavesFolder
        };

        if (dlg.ShowDialog(this) == true)
        {
            try
            {
                var newState = SaveManager.LoadFromPath(dlg.FileName, _world);

                // Validar que la partida pertenece al mundo actual
                if (!string.Equals(newState.WorldId, _world.Game.Id, StringComparison.OrdinalIgnoreCase))
                {
                    new AlertWindow(
                        $"Esta partida pertenece a otro mundo ('{newState.WorldId}').\n\n" +
                        $"No es compatible con el mundo actual ('{_world.Game.Id}').",
                        "Partida incompatible")
                    {
                        Owner = this
                    }.ShowDialog();
                    return;
                }

                // Crear nueva configuración desde el estado cargado (solo para player exportado)
                UiSettings newUiSettings;
                if (_isRunningFromEditor)
                {
                    newUiSettings = _uiSettings;
                }
                else
                {
                    newUiSettings = new UiSettings
                    {
                        SoundEnabled = newState.SoundEnabled,
                        FontSize = newState.FontSize,
                        FontFamily = newState.FontFamily,
                        MusicVolume = newState.MusicVolume,
                        EffectsVolume = newState.EffectsVolume,
                        MasterVolume = newState.MasterVolume,
                        VoiceVolume = newState.VoiceVolume,
                        MapEnabled = newState.MapEnabled,
                        UseLlmForUnknownCommands = newState.UseLlmForUnknownCommands
                    };

                    // Aplicar configuración de sonido al SoundManager
                    _sound.SoundEnabled = newUiSettings.SoundEnabled;
                    _sound.MusicVolume = (float)(newUiSettings.MusicVolume / 10.0);
                    _sound.EffectsVolume = (float)(newUiSettings.EffectsVolume / 10.0);
                    _sound.MasterVolume = (float)(newUiSettings.MasterVolume / 10.0);
                    _sound.VoiceVolume = (float)(newUiSettings.VoiceVolume / 10.0);
                    _sound.RefreshVolumes();

                    // Si la IA estaba activada, intentar iniciar Docker antes de crear la ventana
                    if (newUiSettings.UseLlmForUnknownCommands)
                    {
                        var progressWindow = new DockerProgressWindow
                        {
                            Owner = this,
                            OllamaModel = "llama3.2:3b"
                        };

                        var result = await progressWindow.RunAsync().ConfigureAwait(true);

                        if (!result.Success || result.Canceled)
                        {
                            newUiSettings.UseLlmForUnknownCommands = false;
                            newState.UseLlmForUnknownCommands = false;

                            if (!result.Canceled)
                            {
                                new AlertWindow(
                                    "No se han podido iniciar los servicios de IA y voz. La IA ha sido desactivada.",
                                    "Error")
                                {
                                    Owner = this
                                }.ShowDialog();
                            }
                        }
                    }
                }

                // Suprimir voz al cargar partida para no leer la descripción de la sala
                _sound.SuppressVoicePlayback = true;
                var newWindow = new MainWindow(_world, newState, _sound, newUiSettings, _isRunningFromEditor);
                _sound.SuppressVoicePlayback = false;

                newWindow.Owner = Owner;
                Close();
                newWindow.Show();
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                new AlertWindow("Clave incorrecta o archivo corrupto.", "Error") { Owner = this }.ShowDialog();
            }
            catch (System.Text.Json.JsonException)
            {
                new AlertWindow("El archivo de partida está corrupto o no es válido.", "Error") { Owner = this }.ShowDialog();
            }
            catch (Exception ex)
            {
                new AlertWindow($"Error al cargar partida:\n\n{ex.Message}", "Error") { Owner = this }.ShowDialog();
            }
        }
    }

    private void OptionsMenu_Click(object sender, RoutedEventArgs e)
    {
        // Guardar en archivo .xac solo si estamos en el editor
        var dlg = new OptionsWindow(_uiSettings, OnOptionsChanged, _world.Game.Id, _world.Game.DefaultFontFamily, saveToFile: _isRunningFromEditor);
        dlg.Owner = this;
        dlg.ShowDialog();
    }


    private void OnOptionsChanged(UiSettings settings)
    {
        // Aplicar cambios en vivo
        var wasSoundEnabled = _sound.SoundEnabled;

        _uiSettings.SoundEnabled = settings.SoundEnabled;
        _uiSettings.FontSize = settings.FontSize;
        _uiSettings.FontFamily = settings.FontFamily;
        _uiSettings.UseLlmForUnknownCommands = settings.UseLlmForUnknownCommands;
        _uiSettings.MusicVolume = settings.MusicVolume;
        _uiSettings.EffectsVolume = settings.EffectsVolume;
        _uiSettings.MasterVolume = settings.MasterVolume;
        _uiSettings.VoiceVolume = settings.VoiceVolume;
        _uiSettings.MapEnabled = settings.MapEnabled;

        _sound.SoundEnabled = settings.SoundEnabled;
        _sound.MusicVolume = (float)(settings.MusicVolume / 10.0);
        _sound.EffectsVolume = (float)(settings.EffectsVolume / 10.0);
        _sound.MasterVolume = (float)(settings.MasterVolume / 10.0);
        _sound.VoiceVolume = (float)(settings.VoiceVolume / 10.0);

        _sound.RefreshVolumes();
        ApplyUiSettings();

        // Guardar configuración según el contexto
        if (_isRunningFromEditor)
        {
            UiSettingsManager.SaveForWorld(_engine.State.WorldId, _uiSettings);
        }
        else
        {
            // En el player exportado, guardar en el GameState
            SaveSettingsToGameState();
            try
            {
                SaveManager.AutoSave(_engine.State, CurrentSavesFolder, _world.Game.EncryptionKey);
            }
            catch
            {
                // Ignoramos errores de autosave
            }
        }

        // Si el sonido se acaba de activar, arrancar la música adecuada (mundo o sala actual).
        if (!wasSoundEnabled && _sound.SoundEnabled)
        {
            var room = _engine.CurrentRoom;
            if (room != null)
            {
                var hasRoomMusic = !string.IsNullOrWhiteSpace(room.MusicId);

                if (hasRoomMusic)
                {
                    // Sala con música especial: buscamos en la biblioteca
                    var musicAsset = _world.Musics.FirstOrDefault(m =>
                        m.Id.Equals(room.MusicId, StringComparison.OrdinalIgnoreCase));
                    _sound.PlayRoomMusic(room.MusicId, musicAsset?.Base64, null, null);
                }
                else if (_world.Game != null && !string.IsNullOrWhiteSpace(_world.Game.WorldMusicId))
                {
                    // Sala sin música especial: reproducimos la música del mundo desde la biblioteca
                    var worldMusicAsset = _world.Musics.FirstOrDefault(m =>
                        m.Id.Equals(_world.Game.WorldMusicId, StringComparison.OrdinalIgnoreCase));
                    _sound.PlayWorldMusic(_engine.WorldMusicId, worldMusicAsset?.Base64);
                }
            }
            else if (_world.Game != null && !string.IsNullOrWhiteSpace(_world.Game.WorldMusicId))
            {
                // Si por lo que sea no hay sala actual, intentamos arrancar la música de mundo
                var worldMusicAsset = _world.Musics.FirstOrDefault(m =>
                    m.Id.Equals(_world.Game.WorldMusicId, StringComparison.OrdinalIgnoreCase));
                _sound.PlayWorldMusic(_engine.WorldMusicId, worldMusicAsset?.Base64);
            }
        }
    }

    private void RestartMenu_Click(object sender, RoutedEventArgs e)
    {
        var confirmDlg = new ConfirmWindow(
            "¿Seguro que quieres reiniciar la partida?\n\nPerderás todo el progreso no guardado.",
            "Reiniciar partida")
        {
            Owner = this
        };

        if (confirmDlg.ShowDialog() != true)
            return;

        // Crear un nuevo estado inicial
        var newState = WorldLoader.CreateInitialState(_world);

        // Crear nueva ventana con el estado fresco
        var newWindow = new MainWindow(_world, newState, _sound, _uiSettings, _isRunningFromEditor)
        {
            Owner = Owner
        };

        // Cerrar sin preguntar (ya confirmamos)
        _skipClosingConfirmation = true;
        Close();

        // Mostrar ventana de introducción si hay texto de intro configurado
        if (!string.IsNullOrWhiteSpace(_world.Game.IntroText))
        {
            var introWindow = new IntroWindow
            {
                IntroText = _world.Game.IntroText,
                LogoBase64 = null
            };
            introWindow.ShowDialog();
        }

        newWindow.Show();
    }

    private void AboutMenu_Click(object sender, RoutedEventArgs e)
    {
        var about = new AboutWindow
        {
            Owner = this
        };
        about.ShowDialog();
    }


    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Detener timer de movimiento de NPCs
        _npcMovementTimer?.Stop();

        // Si es un reinicio, no preguntar ni detener música
        if (_skipClosingConfirmation)
        {
            base.OnClosing(e);
            return;
        }

        var saveChangesWindow = new SaveChangesWindow(
            "¿Seguro que quieres salir?",
            saveButtonText: "Guardar y salir",
            dontSaveButtonText: "Salir sin guardar",
            cancelButtonText: "Cancelar")
        {
            Owner = this
        };

        saveChangesWindow.ShowDialog();

        if (saveChangesWindow.Result == SaveChangesResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (saveChangesWindow.Result == SaveChangesResult.Save)
        {
            SaveMenu_Click(this, new RoutedEventArgs());
        }

        // Al cerrar la ventana de partida detenemos toda la música.
        try
        {
            _sound.StopMusic();
            _sound.Dispose();
        }
        catch
        {
            // Ignorar errores al cerrar el sonido.
        }

        // Si la partida NO se ha iniciado desde el editor (Play del editor),
        // intentamos cerrar Docker Desktop por completo.
        try
        {
            if (!_isRunningFromEditor)
            {
                DockerShutdownHelper.TryShutdownDockerDesktop();
            }
        }
        catch
        {
            // Si algo falla al intentar cerrar Docker, lo ignoramos para no
            // bloquear el cierre de la partida.
        }

        base.OnClosing(e);
    }

    private async void UseLlmCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Ignorar durante la inicialización
        if (_isInitializingCheckbox)
            return;

        if (UseLlmCheckBox.IsChecked == true)
        {
            // El usuario está activando la IA: pedir confirmación
            var confirmDlg = new ConfirmWindow(
                "Al activar la IA se iniciará Docker Desktop automáticamente.\n\n" +
                "Si es la primera vez que la usas, se descargarán los modelos necesarios (puede tardar varios minutos dependiendo de tu conexión).\n\n" +
                "¿Deseas continuar?",
                "Activar IA")
            {
                Owner = this
            };

            if (confirmDlg.ShowDialog() != true)
            {
                // Usuario canceló: desmarcar el checkbox
                UseLlmCheckBox.IsChecked = false;
                return;
            }

            var progressWindow = new DockerProgressWindow
            {
                Owner = this,
                OllamaModel = "llama3.2:3b"
            };

            var result = await progressWindow.RunAsync().ConfigureAwait(true);

            if (result.Canceled)
            {
                UseLlmCheckBox.IsChecked = false;
                _uiSettings.UseLlmForUnknownCommands = false;
                UiSettingsManager.SaveForWorld(_world.Game.Id, _uiSettings);
                return;
            }

            if (!result.Success)
            {
                UseLlmCheckBox.IsChecked = false;
                _uiSettings.UseLlmForUnknownCommands = false;
                UiSettingsManager.SaveForWorld(_world.Game.Id, _uiSettings);

                new AlertWindow(
                    "No se han podido iniciar los servicios de IA y voz. Comprueba que Docker Desktop está instalado y en ejecución.",
                    "Error")
                {
                    Owner = this
                }.ShowDialog();

                return;
            }

            _uiSettings.UseLlmForUnknownCommands = true;
        }
        else
        {
            if (_uiSettings.UseLlmForUnknownCommands)
            {
                // El usuario está desactivando la IA.
                // Preguntar si quiere hacer limpieza profunda de Docker.
                var dlg = new ConfirmWindow(
                    "Estás desactivando la IA. ¿Quieres desinstalar y limpiar completamente Docker Desktop y los modelos descargados?\n\n" +
                    "Esto liberará mucho espacio en disco, pero tendrás que volver a instalar Docker si quieres usar la IA en el futuro.",
                    "Limpiar Docker Desktop")
                {
                    Owner = this
                };

                if (dlg.ShowDialog() == true)
                {
                    Mouse.OverrideCursor = Cursors.Wait;
                    try
                    {
                        var result = await XiloAdventures.Wpf.Common.Utilities.DockerDesktopCleaner.CleanDockerDesktopHardAsync(true);
                        Mouse.OverrideCursor = null;
                        var msg = "Limpieza completada con éxito.";
                        new AlertWindow(msg, "Resultado limpieza") { Owner = this }.ShowDialog();
                    }
                    catch (Exception ex)
                    {
                        Mouse.OverrideCursor = null;
                        new AlertWindow($"Error durante la limpieza:\n{ex.Message}", "Error") { Owner = this }.ShowDialog();
                    }
                    finally
                    {
                        Mouse.OverrideCursor = null;
                    }
                }
            }

            _uiSettings.UseLlmForUnknownCommands = false;
        }

        if (_isRunningFromEditor)
        {
            UiSettingsManager.SaveForWorld(_world.Game.Id, _uiSettings);
        }
        else
        {
            SaveSettingsToGameState();
            try
            {
                SaveManager.AutoSave(_engine.State, CurrentSavesFolder, _world.Game.EncryptionKey);
            }
            catch { }
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.M && Keyboard.Modifiers == ModifierKeys.Control)
        {
            MapEnabledCheckBox.IsChecked = !MapEnabledCheckBox.IsChecked;
            e.Handled = true;
        }
    }

    private void MapEnabledCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        // Ignorar durante la inicialización
        if (_isInitializingCheckbox)
            return;

        _uiSettings.MapEnabled = MapEnabledCheckBox.IsChecked == true;

        if (_isRunningFromEditor)
        {
            UiSettingsManager.SaveForWorld(_world.Game.Id, _uiSettings);
        }
        else
        {
            SaveSettingsToGameState();
            try
            {
                SaveManager.AutoSave(_engine.State, CurrentSavesFolder, _world.Game.EncryptionKey);
            }
            catch { }
        }

        ApplyUiSettings();
    }

    private void LlmInfoIcon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var message = "Si activas la IA, el juego intentará entender mejor comandos complejos o mal escritos. Además, si subes el volumen de voz en las opciones, oirás las descripciones de las salas.\n\nPara usarla debes tener Docker Desktop instalado. La primera vez que se use se descargarán algunos componentes y puede tardar unos minutos. Después funcionará muy rápido.";

        var link = new TextBlock
        {
            Margin = new Thickness(0, 12, 0, 0)
        };
        var hyperlink = new Hyperlink
        {
            NavigateUri = new Uri("https://docs.docker.com/desktop/setup/install/windows-install/")
        };
        hyperlink.Inlines.Add("Instala Docker Desktop");
        hyperlink.RequestNavigate += LlmHelpLink_RequestNavigate;
        link.Inlines.Add(hyperlink);

        var dlg = new AlertWindow(message, "IA y voz")
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
            // Ignorar errores al abrir el navegador
        }
    }

    #region Mapa y Navegación

    private void UpdateArrows()
    {
        var room = _engine.CurrentRoom;
        if (room == null) return;

        var exits = GetAllExitsFromRoom(room);
        var doors = GetDoorsInRoom(room);

        UpdateArrowButton(ArrowN, "n", exits, doors);
        UpdateArrowButton(ArrowS, "s", exits, doors);
        UpdateArrowButton(ArrowE, "e", exits, doors);
        UpdateArrowButton(ArrowW, "o", exits, doors);
        UpdateArrowButton(ArrowNE, "ne", exits, doors);
        UpdateArrowButton(ArrowNW, "no", exits, doors);
        UpdateArrowButton(ArrowSE, "se", exits, doors);
        UpdateArrowButton(ArrowSW, "so", exits, doors);
        UpdateArrowButton(ArrowUp, "ar", exits, doors);
        UpdateArrowButton(ArrowDown, "ab", exits, doors);
    }

    private void UpdateArrowButton(Button button, string dirCode, Dictionary<string, (string TargetRoomId, string? DoorId)> exits, Dictionary<string, Door> doors)
    {
        if (exits.TryGetValue(dirCode, out var exitInfo))
        {
            button.Visibility = Visibility.Visible;

            // Verificar si hay puerta
            if (!string.IsNullOrEmpty(exitInfo.DoorId) && doors.TryGetValue(exitInfo.DoorId, out var door))
            {
                if (door.IsOpen)
                    button.Foreground = new SolidColorBrush(Color.FromRgb(100, 200, 100));  // Verde - puerta abierta
                else if (door.IsLocked)
                    button.Foreground = new SolidColorBrush(Color.FromRgb(200, 80, 80));    // Rojo - puerta cerrada con llave
                else
                    button.Foreground = new SolidColorBrush(Color.FromRgb(200, 160, 80));   // Naranja - puerta cerrada sin llave
            }
            else
            {
                button.Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)); // Gris claro - sin puerta
            }
        }
        else
        {
            button.Visibility = Visibility.Hidden;
        }
    }

    private Dictionary<string, (string TargetRoomId, string? DoorId)> GetAllExitsFromRoom(Room room)
    {
        var exits = new Dictionary<string, (string, string?)>(StringComparer.OrdinalIgnoreCase);

        // Salidas directas
        foreach (var exit in room.Exits)
        {
            var norm = NormalizeDirection(exit.Direction);
            if (!exits.ContainsKey(norm))
                exits[norm] = (exit.TargetRoomId, exit.DoorId);
        }

        // Salidas inversas
        foreach (var candidateRoom in _engine.State.Rooms)
        {
            if (candidateRoom.Id.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var candidateExit in candidateRoom.Exits)
            {
                if (!candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                var normCandidate = NormalizeDirection(candidateExit.Direction);
                var opposite = GetOppositeDirection(normCandidate);

                if (!exits.ContainsKey(opposite))
                    exits[opposite] = (candidateRoom.Id, candidateExit.DoorId);
            }
        }

        return exits;
    }

    private Dictionary<string, Door> GetDoorsInRoom(Room room)
    {
        var doors = new Dictionary<string, Door>(StringComparer.OrdinalIgnoreCase);

        // Puertas en salidas directas
        foreach (var exit in room.Exits)
        {
            if (!string.IsNullOrEmpty(exit.DoorId))
            {
                var door = _engine.State.Doors.FirstOrDefault(d =>
                    d.Id.Equals(exit.DoorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                    doors[exit.DoorId] = door;
            }
        }

        // Puertas en salidas inversas (de otras salas que apuntan a esta)
        foreach (var candidateRoom in _engine.State.Rooms)
        {
            if (candidateRoom.Id.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var candidateExit in candidateRoom.Exits)
            {
                if (!candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!string.IsNullOrEmpty(candidateExit.DoorId) && !doors.ContainsKey(candidateExit.DoorId))
                {
                    var door = _engine.State.Doors.FirstOrDefault(d =>
                        d.Id.Equals(candidateExit.DoorId, StringComparison.OrdinalIgnoreCase));
                    if (door != null)
                        doors[candidateExit.DoorId] = door;
                }
            }
        }

        return doors;
    }

    private static string NormalizeDirection(string dir)
    {
        dir = dir.ToLowerInvariant().Trim();
        return dir switch
        {
            "norte" or "north" or "n" => "n",
            "sur" or "south" or "s" => "s",
            "este" or "east" or "e" => "e",
            "oeste" or "west" or "o" or "w" => "o",
            "noreste" or "northeast" or "ne" => "ne",
            "noroeste" or "northwest" or "no" or "nw" => "no",
            "sureste" or "southeast" or "se" => "se",
            "suroeste" or "southwest" or "so" or "sw" => "so",
            "arriba" or "up" or "ar" or "u" => "ar",
            "abajo" or "down" or "ab" or "d" => "ab",
            _ => dir
        };
    }

    private static string GetOppositeDirection(string dir)
    {
        return dir switch
        {
            "n" => "s",
            "s" => "n",
            "e" => "o",
            "o" => "e",
            "ne" => "so",
            "no" => "se",
            "se" => "no",
            "so" => "ne",
            "ar" => "ab",
            "ab" => "ar",
            _ => dir
        };
    }

    private void Arrow_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        var direction = button.Tag?.ToString();
        if (string.IsNullOrEmpty(direction)) return;

        // Mostrar el comando ejecutado
        var cmd = $"ir {direction}";
        AppendText($"> {cmd}");
        AppendSeparator();

        // Ejecutar el comando de movimiento
        var result = _engine.ProcessCommand(cmd);
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            AppendText(result.Message);
        }

        UpdateStatusPanel();
        UpdateRoomVisuals();

        // Autoguardar (en player exportado, incluir configuración)
        if (!_isRunningFromEditor)
        {
            SaveSettingsToGameState();
        }
        try
        {
            SaveManager.AutoSave(_engine.State, CurrentSavesFolder, _world.Game.EncryptionKey);
        }
        catch
        {
            // Ignoramos errores de autosave
        }

        // Devolver el foco al campo de entrada
        InputTextBox.Focus();
    }

    private void DrawMap()
    {
        MapCanvas.Children.Clear();

        var room = _engine.CurrentRoom;
        if (room == null) return;

        // Asegurar que la sala actual está registrada
        TrackVisitedRoom(room);

        // Calcular coordenadas basadas en las conexiones
        CalculateRoomCoordinates(room);

        // Obtener coordenadas de la sala actual
        if (!_roomCoordinates.TryGetValue(room.Id, out var currentCoords)) return;

        var canvasWidth = MapCanvas.ActualWidth > 0 ? MapCanvas.ActualWidth : 260;
        var canvasHeight = MapCanvas.ActualHeight > 0 ? MapCanvas.ActualHeight : 200;

        // Centrar el mapa en la sala actual
        var offsetX = canvasWidth / 2 - currentCoords.X * (RoomWidth + RoomSpacing) - RoomWidth / 2;
        var offsetY = canvasHeight / 2 + currentCoords.Y * (RoomHeight + RoomSpacing) - RoomHeight / 2;

        // Construir mapa de colisiones: agrupar salas por coordenadas
        var collisionMap = new Dictionary<(int X, int Y), List<string>>();
        foreach (var roomId in _visitedRooms)
        {
            if (!_roomCoordinates.TryGetValue(roomId, out var coords)) continue;
            if (!collisionMap.ContainsKey(coords))
                collisionMap[coords] = new List<string>();
            collisionMap[coords].Add(roomId);
        }

        // Función para obtener el desplazamiento de colisión en píxeles
        (double pixelOffsetX, double pixelOffsetY) GetCollisionOffset(string roomId, (int X, int Y) coords)
        {
            if (!collisionMap.TryGetValue(coords, out var roomsAtPosition))
                return (0, 0);
            var index = roomsAtPosition.IndexOf(roomId);
            return (index * 10, index * 10); // 10px por cada sala adicional
        }

        // Dibujar conexiones primero
        foreach (var roomId in _visitedRooms)
        {
            if (!_roomCoordinates.TryGetValue(roomId, out var coords)) continue;
            var visitedRoom = _engine.State.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (visitedRoom == null) continue;

            var (collOffX1, collOffY1) = GetCollisionOffset(roomId, coords);
            var x1 = offsetX + coords.X * (RoomWidth + RoomSpacing) + RoomWidth / 2 + collOffX1;
            var y1 = offsetY - coords.Y * (RoomHeight + RoomSpacing) + RoomHeight / 2 + collOffY1;

            foreach (var exit in visitedRoom.Exits)
            {
                if (_roomCoordinates.TryGetValue(exit.TargetRoomId, out var targetCoords))
                {
                    var (collOffX2, collOffY2) = GetCollisionOffset(exit.TargetRoomId, targetCoords);
                    var x2 = offsetX + targetCoords.X * (RoomWidth + RoomSpacing) + RoomWidth / 2 + collOffX2;
                    var y2 = offsetY - targetCoords.Y * (RoomHeight + RoomSpacing) + RoomHeight / 2 + collOffY2;

                    var line = new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                        StrokeThickness = 2
                    };
                    MapCanvas.Children.Add(line);
                }
            }
        }

        // Dibujar salas
        foreach (var roomId in _visitedRooms)
        {
            if (!_roomCoordinates.TryGetValue(roomId, out var coords)) continue;
            var visitedRoom = _engine.State.Rooms.FirstOrDefault(r => r.Id == roomId);
            if (visitedRoom == null) continue;

            var (collOffX, collOffY) = GetCollisionOffset(roomId, coords);
            var x = offsetX + coords.X * (RoomWidth + RoomSpacing) + collOffX;
            var y = offsetY - coords.Y * (RoomHeight + RoomSpacing) + collOffY;

            var isCurrentRoom = roomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase);

            var rect = new Rectangle
            {
                Width = RoomWidth,
                Height = RoomHeight,
                Fill = isCurrentRoom
                    ? new SolidColorBrush(Color.FromRgb(80, 120, 180))
                    : new SolidColorBrush(Color.FromRgb(50, 50, 60)),
                Stroke = isCurrentRoom
                    ? new SolidColorBrush(Color.FromRgb(100, 180, 255))
                    : new SolidColorBrush(Color.FromRgb(80, 80, 100)),
                StrokeThickness = isCurrentRoom ? 2 : 1,
                RadiusX = 4,
                RadiusY = 4
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MapCanvas.Children.Add(rect);

            // Nombre de la sala (permite múltiples líneas)
            var tooltipContent = new StackPanel { MaxWidth = 300 };
            tooltipContent.Children.Add(new TextBlock
            {
                Text = visitedRoom.Name,
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 8)
            });
            tooltipContent.Children.Add(new TextBlock
            {
                Text = visitedRoom.Description,
                TextWrapping = TextWrapping.Wrap
            });

            var text = new TextBlock
            {
                Text = visitedRoom.Name,
                Foreground = Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = RoomWidth - 4,
                Height = RoomHeight - 4,
                TextAlignment = TextAlignment.Center,
                ToolTip = tooltipContent
            };
            Canvas.SetLeft(text, x + 2);
            Canvas.SetTop(text, y + 2);
            MapCanvas.Children.Add(text);
        }
    }

    private void CalculateRoomCoordinates(Room startRoom)
    {
        // Si es la primera sala (no hay coordenadas aún), iniciar en (0, 0)
        if (_roomCoordinates.Count == 0)
        {
            _roomCoordinates[startRoom.Id] = (0, 0);
        }
        else if (!_roomCoordinates.ContainsKey(startRoom.Id))
        {
            // Buscar una sala ya posicionada que conecte con esta
            foreach (var existingRoomId in _roomCoordinates.Keys.ToList())
            {
                var existingRoom = _engine.State.Rooms.FirstOrDefault(r => r.Id == existingRoomId);
                if (existingRoom == null) continue;

                foreach (var exit in existingRoom.Exits)
                {
                    if (exit.TargetRoomId.Equals(startRoom.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        var existingCoords = _roomCoordinates[existingRoomId];
                        var dir = NormalizeDirection(exit.Direction);
                        var (dx, dy) = GetDirectionOffset(dir);
                        _roomCoordinates[startRoom.Id] = (existingCoords.X + dx, existingCoords.Y + dy);
                        goto foundCoords;
                    }
                }
            }

            // Si no encontramos conexión inversa, buscar desde startRoom hacia salas posicionadas
            foreach (var exit in startRoom.Exits)
            {
                if (_roomCoordinates.TryGetValue(exit.TargetRoomId, out var targetCoords))
                {
                    var dir = NormalizeDirection(exit.Direction);
                    var (dx, dy) = GetDirectionOffset(dir);
                    // La nueva sala está en dirección opuesta respecto a la sala destino
                    _roomCoordinates[startRoom.Id] = (targetCoords.X - dx, targetCoords.Y - dy);
                    break;
                }
            }
        foundCoords:;
        }

        // BFS para calcular posiciones de salas conectadas visitadas
        var processed = new HashSet<string>();
        var queue = new Queue<string>();

        // Iniciar desde todas las salas con coordenadas
        foreach (var roomId in _roomCoordinates.Keys)
        {
            queue.Enqueue(roomId);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            if (processed.Contains(currentId)) continue;
            processed.Add(currentId);

            if (!_roomCoordinates.TryGetValue(currentId, out var currentCoords)) continue;

            var currentRoom = _engine.State.Rooms.FirstOrDefault(r => r.Id == currentId);
            if (currentRoom == null) continue;

            foreach (var exit in currentRoom.Exits)
            {
                if (!_visitedRooms.Contains(exit.TargetRoomId)) continue;
                if (_roomCoordinates.ContainsKey(exit.TargetRoomId)) continue;

                var dir = NormalizeDirection(exit.Direction);
                var (dx, dy) = GetDirectionOffset(dir);

                _roomCoordinates[exit.TargetRoomId] = (currentCoords.X + dx, currentCoords.Y + dy);
                queue.Enqueue(exit.TargetRoomId);
            }
        }
    }

    private static (int dx, int dy) GetDirectionOffset(string dir)
    {
        // Norte = +Y, Sur = -Y, Este = +X, Oeste = -X
        // Arriba/Abajo se desplazan diagonalmente para no solapar con norte/sur
        return dir switch
        {
            "n" => (0, 1),
            "s" => (0, -1),
            "e" => (1, 0),
            "o" => (-1, 0),
            "ne" => (1, 1),
            "no" => (-1, 1),
            "se" => (1, -1),
            "so" => (-1, -1),
            "ar" or "up" => (1, 1),    // Arriba: diagonal derecha-arriba
            "ab" or "down" => (1, -1), // Abajo: diagonal derecha-abajo
            _ => (0, 0)
        };
    }

    private void MapCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var delta = e.Delta > 0 ? ZoomStep : -ZoomStep;
        var newZoom = Math.Clamp(_mapZoom + delta, MinZoom, MaxZoom);
        if (Math.Abs(newZoom - _mapZoom) < 0.001) return;

        // Obtener posición del ratón relativa al canvas
        var mousePos = e.GetPosition(MapCanvas);

        // Calcular el nuevo offset para mantener el punto bajo el cursor
        var scale = newZoom / _mapZoom;
        _panOffsetX = mousePos.X - (mousePos.X - _panOffsetX) * scale;
        _panOffsetY = mousePos.Y - (mousePos.Y - _panOffsetY) * scale;

        _mapZoom = newZoom;
        ApplyMapTransform();
        e.Handled = true;
    }

    private void MapCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Botón central (Middle) para arrastrar
        if (e.MiddleButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _panStart = e.GetPosition(this);
            ((FrameworkElement)sender).CaptureMouse();
            e.Handled = true;
        }
    }

    private void MapCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isPanning && e.MiddleButton == MouseButtonState.Released)
        {
            _isPanning = false;
            ((FrameworkElement)sender).ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;

        var currentPos = e.GetPosition(this);
        var delta = currentPos - _panStart;
        _panStart = currentPos;

        _panOffsetX += delta.X;
        _panOffsetY += delta.Y;
        ApplyMapTransform();
    }

    private void MapCanvas_MouseLeave(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            _isPanning = false;
            ((FrameworkElement)sender).ReleaseMouseCapture();
        }
    }

    private void ApplyMapTransform()
    {
        MapScaleTransform.ScaleX = _mapZoom;
        MapScaleTransform.ScaleY = _mapZoom;
        MapTranslateTransform.X = _panOffsetX;
        MapTranslateTransform.Y = _panOffsetY;
    }

    private void ResetMapTransform()
    {
        _mapZoom = 1.0;
        _panOffsetX = 0;
        _panOffsetY = 0;
        ApplyMapTransform();
    }

    #endregion

    #region Settings Persistence

    /// <summary>
    /// Guarda la configuración de UI en el GameState (para player exportado).
    /// </summary>
    private void SaveSettingsToGameState()
    {
        _engine.State.SoundEnabled = _uiSettings.SoundEnabled;
        _engine.State.FontSize = _uiSettings.FontSize;
        _engine.State.FontFamily = _uiSettings.FontFamily;
        _engine.State.MusicVolume = _uiSettings.MusicVolume;
        _engine.State.EffectsVolume = _uiSettings.EffectsVolume;
        _engine.State.MasterVolume = _uiSettings.MasterVolume;
        _engine.State.VoiceVolume = _uiSettings.VoiceVolume;
        _engine.State.MapEnabled = _uiSettings.MapEnabled;
        _engine.State.UseLlmForUnknownCommands = _uiSettings.UseLlmForUnknownCommands;
    }

    /// <summary>
    /// Carga la configuración de UI desde el GameState (para player exportado).
    /// </summary>
    public async void LoadSettingsFromGameState()
    {
        _uiSettings.SoundEnabled = _engine.State.SoundEnabled;
        _uiSettings.FontSize = _engine.State.FontSize;
        _uiSettings.FontFamily = _engine.State.FontFamily;
        _uiSettings.MusicVolume = _engine.State.MusicVolume;
        _uiSettings.EffectsVolume = _engine.State.EffectsVolume;
        _uiSettings.MasterVolume = _engine.State.MasterVolume;
        _uiSettings.VoiceVolume = _engine.State.VoiceVolume;
        _uiSettings.MapEnabled = _engine.State.MapEnabled;
        _uiSettings.UseLlmForUnknownCommands = _engine.State.UseLlmForUnknownCommands;

        // Aplicar al SoundManager
        _sound.SoundEnabled = _uiSettings.SoundEnabled;
        _sound.MusicVolume = (float)(_uiSettings.MusicVolume / 10.0);
        _sound.EffectsVolume = (float)(_uiSettings.EffectsVolume / 10.0);
        _sound.MasterVolume = (float)(_uiSettings.MasterVolume / 10.0);
        _sound.VoiceVolume = (float)(_uiSettings.VoiceVolume / 10.0);
        _sound.RefreshVolumes();

        // Si la IA estaba activada, intentar iniciar Docker
        if (_uiSettings.UseLlmForUnknownCommands)
        {
            var progressWindow = new DockerProgressWindow
            {
                Owner = this,
                OllamaModel = "llama3.2:3b"
            };

            var result = await progressWindow.RunAsync().ConfigureAwait(true);

            if (!result.Success || result.Canceled)
            {
                _uiSettings.UseLlmForUnknownCommands = false;
                _engine.State.UseLlmForUnknownCommands = false;

                if (!result.Canceled)
                {
                    new AlertWindow(
                        "No se han podido iniciar los servicios de IA y voz. La IA ha sido desactivada.",
                        "Error")
                    {
                        Owner = this
                    }.ShowDialog();
                }
            }
        }

        // Actualizar checkboxes sin disparar eventos
        _isInitializingCheckbox = true;
        UseLlmCheckBox.IsChecked = _uiSettings.UseLlmForUnknownCommands;
        MapEnabledCheckBox.IsChecked = _uiSettings.MapEnabled;
        _isInitializingCheckbox = false;

        ApplyUiSettings();
    }

    #endregion
}
