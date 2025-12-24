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
    public int Gold { get; set; } = 0;

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

/// <summary>
/// Estados dinámicos del jugador que varían durante el juego.
/// Todos los valores van de 0 a 100 (porcentaje).
/// </summary>
public class PlayerDynamicStats
{
    /// <summary>
    /// Salud actual del jugador (0 = muerto, 100 = salud perfecta).
    /// </summary>
    public int Health { get; set; } = 100;

    /// <summary>
    /// Salud máxima del jugador.
    /// </summary>
    public int MaxHealth { get; set; } = 100;

    /// <summary>
    /// Nivel de hambre (0 = lleno, 100 = muriendo de hambre).
    /// </summary>
    public int Hunger { get; set; } = 0;

    /// <summary>
    /// Nivel de sed (0 = hidratado, 100 = deshidratado).
    /// </summary>
    public int Thirst { get; set; } = 0;

    /// <summary>
    /// Nivel de energía (0 = exhausto, 100 = lleno de energía).
    /// </summary>
    public int Energy { get; set; } = 100;

    /// <summary>
    /// Nivel de sueño/cansancio (0 = descansado, 100 = agotamiento extremo).
    /// </summary>
    public int Sleep { get; set; } = 0;

    /// <summary>
    /// Nivel de cordura/salud mental (0 = locura, 100 = mente sana).
    /// </summary>
    public int Sanity { get; set; } = 100;

    /// <summary>
    /// Nivel de mana/poder mágico (0 = sin magia, 100 = máximo poder).
    /// </summary>
    public int Mana { get; set; } = 100;

    /// <summary>
    /// Mana máximo del jugador.
    /// </summary>
    public int MaxMana { get; set; } = 100;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de hambre.
    /// </summary>
    internal double HungerAccumulator { get; set; } = 0;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de sed.
    /// </summary>
    internal double ThirstAccumulator { get; set; } = 0;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de sueño.
    /// </summary>
    internal double SleepAccumulator { get; set; } = 0;
}

/// <summary>
/// Definición del jugador configurable desde el editor de mundos.
/// Las características (Fuerza, Constitución, Inteligencia, Destreza, Carisma)
/// deben sumar 100 puntos en total, con un mínimo de 10 cada una.
/// </summary>
public class PlayerDefinition
{
    /// <summary>
    /// Nombre del jugador.
    /// </summary>
    public string Name { get; set; } = "Aventurero";

    /// <summary>
    /// Edad en años (10-90).
    /// </summary>
    public int Age { get; set; } = 25;

    /// <summary>
    /// Peso en kg (50-150, incrementos de 5).
    /// </summary>
    public int Weight { get; set; } = 70;

    /// <summary>
    /// Altura en cm (50-220, incrementos de 5).
    /// </summary>
    public int Height { get; set; } = 170;

    /// <summary>
    /// Fuerza (mínimo 10, máximo según puntos disponibles).
    /// </summary>
    public int Strength { get; set; } = 20;

    /// <summary>
    /// Constitución (mínimo 10, máximo según puntos disponibles).
    /// </summary>
    public int Constitution { get; set; } = 20;

    /// <summary>
    /// Inteligencia (mínimo 10, máximo según puntos disponibles).
    /// </summary>
    public int Intelligence { get; set; } = 20;

    /// <summary>
    /// Destreza (mínimo 10, máximo según puntos disponibles).
    /// </summary>
    public int Dexterity { get; set; } = 20;

    /// <summary>
    /// Carisma (mínimo 10, máximo según puntos disponibles).
    /// </summary>
    public int Charisma { get; set; } = 20;

    /// <summary>
    /// Dinero inicial en monedas (mínimo 0).
    /// </summary>
    public int InitialGold { get; set; } = 0;

    /// <summary>
    /// IDs de habilidades de combate que el jugador tiene al inicio.
    /// </summary>
    public List<string> AbilityIds { get; set; } = new();

    /// <summary>
    /// Calcula el total de puntos de características asignados.
    /// Debería ser siempre 100.
    /// </summary>
    [Browsable(false)]
    public int TotalAttributePoints => Strength + Constitution + Intelligence + Dexterity + Charisma;
}

/// <summary>
/// Modificador temporal aplicado a un estado del jugador.
/// </summary>
public class TemporaryModifier
{
    /// <summary>
    /// Identificador único del modificador.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre descriptivo del modificador (ej: "Veneno", "Bendición").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado del jugador que modifica.
    /// </summary>
    public PlayerStateType StateType { get; set; }

    /// <summary>
    /// Cantidad a modificar (positivo o negativo).
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Tipo de duración del modificador.
    /// </summary>
    public ModifierDurationType DurationType { get; set; }

    /// <summary>
    /// Duración restante (turnos o segundos según DurationType).
    /// </summary>
    public int RemainingDuration { get; set; }

    /// <summary>
    /// Momento en que se aplicó el modificador (para modo Seconds).
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Si es true, el modificador se aplica cada turno/segundo.
    /// Si es false, es un bonus/penalty estático.
    /// </summary>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Indica si el modificador ya expiró.
    /// </summary>
    [Browsable(false)]
    public bool IsExpired
    {
        get
        {
            if (DurationType == ModifierDurationType.Permanent)
                return false;
            if (DurationType == ModifierDurationType.Turns)
                return RemainingDuration <= 0;
            // Para Seconds, calculamos basado en tiempo transcurrido
            var elapsed = (DateTime.UtcNow - AppliedAt).TotalSeconds;
            return elapsed >= RemainingDuration;
        }
    }
}
