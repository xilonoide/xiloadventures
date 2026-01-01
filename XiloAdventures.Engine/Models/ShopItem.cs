namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un objeto en el inventario de tienda de un NPC comerciante.
/// Define qué objetos vende el NPC y en qué cantidad están disponibles.
/// </summary>
/// <remarks>
/// Los objetos de tienda están separados del inventario personal del NPC.
/// El precio de venta se calcula multiplicando el precio base del objeto
/// por el SellPriceMultiplier del NPC.
/// </remarks>
public class ShopItem
{
    /// <summary>
    /// ID del objeto que se vende.
    /// Debe coincidir con el Id de un GameObject existente en el mundo.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad disponible para vender.
    /// Valores especiales:
    /// - -1: Stock infinito (el objeto siempre está disponible)
    /// - 0: Agotado
    /// - n (positivo): Cantidad limitada que decrece al comprar
    /// </summary>
    public int Quantity { get; set; } = -1;
}
