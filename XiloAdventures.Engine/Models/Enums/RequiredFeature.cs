namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Características del juego que deben estar habilitadas para que un nodo esté disponible.
/// </summary>
public enum RequiredFeature
{
    /// <summary>Sin requisito, siempre disponible.</summary>
    None,
    /// <summary>Requiere que el sistema de combate esté habilitado.</summary>
    Combat,
    /// <summary>Requiere que las necesidades básicas estén habilitadas.</summary>
    BasicNeeds
}
