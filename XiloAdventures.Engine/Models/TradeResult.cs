namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de una transacción de comercio (compra o venta).
/// Indica si la operación fue exitosa y proporciona detalles del intercambio.
/// </summary>
public class TradeResult
{
    /// <summary>
    /// Indica si la transacción fue exitosa.
    /// False si hubo algún problema (fondos insuficientes, objeto no disponible, etc.).
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado para mostrar al jugador.
    /// Incluye detalles de la transacción o el motivo del fallo.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad de dinero transferida en la transacción.
    /// Positivo para compras (jugador paga), usado para ventas también.
    /// </summary>
    public int MoneyTransferred { get; set; }

    /// <summary>
    /// Cantidad de unidades del item transferidas.
    /// Puede ser mayor que 1 para objetos apilables.
    /// </summary>
    public int ItemsTransferred { get; set; }
}
