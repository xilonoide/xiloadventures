using System;
using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado de un combate activo.
/// </summary>
public class CombatState
{
    /// <summary>
    /// Indica si hay combate activo.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// ID del NPC enemigo.
    /// </summary>
    public string EnemyNpcId { get; set; } = string.Empty;

    /// <summary>
    /// Fase actual del combate.
    /// </summary>
    public CombatPhase Phase { get; set; } = CombatPhase.Initiative;

    /// <summary>
    /// De quién es el turno actual (true = jugador, false = NPC).
    /// </summary>
    public bool IsPlayerTurn { get; set; }

    /// <summary>
    /// Número de ronda actual (empieza en 1).
    /// </summary>
    public int RoundNumber { get; set; } = 1;

    /// <summary>
    /// Acción seleccionada por el jugador para este turno.
    /// </summary>
    public CombatAction PlayerAction { get; set; } = CombatAction.None;

    /// <summary>
    /// ID del objeto seleccionado para usar (si PlayerAction = UseItem).
    /// </summary>
    public string? SelectedItemId { get; set; }

    /// <summary>
    /// ID de la habilidad seleccionada (si PlayerAction = UseAbility).
    /// </summary>
    public string? SelectedAbilityId { get; set; }

    /// <summary>
    /// Si el jugador está en postura defensiva este turno (+5 defensa).
    /// </summary>
    public bool PlayerDefending { get; set; }

    /// <summary>
    /// Resultado de la última tirada del jugador.
    /// </summary>
    public DiceRollResult? LastPlayerRoll { get; set; }

    /// <summary>
    /// Resultado de la última tirada del NPC.
    /// </summary>
    public DiceRollResult? LastNpcRoll { get; set; }

    /// <summary>
    /// Historial del combate para mostrar en UI.
    /// </summary>
    public List<CombatLogEntry> CombatLog { get; set; } = new();
}

/// <summary>
/// Entrada en el registro de combate.
/// </summary>
public class CombatLogEntry
{
    /// <summary>
    /// Mensaje a mostrar.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// True si la acción la realizó el jugador, false si fue del NPC.
    /// </summary>
    public bool IsPlayerAction { get; set; }

    /// <summary>
    /// Momento en que ocurrió.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Tipo de entrada para formateo visual.
    /// </summary>
    public CombatLogType LogType { get; set; } = CombatLogType.Normal;
}
