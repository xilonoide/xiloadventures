namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Lado desde el que se puede accionar una puerta.
/// Determina si la puerta puede abrirse/cerrarse desde un lado específico o ambos.
/// </summary>
/// <remarks>
/// Esta restricción afecta solo a los comandos de abrir/cerrar, no al movimiento.
/// Una vez que la puerta está abierta, el jugador puede pasar desde cualquier lado.
/// </remarks>
public enum DoorOpenSide
{
    /// <summary>
    /// La puerta puede accionarse desde cualquier lado (comportamiento por defecto).
    /// </summary>
    Both = 0,

    /// <summary>
    /// La puerta solo puede accionarse desde la sala A.
    /// Útil para puertas que solo se abren desde dentro.
    /// </summary>
    FromAOnly = 1,

    /// <summary>
    /// La puerta solo puede accionarse desde la sala B.
    /// Útil para puertas que solo se abren desde fuera.
    /// </summary>
    FromBOnly = 2
}
