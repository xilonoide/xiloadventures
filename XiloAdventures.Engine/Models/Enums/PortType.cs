namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipos de puertos disponibles en los nodos del editor visual.
/// Determina cómo se representan visualmente y qué tipo de conexiones aceptan.
/// </summary>
public enum PortType
{
    /// <summary>
    /// Puerto de flujo de ejecución.
    /// Representado como un triángulo en el editor.
    /// Determina el orden de ejecución de los nodos.
    /// </summary>
    Execution,

    /// <summary>
    /// Puerto de datos para transferir valores entre nodos.
    /// Representado como un círculo en el editor.
    /// Permite pasar información como números, cadenas o booleanos.
    /// </summary>
    Data
}
