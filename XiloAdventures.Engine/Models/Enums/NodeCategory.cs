namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Categorías de nodos para el editor visual de scripts.
/// Cada categoría agrupa nodos con funcionalidad similar y se representa
/// con un color distintivo en el editor.
/// </summary>
public enum NodeCategory
{
    /// <summary>
    /// Nodos de evento que actúan como puntos de entrada del script.
    /// Representados en color verde en el editor.
    /// Ejemplos: OnEnter, OnTake, OnGameStart.
    /// </summary>
    Event,

    /// <summary>
    /// Nodos de condición que evalúan estados del juego.
    /// Representados en color amarillo en el editor.
    /// Ejemplos: HasItem, IsInRoom, CompareCounter.
    /// </summary>
    Condition,

    /// <summary>
    /// Nodos de acción que modifican el estado del juego.
    /// Representados en color azul en el editor.
    /// Ejemplos: ShowMessage, GiveItem, TeleportPlayer.
    /// </summary>
    Action,

    /// <summary>
    /// Nodos de control de flujo que determinan la ejecución.
    /// Representados en color gris en el editor.
    /// Ejemplos: Branch, Sequence, Delay.
    /// </summary>
    Flow,

    /// <summary>
    /// Nodos de variables que proporcionan o calculan valores.
    /// Representados en color naranja en el editor.
    /// Ejemplos: GetPlayerMoney, ConstantInt, Math operations.
    /// </summary>
    Variable,

    /// <summary>
    /// Nodos de conversación para diálogos con NPCs.
    /// Representados en color morado en el editor.
    /// Ejemplos: Conversation_Start, NpcSay, PlayerChoice.
    /// </summary>
    Dialogue
}
