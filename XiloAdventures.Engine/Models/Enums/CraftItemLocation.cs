namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Ubicación de un item disponible para fabricación.
/// Indica dónde se encuentra físicamente el objeto que puede usarse como ingrediente.
/// </summary>
public enum CraftItemLocation
{
    /// <summary>
    /// El objeto está en el inventario del jugador.
    /// Disponible inmediatamente para usar en fabricación.
    /// </summary>
    Inventory,

    /// <summary>
    /// El objeto está en la sala actual donde se encuentra el jugador.
    /// Puede usarse si el sistema de fabricación permite usar objetos del entorno.
    /// </summary>
    Room
}
