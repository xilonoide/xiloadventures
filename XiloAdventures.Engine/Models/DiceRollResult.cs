namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de una tirada de dado D20.
/// Utilizado en el sistema de combate para calcular ataques, defensas y habilidades.
/// </summary>
public class DiceRollResult
{
    /// <summary>
    /// Valor base del dado (1-20).
    /// Este es el resultado "natural" de la tirada sin modificadores.
    /// </summary>
    public int DiceValue { get; set; }

    /// <summary>
    /// Bonus de estadística aplicable.
    /// Calculado como el valor de la estadística relevante dividido por 5.
    /// Ejemplo: Fuerza 15 = bonus +3.
    /// </summary>
    public int StatBonus { get; set; }

    /// <summary>
    /// Bonus de equipamiento (arma o armadura).
    /// Proviene del objeto equipado relevante para la acción.
    /// </summary>
    public int EquipmentBonus { get; set; }

    /// <summary>
    /// Bonus adicional por efectos temporales.
    /// Incluye: postura defensiva, ventaja táctica, buffs de habilidades, etc.
    /// </summary>
    public int AdditionalBonus { get; set; }

    /// <summary>
    /// Total final de la tirada.
    /// Suma de DiceValue + StatBonus + EquipmentBonus + AdditionalBonus.
    /// </summary>
    public int Total => DiceValue + StatBonus + EquipmentBonus + AdditionalBonus;

    /// <summary>
    /// Natural 20 - Golpe crítico automático.
    /// Un 20 natural siempre impacta y causa daño x2.
    /// </summary>
    public bool IsCritical => DiceValue == 20;

    /// <summary>
    /// Natural 1 - Fallo épico automático.
    /// Un 1 natural siempre falla, independientemente de los bonus.
    /// </summary>
    public bool IsFumble => DiceValue == 1;

    /// <summary>
    /// Descripción del desglose de la tirada para mostrar en el log de combate.
    /// Formato: "X + Y (estado) + Z (equipo) [+ W (bonus)] = Total".
    /// </summary>
    public string Breakdown => $"{DiceValue} + {StatBonus} (estado) + {EquipmentBonus} (equipo)" +
        (AdditionalBonus != 0 ? $" + {AdditionalBonus} (bonus)" : "") +
        $" = {Total}";
}
