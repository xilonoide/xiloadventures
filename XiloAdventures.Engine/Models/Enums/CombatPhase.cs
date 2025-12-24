namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Fase actual del combate por turnos.
/// </summary>
public enum CombatPhase
{
    /// <summary>Determinando orden de turnos (tirada de iniciativa).</summary>
    Initiative,
    /// <summary>Esperando acción del jugador.</summary>
    PlayerAction,
    /// <summary>Jugador tirando dados de ataque/defensa.</summary>
    PlayerRoll,
    /// <summary>NPC eligiendo acción.</summary>
    NpcAction,
    /// <summary>NPC tirando dados.</summary>
    NpcRoll,
    /// <summary>Resolviendo daño del turno.</summary>
    Resolution,
    /// <summary>Fin de ronda (verificar victoria/derrota).</summary>
    RoundEnd,
    /// <summary>Jugador ganó el combate.</summary>
    Victory,
    /// <summary>Jugador perdió el combate.</summary>
    Defeat
}
