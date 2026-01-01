namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un objeto en el inventario con cantidad.
/// Usado para inventarios de jugador, NPCs y definiciones iniciales.
/// </summary>
public class InventoryItem
{
    /// <summary>
    /// ID del objeto en el inventario.
    /// Debe coincidir con el ID de un GameObject existente.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad del objeto.
    /// Debe ser mayor que 0.
    /// </summary>
    public int Quantity { get; set; } = 1;
}
