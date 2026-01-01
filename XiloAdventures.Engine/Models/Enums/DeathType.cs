namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipo de muerte del jugador.
/// </summary>
public enum DeathType
{
    /// <summary>Muerte por hambre (hambre llegó a 100).</summary>
    Hunger,
    /// <summary>Muerte por sed (sed llegó a 100).</summary>
    Thirst,
    /// <summary>Muerte por agotamiento (sueño llegó a 100).</summary>
    Sleep,
    /// <summary>Muerte por perder toda la salud (salud llegó a 0).</summary>
    Health,
    /// <summary>Muerte por perder toda la cordura (cordura llegó a 0).</summary>
    Sanity
}
