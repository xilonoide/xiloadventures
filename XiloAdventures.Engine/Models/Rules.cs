using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Regla de uso de objetos (cuando se usa un objeto sobre otro).
/// </summary>
public class UseRule
{
    /// <summary>
    /// Identificador único de la regla.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del objeto que se usa.
    /// </summary>
    public string? ObjectId { get; set; }

    /// <summary>
    /// ID del objeto sobre el que se usa.
    /// </summary>
    public string? TargetObjectId { get; set; }

    /// <summary>
    /// ID de la misión requerida para que la regla funcione.
    /// </summary>
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión.
    /// </summary>
    public QuestStatus? RequiredQuestStatus { get; set; }

    /// <summary>
    /// Texto que se muestra al aplicar la regla.
    /// </summary>
    public string ResultText { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce.
    /// </summary>
    public string? SoundEffectId { get; set; }
}

/// <summary>
/// Regla de comercio con NPCs.
/// </summary>
public class TradeRule
{
    /// <summary>
    /// Identificador único de la regla.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del NPC comerciante.
    /// </summary>
    public string? NpcId { get; set; }

    /// <summary>
    /// ID del objeto ofrecido por el jugador.
    /// </summary>
    public string? OfferedObjectId { get; set; }

    /// <summary>
    /// ID del objeto solicitado por el NPC.
    /// </summary>
    public string? RequestedObjectId { get; set; }

    /// <summary>
    /// Precio de la transacción.
    /// </summary>
    public int? Price { get; set; }

    /// <summary>
    /// Texto que se muestra al completar el intercambio.
    /// </summary>
    public string ResultText { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce.
    /// </summary>
    public string? SoundEffectId { get; set; }
}

/// <summary>
/// Regla de evento (para lógica legacy basada en flags).
/// </summary>
public class EventRule
{
    /// <summary>
    /// Identificador único del evento.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Flag que dispara el evento.
    /// </summary>
    public string? TriggerFlag { get; set; }

    /// <summary>
    /// Descripción del evento.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce.
    /// </summary>
    public string? SoundEffectId { get; set; }
}
