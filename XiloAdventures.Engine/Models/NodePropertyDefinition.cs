namespace XiloAdventures.Engine.Models;

/// <summary>
/// Definición de una propiedad editable de un nodo.
/// Las propiedades se muestran en el panel de propiedades del editor
/// y permiten configurar el comportamiento específico de cada nodo.
/// </summary>
/// <remarks>
/// Por ejemplo, un nodo "ShowMessage" tiene una propiedad "Message" de tipo string,
/// mientras que un nodo "TeleportPlayer" tiene una propiedad "RoomId" que referencia
/// una entidad Room del mundo.
/// </remarks>
public class NodePropertyDefinition
{
    /// <summary>
    /// Nombre interno de la propiedad (clave en el diccionario Properties del nodo).
    /// Case-insensitive. Ejemplo: "Message", "ObjectId", "Amount".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Nombre legible para mostrar en el editor.
    /// Ejemplo: "Mensaje", "ID del Objeto", "Cantidad".
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de dato de la propiedad.
    /// Valores comunes: "string", "int", "bool", "float", "select".
    /// Determina qué control se usa para editar el valor.
    /// </summary>
    public string DataType { get; set; } = "string";

    /// <summary>
    /// Valor por defecto cuando se crea un nuevo nodo.
    /// El tipo debe ser compatible con DataType.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Para propiedades de tipo "select", las opciones disponibles.
    /// Se muestran en un dropdown en el editor.
    /// </summary>
    public string[]? Options { get; set; }

    /// <summary>
    /// Para propiedades que referencian entidades del mundo.
    /// Valores: "Room", "Object", "Npc", "Door", "Quest", etc.
    /// Habilita el selector de entidades en el editor.
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Indica si la propiedad es obligatoria.
    /// Las propiedades obligatorias se validan antes de ejecutar el script.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Determina si esta propiedad requiere un valor válido para la ejecución.
    /// True si IsRequired es true o si referencia una entidad (EntityType).
    /// </summary>
    public bool RequiresValue => IsRequired || !string.IsNullOrEmpty(EntityType);
}
