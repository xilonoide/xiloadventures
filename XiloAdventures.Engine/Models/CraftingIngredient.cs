namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un ingrediente necesario para fabricar un objeto.
/// Define qué objeto y en qué cantidad se requiere para una receta de fabricación.
/// </summary>
public class CraftingIngredient
{
    /// <summary>
    /// ID del objeto requerido como ingrediente.
    /// Debe coincidir con el ID de un GameObject definido en el mundo.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Cantidad requerida de este objeto para la receta.
    /// Valor por defecto: 1.
    /// </summary>
    public int Quantity { get; set; } = 1;
}
