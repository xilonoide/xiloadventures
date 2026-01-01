using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Regla de uso de objetos (cuando se usa un objeto sobre otro).
/// Define la lógica para interacciones específicas entre objetos del juego,
/// permitiendo crear puzzles y mecánicas personalizadas.
/// </summary>
public class UseRule
{
    /// <summary>
    /// Identificador único de la regla.
    /// Generado automáticamente al crear la regla.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del objeto que se usa (el que tiene el jugador).
    /// Null si la regla aplica a cualquier objeto.
    /// </summary>
    public string? ObjectId { get; set; }

    /// <summary>
    /// ID del objeto sobre el que se usa (el objetivo).
    /// Null si la regla aplica a cualquier objetivo.
    /// </summary>
    public string? TargetObjectId { get; set; }

    /// <summary>
    /// ID de la misión requerida para que la regla funcione.
    /// Permite condicionar el uso a un estado específico del juego.
    /// </summary>
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión para que la regla se active.
    /// Solo se verifica si RequiredQuestId no es null.
    /// </summary>
    public QuestStatus? RequiredQuestStatus { get; set; }

    /// <summary>
    /// Texto que se muestra al jugador cuando la regla se aplica.
    /// Describe el resultado de la acción.
    /// </summary>
    public string ResultText { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce al aplicar la regla.
    /// Null si no hay sonido asociado.
    /// </summary>
    public string? SoundEffectId { get; set; }
}
