using System;
using System.Collections.Generic;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado completo de una partida en curso.
/// Contiene todos los datos necesarios para guardar y restaurar una partida.
/// </summary>
public class GameState
{
    /// <summary>
    /// ID del mundo al que pertenece esta partida.
    /// </summary>
    public string WorldId { get; set; } = string.Empty;

    /// <summary>
    /// ID de la música de fondo global del mundo.
    /// </summary>
    public string? WorldMusicId { get; set; }

    /// <summary>
    /// Clave de cifrado copiada del GameInfo para persistir en la sesión de juego.
    /// </summary>
    public string? WorldEncryptionKey { get; set; }

    /// <summary>
    /// ID de la sala donde se encuentra actualmente el jugador.
    /// </summary>
    public string CurrentRoomId { get; set; } = string.Empty;

    /// <summary>
    /// Estadísticas y estado del jugador.
    /// </summary>
    public PlayerStats Player { get; set; } = new();

    #region World Elements

    /// <summary>
    /// Estado actual de todas las salas (puede modificarse durante el juego).
    /// </summary>
    public List<Room> Rooms { get; set; } = new();

    /// <summary>
    /// Estado actual de todos los objetos (puede modificarse durante el juego).
    /// </summary>
    public List<GameObject> Objects { get; set; } = new();

    /// <summary>
    /// Estado actual de todos los NPCs (puede modificarse durante el juego).
    /// </summary>
    public List<Npc> Npcs { get; set; } = new();

    /// <summary>
    /// Habilidades de combate disponibles en el mundo.
    /// </summary>
    public List<CombatAbility> Abilities { get; set; } = new();

    /// <summary>
    /// Estado actual de las puertas.
    /// </summary>
    public List<Door> Doors { get; set; } = new();

    #endregion

    #region Quests and Rules

    /// <summary>
    /// Estado de las misiones (diccionario por ID de misión).
    /// </summary>
    public Dictionary<string, QuestState> Quests { get; set; } = new(StringComparer.OrdinalIgnoreCase);

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

    #endregion

    #region Script State

    /// <summary>
    /// Flags booleanos usados por los scripts.
    /// </summary>
    public Dictionary<string, bool> Flags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Contadores numéricos usados por los scripts.
    /// </summary>
    public Dictionary<string, int> Counters { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    #endregion

    #region Inventory

    /// <summary>
    /// IDs de objetos en el inventario del jugador.
    /// </summary>
    public List<string> InventoryObjectIds { get; set; } = new();

    #endregion

    #region Active Systems

    /// <summary>
    /// Estado de la conversación activa (null si no hay diálogo en curso).
    /// </summary>
    public ConversationState? ActiveConversation { get; set; }

    /// <summary>
    /// Estado del combate activo (null si no hay combate en curso).
    /// </summary>
    public CombatState? ActiveCombat { get; set; }

    /// <summary>
    /// Lista de modificadores temporales activos en el jugador.
    /// </summary>
    public List<TemporaryModifier> ActiveModifiers { get; set; } = new();

    #endregion

    #region Time and Weather

    /// <summary>
    /// Tiempo de juego actual.
    /// </summary>
    public DateTime GameTime { get; set; } = default;

    /// <summary>
    /// Contador de turnos transcurridos.
    /// </summary>
    public int TurnCounter { get; set; }

    /// <summary>
    /// Momento del día en texto (ej: "día", "noche", "amanecer").
    /// </summary>
    public string TimeOfDay { get; set; } = "día";

    /// <summary>
    /// Clima actual del mundo.
    /// </summary>
    public WeatherType Weather { get; set; } = WeatherType.Despejado;

    #endregion

    #region UI Settings (solo para Player exportado)

    /// <summary>
    /// Sonido activado/desactivado.
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Tamaño de fuente.
    /// </summary>
    public double FontSize { get; set; } = 18.0;

    /// <summary>
    /// Familia de fuente.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Volumen de música (0-10).
    /// </summary>
    public double MusicVolume { get; set; } = 10.0;

    /// <summary>
    /// Volumen de efectos (0-10).
    /// </summary>
    public double EffectsVolume { get; set; } = 10.0;

    /// <summary>
    /// Volumen maestro (1-10).
    /// </summary>
    public double MasterVolume { get; set; } = 10.0;

    /// <summary>
    /// Volumen de voz (0-10).
    /// </summary>
    public double VoiceVolume { get; set; } = 10.0;

    /// <summary>
    /// Mapa visible.
    /// </summary>
    public bool MapEnabled { get; set; } = true;

    /// <summary>
    /// Usar IA para comandos desconocidos.
    /// </summary>
    public bool UseLlmForUnknownCommands { get; set; } = false;

    #endregion
}
