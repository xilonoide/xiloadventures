using System;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Entrada en el registro de transacciones de una sesión de comercio.
/// Permite mantener un historial de compras y ventas realizadas.
/// </summary>
public class TradeLogEntry
{
    /// <summary>
    /// Mensaje descriptivo de la transacción.
    /// Ejemplo: "Compraste Espada de Hierro por 50 monedas".
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Momento en que se realizó la transacción.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Tipo de entrada para determinar el formato visual en la UI.
    /// </summary>
    public TradeLogType LogType { get; set; } = TradeLogType.Info;
}
