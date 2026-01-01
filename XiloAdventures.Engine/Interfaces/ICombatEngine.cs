using System;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Interfaces;

/// <summary>
/// Interface for the combat system engine.
/// Handles turn-based D20-style combat between player and NPCs.
/// </summary>
public interface ICombatEngine
{
    /// <summary>
    /// Event fired when combat ends.
    /// </summary>
    event EventHandler<CombatEndEventArgs>? CombatEnded;

    /// <summary>
    /// Event fired when a new log entry is added.
    /// </summary>
    event EventHandler<CombatLogEntry>? LogEntryAdded;

    /// <summary>
    /// Starts a combat encounter with the specified NPC.
    /// </summary>
    /// <param name="npcId">The ID of the NPC to fight.</param>
    /// <returns>The initial combat state.</returns>
    CombatState StartCombat(string npcId);

    /// <summary>
    /// Resolves initiative rolls to determine turn order.
    /// </summary>
    /// <returns>True if player goes first, false if NPC goes first.</returns>
    bool ResolveInitiative();

    /// <summary>
    /// Sets the player's action for the current turn.
    /// </summary>
    /// <param name="action">The combat action to take.</param>
    /// <param name="itemOrAbilityId">Optional item or ability ID for UseItem/UseAbility actions.</param>
    void SetPlayerAction(CombatAction action, string? itemOrAbilityId = null);

    /// <summary>
    /// Executes the player's attack against the enemy.
    /// </summary>
    /// <param name="playerAttackDice">Optional fixed dice roll for testing.</param>
    /// <param name="npcDefenseDice">Optional fixed dice roll for testing.</param>
    /// <returns>Result of the damage calculation.</returns>
    DamageResult ExecutePlayerAttack(int? playerAttackDice = null, int? npcDefenseDice = null);

    /// <summary>
    /// Attempts to flee from combat.
    /// </summary>
    /// <returns>True if flee was successful.</returns>
    bool AttemptFlee();

    /// <summary>
    /// Executes the NPC's turn in combat.
    /// </summary>
    /// <param name="npcAttackDice">Optional fixed dice roll for testing.</param>
    /// <param name="playerDefenseDice">Optional fixed dice roll for testing.</param>
    /// <returns>Result of the damage calculation.</returns>
    DamageResult ExecuteNpcTurn(int? npcAttackDice = null, int? playerDefenseDice = null);

    /// <summary>
    /// Uses an item from the player's inventory during combat.
    /// </summary>
    /// <param name="objectId">The ID of the object to use.</param>
    /// <returns>True if the item was used successfully.</returns>
    bool UseItem(string objectId);

    /// <summary>
    /// Executes a magic attack ability.
    /// </summary>
    /// <param name="ability">The ability to use.</param>
    /// <param name="playerAttackDice">Optional fixed dice roll for testing.</param>
    /// <param name="npcDefenseDice">Optional fixed dice roll for testing.</param>
    /// <returns>Result of the damage calculation.</returns>
    DamageResult ExecuteMagicAttack(CombatAbility ability, int? playerAttackDice = null, int? npcDefenseDice = null);

    /// <summary>
    /// Executes a magic defense ability.
    /// </summary>
    /// <param name="ability">The ability to use.</param>
    /// <param name="npcAttackDice">Optional fixed dice roll for testing.</param>
    /// <param name="playerDefenseDice">Optional fixed dice roll for testing.</param>
    /// <returns>Result of the damage calculation.</returns>
    DamageResult ExecuteMagicDefense(CombatAbility ability, int? npcAttackDice = null, int? playerDefenseDice = null);

    /// <summary>
    /// Checks if the NPC can use magic attacks.
    /// </summary>
    /// <returns>True if NPC can use magic attacks.</returns>
    bool CanNpcUseMagicAttack();
}
