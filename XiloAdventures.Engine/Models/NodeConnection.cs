using System;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa una conexión entre dos puertos de nodos diferentes en un script.
/// Las conexiones definen el flujo de ejecución y la transferencia de datos
/// entre nodos del grafo visual.
/// </summary>
/// <remarks>
/// Una conexión va desde un puerto de salida (From) hacia un puerto de entrada (To).
/// Los puertos de ejecución solo pueden conectarse con otros puertos de ejecución,
/// y los puertos de datos solo con otros puertos de datos del mismo tipo.
/// </remarks>
public class NodeConnection
{
    /// <summary>
    /// Identificador único de la conexión.
    /// Generado automáticamente como GUID al crear la conexión.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID del nodo origen de la conexión.
    /// Debe corresponder a un nodo existente en el script.
    /// </summary>
    public string FromNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del puerto de salida en el nodo origen.
    /// Debe corresponder a un OutputPort del nodo origen.
    /// </summary>
    public string FromPortName { get; set; } = string.Empty;

    /// <summary>
    /// ID del nodo destino de la conexión.
    /// Debe corresponder a un nodo existente en el script.
    /// </summary>
    public string ToNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del puerto de entrada en el nodo destino.
    /// Debe corresponder a un InputPort del nodo destino.
    /// </summary>
    public string ToPortName { get; set; } = string.Empty;
}
