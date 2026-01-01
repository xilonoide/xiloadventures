namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Acción que un combatiente puede realizar en su turno.
/// </summary>
public enum CombatAction
{
    /// <summary>Sin acción seleccionada.</summary>
    None,
    /// <summary>Atacar al enemigo.</summary>
    Attack,
    /// <summary>Postura defensiva (+5 defensa este turno).</summary>
    Defend,
    /// <summary>Intentar huir del combate.</summary>
    Flee,
    /// <summary>Usar un objeto del inventario.</summary>
    UseItem,
    /// <summary>Usar una habilidad especial (consume maná).</summary>
    UseAbility
}
