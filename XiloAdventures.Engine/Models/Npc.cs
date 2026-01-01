using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un personaje no jugador (NPC) en el mundo del juego.
/// </summary>
public class Npc
{
    /// <summary>
    /// Identificador único del NPC.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del NPC que se muestra al jugador.
    /// </summary>
    public string Name { get; set; } = "NPC sin nombre";

    /// <summary>
    /// Descripción detallada del NPC.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Sala inicial donde aparece el NPC.
    /// </summary>
    public string? RoomId { get; set; }

    #region Shop Properties

    /// <summary>
    /// Si es true, el NPC es un comerciante con tienda.
    /// </summary>
    public bool IsShopkeeper { get; set; }

    /// <summary>
    /// Objetos que el NPC vende (si es comerciante), con cantidad disponible.
    /// </summary>
    public List<ShopItem> ShopInventory { get; set; } = new();

    /// <summary>
    /// Multiplicador de precio al comprar del jugador (ej: 0.5 = compra al 50%).
    /// </summary>
    public double BuyPriceMultiplier { get; set; } = 0.5;

    /// <summary>
    /// Multiplicador de precio al vender al jugador (ej: 1.0 = precio base).
    /// </summary>
    public double SellPriceMultiplier { get; set; } = 1.0;

    /// <summary>
    /// Dinero que lleva el NPC. Para comerciantes: -1 = dinero infinito, 0+ = cantidad limitada.
    /// </summary>
    public int Money { get; set; }

    #endregion

    #region Inventory and Equipment

    /// <summary>
    /// Inventario del NPC con cantidades.
    /// </summary>
    public List<InventoryItem> Inventory { get; set; } = new();

    /// <summary>
    /// ID del objeto equipado en la mano derecha (Arma o Armadura/escudo).
    /// </summary>
    public string? EquippedRightHandId { get; set; }

    /// <summary>
    /// ID del objeto equipado en la mano izquierda (Arma de 1 mano o Armadura/escudo).
    /// </summary>
    public string? EquippedLeftHandId { get; set; }

    /// <summary>
    /// ID del objeto equipado en el torso (solo Armadura).
    /// </summary>
    public string? EquippedTorsoId { get; set; }

    /// <summary>
    /// ID del objeto equipado en la cabeza (solo Casco).
    /// </summary>
    public string? EquippedHeadId { get; set; }

    #endregion

    #region Combat Properties

    /// <summary>
    /// Estadísticas de combate del NPC.
    /// </summary>
    public CombatStats Stats { get; set; } = new();

    /// <summary>
    /// IDs de habilidades de combate que el NPC puede usar.
    /// </summary>
    public List<string> AbilityIds { get; set; } = new();

    /// <summary>
    /// Permite al NPC usar ataques y defensas mágicas propias (habilidades).
    /// Los objetos mágicos funcionan independientemente.
    /// </summary>
    public bool MagicEnabled { get; set; } = false;

    /// <summary>
    /// Indica si el NPC está muerto (es un cadáver).
    /// Se puede examinar y saquear su inventario.
    /// </summary>
    public bool IsCorpse { get; set; } = false;

    #endregion

    #region Patrol Properties

    /// <summary>
    /// Lista ordenada de IDs de salas que forman la ruta de patrulla (modo ping-pong).
    /// </summary>
    public List<string> PatrolRoute { get; set; } = new();

    /// <summary>
    /// Modo de movimiento de patrulla: Turns = por turnos, Time = por tiempo real.
    /// </summary>
    public MovementMode PatrolMovementMode { get; set; } = MovementMode.Turns;

    /// <summary>
    /// Cada cuántos turnos del jugador se mueve el NPC (1 = cada turno, 3 = cada 3 turnos).
    /// Solo aplica en modo Turns.
    /// </summary>
    public int PatrolSpeed { get; set; } = 1;

    /// <summary>
    /// Intervalo en segundos entre movimientos de patrulla (3=Camina, 6=Lento, 10=Muy lento).
    /// Solo aplica en modo Time.
    /// </summary>
    public float PatrolTimeInterval { get; set; } = 3.0f;

    /// <summary>
    /// Si el NPC está patrullando activamente.
    /// </summary>
    public bool IsPatrolling { get; set; } = false;

    /// <summary>
    /// Índice actual en la ruta de patrulla (estado runtime, no se serializa).
    /// </summary>
    [JsonIgnore]
    public int PatrolRouteIndex { get; set; } = 0;

    /// <summary>
    /// Dirección de movimiento en la ruta (-1 o 1 para ping-pong).
    /// </summary>
    [JsonIgnore]
    public int PatrolDirection { get; set; } = 1;

    /// <summary>
    /// Contador de turnos para determinar cuándo mover (estado runtime).
    /// </summary>
    [JsonIgnore]
    public int PatrolTurnCounter { get; set; } = 0;

    /// <summary>
    /// Tiempo del último movimiento de patrulla (estado runtime).
    /// </summary>
    [JsonIgnore]
    public DateTime PatrolLastMoveTime { get; set; } = DateTime.MinValue;

    #endregion

    #region Follow Properties

    /// <summary>
    /// Si el NPC está siguiendo al jugador.
    /// </summary>
    public bool IsFollowingPlayer { get; set; } = false;

    /// <summary>
    /// Modo de movimiento de seguimiento: Turns = por turnos, Time = por tiempo real.
    /// </summary>
    public MovementMode FollowMovementMode { get; set; } = MovementMode.Turns;

    /// <summary>
    /// Velocidad de seguimiento: 1 = cada turno, 2 = cada 2 turnos, 3 = cada 3 turnos.
    /// Solo aplica en modo Turns.
    /// </summary>
    public int FollowSpeed { get; set; } = 1;

    /// <summary>
    /// Intervalo en segundos entre movimientos de seguimiento (3=Camina, 6=Lento, 10=Muy lento).
    /// Solo aplica en modo Time.
    /// </summary>
    public float FollowTimeInterval { get; set; } = 3.0f;

    /// <summary>
    /// Contador de movimientos del jugador para calcular seguimiento (estado runtime).
    /// </summary>
    [JsonIgnore]
    public int FollowMoveCounter { get; set; } = 0;

    /// <summary>
    /// Tiempo del último movimiento de seguimiento (estado runtime).
    /// </summary>
    [JsonIgnore]
    public DateTime FollowLastMoveTime { get; set; } = DateTime.MinValue;

    #endregion

    /// <summary>
    /// Controla si el jugador puede ver / interactuar con el NPC en la sala.
    /// </summary>
    public bool Visible { get; set; } = true;
}
