namespace XiloAdventures.Engine.Models;

/// <summary>
/// Regla de evento (para lógica legacy basada en flags).
/// Permite definir eventos que se disparan cuando ciertas banderas se activan.
/// Esta es una funcionalidad heredada; para nueva lógica se recomienda usar scripts.
/// </summary>
public class EventRule
{
    /// <summary>
    /// Identificador único del evento.
    /// Generado automáticamente al crear la regla.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Flag (bandera) que dispara el evento.
    /// Cuando esta variable booleana se activa, el evento se ejecuta.
    /// </summary>
    public string? TriggerFlag { get; set; }

    /// <summary>
    /// Descripción del evento para el diseñador.
    /// Explica qué hace el evento cuando se dispara.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce al disparar el evento.
    /// Null si no hay sonido asociado.
    /// </summary>
    public string? SoundEffectId { get; set; }
}
