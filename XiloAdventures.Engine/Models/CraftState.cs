using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado de una sesión de fabricación activa.
/// Mantiene toda la información necesaria durante el proceso de crafting,
/// incluyendo los items disponibles, seleccionados y la receta coincidente.
/// </summary>
public class CraftState
{
    /// <summary>
    /// Indica si hay una sesión de fabricación activa.
    /// Solo puede haber una sesión activa a la vez.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Items disponibles para usar como ingredientes.
    /// Incluye objetos del inventario y de la sala actual.
    /// </summary>
    public List<CraftItem> AvailableItems { get; set; } = new();

    /// <summary>
    /// Items que el jugador ha seleccionado como ingredientes.
    /// Se actualizan cuando el jugador añade o quita ingredientes.
    /// </summary>
    public List<CraftItem> SelectedIngredients { get; set; } = new();

    /// <summary>
    /// Receta que coincide con los ingredientes seleccionados.
    /// Null si no hay ninguna receta válida con la combinación actual.
    /// </summary>
    public GameObject? MatchingRecipe { get; set; }

    /// <summary>
    /// Cantidad de objetos a fabricar.
    /// El jugador puede ajustar hasta MaxCraftQuantity.
    /// </summary>
    public int CraftQuantity { get; set; } = 1;

    /// <summary>
    /// Cantidad máxima que se puede fabricar con los ingredientes disponibles.
    /// Calculado automáticamente según las cantidades de ingredientes.
    /// </summary>
    public int MaxCraftQuantity { get; set; } = 0;
}
