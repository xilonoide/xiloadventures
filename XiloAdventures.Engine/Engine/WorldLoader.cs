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
                Money = playerDef.InitialMoney,
                AbilityIds = new List<string>(playerDef.AbilityIds ?? new List<string>()),
                BodyWeight = playerDef.Weight,
                EquippedRightHandId = playerDef.InitialRightHandId,
                EquippedLeftHandId = playerDef.InitialLeftHandId,
                EquippedTorsoId = playerDef.InitialTorsoId,
                EquippedHeadId = playerDef.InitialHeadId
            }
        };

        // Añadir objetos del inventario inicial según cantidades
        foreach (var item in playerDef.InitialInventory ?? new List<InventoryItem>())
        {
            if (string.IsNullOrEmpty(item.ObjectId) || item.Quantity <= 0) continue;

            for (int i = 0; i < item.Quantity; i++)
            {
                state.InventoryObjectIds.Add(item.ObjectId);
            }
        }

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
            // Asegurar que el ID del juego sea un GUID válido
            if (string.IsNullOrWhiteSpace(world.Game.Id) || !Guid.TryParse(world.Game.Id, out _))
            {
                world.Game.Id = Guid.NewGuid().ToString();
            }

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
                    room.ImageId = null;

                // Only clear ImageBase64 if both ImageId and ImageBase64 are empty
                // (AI-generated images have ImageBase64 but no ImageId)
                if (string.IsNullOrWhiteSpace(room.ImageId) && string.IsNullOrWhiteSpace(room.ImageBase64))
                    room.ImageBase64 = null;
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
    /// Creates a .xaw file from a folder containing zone JSON files and a manifest.
    /// The folder structure should be:
    /// - manifest.json (zone manifest with connections)
    /// - zones/ (folder containing zone JSON files)
    ///   - zone1.json
    ///   - zone2.json
    ///   - ...
    /// </summary>
    /// <param name="folderPath">Path to the folder containing zones and manifest.</param>
    /// <param name="outputPath">Path where the .xaw file will be saved.</param>
    /// <exception cref="InvalidDataException">Thrown if the folder structure is invalid.</exception>
    public static void CreateXawFromZoneFolder(string folderPath, string outputPath)
    {
        var manifestPath = Path.Combine(folderPath, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new InvalidDataException("No se encontró manifest.json en la carpeta.");

        var manifestJson = File.ReadAllText(manifestPath, Encoding.UTF8);
        var manifest = JsonSerializer.Deserialize<ZoneManifest>(manifestJson, Options);
        if (manifest == null)
            throw new InvalidDataException("El manifest.json no es válido.");

        var zonesFolder = Path.Combine(folderPath, "zones");
        if (!Directory.Exists(zonesFolder))
            throw new InvalidDataException("No se encontró la carpeta 'zones'.");

        // Load and merge all zones
        var world = LoadAndMergeZones(manifest, zonesFolder);

        // Save as standard .xaw (compressed Base64)
        SaveWorldModel(world, outputPath);
    }

    /// <summary>
    /// Loads zone files and merges them into a single WorldModel.
    /// </summary>
    private static WorldModel LoadAndMergeZones(ZoneManifest manifest, string zonesFolder)
    {
        var world = new WorldModel();
        world.Game = new GameInfo
        {
            Id = manifest.Name.ToLowerInvariant().Replace(" ", "_"),
            Title = manifest.Name
        };

        // Parse startRoom from "zone:room" format
        if (!string.IsNullOrEmpty(manifest.StartRoom))
        {
            var parts = manifest.StartRoom.Split(':');
            if (parts.Length == 2)
            {
                world.Game.StartRoomId = parts[1];
            }
            else
            {
                world.Game.StartRoomId = manifest.StartRoom;
            }
        }

        // Load each zone
        foreach (var zoneName in manifest.Zones)
        {
            var zonePath = Path.Combine(zonesFolder, $"{zoneName}.json");
            if (!File.Exists(zonePath))
            {
                throw new InvalidDataException($"No se encontró el archivo de zona: {zoneName}.json");
            }

            var zoneJson = File.ReadAllText(zonePath, Encoding.UTF8);
            var zoneWorld = JsonSerializer.Deserialize<WorldModel>(zoneJson, Options);
            if (zoneWorld == null)
            {
                throw new InvalidDataException($"El archivo de zona {zoneName}.json no es válido.");
            }

            // Merge zone data into main world, setting Zone attribute on rooms
            MergeZoneIntoWorld(world, zoneWorld, zoneName);
        }

        // Apply connections between zones
        ApplyZoneConnections(world, manifest.Connections);

        // Calculate room positions for merged world
        CalculateZonePositions(world, manifest.Zones);

        return NormalizeWorld(world);
    }

    /// <summary>
    /// Merges a zone's data into the main world, setting the Zone attribute on all rooms.
    /// </summary>
    private static void MergeZoneIntoWorld(WorldModel world, WorldModel zone, string zoneName)
    {
        // Merge Game info from first zone if not set
        if (string.IsNullOrEmpty(world.Game.Title) && zone.Game != null)
        {
            world.Game.Theme = zone.Game.Theme;
            world.Game.StartHour = zone.Game.StartHour;
            world.Game.StartWeather = zone.Game.StartWeather;
            world.Game.MinutesPerGameHour = zone.Game.MinutesPerGameHour;
        }

        // Merge Player from first zone that has it
        if (world.Player == null || string.IsNullOrEmpty(world.Player.Name))
        {
            if (zone.Player != null && !string.IsNullOrEmpty(zone.Player.Name))
            {
                world.Player = zone.Player;
            }
        }

        // Merge rooms with zone attribute
        foreach (var room in zone.Rooms ?? new List<Room>())
        {
            room.Zone = zoneName;
            world.Rooms.Add(room);
        }

        // Merge objects
        foreach (var obj in zone.Objects ?? new List<GameObject>())
        {
            world.Objects.Add(obj);
        }

        // Merge NPCs
        foreach (var npc in zone.Npcs ?? new List<Npc>())
        {
            world.Npcs.Add(npc);
        }

        // Merge quests
        foreach (var quest in zone.Quests ?? new List<QuestDefinition>())
        {
            world.Quests.Add(quest);
        }

        // Merge doors
        foreach (var door in zone.Doors ?? new List<Door>())
        {
            world.Doors.Add(door);
        }

        // Merge scripts
        foreach (var script in zone.Scripts ?? new List<ScriptDefinition>())
        {
            world.Scripts.Add(script);
        }

        // Merge conversations
        foreach (var conv in zone.Conversations ?? new List<ConversationDefinition>())
        {
            world.Conversations.Add(conv);
        }

        // Merge use rules
        foreach (var rule in zone.UseRules ?? new List<UseRule>())
        {
            world.UseRules.Add(rule);
        }

        // Merge trade rules
        foreach (var rule in zone.TradeRules ?? new List<TradeRule>())
        {
            world.TradeRules.Add(rule);
        }

        // Merge events
        foreach (var evt in zone.Events ?? new List<EventRule>())
        {
            world.Events.Add(evt);
        }

        // Merge music assets
        foreach (var music in zone.Musics ?? new List<MusicAsset>())
        {
            if (!world.Musics.Any(m => m.Id == music.Id))
            {
                world.Musics.Add(music);
            }
        }

        // Merge fx assets
        foreach (var fx in zone.Fxs ?? new List<FxAsset>())
        {
            if (!world.Fxs.Any(f => f.Id == fx.Id))
            {
                world.Fxs.Add(fx);
            }
        }

        // Merge abilities
        foreach (var ability in zone.Abilities ?? new List<CombatAbility>())
        {
            if (!world.Abilities.Any(a => a.Id == ability.Id))
            {
                world.Abilities.Add(ability);
            }
        }

        // Merge room positions with offset based on zone index
        foreach (var kvp in zone.RoomPositions ?? new Dictionary<string, MapPosition>())
        {
            if (!world.RoomPositions.ContainsKey(kvp.Key))
            {
                world.RoomPositions[kvp.Key] = kvp.Value;
            }
        }
    }

    /// <summary>
    /// Applies cross-zone connections by creating exits between rooms.
    /// </summary>
    private static void ApplyZoneConnections(WorldModel world, List<ZoneConnection> connections)
    {
        var roomsById = world.Rooms.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);

        foreach (var conn in connections ?? new List<ZoneConnection>())
        {
            // Parse "zone:room" format
            var fromRoom = ParseZoneRoom(conn.From);
            var toRoom = ParseZoneRoom(conn.To);

            if (roomsById.TryGetValue(fromRoom, out var sourceRoom))
            {
                // Check if exit already exists
                var existingExit = sourceRoom.Exits.FirstOrDefault(e =>
                    e.Direction.Equals(conn.Direction, StringComparison.OrdinalIgnoreCase));

                if (existingExit != null)
                {
                    // Update existing exit
                    existingExit.TargetRoomId = toRoom;
                }
                else
                {
                    // Add new exit
                    sourceRoom.Exits.Add(new Exit
                    {
                        Direction = conn.Direction,
                        TargetRoomId = toRoom
                    });
                }
            }

            // Also add reverse connection if applicable
            var reverseDirection = GetReverseDirection(conn.Direction);
            if (!string.IsNullOrEmpty(reverseDirection) && roomsById.TryGetValue(toRoom, out var targetRoom))
            {
                var existingReverseExit = targetRoom.Exits.FirstOrDefault(e =>
                    e.Direction.Equals(reverseDirection, StringComparison.OrdinalIgnoreCase));

                if (existingReverseExit == null)
                {
                    targetRoom.Exits.Add(new Exit
                    {
                        Direction = reverseDirection,
                        TargetRoomId = fromRoom
                    });
                }
            }
        }
    }

    /// <summary>
    /// Parses "zone:room" format and returns just the room ID.
    /// </summary>
    private static string ParseZoneRoom(string zoneRoom)
    {
        if (string.IsNullOrEmpty(zoneRoom))
            return string.Empty;

        var parts = zoneRoom.Split(':');
        return parts.Length == 2 ? parts[1] : zoneRoom;
    }

    /// <summary>
    /// Gets the reverse direction for bidirectional connections.
    /// </summary>
    private static string GetReverseDirection(string direction)
    {
        return direction.ToLowerInvariant() switch
        {
            "norte" => "sur",
            "sur" => "norte",
            "este" => "oeste",
            "oeste" => "este",
            "arriba" => "abajo",
            "abajo" => "arriba",
            "noreste" => "suroeste",
            "noroeste" => "sureste",
            "sureste" => "noroeste",
            "suroeste" => "noreste",
            _ => string.Empty
        };
    }

    /// <summary>
    /// Calculates room positions for zones, arranging them in a grid layout.
    /// </summary>
    private static void CalculateZonePositions(WorldModel world, List<string> zoneNames)
    {
        const double zoneSpacing = 5; // Grid units between zones
        double currentOffsetX = 0;

        foreach (var zoneName in zoneNames)
        {
            var zoneRooms = world.Rooms.Where(r => r.Zone == zoneName).ToList();
            if (!zoneRooms.Any()) continue;

            // Get positions for rooms in this zone
            var roomsWithPositions = zoneRooms
                .Where(r => world.RoomPositions.ContainsKey(r.Id))
                .Select(r => (room: r, pos: world.RoomPositions[r.Id]))
                .ToList();

            // Calculate current zone's bounding box
            double minX = 0, maxX = 0, minY = 0, maxY = 0;
            if (roomsWithPositions.Any())
            {
                minX = roomsWithPositions.Min(rp => rp.pos.X);
                maxX = roomsWithPositions.Max(rp => rp.pos.X);
                minY = roomsWithPositions.Min(rp => rp.pos.Y);
                maxY = roomsWithPositions.Max(rp => rp.pos.Y);
            }

            var zoneWidth = maxX - minX + 1;

            // Calculate offset to normalize zone to origin, then shift to current position
            var normalizeOffsetX = -minX;
            var normalizeOffsetY = -minY;

            // Offset existing positions
            foreach (var (room, pos) in roomsWithPositions)
            {
                world.RoomPositions[room.Id] = new MapPosition
                {
                    X = pos.X + normalizeOffsetX + currentOffsetX,
                    Y = pos.Y + normalizeOffsetY
                };
            }

            // Create positions for rooms without positions
            var roomIndex = 0;
            foreach (var room in zoneRooms.Where(r => !world.RoomPositions.ContainsKey(r.Id)))
            {
                var cols = 5;
                var x = currentOffsetX + (roomIndex % cols) * 2;
                var y = (roomIndex / cols) * 2;
                world.RoomPositions[room.Id] = new MapPosition { X = x, Y = y };
                roomIndex++;
            }

            // Move offset for next zone
            currentOffsetX += zoneWidth + zoneSpacing;
        }
    }

    /// <summary>
    /// Gets the list of unique zones in a world.
    /// </summary>
    public static List<string> GetZones(WorldModel world)
    {
        return world.Rooms
            .Where(r => !string.IsNullOrEmpty(r.Zone))
            .Select(r => r.Zone!)
            .Distinct()
            .OrderBy(z => z)
            .ToList();
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

    /// <summary>
    /// Merges multiple .xaw zone files into a single WorldModel.
    /// Automatically detects which file is the main one (contains Game/Player).
    /// Resolves @zone:room_id references to just room_id.
    /// </summary>
    /// <param name="filePaths">List of .xaw file paths to merge.</param>
    /// <returns>The merged WorldModel.</returns>
    public static WorldModel MergeZoneFiles(IEnumerable<string> filePaths)
    {
        var files = filePaths.ToList();
        if (files.Count == 0)
            throw new InvalidDataException("No se proporcionaron archivos para fusionar.");

        // Load all zone files
        var zoneWorlds = new List<(string path, WorldModel world)>();
        foreach (var path in files)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            if (TryDecodeZippedJson(json, out var decodedJson))
                json = decodedJson;

            var world = JsonSerializer.Deserialize<WorldModel>(json, Options);
            if (world != null)
            {
                zoneWorlds.Add((path, world));
            }
        }

        if (zoneWorlds.Count == 0)
            throw new InvalidDataException("No se pudo cargar ningún archivo de zona.");

        // Find zones with Game and Player defined (the initial zone)
        var hasInitialZone = zoneWorlds.Any(z => z.world.Game != null && !string.IsNullOrEmpty(z.world.Game.Title) && z.world.Player != null);

        // Find zones with RoomPositions defined (the final zone)
        var hasFinalZone = zoneWorlds.Any(z => z.world.RoomPositions != null && z.world.RoomPositions.Count > 0);

        // Validate required zones
        if (!hasInitialZone && !hasFinalZone)
            throw new InvalidDataException("Faltan el archivo con la zona inicial y el archivo con la zona final.");
        if (!hasInitialZone)
            throw new InvalidDataException("Falta el archivo con la zona inicial.");
        if (!hasFinalZone)
            throw new InvalidDataException("Falta el archivo con la zona final.");

        // Use the first zone with Game and Player as the main zone
        var mainZone = zoneWorlds.First(z => z.world.Game != null && !string.IsNullOrEmpty(z.world.Game.Title) && z.world.Player != null);

        // Create merged world starting with main zone
        var mergedWorld = new WorldModel
        {
            Game = mainZone.world.Game ?? new GameInfo(),
            Player = mainZone.world.Player,
            Rooms = new List<Room>(),
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>(),
            Conversations = new List<ConversationDefinition>(),
            UseRules = new List<UseRule>(),
            TradeRules = new List<TradeRule>(),
            Events = new List<EventRule>(),
            Musics = new List<MusicAsset>(),
            Fxs = new List<FxAsset>(),
            Abilities = new List<CombatAbility>(),
            RoomPositions = new Dictionary<string, MapPosition>()
        };

        // Collect all zone names for position calculation
        var zoneNames = new List<string>();

        // Merge all zones (main zone first, then others)
        var orderedZones = new List<(string path, WorldModel world)> { mainZone };
        orderedZones.AddRange(zoneWorlds.Where(z => z.path != mainZone.path));

        foreach (var (path, zoneWorld) in orderedZones)
        {
            // Get zone name from first room or file name
            var zoneName = zoneWorld.Rooms?.FirstOrDefault()?.Zone
                ?? Path.GetFileNameWithoutExtension(path);

            if (!string.IsNullOrEmpty(zoneName) && !zoneNames.Contains(zoneName))
                zoneNames.Add(zoneName);

            MergeZoneIntoWorld(mergedWorld, zoneWorld, zoneName);
        }

        // Resolve @zone:room_id references in all exits
        ResolveZoneReferences(mergedWorld);

        // Reorder rooms based on connections and separate zones
        ReorderRoomPositions(mergedWorld);

        return NormalizeWorld(mergedWorld);
    }

    /// <summary>
    /// Resolves @zone:room_id references to just room_id in all room exits.
    /// </summary>
    private static void ResolveZoneReferences(WorldModel world)
    {
        foreach (var room in world.Rooms)
        {
            foreach (var exit in room.Exits)
            {
                if (!string.IsNullOrEmpty(exit.TargetRoomId) && exit.TargetRoomId.StartsWith("@"))
                {
                    // Format: @zone_name:room_id -> room_id
                    var parts = exit.TargetRoomId.Substring(1).Split(':');
                    if (parts.Length == 2)
                    {
                        exit.TargetRoomId = parts[1];
                    }
                }
            }
        }
    }

    /// <summary>
    /// Merges multiple .xaw zone files and saves the result to a single file.
    /// </summary>
    /// <param name="filePaths">List of .xaw file paths to merge.</param>
    /// <param name="outputPath">Path where the merged .xaw will be saved.</param>
    public static void MergeAndSaveZoneFiles(IEnumerable<string> filePaths, string outputPath)
    {
        var mergedWorld = MergeZoneFiles(filePaths);
        SaveWorldModel(mergedWorld, outputPath);
    }

    /// <summary>
    /// Reorders room positions in the world based on their connections.
    /// Groups by zone, layouts each zone based on exits, and separates zones.
    /// </summary>
    /// <param name="world">The world model to reorder.</param>
    /// <param name="gridSize">Grid size for snapping (default 2).</param>
    public static void ReorderRoomPositions(WorldModel world, int gridSize = 220)
    {
        if (world.Rooms.Count == 0) return;

        // Ensure RoomPositions is initialized
        if (world.RoomPositions == null)
            world.RoomPositions = new Dictionary<string, MapPosition>();

        // Clear existing positions
        world.RoomPositions.Clear();

        // Get zones - include rooms without zone as a separate group
        var zones = GetZones(world);

        // Check if there are rooms without zone
        var roomsWithoutZone = world.Rooms.Where(r => string.IsNullOrEmpty(r.Zone)).ToList();
        bool hasRoomsWithoutZone = roomsWithoutZone.Count > 0;

        // If no zones at all, or only rooms without zones
        if (zones.Count == 0 && hasRoomsWithoutZone)
        {
            zones.Add(""); // Empty string represents rooms without zone
        }
        else if (hasRoomsWithoutZone)
        {
            zones.Add(""); // Add rooms without zone as a separate "zone"
        }

        double currentZoneOffsetX = 0;
        const double zoneSpacing = 10;

        foreach (var zoneName in zones)
        {
            List<Room> zoneRooms;
            if (string.IsNullOrEmpty(zoneName))
            {
                // Get rooms without zone (null or empty)
                zoneRooms = world.Rooms.Where(r => string.IsNullOrEmpty(r.Zone)).ToList();
            }
            else
            {
                zoneRooms = world.Rooms.Where(r => r.Zone == zoneName).ToList();
            }

            if (zoneRooms.Count == 0) continue;

            // Layout this zone based on room connections
            var zonePositions = LayoutZoneByConnections(zoneRooms, world.Rooms, gridSize);

            if (zonePositions.Count == 0) continue;

            // Calculate zone bounding box
            double minX = zonePositions.Values.Min(p => p.X);
            double maxX = zonePositions.Values.Max(p => p.X);

            // Normalize to start at 0 and offset by current zone position
            foreach (var kvp in zonePositions)
            {
                world.RoomPositions[kvp.Key] = new MapPosition
                {
                    X = kvp.Value.X - minX + currentZoneOffsetX,
                    Y = kvp.Value.Y
                };
            }

            // Move offset for next zone
            var zoneWidth = maxX - minX + gridSize;
            currentZoneOffsetX += zoneWidth + zoneSpacing;
        }
    }

    /// <summary>
    /// Layouts rooms in a zone based on their exit connections.
    /// Uses a BFS-based tree layout starting from the first room or start room.
    /// Falls back to simple grid layout if BFS doesn't connect most rooms.
    /// </summary>
    private static Dictionary<string, MapPosition> LayoutZoneByConnections(
        List<Room> zoneRooms,
        List<Room> allRooms,
        int gridSize)
    {
        var positions = new Dictionary<string, MapPosition>(StringComparer.OrdinalIgnoreCase);
        if (zoneRooms.Count == 0) return positions;

        // Filter out rooms with null/empty IDs
        zoneRooms = zoneRooms.Where(r => !string.IsNullOrEmpty(r.Id)).ToList();
        if (zoneRooms.Count == 0) return positions;

        // Try BFS-based layout first
        var bfsPositions = TryBfsLayout(zoneRooms, allRooms, gridSize);

        // If BFS positioned less than 30% of rooms, use simple grid layout instead
        if (bfsPositions.Count < zoneRooms.Count * 0.3)
        {
            return SimpleGridLayout(zoneRooms, gridSize);
        }

        return bfsPositions;
    }

    /// <summary>
    /// Simple grid layout - places all rooms in a grid pattern.
    /// </summary>
    private static Dictionary<string, MapPosition> SimpleGridLayout(List<Room> rooms, int gridSize)
    {
        var positions = new Dictionary<string, MapPosition>(StringComparer.OrdinalIgnoreCase);
        if (rooms.Count == 0) return positions;

        // Use different spacing for X and Y based on room dimensions (160x90)
        int hSpace = gridSize;           // Horizontal spacing
        int vSpace = gridSize * 2 / 3;   // Vertical spacing (rooms are shorter)

        var gridColumns = Math.Max(5, (int)Math.Ceiling(Math.Sqrt(rooms.Count)));

        for (int i = 0; i < rooms.Count; i++)
        {
            var col = i % gridColumns;
            var row = i / gridColumns;
            positions[rooms[i].Id] = new MapPosition
            {
                X = col * hSpace,
                Y = row * vSpace
            };
        }

        return positions;
    }

    /// <summary>
    /// Attempts BFS-based layout using room connections.
    /// </summary>
    private static Dictionary<string, MapPosition> TryBfsLayout(
        List<Room> zoneRooms,
        List<Room> allRooms,
        int gridSize)
    {
        var positions = new Dictionary<string, MapPosition>(StringComparer.OrdinalIgnoreCase);

        var roomById = zoneRooms.ToDictionary(r => r.Id, r => r, StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Build bidirectional adjacency map (room connections within this zone)
        var adjacency = new Dictionary<string, List<(string targetId, string direction)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var room in zoneRooms)
        {
            if (!adjacency.ContainsKey(room.Id))
                adjacency[room.Id] = new List<(string, string)>();

            if (room.Exits == null) continue;

            foreach (var exit in room.Exits)
            {
                if (string.IsNullOrEmpty(exit.TargetRoomId)) continue;
                if (!roomById.ContainsKey(exit.TargetRoomId)) continue;

                adjacency[room.Id].Add((exit.TargetRoomId, exit.Direction ?? "este"));

                // Add reverse connection for bidirectional traversal
                if (!adjacency.ContainsKey(exit.TargetRoomId))
                    adjacency[exit.TargetRoomId] = new List<(string, string)>();

                var reverseDir = GetReverseDirection(exit.Direction ?? "este");
                if (!adjacency[exit.TargetRoomId].Any(a => a.targetId.Equals(room.Id, StringComparison.OrdinalIgnoreCase)))
                    adjacency[exit.TargetRoomId].Add((room.Id, reverseDir));
            }
        }

        // Direction offsets - use different spacing for horizontal (220) and vertical (150)
        // Room dimensions are approximately 160x90, so we add margin for connections
        int hSpace = gridSize;       // Horizontal spacing (220 by default)
        int vSpace = gridSize * 2 / 3; // Vertical spacing (~150 for default gridSize of 220)

        var directionOffsets = new Dictionary<string, (int dx, int dy)>(StringComparer.OrdinalIgnoreCase)
        {
            { "norte", (0, -vSpace) },
            { "sur", (0, vSpace) },
            { "este", (hSpace, 0) },
            { "oeste", (-hSpace, 0) },
            { "noreste", (hSpace, -vSpace) },
            { "noroeste", (-hSpace, -vSpace) },
            { "sureste", (hSpace, vSpace) },
            { "suroeste", (-hSpace, vSpace) },
            { "arriba", (hSpace / 2, -vSpace) },
            { "abajo", (-hSpace / 2, vSpace) },
            { "entrar", (hSpace, 0) },
            { "salir", (-hSpace, 0) }
        };

        // Start with first room at origin
        var startRoom = zoneRooms[0];
        var queue = new Queue<(Room room, double x, double y)>();
        queue.Enqueue((startRoom, 0, 0));
        visited.Add(startRoom.Id);
        positions[startRoom.Id] = new MapPosition { X = 0, Y = 0 };

        // BFS to place connected rooms using bidirectional adjacency
        while (queue.Count > 0)
        {
            var (currentRoom, currentX, currentY) = queue.Dequeue();

            if (!adjacency.TryGetValue(currentRoom.Id, out var neighbors)) continue;

            foreach (var (targetId, direction) in neighbors)
            {
                if (visited.Contains(targetId)) continue;
                if (!roomById.TryGetValue(targetId, out var targetRoom)) continue;

                visited.Add(targetId);

                // Calculate position based on direction
                var dir = direction?.ToLowerInvariant() ?? "este";
                if (!directionOffsets.TryGetValue(dir, out var offset))
                {
                    offset = (gridSize, 0); // Default to east
                }

                var newX = currentX + offset.dx;
                var newY = currentY + offset.dy;

                // Check for collision and find free spot
                var attempts = 0;
                while (positions.Values.Any(p => Math.Abs(p.X - newX) < hSpace * 0.9 && Math.Abs(p.Y - newY) < vSpace * 0.9) && attempts < 100)
                {
                    // Try different directions to find free spot
                    if (attempts % 4 == 0) newX += hSpace;
                    else if (attempts % 4 == 1) newY += vSpace;
                    else if (attempts % 4 == 2) newX -= hSpace;
                    else newY -= vSpace;
                    attempts++;
                }

                positions[targetRoom.Id] = new MapPosition { X = newX, Y = newY };
                queue.Enqueue((targetRoom, newX, newY));
            }
        }

        // Place any unvisited rooms (disconnected) in a grid pattern below
        var unvisitedRooms = zoneRooms.Where(r => !visited.Contains(r.Id)).ToList();
        if (unvisitedRooms.Count > 0)
        {
            var maxY = positions.Values.Any() ? positions.Values.Max(p => p.Y) : -vSpace * 2;
            var gridColumns = Math.Max(5, (int)Math.Ceiling(Math.Sqrt(unvisitedRooms.Count)));
            var startY = maxY + vSpace * 2;

            for (int i = 0; i < unvisitedRooms.Count; i++)
            {
                var col = i % gridColumns;
                var row = i / gridColumns;
                var x = col * hSpace;
                var y = startY + row * vSpace;

                positions[unvisitedRooms[i].Id] = new MapPosition { X = x, Y = y };
            }
        }

        // Normalize Y to start at 0
        if (positions.Any())
        {
            var minY = positions.Values.Min(p => p.Y);
            foreach (var key in positions.Keys.ToList())
            {
                var pos = positions[key];
                positions[key] = new MapPosition { X = pos.X, Y = pos.Y - minY };
            }
        }

        return positions;
    }
}
