using System;
using System.Collections.Generic;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un nodo individual en el grafo de script visual.
/// Cada nodo tiene un tipo específico que determina su comportamiento,
/// puertos de entrada/salida, y propiedades configurables.
/// </summary>
/// <remarks>
/// Los nodos se conectan entre sí mediante sus puertos para formar
/// el flujo de ejecución del script. El tipo de nodo (NodeType)
/// determina qué operación realiza cuando se ejecuta.
/// </remarks>
public class ScriptNode
{
    /// <summary>
    /// Identificador único del nodo dentro del script.
    /// Generado automáticamente como GUID al crear el nodo.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tipo de nodo que determina su comportamiento y puertos disponibles.
    /// El tipo se define en el registro de tipos de nodo (NodeTypeRegistry).
    /// </summary>
    public NodeTypeId NodeType { get; set; }

    /// <summary>
    /// Categoría del nodo: Event, Condition, Action, Flow, Variable o Dialogue.
    /// Determina el color del nodo en el editor visual.
    /// </summary>
    public NodeCategory Category { get; set; }

    /// <summary>
    /// Posición horizontal del nodo en el canvas del editor.
    /// Unidades en píxeles lógicos.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Posición vertical del nodo en el canvas del editor.
    /// Unidades en píxeles lógicos.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Propiedades configurables del nodo.
    /// Las claves son case-insensitive y los valores dependen del tipo de nodo.
    /// Ejemplos: "Message", "ObjectId", "RoomId", "FlagName".
    /// </summary>
    public Dictionary<string, object?> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Comentario opcional del usuario para documentar el propósito del nodo.
    /// Se muestra como tooltip en el editor.
    /// </summary>
    public string? Comment { get; set; }
}
