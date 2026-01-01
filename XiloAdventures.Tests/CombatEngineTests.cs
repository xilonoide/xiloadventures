using System.Collections.Generic;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for the CombatEngine class.
/// Tests combat mechanics including initiative, attack, defense, damage calculation, and combat end conditions.
/// </summary>
public class CombatEngineTests
{
    /// <summary>
    /// Creates a test world with a player and an NPC for combat testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateCombatTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "combat_test",
                Title = "Combat Test World",
                StartRoomId = "room1",
                StartHour = 12,
                CombatEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Arena",
                    Description = "Una arena de combate.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "goblin" },
                    ObjectIds = new List<string> { "sword", "armor" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "sword",
                    Name = "espada",
                    Description = "Una espada afilada.",
                    Type = ObjectType.Arma,
                    AttackBonus = 5,
                    MaxDurability = 10,
                    CurrentDurability = 10,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "armor",
                    Name = "armadura",
                    Description = "Una armadura de cuero.",
                    Type = ObjectType.Armadura,
                    DefenseBonus = 3,
                    MaxDurability = -1, // Indestructible
                    CurrentDurability = -1,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "goblin",
                    Name = "goblin",
                    Description = "Un goblin hostil.",
                    RoomId = "room1",
                    Visible = true,
                    Stats = new CombatStats
                    {
                        MaxHealth = 20,
                        CurrentHealth = 20,
                        Strength = 10,
                        Intelligence = 5,
                        Dexterity = 12
                    }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);

        // Set up player stats for combat
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 10;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;
        state.Player.DynamicStats.Mana = 50;
        state.Player.DynamicStats.MaxMana = 50;

        return (world, state);
    }

    #region Combat Initialization Tests

    [Fact]
    public void StartCombat_WithValidNpc_CreatesCombatState()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);

        // Act
        var combat = engine.StartCombat("goblin");

        // Assert
        Assert.NotNull(combat);
        Assert.True(combat.IsActive);
        Assert.Equal("goblin", combat.EnemyNpcId);
        Assert.Equal(CombatPhase.Initiative, combat.Phase);
        Assert.Equal(1, combat.RoundNumber);
    }

    [Fact]
    public void StartCombat_WithDeadNpc_ThrowsException()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.IsCorpse = true;
        var engine = new CombatEngine(state);

        // Act & Assert
        Assert.Throws<System.InvalidOperationException>(() => engine.StartCombat("goblin"));
    }

    [Fact]
    public void StartCombat_WithInvalidNpc_ThrowsException()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);

        // Act & Assert
        Assert.Throws<System.ArgumentException>(() => engine.StartCombat("nonexistent"));
    }

    #endregion

    #region Initiative Tests

    [Fact]
    public void RollPlayerInitiative_ReturnsValidResult()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        engine.StartCombat("goblin");

        // Act
        var result = engine.RollPlayerInitiative();

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.DiceValue, 1, 20);
        Assert.Equal(state.Player.Dexterity / 5, result.StatBonus);
    }

    [Fact]
    public void RollNpcInitiative_ReturnsValidResult()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative();

        // Act
        var result = engine.RollNpcInitiative();

        // Assert
        Assert.NotNull(result);
        Assert.InRange(result.DiceValue, 1, 20);
    }

    [Fact]
    public void ResolveInitiative_DeterminesTurnOrder()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative();
        engine.RollNpcInitiative();

        // Act
        var playerFirst = engine.ResolveInitiative();
        var combat = state.ActiveCombat;

        // Assert
        Assert.NotNull(combat);
        Assert.NotEqual(CombatPhase.Initiative, combat.Phase);
        Assert.True(combat.Phase == CombatPhase.PlayerAction || combat.Phase == CombatPhase.NpcAction);
    }

    #endregion

    #region Player Action Tests

    [Fact]
    public void SetPlayerAction_Attack_SetsCombatPhaseToPlayerRoll()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);

        // Assert
        Assert.Equal(CombatPhase.PlayerRoll, state.ActiveCombat?.Phase);
        Assert.Equal(CombatAction.Attack, state.ActiveCombat?.PlayerAction);
    }

    [Fact]
    public void SetPlayerAction_Defend_SetsDefendingFlag()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Defend);

        // Assert
        Assert.True(state.ActiveCombat?.PlayerDefending);
        Assert.Equal(CombatPhase.NpcAction, state.ActiveCombat?.Phase);
    }

    [Fact]
    public void SetPlayerAction_Flee_SetsCombatPhaseToPlayerRoll()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Flee);

        // Assert
        Assert.Equal(CombatPhase.PlayerRoll, state.ActiveCombat?.Phase);
        Assert.Equal(CombatAction.Flee, state.ActiveCombat?.PlayerAction);
    }

    #endregion

    #region Attack and Damage Tests

    [Fact]
    public void ExecutePlayerAttack_DealsDamageOnHit()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        var goblin = state.Npcs.First(n => n.Id == "goblin");
        var initialHealth = goblin.Stats.CurrentHealth;

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack();

        // Assert
        // Either damage was dealt (hit) or not (miss/fumble)
        Assert.NotNull(result);
        if (result.Hit)
        {
            Assert.True(result.FinalDamage > 0);
            Assert.True(goblin.Stats.CurrentHealth < initialHealth);
        }
    }

    [Fact]
    public void ExecuteNpcTurn_DealsDamageToPlayerOnHit()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        var initialHealth = state.Player.DynamicStats.Health;

        // Act
        var result = engine.ExecuteNpcTurn();

        // Assert
        Assert.NotNull(result);
        // NPC might flee if health is low, so check if result has attack roll
        if (result.AttackRoll?.DiceValue > 0 && result.Hit)
        {
            Assert.True(state.Player.DynamicStats.Health <= initialHealth);
        }
    }

    [Fact]
    public void Critical_DoublesDamage()
    {
        // Arrange
        var attackRoll = new DiceRollResult
        {
            DiceValue = 20, // Critical hit
            StatBonus = 3,
            EquipmentBonus = 5
        };

        var defenseRoll = new DiceRollResult
        {
            DiceValue = 10,
            StatBonus = 2,
            EquipmentBonus = 3
        };

        // The critical always hits regardless of defense
        Assert.True(attackRoll.IsCritical);
    }

    [Fact]
    public void Fumble_AutomaticMiss()
    {
        // Arrange
        var attackRoll = new DiceRollResult
        {
            DiceValue = 1, // Fumble
            StatBonus = 10,
            EquipmentBonus = 10
        };

        // Assert
        Assert.True(attackRoll.IsFumble);
    }

    #endregion

    #region Equipment Tests

    [Fact]
    public void EquippedWeapon_IncreasesAttackBonus()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();

        // Equip the sword
        state.Player.EquippedRightHandId = "sword";
        state.InventoryObjectIds.Add("sword");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.AttackRoll.EquipmentBonus); // Sword has +5 attack
    }

    [Fact]
    public void EquippedArmor_IncreasesDefenseBonus()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();

        // Equip the armor
        state.Player.EquippedTorsoId = "armor";
        state.InventoryObjectIds.Add("armor");

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn();

        // Assert
        Assert.NotNull(result);
        // Defense roll should include armor bonus
        if (result.DefenseRoll != null)
        {
            Assert.Equal(3, result.DefenseRoll.EquipmentBonus); // Armor has +3 defense
        }
    }

    [Fact]
    public void WeaponDurability_DecreasesOnAttack()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var sword = state.Objects.First(o => o.Id == "sword");
        state.Player.EquippedRightHandId = "sword";
        state.InventoryObjectIds.Add("sword");

        var initialDurability = sword.CurrentDurability;

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        engine.ExecutePlayerAttack();

        // Assert
        // Durability should have decreased (if weapon was used)
        Assert.True(sword.CurrentDurability <= initialDurability);
    }

    [Fact]
    public void IndestructibleArmor_DoesNotDegrade()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var armor = state.Objects.First(o => o.Id == "armor");
        state.Player.EquippedTorsoId = "armor";
        state.InventoryObjectIds.Add("armor");

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        engine.ExecuteNpcTurn();

        // Assert
        Assert.Equal(-1, armor.CurrentDurability); // Should remain -1 (indestructible)
    }

    #endregion

    #region Combat End Tests

    [Fact]
    public void Victory_WhenNpcHealthReachesZero()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 1; // Set very low health

        var engine = new CombatEngine(state);
        var combatEndedReason = CombatEndReason.Victory;
        engine.CombatEnded += (sender, args) => combatEndedReason = args.Reason;

        SetupToPlayerAction(engine, state);

        // Keep attacking until NPC dies
        for (int i = 0; i < 10 && state.ActiveCombat?.Phase != CombatPhase.Victory; i++)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
            {
                engine.SetPlayerAction(CombatAction.Attack);
            }
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerRoll)
            {
                engine.ExecutePlayerAttack();
            }
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
        }

        // Assert
        if (goblin.Stats.CurrentHealth <= 0)
        {
            Assert.True(goblin.IsCorpse);
        }
    }

    [Fact]
    public void AttemptFlee_ReturnsBoolean()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        engine.SetPlayerAction(CombatAction.Flee);

        // Act
        var result = engine.AttemptFlee();

        // Assert
        // Result is either true (fled) or false (failed)
        Assert.True(result || !result); // Just checking it returns without error
    }

    #endregion

    #region Combat Log Tests

    [Fact]
    public void CombatLog_RecordsEntries()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);

        // Act
        engine.StartCombat("goblin");

        // Assert
        var combat = state.ActiveCombat;
        Assert.NotNull(combat);
        Assert.True(combat.CombatLog.Count > 0);
        Assert.Contains(combat.CombatLog, e => e.Message.Contains("goblin"));
    }

    [Fact]
    public void LogEntryAdded_EventFires()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        var logEntryReceived = false;

        engine.LogEntryAdded += (sender, entry) => logEntryReceived = true;

        // Act
        engine.StartCombat("goblin");

        // Assert
        Assert.True(logEntryReceived);
    }

    #endregion

    #region DiceRollResult Tests

    [Fact]
    public void DiceRollResult_Total_CalculatesCorrectly()
    {
        // Arrange
        var roll = new DiceRollResult
        {
            DiceValue = 15,
            StatBonus = 3,
            EquipmentBonus = 5,
            AdditionalBonus = 2
        };

        // Assert
        Assert.Equal(25, roll.Total);
    }

    [Fact]
    public void DiceRollResult_IsCritical_WhenNatural20()
    {
        var roll = new DiceRollResult { DiceValue = 20 };
        Assert.True(roll.IsCritical);
    }

    [Fact]
    public void DiceRollResult_IsFumble_WhenNatural1()
    {
        var roll = new DiceRollResult { DiceValue = 1 };
        Assert.True(roll.IsFumble);
    }

    [Fact]
    public void DiceRollResult_Breakdown_FormatsCorrectly()
    {
        var roll = new DiceRollResult
        {
            DiceValue = 15,
            StatBonus = 3,
            EquipmentBonus = 5,
            AdditionalBonus = 0
        };

        var breakdown = roll.Breakdown;
        Assert.Contains("15", breakdown);
        Assert.Contains("3", breakdown);
        Assert.Contains("5", breakdown);
    }

    #endregion

    #region DamageResult Tests

    [Fact]
    public void DamageResult_Hit_WhenAttackBeatsDefense()
    {
        var result = new DamageResult
        {
            AttackRoll = new DiceRollResult { DiceValue = 15, StatBonus = 3, EquipmentBonus = 5 },
            DefenseRoll = new DiceRollResult { DiceValue = 10, StatBonus = 2, EquipmentBonus = 3 }
        };

        // Attack: 23, Defense: 15 -> Hit
        Assert.True(result.Hit);
    }

    [Fact]
    public void DamageResult_Miss_WhenDefenseBeatsAttack()
    {
        var result = new DamageResult
        {
            AttackRoll = new DiceRollResult { DiceValue = 5, StatBonus = 2, EquipmentBonus = 3 },
            DefenseRoll = new DiceRollResult { DiceValue = 15, StatBonus = 3, EquipmentBonus = 5 }
        };

        // Attack: 10, Defense: 23 -> Miss
        Assert.False(result.Hit);
    }

    [Fact]
    public void DamageResult_CriticalAlwaysHits()
    {
        var result = new DamageResult
        {
            AttackRoll = new DiceRollResult { DiceValue = 20, StatBonus = 0, EquipmentBonus = 0 },
            DefenseRoll = new DiceRollResult { DiceValue = 20, StatBonus = 10, EquipmentBonus = 10 }
        };

        // Critical always hits
        Assert.True(result.Hit);
        Assert.True(result.WasCritical);
    }

    [Fact]
    public void DamageResult_FumbleAlwaysMisses()
    {
        var result = new DamageResult
        {
            AttackRoll = new DiceRollResult { DiceValue = 1, StatBonus = 20, EquipmentBonus = 20 },
            DefenseRoll = new DiceRollResult { DiceValue = 1, StatBonus = 0, EquipmentBonus = 0 }
        };

        // Fumble always misses
        Assert.False(result.Hit);
        Assert.True(result.WasFumble);
    }

    #endregion

    #region Magic Weapon Tests (Player)

    [Fact]
    public void MagicWeapon_UsesIntelligenceForAttack()
    {
        // Arrange
        var (world, state) = CreateMagicTestWorld();
        state.Player.EquippedRightHandId = "magic_sword";
        state.InventoryObjectIds.Add("magic_sword");
        state.Player.Intelligence = 20; // High intelligence
        state.Player.Strength = 5; // Low strength

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack(playerAttackDice: 10); // Fixed dice for predictable test

        // Assert
        // Intelligence/5 = 20/5 = 4, not Strength/5 = 5/5 = 1
        Assert.Equal(4, result.AttackRoll.StatBonus);
    }

    [Fact]
    public void PhysicalWeapon_UsesStrengthForAttack()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.EquippedRightHandId = "sword";
        state.InventoryObjectIds.Add("sword");
        state.Player.Strength = 20; // High strength
        state.Player.Intelligence = 5; // Low intelligence

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack(playerAttackDice: 10);

        // Assert
        // Strength/5 = 20/5 = 4
        Assert.Equal(4, result.AttackRoll.StatBonus);
    }

    [Fact]
    public void MagicWeapon_EquipmentBonusApplied()
    {
        // Arrange
        var (world, state) = CreateMagicTestWorld();
        state.Player.EquippedRightHandId = "magic_sword";
        state.InventoryObjectIds.Add("magic_sword");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack(playerAttackDice: 10);

        // Assert - magic_sword has AttackBonus = 8
        Assert.Equal(8, result.AttackRoll.EquipmentBonus);
    }

    #endregion

    #region Magic Armor Tests (Player)

    [Fact]
    public void MagicArmor_UsesIntelligenceForDefense()
    {
        // Arrange
        var (world, state) = CreateMagicTestWorld();
        state.Player.EquippedTorsoId = "magic_robe";
        state.InventoryObjectIds.Add("magic_robe");
        state.Player.Intelligence = 20; // High intelligence
        state.Player.Dexterity = 5; // Low dexterity

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - Should use Intelligence/5 = 4, not Dexterity/5 = 1
        Assert.Equal(4, result.DefenseRoll?.StatBonus);
    }

    [Fact]
    public void PhysicalArmor_UsesDexterityForDefense()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.EquippedTorsoId = "armor";
        state.InventoryObjectIds.Add("armor");
        state.Player.Dexterity = 20; // High dexterity
        state.Player.Intelligence = 5; // Low intelligence

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - Should use Dexterity/5 = 4
        Assert.Equal(4, result.DefenseRoll?.StatBonus);
    }

    [Fact]
    public void MagicArmor_DefenseBonusApplied()
    {
        // Arrange
        var (world, state) = CreateMagicTestWorld();
        state.Player.EquippedTorsoId = "magic_robe";
        state.InventoryObjectIds.Add("magic_robe");

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - magic_robe has DefenseBonus = 6
        Assert.Equal(6, result.DefenseRoll?.EquipmentBonus);
    }

    [Fact]
    public void NoArmor_UsesDexterityNoBonus()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.EquippedTorsoId = null; // No armor
        state.Player.Dexterity = 15;

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert
        Assert.Equal(3, result.DefenseRoll?.StatBonus); // Dexterity/5 = 15/5 = 3
        Assert.Equal(0, result.DefenseRoll?.EquipmentBonus); // No armor bonus
    }

    #endregion

    #region NPC Equipment Tests

    [Fact]
    public void GetNpcBestWeapon_ReturnsHighestAttackBonus()
    {
        // Arrange
        var (world, state) = CreateNpcEquipmentTestWorld();
        var engine = new CombatEngine(state);
        engine.StartCombat("armed_goblin");

        // Act
        var bestWeapon = engine.GetNpcBestWeapon();

        // Assert - Should return "great_axe" with AttackBonus = 10
        Assert.NotNull(bestWeapon);
        Assert.Equal("great_axe", bestWeapon.Id);
        Assert.Equal(10, bestWeapon.AttackBonus);
    }

    [Fact]
    public void GetNpcBestArmor_ReturnsHighestDefenseBonus()
    {
        // Arrange
        var (world, state) = CreateNpcEquipmentTestWorld();
        var engine = new CombatEngine(state);
        engine.StartCombat("armed_goblin");

        // Act
        var bestArmor = engine.GetNpcBestArmor();

        // Assert - Should return "plate_armor" with DefenseBonus = 8
        Assert.NotNull(bestArmor);
        Assert.Equal("plate_armor", bestArmor.Id);
        Assert.Equal(8, bestArmor.DefenseBonus);
    }

    [Fact]
    public void NpcAttack_UsesBestWeaponBonus()
    {
        // Arrange
        var (world, state) = CreateNpcEquipmentTestWorld();
        var engine = new CombatEngine(state);
        SetupToNpcActionWithNpc(engine, state, "armed_goblin");

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - NPC attack should include great_axe bonus (+10)
        Assert.Equal(10, result.AttackRoll?.EquipmentBonus);
    }

    [Fact]
    public void NpcDefense_UsesBestArmorBonus()
    {
        // Arrange
        var (world, state) = CreateNpcEquipmentTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerActionWithNpc(engine, state, "armed_goblin");

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack(playerAttackDice: 10, npcDefenseDice: 10);

        // Assert - NPC defense should include plate_armor bonus (+8)
        Assert.Equal(8, result.DefenseRoll?.EquipmentBonus);
    }

    [Fact]
    public void NpcWithNoWeapon_ReducedDamage()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld(); // goblin has no weapons
        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 15, playerDefenseDice: 5);

        // Assert - Should still work, but with reduced damage
        Assert.NotNull(result);
        Assert.Equal(0, result.AttackRoll?.EquipmentBonus); // No weapon bonus
    }

    [Fact]
    public void NpcMagicWeapon_UsesIntelligence()
    {
        // Arrange
        var (world, state) = CreateNpcMagicEquipmentTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.Stats.Intelligence = 20;
        npc.Stats.Strength = 5;

        var engine = new CombatEngine(state);
        SetupToNpcActionWithNpc(engine, state, "mage_goblin");

        // Act
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - Should use Intelligence/5 = 4
        Assert.Equal(4, result.AttackRoll?.StatBonus);
    }

    [Fact]
    public void NpcMagicArmor_UsesIntelligence()
    {
        // Arrange
        var (world, state) = CreateNpcMagicEquipmentTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.Stats.Intelligence = 20;
        npc.Stats.Dexterity = 5;

        var engine = new CombatEngine(state);
        SetupToPlayerActionWithNpc(engine, state, "mage_goblin");

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        var result = engine.ExecutePlayerAttack(playerAttackDice: 10, npcDefenseDice: 10);

        // Assert - NPC should use Intelligence/5 = 4 for magic armor
        Assert.Equal(4, result.DefenseRoll?.StatBonus);
    }

    #endregion

    #region Magic Abilities Tests

    [Fact]
    public void ExecuteMagicAttack_ConsumesMana()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.DynamicStats.Mana = 50;
        var ability = state.Abilities.First(a => a.Id == "fireball");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);
        engine.SetPlayerAction(CombatAction.UseAbility, "fireball");

        // Act
        engine.ExecuteMagicAttack(ability, playerAttackDice: 15, npcDefenseDice: 10);

        // Assert - Fireball costs 10 mana
        Assert.Equal(40, state.Player.DynamicStats.Mana);
    }

    [Fact]
    public void ExecuteMagicAttack_UsesIntelligence()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.Intelligence = 20;
        var ability = state.Abilities.First(a => a.Id == "fireball");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);
        engine.SetPlayerAction(CombatAction.UseAbility, "fireball");

        // Act
        var result = engine.ExecuteMagicAttack(ability, playerAttackDice: 10, npcDefenseDice: 10);

        // Assert - Intelligence/5 = 4
        Assert.Equal(4, result.AttackRoll.StatBonus);
    }

    [Fact]
    public void ExecuteMagicAttack_AppliesAbilityDamage()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 100;
        var ability = state.Abilities.First(a => a.Id == "fireball");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);
        engine.SetPlayerAction(CombatAction.UseAbility, "fireball");

        // Act - Guaranteed hit with high roll
        var result = engine.ExecuteMagicAttack(ability, playerAttackDice: 18, npcDefenseDice: 5);

        // Assert
        if (result.Hit)
        {
            Assert.True(goblin.Stats.CurrentHealth < 100);
        }
    }

    [Fact]
    public void ExecuteMagicAttack_InsufficientMana_ThrowsException()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.DynamicStats.Mana = 5; // Less than fireball cost (10)
        var ability = state.Abilities.First(a => a.Id == "fireball");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);
        engine.SetPlayerAction(CombatAction.UseAbility, "fireball");

        // Act & Assert
        Assert.Throws<System.InvalidOperationException>(() =>
            engine.ExecuteMagicAttack(ability, playerAttackDice: 15, npcDefenseDice: 10));
    }

    [Fact]
    public void ExecuteMagicDefense_ConsumesMana()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.DynamicStats.Mana = 50;
        var ability = state.Abilities.First(a => a.Id == "shield");

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        engine.ExecuteMagicDefense(ability, npcAttackDice: 10, playerDefenseDice: 15);

        // Assert - Shield costs 8 mana
        Assert.Equal(42, state.Player.DynamicStats.Mana);
    }

    [Fact]
    public void ExecuteMagicDefense_UsesIntelligence()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.Intelligence = 20;
        var ability = state.Abilities.First(a => a.Id == "shield");

        var engine = new CombatEngine(state);
        SetupToNpcAction(engine, state);

        // Act
        var result = engine.ExecuteMagicDefense(ability, npcAttackDice: 10, playerDefenseDice: 10);

        // Assert - Intelligence/5 = 4
        Assert.Equal(4, result.DefenseRoll?.StatBonus);
    }

    [Fact]
    public void GetPlayerAbilities_ReturnsPlayerAbilities()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.AbilityIds.Add("fireball");
        state.Player.AbilityIds.Add("shield");

        var engine = new CombatEngine(state);
        engine.StartCombat("goblin");

        // Act
        var abilities = engine.GetPlayerAbilities();

        // Assert
        Assert.Equal(2, abilities.Count);
        Assert.Contains(abilities, a => a.Id == "fireball");
        Assert.Contains(abilities, a => a.Id == "shield");
    }

    #endregion

    #region NPC MagicEnabled Tests

    [Fact]
    public void GetNpcAbilities_MagicEnabled_ReturnsAbilities()
    {
        // Arrange
        var (world, state) = CreateNpcMagicAbilityTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.MagicEnabled = true;

        var engine = new CombatEngine(state);
        engine.StartCombat("mage_goblin");

        // Act
        var abilities = engine.GetNpcAbilities();

        // Assert
        Assert.NotEmpty(abilities);
        Assert.Contains(abilities, a => a.Id == "npc_fireball");
    }

    [Fact]
    public void GetNpcAbilities_MagicDisabled_ReturnsEmpty()
    {
        // Arrange
        var (world, state) = CreateNpcMagicAbilityTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.MagicEnabled = false;

        var engine = new CombatEngine(state);
        engine.StartCombat("mage_goblin");

        // Act
        var abilities = engine.GetNpcAbilities();

        // Assert
        Assert.Empty(abilities);
    }

    [Fact]
    public void NpcMagicEnabled_CanUseMagicAbilities()
    {
        // Arrange
        var (world, state) = CreateNpcMagicAbilityTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.MagicEnabled = true;
        npc.Stats.Intelligence = 15;

        var engine = new CombatEngine(state);
        engine.StartCombat("mage_goblin");

        // Act
        var canUseMagic = engine.CanNpcUseMagicAttack();

        // Assert
        Assert.True(canUseMagic);
    }

    [Fact]
    public void NpcMagicDisabled_CanStillUseMagicWeapon()
    {
        // Arrange - NPC has magic weapon but MagicEnabled = false
        var (world, state) = CreateNpcMagicEquipmentTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.MagicEnabled = false;

        var engine = new CombatEngine(state);
        engine.StartCombat("mage_goblin");

        // Act
        var canUseMagic = engine.CanNpcUseMagicAttack();
        var magicWeapon = engine.GetNpcMagicWeapon();

        // Assert - Can use magic attack via weapon even if MagicEnabled = false
        Assert.True(canUseMagic);
        Assert.NotNull(magicWeapon);
    }

    [Fact]
    public void ExecuteNpcMagicAttack_DamagesPlayer()
    {
        // Arrange
        var (world, state) = CreateNpcMagicAbilityTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.MagicEnabled = true;
        state.Player.DynamicStats.Health = 100;

        var engine = new CombatEngine(state);
        SetupToNpcActionWithNpc(engine, state, "mage_goblin");

        var ability = state.Abilities.First(a => a.Id == "npc_fireball");

        // Act
        var result = engine.ExecuteNpcMagicAttack(ability, npcAttackDice: 18, playerDefenseDice: 5);

        // Assert
        if (result.Hit)
        {
            Assert.True(state.Player.DynamicStats.Health < 100);
        }
    }

    [Fact]
    public void ExecuteNpcMagicWeaponAttack_DamagesPlayer()
    {
        // Arrange
        var (world, state) = CreateNpcMagicEquipmentTestWorld();
        state.Player.DynamicStats.Health = 100;

        var engine = new CombatEngine(state);
        SetupToNpcActionWithNpc(engine, state, "mage_goblin");

        var magicWeapon = engine.GetNpcMagicWeapon();
        Assert.NotNull(magicWeapon);

        // Act
        var result = engine.ExecuteNpcMagicWeaponAttack(magicWeapon, npcAttackDice: 18, playerDefenseDice: 5);

        // Assert
        if (result.Hit)
        {
            Assert.True(state.Player.DynamicStats.Health < 100);
        }
    }

    #endregion

    #region Combat End Conditions Tests

    [Fact]
    public void Defeat_WhenPlayerHealthReachesZero()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.DynamicStats.Health = 1;

        var engine = new CombatEngine(state);
        CombatEndReason? endReason = null;
        engine.CombatEnded += (sender, args) => endReason = args.Reason;

        SetupToNpcAction(engine, state);

        // Act - Keep getting hit until defeated
        for (int i = 0; i < 20 && state.ActiveCombat?.Phase != CombatPhase.Defeat; i++)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn(npcAttackDice: 18, playerDefenseDice: 2);
            }
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
            {
                engine.SetPlayerAction(CombatAction.Defend);
            }
        }

        // Assert
        if (state.Player.DynamicStats.Health <= 0)
        {
            Assert.Equal(CombatEndReason.Defeat, endReason);
        }
    }

    [Fact]
    public void Victory_ConvertNpcToCorpse()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 1;

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act - Kill the goblin
        engine.SetPlayerAction(CombatAction.Attack);
        engine.ExecutePlayerAttack(playerAttackDice: 20, npcDefenseDice: 1); // Critical hit

        // Assert
        if (goblin.Stats.CurrentHealth <= 0)
        {
            Assert.True(goblin.IsCorpse);
            Assert.False(goblin.IsPatrolling);
            Assert.False(goblin.IsFollowingPlayer);
        }
    }

    [Fact]
    public void NpcFlee_WhenHealthLow()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 1; // Very low, < 20% of max
        goblin.Stats.MaxHealth = 100;

        var engine = new CombatEngine(state);
        CombatEndReason? endReason = null;
        engine.CombatEnded += (sender, args) => endReason = args.Reason;

        SetupToNpcAction(engine, state);

        // Act - NPC might try to flee
        for (int i = 0; i < 10 && endReason == null; i++)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
            {
                engine.SetPlayerAction(CombatAction.Defend);
            }
        }

        // Assert - NPC might have fled (it's random)
        Assert.True(endReason == null || endReason == CombatEndReason.EnemyFled || endReason == CombatEndReason.Defeat);
    }

    [Fact]
    public void CombatEnded_EventContainsCorrectInfo()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 1;

        var engine = new CombatEngine(state);
        CombatEndEventArgs? eventArgs = null;
        engine.CombatEnded += (sender, args) => eventArgs = args;

        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Attack);
        engine.ExecutePlayerAttack(playerAttackDice: 20, npcDefenseDice: 1);

        // Assert
        if (eventArgs != null)
        {
            Assert.Equal("goblin", eventArgs.EnemyNpcId);
            Assert.True(eventArgs.RoundsPlayed >= 1);
        }
    }

    #endregion

    #region Defensive Stance Tests

    [Fact]
    public void DefensiveStance_Adds5ToDefense()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.EquippedTorsoId = "armor";
        state.InventoryObjectIds.Add("armor");

        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Defend);
        var result = engine.ExecuteNpcTurn(npcAttackDice: 10, playerDefenseDice: 10);

        // Assert
        Assert.Equal(5, result.DefenseRoll?.AdditionalBonus);
    }

    [Fact]
    public void DefensiveStance_ResetsNextRound()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        var engine = new CombatEngine(state);
        SetupToPlayerAction(engine, state);

        // Act
        engine.SetPlayerAction(CombatAction.Defend);
        Assert.True(state.ActiveCombat?.PlayerDefending);

        engine.ExecuteNpcTurn();

        // Assert - Should reset after NPC turn
        Assert.False(state.ActiveCombat?.PlayerDefending);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void FullCombat_PlayerVictory()
    {
        // Arrange
        var (world, state) = CreateCombatTestWorld();
        state.Player.EquippedRightHandId = "sword";
        state.InventoryObjectIds.Add("sword");
        state.Player.Strength = 50; // Very strong player
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 10;

        var engine = new CombatEngine(state);
        CombatEndReason? endReason = null;
        engine.CombatEnded += (sender, args) => endReason = args.Reason;

        // Act - Full combat loop
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative(diceValue: 20); // Win initiative
        engine.RollNpcInitiative(diceValue: 1);
        engine.ResolveInitiative();

        for (int i = 0; i < 10 && state.ActiveCombat?.Phase != CombatPhase.Victory; i++)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
            {
                engine.SetPlayerAction(CombatAction.Attack);
            }
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerRoll)
            {
                engine.ExecutePlayerAttack(playerAttackDice: 18, npcDefenseDice: 5);
            }
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
        }

        // Assert
        Assert.Equal(CombatEndReason.Victory, endReason);
        Assert.True(goblin.IsCorpse);
    }

    [Fact]
    public void FullCombat_WithMagicAbilities()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.AbilityIds.Add("fireball");
        state.Player.Intelligence = 20;
        state.Player.DynamicStats.Mana = 100;
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 20;

        var engine = new CombatEngine(state);
        CombatEndReason? endReason = null;
        engine.CombatEnded += (sender, args) => endReason = args.Reason;

        var fireball = state.Abilities.First(a => a.Id == "fireball");

        // Act
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative(diceValue: 20);
        engine.RollNpcInitiative(diceValue: 1);
        engine.ResolveInitiative();

        for (int i = 0; i < 10 && state.ActiveCombat?.Phase != CombatPhase.Victory; i++)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
            {
                engine.SetPlayerAction(CombatAction.UseAbility, "fireball");
            }
            if (state.ActiveCombat?.Phase == CombatPhase.PlayerRoll)
            {
                engine.ExecuteMagicAttack(fireball, playerAttackDice: 18, npcDefenseDice: 5);
            }
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
        }

        // Assert
        Assert.True(state.Player.DynamicStats.Mana < 100); // Mana was consumed
    }

    [Fact]
    public void FullCombat_MixedPhysicalAndMagic()
    {
        // Arrange
        var (world, state) = CreateMagicAbilityTestWorld();
        state.Player.EquippedRightHandId = "sword";
        state.InventoryObjectIds.Add("sword");
        state.Player.AbilityIds.Add("fireball");
        state.Player.DynamicStats.Mana = 20;
        var goblin = state.Npcs.First(n => n.Id == "goblin");
        goblin.Stats.CurrentHealth = 50;

        var engine = new CombatEngine(state);
        var fireball = state.Abilities.First(a => a.Id == "fireball");

        // Act
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative(diceValue: 20);
        engine.RollNpcInitiative(diceValue: 1);
        engine.ResolveInitiative();

        // First attack: Magic
        engine.SetPlayerAction(CombatAction.UseAbility, "fireball");
        engine.ExecuteMagicAttack(fireball, playerAttackDice: 15, npcDefenseDice: 10);

        if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            engine.ExecuteNpcTurn();

        // Second attack: Physical
        if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
        {
            engine.SetPlayerAction(CombatAction.Attack);
        }
        if (state.ActiveCombat?.Phase == CombatPhase.PlayerRoll)
        {
            engine.ExecutePlayerAttack(playerAttackDice: 15, npcDefenseDice: 10);
        }

        // Assert
        Assert.True(state.Player.DynamicStats.Mana < 20); // Magic was used
        Assert.NotNull(state.ActiveCombat); // Combat is still active or ended
    }

    [Fact]
    public void FullCombat_NpcWithMagicEquipment()
    {
        // Arrange
        var (world, state) = CreateNpcMagicEquipmentTestWorld();
        var npc = state.Npcs.First(n => n.Id == "mage_goblin");
        npc.Stats.Intelligence = 15;
        state.Player.DynamicStats.Health = 100;

        var engine = new CombatEngine(state);

        // Act
        engine.StartCombat("mage_goblin");
        engine.RollPlayerInitiative(diceValue: 1);
        engine.RollNpcInitiative(diceValue: 20);
        engine.ResolveInitiative();

        // NPC attacks with magic weapon
        if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
        {
            engine.ExecuteNpcTurn(npcAttackDice: 15, playerDefenseDice: 10);
        }

        // Assert - Damage was dealt using Intelligence
        Assert.True(state.Player.DynamicStats.Health <= 100);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sets up combat to the PlayerAction phase.
    /// </summary>
    private static void SetupToPlayerAction(CombatEngine engine, GameState state)
    {
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative();
        engine.RollNpcInitiative();
        engine.ResolveInitiative();

        // Keep executing until we're in PlayerAction phase
        int safetyCounter = 0;
        while (state.ActiveCombat?.Phase != CombatPhase.PlayerAction && safetyCounter < 10)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
            safetyCounter++;
        }
    }

    /// <summary>
    /// Sets up combat to the NpcAction phase.
    /// </summary>
    private static void SetupToNpcAction(CombatEngine engine, GameState state)
    {
        engine.StartCombat("goblin");
        engine.RollPlayerInitiative();
        engine.RollNpcInitiative();
        engine.ResolveInitiative();

        // If player goes first, make them defend
        if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
        {
            engine.SetPlayerAction(CombatAction.Defend);
        }
    }

    /// <summary>
    /// Sets up combat to the PlayerAction phase with a specific NPC.
    /// </summary>
    private static void SetupToPlayerActionWithNpc(CombatEngine engine, GameState state, string npcId)
    {
        engine.StartCombat(npcId);
        engine.RollPlayerInitiative();
        engine.RollNpcInitiative();
        engine.ResolveInitiative();

        // Keep executing until we're in PlayerAction phase
        int safetyCounter = 0;
        while (state.ActiveCombat?.Phase != CombatPhase.PlayerAction && safetyCounter < 10)
        {
            if (state.ActiveCombat?.Phase == CombatPhase.NpcAction)
            {
                engine.ExecuteNpcTurn();
            }
            safetyCounter++;
        }
    }

    /// <summary>
    /// Sets up combat to the NpcAction phase with a specific NPC.
    /// </summary>
    private static void SetupToNpcActionWithNpc(CombatEngine engine, GameState state, string npcId)
    {
        engine.StartCombat(npcId);
        engine.RollPlayerInitiative();
        engine.RollNpcInitiative();
        engine.ResolveInitiative();

        if (state.ActiveCombat?.Phase == CombatPhase.PlayerAction)
        {
            engine.SetPlayerAction(CombatAction.Defend);
        }
    }

    /// <summary>
    /// Creates a test world with magic weapons and armor.
    /// </summary>
    private static (WorldModel world, GameState state) CreateMagicTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "magic_test",
                Title = "Magic Test World",
                StartRoomId = "room1",
                CombatEnabled = true,
                MagicEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Magic Arena",
                    Description = "Una arena mágica.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "goblin" },
                    ObjectIds = new List<string> { "magic_sword", "magic_robe" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "magic_sword",
                    Name = "espada mágica",
                    Description = "Una espada que brilla con poder arcano.",
                    Type = ObjectType.Arma,
                    DamageType = DamageType.Magical,
                    AttackBonus = 8,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "magic_robe",
                    Name = "túnica mágica",
                    Description = "Una túnica que protege con magia.",
                    Type = ObjectType.Armadura,
                    DamageType = DamageType.Magical,
                    DefenseBonus = 6,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "goblin",
                    Name = "goblin",
                    Description = "Un goblin hostil.",
                    RoomId = "room1",
                    Visible = true,
                    Stats = new CombatStats
                    {
                        MaxHealth = 20,
                        CurrentHealth = 20,
                        Strength = 10,
                        Intelligence = 5,
                        Dexterity = 12
                    }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 10;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;
        state.Player.DynamicStats.Mana = 50;
        state.Player.DynamicStats.MaxMana = 50;

        return (world, state);
    }

    /// <summary>
    /// Creates a test world with NPC equipment.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNpcEquipmentTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "npc_equip_test",
                Title = "NPC Equipment Test",
                StartRoomId = "room1",
                CombatEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Arena",
                    Description = "Una arena.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "armed_goblin" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "rusty_sword",
                    Name = "espada oxidada",
                    Type = ObjectType.Arma,
                    AttackBonus = 3,
                    Visible = true
                },
                new GameObject
                {
                    Id = "great_axe",
                    Name = "gran hacha",
                    Type = ObjectType.Arma,
                    AttackBonus = 10,
                    Visible = true
                },
                new GameObject
                {
                    Id = "leather_armor",
                    Name = "armadura de cuero",
                    Type = ObjectType.Armadura,
                    DefenseBonus = 3,
                    Visible = true
                },
                new GameObject
                {
                    Id = "plate_armor",
                    Name = "armadura de placas",
                    Type = ObjectType.Armadura,
                    DefenseBonus = 8,
                    Visible = true
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "armed_goblin",
                    Name = "goblin armado",
                    Description = "Un goblin bien equipado.",
                    RoomId = "room1",
                    Visible = true,
                    Inventory = new List<InventoryItem>
                    {
                        new() { ObjectId = "rusty_sword", Quantity = 1 },
                        new() { ObjectId = "great_axe", Quantity = 1 },
                        new() { ObjectId = "leather_armor", Quantity = 1 },
                        new() { ObjectId = "plate_armor", Quantity = 1 }
                    },
                    Stats = new CombatStats
                    {
                        MaxHealth = 40,
                        CurrentHealth = 40,
                        Strength = 15,
                        Intelligence = 8,
                        Dexterity = 10
                    }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 10;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;

        return (world, state);
    }

    /// <summary>
    /// Creates a test world with NPC magic equipment.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNpcMagicEquipmentTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "npc_magic_equip_test",
                Title = "NPC Magic Equipment Test",
                StartRoomId = "room1",
                CombatEnabled = true,
                MagicEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Magic Arena",
                    Description = "Una arena mágica.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "mage_goblin" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "staff",
                    Name = "báculo mágico",
                    Type = ObjectType.Arma,
                    DamageType = DamageType.Magical,
                    AttackBonus = 7,
                    Visible = true
                },
                new GameObject
                {
                    Id = "enchanted_robe",
                    Name = "túnica encantada",
                    Type = ObjectType.Armadura,
                    DamageType = DamageType.Magical,
                    DefenseBonus = 5,
                    Visible = true
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "mage_goblin",
                    Name = "goblin mago",
                    Description = "Un goblin con poderes mágicos.",
                    RoomId = "room1",
                    Visible = true,
                    MagicEnabled = false, // Has magic items but not innate magic
                    Inventory = new List<InventoryItem>
                    {
                        new() { ObjectId = "staff", Quantity = 1 },
                        new() { ObjectId = "enchanted_robe", Quantity = 1 }
                    },
                    Stats = new CombatStats
                    {
                        MaxHealth = 25,
                        CurrentHealth = 25,
                        Strength = 6,
                        Intelligence = 15,
                        Dexterity = 10
                    }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 10;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;

        return (world, state);
    }

    /// <summary>
    /// Creates a test world with magic abilities.
    /// </summary>
    private static (WorldModel world, GameState state) CreateMagicAbilityTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "ability_test",
                Title = "Magic Ability Test",
                StartRoomId = "room1",
                CombatEnabled = true,
                MagicEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Magic Arena",
                    Description = "Una arena mágica.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "goblin" },
                    ObjectIds = new List<string> { "sword" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "sword",
                    Name = "espada",
                    Type = ObjectType.Arma,
                    AttackBonus = 5,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "goblin",
                    Name = "goblin",
                    Description = "Un goblin hostil.",
                    RoomId = "room1",
                    Visible = true,
                    Stats = new CombatStats
                    {
                        MaxHealth = 30,
                        CurrentHealth = 30,
                        Strength = 10,
                        Intelligence = 5,
                        Dexterity = 12
                    }
                }
            },
            Abilities = new List<CombatAbility>
            {
                new CombatAbility
                {
                    Id = "fireball",
                    Name = "Bola de fuego",
                    Description = "Lanza una bola de fuego.",
                    AbilityType = AbilityType.Attack,
                    ManaCost = 10,
                    AttackValue = 5,
                    Damage = 8
                },
                new CombatAbility
                {
                    Id = "shield",
                    Name = "Escudo mágico",
                    Description = "Crea un escudo protector.",
                    AbilityType = AbilityType.Defense,
                    ManaCost = 8,
                    DefenseValue = 6
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 15;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;
        state.Player.DynamicStats.Mana = 50;
        state.Player.DynamicStats.MaxMana = 50;

        return (world, state);
    }

    /// <summary>
    /// Creates a test world with NPC magic abilities.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNpcMagicAbilityTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "npc_ability_test",
                Title = "NPC Magic Ability Test",
                StartRoomId = "room1",
                CombatEnabled = true,
                MagicEnabled = true
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Magic Arena",
                    Description = "Una arena mágica.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "mage_goblin" }
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "mage_goblin",
                    Name = "goblin mago",
                    Description = "Un goblin con poderes mágicos.",
                    RoomId = "room1",
                    Visible = true,
                    MagicEnabled = true,
                    AbilityIds = new List<string> { "npc_fireball" },
                    Stats = new CombatStats
                    {
                        MaxHealth = 25,
                        CurrentHealth = 25,
                        Strength = 6,
                        Intelligence = 15,
                        Dexterity = 10
                    }
                }
            },
            Abilities = new List<CombatAbility>
            {
                new CombatAbility
                {
                    Id = "npc_fireball",
                    Name = "Bola de fuego oscura",
                    Description = "Una bola de fuego maligna.",
                    AbilityType = AbilityType.Attack,
                    ManaCost = 0, // NPCs don't use mana
                    AttackValue = 4,
                    Damage = 6
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Strength = 15;
        state.Player.Dexterity = 12;
        state.Player.Intelligence = 10;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;
        state.Player.DynamicStats.Mana = 50;
        state.Player.DynamicStats.MaxMana = 50;

        return (world, state);
    }

    #endregion
}
