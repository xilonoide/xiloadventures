using System;
using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Define un script visual completo asociado a una entidad del juego.
/// Los scripts son grafos de nodos que definen comportamientos reactivos
/// a eventos del juego, como entrar en una habitación o interactuar con un objeto.
/// </summary>
/// <remarks>
/// Los scripts se editan visualmente en el editor de scripts y se ejecutan
/// por el ScriptEngine cuando ocurren los eventos correspondientes.
/// Cada script pertenece a una entidad específica (Room, NPC, Object, etc.)
/// identificada por OwnerType y OwnerId.
/// </remarks>
public class ScriptDefinition
{
    /// <summary>
    /// Identificador único del script.
    /// Generado automáticamente como GUID al crear el script.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre descriptivo del script para mostrar en el editor.
    /// </summary>
    public string Name { get; set; } = "Nuevo Script";

    /// <summary>
    /// Tipo de entidad propietaria del script.
    /// Valores posibles: "Game", "Room", "Door", "Npc", "GameObject", "Quest".
    /// </summary>
    public string OwnerType { get; set; } = string.Empty;

    /// <summary>
    /// ID de la entidad propietaria del script.
    /// Debe corresponder a una entidad válida del tipo especificado.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Lista de nodos que componen el script.
    /// Cada nodo representa una operación: evento, condición, acción o flujo.
    /// </summary>
    public List<ScriptNode> Nodes { get; set; } = new();

    /// <summary>
    /// Lista de conexiones entre los puertos de los nodos.
    /// Las conexiones definen el flujo de ejecución y transferencia de datos.
    /// </summary>
    public List<NodeConnection> Connections { get; set; } = new();
}
