using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Definición de una habilidad de combate.
/// </summary>
public class CombatAbility
{
    /// <summary>
    /// Identificador único de la habilidad.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la habilidad.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción de la habilidad.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de habilidad (ataque o defensa).
    /// </summary>
    public AbilityType AbilityType { get; set; } = AbilityType.Attack;

    /// <summary>
    /// Coste de maná para usar la habilidad.
    /// </summary>
    public int ManaCost { get; set; }

    /// <summary>
    /// Bonus de ataque mágico (se suma a la tirada de ataque).
    /// </summary>
    public int AttackValue { get; set; }

    /// <summary>
    /// Bonus de defensa mágica (se suma a la tirada de defensa).
    /// </summary>
    public int DefenseValue { get; set; }

    /// <summary>
    /// Daño base que causa (0 si no hace daño).
    /// </summary>
    public int Damage { get; set; }

    /// <summary>
    /// Curación que proporciona (0 si no cura).
    /// </summary>
    public int Healing { get; set; }

    /// <summary>
    /// Tipo de daño de la habilidad.
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Magical;

    /// <summary>
    /// Efecto de estado que aplica (null = ninguno).
    /// </summary>
    public string? StatusEffect { get; set; }

    /// <summary>
    /// Duración del efecto de estado en turnos.
    /// </summary>
    public int StatusEffectDuration { get; set; }

    /// <summary>
    /// Si la habilidad afecta al usuario en vez de al enemigo.
    /// </summary>
    public bool TargetsSelf { get; set; }
}
