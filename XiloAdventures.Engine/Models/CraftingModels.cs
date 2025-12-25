using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un ingrediente necesario para fabricar un objeto.
/// </summary>
public class CraftingIngredient
{
    /// <summary>ID del objeto requerido.</summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>Cantidad requerida de este objeto.</summary>
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Ubicacion de un item para fabricacion.
/// </summary>
public enum CraftItemLocation
{
    /// <summary>El objeto esta en el inventario del jugador.</summary>
    Inventory,
    /// <summary>El objeto esta en la sala actual.</summary>
    Room
}

/// <summary>
/// Item disponible para fabricacion con informacion completa.
/// </summary>
public class CraftItem
{
    /// <summary>ID del objeto en el juego.</summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>Nombre del objeto.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Cantidad disponible.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Cantidad seleccionada para fabricar.</summary>
    public int SelectedQuantity { get; set; } = 0;

    /// <summary>Peso del objeto en gramos.</summary>
    public int Weight { get; set; } = 0;

    /// <summary>Volumen del objeto en cm3.</summary>
    public double Volume { get; set; } = 0;

    /// <summary>Ubicacion del objeto (inventario o sala).</summary>
    public CraftItemLocation Location { get; set; } = CraftItemLocation.Inventory;

    /// <summary>Descripcion formateada de la ubicacion.</summary>
    public string LocationText => Location == CraftItemLocation.Inventory ? "(inventario)" : "(sala)";
}

/// <summary>
/// Estado de una sesion de fabricacion activa.
/// </summary>
public class CraftState
{
    /// <summary>Indica si hay fabricacion activa.</summary>
    public bool IsActive { get; set; }

    /// <summary>Items disponibles para usar como ingredientes.</summary>
    public List<CraftItem> AvailableItems { get; set; } = new();

    /// <summary>Items seleccionados como ingredientes.</summary>
    public List<CraftItem> SelectedIngredients { get; set; } = new();

    /// <summary>Receta que coincide con los ingredientes seleccionados (null si no hay coincidencia).</summary>
    public GameObject? MatchingRecipe { get; set; }

    /// <summary>Cantidad a fabricar.</summary>
    public int CraftQuantity { get; set; } = 1;

    /// <summary>Cantidad maxima que se puede fabricar con los ingredientes disponibles.</summary>
    public int MaxCraftQuantity { get; set; } = 0;
}

/// <summary>
/// Resultado de una operacion de fabricacion.
/// </summary>
public class CraftResult
{
    /// <summary>Si la fabricacion fue exitosa.</summary>
    public bool Success { get; set; }

    /// <summary>Mensaje descriptivo del resultado.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>ID del objeto creado.</summary>
    public string? CreatedObjectId { get; set; }

    /// <summary>Cantidad de objetos creados.</summary>
    public int QuantityCreated { get; set; }

    /// <summary>Indica si el objeto fue anadido al inventario o dejado en la sala.</summary>
    public bool AddedToInventory { get; set; }
}
