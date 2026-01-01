using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado de una sesión de comercio activa entre el jugador y un NPC.
/// Contiene toda la información necesaria para gestionar la transacción,
/// incluyendo inventarios, precios y el historial de operaciones.
/// </summary>
/// <remarks>
/// El sistema de comercio permite comprar y vender objetos con NPCs comerciantes.
/// Los precios se calculan aplicando multiplicadores al precio base de los objetos.
/// El NPC puede tener dinero limitado o infinito (-1).
/// </remarks>
public class TradeState
{
    /// <summary>
    /// Indica si hay una sesión de comercio activa.
    /// Solo puede haber una sesión activa a la vez.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// ID del NPC comerciante con quien se está negociando.
    /// </summary>
    public string NpcId { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del NPC comerciante para mostrar en la UI.
    /// </summary>
    public string NpcName { get; set; } = string.Empty;

    /// <summary>
    /// Items del inventario del NPC disponibles para que el jugador compre.
    /// Incluye precios calculados con el multiplicador de venta.
    /// </summary>
    public List<TradeItem> NpcItems { get; set; } = new();

    /// <summary>
    /// Items del inventario del jugador disponibles para vender.
    /// Incluye precios calculados con el multiplicador de compra.
    /// </summary>
    public List<TradeItem> PlayerItems { get; set; } = new();

    /// <summary>
    /// Cantidad de dinero que tiene el NPC.
    /// -1 indica dinero infinito (puede comprar cualquier cantidad).
    /// </summary>
    public int NpcMoney { get; set; } = -1;

    /// <summary>
    /// Cantidad de dinero actual del jugador.
    /// </summary>
    public int PlayerMoney { get; set; }

    /// <summary>
    /// Multiplicador aplicado al precio base cuando el jugador vende.
    /// Valor típico: 0.5 (el jugador recibe el 50% del precio base).
    /// </summary>
    public double BuyMultiplier { get; set; } = 0.5;

    /// <summary>
    /// Multiplicador aplicado al precio base cuando el jugador compra.
    /// Valor típico: 1.0 (el jugador paga el 100% del precio base).
    /// </summary>
    public double SellMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Historial de transacciones realizadas durante esta sesión.
    /// Útil para mostrar un registro de actividad al jugador.
    /// </summary>
    public List<TradeLogEntry> TradeLog { get; set; } = new();
}
