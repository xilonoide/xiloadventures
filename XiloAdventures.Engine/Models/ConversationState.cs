using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado de una conversación activa en tiempo de ejecución.
/// Mantiene el progreso del diálogo, las opciones disponibles y las variables locales.
/// </summary>
/// <remarks>
/// El ConversationEngine utiliza este estado para saber en qué punto
/// de la conversación se encuentra el jugador y qué opciones mostrarle.
/// Las variables locales permiten mantener información específica de la conversación.
/// </remarks>
public class ConversationState
{
    /// <summary>
    /// ID de la conversación activa (referencia a ConversationDefinition).
    /// </summary>
    public string ConversationId { get; set; } = "";

    /// <summary>
    /// ID del NPC con quien se está manteniendo la conversación.
    /// </summary>
    public string NpcId { get; set; } = "";

    /// <summary>
    /// ID del nodo actual en el árbol de conversación.
    /// </summary>
    public string CurrentNodeId { get; set; } = "";

    /// <summary>
    /// Indica si la conversación está activa.
    /// False cuando la conversación ha terminado o no se ha iniciado.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// IDs de nodos que ya han sido visitados en esta conversación.
    /// Útil para implementar diálogos que cambian según lo que ya se ha dicho.
    /// </summary>
    public List<string> VisitedNodeIds { get; set; } = new();

    /// <summary>
    /// Opciones de diálogo disponibles actualmente para el jugador.
    /// Se actualiza cada vez que se llega a un nodo PlayerChoice.
    /// </summary>
    public List<DialogueOption> CurrentOptions { get; set; } = new();

    /// <summary>
    /// Variables locales de la conversación.
    /// Permiten almacenar información que solo existe durante esta conversación.
    /// </summary>
    public Dictionary<string, object?> LocalVariables { get; set; } = new();
}
