namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Modo de movimiento para NPCs (patrulla y seguimiento).
/// </summary>
public enum MovementMode
{
    /// <summary>Movimiento basado en turnos del jugador.</summary>
    Turns,
    /// <summary>Movimiento basado en tiempo real (segundos).</summary>
    Time
}
