using System;
using System.Collections.Generic;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado de una sesión de comercio activa.
/// </summary>
public class TradeState
{
    /// <summary>Indica si hay comercio activo.</summary>
    public bool IsActive { get; set; }

    /// <summary>ID del NPC comerciante.</summary>
    public string NpcId { get; set; } = string.Empty;

    /// <summary>Nombre del NPC comerciante.</summary>
    public string NpcName { get; set; } = string.Empty;

    /// <summary>Items del NPC disponibles para comprar.</summary>
    public List<TradeItem> NpcItems { get; set; } = new();

    /// <summary>Items del jugador disponibles para vender.</summary>
    public List<TradeItem> PlayerItems { get; set; } = new();

    /// <summary>Oro del NPC (-1 = infinito).</summary>
    public int NpcGold { get; set; } = -1;

    /// <summary>Oro del jugador.</summary>
    public int PlayerGold { get; set; }

    /// <summary>Multiplicador de precio al comprar del jugador (ej: 0.5 = 50%).</summary>
    public double BuyMultiplier { get; set; } = 0.5;

    /// <summary>Multiplicador de precio al vender al jugador (ej: 1.0 = 100%).</summary>
    public double SellMultiplier { get; set; } = 1.0;

    /// <summary>Historial de transacciones.</summary>
    public List<TradeLogEntry> TradeLog { get; set; } = new();
}

/// <summary>
/// Objeto disponible para comerciar con información completa.
/// </summary>
public class TradeItem
{
    /// <summary>ID del objeto en el juego.</summary>
    public string ObjectId { get; set; } = string.Empty;

    /// <summary>Nombre del objeto.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripción del objeto.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Precio base del objeto.</summary>
    public int BasePrice { get; set; }

    /// <summary>Precio calculado con multiplicador aplicado.</summary>
    public int CalculatedPrice { get; set; }

    /// <summary>Cantidad disponible.</summary>
    public int Quantity { get; set; } = 1;

    /// <summary>Tipo del objeto.</summary>
    public ObjectType Type { get; set; } = ObjectType.Ninguno;

    /// <summary>Bonus de ataque (para armas).</summary>
    public int AttackBonus { get; set; }

    /// <summary>Bonus de defensa (para armaduras).</summary>
    public int DefenseBonus { get; set; }

    /// <summary>Salud que restaura (para consumibles).</summary>
    public int HealthRestore { get; set; }

    /// <summary>Mana que restaura (para consumibles mágicos).</summary>
    public int ManaRestore { get; set; }

    /// <summary>Si es un arma mágica.</summary>
    public bool IsMagicWeapon { get; set; }

    /// <summary>Descripción formateada con bonificaciones para mostrar en UI.</summary>
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

/// <summary>
/// Entrada en el registro de transacciones.
/// </summary>
public class TradeLogEntry
{
    /// <summary>Mensaje de la transacción.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Momento de la transacción.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Tipo de entrada para formateo visual.</summary>
    public TradeLogType LogType { get; set; } = TradeLogType.Info;
}

/// <summary>
/// Tipo de entrada en el log de comercio.
/// </summary>
public enum TradeLogType
{
    /// <summary>Compra realizada (jugador compra del NPC).</summary>
    Buy,
    /// <summary>Venta realizada (jugador vende al NPC).</summary>
    Sell,
    /// <summary>Información general.</summary>
    Info,
    /// <summary>Error en la transacción.</summary>
    Error
}

/// <summary>
/// Resultado de una transacción de comercio.
/// </summary>
public class TradeResult
{
    /// <summary>Si la transacción fue exitosa.</summary>
    public bool Success { get; set; }

    /// <summary>Mensaje descriptivo del resultado.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Cantidad de oro transferida.</summary>
    public int GoldTransferred { get; set; }

    /// <summary>Cantidad de items transferidos.</summary>
    public int ItemsTransferred { get; set; }
}
