namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Modo de duración para los modificadores temporales.
/// </summary>
public enum ModifierDurationType
{
    /// <summary>El modificador dura un número de turnos.</summary>
    Turns,
    /// <summary>El modificador dura un número de segundos (tiempo real).</summary>
    Seconds,
    /// <summary>El modificador no caduca (permanente hasta que se elimine).</summary>
    Permanent
}
