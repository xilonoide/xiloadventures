using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace XiloAdventures.Engine.Models;

public class WorldModel
{
    public GameInfo Game { get; set; } = new();
    public PlayerDefinition Player { get; set; } = new();
    public List<Room> Rooms { get; set; } = new();
    public List<GameObject> Objects { get; set; } = new();
    public List<Npc> Npcs { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
    public List<UseRule> UseRules { get; set; } = new();
    public List<TradeRule> TradeRules { get; set; } = new();
    public List<EventRule> Events { get; set; } = new();

    public List<Door> Doors { get; set; } = new();

    /// <summary>
    /// Scripts visuales del mundo (comportamientos definidos mediante nodos).
    /// </summary>
    public List<ScriptDefinition> Scripts { get; set; } = new();

    /// <summary>
    /// Conversaciones editables visualmente (diálogos con NPCs).
    /// </summary>
    public List<ConversationDefinition> Conversations { get; set; } = new();

    /// <summary>
    /// Biblioteca de música del mundo (archivos compartidos entre salas).
    /// </summary>
    public List<MusicAsset> Musics { get; set; } = new();

    /// <summary>
    /// Biblioteca de efectos de sonido del mundo.
    /// </summary>
    public List<FxAsset> Fxs { get; set; } = new();

    /// <summary>
    /// Posiciones del mapa para cada sala (coordenadas lógicas X/Y) usadas por el editor.
    /// </summary>
    public Dictionary<string, MapPosition> RoomPositions { get; set; } = new();

    /// <summary>
    /// Estado del grid en el editor (visible/oculto).
    /// </summary>
    public bool ShowGrid { get; set; } = false;

    /// <summary>
    /// Estado del snap-to-grid en el editor (activado/desactivado).
    /// </summary>
    public bool SnapToGrid { get; set; } = true;

    /// <summary>
    /// Indica si usar IA para determinar géneros gramaticales al guardar.
    /// </summary>
    public bool UseLlmForGenders { get; set; } = false;
}

public enum WeatherType
{
    Despejado,
    Lluvioso,
    Nublado,
    Tormenta
}

public enum ObjectType
{
    Ninguno,        // Sin especificar
    Arma,           // Arma
    Armadura,       // Armadura
    Comida,         // Comida
    Bebida,         // Bebida
    Ropa,           // Ropa
    Llave,          // Llave
    Texto           // Documento legible (libro, carta, pergamino, etc.)
}

/// <summary>
/// Género gramatical para artículos en español (el/la/los/las).
/// </summary>
public enum GrammaticalGender
{
    Masculine,  // el, los, un, unos
    Feminine    // la, las, una, unas
}

/// <summary>Modo de movimiento para NPCs.</summary>
public enum MovementMode
{
    /// <summary>Movimiento basado en turnos del jugador.</summary>
    Turns,
    /// <summary>Movimiento basado en tiempo real (segundos).</summary>
    Time
}

public class GameInfo
{
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Tema o ambientación del mundo (ej: "fantasía medieval", "ciencia ficción", "horror gótico").
    /// Se usa como contexto para la generación de contenido con IA.
    /// </summary>
    public string? Theme { get; set; }

    public string Title { get; set; } = "Aventura sin título";
    public string StartRoomId { get; set; } = string.Empty;
    public string? WorldMusicId { get; set; }

    /// <summary>
    /// Música por defecto del mundo en Base64 (se guarda dentro del JSON del mundo).
    /// Si es null o vacío, no sonará música global.
    /// </summary>
    [Browsable(false)]
    public string? WorldMusicBase64 { get; set; }

    /// <summary>
    /// Clave de cifrado para las partidas guardadas de los jugadores.
    /// Debe tener 8 caracteres.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>Hora inicial de la partida (0-23).</summary>
    public int StartHour { get; set; } = 9;

    /// <summary>Clima inicial del mundo.</summary>
    public WeatherType StartWeather { get; set; } = WeatherType.Despejado;

    /// <summary>Minutos reales que equivalen a 1 hora de juego (1-10).</summary>
    public int MinutesPerGameHour { get; set; } = 6;

    /// <summary>Diccionario de sinónimos por mundo para el parser (JSON).</summary>
    public string? ParserDictionaryJson { get; set; }

    /// <summary>Activar sonido en modo pruebas del editor.</summary>
    public bool TestModeSoundEnabled { get; set; } = false;

    /// <summary>Activar IA en modo pruebas del editor.</summary>
    public bool TestModeAiEnabled { get; set; } = false;
}

public class Room
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Sala sin nombre";
    public string Description { get; set; } = string.Empty;

    public string? ImageId { get; set; }

    /// <summary>
    /// Contenido de la imagen de la sala en Base64 (se guarda dentro del JSON del mundo).
    /// Si es null o vacío, no se mostrará imagen.
    /// </summary>
    [Browsable(false)]
    public string? ImageBase64 { get; set; }

    public string? MusicId { get; set; }

    /// <summary>
    /// Música específica de la sala en Base64 (se guarda dentro del JSON del mundo).
    /// Si es null o vacío, se usará la música global del mundo (si la hay).
    /// </summary>
    [Browsable(false)]
    public string? MusicBase64 { get; set; }
    public bool IsInterior { get; set; } = false;
    public bool IsIlluminated { get; set; } = true;

    public List<Exit> Exits { get; set; } = new();

    [Browsable(false)]
    public List<string> ObjectIds { get; set; } = new();

    [Browsable(false)]
    public List<string> NpcIds { get; set; } = new();

    public string? RequiredQuestId { get; set; }
    public QuestStatus? RequiredQuestStatus { get; set; }

    public List<string> Tags { get; set; } = new();
}

public class Exit
{
    public string Direction { get; set; } = string.Empty;
    public string TargetRoomId { get; set; } = string.Empty;

    public bool IsLocked { get; set; }

    /// <summary>
    /// ID del objeto (tipo Key) necesario para abrir esta salida.
    /// </summary>
    public string? KeyObjectId { get; set; }

    /// <summary>
    /// Si esta salida está asociada a una puerta física del mundo, su Id.
    /// Si es null, la salida funciona como hasta ahora (solo con IsLocked/KeyObjectId).
    /// </summary>
    public string? DoorId { get; set; }

    public string? RequiredQuestId { get; set; }
    public QuestStatus? RequiredQuestStatus { get; set; }

    public List<string> Tags { get; set; } = new();
}

public class GameObject
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Objeto sin nombre";
    public string Description { get; set; } = string.Empty;

    public ObjectType Type { get; set; } = ObjectType.Ninguno;

    /// <summary>
    /// Contenido de texto legible (solo para objetos de tipo Texto).
    /// Se muestra al usar el comando "leer" sobre el objeto.
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>Género gramatical del objeto (para artículos: el/la).</summary>
    public GrammaticalGender Gender { get; set; } = GrammaticalGender.Masculine;

    /// <summary>Si el nombre del objeto es plural (para artículos: los/las).</summary>
    public bool IsPlural { get; set; } = false;

    /// <summary>Indica si el género y plural fueron establecidos manualmente (no sobrescribir con IA).</summary>
    public bool GenderAndPluralSetManually { get; set; } = false;

    public bool CanTake { get; set; }

    // Propiedades de contenedor
    public bool IsContainer { get; set; }
    public List<string> ContainedObjectIds { get; set; } = new();
    public bool IsOpenable { get; set; } // Si el contenedor se puede abrir/cerrar
    public bool IsOpen { get; set; } = true; // Estado actual (por defecto abierto)
    public bool IsLocked { get; set; } // Si está bloqueado

    /// <summary>
    /// ID del objeto (tipo Key) necesario para abrir este contenedor.
    /// </summary>
    public string? KeyId { get; set; }

    public bool ContentsVisible { get; set; } // Si el contenido es visible sin abrir (ej: estante vs cofre)

    /// <summary>Capacidad máxima del contenedor en centímetros cúbicos (cm³). -1 = ilimitado.</summary>
    public double MaxCapacity { get; set; } = -1;

    /// <summary>Volumen del objeto en centímetros cúbicos (cm³).</summary>
    public double Volume { get; set; } = 0;

    /// <summary>Peso del objeto en gramos.</summary>
    public int Weight { get; set; } = 0;

    /// <summary>Precio del objeto en monedas.</summary>
    public int Price { get; set; } = 0;

    public List<string> Tags { get; set; } = new();

    /// <summary>Sala inicial donde se encuentra el objeto.</summary>
    public string? RoomId { get; set; }

    /// <summary>Controla si el jugador puede ver / interactuar con el objeto en la sala.</summary>
    public bool Visible { get; set; } = true;
}

public class Npc
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "NPC sin nombre";
    public string Description { get; set; } = string.Empty;

    /// <summary>Sala inicial donde aparece el NPC.</summary>
    public string? RoomId { get; set; }

    /// <summary>ID de la conversación principal del NPC (obsoleto - las conversaciones ahora se editan en el script del NPC).</summary>
    [Browsable(false)]
    public string? ConversationId { get; set; }

    /// <summary>Si es true, el NPC es un comerciante con tienda.</summary>
    public bool IsShopkeeper { get; set; }

    /// <summary>IDs de objetos que el NPC vende (si es comerciante).</summary>
    public List<string> ShopInventory { get; set; } = new();

    /// <summary>Multiplicador de precio al comprar del jugador (ej: 0.5 = compra al 50%).</summary>
    public double BuyPriceMultiplier { get; set; } = 0.5;

    /// <summary>Multiplicador de precio al vender al jugador (ej: 1.0 = precio base).</summary>
    public double SellPriceMultiplier { get; set; } = 1.0;

    /// <summary>Inventario del NPC.</summary>
    public List<string> InventoryObjectIds { get; set; } = new();

    /// <summary>Estadísticas de combate del NPC.</summary>
    public CombatStats Stats { get; set; } = new();

    /// <summary>Tags arbitrarios para lógica de eventos, etc.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Controla si el jugador puede ver / interactuar con el NPC en la sala.</summary>
    public bool Visible { get; set; } = true;

    // === PATRULLA ===

    /// <summary>Lista ordenada de IDs de salas que forman la ruta de patrulla (modo ping-pong).</summary>
    public List<string> PatrolRoute { get; set; } = new();

    /// <summary>Modo de movimiento de patrulla: Turns = por turnos, Time = por tiempo real.</summary>
    public MovementMode PatrolMovementMode { get; set; } = MovementMode.Turns;

    /// <summary>Cada cuántos turnos del jugador se mueve el NPC (1 = cada turno, 3 = cada 3 turnos). Solo aplica en modo Turns.</summary>
    public int PatrolSpeed { get; set; } = 1;

    /// <summary>Intervalo en segundos entre movimientos de patrulla (3=Camina, 6=Lento, 10=Muy lento). Solo aplica en modo Time.</summary>
    public float PatrolTimeInterval { get; set; } = 3.0f;

    /// <summary>Si el NPC está patrullando activamente.</summary>
    public bool IsPatrolling { get; set; } = false;

    /// <summary>Índice actual en la ruta de patrulla (estado runtime, no se serializa).</summary>
    [JsonIgnore] public int PatrolRouteIndex { get; set; } = 0;

    /// <summary>Dirección de movimiento en la ruta (-1 o 1 para ping-pong).</summary>
    [JsonIgnore] public int PatrolDirection { get; set; } = 1;

    /// <summary>Contador de turnos para determinar cuándo mover (estado runtime).</summary>
    [JsonIgnore] public int PatrolTurnCounter { get; set; } = 0;

    /// <summary>Tiempo del último movimiento de patrulla (estado runtime).</summary>
    [JsonIgnore] public DateTime PatrolLastMoveTime { get; set; } = DateTime.MinValue;

    // === SEGUIMIENTO ===

    /// <summary>Si el NPC está siguiendo al jugador.</summary>
    public bool IsFollowingPlayer { get; set; } = false;

    /// <summary>Modo de movimiento de seguimiento: Turns = por turnos, Time = por tiempo real.</summary>
    public MovementMode FollowMovementMode { get; set; } = MovementMode.Turns;

    /// <summary>Velocidad de seguimiento: 1 = cada turno, 2 = cada 2 turnos, 3 = cada 3 turnos. Solo aplica en modo Turns.</summary>
    public int FollowSpeed { get; set; } = 1;

    /// <summary>Intervalo en segundos entre movimientos de seguimiento (3=Camina, 6=Lento, 10=Muy lento). Solo aplica en modo Time.</summary>
    public float FollowTimeInterval { get; set; } = 3.0f;

    /// <summary>Contador de movimientos del jugador para calcular seguimiento (estado runtime).</summary>
    [JsonIgnore] public int FollowMoveCounter { get; set; } = 0;

    /// <summary>Tiempo del último movimiento de seguimiento (estado runtime).</summary>
    [JsonIgnore] public DateTime FollowLastMoveTime { get; set; } = DateTime.MinValue;
}

public class CombatStats
{
    public int Level { get; set; } = 1;
    public int Strength { get; set; } = 5;
    public int Dexterity { get; set; } = 5;
    public int Intelligence { get; set; } = 5;

    public int MaxHealth { get; set; } = 10;
    public int CurrentHealth { get; set; } = 10;

    public int Gold { get; set; }
}

public class QuestDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Misión sin nombre";
    public string Description { get; set; } = string.Empty;
    public List<string> Objectives { get; set; } = new();
}

public class QuestState
{
    public string QuestId { get; set; } = string.Empty;
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;
    public int CurrentObjectiveIndex { get; set; }
}

public enum QuestStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed
}

public class UseRule
{
    public string Id { get; set; } = string.Empty;
    public string? ObjectId { get; set; }
    public string? TargetObjectId { get; set; }
    public string? RequiredQuestId { get; set; }
    public QuestStatus? RequiredQuestStatus { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public string? SoundEffectId { get; set; }
}

public class TradeRule
{
    public string Id { get; set; } = string.Empty;
    public string? NpcId { get; set; }
    public string? OfferedObjectId { get; set; }
    public string? RequestedObjectId { get; set; }
    public int? Price { get; set; }
    public string ResultText { get; set; } = string.Empty;
    public string? SoundEffectId { get; set; }
}

public class EventRule
{
    public string Id { get; set; } = string.Empty;
    public string? TriggerFlag { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? SoundEffectId { get; set; }
}

public class PlayerStats
{
    /// <summary>Nombre del jugador.</summary>
    public string Name { get; set; } = "Aventurero";

    /// <summary>Fuerza del jugador.</summary>
    public int Strength { get; set; } = 20;

    /// <summary>Constitución del jugador.</summary>
    public int Constitution { get; set; } = 20;

    /// <summary>Inteligencia del jugador.</summary>
    public int Intelligence { get; set; } = 20;

    /// <summary>Destreza del jugador.</summary>
    public int Dexterity { get; set; } = 20;

    /// <summary>Carisma del jugador.</summary>
    public int Charisma { get; set; } = 20;

    /// <summary>Dinero del jugador en monedas.</summary>
    public int Gold { get; set; } = 0;
}

/// <summary>
/// Definición del jugador configurable desde el editor de mundos.
/// Las características (Fuerza, Constitución, Inteligencia, Destreza, Carisma)
/// deben sumar 100 puntos en total, con un mínimo de 10 cada una.
/// </summary>
public class PlayerDefinition
{
    public string Name { get; set; } = "Aventurero";

    /// <summary>Edad en años (10-90).</summary>
    public int Age { get; set; } = 25;

    /// <summary>Peso en kg (50-150, incrementos de 5).</summary>
    public int Weight { get; set; } = 70;

    /// <summary>Altura en cm (50-220, incrementos de 5).</summary>
    public int Height { get; set; } = 170;

    /// <summary>Fuerza (mínimo 10, máximo según puntos disponibles).</summary>
    public int Strength { get; set; } = 20;

    /// <summary>Constitución (mínimo 10, máximo según puntos disponibles).</summary>
    public int Constitution { get; set; } = 20;

    /// <summary>Inteligencia (mínimo 10, máximo según puntos disponibles).</summary>
    public int Intelligence { get; set; } = 20;

    /// <summary>Destreza (mínimo 10, máximo según puntos disponibles).</summary>
    public int Dexterity { get; set; } = 20;

    /// <summary>Carisma (mínimo 10, máximo según puntos disponibles).</summary>
    public int Charisma { get; set; } = 20;

    /// <summary>Dinero inicial en monedas (mínimo 0).</summary>
    public int InitialGold { get; set; } = 0;

    /// <summary>
    /// Calcula el total de puntos de características asignados.
    /// Debería ser siempre 100.
    /// </summary>
    [Browsable(false)]
    public int TotalAttributePoints => Strength + Constitution + Intelligence + Dexterity + Charisma;
}

public class GameState
{
    public string WorldId { get; set; } = string.Empty;
    public string? WorldMusicId { get; set; }
    
    // Clave copiada del GameInfo para persistir en la sesión de juego
    public string? WorldEncryptionKey { get; set; }
    
    public string CurrentRoomId { get; set; } = string.Empty;

    public PlayerStats Player { get; set; } = new();

    public List<Room> Rooms { get; set; } = new();
    public List<GameObject> Objects { get; set; } = new();
    public List<Npc> Npcs { get; set; } = new();

    public Dictionary<string, QuestState> Quests { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<UseRule> UseRules { get; set; } = new();
    public List<TradeRule> TradeRules { get; set; } = new();
    public List<EventRule> Events { get; set; } = new();

    public List<Door> Doors { get; set; } = new();

    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Counters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> InventoryObjectIds { get; set; } = new();

    /// <summary>Estado de la conversación activa (null si no hay diálogo en curso).</summary>
    public ConversationState? ActiveConversation { get; set; }

    public DateTime GameTime { get; set; } = default;
    public int TurnCounter { get; set; }
    public string TimeOfDay { get; set; } = "día";
    public WeatherType Weather { get; set; } = WeatherType.Despejado;
}

public class MapPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class MusicAsset
{
    /// <summary>
    /// Nombre del archivo de música (ej: "theme.mp3").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo de música en Base64.
    /// </summary>
    [Browsable(false)]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Duración del archivo en segundos.
    /// </summary>
    public double DurationSeconds { get; set; }
}

public class FxAsset
{
    /// <summary>
    /// Nombre del archivo de FX (ej: "explosion.wav").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo de FX en Base64.
    /// </summary>
    [Browsable(false)]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Duración del archivo en segundos.
    /// </summary>
    public double DurationSeconds { get; set; }
}

