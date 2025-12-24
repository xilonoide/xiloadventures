namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipos de daño para armas y habilidades de combate.
/// </summary>
public enum DamageType
{
    /// <summary>Daño físico (usa Fuerza).</summary>
    Physical,
    /// <summary>Daño mágico (usa Inteligencia).</summary>
    Magical,
    /// <summary>Daño perforante (ignora parte de la defensa).</summary>
    Piercing
}
