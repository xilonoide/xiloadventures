using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Modelo completo del mundo del juego.
/// Contiene todas las definiciones de salas, objetos, NPCs, misiones y recursos.
/// </summary>
public class WorldModel
{
    /// <summary>
    /// Información general del juego (título, configuración inicial, etc.).
    /// </summary>
    public GameInfo Game { get; set; } = new();

    /// <summary>
    /// Definición del jugador (estadísticas iniciales, nombre, etc.).
    /// </summary>
    public PlayerDefinition Player { get; set; } = new();

    /// <summary>
    /// Lista de todas las salas del mundo.
    /// </summary>
    public List<Room> Rooms { get; set; } = new();

    /// <summary>
    /// Lista de todos los objetos del mundo.
    /// </summary>
    public List<GameObject> Objects { get; set; } = new();

    /// <summary>
    /// Lista de todos los NPCs del mundo.
    /// </summary>
    public List<Npc> Npcs { get; set; } = new();

    /// <summary>
    /// Lista de todas las misiones definidas.
    /// </summary>
    public List<QuestDefinition> Quests { get; set; } = new();

    /// <summary>
    /// Reglas de uso de objetos.
    /// </summary>
    public List<UseRule> UseRules { get; set; } = new();

    /// <summary>
    /// Reglas de comercio.
    /// </summary>
    public List<TradeRule> TradeRules { get; set; } = new();

    /// <summary>
    /// Reglas de eventos.
    /// </summary>
    public List<EventRule> Events { get; set; } = new();

    /// <summary>
    /// Lista de puertas del mundo.
    /// </summary>
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
    /// Biblioteca de habilidades de combate del mundo.
    /// </summary>
    public List<CombatAbility> Abilities { get; set; } = new();

    /// <summary>
    /// Posiciones del mapa para cada sala (coordenadas lógicas X/Y) usadas por el editor.
    /// </summary>
    public Dictionary<string, MapPosition> RoomPositions { get; set; } = new();

    /// <summary>
    /// Carpetas para organizar elementos en el editor.
    /// Solo afectan la visualización, no el comportamiento del juego.
    /// </summary>
    public List<EditorFolder> Folders { get; set; } = new();

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
