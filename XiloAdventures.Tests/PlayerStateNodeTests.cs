using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Tests for player state nodes including dynamic stats (Health, Hunger, etc.),
/// modifiers, and threshold events.
/// </summary>
public class PlayerStateNodeTests
{
    #region Test Helpers

    private static (WorldModel world, GameState state) CreateTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_player_states",
                Title = "Test Player States",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Room 1",
                    Description = "First room.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    private static ScriptDefinition CreateScript(
        NodeTypeId eventType,
        NodeTypeId actionType,
        Dictionary<string, object?>? actionProps = null)
    {
        return new ScriptDefinition
        {
            Id = $"script_{eventType}_{actionType}",
            OwnerType = "Game",
            OwnerId = "test_player_states",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_node",
                    NodeType = eventType,
                    Category = NodeCategory.Event,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "action_node",
                    NodeType = actionType,
                    Category = NodeCategory.Action,
                    Properties = actionProps ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_node",
                    FromPortName = "Exec",
                    ToNodeId = "action_node",
                    ToPortName = "Exec"
                }
            }
        };
    }

    private static ScriptDefinition CreateConditionScript(
        NodeTypeId conditionType,
        Dictionary<string, object?>? conditionProps = null)
    {
        return new ScriptDefinition
        {
            Id = $"script_condition_{conditionType}",
            OwnerType = "Game",
            OwnerId = "test_player_states",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_node",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "condition_node",
                    NodeType = conditionType,
                    Category = NodeCategory.Condition,
                    Properties = conditionProps ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "action_true",
                    NodeType = NodeTypeId.Action_SetFlag,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["FlagName"] = "condition_true",
                        ["Value"] = true
                    }
                },
                new ScriptNode
                {
                    Id = "action_false",
                    NodeType = NodeTypeId.Action_SetFlag,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["FlagName"] = "condition_false",
                        ["Value"] = true
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_node",
                    FromPortName = "Exec",
                    ToNodeId = "condition_node",
                    ToPortName = "Exec"
                },
                new NodeConnection
                {
                    FromNodeId = "condition_node",
                    FromPortName = "True",
                    ToNodeId = "action_true",
                    ToPortName = "Exec"
                },
                new NodeConnection
                {
                    FromNodeId = "condition_node",
                    FromPortName = "False",
                    ToNodeId = "action_false",
                    ToPortName = "Exec"
                }
            }
        };
    }

    #endregion

    #region Action_HealPlayer Tests

    [Fact]
    public async Task Action_HealPlayer_RestoresHealth()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 50;
        state.Player.DynamicStats.MaxHealth = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 30
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_HealPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(80, state.Player.DynamicStats.Health);
    }

    [Fact]
    public async Task Action_HealPlayer_DoesNotExceedMaxHealth()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 90;
        state.Player.DynamicStats.MaxHealth = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 50
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_HealPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(100, state.Player.DynamicStats.Health);
    }

    #endregion

    #region Action_DamagePlayer Tests

    [Fact]
    public async Task Action_DamagePlayer_ReducesHealth()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 25
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_DamagePlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(75, state.Player.DynamicStats.Health);
    }

    [Fact]
    public async Task Action_DamagePlayer_HealthCannotGoBelowZero()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 20;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 50
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_DamagePlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(0, state.Player.DynamicStats.Health);
    }

    [Fact]
    public async Task Action_DamagePlayer_TriggersDeathMessage_WhenHealthReachesZero()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 10;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 20
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_DamagePlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Contains("muerto", receivedMessage ?? "", StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Action_SetPlayerState Tests

    [Theory]
    [InlineData("Health", 75)]
    [InlineData("Hunger", 50)]
    [InlineData("Thirst", 40)]
    [InlineData("Energy", 80)]
    [InlineData("Sanity", 60)]
    [InlineData("Mana", 30)]
    public async Task Action_SetPlayerState_SetsCorrectValue(string stateType, int value)
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = stateType,
            ["Value"] = value
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_SetPlayerState, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        var actualValue = stateType switch
        {
            "Health" => state.Player.DynamicStats.Health,
            "Hunger" => state.Player.DynamicStats.Hunger,
            "Thirst" => state.Player.DynamicStats.Thirst,
            "Energy" => state.Player.DynamicStats.Energy,
            "Sanity" => state.Player.DynamicStats.Sanity,
            "Mana" => state.Player.DynamicStats.Mana,
            _ => 0
        };

        Assert.Equal(value, actualValue);
    }

    #endregion

    #region Action_ModifyPlayerState Tests

    [Fact]
    public async Task Action_ModifyPlayerState_AddsToValue()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Energy = 50;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = "Energy",
            ["Amount"] = 25
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ModifyPlayerState, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(75, state.Player.DynamicStats.Energy);
    }

    [Fact]
    public async Task Action_ModifyPlayerState_SubtractsFromValue()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Sanity = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = "Sanity",
            ["Amount"] = -30
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ModifyPlayerState, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(70, state.Player.DynamicStats.Sanity);
    }

    #endregion

    #region Action_FeedPlayer Tests

    [Fact]
    public async Task Action_FeedPlayer_ReducesHunger()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Hunger = 80;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 30
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_FeedPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(50, state.Player.DynamicStats.Hunger);
    }

    [Fact]
    public async Task Action_FeedPlayer_HungerCannotGoBelowZero()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Hunger = 20;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 50
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_FeedPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(0, state.Player.DynamicStats.Hunger);
    }

    #endregion

    #region Action_HydratePlayer Tests

    [Fact]
    public async Task Action_HydratePlayer_ReducesThirst()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Thirst = 70;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 40
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_HydratePlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(30, state.Player.DynamicStats.Thirst);
    }

    #endregion

    #region Action_RestPlayer Tests

    [Fact]
    public async Task Action_RestPlayer_RestoresEnergy()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Energy = 30;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 50
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RestPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(80, state.Player.DynamicStats.Energy);
    }

    #endregion

    #region Action_RestoreMana Tests

    [Fact]
    public async Task Action_RestoreMana_RestoresMana()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Mana = 20;
        state.Player.DynamicStats.MaxMana = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 30
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RestoreMana, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(50, state.Player.DynamicStats.Mana);
    }

    #endregion

    #region Action_ConsumeMana Tests

    [Fact]
    public async Task Action_ConsumeMana_ReducesMana()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Mana = 50;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 20
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ConsumeMana, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(30, state.Player.DynamicStats.Mana);
    }

    #endregion

    #region Action_RestoreAllStats Tests

    [Fact]
    public async Task Action_RestoreAllStats_RestoresAllStatsToOptimal()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 50;
        state.Player.DynamicStats.MaxHealth = 100;
        state.Player.DynamicStats.Mana = 30;
        state.Player.DynamicStats.MaxMana = 100;
        state.Player.DynamicStats.Hunger = 80;
        state.Player.DynamicStats.Thirst = 70;
        state.Player.DynamicStats.Energy = 20;
        state.Player.DynamicStats.Sanity = 40;

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RestoreAllStats));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(100, state.Player.DynamicStats.Health);
        Assert.Equal(100, state.Player.DynamicStats.Mana);
        Assert.Equal(0, state.Player.DynamicStats.Hunger);
        Assert.Equal(0, state.Player.DynamicStats.Thirst);
        Assert.Equal(100, state.Player.DynamicStats.Energy);
        Assert.Equal(100, state.Player.DynamicStats.Sanity);
    }

    #endregion

    #region Condition_PlayerStateAbove Tests

    [Fact]
    public async Task Condition_PlayerStateAbove_ReturnsTrueWhenAboveThreshold()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 80;

        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = "Health",
            ["Threshold"] = 50
        };

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_PlayerStateAbove, conditionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_true") && state.Flags["condition_true"]);
        Assert.False(state.Flags.ContainsKey("condition_false"));
    }

    [Fact]
    public async Task Condition_PlayerStateAbove_ReturnsFalseWhenBelowThreshold()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 30;

        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = "Health",
            ["Threshold"] = 50
        };

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_PlayerStateAbove, conditionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_false") && state.Flags["condition_false"]);
        Assert.False(state.Flags.ContainsKey("condition_true"));
    }

    #endregion

    #region Condition_PlayerStateBelow Tests

    [Fact]
    public async Task Condition_PlayerStateBelow_ReturnsTrueWhenBelowThreshold()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Energy = 20;

        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["StateType"] = "Energy",
            ["Threshold"] = 30
        };

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_PlayerStateBelow, conditionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_true") && state.Flags["condition_true"]);
    }

    #endregion

    #region Condition_IsPlayerAlive Tests

    [Fact]
    public async Task Condition_IsPlayerAlive_ReturnsTrueWhenHealthAboveZero()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 50;

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_IsPlayerAlive));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_true") && state.Flags["condition_true"]);
    }

    [Fact]
    public async Task Condition_IsPlayerAlive_ReturnsFalseWhenHealthIsZero()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 0;

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_IsPlayerAlive));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_false") && state.Flags["condition_false"]);
    }

    #endregion

    #region Modifier Tests

    [Fact]
    public async Task Action_ApplyModifier_AddsModifierToActiveModifiers()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModifierName"] = "Poison",
            ["StateType"] = "Health",
            ["Amount"] = -5,
            ["DurationType"] = "Turns",
            ["Duration"] = 3,
            ["IsRecurring"] = true
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ApplyModifier, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Single(state.ActiveModifiers);
        var modifier = state.ActiveModifiers[0];
        Assert.Equal("Poison", modifier.Name);
        Assert.Equal(PlayerStateType.Health, modifier.StateType);
        Assert.Equal(-5, modifier.Amount);
        Assert.Equal(ModifierDurationType.Turns, modifier.DurationType);
        Assert.Equal(3, modifier.RemainingDuration);
        Assert.True(modifier.IsRecurring);
    }

    [Fact]
    public async Task Action_RemoveModifier_RemovesModifierByName()
    {
        var (world, state) = CreateTestWorld();
        state.ActiveModifiers.Add(new TemporaryModifier
        {
            Name = "Blessing",
            StateType = PlayerStateType.Strength,
            Amount = 5,
            DurationType = ModifierDurationType.Permanent
        });

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModifierName"] = "Blessing"
        };

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RemoveModifier, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public async Task Action_RemoveAllModifiers_ClearsAllModifiers()
    {
        var (world, state) = CreateTestWorld();
        state.ActiveModifiers.Add(new TemporaryModifier { Name = "Mod1", StateType = PlayerStateType.Health });
        state.ActiveModifiers.Add(new TemporaryModifier { Name = "Mod2", StateType = PlayerStateType.Energy });
        state.ActiveModifiers.Add(new TemporaryModifier { Name = "Mod3", StateType = PlayerStateType.Sanity });

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RemoveAllModifiers));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public async Task Action_ProcessModifiers_AppliesRecurringEffects()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Health = 100;
        state.ActiveModifiers.Add(new TemporaryModifier
        {
            Name = "Poison",
            StateType = PlayerStateType.Health,
            Amount = -10,
            DurationType = ModifierDurationType.Turns,
            RemainingDuration = 3,
            IsRecurring = true
        });

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ProcessModifiers));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Equal(90, state.Player.DynamicStats.Health);
        Assert.Equal(2, state.ActiveModifiers[0].RemainingDuration);
    }

    [Fact]
    public async Task Action_ProcessModifiers_RemovesExpiredModifiers()
    {
        var (world, state) = CreateTestWorld();
        state.ActiveModifiers.Add(new TemporaryModifier
        {
            Name = "ExpiredMod",
            StateType = PlayerStateType.Health,
            Amount = 5,
            DurationType = ModifierDurationType.Turns,
            RemainingDuration = 0, // Already expired
            IsRecurring = true
        });

        world.Scripts.Add(CreateScript(NodeTypeId.Event_OnGameStart, NodeTypeId.Action_ProcessModifiers));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.Empty(state.ActiveModifiers);
    }

    [Fact]
    public async Task Condition_HasModifier_ReturnsTrueWhenModifierExists()
    {
        var (world, state) = CreateTestWorld();
        state.ActiveModifiers.Add(new TemporaryModifier
        {
            Name = "Shield",
            StateType = PlayerStateType.Constitution,
            DurationType = ModifierDurationType.Permanent
        });

        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModifierName"] = "Shield"
        };

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_HasModifier, conditionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_true") && state.Flags["condition_true"]);
    }

    [Fact]
    public async Task Condition_HasModifier_ReturnsFalseWhenModifierDoesNotExist()
    {
        var (world, state) = CreateTestWorld();

        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ModifierName"] = "NonExistent"
        };

        world.Scripts.Add(CreateConditionScript(NodeTypeId.Condition_HasModifier, conditionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_player_states", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("condition_false") && state.Flags["condition_false"]);
    }

    #endregion

    #region Modifier Duration Type Tests

    [Fact]
    public async Task TemporaryModifier_TurnsBased_ExpiresAfterDurationReachesZero()
    {
        var modifier = new TemporaryModifier
        {
            Name = "Test",
            DurationType = ModifierDurationType.Turns,
            RemainingDuration = 0
        };

        Assert.True(modifier.IsExpired);
    }

    [Fact]
    public void TemporaryModifier_Permanent_NeverExpires()
    {
        var modifier = new TemporaryModifier
        {
            Name = "Permanent",
            DurationType = ModifierDurationType.Permanent,
            RemainingDuration = 0 // Even with 0 duration
        };

        Assert.False(modifier.IsExpired);
    }

    [Fact]
    public void TemporaryModifier_SecondsBased_ExpiresAfterTimeElapsed()
    {
        var modifier = new TemporaryModifier
        {
            Name = "Timed",
            DurationType = ModifierDurationType.Seconds,
            RemainingDuration = 1, // 1 second
            AppliedAt = DateTime.UtcNow.AddSeconds(-2) // Applied 2 seconds ago
        };

        Assert.True(modifier.IsExpired);
    }

    [Fact]
    public void TemporaryModifier_SecondsBased_NotExpiredBeforeTimeElapsed()
    {
        var modifier = new TemporaryModifier
        {
            Name = "Timed",
            DurationType = ModifierDurationType.Seconds,
            RemainingDuration = 60, // 60 seconds
            AppliedAt = DateTime.UtcNow // Just applied
        };

        Assert.False(modifier.IsExpired);
    }

    #endregion

    #region PlayerDynamicStats Default Values Tests

    [Fact]
    public void PlayerDynamicStats_DefaultValues_AreCorrect()
    {
        var stats = new PlayerDynamicStats();

        Assert.Equal(100, stats.Health);
        Assert.Equal(100, stats.MaxHealth);
        Assert.Equal(0, stats.Hunger);
        Assert.Equal(0, stats.Thirst);
        Assert.Equal(100, stats.Energy);
        Assert.Equal(100, stats.Sanity);
        Assert.Equal(100, stats.Mana);
        Assert.Equal(100, stats.MaxMana);
    }

    #endregion
}
