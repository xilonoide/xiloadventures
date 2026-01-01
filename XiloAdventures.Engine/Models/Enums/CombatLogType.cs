namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipo de entrada en el log de combate para formateo.
/// </summary>
public enum CombatLogType
{
    /// <summary>Mensaje normal.</summary>
    Normal,
    /// <summary>Ataque exitoso.</summary>
    Hit,
    /// <summary>Ataque fallido.</summary>
    Miss,
    /// <summary>Golpe crítico.</summary>
    Critical,
    /// <summary>Fallo épico.</summary>
    Fumble,
    /// <summary>Victoria.</summary>
    Victory,
    /// <summary>Derrota.</summary>
    Defeat,
    /// <summary>Huida exitosa.</summary>
    Fled,
    /// <summary>Información del sistema.</summary>
    System
}
