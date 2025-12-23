namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de una tirada de dado D20.
/// </summary>
public class DiceRollResult
{
    /// <summary>
    /// Valor del dado (1-20).
    /// </summary>
    public int DiceValue { get; set; }

    /// <summary>
    /// Bonus de estadística (Fuerza, Destreza, etc. / 5).
    /// </summary>
    public int StatBonus { get; set; }

    /// <summary>
    /// Bonus de equipamiento (arma o armadura).
    /// </summary>
    public int EquipmentBonus { get; set; }

    /// <summary>
    /// Bonus adicional (defender, ventaja, etc.).
    /// </summary>
    public int AdditionalBonus { get; set; }

    /// <summary>
    /// Total final de la tirada.
    /// </summary>
    public int Total => DiceValue + StatBonus + EquipmentBonus + AdditionalBonus;

    /// <summary>
    /// Natural 20 - Golpe crítico (daño x2).
    /// </summary>
    public bool IsCritical => DiceValue == 20;

    /// <summary>
    /// Natural 1 - Fallo automático.
    /// </summary>
    public bool IsFumble => DiceValue == 1;

    /// <summary>
    /// Descripción del desglose de la tirada.
    /// </summary>
    public string Breakdown => $"{DiceValue} + {StatBonus} (estado) + {EquipmentBonus} (equipo)" +
        (AdditionalBonus != 0 ? $" + {AdditionalBonus} (bonus)" : "") +
        $" = {Total}";
}

/// <summary>
/// Resultado del cálculo de daño en combate.
/// </summary>
public class DamageResult
{
    /// <summary>
    /// Tirada de ataque del atacante.
    /// </summary>
    public DiceRollResult AttackRoll { get; set; } = new();

    /// <summary>
    /// Tirada de defensa del defensor.
    /// </summary>
    public DiceRollResult DefenseRoll { get; set; } = new();

    /// <summary>
    /// True si el ataque impactó.
    /// </summary>
    public bool Hit => !AttackRoll.IsFumble && (AttackRoll.IsCritical || AttackRoll.Total > DefenseRoll.Total);

    /// <summary>
    /// Daño base calculado.
    /// </summary>
    public int BaseDamage { get; set; }

    /// <summary>
    /// Daño final aplicado (tras críticos y reducciones).
    /// </summary>
    public int FinalDamage { get; set; }

    /// <summary>
    /// True si fue golpe crítico.
    /// </summary>
    public bool WasCritical => AttackRoll.IsCritical;

    /// <summary>
    /// True si fue fallo épico.
    /// </summary>
    public bool WasFumble => AttackRoll.IsFumble;
}
