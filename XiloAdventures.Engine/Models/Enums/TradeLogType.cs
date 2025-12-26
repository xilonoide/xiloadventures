namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipo de entrada en el log de comercio.
/// Determina el color y estilo con que se muestra en la UI.
/// </summary>
public enum TradeLogType
{
    /// <summary>
    /// Compra realizada (jugador compra del NPC).
    /// Típicamente mostrado en verde.
    /// </summary>
    Buy,

    /// <summary>
    /// Venta realizada (jugador vende al NPC).
    /// Típicamente mostrado en amarillo.
    /// </summary>
    Sell,

    /// <summary>
    /// Información general o mensaje del sistema.
    /// Mostrado en color neutral.
    /// </summary>
    Info,

    /// <summary>
    /// Error en la transacción (fondos insuficientes, etc.).
    /// Típicamente mostrado en rojo.
    /// </summary>
    Error
}
