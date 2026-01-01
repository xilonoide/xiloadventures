using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Item disponible para fabricación con información completa.
/// Representa un objeto que puede usarse como ingrediente en el sistema de crafting,
/// incluyendo su ubicación, cantidad disponible y cantidad seleccionada.
/// </summary>
public class CraftItem
{
    /// <summary>
    /// ID del objeto en el juego.
    /// Referencia al GameObject correspondiente.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del objeto para mostrar en la UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad disponible del objeto.
    /// Representa cuántas unidades tiene el jugador o hay en la sala.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Cantidad seleccionada para usar en la fabricación actual.
    /// El jugador puede ajustar este valor hasta el máximo disponible.
    /// </summary>
    public int SelectedQuantity { get; set; } = 0;

    /// <summary>
    /// Peso del objeto en gramos.
    /// Usado para cálculos de carga del inventario.
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// Volumen del objeto en centímetros cúbicos (cm³).
    /// Usado para cálculos de capacidad de contenedores.
    /// </summary>
    public double Volume { get; set; } = 0;

    /// <summary>
    /// Ubicación del objeto (inventario o sala).
    /// Determina de dónde se tomará el objeto al fabricar.
    /// </summary>
    public CraftItemLocation Location { get; set; } = CraftItemLocation.Inventory;

    /// <summary>
    /// Descripción formateada de la ubicación para mostrar en la UI.
    /// Devuelve "(inventario)" o "(sala)" según corresponda.
    /// </summary>
    public string LocationText => Location == CraftItemLocation.Inventory ? "(inventario)" : "(sala)";
}
