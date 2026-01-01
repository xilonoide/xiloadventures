using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Define un puerto de entrada o salida en un nodo del editor visual.
/// Los puertos permiten conectar nodos entre sí para transferir
/// flujo de ejecución (Execution) o datos (Data).
/// </summary>
/// <remarks>
/// Los puertos de ejecución se representan como triángulos y determinan
/// el orden de ejecución. Los puertos de datos se representan como círculos
/// y permiten pasar valores como números, cadenas o booleanos.
/// </remarks>
public class NodePort
{
    /// <summary>
    /// Nombre identificador del puerto.
    /// Debe ser único dentro de los puertos de entrada o salida del nodo.
    /// Ejemplos: "Exec", "True", "False", "Value", "Result".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de puerto: Execution (flujo) o Data (valor).
    /// Determina la representación visual y el tipo de conexiones permitidas.
    /// </summary>
    public PortType PortType { get; set; }

    /// <summary>
    /// Tipo de dato para puertos Data.
    /// Valores comunes: "string", "int", "bool", "float".
    /// Null para puertos de tipo Execution.
    /// </summary>
    public string? DataType { get; set; }

    /// <summary>
    /// Valor por defecto para puertos de entrada cuando no están conectados.
    /// El tipo debe ser compatible con DataType.
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Etiqueta descriptiva a mostrar junto al puerto en el editor.
    /// Si es null, se usa el Name como etiqueta.
    /// </summary>
    public string? Label { get; set; }
}
