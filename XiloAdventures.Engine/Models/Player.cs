using System;
using System.Collections.Generic;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estadísticas del jugador durante la partida.
/// </summary>
public class PlayerStats
{
    /// <summary>
    /// Nombre del jugador.
    /// </summary>
    public string Name { get; set; } = "Aventurero";

    /// <summary>
    /// Fuerza del jugador (afecta daño físico y capacidad de carga).
    /// </summary>
    public int Strength { get; set; } = 20;

    /// <summary>
    /// Constitución del jugador (afecta salud y resistencia).
    /// </summary>
    public int Constitution { get; set; } = 20;

    /// <summary>
    /// Inteligencia del jugador (afecta daño mágico y maná).
    /// </summary>
    public int Intelligence { get; set; } = 20;

    /// <summary>
    /// Destreza del jugador (afecta precisión y evasión).
    /// </summary>
    public int Dexterity { get; set; } = 20;

    /// <summary>
    /// Carisma del jugador (afecta precios de comercio y diálogos).
    /// </summary>
    public int Charisma { get; set; } = 20;

    /// <summary>
    /// Dinero del jugador en monedas.
    /// </summary>
    public int Money { get; set; } = 0;

    #region Inventory Capacity

    /// <summary>
    /// Peso máximo que puede cargar el jugador en gramos. -1 = ilimitado.
    /// </summary>
    public int MaxInventoryWeight { get; set; } = -1;

    /// <summary>
    /// Volumen máximo del inventario en centímetros cúbicos (cm³). -1 = ilimitado.
    /// </summary>
    public double MaxInventoryVolume { get; set; } = -1;

    #endregion

    #region Equipment

    /// <summary>
    /// ID del arma equipada actualmente (null = sin arma).
    /// </summary>
    public string? EquippedWeaponId { get; set; }

    /// <summary>
    /// ID de la armadura equipada actualmente (null = sin armadura).
    /// </summary>
    public string? EquippedArmorId { get; set; }

    /// <summary>
    /// IDs de habilidades de combate que el jugador tiene actualmente.
    /// </summary>
    public List<string> AbilityIds { get; set; } = new();

    #endregion

    /// <summary>
    /// Estados dinámicos del jugador (salud, hambre, sed, energía, cordura).
    /// </summary>
    public PlayerDynamicStats DynamicStats { get; set; } = new();
}
