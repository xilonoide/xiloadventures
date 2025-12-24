using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Handles loading and saving world models (.xaw files).
/// Supports both compressed (ZIP+Base64) and raw JSON formats.
/// </summary>
public static class WorldLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    /// <summary>
    /// Loads a world model from a .xaw file.
    /// </summary>
    /// <param name="path">Path to the .xaw file.</param>
    /// <param name="encryptionKey">Optional encryption key (currently unused).</param>
    /// <param name="promptForKey">Optional callback to prompt user for encryption key.</param>
    /// <returns>The loaded and normalized world model.</returns>
    /// <exception cref="InvalidDataException">Thrown if the file cannot be parsed.</exception>
    public static WorldModel LoadWorldModel(string path, string? encryptionKey = null, Func<string?>? promptForKey = null)
    {
        // Leer archivo directamente sin cifrado
        var rawText = File.ReadAllText(path, Encoding.UTF8);

        if (string.IsNullOrWhiteSpace(rawText))
            throw new InvalidDataException("El archivo está vacío.");

        string json;
        if (!TryDecodeZippedJson(rawText, out json))
        {
            // Formato antiguo: el contenido es directamente JSON.
            json = rawText;
        }

        try
        {
            var world = JsonSerializer.Deserialize<WorldModel>(json, Options);
            if (world == null)
                throw new InvalidDataException("El JSON se deserializó como null.");
            return NormalizeWorld(world);
        }
        catch (JsonException ex)
        {
            // Construir mensaje de error detallado
            var errorDetails = new StringBuilder();
            errorDetails.AppendLine("Error de JSON:");
            errorDetails.AppendLine();
            errorDetails.AppendLine($"Mensaje: {ex.Message}");

            if (ex.LineNumber.HasValue)
                errorDetails.AppendLine($"Línea: {ex.LineNumber.Value + 1}");

            if (ex.BytePositionInLine.HasValue)
                errorDetails.AppendLine($"Posición: {ex.BytePositionInLine.Value}");

            if (!string.IsNullOrEmpty(ex.Path))
                errorDetails.AppendLine($"Ruta JSON: {ex.Path}");

            // Mostrar contexto del error si es posible
            if (ex.LineNumber.HasValue)
            {
                var lines = json.Split('\n');
                var errorLine = (int)ex.LineNumber.Value;
                errorDetails.AppendLine();
                errorDetails.AppendLine("Contexto:");

                // Mostrar 2 líneas antes y después del error
                for (int i = Math.Max(0, errorLine - 2); i <= Math.Min(lines.Length - 1, errorLine + 2); i++)
                {
                    var prefix = i == errorLine ? ">>> " : "    ";
                    var lineNum = (i + 1).ToString().PadLeft(4);
                    var lineContent = lines[i].TrimEnd('\r');
                    if (lineContent.Length > 80)
                        lineContent = lineContent.Substring(0, 77) + "...";
                    errorDetails.AppendLine($"{prefix}{lineNum}: {lineContent}");
                }
            }

            throw new InvalidDataException(errorDetails.ToString(), ex);
        }
    }

    private static WorldModel NormalizeWorld(WorldModel? world)
    {
        world ??= new WorldModel();

        // Asegurar listas inicializadas
        world.Rooms ??= new List<Room>();
        world.Objects ??= new List<GameObject>();
        world.Npcs ??= new List<Npc>();
        world.Quests ??= new List<QuestDefinition>();
        world.UseRules ??= new List<UseRule>();
        world.TradeRules ??= new List<TradeRule>();
        world.Events ??= new List<EventRule>();
        world.Doors ??= new List<Door>();
        world.RoomPositions ??= new Dictionary<string, MapPosition>();
        world.Musics ??= new List<MusicAsset>();
        world.Fxs ??= new List<FxAsset>();
        world.Scripts ??= new List<ScriptDefinition>();
        world.Abilities ??= new List<CombatAbility>();

        // Normalizar Properties de scripts y corregir categorías de nodos
        foreach (var script in world.Scripts)
        {
            foreach (var node in script.Nodes)
            {
                // Recrear el diccionario con comparer case-insensitive
                var normalizedProps = new Dictionary<string, object?>(
                    node.Properties, StringComparer.OrdinalIgnoreCase);
                node.Properties = normalizedProps;

                // Corregir la categoría del nodo según el registro de tipos
                // (útil cuando se carga JSON generado externamente que no incluye Category)
                var typeDef = NodeTypeRegistry.GetNodeType(node.NodeType);
                if (typeDef != null)
                {
                    node.Category = typeDef.Category;
                }
            }
        }

        return world;
    }

    /// <summary>
    /// Creates a new game state from a world model.
    /// Clones all mutable data to allow independent game sessions.
    /// </summary>
    /// <param name="world">The world model to create state from.</param>
    /// <returns>A new game state initialized with world data.</returns>
    public static GameState CreateInitialState(WorldModel world)
    {
        var playerDef = world.Player ?? new PlayerDefinition();
        var state = new GameState
        {
            WorldId = world.Game.Id,
            WorldMusicId = world.Game.WorldMusicId,
            CurrentRoomId = world.Game.StartRoomId,
            Rooms = CloneList(world.Rooms),
            Objects = CloneList(world.Objects),
            Npcs = CloneList(world.Npcs),
            Abilities = CloneList(world.Abilities),
            UseRules = CloneList(world.UseRules),
            TradeRules = CloneList(world.TradeRules),
            Events = CloneList(world.Events),
            Doors = CloneList(world.Doors),
            Player = new PlayerStats
            {
                Name = playerDef.Name,
                Strength = playerDef.Strength,
                Constitution = playerDef.Constitution,
                Intelligence = playerDef.Intelligence,
                Dexterity = playerDef.Dexterity,
                Charisma = playerDef.Charisma,
                Gold = playerDef.InitialGold,
                AbilityIds = new List<string>(playerDef.AbilityIds ?? new List<string>())
            }
        };


        // Inicializar hora y clima de la partida según la configuración del juego.
        var startHour = world.Game.StartHour;
        if (startHour < 0) startHour = 0;
        if (startHour > 23) startHour = 23;
        var today = DateTime.Today;
        state.GameTime = new DateTime(today.Year, today.Month, today.Day, startHour, 0, 0);
        state.Weather = world.Game.StartWeather;


        state.Quests = new Dictionary<string, QuestState>();
        foreach (var q in world.Quests)
        {
            state.Quests[q.Id] = new QuestState
            {
                QuestId = q.Id,
                Status = QuestStatus.NotStarted,
                CurrentObjectiveIndex = 0
            };
        }

        // Copiamos la clave de cifrado del mundo al estado para que el motor
        // pueda usarla al guardar la partida del jugador.
        state.WorldEncryptionKey = world.Game.EncryptionKey;

        RebuildRoomIndexes(state);
        return state;
    }

    /// <summary>
    /// Rebuilds the room indexes for objects and NPCs.
    /// Updates each room's ObjectIds and NpcIds lists based on
    /// the RoomId property of each object and NPC.
    /// </summary>
    /// <param name="state">The game state to rebuild indexes for.</param>
    public static void RebuildRoomIndexes(GameState state)
    {
        var roomsById = state.Rooms.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);

        foreach (var room in state.Rooms)
        {
            room.ObjectIds.Clear();
            room.NpcIds.Clear();
        }

        foreach (var obj in state.Objects)
        {
            if (!string.IsNullOrWhiteSpace(obj.RoomId) &&
                roomsById.TryGetValue(obj.RoomId, out var room))
            {
                room.ObjectIds.Add(obj.Id);
            }
        }

        foreach (var npc in state.Npcs)
        {
            if (!string.IsNullOrWhiteSpace(npc.RoomId) &&
                roomsById.TryGetValue(npc.RoomId, out var room))
            {
                room.NpcIds.Add(npc.Id);
            }
        }
    }


    /// <summary>
    /// Saves a world model to a .xaw file.
    /// Compresses the JSON as Base64-encoded ZIP.
    /// </summary>
    /// <param name="world">The world model to save.</param>
    /// <param name="path">The file path to save to.</param>
    /// <exception cref="ArgumentNullException">Thrown if world is null.</exception>
    public static void SaveWorldModel(WorldModel world, string path)
    {
        if (world is null)
            throw new ArgumentNullException(nameof(world));

        // Normalizamos campos dependientes de Ids para que, si el usuario
        // ha dejado en blanco los textbox de Id en el editor, se limpien
        // también los contenidos en Base64 antes de serializar el mundo.
        if (world.Game is not null)
        {
            if (string.IsNullOrWhiteSpace(world.Game.WorldMusicId))
                world.Game.WorldMusicId = null;
        }

        if (world.Rooms != null)
        {
            foreach (var room in world.Rooms)
            {
                if (room is null) continue;

                if (string.IsNullOrWhiteSpace(room.MusicId))
                    room.MusicId = null;

                if (string.IsNullOrWhiteSpace(room.ImageId))
                {
                    room.ImageId = null;
                    room.ImageBase64 = null;
                }
            }
        }

        var json = JsonSerializer.Serialize(world, Options);

        // Comprimir el JSON en un ZIP (entrada world.json) y codificarlo en Base64
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        using var ms = new MemoryStream();
        using (var zip = new ZipArchive(ms, ZipArchiveMode.Create, true))
        {
            var entry = zip.CreateEntry("world.json", CompressionLevel.SmallestSize);
            using var entryStream = entry.Open();
            entryStream.Write(jsonBytes, 0, jsonBytes.Length);
        }

        var compressedBytes = ms.ToArray();
        var base64 = Convert.ToBase64String(compressedBytes);

        // Guardar directamente sin cifrado
        File.WriteAllText(path, base64, Encoding.UTF8);
    }



    /// <summary>
    /// Intenta interpretar el texto desencriptado como un ZIP codificado en Base64
    /// que contiene un único JSON (world.json). Devuelve true si se ha podido
    /// obtener el JSON correctamente.
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

            // Intentamos obtener la entrada "world.json". Si no existe,
            // usamos la primera entrada disponible.
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
            // Si falla cualquier cosa (no es Base64, no es ZIP, etc.), asumimos
            // que no está comprimido y devolvemos false.
            json = string.Empty;
            return false;
        }
    }

    private static List<T> CloneList<T>(List<T> source)
    {
        // Serialización simple para clonar; suficiente para este proyecto.
        var json = JsonSerializer.Serialize(source, Options);
        return JsonSerializer.Deserialize<List<T>>(json, Options) ?? new List<T>();
    }
}
