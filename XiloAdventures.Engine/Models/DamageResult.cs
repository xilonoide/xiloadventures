namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado del cálculo de daño en combate.
/// Encapsula las tiradas de ataque y defensa, y el daño resultante.
/// </summary>
public class DamageResult
{
    /// <summary>
    /// Tirada de ataque del atacante.
    /// Incluye todos los bonus aplicables (estadística, arma, habilidades).
    /// </summary>
    public DiceRollResult AttackRoll { get; set; } = new();

    /// <summary>
    /// Tirada de defensa del defensor.
    /// Incluye todos los bonus aplicables (estadística, armadura, postura defensiva).
    /// </summary>
    public DiceRollResult DefenseRoll { get; set; } = new();

    /// <summary>
    /// Determina si el ataque impactó al objetivo.
    /// True si: no fue fumble Y (fue crítico O ataque > defensa).
    /// </summary>
    public bool Hit => !AttackRoll.IsFumble && (AttackRoll.IsCritical || AttackRoll.Total > DefenseRoll.Total);

    /// <summary>
    /// Daño base calculado antes de modificadores.
    /// Basado en el arma del atacante y su estadística de daño.
    /// </summary>
    public int BaseDamage { get; set; }

    /// <summary>
    /// Daño final aplicado tras todos los modificadores.
    /// Incluye: multiplicador de crítico (x2), reducción de armadura, resistencias.
    /// </summary>
    public int FinalDamage { get; set; }

    /// <summary>
    /// Indica si fue golpe crítico (natural 20).
    /// Los críticos causan daño doble.
    /// </summary>
    public bool WasCritical => AttackRoll.IsCritical;

    /// <summary>
    /// Indica si fue fallo épico (natural 1).
    /// Los fumbles siempre fallan y pueden tener efectos negativos adicionales.
    /// </summary>
    public bool WasFumble => AttackRoll.IsFumble;
}
