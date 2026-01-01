namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de una operación de fabricación.
/// Contiene información sobre el éxito o fracaso de la operación
/// y los objetos creados si fue exitosa.
/// </summary>
public class CraftResult
{
    /// <summary>
    /// Indica si la fabricación fue exitosa.
    /// False si faltaron ingredientes o hubo otro problema.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Mensaje descriptivo del resultado para mostrar al jugador.
    /// Incluye detalles sobre qué se creó o por qué falló.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// ID del objeto creado (si la fabricación fue exitosa).
    /// Null si la fabricación falló.
    /// </summary>
    public string? CreatedObjectId { get; set; }

    /// <summary>
    /// Cantidad de objetos creados.
    /// 0 si la fabricación falló.
    /// </summary>
    public int QuantityCreated { get; set; }

    /// <summary>
    /// Indica si el objeto fue añadido al inventario.
    /// True: añadido al inventario del jugador.
    /// False: dejado en la sala (por falta de espacio o configuración).
    /// </summary>
    public bool AddedToInventory { get; set; }
}
