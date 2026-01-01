namespace XiloAdventures.Engine.Models;

/// <summary>
/// Regla de comercio con NPCs.
/// Define intercambios específicos entre el jugador y un NPC comerciante,
/// incluyendo qué objetos se pueden intercambiar y a qué precio.
/// </summary>
public class TradeRule
{
    /// <summary>
    /// Identificador único de la regla de comercio.
    /// Generado automáticamente al crear la regla.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// ID del NPC comerciante al que aplica esta regla.
    /// Null si aplica a cualquier NPC comerciante.
    /// </summary>
    public string? NpcId { get; set; }

    /// <summary>
    /// ID del objeto ofrecido por el jugador en el intercambio.
    /// Null si el jugador no ofrece objeto (solo paga dinero).
    /// </summary>
    public string? OfferedObjectId { get; set; }

    /// <summary>
    /// ID del objeto solicitado/entregado por el NPC.
    /// Null si el NPC no entrega objeto (solo recibe dinero).
    /// </summary>
    public string? RequestedObjectId { get; set; }

    /// <summary>
    /// Precio de la transacción en monedas.
    /// Positivo: el jugador paga. Negativo: el jugador recibe.
    /// </summary>
    public int? Price { get; set; }

    /// <summary>
    /// Texto que se muestra al completar el intercambio.
    /// Describe el resultado de la transacción.
    /// </summary>
    public string ResultText { get; set; } = string.Empty;

    /// <summary>
    /// ID del efecto de sonido que se reproduce al completar el intercambio.
    /// Null si no hay sonido asociado.
    /// </summary>
    public string? SoundEffectId { get; set; }
}
