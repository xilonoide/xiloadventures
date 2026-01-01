using System.Collections.Generic;
using System.ComponentModel;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Definición del jugador configurable desde el editor de mundos.
/// Establece las características iniciales del jugador al comenzar una nueva partida.
/// </summary>
/// <remarks>
/// Las características (Fuerza, Constitución, Inteligencia, Destreza, Carisma)
/// deben sumar 100 puntos en total, con un mínimo de 10 cada una.
/// Esto garantiza un balance entre personajes, permitiendo especialización sin
/// hacer ningún atributo completamente inútil.
/// </remarks>
public class PlayerDefinition
{
    /// <summary>
    /// Nombre del jugador.
    /// Puede ser modificado por el usuario al iniciar una partida.
    /// </summary>
    public string Name { get; set; } = "Aventurero";

    /// <summary>
    /// Edad en años (rango válido: 10-90).
    /// Afecta algunos diálogos y puede ser usado en condiciones de scripts.
    /// </summary>
    public int Age { get; set; } = 25;

    /// <summary>
    /// Peso en kg (rango válido: 50-150, incrementos de 5).
    /// Puede afectar la velocidad de movimiento o capacidad de carga.
    /// </summary>
    public int Weight { get; set; } = 70;

    /// <summary>
    /// Altura en cm (rango válido: 50-220, incrementos de 5).
    /// Puede afectar accesibilidad a ciertas áreas o diálogos.
    /// </summary>
    public int Height { get; set; } = 170;

    /// <summary>
    /// Fuerza (mínimo 10, máximo según puntos disponibles).
    /// Afecta: daño físico cuerpo a cuerpo, capacidad de carga.
    /// Bonus de combate = Strength / 5.
    /// </summary>
    public int Strength { get; set; } = 20;

    /// <summary>
    /// Constitución (mínimo 10, máximo según puntos disponibles).
    /// Afecta: salud máxima, resistencia a efectos negativos, recuperación.
    /// Bonus de salud = Constitution * 2.
    /// </summary>
    public int Constitution { get; set; } = 20;

    /// <summary>
    /// Inteligencia (mínimo 10, máximo según puntos disponibles).
    /// Afecta: daño mágico, mana máximo, resolución de puzzles.
    /// Bonus de magia = Intelligence / 5.
    /// </summary>
    public int Intelligence { get; set; } = 20;

    /// <summary>
    /// Destreza (mínimo 10, máximo según puntos disponibles).
    /// Afecta: precisión de ataques, evasión, velocidad en combate.
    /// Bonus de ataque/defensa = Dexterity / 5.
    /// </summary>
    public int Dexterity { get; set; } = 20;

    /// <summary>
    /// Carisma (mínimo 10, máximo según puntos disponibles).
    /// Afecta: precios de comercio, opciones de diálogo, persuasión.
    /// Descuento = Charisma / 10 %.
    /// </summary>
    public int Charisma { get; set; } = 20;

    /// <summary>
    /// Dinero inicial en monedas (mínimo 0).
    /// El jugador comienza la partida con esta cantidad.
    /// </summary>
    public int InitialMoney { get; set; } = 0;

    /// <summary>
    /// Peso máximo que puede cargar el jugador en gramos.
    /// -1 = ilimitado (sin restricción de peso).
    /// </summary>
    public int MaxInventoryWeight { get; set; } = -1;

    /// <summary>
    /// Volumen máximo del inventario en centímetros cúbicos (cm³).
    /// -1 = ilimitado (sin restricción de volumen).
    /// </summary>
    public double MaxInventoryVolume { get; set; } = -1;

    /// <summary>
    /// IDs de habilidades de combate que el jugador tiene al inicio.
    /// Permite comenzar con habilidades especiales ya desbloqueadas.
    /// </summary>
    public List<string> AbilityIds { get; set; } = new();

    #region Initial Equipment and Inventory

    /// <summary>
    /// Inventario inicial del jugador con cantidades.
    /// Los objetos se añaden según la cantidad al iniciar partida.
    /// </summary>
    public List<InventoryItem> InitialInventory { get; set; } = new();

    /// <summary>
    /// ID del objeto equipado inicialmente en la mano derecha (Arma o Armadura/escudo).
    /// </summary>
    public string? InitialRightHandId { get; set; }

    /// <summary>
    /// ID del objeto equipado inicialmente en la mano izquierda (Arma de 1 mano o Armadura/escudo).
    /// </summary>
    public string? InitialLeftHandId { get; set; }

    /// <summary>
    /// ID del objeto equipado inicialmente en el torso (solo Armadura).
    /// </summary>
    public string? InitialTorsoId { get; set; }

    /// <summary>
    /// ID del objeto equipado inicialmente en la cabeza (solo Casco).
    /// </summary>
    public string? InitialHeadId { get; set; }

    #endregion

    /// <summary>
    /// Calcula el total de puntos de características asignados.
    /// Debería ser siempre 100 para un personaje válido.
    /// </summary>
    [Browsable(false)]
    public int TotalAttributePoints => Strength + Constitution + Intelligence + Dexterity + Charisma;
}
