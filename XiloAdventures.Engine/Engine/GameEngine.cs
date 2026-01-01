using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Core game engine that processes player commands and manages game state.
/// Handles movement, inventory, doors, NPCs, quests, and room descriptions.
/// </summary>
/// <remarks>
/// The engine uses a command parser to interpret player input and updates
/// the game state accordingly. It also manages audio playback for room
/// transitions and integrates with the door/key system for locked passages.
/// </remarks>
public class GameEngine
{
    private readonly WorldModel _world;
    private readonly SoundManager _sound;
    private readonly bool _isDebugMode;
    private GameState _state;

    private DoorService _doorService;
    private DateTime _lastRealTime;
    private ScriptEngine? _scriptEngine;
    private ConversationEngine? _conversationEngine;
    private bool _initialScriptsReady;
    private int _lastEventMinute = -1;
    private int _lastEventHour = -1;

    // Salas visitadas en esta sesión (para TTS solo en primera visita)
    private readonly HashSet<string> _visitedRoomsForTts = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads a new game state into the engine, replacing the current state.
    /// Rebuilds room indexes and triggers room change events.
    /// </summary>
    /// <param name="newState">The new game state to load.</param>
    public void LoadState(GameState newState)
    {
        _state = newState;
        _doorService = new DoorService(_state.Doors, _state.Objects);
        _lastRealTime = DateTime.Now;

        // Reiniciar salas visitadas para TTS (al cargar partida se leen de nuevo)
        _visitedRoomsForTts.Clear();

        // Reinicializar el motor de scripts con el nuevo estado
        _scriptEngine = new ScriptEngine(_world, _state, _isDebugMode);
        _scriptEngine.OnMessage += message => ScriptMessage?.Invoke(message);
        _scriptEngine.OnPlaySound += soundId =>
        {
            // TODO: Implementar reproducción de efectos de sonido cuando SoundManager lo soporte
            // var fxAsset = _world.Fxs.FirstOrDefault(f => f.Id.Equals(soundId, StringComparison.OrdinalIgnoreCase));
        };
        _scriptEngine.OnPlayerTeleported += roomId =>
        {
            WorldLoader.RebuildRoomIndexes(_state);
            OnRoomChanged();
        };
        _scriptEngine.OnStartConversation += npcId =>
        {
            _ = StartConversationWithNpcAsync(npcId);
        };
        _scriptEngine.OnStartTrade += npcId =>
        {
            var npc = _state.Npcs.FirstOrDefault(n =>
                string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
            if (npc != null && npc.IsShopkeeper)
            {
                TradeOpened?.Invoke(npc);
            }
        };
        _scriptEngine.OnAdventureCompleted += () => AdventureCompleted?.Invoke();

        // Reinicializar el motor de conversaciones
        InitializeConversationEngine();

        WorldLoader.RebuildRoomIndexes(_state);
        OnRoomChanged();
    }

    /// <summary>
    /// Inicializa el motor de conversaciones y conecta sus eventos.
    /// </summary>
    private void InitializeConversationEngine()
    {
        _conversationEngine = new ConversationEngine(_world, _state, _isDebugMode);
        _conversationEngine.OnDialogue += msg => ConversationDialogue?.Invoke(msg);
        _conversationEngine.OnPlayerOptions += options => ConversationOptions?.Invoke(options);
        _conversationEngine.OnTradeOpen += npc => TradeOpened?.Invoke(npc);
        _conversationEngine.OnConversationEnded += () => ConversationEnded?.Invoke();
        _conversationEngine.OnSystemMessage += msg => ScriptMessage?.Invoke(msg);
    }

    /// <summary>
    /// Gets the current game state containing all runtime data.
    /// </summary>
    public GameState State => _state;

    /// <summary>
    /// Reproduce la descripción de la sala actual con TTS y la marca como visitada.
    /// Usar después de que MainWindow esté lista y la voz no esté suprimida.
    /// </summary>
    public void PlayCurrentRoomDescription()
    {
        var room = CurrentRoom;
        if (room != null && _visitedRoomsForTts.Add(room.Id))
        {
            _ = _sound.PlayRoomDescriptionAsync(room.Id, room.Description);
        }
    }

    /// <summary>
    /// Event raised when the player moves to a different room.
    /// Used by UI to update room visuals and trigger audio changes.
    /// </summary>
    public event Action<Room>? RoomChanged;

    /// <summary>
    /// Event raised when a script wants to show a message to the player.
    /// </summary>
    public event Action<string>? ScriptMessage;

    /// <summary>
    /// Evento cuando hay texto de diálogo en una conversación.
    /// </summary>
    public event Action<ConversationMessage>? ConversationDialogue;

    /// <summary>
    /// Evento cuando hay opciones de diálogo para el jugador.
    /// </summary>
    public event Action<List<DialogueOption>>? ConversationOptions;

    /// <summary>
    /// Evento cuando se abre el comercio con un NPC.
    /// </summary>
    public event Action<Npc>? TradeOpened;

    /// <summary>
    /// Evento cuando se abre la ventana de fabricación.
    /// </summary>
    public event Action? CraftOpened;

    /// <summary>
    /// Evento cuando termina una conversación.
    /// </summary>
    public event Action? ConversationEnded;

    /// <summary>
    /// Evento cuando se completan todas las misiones principales (fin de la aventura).
    /// </summary>
    public event Action? AdventureCompleted;

    /// <summary>
    /// Evento cuando el jugador muere por necesidades básicas, falta de salud o cordura.
    /// </summary>
    public event Action<DeathType>? PlayerDied;

    /// <summary>
    /// Evento cuando el jugador solicita ayuda (ayuda, help, ?).
    /// </summary>
    public event Action? HelpRequested;

    /// <summary>
    /// Indica si hay una conversación activa.
    /// </summary>
    public bool IsConversationActive => _conversationEngine?.IsConversationActive == true;

    /// <summary>
    /// Cierra la tienda/comercio activo.
    /// </summary>
    public void CloseShop()
    {
        _ = _conversationEngine?.CloseShopAsync();
    }

    /// <summary>
    /// Dispara los scripts iniciales (Event_OnGameStart y Event_OnEnter de la sala inicial).
    /// Debe llamarse después de suscribir los eventos del engine.
    /// </summary>
    public void TriggerInitialScripts()
    {
        _initialScriptsReady = true;

        // Disparar Event_OnGameStart
        var gameId = _world.Game?.Id ?? "game";
        _ = TriggerEntityScriptAsync("Game", gameId, "Event_OnGameStart");

        // Disparar Event_OnEnter de la sala actual
        var room = CurrentRoom;
        if (room != null)
        {
            _ = TriggerRoomScriptsAsync(room.Id, "Event_OnEnter");
        }
    }

    /// <summary>
    /// Dispara un evento de script para una entidad específica.
    /// Usado para eventos externos que se originan fuera de GameEngine (combat, trade, etc.).
    /// </summary>
    /// <param name="ownerType">Tipo de entidad: "Game", "Room", "Door", "Npc", "GameObject", "Player", "Quest"</param>
    /// <param name="ownerId">ID de la entidad</param>
    /// <param name="eventType">Tipo de evento (ej: "Event_OnCombatVictory")</param>
    public void TriggerScriptEvent(string ownerType, string ownerId, string eventType)
    {
        _ = TriggerEntityScriptAsync(ownerType, ownerId, eventType);
    }

    /// <summary>
    /// Dispara un evento de combate (victoria, derrota, huida).
    /// </summary>
    /// <param name="npcId">ID del NPC enemigo</param>
    /// <param name="reason">Razón del fin del combate</param>
    public void TriggerCombatEndEvent(string npcId, CombatEndReason reason)
    {
        var eventType = reason switch
        {
            CombatEndReason.Victory => "Event_OnCombatVictory",
            CombatEndReason.Defeat => "Event_OnCombatDefeat",
            CombatEndReason.Fled => "Event_OnCombatFlee",
            CombatEndReason.EnemyFled => "Event_OnCombatFlee",
            _ => null
        };

        if (eventType != null)
        {
            // Disparar en el NPC
            _ = TriggerEntityScriptAsync("Npc", npcId, eventType);
            // También disparar como evento global del juego
            _ = TriggerEntityScriptAsync("Game", _world.Game?.Id ?? "game", eventType);
        }

        // Si el jugador murió, disparar Event_OnPlayerDeath
        if (reason == CombatEndReason.Defeat)
        {
            _ = TriggerEntityScriptAsync("Player", "player", "Event_OnPlayerDeath");
        }
    }

    /// <summary>
    /// Dispara eventos de comercio.
    /// </summary>
    /// <param name="npcId">ID del NPC comerciante</param>
    /// <param name="eventType">Tipo de evento: "Event_OnTradeStart", "Event_OnTradeEnd", "Event_OnItemBought", "Event_OnItemSold"</param>
    public void TriggerTradeEvent(string npcId, string eventType)
    {
        _ = TriggerEntityScriptAsync("Npc", npcId, eventType);
    }

    /// <summary>
    /// Dispara eventos relacionados con el oro del jugador.
    /// </summary>
    /// <param name="amount">Cantidad de oro ganada (positivo) o perdida (negativo)</param>
    /// <param name="newTotal">Nuevo total de oro del jugador</param>
    public void TriggerGoldChangeEvent(int amount, int newTotal)
    {
        var gameId = _world.Game?.Id ?? "game";
        if (amount > 0)
        {
            _ = TriggerEntityScriptAsync("Game", gameId, "Event_OnGoldGained");
        }
        else if (amount < 0)
        {
            _ = TriggerEntityScriptAsync("Game", gameId, "Event_OnGoldLost");
        }
    }

    /// <summary>
    /// Creates a new game engine instance.
    /// </summary>
    /// <param name="world">The world model containing static game definitions.</param>
    /// <param name="state">The initial game state (can be new or loaded from save).</param>
    /// <param name="soundManager">Sound manager for music and voice playback.</param>
    /// <param name="isDebugMode">If true, shows debug messages (for editor test mode).</param>
    public GameEngine(WorldModel world, GameState state, SoundManager soundManager, bool isDebugMode = false)
    {
        _world = world;
        _sound = soundManager;
        _isDebugMode = isDebugMode;
        _state = state;
        _doorService = new DoorService(_state.Doors, _state.Objects);

        // Inicializar hora de juego al comenzar la partida si no viene informada.
        if (_state.GameTime == default)
        {
            // Usamos la hora inicial configurada en el juego, en lugar de la hora real.
            var startHour = _world.Game?.StartHour ?? 9;
            if (startHour < 0) startHour = 0;
            if (startHour > 23) startHour = 23;

            var today = DateTime.Today;
            _state.GameTime = new DateTime(today.Year, today.Month, today.Day, startHour, 0, 0);
        }
        _lastRealTime = DateTime.Now;

        // Asegurar índices consistentes
        WorldLoader.RebuildRoomIndexes(_state);
        EnsurePlayerRoom();

        // Inicializar el motor de scripts
        _scriptEngine = new ScriptEngine(_world, _state, _isDebugMode);
        _scriptEngine.OnMessage += message => ScriptMessage?.Invoke(message);
        _scriptEngine.OnPlaySound += soundId =>
        {
            // TODO: Implementar reproducción de efectos de sonido cuando SoundManager lo soporte
            // var fxAsset = _world.Fxs.FirstOrDefault(f => f.Id.Equals(soundId, StringComparison.OrdinalIgnoreCase));
        };
        _scriptEngine.OnPlayerTeleported += roomId =>
        {
            WorldLoader.RebuildRoomIndexes(_state);
            OnRoomChanged();
        };
        _scriptEngine.OnStartConversation += npcId =>
        {
            _ = StartConversationWithNpcAsync(npcId);
        };
        _scriptEngine.OnStartTrade += npcId =>
        {
            var npc = _state.Npcs.FirstOrDefault(n =>
                string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
            if (npc != null && npc.IsShopkeeper)
            {
                TradeOpened?.Invoke(npc);
            }
        };
        _scriptEngine.OnAdventureCompleted += () => AdventureCompleted?.Invoke();

        // Inicializar el motor de conversaciones
        InitializeConversationEngine();

        // Arrancar la música global del mundo (si la hay) al inicio de la partida.
        if (_world.Game != null && !string.IsNullOrWhiteSpace(_state.WorldMusicId))
        {
            var musicAsset = _world.Musics.FirstOrDefault(m => m.Id.Equals(_state.WorldMusicId, StringComparison.OrdinalIgnoreCase));
            _sound.PlayWorldMusic(_state.WorldMusicId, musicAsset?.Base64);
        }

        OnRoomChanged();
    }

    // Helper methods for common searches with case-insensitive comparison
    private GameObject? FindObjectById(string id)
        => _state.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Comprueba si el nombre del objeto coincide con alguna de las variantes proporcionadas.
    /// Útil cuando el parser normaliza sustantivos (ej: "sable" → "espada") pero el objeto
    /// real se llama "sable oxidado". Primero se intenta con el valor normalizado,
    /// y si falla, con el valor original.
    /// Normaliza ambos lados de la comparación para ignorar acentos (ej: "baúl" coincide con "baul").
    /// </summary>
    private static bool MatchesName(string objectName, string? normalizedName, string? originalName)
    {
        // Normalizar el nombre del objeto para comparaciones sin acentos
        var objectNameNormalized = RemoveAccents(objectName);

        if (!string.IsNullOrEmpty(normalizedName) &&
            objectNameNormalized.Contains(normalizedName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrEmpty(originalName) &&
            !string.Equals(originalName, normalizedName, StringComparison.OrdinalIgnoreCase) &&
            objectNameNormalized.Contains(RemoveAccents(originalName), StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private Room? FindRoomById(string id)
        => _state.Rooms.FirstOrDefault(r => r.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    private Npc? FindNpcById(string id)
        => _state.Npcs.FirstOrDefault(n => n.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Returns the name with first letter capitalized (for start of sentence).
    /// </summary>
    private static string Cap(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToUpper(name[0]) + name[1..];
    }

    /// <summary>
    /// Elimina acentos de una cadena para comparaciones insensibles a tildes.
    /// </summary>
    private static string RemoveAccents(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    /// <summary>
    /// Returns the name with first letter lowercased (for mid-sentence use).
    /// </summary>
    private static string Low(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        return char.ToLower(name[0]) + name[1..];
    }

    /// <summary>Devuelve el artículo definido (el/la/los/las) según género y número.</summary>
    private static string Article(GameObject obj) => obj.Gender switch
    {
        GrammaticalGender.Feminine when obj.IsPlural => "las",
        GrammaticalGender.Feminine => "la",
        GrammaticalGender.Masculine when obj.IsPlural => "los",
        _ => "el"
    };

    /// <summary>Devuelve el pronombre acusativo (lo/la/los/las) según género y número.</summary>
    private static string AccusativePronoun(GameObject obj) => obj.Gender switch
    {
        GrammaticalGender.Feminine when obj.IsPlural => "las",
        GrammaticalGender.Feminine => "la",
        GrammaticalGender.Masculine when obj.IsPlural => "los",
        _ => "lo"
    };

    /// <summary>Devuelve el artículo indefinido (un/una/unos/unas) según género y número.</summary>
    private static string IndefiniteArticle(GameObject obj) => obj.Gender switch
    {
        GrammaticalGender.Feminine when obj.IsPlural => "unas",
        GrammaticalGender.Feminine => "una",
        GrammaticalGender.Masculine when obj.IsPlural => "unos",
        _ => "un"
    };

    /// <summary>Devuelve "el/la + nombre" en minúsculas.</summary>
    private static string WithArticle(GameObject obj) => $"{Article(obj)} {Low(obj.Name)}";

    /// <summary>Devuelve "El/La + nombre" con mayúscula inicial.</summary>
    private static string WithArticleCap(GameObject obj) => Cap($"{Article(obj)} {Low(obj.Name)}");

    /// <summary>Devuelve "un/una + nombre" en minúsculas.</summary>
    private static string WithIndefiniteArticle(GameObject obj) => $"{IndefiniteArticle(obj)} {Low(obj.Name)}";

    /// <summary>Devuelve "Un/Una + nombre" con mayúscula inicial.</summary>
    private static string WithIndefiniteArticleCap(GameObject obj) => Cap($"{IndefiniteArticle(obj)} {Low(obj.Name)}");

    /// <summary>
    /// Find an object in the current room or player inventory by name.
    /// Supports fallback to original name when noun aliases don't match.
    /// Objects in inventory are always findable regardless of Visible property.
    /// </summary>
    private GameObject? FindObjectInRoomOrInventory(Room room, string name, string? originalName = null)
    {
        // Primero buscar en inventario (siempre accesible, sin importar Visible)
        foreach (var objId in _state.InventoryObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null && MatchesName(obj.Name, name, originalName))
                return obj;
        }

        // Luego buscar en la sala (solo objetos visibles)
        foreach (var objId in room.ObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null && obj.Visible && MatchesName(obj.Name, name, originalName))
                return obj;
        }

        return null;
    }

    /// <summary>
    /// Helper methods for container objects
    /// </summary>
    private bool CanOpenContainer(GameObject container, out string message)
    {
        message = "";

        if (!container.IsContainer)
        {
            message = $"{Cap(container.Name)} no es un contenedor.";
            return false;
        }

        if (!container.IsOpenable)
        {
            message = $"No consigues abrir{AccusativePronoun(container)}.";
            return false;
        }

        if (container.IsOpen)
        {
            message = $"{Cap(container.Name)} ya está abiert{(container.Gender == GrammaticalGender.Feminine ? "a" : "o")}.";
            return false;
        }

        if (container.IsLocked)
        {
            message = $"{Cap(container.Name)} está cerrad{(container.Gender == GrammaticalGender.Feminine ? "a" : "o")} con llave.";
            return false;
        }

        return true;
    }

    private bool CanCloseContainer(GameObject container, out string message)
    {
        message = "";

        if (!container.IsContainer)
        {
            message = $"{Cap(container.Name)} no es un contenedor.";
            return false;
        }

        if (!container.IsOpenable)
        {
            message = $"No consigues cerrar{AccusativePronoun(container)}.";
            return false;
        }

        if (!container.IsOpen)
        {
            message = $"{Cap(container.Name)} ya está cerrad{(container.Gender == GrammaticalGender.Feminine ? "a" : "o")}.";
            return false;
        }

        return true;
    }

    private bool CanUnlockContainer(GameObject container, string? keyId, out string message)
    {
        message = "";

        if (!container.IsLocked)
        {
            message = $"{Cap(container.Name)} no está cerrado con llave.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(container.KeyId))
        {
            message = $"{Cap(container.Name)} no tiene cerradura.";
            return false;
        }

        if (keyId != container.KeyId)
        {
            message = "La llave no encaja.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the room where the player is currently located.
    /// </summary>
    public Room? CurrentRoom => FindRoomById(_state.CurrentRoomId);


    /// <summary>
    /// Gets the ID of the world's background music track.
    /// </summary>
    public string? WorldMusicId => _state.WorldMusicId;




    /// <summary>
    /// Precarga las voces de la sala actual y de las salas conectadas
    /// hasta una cierta distancia en movimientos, y elimina de la caché
    /// las salas que queden más lejos.
    /// </summary>
    public async Task PreloadVoicesAroundCurrentRoomAsync(int maxDistance = 2)
    {
        var origin = CurrentRoom;
        if (origin == null)
            return;

        if (maxDistance < 0)
            maxDistance = 0;

        var roomsById = _state.Rooms
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<(string roomId, int distance)>();

        visited.Add(origin.Id);
        queue.Enqueue((origin.Id, 0));

        var toPreload = new List<Room>();

        while (queue.Count > 0)
        {
            var (roomId, distance) = queue.Dequeue();
            if (!roomsById.TryGetValue(roomId, out var room))
                continue;

            if (distance > maxDistance)
                continue;

            toPreload.Add(room);

            if (distance == maxDistance)
                continue;

            foreach (var exit in room.Exits)
            {
                if (string.IsNullOrWhiteSpace(exit.TargetRoomId))
                    continue;

                if (visited.Add(exit.TargetRoomId))
                    queue.Enqueue((exit.TargetRoomId, distance + 1));
            }
        }

        var tasks = new List<Task>();
        foreach (var room in toPreload)
        {
            tasks.Add(_sound.PreloadRoomVoiceAsync(room.Id, room.Description));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);

        var allowedIds = new HashSet<string>(toPreload.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
        var cachedIds = _sound.GetCachedVoiceRoomIds();

        foreach (var cachedId in cachedIds)
        {
            if (!allowedIds.Contains(cachedId))
                _sound.RemoveVoiceFromCache(cachedId);
        }
    }

    private void UpdateGameTimeFromReal()
    {
        var now = DateTime.Now;
        var realDelta = now - _lastRealTime;
        if (realDelta < TimeSpan.Zero)
            realDelta = TimeSpan.Zero;

        // Escalado configurable: MinutesPerGameHour minutos reales equivalen a 60 minutos de juego.
        var minutesPerGameHour = _world.Game?.MinutesPerGameHour ?? 6;
        if (minutesPerGameHour <= 0) minutesPerGameHour = 6;
        if (minutesPerGameHour > 10) minutesPerGameHour = 10;

        var previousMinute = _state.GameTime.Minute;
        var previousHour = _state.GameTime.Hour;

        double factor = 60.0 / minutesPerGameHour;
        var scaledTicks = (long)(realDelta.Ticks * factor);
        if (scaledTicks != 0)
        {
            _state.GameTime = _state.GameTime.Add(TimeSpan.FromTicks(scaledTicks));
            _lastRealTime = now;

            // Disparar eventos de tiempo si cambiaron
            var currentMinute = _state.GameTime.Minute;
            var currentHour = _state.GameTime.Hour;
            var gameId = _world.Game?.Id ?? "game";

            // Event_EveryMinute - disparar si cambió el minuto
            if (currentMinute != _lastEventMinute)
            {
                _lastEventMinute = currentMinute;
                _ = TriggerEntityScriptAsync("Game", gameId, "Event_EveryMinute");
            }

            // Event_EveryHour - disparar si cambió la hora
            if (currentHour != _lastEventHour)
            {
                _lastEventHour = currentHour;
                _ = TriggerEntityScriptAsync("Game", gameId, "Event_EveryHour");
            }
        }
    }

    /// <summary>
    /// Processes a player command and returns the result text.
    /// </summary>
    /// <param name="input">The raw command string entered by the player.</param>
    /// <returns>CommandResult con el mensaje para mostrar al jugador y si fue exitoso.</returns>
    /// <remarks>
    /// Supported commands: look, go, open, close, take, drop, talk, use, give,
    /// quests, save, load, help, and inventory.
    /// </remarks>
    public CommandResult ProcessCommand(string input)
    {
        UpdateGameTimeFromReal();

        // Usar el parser para detectar comandos compuestos y resolver pronombres
        // Ej: "coger el vaso y meterlo en el baul" -> [take vaso, put vaso in baul]
        var parsedCommands = Parser.ParseAll(input);

        CommandResult result;
        if (parsedCommands.Length > 1)
        {
            var results = new List<CommandResult>();
            foreach (var cmd in parsedCommands)
            {
                results.Add(ProcessParsedCommand(cmd));
            }
            result = CommandResult.Combine(results.ToArray());
        }
        else
        {
            result = parsedCommands.Length > 0
                ? ProcessParsedCommand(parsedCommands[0])
                : CommandResult.Empty;
        }

        // Solo contar como turno si el comando fue exitoso
        // (comandos no reconocidos o acciones fallidas no consumen turno)
        if (result.IsSuccess)
        {
            _state.TurnCounter++;

            // Disparar evento de inicio de turno
            var gameId = _world.Game?.Id ?? "game";
            _ = TriggerEntityScriptAsync("Game", gameId, "Event_OnTurnStart");

            // Actualizar patrullas de NPCs después de cada comando del jugador
            UpdateNpcPatrols();

            // Actualizar objetos luminosos encendidos (reducir turnos)
            var lightMessage = UpdateLightSources();
            if (!string.IsNullOrEmpty(lightMessage))
            {
                result = result.AppendMessage(lightMessage);
            }

            // Procesar necesidades básicas
            var needsMessage = ProcessBasicNeeds();
            if (!string.IsNullOrEmpty(needsMessage))
            {
                result = result.AppendMessage(needsMessage);
            }

            // Verificar stats vitales (salud y cordura)
            CheckVitalStats();
        }

        return result;
    }

    /// <summary>
    /// Actualiza los objetos luminosos encendidos, reduciendo sus turnos de luz.
    /// Devuelve un mensaje si algún objeto se apaga.
    /// </summary>
    private string UpdateLightSources()
    {
        var sb = new StringBuilder();

        foreach (var obj in _state.Objects)
        {
            if (!obj.IsLightSource || !obj.IsLit)
                continue;

            // Si tiene turnos infinitos (-1), no reducir
            if (obj.LightTurnsRemaining == -1)
                continue;

            // Reducir turnos
            obj.LightTurnsRemaining--;

            // Si llegó a 0, apagar el objeto
            if (obj.LightTurnsRemaining <= 0)
            {
                obj.IsLit = false;
                obj.LightTurnsRemaining = 0;
                sb.AppendLine(RandomMessages.GetLightGoesOut(Cap(obj.Name), obj.Gender, obj.IsPlural));
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Processes an already-parsed command.
    /// Used for compound commands where parsing has already been done.
    /// </summary>
    private CommandResult ProcessParsedCommand(ParsedCommand parsedCmd)
    {
        // Manejar "?" directamente
        if (parsedCmd.Verb == "?")
        {
            HelpRequested?.Invoke();
            return CommandResult.Empty;
        }

        // Si hay una conversación activa, manejar opciones de diálogo
        if (IsConversationActive)
        {
            // Permitir escribir el número directamente (1, 2, 3, 4) o "opcion N" / "decir N"
            var numStr = parsedCmd.DirectObject ?? parsedCmd.Verb ?? "";
            if (int.TryParse(numStr, out int optNum) && optNum >= 1 && optNum <= 4)
            {
                _ = HandleConversationOptionAsync(optNum - 1);
                return CommandResult.Empty;
            }

            // Permitir salir de la conversación con "salir", "exit", "terminar", "adios"
            var exitWords = new[] { "salir", "exit", "terminar", "adios", "adiós", "go" };
            var inputLower = (parsedCmd.Verb ?? "").ToLowerInvariant();
            if (exitWords.Contains(inputLower) ||
                exitWords.Contains((parsedCmd.DirectObject ?? "").ToLowerInvariant()))
            {
                _conversationEngine?.EndConversation();
                return CommandResult.Success(RandomMessages.EndConversation);
            }

            return CommandResult.Error("Escribe el número de la opción (1-4), o 'salir' para terminar.");
        }

        if (string.IsNullOrEmpty(parsedCmd.Verb))
            return CommandResult.Empty;

        // Debug: mostrar el verbo parseado (solo en modo debug)
        if (_isDebugMode)
            ScriptMessage?.Invoke($"[Debug] Verbo parseado: '{parsedCmd.Verb}', DirectObject: '{parsedCmd.DirectObject}'");

        return parsedCmd.Verb switch
        {
            "examine" => HandleExamine(parsedCmd),
            "go" => HandleGo(parsedCmd),
            "open" => HandleOpen(parsedCmd),
            "close" => HandleClose(parsedCmd),
            "unlock" => HandleUnlock(parsedCmd),
            "lock" => HandleLock(parsedCmd),
            "put" => HandlePutIn(parsedCmd),
            "get_from" => HandleGetFrom(parsedCmd),
            "look_in" => HandleLookIn(parsedCmd),
            "inventory" => CommandResult.Success(DescribeInventory()),
            "take" => HandleTake(parsedCmd),
            "drop" => HandleDrop(parsedCmd),
            "talk" or "say" or "option" => HandleTalk(parsedCmd),
            "use" => HandleUse(parsedCmd),
            "give" => HandleGive(parsedCmd),
            "read" => HandleRead(parsedCmd),
            "quests" => CommandResult.Success(DescribeQuests()),
            "wait" => HandleWait(),
            "attack" => HandleAttack(parsedCmd),
            "equip" => HandleEquip(parsedCmd),
            "unequip" => HandleUnequip(parsedCmd),
            "loot" => HandleLoot(parsedCmd),
            "equipment" => CommandResult.Success(DescribeEquipment()),
            "ignite" => HandleIgnite(parsedCmd),
            "extinguish" => HandleExtinguish(parsedCmd),
            "craft" => HandleCraft(parsedCmd),
            "eat" => HandleEat(parsedCmd),
            "drink" => HandleDrink(parsedCmd),
            "sleep" => HandleSleep(parsedCmd),
            "save" => CommandResult.Success("Usa el menú Archivo -> Guardar partida... para guardar."),
            "load" => CommandResult.Success("Usa el menú Archivo -> Cargar partida... para cargar."),
            "help" or "commands" => HandleHelp(),
            _ => CommandResult.Error(RandomMessages.UnknownCommand)
        };
    }

    /// <summary>
    /// Verifica si la sala actual está iluminada.
    /// </summary>
    public bool IsCurrentRoomLit => CurrentRoom != null && IsRoomLit(CurrentRoom);

    /// <summary>
    /// Determina si la sala tiene iluminación suficiente para que el jugador pueda ver.
    /// </summary>
    private bool IsRoomLit(Room room)
    {
        var timeOfDay = _state.GameTime.TimeOfDay;
        bool isNight = timeOfDay.Hours >= 20 || timeOfDay.Hours < 7;

        // Determinar iluminación base de la sala
        bool baseIllumination;
        if (room.IsInterior)
        {
            // En interiores la iluminación depende del propio flag de la sala.
            baseIllumination = room.IsIlluminated;
        }
        else
        {
            // En exteriores depende de si es de día o de noche (oscuro de 20:00 a 7:00).
            baseIllumination = !isNight;
        }

        // Si la sala ya está iluminada, no necesitamos buscar fuentes de luz
        if (baseIllumination)
            return true;

        // Buscar objetos luminosos encendidos que iluminen la sala
        return HasActiveLightSource(room);
    }

    /// <summary>
    /// Determina si hay algún objeto luminoso encendido que ilumine la sala.
    /// Considera objetos en la sala (no en contenedores cerrados sin visibilidad) y en el inventario del jugador.
    /// </summary>
    private bool HasActiveLightSource(Room room)
    {
        // Verificar objetos luminosos en el inventario del jugador
        foreach (var objId in _state.InventoryObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null && obj.IsLightSource && obj.IsLit)
                return true;
        }

        // Verificar objetos luminosos en la sala
        foreach (var objId in room.ObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj == null) continue;

            // Si el objeto es luminoso y está encendido
            if (obj.IsLightSource && obj.IsLit)
                return true;

            // Si el objeto es un contenedor, verificar su contenido
            if (obj.IsContainer)
            {
                // El contenido solo ilumina si el contenedor está abierto o tiene contenido visible
                if (obj.IsOpen || obj.ContentsVisible)
                {
                    foreach (var containedId in obj.ContainedObjectIds)
                    {
                        var containedObj = FindObjectById(containedId);
                        if (containedObj != null && containedObj.IsLightSource && containedObj.IsLit)
                            return true;
                    }
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Generates a text description of the current room.
    /// Includes visible objects, NPCs, and available exits.
    /// </summary>
    /// <returns>The room description text.</returns>
    public string DescribeCurrentRoom()
    {
        var room = CurrentRoom;
        if (room == null)
            return "Te encuentras en un lugar desconocido.";

        var sb = new StringBuilder();

        if (!IsRoomLit(room))
        {
            sb.AppendLine(RandomMessages.TooDarkToSee);
            return sb.ToString().TrimEnd();
        }

        sb.AppendLine(room.Description);

        // Objetos visibles en la sala (no en inventario)
        var visibleObjects = _state.Objects
            .Where(o => o.Visible && _state.InventoryObjectIds.All(id => !id.Equals(o.Id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // IDs de objetos contenidos en otros (para excluirlos del nivel superior)
        var containedIds = _state.Objects
            .Where(o => o.IsContainer && o.ContainedObjectIds.Any())
            .SelectMany(o => o.ContainedObjectIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Objetos de nivel superior (no contenidos en ningún contenedor)
        var topLevelObjects = visibleObjects
            .Where(o => !containedIds.Contains(o.Id))
            .ToList();

        if (topLevelObjects.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Ves aquí:");
            foreach (var obj in topLevelObjects)
            {
                sb.AppendLine($" - {Cap(obj.Name)}");
                // Mostrar contenido si: está abierto O tiene contenido visible (ej: vitrina cerrada)
                if (obj.IsContainer && (obj.IsOpen || obj.ContentsVisible) && obj.ContainedObjectIds.Any())
                {
                    foreach (var containedId in obj.ContainedObjectIds)
                    {
                        var contained = _state.Objects.FirstOrDefault(o =>
                            o.Id.Equals(containedId, StringComparison.OrdinalIgnoreCase) && o.Visible);
                        if (contained != null)
                            sb.AppendLine($"     └ {Cap(contained.Name)}");
                    }
                }
            }
        }

        // NPCs visibles
        var visibleNpcs = _state.Npcs
            .Where(n => n.Visible && room.NpcIds.Contains(n.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        if (visibleNpcs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("Personajes presentes:");
            foreach (var npc in visibleNpcs)
                sb.AppendLine($" - {Cap(npc.Name)}");
        }

        return sb.ToString().TrimEnd();
    }

    private static string GetDisplayDirection(string normalizedDir)
    {
        return normalizedDir switch
        {
            "n" => "norte",
            "s" => "sur",
            "e" => "este",
            "o" => "oeste",
            "ne" => "noreste",
            "no" => "noroeste",
            "se" => "sureste",
            "so" => "suroeste",
            "up" => "arriba",
            "down" => "abajo",
            _ => normalizedDir
        };
    }


    /// <summary>
    /// Describes the player's equipped items (summary for status panel).
    /// </summary>
    /// <returns>A formatted list of equipped items by slot.</returns>
    public string DescribeEquipmentSummary()
    {
        var sb = new StringBuilder();

        var rightHand = !string.IsNullOrEmpty(_state.Player.EquippedRightHandId)
            ? FindObjectById(_state.Player.EquippedRightHandId)
            : null;
        var leftHand = !string.IsNullOrEmpty(_state.Player.EquippedLeftHandId)
            ? FindObjectById(_state.Player.EquippedLeftHandId)
            : null;
        var torso = !string.IsNullOrEmpty(_state.Player.EquippedTorsoId)
            ? FindObjectById(_state.Player.EquippedTorsoId)
            : null;
        var head = !string.IsNullOrEmpty(_state.Player.EquippedHeadId)
            ? FindObjectById(_state.Player.EquippedHeadId)
            : null;

        sb.AppendLine($"Cabeza: {(head != null ? Cap(head.Name) : "-")}");
        sb.AppendLine($"Mano derecha: {(rightHand != null ? Cap(rightHand.Name) : "-")}");
        // Solo mostrar mano izquierda si no es arma de 2 manos
        if (rightHand == null || _state.Player.EquippedLeftHandId != _state.Player.EquippedRightHandId)
        {
            sb.AppendLine($"Mano izquierda: {(leftHand != null ? Cap(leftHand.Name) : "-")}");
        }
        sb.AppendLine($"Torso: {(torso != null ? Cap(torso.Name) : "-")}");

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Lists the items currently in the player's inventory.
    /// </summary>
    /// <returns>A formatted list of inventory items, or a message if empty.</returns>
    public string DescribeInventory()
    {
        var sb = new StringBuilder();

        if (!_state.InventoryObjectIds.Any())
        {
            sb.AppendLine("(vacío)");
        }
        else
        {
            // Agrupar objetos por ID y contar
            var groupedItems = _state.InventoryObjectIds
                .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .ToList();

            foreach (var group in groupedItems)
            {
                var obj = FindObjectById(group.Id);
                if (obj != null)
                {
                    var countSuffix = group.Count > 1 ? $" (x{group.Count})" : "";

                    if (obj.IsLightSource && obj.IsLit)
                    {
                        var turnsDisplay = obj.LightTurnsRemaining == -1 ? "∞" : obj.LightTurnsRemaining.ToString();
                        sb.AppendLine($"- {Cap(obj.Name)} ({turnsDisplay}){countSuffix}");
                    }
                    else
                        sb.AppendLine($"- {Cap(obj.Name)}{countSuffix}");
                }
            }
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Describe las puertas de la sala actual con su estado.
    /// </summary>
    public string DescribeDoorsInCurrentRoom()
    {
        var room = CurrentRoom;
        if (room == null)
            return "No hay información de puertas.";

        var doors = GetAllDoorsInRoom(room)
            .Where(d => IsDoorVisible(d.Door))
            .ToList();

        if (doors.Count == 0)
            return "No hay puertas en esta sala.";

        var sb = new StringBuilder();
        foreach (var (door, direction) in doors)
        {
            var estado = door.IsOpen ? "abierta" : "cerrada";
            var conLlave = door.IsLocked ? " (con llave)" : "";
            sb.AppendLine($"- {door.Name} al {direction}: {estado}{conLlave}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Generates a text summary of the player's stats.
    /// Includes the 5 characteristics and gold.
    /// </summary>
    /// <returns>The player stats text.</returns>
    public string DescribePlayerStats()
    {
        var p = _state.Player;
        var sb = new StringBuilder();
        sb.AppendLine($"Fuerza: {p.Strength}");
        sb.AppendLine($"Constitución: {p.Constitution}");
        sb.AppendLine($"Inteligencia: {p.Intelligence}");
        sb.AppendLine($"Destreza: {p.Dexterity}");
        sb.AppendLine($"Carisma: {p.Charisma}");
        return sb.ToString().TrimEnd();
    }

    public string DescribePlayerMoney()
    {
        return _state.Player.Money.ToString("N0");
    }

    /// <summary>
    /// Generates a text summary of the current room exits.
    /// </summary>
    /// <returns>The exits text.</returns>
    public string DescribeExits()
    {
        var room = CurrentRoom;
        if (room == null)
            return "Ninguna";

        var allExits = new List<(string Direction, string? DoorId, bool IsLocked)>();

        // Salidas directas definidas en esta sala
        foreach (var exit in room.Exits)
        {
            // Si hay puerta asociada, verificar si es visible
            if (!string.IsNullOrEmpty(exit.DoorId))
            {
                var door = _state.Doors.FirstOrDefault(d =>
                    d.Id.Equals(exit.DoorId, StringComparison.OrdinalIgnoreCase));
                if (door != null && !IsDoorVisible(door))
                    continue; // Saltar salida con puerta invisible
            }
            allExits.Add((exit.Direction, exit.DoorId, exit.IsLocked));
        }

        // Salidas inversas: otras salas que tienen salidas apuntando a esta sala
        var directDirections = new HashSet<string>(
            room.Exits
                .Where(e =>
                {
                    if (string.IsNullOrEmpty(e.DoorId)) return true;
                    var door = _state.Doors.FirstOrDefault(d =>
                        d.Id.Equals(e.DoorId, StringComparison.OrdinalIgnoreCase));
                    return door == null || IsDoorVisible(door);
                })
                .Select(e => NormalizeDirection(e.Direction)),
            StringComparer.OrdinalIgnoreCase);

        foreach (var candidateRoom in _state.Rooms)
        {
            if (candidateRoom.Id.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var candidateExit in candidateRoom.Exits)
            {
                if (!candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Si hay puerta asociada, verificar si es visible
                if (!string.IsNullOrEmpty(candidateExit.DoorId))
                {
                    var door = _state.Doors.FirstOrDefault(d =>
                        d.Id.Equals(candidateExit.DoorId, StringComparison.OrdinalIgnoreCase));
                    if (door != null && !IsDoorVisible(door))
                        continue; // Saltar salida con puerta invisible
                }

                var normCandidate = NormalizeDirection(candidateExit.Direction);
                var opposite = GetOppositeDirectionCode(normCandidate);

                if (!directDirections.Contains(opposite))
                {
                    var displayDir = GetDisplayDirection(opposite);
                    allExits.Add((displayDir, candidateExit.DoorId, candidateExit.IsLocked));
                    directDirections.Add(opposite);
                }
            }
        }

        if (allExits.Count == 0)
            return "Ninguna";

        var sb = new StringBuilder();
        foreach (var (dir, doorId, isLocked) in allExits)
        {
            var doorInfo = "";
            if (!string.IsNullOrEmpty(doorId))
            {
                var door = _state.Doors.FirstOrDefault(d =>
                    d.Id.Equals(doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    var doorName = string.IsNullOrWhiteSpace(door.Name) ? "puerta" : Low(door.Name);
                    var doorState = door.IsOpen ? "abierta" : "cerrada";
                    doorInfo = $" ({doorName} {doorState})";
                }
            }

            if (isLocked)
                sb.AppendLine($"• {Cap(dir)} (bloqueada){doorInfo}");
            else
                sb.AppendLine($"• {Cap(dir)}{doorInfo}");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Maneja el comando de movimiento del jugador en una dirección especificada.
    /// Soporta movimiento bidireccional: busca salidas directas y también permite
    /// regresar por salidas inversas (si hay una sala con salida hacia aquí, se puede volver).
    /// </summary>
    private CommandResult HandleGo(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var dir = parsed.DirectObject ?? string.Empty;
        dir = dir.ToLowerInvariant();

        if (string.IsNullOrEmpty(dir))
            return CommandResult.Error(RandomMessages.WhereToGo);

        var normalizedRequested = NormalizeDirection(dir);

        // Primero intentamos encontrar una salida definida en la sala actual.
        Exit? exit = room.Exits.FirstOrDefault(e =>
            string.Equals(NormalizeDirection(e.Direction), normalizedRequested, StringComparison.OrdinalIgnoreCase));

        Room? targetRoom = null;

        if (exit != null)
        {
            // Salida directa encontrada
            targetRoom = _state.Rooms.FirstOrDefault(r =>
                r.Id.Equals(exit.TargetRoomId, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            // Si no hay salida directa, intentamos una conexión inversa:
            // Buscamos alguna otra sala que tenga una salida hacia la sala actual,
            // cuya dirección opuesta coincida con la dirección que el jugador ha pedido.
            // Ejemplo: Si estamos en B y A tiene salida "este" hacia B,
            // el jugador puede ir "oeste" desde B hacia A sin que B defina esa salida.
            Exit? reverseExit = null;
            Room? sourceRoom = null;

            foreach (var candidateRoom in _state.Rooms)
            {
                foreach (var candidateExit in candidateRoom.Exits)
                {
                    var normCandidate = NormalizeDirection(candidateExit.Direction);
                    var opposite = GetOppositeDirectionCode(normCandidate);

                    // Si la dirección opuesta de esta salida coincide con lo pedido
                    // y apunta a nuestra sala actual, entonces podemos usarla
                    if (string.Equals(opposite, normalizedRequested, StringComparison.OrdinalIgnoreCase) &&
                        candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        reverseExit = candidateExit;
                        sourceRoom = candidateRoom;
                        break;
                    }
                }

                if (reverseExit != null)
                    break;
            }

            if (reverseExit != null && sourceRoom != null)
            {
                exit = reverseExit;
                targetRoom = sourceRoom;
            }
        }

        if (exit == null || targetRoom == null)
            return CommandResult.Error(RandomMessages.CannotGoThatWay);

        // Si la salida está asociada a una puerta, verificar visibilidad y estado.
        if (!string.IsNullOrEmpty(exit.DoorId))
        {
            var door = _state.Doors.FirstOrDefault(d => d.Id.Equals(exit.DoorId, StringComparison.OrdinalIgnoreCase));
            if (door != null)
            {
                if (!IsDoorVisible(door))
                    return CommandResult.Error(RandomMessages.CannotGoThatWay);
                if (!door.IsOpen)
                    return CommandResult.Error(RandomMessages.DoorIsLocked);
            }
        }
        else if (exit.IsLocked)
        {
            return CommandResult.Error(RandomMessages.ExitBlocked);
        }

        // Verificar requisitos de misiones de la sala destino
        if (targetRoom.RequiredQuests.Count > 0)
        {
            foreach (var requirement in targetRoom.RequiredQuests)
            {
                var quest = _state.Quests.Values.FirstOrDefault(q =>
                    q.QuestId.Equals(requirement.QuestId, StringComparison.OrdinalIgnoreCase));
                if (quest == null || quest.Status != requirement.RequiredStatus)
                {
                    return CommandResult.Error("No puedes acceder a esta zona todavía.");
                }
            }
        }

        // Disparar Event_OnExit de la sala actual antes de salir
        _ = TriggerRoomScriptsAsync(room.Id, "Event_OnExit");

        _state.CurrentRoomId = targetRoom.Id;

        // NPCs que siguen al jugador se mueven a la nueva sala
        UpdateFollowingNpcs(targetRoom.Id);

        WorldLoader.RebuildRoomIndexes(_state); // por si algún script ha cambiado cosas
        OnRoomChanged();
        return CommandResult.Success(""); // La descripción se muestra en el área fija superior
    }



    private Door? FindDoorInCurrentRoomByName(Room room, string arg)
    {
        if (string.IsNullOrWhiteSpace(arg))
            return null;

        return _state.Doors.FirstOrDefault(d =>
            (string.Equals(d.RoomIdA, room.Id, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(d.RoomIdB, room.Id, StringComparison.OrdinalIgnoreCase)) &&
            !string.IsNullOrEmpty(d.Name) &&
            d.Name.Contains(arg, StringComparison.OrdinalIgnoreCase));
    }

    private CommandResult HandleOpen(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = (parsed.DirectObject ?? string.Empty).Trim();
        var originalArg = parsed.OriginalDirectObject;
        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToOpen);

        // Primero intentar con objetos contenedores
        var obj = FindObjectInRoomOrInventory(room, arg, originalArg);
        if (obj != null && obj.IsContainer)
        {
            if (CanOpenContainer(obj, out string message))
            {
                // Guardar si el contenido estaba oculto antes de abrir
                bool wasContentHidden = !obj.ContentsVisible && !obj.IsOpen;

                obj.IsOpen = true;
                // Disparar evento de contenedor abierto
                _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnContainerOpen");
                // Preparar mensaje de contenidos si estaban ocultos
                string? contentsMessage = null;
                if (wasContentHidden && obj.ContainedObjectIds.Any())
                {
                    var contents = obj.ContainedObjectIds
                        .Select(id => _state.Objects.FirstOrDefault(o =>
                            string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)))
                        .Where(o => o != null && o.Visible)
                        .Select(o => Low(o!.Name))
                        .ToList();

                    if (contents.Any())
                    {
                        contentsMessage = $"Dentro encuentras: {string.Join(", ", contents)}.";
                    }
                }

                // Si hay script con mensaje personalizado, añadir contenidos después
                if (HasScriptWithMessage("GameObject", obj.Id, "Event_OnContainerOpen"))
                {
                    if (contentsMessage != null)
                        ScriptMessage?.Invoke(contentsMessage);
                    return CommandResult.Empty;
                }

                // Sin script: mostrar mensaje por defecto con contenidos
                var openMessage = $"Abres {Low(obj.Name)}.";
                if (contentsMessage != null)
                    openMessage += "\n" + contentsMessage;
                return CommandResult.Success(openMessage);
            }
            return CommandResult.Error(message);
        }

        // Buscar puerta
        var (door, errorMsg) = FindDoorByArgument(room, arg);
        if (door == null)
            return CommandResult.Error(errorMsg ?? RandomMessages.NoDoorThere);

        // Solo los objetos del inventario sirven como llaves
        var result = _doorService.TryOpenDoor(door.Id, room.Id, _state.InventoryObjectIds);

        if (result.MessageKey == "door_opened")
        {
            // Disparar evento de puerta abierta
            _ = TriggerEntityScriptAsync("Door", door.Id, "Event_OnDoorOpen");
            return CommandResult.Success(GetDoorOpenedMessage(door));
        }

        return result.MessageKey switch
        {
            "door_wrong_side" => CommandResult.Error("No puedes abrir la puerta desde este lado."),
            "door_requires_key" => CommandResult.Error(RandomMessages.DoorIsLocked),
            "door_already_open" => CommandResult.Error(RandomMessages.DoorAlreadyOpen),
            _ => CommandResult.Error(RandomMessages.NoDoorThere)
        };
    }

    private string GetDoorOpenedMessage(Door door)
    {
        if (!string.IsNullOrWhiteSpace(door.KeyObjectId))
        {
            var keyObj = FindObjectById(door.KeyObjectId);
            if (keyObj != null)
                return $"Abres {Low(door.Name)} con {Low(keyObj.Name)}.";
        }
        return $"Abres {Low(door.Name)}.";
    }

    private CommandResult HandleClose(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = (parsed.DirectObject ?? string.Empty).Trim();
        var originalArg = parsed.OriginalDirectObject;
        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToClose);

        // Primero intentar con objetos contenedores
        var obj = FindObjectInRoomOrInventory(room, arg, originalArg);
        if (obj != null && obj.IsContainer)
        {
            if (CanCloseContainer(obj, out string message))
            {
                obj.IsOpen = false;
                // Disparar evento de contenedor cerrado
                _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnContainerClose");
                // Solo mostrar mensaje por defecto si no hay script con mensaje personalizado
                if (HasScriptWithMessage("GameObject", obj.Id, "Event_OnContainerClose"))
                    return CommandResult.Empty;
                return CommandResult.Success($"Cierras {Low(obj.Name)}.");
            }
            return CommandResult.Error(message);
        }

        // Buscar puerta
        var (door, errorMsg) = FindDoorByArgument(room, arg);
        if (door == null)
            return CommandResult.Error(errorMsg ?? RandomMessages.NoDoorThere);

        // Solo los objetos del inventario sirven como llaves
        var result = _doorService.TryCloseDoor(door.Id, room.Id, _state.InventoryObjectIds);

        if (result.MessageKey == "door_closed")
        {
            // Disparar evento de puerta cerrada
            _ = TriggerEntityScriptAsync("Door", door.Id, "Event_OnDoorClose");
            return CommandResult.Success(GetDoorClosedMessage(door));
        }

        return result.MessageKey switch
        {
            "door_wrong_side" => CommandResult.Error("No puedes cerrar la puerta desde este lado."),
            "door_requires_key" => CommandResult.Error("No tienes la llave necesaria para cerrar esta puerta."),
            "door_already_closed" => CommandResult.Error(RandomMessages.DoorAlreadyClosed),
            _ => CommandResult.Error(RandomMessages.NoDoorThere)
        };
    }

    private string GetDoorClosedMessage(Door door)
    {
        if (!string.IsNullOrWhiteSpace(door.KeyObjectId))
        {
            var keyObj = FindObjectById(door.KeyObjectId);
            if (keyObj != null)
                return $"Cierras {Low(door.Name)} con {Low(keyObj.Name)}.";
        }
        return $"Cierras {Low(door.Name)}.";
    }

    /// <summary>
    /// Busca una puerta basándose en el argumento del jugador.
    /// Soporta: nombre de puerta, dirección, "puerta norte", "puerta del norte", etc.
    /// </summary>
    private (Door? door, string? errorMessage) FindDoorByArgument(Room room, string arg)
    {
        // 1) Si el argumento es "puerta" genérico sin dirección, comprobar cuántas puertas hay
        if (IsDoorWord(arg))
        {
            var allDoors = GetAllDoorsInRoom(room);
            if (allDoors.Count == 0)
                return (null, RandomMessages.NoDoorsHere);
            if (allDoors.Count == 1)
                return (allDoors[0].Door, null);

            // Múltiples puertas: pedir especificar
            var directions = string.Join(", ", allDoors.Select(d => d.Direction));
            return (null, $"Hay varias puertas aquí. Especifica cuál: {directions}.");
        }

        // 2) Extraer dirección del argumento (ej: "puerta norte", "puerta del este", "norte")
        var direction = ExtractDirectionFromArg(arg);

        // 3) Si hay dirección, buscar puerta en esa dirección
        if (!string.IsNullOrEmpty(direction))
        {
            var door = FindDoorByDirection(room, direction);
            if (door != null)
                return (door, null);
            return (null, $"No hay ninguna puerta en esa dirección.");
        }

        // 4) Buscar puerta por nombre
        var doorByName = FindDoorInCurrentRoomByName(room, arg);
        if (doorByName != null)
            return (doorByName, null);

        return (null, RandomMessages.NoDoorThere);
    }

    /// <summary>
    /// Comprueba si el argumento es una palabra que significa "puerta".
    /// </summary>
    private static bool IsDoorWord(string arg)
    {
        var lower = arg.ToLowerInvariant();
        return lower == "puerta" || lower == "la puerta" || lower == "una puerta";
    }

    /// <summary>
    /// Extrae la dirección de un argumento como "puerta norte", "puerta del este", etc.
    /// </summary>
    private static string? ExtractDirectionFromArg(string arg)
    {
        var lower = arg.ToLowerInvariant().Trim();

        // Patrones: "puerta norte", "puerta del norte", "puerta al norte", "la puerta norte", etc.
        var patterns = new[] { "puerta del ", "puerta al ", "puerta de ", "puerta ", "la puerta del ", "la puerta al ", "la puerta de ", "la puerta " };
        foreach (var pattern in patterns)
        {
            if (lower.StartsWith(pattern))
            {
                var dir = lower.Substring(pattern.Length).Trim();
                if (!string.IsNullOrEmpty(dir))
                    return dir;
            }
        }

        // Si es directamente una dirección
        var normalized = NormalizeDirection(lower);
        if (normalized != lower || IsKnownDirection(lower))
            return lower;

        return null;
    }

    private static bool IsKnownDirection(string dir)
    {
        var known = new[] { "norte", "sur", "este", "oeste", "noreste", "noroeste", "sureste", "suroeste", "arriba", "abajo", "subir", "bajar", "n", "s", "e", "o", "ne", "no", "se", "so", "up", "down" };
        return known.Contains(dir.ToLowerInvariant());
    }

    /// <summary>
    /// Determina si el objetivo de "mirar" se refiere a la sala/habitación/alrededor.
    /// </summary>
    private static bool IsRoomLookTarget(string target)
    {
        var roomWords = new[] { "sala", "habitacion", "habitación", "cuarto", "lugar", "alrededor", "entorno", "aqui", "aquí", "room" };
        return roomWords.Contains(target.ToLowerInvariant());
    }

    /// <summary>
    /// Busca una puerta en una dirección específica (directa o inversa).
    /// </summary>
    private Door? FindDoorByDirection(Room room, string direction)
    {
        var normalizedDir = NormalizeDirection(direction);

        // Buscar en salidas directas
        var exit = room.Exits.FirstOrDefault(e =>
            string.Equals(NormalizeDirection(e.Direction), normalizedDir, StringComparison.OrdinalIgnoreCase));

        if (exit != null && !string.IsNullOrEmpty(exit.DoorId))
        {
            return _state.Doors.FirstOrDefault(d => d.Id.Equals(exit.DoorId, StringComparison.OrdinalIgnoreCase));
        }

        // Buscar en salidas inversas
        foreach (var candidateRoom in _state.Rooms)
        {
            if (candidateRoom.Id.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var candidateExit in candidateRoom.Exits)
            {
                if (!candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                var opposite = GetOppositeDirectionCode(NormalizeDirection(candidateExit.Direction));
                if (string.Equals(opposite, normalizedDir, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(candidateExit.DoorId))
                {
                    return _state.Doors.FirstOrDefault(d => d.Id.Equals(candidateExit.DoorId, StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Obtiene todas las puertas accesibles desde una sala (directas e inversas).
    /// </summary>
    private List<(Door Door, string Direction)> GetAllDoorsInRoom(Room room)
    {
        var result = new List<(Door Door, string Direction)>();
        var addedDoorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Puertas de salidas directas
        foreach (var exit in room.Exits)
        {
            if (string.IsNullOrEmpty(exit.DoorId))
                continue;

            var door = _state.Doors.FirstOrDefault(d => d.Id.Equals(exit.DoorId, StringComparison.OrdinalIgnoreCase));
            if (door != null && addedDoorIds.Add(door.Id))
            {
                result.Add((door, exit.Direction));
            }
        }

        // Puertas de salidas inversas
        foreach (var candidateRoom in _state.Rooms)
        {
            if (candidateRoom.Id.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (var candidateExit in candidateRoom.Exits)
            {
                if (!candidateExit.TargetRoomId.Equals(room.Id, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.IsNullOrEmpty(candidateExit.DoorId))
                    continue;

                var door = _state.Doors.FirstOrDefault(d => d.Id.Equals(candidateExit.DoorId, StringComparison.OrdinalIgnoreCase));
                if (door != null && addedDoorIds.Add(door.Id))
                {
                    var opposite = GetOppositeDirectionCode(NormalizeDirection(candidateExit.Direction));
                    result.Add((door, GetDisplayDirection(opposite)));
                }
            }
        }

        return result;
    }

    private CommandResult HandleUnlock(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = (parsed.DirectObject ?? string.Empty).Trim();
        var originalArg = parsed.OriginalDirectObject;
        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToUnlock);

        var obj = FindObjectInRoomOrInventory(room, arg, originalArg);
        if (obj == null || !obj.IsContainer)
            return CommandResult.Error(RandomMessages.NoSuchContainer);

        // Buscar la llave en el inventario
        var key = _state.InventoryObjectIds
            .Select(FindObjectById)
            .FirstOrDefault(k => k != null && k.Id == obj.KeyId);

        if (CanUnlockContainer(obj, key?.Id, out string message))
        {
            obj.IsLocked = false;
            // Disparar evento de desbloqueo (se usa el mismo evento que para puertas)
            _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnDoorUnlock");
            return CommandResult.Success($"Desbloqueas {Low(obj.Name)} con {Low(key?.Name ?? "")}.");
        }

        return CommandResult.Error(message);
    }

    private CommandResult HandleLock(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = (parsed.DirectObject ?? string.Empty).Trim();
        var originalArg = parsed.OriginalDirectObject;
        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToLock);

        var obj = FindObjectInRoomOrInventory(room, arg, originalArg);
        if (obj == null || !obj.IsContainer)
            return CommandResult.Error(RandomMessages.NoSuchContainer);

        if (obj.IsLocked)
            return CommandResult.Error($"{Cap(obj.Name)} ya está bloqueado.");

        if (string.IsNullOrWhiteSpace(obj.KeyId))
            return CommandResult.Error($"{Cap(obj.Name)} no tiene cerradura.");

        // Buscar la llave en el inventario
        var key = _state.InventoryObjectIds
            .Select(FindObjectById)
            .FirstOrDefault(k => k != null && k.Id == obj.KeyId);

        if (key == null)
            return CommandResult.Error(RandomMessages.NoKeyForDoor);

        obj.IsLocked = true;
        // Disparar evento de bloqueo
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnDoorLock");
        return CommandResult.Success($"Bloqueas {Low(obj.Name)} con {Low(key.Name)}.");
    }

    private CommandResult HandlePutIn(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Necesitamos parsear "meter X en Y" - DirectObject es X, Preposition + IndirectObject es "en Y"
        var objectName = (parsed.DirectObject ?? string.Empty).Trim();
        var originalObjectName = parsed.OriginalDirectObject;
        var containerName = (parsed.IndirectObject ?? string.Empty).Trim();
        var originalContainerName = parsed.OriginalIndirectObject;

        if (string.IsNullOrEmpty(objectName))
            return CommandResult.Error(RandomMessages.WhatToPutIn);

        if (string.IsNullOrEmpty(containerName))
            return CommandResult.Error(RandomMessages.WhereToPutIt);

        // Buscar el objeto a meter (puede estar en el inventario o en la sala)
        var objToInsert = FindObjectInRoomOrInventory(room, objectName, originalObjectName);

        if (objToInsert == null)
            return CommandResult.Error("No ves ese objeto por aquí.");

        // Verificar que el objeto está en inventario o en la sala (no en otro contenedor)
        var isInInventory = _state.InventoryObjectIds.Contains(objToInsert.Id);
        var isInRoom = string.Equals(objToInsert.RoomId, room.Id, StringComparison.OrdinalIgnoreCase);

        if (!isInInventory && !isInRoom)
            return CommandResult.Error("No puedes coger ese objeto.");

        // Buscar el contenedor
        var container = FindObjectInRoomOrInventory(room, containerName, originalContainerName);
        if (container == null || !container.IsContainer)
            return CommandResult.Error(RandomMessages.NoSuchContainer);

        if (container.IsOpenable && !container.IsOpen)
            return CommandResult.Error(RandomMessages.GetContainerIsClosed(Cap(container.Name), container.Gender, container.IsPlural));

        // Verificar capacidad por volumen
        if (container.MaxCapacity > 0)
        {
            var currentVolume = container.ContainedObjectIds
                .Select(FindObjectById)
                .Where(o => o != null)
                .Sum(o => o!.Volume);

            if (currentVolume + objToInsert.Volume > container.MaxCapacity)
                return CommandResult.Error($"{WithArticleCap(objToInsert)} no cabe en {WithArticle(container)}.");
        }

        // Mover el objeto al contenedor (desde inventario o sala)
        if (isInInventory)
            _state.InventoryObjectIds.Remove(objToInsert.Id);

        container.ContainedObjectIds.Add(objToInsert.Id);
        objToInsert.RoomId = null; // El objeto ya no está en una sala

        return CommandResult.Success($"Metes {WithArticle(objToInsert)} en {WithArticle(container)}.");
    }

    private CommandResult HandleGetFrom(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var objectName = (parsed.DirectObject ?? string.Empty).Trim();
        var originalObjectName = parsed.OriginalDirectObject;
        var containerName = (parsed.IndirectObject ?? string.Empty).Trim();
        var originalContainerName = parsed.OriginalIndirectObject;

        if (string.IsNullOrEmpty(objectName))
            return CommandResult.Error(RandomMessages.WhatToGetFrom);

        if (string.IsNullOrEmpty(containerName))
            return CommandResult.Error(RandomMessages.WhereToGetFrom);

        // Buscar el contenedor
        var container = FindObjectInRoomOrInventory(room, containerName, originalContainerName);
        if (container == null || !container.IsContainer)
            return CommandResult.Error(RandomMessages.NoSuchContainer);

        if (container.IsOpenable && !container.IsOpen && !container.ContentsVisible)
            return CommandResult.Error(RandomMessages.GetContainerIsClosed(Cap(container.Name), container.Gender, container.IsPlural));

        // Buscar el objeto dentro del contenedor
        var objToExtract = container.ContainedObjectIds
            .Select(FindObjectById)
            .FirstOrDefault(o => o != null && MatchesName(o.Name, objectName, originalObjectName));

        if (objToExtract == null)
            return CommandResult.Error($"No hay ningún {objectName} en {WithArticle(container)}.");

        // Mover el objeto del contenedor a la sala
        container.ContainedObjectIds.Remove(objToExtract.Id);
        objToExtract.RoomId = room.Id;

        return CommandResult.Success($"Sacas {WithArticle(objToExtract)} de {WithArticle(container)}.");
    }

    private CommandResult HandleLookIn(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var containerName = (parsed.DirectObject ?? string.Empty).Trim();
        var originalContainerName = parsed.OriginalDirectObject;
        if (string.IsNullOrEmpty(containerName))
            return CommandResult.Error(RandomMessages.WhatToLookIn);

        var container = FindObjectInRoomOrInventory(room, containerName, originalContainerName);
        if (container == null || !container.IsContainer)
            return CommandResult.Error(RandomMessages.NoSuchContainer);

        if (container.IsOpenable && !container.IsOpen && !container.ContentsVisible)
            return CommandResult.Error($"{Cap(container.Name)} está cerrado y no puedes ver su interior.");

        if (container.ContainedObjectIds.Count == 0)
            return CommandResult.Success(RandomMessages.GetContainerEmpty(Cap(container.Name), container.Gender, container.IsPlural));

        var sb = new StringBuilder();
        sb.AppendLine($"Dentro de {WithArticle(container)} ves:");

        foreach (var objId in container.ContainedObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null)
                sb.AppendLine($"- {Cap(obj.Name)}");
        }

        return CommandResult.Success(sb.ToString().TrimEnd());
    }

    private CommandResult HandleExamine(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var target = (parsed.DirectObject ?? string.Empty).Trim();
        var originalTarget = parsed.OriginalDirectObject;

        // Si no hay objetivo o se mira la sala, disparar Event_OnLook y mostrar descripción
        if (string.IsNullOrEmpty(target) || IsRoomLookTarget(target))
        {
            _ = TriggerRoomScriptsAsync(room.Id, "Event_OnLook");
            return CommandResult.Success(""); // La descripción se muestra en el área fija superior
        }

        // Buscar objeto en la sala o inventario
        var obj = FindObjectInRoomOrInventory(room, target, originalTarget);
        if (obj != null)
        {
            // Disparar script Event_OnExamine
            _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnExamine");

            var sb = new StringBuilder();

            // Descripción base
            if (!string.IsNullOrWhiteSpace(obj.Description))
                sb.Append(obj.Description);
            else
                sb.Append($"No ves nada especial en {WithArticle(obj)}.");

            // Si es contenedor, añadir información adicional
            if (obj.IsContainer)
            {
                // Estado abierto/cerrado si es abrible
                if (obj.IsOpenable)
                {
                    sb.Append(obj.IsOpen ? " Está abierto." : " Está cerrado.");
                    if (obj.IsLocked && !obj.IsOpen)
                        sb.Append(" Parece que necesita una llave.");
                }

                // Mostrar contenido si está abierto o si el contenido es visible
                if (obj.IsOpen || obj.ContentsVisible || !obj.IsOpenable)
                {
                    if (obj.ContainedObjectIds.Count == 0)
                    {
                        sb.Append(" Está vacío.");
                    }
                    else
                    {
                        var contents = obj.ContainedObjectIds
                            .Select(FindObjectById)
                            .Where(o => o != null)
                            .Select(o => Low(o!.Name))
                            .ToList();

                        if (contents.Count > 0)
                            sb.Append($" Dentro hay: {string.Join(", ", contents)}.");
                    }
                }
            }

            return CommandResult.Success(sb.ToString());
        }

        // Buscar NPC en la sala
        var npc = _state.Npcs.FirstOrDefault(n =>
            n.Visible &&
            n.RoomId?.Equals(room.Id, StringComparison.OrdinalIgnoreCase) == true &&
            MatchesName(n.Name, target, originalTarget));
        if (npc != null)
        {
            if (!string.IsNullOrWhiteSpace(npc.Description))
                return CommandResult.Success(npc.Description);
            return CommandResult.Success($"No ves nada especial en {Low(npc.Name)}.");
        }

        // Buscar puerta en la sala
        var door = FindDoorInCurrentRoomByName(room, target);
        if (door != null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(door.Description))
            {
                sb.Append(door.Description);
                // Añadir punto si la descripción no termina con signo de puntuación
                if (!door.Description.EndsWith('.') && !door.Description.EndsWith('!') && !door.Description.EndsWith('?'))
                    sb.Append('.');
            }
            else
                sb.Append($"Es {Low(door.Name)}.");

            // Añadir estado de la puerta
            sb.Append(door.IsOpen ? " Está abierta." : " Está cerrada.");
            if (door.IsLocked && !door.IsOpen)
                sb.Append(" Parece que necesita una llave.");

            return CommandResult.Success(sb.ToString());
        }

        return CommandResult.Error(RandomMessages.ObjectNotFound);
    }

    private CommandResult HandleTake(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = parsed.DirectObject ?? string.Empty;
        var originalArg = parsed.OriginalDirectObject;
        arg = arg.ToLowerInvariant();

        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToTake);

        if (arg.StartsWith("todo"))
        {
            return HandleTakeAll(arg, room);
        }

        // Primero buscar en la sala directamente
        var obj = FindVisibleObjectInRoom(room, arg, originalArg);
        GameObject? container = null;

        // Si no está en la sala, buscar dentro de contenedores abiertos
        if (obj == null)
        {
            (obj, container) = FindObjectInOpenContainers(room, arg, originalArg);
        }

        if (obj == null)
            return CommandResult.Error(RandomMessages.ObjectNotFound);

        if (!obj.CanTake)
            return CommandResult.Error(RandomMessages.CannotTakeThat);

        // Validar capacidad de inventario (peso y volumen)
        if (!CanAddToInventory(obj))
            return CommandResult.Error("No puedes llevar más peso o volumen en tu inventario.");

        if (!_state.InventoryObjectIds.Contains(obj.Id))
            _state.InventoryObjectIds.Add(obj.Id);

        // Si estaba en un contenedor, sacarlo del contenedor
        if (container != null)
        {
            container.ContainedObjectIds.Remove(obj.Id);
        }
        else
        {
            // Estaba directamente en la sala
            room.ObjectIds.RemoveAll(id => id.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
        }

        // Marcar que el objeto ya no está en ninguna sala
        obj.RoomId = null;

        // Disparar evento Event_OnTake del objeto
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnTake");

        if (container != null)
            return CommandResult.Success($"Coges {WithArticle(obj)} de {WithArticle(container)}.");

        return CommandResult.Success($"Coges {WithArticle(obj)}.");
    }

    private CommandResult HandleTakeAll(string arg, Room room)
    {
        var exceptName = string.Empty;

        if (arg.StartsWith("todo menos"))
            exceptName = arg.Substring("todo menos".Length).Trim();
        else if (arg == "todo")
            exceptName = string.Empty;

        // Objetos directamente en la sala (separar los que se pueden coger de los que no)
        var allVisibleObjs = _state.Objects
            .Where(o => o.Visible && room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase) && !o.IsContainer)
            .ToList();

        var takableObjs = allVisibleObjs.Where(o => o.CanTake).ToList();
        var untakableObjs = allVisibleObjs.Where(o => !o.CanTake).ToList();

        // Buscar objetos en contenedores abiertos
        var containers = _state.Objects
            .Where(o => o.Visible && o.IsContainer && room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase))
            .Where(o => o.IsOpen || o.ContentsVisible || !o.IsOpenable)
            .ToList();

        var objectsInContainers = new List<(GameObject obj, GameObject container)>();
        var untakableInContainers = new List<(GameObject obj, GameObject container)>();

        foreach (var container in containers)
        {
            var containedObjs = container.ContainedObjectIds
                .Select(FindObjectById)
                .Where(o => o != null && o.Visible)
                .ToList();

            foreach (var obj in containedObjs)
            {
                if (obj!.CanTake)
                    objectsInContainers.Add((obj, container));
                else
                    untakableInContainers.Add((obj, container));
            }
        }

        if (!takableObjs.Any() && !objectsInContainers.Any() && !untakableObjs.Any() && !untakableInContainers.Any())
            return CommandResult.Error(RandomMessages.ObjectNotFound);

        var sb = new StringBuilder();
        var couldNotTake = new List<string>();

        // Coger objetos directamente de la sala
        foreach (var obj in takableObjs)
        {
            if (!string.IsNullOrEmpty(exceptName) &&
                obj.Name.Contains(exceptName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_state.InventoryObjectIds.Contains(obj.Id))
                _state.InventoryObjectIds.Add(obj.Id);

            room.ObjectIds.RemoveAll(id => id.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
            obj.RoomId = null; // Ya no está en ninguna sala

            // Disparar evento Event_OnTake del objeto
            _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnTake");

            sb.AppendLine($"Coges {WithArticle(obj)}.");
        }

        // Coger objetos de contenedores abiertos
        foreach (var (obj, container) in objectsInContainers)
        {
            if (!string.IsNullOrEmpty(exceptName) &&
                obj.Name.Contains(exceptName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!_state.InventoryObjectIds.Contains(obj.Id))
                _state.InventoryObjectIds.Add(obj.Id);

            container.ContainedObjectIds.Remove(obj.Id);
            obj.RoomId = null; // Ya no está en ninguna sala

            // Disparar evento Event_OnTake del objeto
            _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnTake");

            sb.AppendLine($"Coges {WithArticle(obj)} de {WithArticle(container)}.");
        }

        // Recopilar objetos que no se pudieron coger
        foreach (var obj in untakableObjs)
        {
            if (string.IsNullOrEmpty(exceptName) ||
                !obj.Name.Contains(exceptName, StringComparison.OrdinalIgnoreCase))
            {
                couldNotTake.Add(Low(obj.Name));
            }
        }

        foreach (var (obj, _) in untakableInContainers)
        {
            if (string.IsNullOrEmpty(exceptName) ||
                !obj.Name.Contains(exceptName, StringComparison.OrdinalIgnoreCase))
            {
                couldNotTake.Add(Low(obj.Name));
            }
        }

        // Añadir mensaje de objetos que no se pudieron coger
        if (couldNotTake.Any())
        {
            sb.AppendLine($"No puedes coger: {string.Join(", ", couldNotTake)}.");
        }

        if (sb.Length == 0)
            return CommandResult.Error(RandomMessages.NothingToTake);

        return CommandResult.Success(sb.ToString().TrimEnd());
    }

    private CommandResult HandleDrop(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = parsed.DirectObject ?? string.Empty;
        var originalArg = parsed.OriginalDirectObject;
        arg = arg.ToLowerInvariant();

        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhatToDrop);

        var obj = _state.InventoryObjectIds
            .Select(FindObjectById)
            .FirstOrDefault(o => o != null && MatchesName(o.Name, arg, originalArg));

        if (obj == null)
            return CommandResult.Error(RandomMessages.NotCarryingThat);

        // Si el objeto está equipado, desequiparlo primero
        var wasEquipped = false;
        if (_state.Player.EquippedRightHandId == obj.Id)
        {
            // Si es arma de 2 manos, liberar ambas manos
            if (_state.Player.EquippedLeftHandId == obj.Id)
                _state.Player.EquippedLeftHandId = null;
            _state.Player.EquippedRightHandId = null;
            wasEquipped = true;
        }
        else if (_state.Player.EquippedLeftHandId == obj.Id)
        {
            _state.Player.EquippedLeftHandId = null;
            wasEquipped = true;
        }
        else if (_state.Player.EquippedTorsoId == obj.Id)
        {
            _state.Player.EquippedTorsoId = null;
            wasEquipped = true;
        }
        else if (_state.Player.EquippedHeadId == obj.Id)
        {
            _state.Player.EquippedHeadId = null;
            wasEquipped = true;
        }

        _state.InventoryObjectIds.RemoveAll(id => id.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
        if (!room.ObjectIds.Contains(obj.Id))
            room.ObjectIds.Add(obj.Id);

        obj.RoomId = room.Id;

        // Disparar evento Event_OnDrop del objeto
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnDrop");

        var message = wasEquipped
            ? $"Te quitas {WithArticle(obj)} y lo dejas en el suelo."
            : $"Sueltas {WithArticle(obj)}.";

        return CommandResult.Success(message);
    }

    private CommandResult HandleTalk(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var arg = parsed.DirectObject ?? string.Empty;
        var originalArg = parsed.OriginalDirectObject;

        // Si no hay objeto directo, intentar con el indirecto
        if (string.IsNullOrEmpty(arg))
        {
            arg = parsed.IndirectObject ?? string.Empty;
            originalArg = parsed.OriginalIndirectObject;
        }

        if (string.IsNullOrEmpty(arg))
            return CommandResult.Error(RandomMessages.WhoToTalkTo);

        // Debug: mostrar qué estamos buscando
        if (_isDebugMode)
            ScriptMessage?.Invoke($"[Debug] Buscando NPC: '{arg}' en sala '{room.Id}'");

        // Buscar NPC en la sala
        var npcsInRoom = _state.Npcs
            .Where(n => n.Visible && room.NpcIds.Contains(n.Id, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // Debug: mostrar NPCs encontrados
        if (_isDebugMode)
            ScriptMessage?.Invoke($"[Debug] NPCs en sala: {string.Join(", ", npcsInRoom.Select(n => $"'{n.Name}' (Id:{n.Id})"))}");

        // Primero buscar con el objeto directo
        var npc = npcsInRoom.FirstOrDefault(n => MatchesName(n.Name, arg, originalArg));

        // Si no se encuentra y hay objeto indirecto, buscar con él (para "decir hola a gordo")
        if (npc == null && !string.IsNullOrEmpty(parsed.IndirectObject))
        {
            npc = npcsInRoom.FirstOrDefault(n => MatchesName(n.Name, parsed.IndirectObject, parsed.OriginalIndirectObject));
        }

        if (npc == null)
            return CommandResult.Error(RandomMessages.PersonNotFound);

        // Disparar script Event_OnTalk
        _ = TriggerEntityScriptAsync("Npc", npc.Id, "Event_OnTalk");

        // Verificar si el NPC tiene un script con nodos de diálogo
        var hasDialogue = _world.Scripts.Any(s =>
            string.Equals(s.OwnerType, "Npc", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, npc.Id, StringComparison.OrdinalIgnoreCase) &&
            s.Nodes.Any(n => n.Category == NodeCategory.Dialogue));

        if (!hasDialogue)
        {
            // Si no tiene diálogo pero es comerciante, abrir tienda (puede vender aunque no tenga artículos)
            if (npc.IsShopkeeper)
            {
                TradeOpened?.Invoke(npc);
                return CommandResult.Empty;
            }
            return CommandResult.Success(string.Format(RandomMessages.NothingToSay, Cap(npc.Name)));
        }

        // Iniciar conversación con el NPC
        // Usamos ContinueWith para capturar errores sin bloquear
        StartConversationWithNpcAsync(npc.Id).ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                ScriptMessage?.Invoke($"[Error] Conversación falló: {t.Exception.InnerException?.Message ?? t.Exception.Message}");
            }
        });
        return CommandResult.Empty; // La UI se actualiza via eventos
    }

    /// <summary>
    /// Inicia una conversación con un NPC de forma asíncrona.
    /// </summary>
    private async Task StartConversationWithNpcAsync(string npcId)
    {
        if (_conversationEngine == null) return;

        try
        {
            await _conversationEngine.StartConversationAsync(npcId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Conversation error: {ex.Message}");
            ScriptMessage?.Invoke("Error al iniciar la conversación.");
        }
    }

    /// <summary>
    /// Maneja la selección de una opción de conversación.
    /// </summary>
    private async Task HandleConversationOptionAsync(int optionIndex)
    {
        if (_conversationEngine == null) return;

        try
        {
            await _conversationEngine.SelectOptionAsync(optionIndex);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Conversation option error: {ex.Message}");
            ScriptMessage?.Invoke("Error al seleccionar opción.");
        }
    }

    /// <summary>
    /// Inicia una conversación desde un script (Action_StartConversation).
    /// </summary>
    public async Task StartConversationFromScriptAsync(string npcId)
    {
        await StartConversationWithNpcAsync(npcId);
    }

    private CommandResult HandleUse(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        var objName = parsed.DirectObject ?? string.Empty;
        var originalObjName = parsed.OriginalDirectObject;
        if (string.IsNullOrWhiteSpace(objName))
            return CommandResult.Error(RandomMessages.WhatToUse);

        // Buscar el objeto en la sala o inventario
        var obj = room != null ? FindObjectInRoomOrInventory(room, objName, originalObjName) : null;
        if (obj != null)
        {
            // Verificar tipos de objetos que no tiene sentido "usar"
            switch (obj.Type)
            {
                case ObjectType.Arma:
                    return CommandResult.Error($"{WithArticleCap(obj)} no se usa así. ¿Quizás quieres atacar a alguien?");

                case ObjectType.Armadura:
                case ObjectType.Escudo:
                case ObjectType.Casco:
                    return CommandResult.Error($"{WithArticleCap(obj)} no se usa así. ¿Quizás quieres equipártelo?");

                case ObjectType.Comida:
                    return CommandResult.Error($"{WithArticleCap(obj)} no se usa así. ¿Quizás quieres comer?");

                case ObjectType.Bebida:
                    return CommandResult.Error($"{WithArticleCap(obj)} no se usa así. ¿Quizás quieres beber?");
            }

            // Verificar si el objeto tiene un script Event_OnUse
            if (!HasEventScript("GameObject", obj.Id, "Event_OnUse"))
                return CommandResult.Error($"No parece que puedas hacer nada especial con {WithArticle(obj)}.");

            // Disparar script Event_OnUse
            _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnUse");
            return CommandResult.Success($"Usas {Low(obj.Name)}.");
        }

        // Si no se encuentra el objeto, mensaje por defecto
        return CommandResult.Error($"No ves ningún '{objName}' que puedas usar.");
    }

    /// <summary>
    /// Comprueba si una entidad tiene un script con un evento específico.
    /// </summary>
    private bool HasEventScript(string ownerType, string ownerId, string eventType)
    {
        var script = _world.Scripts.FirstOrDefault(s =>
            string.Equals(s.OwnerType, ownerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase));

        if (script == null) return false;

        // Buscar el nodo de evento
        if (!Enum.TryParse<NodeTypeId>(eventType, true, out var nodeTypeId))
            return false;

        return script.Nodes.Any(n => n.NodeType == nodeTypeId);
    }

    private CommandResult HandleGive(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var objName = parsed.DirectObject ?? string.Empty;
        var originalObjName = parsed.OriginalDirectObject;

        if (string.IsNullOrWhiteSpace(objName))
            return CommandResult.Error(RandomMessages.WhatToGive);

        // Buscar el objeto en el inventario del jugador
        var obj = _state.InventoryObjectIds
            .Select(FindObjectById)
            .FirstOrDefault(o => o != null && MatchesName(o.Name, objName, originalObjName));

        if (obj == null)
            return CommandResult.Error($"No llevas ningún '{parsed.OriginalDirectObject ?? objName}'.");

        // Buscar el NPC destinatario
        var targetName = parsed.IndirectObject ?? string.Empty;
        var originalTargetName = parsed.OriginalIndirectObject;

        Npc? targetNpc = null;

        if (!string.IsNullOrWhiteSpace(targetName))
        {
            // Buscar NPC por nombre
            targetNpc = _state.Npcs.FirstOrDefault(n =>
                n.Visible &&
                n.RoomId?.Equals(room.Id, StringComparison.OrdinalIgnoreCase) == true &&
                MatchesName(n.Name, targetName, originalTargetName));
        }
        else
        {
            // Si no se especifica destinatario, buscar el único NPC visible en la sala
            var npcsInRoom = _state.Npcs.Where(n =>
                n.Visible &&
                n.RoomId?.Equals(room.Id, StringComparison.OrdinalIgnoreCase) == true).ToList();

            if (npcsInRoom.Count == 0)
                return CommandResult.Error(RandomMessages.NoOneToGiveTo);
            if (npcsInRoom.Count > 1)
                return CommandResult.Error(RandomMessages.WhoToGiveTo);

            targetNpc = npcsInRoom[0];
        }

        if (targetNpc == null)
            return CommandResult.Error($"No ves a '{parsed.OriginalIndirectObject ?? targetName}' aquí.");

        // Transferir el objeto del jugador al NPC
        _state.InventoryObjectIds.RemoveAll(id => id.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
        var existingItem = targetNpc.Inventory.FirstOrDefault(i => i.ObjectId.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
        if (existingItem != null)
            existingItem.Quantity++;
        else
            targetNpc.Inventory.Add(new InventoryItem { ObjectId = obj.Id, Quantity = 1 });

        // Disparar evento Event_OnGive del objeto (si existe)
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnGive");

        return CommandResult.Success($"Le das {WithArticle(obj)} a {targetNpc.Name}.");
    }

    private CommandResult HandleRead(ParsedCommand parsed)
    {
        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var objName = parsed.DirectObject ?? string.Empty;
        var originalObjName = parsed.OriginalDirectObject;
        if (string.IsNullOrWhiteSpace(objName))
            return CommandResult.Error(RandomMessages.WhatToRead);

        // Buscar el objeto en la sala o inventario
        var obj = FindObjectInRoomOrInventory(room, objName, originalObjName);
        if (obj == null)
            return CommandResult.Error(RandomMessages.ObjectNotFound);

        // Verificar que el objeto se pueda leer
        if (!obj.CanRead)
            return CommandResult.Error($"No puedes leer {WithArticle(obj)}.");

        // Verificar que tenga contenido de texto
        if (string.IsNullOrWhiteSpace(obj.TextContent))
            return CommandResult.Error($"{WithArticleCap(obj)} está en blanco.");

        // Disparar script Event_OnExamine (leer es una forma de examinar)
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnExamine");

        // Mostrar el contenido con formato de texto leído (cursiva simulada con comillas)
        var sb = new StringBuilder();
        sb.AppendLine($"Lees {WithArticle(obj)}:");
        sb.AppendLine();
        sb.AppendLine($"« {obj.TextContent} »");

        return CommandResult.Success(sb.ToString().TrimEnd());
    }

    private string DescribeQuests()
    {
        if (_state.Quests.Count == 0)
            return "No tienes misiones activas.";

        var sb = new StringBuilder();
        sb.AppendLine("Misiones:");

        foreach (var kvp in _state.Quests)
        {
            var def = _world.Quests.FirstOrDefault(q => q.Id == kvp.Key);
            var st = kvp.Value;

            var name = def?.Name ?? kvp.Key;
            sb.Append($" - {name} [{st.Status}]");

            if (st.Status == QuestStatus.InProgress && def != null && st.CurrentObjectiveIndex < def.Objectives.Count)
            {
                sb.Append($": {def.Objectives[st.CurrentObjectiveIndex]}");
            }

            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Maneja el comando de ayuda. Dispara el evento HelpRequested.
    /// </summary>
    private CommandResult HandleHelp()
    {
        HelpRequested?.Invoke();
        return CommandResult.Empty;
    }

    /// <summary>
    /// Genera el texto de ayuda con todos los comandos disponibles.
    /// Se muestra cuando el jugador usa "?", "ayuda", "help" o "comandos".
    /// </summary>
    private static string GetCommandsText()
    {
        return @"╔══════════════════════════════════════════════════════════════╗
║                    VERBOS DISPONIBLES                        ║
╠══════════════════════════════════════════════════════════════╣
║ MOVIMIENTO                                                   ║
║  ir, ve, andar, caminar    → ""ir norte"", ""ve al sur""         ║
║  n, s, e, o, ne, no, se, so → ""n"" (ir al norte)              ║
║  subir, bajar, arriba, abajo                                 ║
╠══════════════════════════════════════════════════════════════╣
║ EXPLORACIÓN                                                  ║
║  examinar, x                → ""examinar espada"", ""x cofre""   ║
║  leer, lee                  → ""leer pergamino"", ""lee carta""  ║
╠══════════════════════════════════════════════════════════════╣
║ INVENTARIO                                                   ║
║  inventario, inv, i         → ""inventario"" (ver objetos)     ║
║  coger, tomar, recoger      → ""coger llave"", ""coger todo""    ║
║  soltar, dejar, tirar       → ""soltar espada""                ║
╠══════════════════════════════════════════════════════════════╣
║ INTERACCIÓN                                                  ║
║  abrir, abre                → ""abrir cofre"", ""abrir puerta""  ║
║  cerrar, cierra             → ""cerrar puerta""                ║
║  usar, utilizar             → ""usar llave"", ""usar llave con  ║
║                               puerta""                        ║
║  meter, poner, guardar      → ""meter espada en cofre""        ║
║  sacar, quitar              → ""sacar llave del cofre""        ║
╠══════════════════════════════════════════════════════════════╣
║ CONVERSACIÓN                                                 ║
║  hablar, charlar            → ""hablar con mercader""          ║
║  decir, di                  → ""decir 1"", ""di 2""              ║
║  opcion                     → ""opcion 1""                     ║
╠══════════════════════════════════════════════════════════════╣
║ OTROS                                                        ║
║  misiones, quest            → ""misiones"" (ver progreso)      ║
║  ayuda, help                → ""ayuda"" (información básica)   ║
╚══════════════════════════════════════════════════════════════╝

💡 CONSEJO: Puedes escribir frases naturales como:
   ""quiero coger la espada del suelo""
   ""ve hacia el norte y abre la puerta""
   ""examina el libro que hay en la mesa""

🤖 ¿No te entiende? Activa la IA en Opciones para que interprete
   mejor tus comandos con lenguaje natural.
";
    }

    private GameObject? FindVisibleObjectInRoom(Room room, string namePart, string? originalNamePart = null)
    {
        return _state.Objects
            .Where(o => o.Visible && room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase))
            .FirstOrDefault(o => MatchesName(o.Name, namePart, originalNamePart));
    }

    /// <summary>
    /// Busca un objeto dentro de contenedores abiertos en la sala.
    /// Devuelve el objeto encontrado y el contenedor que lo contiene.
    /// </summary>
    private (GameObject? obj, GameObject? container) FindObjectInOpenContainers(Room room, string namePart, string? originalNamePart = null)
    {
        // Buscar todos los contenedores visibles en la sala
        var containers = _state.Objects
            .Where(o => o.Visible && o.IsContainer && room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase))
            .Where(o => o.IsOpen || o.ContentsVisible || !o.IsOpenable) // Contenedor accesible
            .ToList();

        foreach (var container in containers)
        {
            var objInContainer = container.ContainedObjectIds
                .Select(FindObjectById)
                .FirstOrDefault(o => o != null && o.Visible && MatchesName(o.Name, namePart, originalNamePart));

            if (objInContainer != null)
                return (objInContainer, container);
        }

        return (null, null);
    }

    private bool HasDistinctRoomMusic(Room room)
    {
        var roomMusicId = room.MusicId;
        var worldMusicId = _state.WorldMusicId;

        // La sala tiene música propia si tiene un MusicId definido
        if (string.IsNullOrWhiteSpace(roomMusicId))
            return false;

        // Si es la misma que la del mundo, no es "distinta"
        if (!string.IsNullOrWhiteSpace(worldMusicId) &&
            roomMusicId.Equals(worldMusicId, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }


    private void OnRoomChanged()
    {
        var room = CurrentRoom;
        if (room != null)
        {
            string? roomMusicId = null;
            string? roomMusicBase64 = null;

            if (HasDistinctRoomMusic(room))
            {
                roomMusicId = room.MusicId;
                // Buscar la música en la lista de músicas del mundo
                var musicAsset = _world.Musics.FirstOrDefault(m => m.Id.Equals(roomMusicId, StringComparison.OrdinalIgnoreCase));
                roomMusicBase64 = musicAsset?.Base64;
            }

            _sound.PlayRoomMusic(
                roomMusicId,
                roomMusicBase64,
                null,
                null);

            // Voz: solo reproducimos la descripción la primera vez que visitamos la sala (por ID)
            // Solo marcar como visitada si la voz no está suprimida (para que suene al cargar)
            if (!_sound.SuppressVoicePlayback && _visitedRoomsForTts.Add(room.Id))
            {
                _ = _sound.PlayRoomDescriptionAsync(room.Id, room.Description);
            }

            // Precargamos en segundo plano las voces de las salas cercanas
            // (hasta dos movimientos de distancia) y limpiamos las lejanas.
            _ = PreloadVoicesAroundCurrentRoomAsync();

            // Ejecutar scripts de la sala (Event_OnEnter) - solo si ya se inicializaron los scripts
            if (_initialScriptsReady)
            {
                _ = TriggerRoomScriptsAsync(room.Id, "Event_OnEnter");

                // Disparar Event_OnNpcSee para cada NPC en la sala (el NPC ve al jugador)
                foreach (var npcId in room.NpcIds)
                {
                    _ = TriggerEntityScriptAsync("Npc", npcId, "Event_OnNpcSee");
                }
            }

            RoomChanged?.Invoke(room);
        }
    }

    /// <summary>
    /// Ejecuta los scripts asociados a un evento de sala de forma asíncrona.
    /// </summary>
    private async Task TriggerRoomScriptsAsync(string roomId, string eventType)
    {
        if (_scriptEngine == null) return;

        try
        {
            await _scriptEngine.TriggerEventAsync("Room", roomId, eventType);
        }
        catch (Exception ex)
        {
            // Log error silently - don't crash the game due to script errors
            System.Diagnostics.Debug.WriteLine($"Script error: {ex.Message}");
        }
    }

    /// <summary>
    /// Ejecuta los scripts asociados a un evento de cualquier entidad.
    /// </summary>
    private async Task TriggerEntityScriptAsync(string ownerType, string ownerId, string eventType)
    {
        if (_scriptEngine == null) return;

        try
        {
            await _scriptEngine.TriggerEventAsync(ownerType, ownerId, eventType);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Script error: {ex.Message}");
        }
    }

    /// <summary>
    /// Comprueba si una entidad tiene un script con un evento que lleva a Action_ShowMessage.
    /// </summary>
    private bool HasScriptWithMessage(string ownerType, string ownerId, string eventType)
    {
        var script = _world.Scripts.FirstOrDefault(s =>
            string.Equals(s.OwnerType, ownerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase));

        if (script == null) return false;

        // Buscar el nodo de evento
        if (!Enum.TryParse<NodeTypeId>(eventType, true, out var nodeTypeId))
            return false;

        var eventNode = script.Nodes.FirstOrDefault(n => n.NodeType == nodeTypeId);

        if (eventNode == null) return false;

        // Recorrer las conexiones para ver si llega a un Action_ShowMessage
        var visited = new HashSet<string>();
        var toVisit = new Queue<string>();
        toVisit.Enqueue(eventNode.Id);

        while (toVisit.Count > 0)
        {
            var currentId = toVisit.Dequeue();
            if (visited.Contains(currentId)) continue;
            visited.Add(currentId);

            var currentNode = script.Nodes.FirstOrDefault(n => n.Id == currentId);
            if (currentNode?.NodeType == NodeTypeId.Action_ShowMessage)
                return true;

            // Añadir nodos conectados
            foreach (var conn in script.Connections.Where(c => c.FromNodeId == currentId))
            {
                if (!visited.Contains(conn.ToNodeId))
                    toVisit.Enqueue(conn.ToNodeId);
            }
        }

        return false;
    }

    private void EnsurePlayerRoom()
    {
        if (_state.Rooms.All(r => !r.Id.Equals(_state.CurrentRoomId, StringComparison.OrdinalIgnoreCase)))
        {
            var first = _state.Rooms.FirstOrDefault();
            if (first != null)
                _state.CurrentRoomId = first.Id;
        }
    }

    /// <summary>
    /// Comprueba si una puerta es visible para el jugador.
    /// Una puerta es visible si Visible = true y todos los requisitos de misiones se cumplen.
    /// </summary>
    private bool IsDoorVisible(Door door)
    {
        if (!door.Visible)
            return false;

        if (door.RequiredQuests.Count == 0)
            return true;

        foreach (var requirement in door.RequiredQuests)
        {
            var quest = _state.Quests.Values.FirstOrDefault(q =>
                q.QuestId.Equals(requirement.QuestId, StringComparison.OrdinalIgnoreCase));
            if (quest == null || quest.Status != requirement.RequiredStatus)
                return false;
        }

        return true;
    }

    private static string NormalizeDirection(string dir)
    {
        dir = dir.ToLowerInvariant().Trim();
        return dir switch
        {
            "norte" or "n" => "n",
            "sur" or "s" => "s",
            "este" or "e" => "e",
            "oeste" or "o" => "o",
            "noreste" or "ne" => "ne",
            "noroeste" or "no" => "no",
            "sureste" or "se" => "se",
            "suroeste" or "so" => "so",
            "arriba" or "subir" => "up",
            "abajo" or "bajar" => "down",
            _ => dir
        };
    }


    private static string GetOppositeDirectionCode(string normalizedDirection)
    {
        return normalizedDirection switch
        {
            "n" => "s",
            "s" => "n",
            "e" => "o",
            "o" => "e",
            "ne" => "so",
            "so" => "ne",
            "no" => "se",
            "se" => "no",
            "up" => "down",
            "down" => "up",
            _ => normalizedDirection
        };
    }

    #region NPC Movement (Patrol & Follow)

    private static readonly string[] _npcArrivalMessages =
    {
        "{0} ha llegado.",
        "{0} aparece por aquí.",
        "{0} entra en la sala.",
        "{0} está aquí contigo.",
        "{0} acaba de llegar."
    };

    private static readonly string[] _npcDepartureMessages =
    {
        "{0} se marcha.",
        "{0} se aleja.",
        "{0} abandona la sala.",
        "{0} se va.",
        "{0} desaparece por una salida."
    };

    private static readonly Random _arrivalRandom = new();

    /// <summary>
    /// Mueve un NPC de una sala a otra, abriendo puertas si es necesario.
    /// Si hay una puerta cerrada con llave y el NPC no tiene la llave, no se mueve.
    /// </summary>
    private void MoveNpcToRoom(Npc npc, string targetRoomId)
    {
        if (string.IsNullOrEmpty(targetRoomId)) return;
        if (string.IsNullOrEmpty(npc.RoomId)) return;

        // Buscar si hay una puerta entre la sala actual y la sala destino
        var door = FindDoorBetweenRooms(npc.RoomId, targetRoomId);
        if (door != null && !door.IsOpen)
        {
            // La puerta está cerrada, intentar abrirla
            if (door.IsLocked)
            {
                // La puerta está cerrada con llave
                if (!string.IsNullOrEmpty(door.KeyObjectId))
                {
                    // Verificar si el NPC tiene la llave
                    var hasKey = npc.Inventory.Any(i =>
                        string.Equals(i.ObjectId, door.KeyObjectId, StringComparison.OrdinalIgnoreCase));

                    if (!hasKey)
                    {
                        // El NPC no tiene la llave, no puede pasar - esperar
                        return;
                    }

                    // El NPC tiene la llave, desbloquear la puerta
                    door.IsLocked = false;
                }
                else
                {
                    // Está bloqueada pero no tiene llave definida, no puede pasar
                    return;
                }
            }

            // Abrir la puerta (ya sea que estaba solo cerrada o que el NPC la desbloqueó)
            door.IsOpen = true;
        }

        // Comprobar si el NPC entra a la sala del jugador (y no estaba ya ahí)
        var wasInPlayerRoom = string.Equals(npc.RoomId, _state.CurrentRoomId, StringComparison.OrdinalIgnoreCase);
        var willBeInPlayerRoom = string.Equals(targetRoomId, _state.CurrentRoomId, StringComparison.OrdinalIgnoreCase);

        // Quitar de sala anterior
        var oldRoom = _state.Rooms.FirstOrDefault(r =>
            string.Equals(r.Id, npc.RoomId, StringComparison.OrdinalIgnoreCase));
        oldRoom?.NpcIds.RemoveAll(id =>
            string.Equals(id, npc.Id, StringComparison.OrdinalIgnoreCase));

        // Añadir a nueva sala
        npc.RoomId = targetRoomId;
        var newRoom = _state.Rooms.FirstOrDefault(r =>
            string.Equals(r.Id, targetRoomId, StringComparison.OrdinalIgnoreCase));
        if (newRoom != null && !newRoom.NpcIds.Any(id =>
            string.Equals(id, npc.Id, StringComparison.OrdinalIgnoreCase)))
        {
            newRoom.NpcIds.Add(npc.Id);
        }

        // Notificar si el NPC abandona la sala del jugador
        if (wasInPlayerRoom && !willBeInPlayerRoom)
        {
            var message = string.Format(_npcDepartureMessages[_arrivalRandom.Next(_npcDepartureMessages.Length)], npc.Name);
            ScriptMessage?.Invoke(message);
        }

        // Notificar si el NPC entra a la sala del jugador
        if (!wasInPlayerRoom && willBeInPlayerRoom)
        {
            var message = string.Format(_npcArrivalMessages[_arrivalRandom.Next(_npcArrivalMessages.Length)], npc.Name);
            ScriptMessage?.Invoke(message);
        }
    }

    /// <summary>
    /// Busca una puerta que conecte dos salas.
    /// </summary>
    private Door? FindDoorBetweenRooms(string roomIdA, string roomIdB)
    {
        return _state.Doors.FirstOrDefault(d =>
            (string.Equals(d.RoomIdA, roomIdA, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(d.RoomIdB, roomIdB, StringComparison.OrdinalIgnoreCase)) ||
            (string.Equals(d.RoomIdA, roomIdB, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(d.RoomIdB, roomIdA, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Actualiza las patrullas de todos los NPCs en modo turnos. Llamar después de cada comando del jugador.
    /// </summary>
    private void UpdateNpcPatrols()
    {
        foreach (var npc in _state.Npcs.Where(n => n.IsPatrolling && !n.IsFollowingPlayer && n.PatrolRoute.Count > 1 && n.PatrolMovementMode == MovementMode.Turns))
        {
            npc.PatrolTurnCounter++;
            if (npc.PatrolTurnCounter >= npc.PatrolSpeed)
            {
                npc.PatrolTurnCounter = 0;
                MoveNpcToNextPatrolPoint(npc);
            }
        }
    }

    /// <summary>
    /// Actualiza las patrullas de todos los NPCs en modo tiempo. Llamar periódicamente desde el cliente.
    /// </summary>
    private void UpdateNpcPatrolsByTime()
    {
        var now = DateTime.UtcNow;
        foreach (var npc in _state.Npcs.Where(n => n.IsPatrolling && !n.IsFollowingPlayer && n.PatrolRoute.Count > 1 && n.PatrolMovementMode == MovementMode.Time))
        {
            // Inicializar tiempo si es la primera vez
            if (npc.PatrolLastMoveTime == DateTime.MinValue)
            {
                npc.PatrolLastMoveTime = now;
                continue;
            }

            var elapsed = (now - npc.PatrolLastMoveTime).TotalSeconds;
            if (elapsed >= npc.PatrolTimeInterval)
            {
                npc.PatrolLastMoveTime = now;
                MoveNpcToNextPatrolPoint(npc);
            }
        }
    }

    /// <summary>
    /// Mueve un NPC al siguiente punto de su ruta de patrulla.
    /// </summary>
    private void MoveNpcToNextPatrolPoint(Npc npc)
    {
        var nextIndex = GetNextPatrolIndex(npc);
        if (nextIndex >= 0 && nextIndex < npc.PatrolRoute.Count)
        {
            var nextRoomId = npc.PatrolRoute[nextIndex];
            MoveNpcToRoom(npc, nextRoomId);
            npc.PatrolRouteIndex = nextIndex;
        }
    }

    /// <summary>
    /// Calcula el siguiente índice en la ruta de patrulla (modo ping-pong).
    /// </summary>
    private int GetNextPatrolIndex(Npc npc)
    {
        if (npc.PatrolRoute.Count == 0) return 0;

        // Ping-pong: va y viene
        var next = npc.PatrolRouteIndex + npc.PatrolDirection;
        if (next >= npc.PatrolRoute.Count || next < 0)
        {
            npc.PatrolDirection *= -1;
            next = npc.PatrolRouteIndex + npc.PatrolDirection;
        }
        return Math.Clamp(next, 0, npc.PatrolRoute.Count - 1);
    }

    /// <summary>
    /// Actualiza los NPCs en modo turnos que siguen al jugador. Llamar cuando el jugador cambia de sala.
    /// </summary>
    private void UpdateFollowingNpcs(string newRoomId)
    {
        foreach (var npc in _state.Npcs.Where(n => n.IsFollowingPlayer && n.FollowMovementMode == MovementMode.Turns))
        {
            // FollowSpeed: 1 = cada turno, 2 = cada 2 turnos, 3 = cada 3 turnos (igual que patrulla)
            npc.FollowMoveCounter++;
            if (npc.FollowMoveCounter >= npc.FollowSpeed)
            {
                npc.FollowMoveCounter = 0;
                MoveNpcToRoom(npc, newRoomId);
            }
        }
    }

    /// <summary>
    /// Actualiza los NPCs en modo turnos que siguen al jugador cuando el jugador espera.
    /// Los NPCs que no están en la misma sala pueden alcanzar al jugador.
    /// </summary>
    private void UpdateFollowingNpcsOnWait()
    {
        var playerRoomId = _state.CurrentRoomId;
        foreach (var npc in _state.Npcs.Where(n => n.IsFollowingPlayer && n.FollowMovementMode == MovementMode.Turns))
        {
            // FollowSpeed: 1 = cada turno, 2 = cada 2 turnos, 3 = cada 3 turnos
            npc.FollowMoveCounter++;
            if (npc.FollowMoveCounter >= npc.FollowSpeed)
            {
                npc.FollowMoveCounter = 0;
                // Solo mover si el NPC no está ya en la sala del jugador
                if (!string.Equals(npc.RoomId, playerRoomId, StringComparison.OrdinalIgnoreCase))
                {
                    MoveNpcToRoom(npc, playerRoomId);
                }
            }
        }
    }

    /// <summary>
    /// Actualiza los NPCs en modo tiempo que siguen al jugador. Llamar periódicamente desde el cliente.
    /// </summary>
    private void UpdateFollowingNpcsByTime()
    {
        var now = DateTime.UtcNow;
        var playerRoomId = _state.CurrentRoomId;
        foreach (var npc in _state.Npcs.Where(n => n.IsFollowingPlayer && n.FollowMovementMode == MovementMode.Time))
        {
            // Inicializar tiempo si es la primera vez
            if (npc.FollowLastMoveTime == DateTime.MinValue)
            {
                npc.FollowLastMoveTime = now;
                continue;
            }

            // Solo mover si no está en la misma sala que el jugador
            if (string.Equals(npc.RoomId, playerRoomId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var elapsed = (now - npc.FollowLastMoveTime).TotalSeconds;
            if (elapsed >= npc.FollowTimeInterval)
            {
                npc.FollowLastMoveTime = now;
                MoveNpcToRoom(npc, playerRoomId);
            }
        }
    }

    /// <summary>
    /// Actualiza el movimiento de NPCs basado en tiempo. Llamar periódicamente desde el cliente (cada segundo aprox).
    /// </summary>
    public void UpdateNpcTimedMovement()
    {
        UpdateNpcPatrolsByTime();
        UpdateFollowingNpcsByTime();
    }

    /// <summary>
    /// Maneja el comando esperar. Actualiza NPCs que siguen al jugador.
    /// </summary>
    private CommandResult HandleWait()
    {
        // Primero mostrar el mensaje de espera
        ScriptMessage?.Invoke(RandomMessages.WaitMessage);
        // Luego actualizar NPCs (puede generar mensajes de llegada)
        UpdateFollowingNpcsOnWait();
        return CommandResult.Empty;
    }

    #endregion

    #region Combat Commands

    /// <summary>
    /// Evento que se dispara cuando se inicia un combate.
    /// La UI debe abrir la ventana de combate al recibir este evento.
    /// </summary>
    public event Action<string>? CombatStarted;

    /// <summary>
    /// Maneja el comando atacar.
    /// </summary>
    private CommandResult HandleAttack(ParsedCommand parsed)
    {
        // Verificar que el sistema de combate está activo
        if (!_world.Game.CombatEnabled)
            return CommandResult.Error("El sistema de combate no está activo en esta aventura.");

        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhoToAttack);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Buscar el NPC en la sala (incluyendo cadáveres para dar mensaje específico)
        var npcsInRoom = _state.Npcs
            .Where(n => n.Visible && room.NpcIds.Contains(n.Id))
            .ToList();

        var npc = MatchNpcByName(npcsInRoom, parsed.DirectObject);
        if (npc == null)
            return CommandResult.Error($"No ves a '{parsed.OriginalDirectObject ?? parsed.DirectObject}' aquí.");

        if (npc.IsCorpse)
            return CommandResult.Error($"{npc.Name} ya está muerto.");

        // Disparar evento Event_OnCombatStart del NPC
        _ = TriggerEntityScriptAsync("Npc", npc.Id, "Event_OnCombatStart");

        // Notificar a la UI para abrir la ventana de combate
        CombatStarted?.Invoke(npc.Id);

        return CommandResult.Empty;
    }

    /// <summary>
    /// Maneja el comando equipar.
    /// </summary>
    private CommandResult HandleEquip(ParsedCommand parsed)
    {
        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhatToEquip);

        // Buscar el objeto en el inventario
        GameObject? obj = null;
        foreach (var objId in _state.InventoryObjectIds)
        {
            var candidate = _state.Objects.FirstOrDefault(o => o.Id == objId);
            if (candidate != null && MatchesName(candidate.Name, parsed.DirectObject, parsed.OriginalDirectObject))
            {
                obj = candidate;
                break;
            }
        }

        // Si no está en el inventario, buscar en la sala actual
        bool pickedUp = false;
        GameObject? sourceContainer = null;
        if (obj == null)
        {
            var room = CurrentRoom;
            if (room != null)
            {
                // Primero buscar directamente en la sala
                foreach (var objId in room.ObjectIds.ToList())
                {
                    var candidate = _state.Objects.FirstOrDefault(o => o.Id == objId);
                    if (candidate != null && candidate.Visible && candidate.CanTake &&
                        MatchesName(candidate.Name, parsed.DirectObject, parsed.OriginalDirectObject))
                    {
                        // Coger el objeto automáticamente
                        room.ObjectIds.Remove(objId);
                        candidate.RoomId = null; // Ya no está en ninguna sala
                        _state.InventoryObjectIds.Add(objId);
                        obj = candidate;
                        pickedUp = true;
                        break;
                    }
                }

                // Si no se encontró, buscar dentro de contenedores abiertos en la sala
                if (obj == null)
                {
                    foreach (var containerId in room.ObjectIds)
                    {
                        var container = _state.Objects.FirstOrDefault(o => o.Id == containerId);
                        if (container != null && container.Visible && container.IsContainer &&
                            container.IsOpen && container.ContentsVisible)
                        {
                            foreach (var itemId in container.ContainedObjectIds.ToList())
                            {
                                var candidate = _state.Objects.FirstOrDefault(o => o.Id == itemId);
                                if (candidate != null && candidate.Visible && candidate.CanTake &&
                                    MatchesName(candidate.Name, parsed.DirectObject, parsed.OriginalDirectObject))
                                {
                                    // Coger el objeto del contenedor automáticamente
                                    container.ContainedObjectIds.Remove(itemId);
                                    candidate.RoomId = null; // Ya no está en ninguna sala
                                    _state.InventoryObjectIds.Add(itemId);
                                    obj = candidate;
                                    sourceContainer = container;
                                    pickedUp = true;
                                    break;
                                }
                            }
                            if (obj != null) break;
                        }
                    }
                }
            }
        }

        if (obj == null)
            return CommandResult.Error($"No tienes '{parsed.OriginalDirectObject ?? parsed.DirectObject}' en tu inventario.");

        // Helper para formatear mensajes con el prefijo de recoger
        string GetPickupPrefix() => sourceContainer != null
            ? $"Coges {WithArticle(obj)} de {WithArticle(sourceContainer)} y "
            : $"Coges {WithArticle(obj)} y ";
        string FormatEquipMessage(string message) =>
            pickedUp ? $"{GetPickupPrefix()}{char.ToLower(message[0])}{message[1..]}" : message;

        // Verificar que es un arma o armadura
        if (obj.Type == ObjectType.Arma)
        {
            // Quitar el objeto del inventario antes de equipar
            _state.InventoryObjectIds.Remove(obj.Id);

            if (obj.HandsRequired >= 2)
            {
                // Arma de 2 manos: ocupar ambas manos
                var messages = new List<string>();

                // Desequipar lo que haya en ambas manos y devolverlo al inventario
                if (_state.Player.EquippedRightHandId != null)
                {
                    var prevRight = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedRightHandId);
                    if (prevRight != null)
                    {
                        _state.InventoryObjectIds.Add(prevRight.Id);
                        messages.Add($"Guardas {WithArticle(prevRight)}");
                    }
                }
                if (_state.Player.EquippedLeftHandId != null && _state.Player.EquippedLeftHandId != _state.Player.EquippedRightHandId)
                {
                    var prevLeft = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedLeftHandId);
                    if (prevLeft != null)
                    {
                        _state.InventoryObjectIds.Add(prevLeft.Id);
                        messages.Add($"guardas {WithArticle(prevLeft)}");
                    }
                }

                _state.Player.EquippedRightHandId = obj.Id;
                _state.Player.EquippedLeftHandId = obj.Id; // Mismo ID en ambas manos para arma de 2 manos

                string result;
                if (messages.Count > 0)
                {
                    var equipPart = string.Join(", ", messages) + $" y empuñas {WithArticle(obj)} con ambas manos.";
                    result = pickedUp ? $"{GetPickupPrefix()}{char.ToLower(equipPart[0])}{equipPart[1..]}" : equipPart;
                }
                else
                {
                    result = FormatEquipMessage($"Empuñas {WithArticle(obj)} con ambas manos.");
                }
                return CommandResult.Success(result);
            }
            else
            {
                // Arma de 1 mano: preferir mano derecha si está libre
                if (_state.Player.EquippedRightHandId == null)
                {
                    _state.Player.EquippedRightHandId = obj.Id;
                    return CommandResult.Success(FormatEquipMessage($"Empuñas {WithArticle(obj)} en tu mano derecha."));
                }
                else if (_state.Player.EquippedLeftHandId == null)
                {
                    _state.Player.EquippedLeftHandId = obj.Id;
                    return CommandResult.Success(FormatEquipMessage($"Empuñas {WithArticle(obj)} en tu mano izquierda."));
                }
                else
                {
                    // Ambas manos ocupadas: reemplazar mano derecha
                    var prevRight = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedRightHandId);
                    // Si la mano izquierda tenía la misma arma (arma de 2 manos), liberarla
                    if (_state.Player.EquippedLeftHandId == _state.Player.EquippedRightHandId)
                        _state.Player.EquippedLeftHandId = null;

                    // Devolver arma anterior al inventario
                    if (prevRight != null)
                        _state.InventoryObjectIds.Add(prevRight.Id);

                    _state.Player.EquippedRightHandId = obj.Id;

                    if (prevRight != null)
                    {
                        var msg = $"Guardas {WithArticle(prevRight)} y empuñas {WithArticle(obj)}.";
                        return CommandResult.Success(pickedUp ? $"{GetPickupPrefix()}{char.ToLower(msg[0])}{msg[1..]}" : msg);
                    }
                    return CommandResult.Success(FormatEquipMessage($"Empuñas {WithArticle(obj)}."));
                }
            }
        }
        else if (obj.Type == ObjectType.Armadura)
        {
            // Verificar restricción de peso de armadura
            var totalArmorWeight = GetEquippedArmorWeight();
            var currentTorsoArmor = _state.Player.EquippedTorsoId != null
                ? _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedTorsoId)
                : null;
            if (currentTorsoArmor != null)
                totalArmorWeight -= currentTorsoArmor.Weight;
            totalArmorWeight += obj.Weight;

            var maxArmorWeight = _state.Player.BodyWeight * 1000; // kg a gramos
            if (totalArmorWeight > maxArmorWeight)
                return CommandResult.Error($"No puedes llevar tanta armadura. Tu peso corporal ({_state.Player.BodyWeight} kg) no lo permite.");

            // Quitar el objeto del inventario antes de equipar
            _state.InventoryObjectIds.Remove(obj.Id);

            var previousArmorId = _state.Player.EquippedTorsoId;
            _state.Player.EquippedTorsoId = obj.Id;

            if (previousArmorId != null)
            {
                // Devolver armadura anterior al inventario
                _state.InventoryObjectIds.Add(previousArmorId);
                var prevObj = _state.Objects.FirstOrDefault(o => o.Id == previousArmorId);
                if (prevObj != null)
                {
                    var msg = $"Te quitas {WithArticle(prevObj)} y te pones {WithArticle(obj)}.";
                    return CommandResult.Success(pickedUp ? $"{GetPickupPrefix()}{char.ToLower(msg[0])}{msg[1..]}" : msg);
                }
            }

            return CommandResult.Success(FormatEquipMessage($"Te pones {WithArticle(obj)}."));
        }
        else if (obj.Type == ObjectType.Escudo)
        {
            // Verificar si la mano izquierda está ocupada por un arma de 2 manos
            if (_state.Player.EquippedLeftHandId != null &&
                _state.Player.EquippedLeftHandId == _state.Player.EquippedRightHandId)
            {
                var twoHandedWeapon = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedRightHandId);
                if (twoHandedWeapon != null)
                    return CommandResult.Error($"No puedes equipar {WithArticle(obj)} mientras empuñas {WithArticle(twoHandedWeapon)} con ambas manos.");
            }

            // Quitar el objeto del inventario antes de equipar
            _state.InventoryObjectIds.Remove(obj.Id);

            var previousShieldId = _state.Player.EquippedLeftHandId;
            _state.Player.EquippedLeftHandId = obj.Id;

            if (previousShieldId != null)
            {
                // Devolver escudo/objeto anterior al inventario
                _state.InventoryObjectIds.Add(previousShieldId);
                var prevObj = _state.Objects.FirstOrDefault(o => o.Id == previousShieldId);
                if (prevObj != null)
                {
                    var msg = $"Guardas {WithArticle(prevObj)} y equipas {WithArticle(obj)} en tu mano izquierda.";
                    return CommandResult.Success(pickedUp ? $"{GetPickupPrefix()}{char.ToLower(msg[0])}{msg[1..]}" : msg);
                }
            }

            return CommandResult.Success(FormatEquipMessage($"Equipas {WithArticle(obj)} en tu mano izquierda."));
        }
        else if (obj.Type == ObjectType.Casco)
        {
            // Verificar restricción de peso de armadura
            var totalArmorWeight = GetEquippedArmorWeight();
            var currentHeadArmor = _state.Player.EquippedHeadId != null
                ? _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedHeadId)
                : null;
            if (currentHeadArmor != null)
                totalArmorWeight -= currentHeadArmor.Weight;
            totalArmorWeight += obj.Weight;

            var maxArmorWeight = _state.Player.BodyWeight * 1000; // kg a gramos
            if (totalArmorWeight > maxArmorWeight)
                return CommandResult.Error($"No puedes llevar tanta armadura. Tu peso corporal ({_state.Player.BodyWeight} kg) no lo permite.");

            // Quitar el objeto del inventario antes de equipar
            _state.InventoryObjectIds.Remove(obj.Id);

            var previousHelmetId = _state.Player.EquippedHeadId;
            _state.Player.EquippedHeadId = obj.Id;

            if (previousHelmetId != null)
            {
                // Devolver casco anterior al inventario
                _state.InventoryObjectIds.Add(previousHelmetId);
                var prevObj = _state.Objects.FirstOrDefault(o => o.Id == previousHelmetId);
                if (prevObj != null)
                {
                    var msg = $"Te quitas {WithArticle(prevObj)} y te pones {WithArticle(obj)}.";
                    return CommandResult.Success(pickedUp ? $"{GetPickupPrefix()}{char.ToLower(msg[0])}{msg[1..]}" : msg);
                }
            }

            return CommandResult.Success(FormatEquipMessage($"Te pones {WithArticle(obj)}."));
        }
        else
        {
            return CommandResult.Error($"No puedes equipar {WithArticle(obj)}.");
        }
    }

    /// <summary>
    /// Calcula el peso total de armaduras equipadas en todos los slots.
    /// </summary>
    private int GetEquippedArmorWeight()
    {
        int total = 0;

        // Mano derecha (si es armadura/escudo)
        if (_state.Player.EquippedRightHandId != null)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedRightHandId);
            if (obj?.Type == ObjectType.Armadura)
                total += obj.Weight;
        }

        // Mano izquierda (si es armadura/escudo y no es el mismo que mano derecha)
        if (_state.Player.EquippedLeftHandId != null && _state.Player.EquippedLeftHandId != _state.Player.EquippedRightHandId)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedLeftHandId);
            if (obj?.Type == ObjectType.Armadura)
                total += obj.Weight;
        }

        // Torso
        if (_state.Player.EquippedTorsoId != null)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedTorsoId);
            if (obj?.Type == ObjectType.Armadura)
                total += obj.Weight;
        }

        // Cabeza (casco)
        if (_state.Player.EquippedHeadId != null)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedHeadId);
            if (obj?.Type == ObjectType.Casco)
                total += obj.Weight;
        }

        return total;
    }

    /// <summary>
    /// Maneja el comando desequipar.
    /// </summary>
    private CommandResult HandleUnequip(ParsedCommand parsed)
    {
        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhatToUnequip);

        // Buscar el objeto equipado en cualquier slot
        GameObject? obj = null;
        string? slot = null;

        // Mano derecha
        if (_state.Player.EquippedRightHandId != null)
        {
            var rightHand = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedRightHandId);
            if (rightHand != null && MatchesName(rightHand.Name, parsed.DirectObject, parsed.OriginalDirectObject))
            {
                obj = rightHand;
                slot = "right";
            }
        }

        // Mano izquierda (si no es el mismo objeto que mano derecha)
        if (obj == null && _state.Player.EquippedLeftHandId != null && _state.Player.EquippedLeftHandId != _state.Player.EquippedRightHandId)
        {
            var leftHand = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedLeftHandId);
            if (leftHand != null && MatchesName(leftHand.Name, parsed.DirectObject, parsed.OriginalDirectObject))
            {
                obj = leftHand;
                slot = "left";
            }
        }

        // Torso
        if (obj == null && _state.Player.EquippedTorsoId != null)
        {
            var torso = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedTorsoId);
            if (torso != null && MatchesName(torso.Name, parsed.DirectObject, parsed.OriginalDirectObject))
            {
                obj = torso;
                slot = "torso";
            }
        }

        // Cabeza
        if (obj == null && _state.Player.EquippedHeadId != null)
        {
            var head = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedHeadId);
            if (head != null && MatchesName(head.Name, parsed.DirectObject, parsed.OriginalDirectObject))
            {
                obj = head;
                slot = "head";
            }
        }

        if (obj == null)
            return CommandResult.Error($"No tienes '{parsed.OriginalDirectObject ?? parsed.DirectObject}' equipado.");

        // Desequipar según el slot
        if (slot == "right")
        {
            // Si es arma de 2 manos, liberar ambas manos
            if (_state.Player.EquippedLeftHandId == _state.Player.EquippedRightHandId)
                _state.Player.EquippedLeftHandId = null;
            _state.Player.EquippedRightHandId = null;
        }
        else if (slot == "left")
        {
            _state.Player.EquippedLeftHandId = null;
        }
        else if (slot == "torso")
        {
            _state.Player.EquippedTorsoId = null;
        }
        else if (slot == "head")
        {
            _state.Player.EquippedHeadId = null;
        }

        // Devolver el objeto al inventario
        _state.InventoryObjectIds.Add(obj.Id);

        if (obj.Type == ObjectType.Arma)
            return CommandResult.Success($"Guardas {WithArticle(obj)}.");
        else
            return CommandResult.Success($"Te quitas {WithArticle(obj)}.");
    }

    /// <summary>
    /// Maneja el comando saquear (para cadáveres).
    /// </summary>
    private CommandResult HandleLoot(ParsedCommand parsed)
    {
        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhatToLoot);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Buscar el NPC cadáver en la sala
        var corpses = _state.Npcs
            .Where(n => n.Visible && room.NpcIds.Contains(n.Id) && n.IsCorpse)
            .ToList();

        var npc = MatchNpcByName(corpses, parsed.DirectObject);
        if (npc == null)
        {
            // Intentar buscar por "cadáver" o el nombre original
            npc = corpses.FirstOrDefault(n =>
                n.Name.Contains("cadáver", StringComparison.OrdinalIgnoreCase) ||
                parsed.DirectObject.Contains("cadaver", StringComparison.OrdinalIgnoreCase));
        }

        if (npc == null)
            return CommandResult.Error($"No ves ningún cadáver de '{parsed.OriginalDirectObject ?? parsed.DirectObject}' aquí.");

        if (!npc.IsCorpse)
            return CommandResult.Error($"{npc.Name} no está muerto.");

        // Verificar si hay algo que saquear
        var hasInventory = npc.Inventory.Count > 0;
        var hasEquipment = npc.EquippedRightHandId != null || npc.EquippedLeftHandId != null || npc.EquippedTorsoId != null || npc.EquippedHeadId != null;
        var hasMoney = npc.Money > 0;

        if (!hasInventory && !hasEquipment && !hasMoney)
            return CommandResult.Success($"El cadáver de {npc.Name} no tiene nada de valor.");

        var sb = new StringBuilder();
        sb.AppendLine($"Saqueas el cadáver de {npc.Name} y encuentras:");

        // Transferir equipamiento
        var equippedIds = new HashSet<string>();
        if (npc.EquippedRightHandId != null)
            equippedIds.Add(npc.EquippedRightHandId);
        if (npc.EquippedLeftHandId != null && npc.EquippedLeftHandId != npc.EquippedRightHandId)
            equippedIds.Add(npc.EquippedLeftHandId);
        if (npc.EquippedTorsoId != null)
            equippedIds.Add(npc.EquippedTorsoId);
        if (npc.EquippedHeadId != null)
            equippedIds.Add(npc.EquippedHeadId);

        foreach (var eqId in equippedIds)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == eqId);
            if (obj != null)
            {
                _state.InventoryObjectIds.Add(obj.Id);
                sb.AppendLine($"  - {obj.Name}");
            }
        }
        npc.EquippedRightHandId = null;
        npc.EquippedLeftHandId = null;
        npc.EquippedTorsoId = null;
        npc.EquippedHeadId = null;

        // Transferir inventario
        foreach (var item in npc.Inventory.ToList())
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == item.ObjectId);
            if (obj != null)
            {
                _state.InventoryObjectIds.Add(item.ObjectId);
                sb.AppendLine(item.Quantity > 1 ? $"  - {obj.Name} x{item.Quantity}" : $"  - {obj.Name}");
            }
        }
        npc.Inventory.Clear();

        // Añadir dinero del NPC
        if (npc.Money > 0)
        {
            _state.Player.Money += npc.Money;
            sb.AppendLine($"  - {npc.Money} de dinero");
            npc.Money = 0;
        }

        return CommandResult.Success(sb.ToString().TrimEnd());
    }

    /// <summary>
    /// Maneja el comando encender un objeto luminoso.
    /// Si se usa "encender X con Y", Y es el objeto encendedor.
    /// Si solo se usa "encender X", se enciende sin objeto (si está permitido).
    /// </summary>
    private CommandResult HandleIgnite(ParsedCommand parsed)
    {
        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhatToIgnite);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Buscar el objeto a encender en inventario o en la sala
        var obj = FindAccessibleObject(parsed.DirectObject, parsed.OriginalDirectObject, room);
        if (obj == null)
            return CommandResult.Error($"No ves ningún '{parsed.OriginalDirectObject ?? parsed.DirectObject}' aquí.");

        // Verificar que es un objeto luminoso
        if (!obj.IsLightSource)
            return CommandResult.Error(RandomMessages.GetCannotIgnite(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Verificar que se puede encender
        if (!obj.CanIgnite)
            return CommandResult.Error(RandomMessages.GetCannotIgnite(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Si ya está encendido
        if (obj.IsLit)
            return CommandResult.Error(RandomMessages.GetAlreadyLit(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Verificar si necesita un objeto encendedor
        if (!string.IsNullOrEmpty(obj.IgniterObjectId))
        {
            // Necesita un objeto específico para encender
            if (parsed.Preposition != PrepositionKind.With || string.IsNullOrEmpty(parsed.IndirectObject))
                return CommandResult.Error($"Necesitas usar algo para encender {WithArticle(obj)}.");

            // Buscar el objeto encendedor en el inventario
            var igniter = _state.Objects.FirstOrDefault(o =>
                _state.InventoryObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase) &&
                MatchesName(o.Name, parsed.IndirectObject, parsed.OriginalIndirectObject));

            if (igniter == null)
                return CommandResult.Error($"No llevas ningún '{parsed.OriginalIndirectObject ?? parsed.IndirectObject}'.");

            // Verificar que es el objeto encendedor correcto
            if (!igniter.Id.Equals(obj.IgniterObjectId, StringComparison.OrdinalIgnoreCase))
            {
                var requiredIgniter = FindObjectById(obj.IgniterObjectId);
                var requiredName = requiredIgniter?.Name ?? "otro objeto";
                return CommandResult.Error($"No puedes encender {WithArticle(obj)} con {WithArticle(igniter)}. Necesitas {requiredName}.");
            }

            // Encender el objeto
            obj.IsLit = true;
            return CommandResult.Success($"Enciendes {WithArticle(obj)} con {WithArticle(igniter)}.");
        }
        else
        {
            // Se puede encender sin objeto encendedor
            obj.IsLit = true;
            return CommandResult.Success($"Enciendes {WithArticle(obj)}.");
        }
    }

    /// <summary>
    /// Maneja el comando apagar un objeto luminoso.
    /// </summary>
    private CommandResult HandleExtinguish(ParsedCommand parsed)
    {
        if (string.IsNullOrEmpty(parsed.DirectObject))
            return CommandResult.Error(RandomMessages.WhatToExtinguish);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Buscar el objeto a apagar en inventario o en la sala
        var obj = FindAccessibleObject(parsed.DirectObject, parsed.OriginalDirectObject, room);
        if (obj == null)
            return CommandResult.Error($"No ves ningún '{parsed.OriginalDirectObject ?? parsed.DirectObject}' aquí.");

        // Verificar que es un objeto luminoso
        if (!obj.IsLightSource)
            return CommandResult.Error(RandomMessages.GetCannotExtinguish(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Verificar que se puede apagar
        if (!obj.CanExtinguish)
            return CommandResult.Error(RandomMessages.GetCannotExtinguish(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Si ya está apagado
        if (!obj.IsLit)
            return CommandResult.Error(RandomMessages.GetAlreadyOff(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Apagar el objeto
        obj.IsLit = false;
        return CommandResult.Success($"Apagas {WithArticle(obj)}.");
    }

    /// <summary>
    /// Maneja el comando de fabricación.
    /// </summary>
    private CommandResult HandleCraft(ParsedCommand parsed)
    {
        // Verificar si el sistema de fabricación está activo
        if (!_world.Game.CraftingEnabled)
            return CommandResult.Error("No puedes fabricar nada aquí.");

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Disparar evento para abrir ventana de fabricación
        CraftOpened?.Invoke();

        return CommandResult.Success("");
    }

    #region Inventory Capacity Helpers

    /// <summary>
    /// Verifica si el jugador puede añadir un objeto a su inventario
    /// considerando los límites de peso y volumen.
    /// </summary>
    private bool CanAddToInventory(GameObject obj)
    {
        var player = _state.Player;

        // Si no hay límites, siempre cabe
        if (player.MaxInventoryWeight < 0 && player.MaxInventoryVolume < 0)
            return true;

        // Calcular peso/volumen actual del inventario
        var currentWeight = GetCurrentInventoryWeight();
        var currentVolume = GetCurrentInventoryVolume();

        // Verificar peso
        if (player.MaxInventoryWeight >= 0)
        {
            if (currentWeight + obj.Weight > player.MaxInventoryWeight)
                return false;
        }

        // Verificar volumen
        if (player.MaxInventoryVolume >= 0)
        {
            if (currentVolume + obj.Volume > player.MaxInventoryVolume)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Calcula el peso total actual del inventario del jugador en gramos.
    /// </summary>
    private int GetCurrentInventoryWeight()
    {
        return _state.InventoryObjectIds
            .Select(id => _state.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o != null)
            .Sum(o => o!.Weight);
    }

    /// <summary>
    /// Calcula el volumen total actual del inventario del jugador en cm³.
    /// </summary>
    private double GetCurrentInventoryVolume()
    {
        return _state.InventoryObjectIds
            .Select(id => _state.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o != null)
            .Sum(o => o!.Volume);
    }

    #endregion

    /// <summary>
    /// Busca un objeto accesible por nombre en inventario, sala, o contenedores abiertos/visibles.
    /// </summary>
    private GameObject? FindAccessibleObject(string normalizedName, string? originalName, Room room)
    {
        // Buscar en inventario
        var invObj = _state.Objects.FirstOrDefault(o =>
            _state.InventoryObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase) &&
            MatchesName(o.Name, normalizedName, originalName));
        if (invObj != null) return invObj;

        // Buscar en la sala (objetos de nivel superior)
        var roomObj = _state.Objects.FirstOrDefault(o =>
            room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase) &&
            o.Visible &&
            MatchesName(o.Name, normalizedName, originalName));
        if (roomObj != null) return roomObj;

        // Buscar en contenedores abiertos o con contenido visible
        foreach (var containerId in room.ObjectIds)
        {
            var container = FindObjectById(containerId);
            if (container == null || !container.IsContainer) continue;
            if (!container.IsOpen && !container.ContentsVisible) continue;

            foreach (var containedId in container.ContainedObjectIds)
            {
                var contained = FindObjectById(containedId);
                if (contained != null && contained.Visible && MatchesName(contained.Name, normalizedName, originalName))
                    return contained;
            }
        }

        return null;
    }

    /// <summary>
    /// Describe el equipamiento actual del jugador.
    /// </summary>
    private string DescribeEquipment()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Tu equipamiento:");

        // Mano derecha
        var rightHandId = _state.Player.EquippedRightHandId;
        var leftHandId = _state.Player.EquippedLeftHandId;

        // Verificar si es arma de 2 manos (mismo ID en ambas manos)
        var isTwoHanded = rightHandId != null && rightHandId == leftHandId;

        if (isTwoHanded)
        {
            var weapon = _state.Objects.FirstOrDefault(o => o.Id == rightHandId);
            if (weapon != null)
            {
                var durability = weapon.MaxDurability < 0 ? "" : $" ({weapon.CurrentDurability}/{weapon.MaxDurability})";
                var bonus = weapon.Type == ObjectType.Arma ? $"+{weapon.AttackBonus} ataque" : $"+{weapon.DefenseBonus} defensa";
                sb.AppendLine($"  Manos: {weapon.Name} ({bonus}){durability} [2 manos]");
            }
        }
        else
        {
            // Mano derecha
            if (rightHandId != null)
            {
                var rightHand = _state.Objects.FirstOrDefault(o => o.Id == rightHandId);
                if (rightHand != null)
                {
                    var durability = rightHand.MaxDurability < 0 ? "" : $" ({rightHand.CurrentDurability}/{rightHand.MaxDurability})";
                    var bonus = rightHand.Type == ObjectType.Arma ? $"+{rightHand.AttackBonus} ataque" : $"+{rightHand.DefenseBonus} defensa";
                    sb.AppendLine($"  Mano derecha: {rightHand.Name} ({bonus}){durability}");
                }
            }
            else
            {
                sb.AppendLine("  Mano derecha: (vacía)");
            }

            // Mano izquierda
            if (leftHandId != null)
            {
                var leftHand = _state.Objects.FirstOrDefault(o => o.Id == leftHandId);
                if (leftHand != null)
                {
                    var durability = leftHand.MaxDurability < 0 ? "" : $" ({leftHand.CurrentDurability}/{leftHand.MaxDurability})";
                    var bonus = leftHand.Type == ObjectType.Arma ? $"+{leftHand.AttackBonus} ataque" : $"+{leftHand.DefenseBonus} defensa";
                    sb.AppendLine($"  Mano izquierda: {leftHand.Name} ({bonus}){durability}");
                }
            }
            else
            {
                sb.AppendLine("  Mano izquierda: (vacía)");
            }
        }

        // Torso
        var torsoId = _state.Player.EquippedTorsoId;
        if (torsoId != null)
        {
            var torso = _state.Objects.FirstOrDefault(o => o.Id == torsoId);
            if (torso != null)
            {
                var durability = torso.MaxDurability < 0 ? "" : $" ({torso.CurrentDurability}/{torso.MaxDurability})";
                sb.AppendLine($"  Torso: {torso.Name} (+{torso.DefenseBonus} defensa){durability}");
            }
        }
        else
        {
            sb.AppendLine("  Torso: (vacío)");
        }

        // Cabeza
        var headId = _state.Player.EquippedHeadId;
        if (headId != null)
        {
            var head = _state.Objects.FirstOrDefault(o => o.Id == headId);
            if (head != null)
            {
                var durability = head.MaxDurability < 0 ? "" : $" ({head.CurrentDurability}/{head.MaxDurability})";
                sb.AppendLine($"  Cabeza: {head.Name} (+{head.DefenseBonus} defensa){durability}");
            }
        }
        else
        {
            sb.AppendLine("  Cabeza: (vacía)");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Busca un NPC por nombre en una lista.
    /// </summary>
    private Npc? MatchNpcByName(List<Npc> npcs, string name)
    {
        if (string.IsNullOrEmpty(name) || npcs.Count == 0)
            return null;

        // Coincidencia exacta
        var exact = npcs.FirstOrDefault(n =>
            n.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            n.Id.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (exact != null) return exact;

        // Coincidencia parcial
        return npcs.FirstOrDefault(n =>
            n.Name.Contains(name, StringComparison.OrdinalIgnoreCase) ||
            n.Id.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region Script Execution

    /// <summary>
    /// Ejecuta un script manualmente (para pruebas desde el editor).
    /// Busca el primer nodo de evento y lo ejecuta.
    /// </summary>
    public ScriptExecutionResult ExecuteScript(ScriptDefinition script)
    {
        if (_scriptEngine == null)
            return ScriptExecutionResult.Error("Motor de scripts no inicializado");

        if (script.Nodes.Count == 0)
            return ScriptExecutionResult.Error("El script no tiene nodos");

        // Buscar un nodo de evento para iniciar la ejecución
        var eventNode = script.Nodes.FirstOrDefault(n =>
            n.Category == NodeCategory.Event);

        if (eventNode == null)
            return ScriptExecutionResult.Error("El script no tiene ningún evento de inicio");

        // Capturar mensajes durante la ejecución
        var messages = new List<string>();
        void messageHandler(string msg) => messages.Add(msg);

        try
        {
            // Suscribirse temporalmente a los mensajes
            ScriptMessage += messageHandler;

            // Ejecutar el evento del script
            var task = _scriptEngine.TriggerEventAsync(script.OwnerType, script.OwnerId, eventNode.NodeType.ToString());
            task.Wait();

            return ScriptExecutionResult.Ok(messages);
        }
        catch (Exception ex)
        {
            return ScriptExecutionResult.Error(ex.Message);
        }
        finally
        {
            ScriptMessage -= messageHandler;
        }
    }

    /// <summary>
    /// Ejecuta un nodo de acción individual (para pruebas desde el editor de scripts).
    /// </summary>
    public ScriptExecutionResult ExecuteSingleAction(ScriptNode actionNode)
    {
        if (_scriptEngine == null)
            return ScriptExecutionResult.Error("Motor de scripts no inicializado");

        // Capturar mensajes durante la ejecución
        var messages = new List<string>();
        void messageHandler(string msg) => messages.Add(msg);

        try
        {
            // Suscribirse temporalmente a los mensajes
            ScriptMessage += messageHandler;

            // Ejecutar la acción directamente
            var task = _scriptEngine.ExecuteSingleNodeAsync(actionNode);
            task.Wait();

            return ScriptExecutionResult.Ok(messages);
        }
        catch (Exception ex)
        {
            return ScriptExecutionResult.Error(ex.Message);
        }
        finally
        {
            ScriptMessage -= messageHandler;
        }
    }

    #endregion

    #region Basic Needs

    /// <summary>
    /// Procesa las necesidades básicas del jugador al final de cada turno.
    /// Retorna un mensaje para mostrar al jugador si corresponde, o null.
    /// </summary>
    private string? ProcessBasicNeeds()
    {
        if (!_world.Game.BasicNeedsEnabled) return null;

        var stats = _state.Player.DynamicStats;
        var messages = new List<string>();

        // Acumular incrementos fraccionarios (tasas base: hambre=1.3, sed=1.0, sueño=0.7)
        stats.HungerAccumulator += 1.3 * GetNeedRateModifier(_world.Game.HungerRate);
        stats.ThirstAccumulator += 1.0 * GetNeedRateModifier(_world.Game.ThirstRate);
        stats.SleepAccumulator += 0.7 * GetNeedRateModifier(_world.Game.SleepRate);

        // Convertir acumuladores a incrementos enteros
        var hungerInc = (int)stats.HungerAccumulator;
        var thirstInc = (int)stats.ThirstAccumulator;
        var sleepInc = (int)stats.SleepAccumulator;

        // Restar la parte entera del acumulador
        stats.HungerAccumulator -= hungerInc;
        stats.ThirstAccumulator -= thirstInc;
        stats.SleepAccumulator -= sleepInc;

        var oldHunger = stats.Hunger;
        var oldThirst = stats.Thirst;
        var oldSleep = stats.Sleep;

        stats.Hunger = Math.Min(100, stats.Hunger + hungerInc);
        stats.Thirst = Math.Min(100, stats.Thirst + thirstInc);
        stats.Sleep = Math.Min(100, stats.Sleep + sleepInc);

        // Verificar muerte (100)
        if (stats.Hunger >= 100)
        {
            PlayerDied?.Invoke(DeathType.Hunger);
            return null;
        }
        if (stats.Thirst >= 100)
        {
            PlayerDied?.Invoke(DeathType.Thirst);
            return null;
        }
        if (stats.Sleep >= 100)
        {
            PlayerDied?.Invoke(DeathType.Sleep);
            return null;
        }

        // Mensajes de advertencia (solo al cruzar umbral)
        CheckNeedThreshold(oldHunger, stats.Hunger, "hambre", messages);
        CheckNeedThreshold(oldThirst, stats.Thirst, "sed", messages);
        CheckNeedThreshold(oldSleep, stats.Sleep, "sueño", messages);

        return messages.Count > 0 ? string.Join("\n", messages) : null;
    }

    /// <summary>
    /// Verifica si el jugador ha muerto por perder toda la salud o cordura.
    /// </summary>
    private void CheckVitalStats()
    {
        var stats = _state.Player.DynamicStats;

        if (stats.Health <= 0)
        {
            PlayerDied?.Invoke(DeathType.Health);
            return;
        }

        if (stats.Sanity <= 0)
        {
            PlayerDied?.Invoke(DeathType.Sanity);
        }
    }

    private void CheckNeedThreshold(int oldValue, int newValue, string needName, List<string> messages)
    {
        if (oldValue < 70 && newValue >= 70)
            messages.Add($"[Sientes algo de {needName}.]");
        else if (oldValue < 80 && newValue >= 80)
            messages.Add($"[Tu {needName} se hace más intensa.]");
        else if (oldValue < 90 && newValue >= 90)
            messages.Add($"[¡Tu {needName} es crítica! Necesitas hacer algo pronto.]");
    }

    private static double GetNeedRateModifier(NeedRate rate) => rate switch
    {
        NeedRate.Low => 0.5,
        NeedRate.Normal => 1.0,
        NeedRate.High => 1.5,
        _ => 1.0
    };

    /// <summary>
    /// Busca un objeto visible para el jugador (en inventario, sala o contenedor visible).
    /// </summary>
    private GameObject? FindVisibleObject(Room room, string name, string? originalName = null)
    {
        // Primero buscar en inventario
        foreach (var objId in _state.InventoryObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null && MatchesName(obj.Name, name, originalName))
                return obj;
        }

        // Luego buscar en la sala
        foreach (var objId in room.ObjectIds)
        {
            var obj = FindObjectById(objId);
            if (obj != null && obj.Visible && MatchesName(obj.Name, name, originalName))
                return obj;
        }

        // Buscar en contenedores visibles (abiertos o con contenido visible)
        var allContainers = room.ObjectIds
            .Select(FindObjectById)
            .Where(o => o != null && o.IsContainer && o.Visible && (o.IsOpen || o.ContentsVisible))
            .Concat(_state.InventoryObjectIds
                .Select(FindObjectById)
                .Where(o => o != null && o.IsContainer && (o.IsOpen || o.ContentsVisible)));

        foreach (var container in allContainers)
        {
            if (container == null) continue;
            foreach (var containedId in container.ContainedObjectIds)
            {
                var contained = FindObjectById(containedId);
                if (contained != null && contained.Visible && MatchesName(contained.Name, name, originalName))
                    return contained;
            }
        }

        return null;
    }

    /// <summary>
    /// Maneja el comando comer.
    /// </summary>
    private CommandResult HandleEat(ParsedCommand parsed)
    {
        // Verificar que el sistema de necesidades básicas esté activo
        if (!_world.Game.BasicNeedsEnabled)
            return CommandResult.Error(RandomMessages.BasicNeedsNotEnabled);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var objName = (parsed.DirectObject ?? string.Empty).Trim();
        var originalObjName = parsed.OriginalDirectObject;

        if (string.IsNullOrWhiteSpace(objName))
            return CommandResult.Error(RandomMessages.WhatToEat);

        // Buscar el objeto
        var obj = FindVisibleObject(room, objName, originalObjName);
        if (obj == null)
            return CommandResult.Error(RandomMessages.ObjectNotFound);

        // Verificar que sea comida
        if (obj.Type != ObjectType.Comida)
            return CommandResult.Error(RandomMessages.GetCannotEat(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Consumir el objeto
        var stats = _state.Player.DynamicStats;
        var oldHunger = stats.Hunger;
        stats.Hunger = Math.Max(0, stats.Hunger - obj.NutritionAmount);

        // Eliminar el objeto del juego
        RemoveObjectFromGame(obj);

        // Disparar evento de comer
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnEat");

        // Mostrar mensaje
        var message = RandomMessages.GetEatSuccess(Low(obj.Name), obj.Gender, obj.IsPlural);
        if (oldHunger > 0 && stats.Hunger == 0)
            message += " " + RandomMessages.NotHungry;

        return CommandResult.Success(message);
    }

    /// <summary>
    /// Maneja el comando beber.
    /// </summary>
    private CommandResult HandleDrink(ParsedCommand parsed)
    {
        // Verificar que el sistema de necesidades básicas esté activo
        if (!_world.Game.BasicNeedsEnabled)
            return CommandResult.Error(RandomMessages.BasicNeedsNotEnabled);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var objName = (parsed.DirectObject ?? string.Empty).Trim();
        var originalObjName = parsed.OriginalDirectObject;

        if (string.IsNullOrWhiteSpace(objName))
            return CommandResult.Error(RandomMessages.WhatToDrink);

        // Buscar el objeto
        var obj = FindVisibleObject(room, objName, originalObjName);
        if (obj == null)
            return CommandResult.Error(RandomMessages.ObjectNotFound);

        // Verificar que sea bebida
        if (obj.Type != ObjectType.Bebida)
            return CommandResult.Error(RandomMessages.GetCannotDrink(Cap(obj.Name), obj.Gender, obj.IsPlural));

        // Consumir el objeto
        var stats = _state.Player.DynamicStats;
        var oldThirst = stats.Thirst;
        stats.Thirst = Math.Max(0, stats.Thirst - obj.NutritionAmount);

        // Eliminar el objeto del juego
        RemoveObjectFromGame(obj);

        // Disparar evento de beber
        _ = TriggerEntityScriptAsync("GameObject", obj.Id, "Event_OnDrink");

        // Mostrar mensaje
        var message = RandomMessages.GetDrinkSuccess(Low(obj.Name), obj.Gender, obj.IsPlural);
        if (oldThirst > 0 && stats.Thirst == 0)
            message += " " + RandomMessages.NotThirsty;

        return CommandResult.Success(message);
    }

    /// <summary>
    /// Elimina un objeto del juego (inventario, sala o contenedor).
    /// </summary>
    private void RemoveObjectFromGame(GameObject obj)
    {
        // Eliminar del inventario
        _state.InventoryObjectIds.Remove(obj.Id);

        // Eliminar de la sala
        var rooms = _state.Rooms.Where(r => r.ObjectIds.Contains(obj.Id));
        foreach (var room in rooms)
            room.ObjectIds.Remove(obj.Id);

        // Eliminar de contenedores
        foreach (var container in _state.Objects.Where(o => o.IsContainer))
            container.ContainedObjectIds.Remove(obj.Id);

        // Eliminar el objeto del estado
        _state.Objects.RemoveAll(o => o.Id == obj.Id);
    }

    /// <summary>
    /// Estado de sueño pendiente para responder a la pregunta de horas.
    /// </summary>
    private bool _awaitingSleepHours;

    /// <summary>
    /// Maneja el comando dormir.
    /// </summary>
    private CommandResult HandleSleep(ParsedCommand parsed)
    {
        // Verificar que el sistema de necesidades básicas esté activo
        if (!_world.Game.BasicNeedsEnabled)
            return CommandResult.Error(RandomMessages.BasicNeedsNotEnabled);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        // Si ya tenemos un número de horas especificado
        var hoursStr = (parsed.DirectObject ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(hoursStr) && int.TryParse(hoursStr, out int hours))
        {
            return ExecuteSleep(hours);
        }

        // Si no hay número, pedir las horas
        _awaitingSleepHours = true;
        return CommandResult.Success(RandomMessages.HowManyHoursToSleep);
    }

    /// <summary>
    /// Procesa la respuesta de horas de sueño.
    /// </summary>
    public bool TryProcessSleepResponse(string input, out CommandResult result)
    {
        result = CommandResult.Empty;

        if (!_awaitingSleepHours)
            return false;

        _awaitingSleepHours = false;

        if (int.TryParse(input.Trim(), out int hours))
        {
            result = ExecuteSleep(hours);
            return true;
        }

        result = CommandResult.Error(RandomMessages.InvalidSleepHours);
        return true;
    }

    /// <summary>
    /// Ejecuta el proceso de dormir por las horas especificadas.
    /// </summary>
    private CommandResult ExecuteSleep(int hours)
    {
        if (hours < 1 || hours > 8)
            return CommandResult.Error(RandomMessages.InvalidSleepHours);

        var room = CurrentRoom;
        if (room == null)
            return CommandResult.Error(RandomMessages.PlayerLost);

        var stats = _state.Player.DynamicStats;
        var messages = new StringBuilder();
        bool wokeUpStartled = false;
        int hoursSlept = 0;

        // Disparar evento de inicio de sueño
        _ = TriggerEntityScriptAsync("Player", "player", "Event_OnSleep");

        // Calcular cuántos turnos por hora del juego
        // MinutesPerGameHour = minutos reales por hora del juego
        // Asumimos 1 turno = 1 minuto real (aproximadamente)
        int turnsPerGameHour = Math.Max(1, _world.Game.MinutesPerGameHour);

        for (int hour = 1; hour <= hours; hour++)
        {
            // Simular los turnos de esa hora
            for (int turn = 0; turn < turnsPerGameHour; turn++)
            {
                // Guardar NPCs en la sala antes de procesar el turno
                var npcsBeforeTurn = room.NpcIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

                // Procesar el turno (como si el jugador escribiera "z")
                _state.TurnCounter++;
                UpdateGameTimeFromReal();
                _ = UpdateLightSources();
                UpdateNpcPatrols();
                var needsMsg = ProcessBasicNeeds();

                // Verificar si un NPC entró a la sala
                var npcsAfterTurn = room.NpcIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
                var newNpcs = npcsAfterTurn.Except(npcsBeforeTurn).ToList();

                if (newNpcs.Any())
                {
                    // Alguien entró, despertar sobresaltado
                    messages.AppendLine(RandomMessages.SomeoneEnteredWhileSleeping);
                    messages.AppendLine(RandomMessages.WakeUpStartled);
                    stats.Sanity = Math.Max(0, stats.Sanity - 10);
                    wokeUpStartled = true;
                    break;
                }

                // Verificar si alguna necesidad supera 80
                if (stats.Hunger > 80 || stats.Thirst > 80 || stats.Sleep > 80)
                {
                    messages.AppendLine(RandomMessages.NeedWokeYouUp);
                    messages.AppendLine(RandomMessages.WakeUpStartled);
                    stats.Sanity = Math.Max(0, stats.Sanity - 10);
                    wokeUpStartled = true;
                    break;
                }
            }

            if (wokeUpStartled)
                break;

            // Reducir sueño por cada hora dormida
            stats.Sleep = Math.Max(0, stats.Sleep - 10);
            hoursSlept++;

            // Mostrar progreso
            messages.AppendLine(RandomMessages.SleepProgress(hoursSlept, hours));
        }

        // Mensaje final si no se despertó sobresaltado
        if (!wokeUpStartled && hoursSlept == hours)
        {
            messages.AppendLine(RandomMessages.WakeUpNormal);
        }

        // Disparar evento de despertar
        _ = TriggerEntityScriptAsync("Player", "player", wokeUpStartled ? "Event_OnWakeUpStartled" : "Event_OnWakeUp");

        return CommandResult.Success(messages.ToString().TrimEnd());
    }

    #endregion
}