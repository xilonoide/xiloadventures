using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Define una conversación editable visualmente usando el sistema de blueprints.
/// Especializada para diálogos con NPCs, permitiendo crear árboles de conversación
/// con múltiples ramas, condiciones y acciones.
/// </summary>
/// <remarks>
/// Las conversaciones usan el mismo sistema de nodos que los scripts,
/// pero con tipos específicos para diálogos: Conversation_Start, NpcSay,
/// PlayerChoice, etc. Cada NPC puede tener una o más conversaciones definidas.
/// </remarks>
public class ConversationDefinition
{
    /// <summary>
    /// Identificador único de la conversación.
    /// Generado automáticamente como GUID.
    /// </summary>
    [Browsable(false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre descriptivo de la conversación para el editor.
    /// </summary>
    public string Name { get; set; } = "Nueva Conversación";

    /// <summary>
    /// Nodos que componen el árbol de conversación.
    /// Reutiliza ScriptNode para compatibilidad con el sistema de scripts.
    /// </summary>
    [Browsable(false)]
    public List<ScriptNode> Nodes { get; set; } = new();

    /// <summary>
    /// Conexiones entre los nodos de la conversación.
    /// </summary>
    [Browsable(false)]
    public List<NodeConnection> Connections { get; set; } = new();

    /// <summary>
    /// ID del nodo de inicio de la conversación.
    /// Debe ser un nodo de tipo Conversation_Start.
    /// </summary>
    [Browsable(false)]
    public string? StartNodeId { get; set; }
}
