using System.Collections.Generic;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Objeto disponible para comerciar con información completa para la UI.
/// Incluye el precio calculado, cantidad disponible y propiedades del objeto.
/// </summary>
/// <remarks>
/// TradeItem es una vista del objeto para el sistema de comercio,
/// que incluye toda la información necesaria para que el jugador
/// tome decisiones de compra/venta informadas.
/// </remarks>
public class TradeItem
{
    /// <summary>
    /// ID del objeto en el modelo del mundo.
    /// </summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del objeto para mostrar en la UI.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Descripción del objeto.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Precio base del objeto sin multiplicadores.
    /// </summary>
    public int BasePrice { get; set; }

    /// <summary>
    /// Precio final con el multiplicador correspondiente aplicado.
    /// Este es el precio que paga/recibe el jugador.
    /// </summary>
    public int CalculatedPrice { get; set; }

    /// <summary>
    /// Cantidad disponible del objeto.
    /// Para objetos apilables, puede ser mayor que 1.
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Tipo del objeto (Arma, Armadura, Consumible, etc.).
    /// </summary>
    public ObjectType Type { get; set; } = ObjectType.Ninguno;

    /// <summary>
    /// Bonus de ataque que proporciona el objeto (para armas).
    /// </summary>
    public int AttackBonus { get; set; }

    /// <summary>
    /// Bonus de defensa que proporciona el objeto (para armaduras).
    /// </summary>
    public int DefenseBonus { get; set; }

    /// <summary>
    /// Cantidad de salud que restaura al usar el objeto (para consumibles).
    /// </summary>
    public int HealthRestore { get; set; }

    /// <summary>
    /// Cantidad de maná que restaura al usar el objeto (para consumibles mágicos).
    /// </summary>
    public int ManaRestore { get; set; }

    /// <summary>
    /// Indica si el arma es mágica (ignora resistencias normales).
    /// </summary>
    public bool IsMagicWeapon { get; set; }

    /// <summary>
    /// Información formateada de las bonificaciones para mostrar en la UI.
    /// Incluye ATQ, DEF, HP, MP según corresponda.
    /// </summary>
    public string FormattedInfo
    {
        get
        {
            var parts = new List<string>();

            if (AttackBonus > 0)
                parts.Add($"ATQ:+{AttackBonus}");
            if (DefenseBonus > 0)
                parts.Add($"DEF:+{DefenseBonus}");
            if (HealthRestore > 0)
                parts.Add($"+{HealthRestore}HP");
            if (ManaRestore > 0)
                parts.Add($"+{ManaRestore}MP");
            if (IsMagicWeapon)
                parts.Add("Magica");

            return parts.Count > 0 ? string.Join(" | ", parts) : "";
        }
    }
}
