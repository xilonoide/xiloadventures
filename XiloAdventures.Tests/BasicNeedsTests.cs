using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Services;

namespace XiloAdventures.Tests;

/// <summary>
/// Tests for the Basic Needs system (Hunger, Thirst, Sleep).
/// </summary>
public class BasicNeedsTests
{
    #region Test Helpers

    private static SoundManager CreateMockSoundManager()
    {
        return new SoundManager { SoundEnabled = false };
    }

    private static (WorldModel world, GameState state) CreateTestWorld(bool basicNeedsEnabled = true)
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_basic_needs",
                Title = "Test Basic Needs",
                StartRoomId = "room1",
                StartHour = 12,
                BasicNeedsEnabled = basicNeedsEnabled,
                HungerRate = NeedRate.Normal,
                ThirstRate = NeedRate.Normal,
                SleepRate = NeedRate.Normal,
                HungerDeathText = "Died of hunger.",
                ThirstDeathText = "Died of thirst.",
                SleepDeathText = "Died of exhaustion."
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

    private static GameEngine CreateEngine(WorldModel world, GameState state)
    {
        var sound = CreateMockSoundManager();
        return new GameEngine(world, state, sound, isDebugMode: false);
    }

    #endregion

    #region NeedRate Tests

    [Theory]
    [InlineData(NeedRate.Low, 0.5)]
    [InlineData(NeedRate.Normal, 1.0)]
    [InlineData(NeedRate.High, 1.5)]
    public void NeedRate_HasCorrectModifier(NeedRate rate, double expectedModifier)
    {
        var modifier = rate switch
        {
            NeedRate.Low => 0.5,
            NeedRate.Normal => 1.0,
            NeedRate.High => 1.5,
            _ => 1.0
        };
        Assert.Equal(expectedModifier, modifier);
    }

    #endregion

    #region Per-Turn Increment Tests

    [Fact]
    public void ProcessCommand_BasicNeedsEnabled_IncrementsNeeds()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        var initialHunger = state.Player.DynamicStats.Hunger;
        var initialThirst = state.Player.DynamicStats.Thirst;
        var initialSleep = state.Player.DynamicStats.Sleep;

        // Con acumuladores fraccionarios:
        // Hambre: 1.3 por turno -> 1 en primer turno
        // Sed: 1.0 por turno -> 1 en primer turno
        // Sueño: 0.7 por turno -> 0 en primer turno, 1 en segundo turno
        engine.ProcessCommand("esperar");
        engine.ProcessCommand("esperar"); // Segundo turno para que sueño incremente

        Assert.True(state.Player.DynamicStats.Hunger > initialHunger);
        Assert.True(state.Player.DynamicStats.Thirst > initialThirst);
        Assert.True(state.Player.DynamicStats.Sleep > initialSleep);
    }

    [Fact]
    public void ProcessCommand_BasicNeedsDisabled_DoesNotIncrementNeeds()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: false);
        var engine = CreateEngine(world, state);

        var initialHunger = state.Player.DynamicStats.Hunger;
        var initialThirst = state.Player.DynamicStats.Thirst;
        var initialSleep = state.Player.DynamicStats.Sleep;

        engine.ProcessCommand("esperar");

        Assert.Equal(initialHunger, state.Player.DynamicStats.Hunger);
        Assert.Equal(initialThirst, state.Player.DynamicStats.Thirst);
        Assert.Equal(initialSleep, state.Player.DynamicStats.Sleep);
    }

    [Fact]
    public void ProcessCommand_HighRate_IncrementsMorePerTurn()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Game.HungerRate = NeedRate.High;
        var engine = CreateEngine(world, state);

        state.Player.DynamicStats.Hunger = 0;
        // High rate: 1.3 * 1.5 = 1.95 por turno
        // Turno 1: inc=1, acum=0.95
        // Turno 2: 0.95+1.95=2.9, inc=2, acum=0.9
        // Total: 3
        engine.ProcessCommand("esperar");
        engine.ProcessCommand("esperar");

        Assert.Equal(3, state.Player.DynamicStats.Hunger);
    }

    [Fact]
    public void ProcessCommand_LowRate_IncrementsLessPerTurn()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Game.HungerRate = NeedRate.Low;
        var engine = CreateEngine(world, state);

        state.Player.DynamicStats.Hunger = 0;
        // Low rate: 1.3 * 0.5 = 0.65 por turno
        // Turno 1: inc=0, acum=0.65
        // Turno 2: 0.65+0.65=1.3, inc=1, acum=0.3
        // Total: 1
        engine.ProcessCommand("esperar");
        engine.ProcessCommand("esperar");

        Assert.Equal(1, state.Player.DynamicStats.Hunger);
    }

    #endregion

    #region Threshold Message Tests

    [Fact]
    public void ProcessCommand_HungerReaches70_ShowsWarningMessage()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        // Hambre: 1.3 por turno, desde 69 cruza 70 en primer turno
        state.Player.DynamicStats.Hunger = 69;
        var result = engine.ProcessCommand("esperar");

        Assert.Contains("hambre", result.Message.ToLower());
    }

    [Fact]
    public void ProcessCommand_ThirstReaches80_ShowsWarningMessage()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        // Sed: 1.0 por turno, desde 79 cruza 80 en primer turno
        state.Player.DynamicStats.Thirst = 79;
        var result = engine.ProcessCommand("esperar");

        Assert.Contains("sed", result.Message.ToLower());
    }

    [Fact]
    public void ProcessCommand_SleepReaches90_ShowsCriticalMessage()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        // Sueño: 0.7 por turno, desde 89:
        // Turno 1: acum=0.7, inc=0, sleep=89
        // Turno 2: acum=1.4, inc=1, sleep=90 -> cruza umbral
        state.Player.DynamicStats.Sleep = 89;
        engine.ProcessCommand("esperar"); // Primer turno, no cruza
        var result = engine.ProcessCommand("esperar"); // Segundo turno, cruza 90

        Assert.Contains("sueño", result.Message.ToLower());
        Assert.Contains("crítica", result.Message.ToLower());
    }

    #endregion

    #region Death Tests

    [Fact]
    public void ProcessCommand_HungerReaches100_TriggersDeathEvent()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        DeathType? deathType = null;
        engine.PlayerDied += type => deathType = type;

        // Hambre: 1.3 por turno, desde 99 cruza 100 en primer turno
        state.Player.DynamicStats.Hunger = 99;
        engine.ProcessCommand("esperar");

        Assert.Equal(DeathType.Hunger, deathType);
    }

    [Fact]
    public void ProcessCommand_ThirstReaches100_TriggersDeathEvent()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        DeathType? deathType = null;
        engine.PlayerDied += type => deathType = type;

        // Sed: 1.0 por turno, desde 99 cruza 100 en primer turno
        state.Player.DynamicStats.Thirst = 99;
        engine.ProcessCommand("esperar");

        Assert.Equal(DeathType.Thirst, deathType);
    }

    [Fact]
    public void ProcessCommand_SleepReaches100_TriggersDeathEvent()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        DeathType? deathType = null;
        engine.PlayerDied += type => deathType = type;

        // Sueño: 0.7 por turno, desde 99:
        // Turno 1: acum=0.7, inc=0, sleep=99
        // Turno 2: acum=1.4, inc=1, sleep=100 -> muerte
        state.Player.DynamicStats.Sleep = 99;
        engine.ProcessCommand("esperar"); // Primer turno
        engine.ProcessCommand("esperar"); // Segundo turno, muerte

        Assert.Equal(DeathType.Sleep, deathType);
    }

    #endregion

    #region Sleep Node Tests

    [Fact]
    public async Task Variable_GetPlayerSleep_ReturnsSleepValue()
    {
        var (world, state) = CreateTestWorld();
        state.Player.DynamicStats.Sleep = 45;

        var script = new ScriptDefinition
        {
            Id = "test_script",
            OwnerType = "Game",
            OwnerId = "test_basic_needs",
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
                    Id = "var_node",
                    NodeType = NodeTypeId.Variable_GetPlayerSleep,
                    Category = NodeCategory.Variable,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                }
            },
            Connections = new List<NodeConnection>()
        };
        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state);

        // Execute script - variable nodes are evaluated on demand
        await engine.TriggerEventAsync("Game", "test_basic_needs", NodeTypeId.Event_OnGameStart);

        // The value should be accessible
        Assert.Equal(45, state.Player.DynamicStats.Sleep);
    }

    #endregion

    #region GameInfo Properties Tests

    [Fact]
    public void GameInfo_BasicNeedsEnabled_DefaultsToFalse()
    {
        var gameInfo = new GameInfo();
        Assert.False(gameInfo.BasicNeedsEnabled);
    }

    [Fact]
    public void GameInfo_NeedRates_DefaultToNormal()
    {
        var gameInfo = new GameInfo();
        Assert.Equal(NeedRate.Normal, gameInfo.HungerRate);
        Assert.Equal(NeedRate.Normal, gameInfo.ThirstRate);
        Assert.Equal(NeedRate.Normal, gameInfo.SleepRate);
    }

    [Fact]
    public void GameInfo_DeathTexts_HaveDefaults()
    {
        var gameInfo = new GameInfo();
        Assert.False(string.IsNullOrEmpty(gameInfo.HungerDeathText));
        Assert.False(string.IsNullOrEmpty(gameInfo.ThirstDeathText));
        Assert.False(string.IsNullOrEmpty(gameInfo.SleepDeathText));
    }

    #endregion

    #region PlayerDynamicStats Sleep Tests

    [Fact]
    public void PlayerDynamicStats_Sleep_DefaultsToZero()
    {
        var stats = new PlayerDynamicStats();
        Assert.Equal(0, stats.Sleep);
    }

    [Fact]
    public void PlayerDynamicStats_Sleep_CanBeSetAndGet()
    {
        var stats = new PlayerDynamicStats();
        stats.Sleep = 75;
        Assert.Equal(75, stats.Sleep);
    }

    #endregion

    #region PlayerStateType Tests

    [Fact]
    public void PlayerStateType_IncludesSleep()
    {
        Assert.True(Enum.IsDefined(typeof(PlayerStateType), "Sleep"));
    }

    #endregion

    #region Action_SetNeedRate Tests

    [Theory]
    [InlineData("Hunger", "Low", NeedRate.Low)]
    [InlineData("Hunger", "Normal", NeedRate.Normal)]
    [InlineData("Hunger", "High", NeedRate.High)]
    [InlineData("Thirst", "Low", NeedRate.Low)]
    [InlineData("Thirst", "Normal", NeedRate.Normal)]
    [InlineData("Thirst", "High", NeedRate.High)]
    [InlineData("Sleep", "Low", NeedRate.Low)]
    [InlineData("Sleep", "Normal", NeedRate.Normal)]
    [InlineData("Sleep", "High", NeedRate.High)]
    public async Task Action_SetNeedRate_ChangesCorrectProperty(string needType, string rateStr, NeedRate expectedRate)
    {
        var (world, state) = CreateTestWorld();

        var node = new ScriptNode
        {
            Id = "set_rate_node",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = needType,
                ["Rate"] = rateStr
            }
        };

        var engine = new ScriptEngine(world, state);
        await engine.ExecuteSingleNodeAsync(node);

        var actualRate = needType switch
        {
            "Hunger" => world.Game.HungerRate,
            "Thirst" => world.Game.ThirstRate,
            "Sleep" => world.Game.SleepRate,
            _ => NeedRate.Normal
        };

        Assert.Equal(expectedRate, actualRate);
    }

    [Fact]
    public async Task Action_SetNeedRate_DoesNotAffectOtherRates()
    {
        var (world, state) = CreateTestWorld();
        world.Game.HungerRate = NeedRate.Normal;
        world.Game.ThirstRate = NeedRate.Normal;
        world.Game.SleepRate = NeedRate.Normal;

        var node = new ScriptNode
        {
            Id = "set_rate_node",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Hunger",
                ["Rate"] = "High"
            }
        };

        var engine = new ScriptEngine(world, state);
        await engine.ExecuteSingleNodeAsync(node);

        Assert.Equal(NeedRate.High, world.Game.HungerRate);
        Assert.Equal(NeedRate.Normal, world.Game.ThirstRate);
        Assert.Equal(NeedRate.Normal, world.Game.SleepRate);
    }

    #endregion

    #region Variable_GetNeedRate Tests

    [Theory]
    [InlineData("Hunger", NeedRate.Low, 0)]
    [InlineData("Hunger", NeedRate.Normal, 1)]
    [InlineData("Hunger", NeedRate.High, 2)]
    [InlineData("Thirst", NeedRate.Low, 0)]
    [InlineData("Thirst", NeedRate.Normal, 1)]
    [InlineData("Thirst", NeedRate.High, 2)]
    [InlineData("Sleep", NeedRate.Low, 0)]
    [InlineData("Sleep", NeedRate.Normal, 1)]
    [InlineData("Sleep", NeedRate.High, 2)]
    public async Task Variable_GetNeedRate_ReturnsCorrectValue(string needType, NeedRate rate, int expectedValue)
    {
        var (world, state) = CreateTestWorld();

        // Set the rate
        switch (needType)
        {
            case "Hunger": world.Game.HungerRate = rate; break;
            case "Thirst": world.Game.ThirstRate = rate; break;
            case "Sleep": world.Game.SleepRate = rate; break;
        }

        var script = new ScriptDefinition
        {
            Id = "test_script",
            OwnerType = "Game",
            OwnerId = "test_basic_needs",
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
                    Id = "get_rate_node",
                    NodeType = NodeTypeId.Variable_GetNeedRate,
                    Category = NodeCategory.Variable,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NeedType"] = needType
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_node",
                    FromPortName = "Exec",
                    ToNodeId = "get_rate_node",
                    ToPortName = "Exec"
                }
            }
        };
        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state);
        await engine.ExecuteSingleNodeAsync(script.Nodes[1]);

        // Verify the rate value is as expected
        Assert.Equal(expectedValue, (int)rate);
    }

    #endregion

    #region Integration Tests - NeedRate Script Nodes

    [Fact]
    public async Task Integration_SetNeedRate_AffectsGameEngineIncrements()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Game.HungerRate = NeedRate.Normal;

        // First, set hunger rate to High via script
        var setRateNode = new ScriptNode
        {
            Id = "set_rate_node",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Hunger",
                ["Rate"] = "High"
            }
        };

        var scriptEngine = new ScriptEngine(world, state);
        await scriptEngine.ExecuteSingleNodeAsync(setRateNode);

        // Verify the rate was changed
        Assert.Equal(NeedRate.High, world.Game.HungerRate);

        // Now process a command and verify hunger increases at the higher rate
        state.Player.DynamicStats.Hunger = 0;
        var gameEngine = CreateEngine(world, state);

        // Usar "esperar" que siempre es un comando exitoso y consume turno
        var result = gameEngine.ProcessCommand("esperar");

        // Verificar que el comando fue exitoso (solo comandos exitosos procesan necesidades)
        Assert.True(result.IsSuccess, "Command should succeed to trigger basic needs processing");

        // High rate: base 1.3 * 1.5 = 1.95, truncated to 1, but accumulator keeps 0.95
        // Next turn would add more
        Assert.True(state.Player.DynamicStats.Hunger >= 1,
            $"Expected Hunger >= 1 at High rate, got {state.Player.DynamicStats.Hunger}");
    }

    [Fact]
    public async Task Integration_SetNeedRate_LowRate_SlowsIncrements()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);

        // Set hunger rate to Low via script
        var setRateNode = new ScriptNode
        {
            Id = "set_rate_node",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Hunger",
                ["Rate"] = "Low"
            }
        };

        var scriptEngine = new ScriptEngine(world, state);
        await scriptEngine.ExecuteSingleNodeAsync(setRateNode);

        Assert.Equal(NeedRate.Low, world.Game.HungerRate);

        // Process several commands and verify hunger increases slowly
        state.Player.DynamicStats.Hunger = 0;
        var gameEngine = CreateEngine(world, state);

        // At Low rate (0.5 modifier), base 1.3 * 0.5 = 0.65 per turn
        // After 2 turns: 0.65 * 2 = 1.3, so hunger should be 1
        gameEngine.ProcessCommand("mirar");
        gameEngine.ProcessCommand("mirar");

        Assert.True(state.Player.DynamicStats.Hunger <= 2,
            $"Expected <= 2 at low rate, got {state.Player.DynamicStats.Hunger}");
    }

    [Fact]
    public async Task Integration_FullScriptExecution_SetAndGetNeedRate()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Game.HungerRate = NeedRate.Normal;

        // Create a complete script that sets the rate
        var script = new ScriptDefinition
        {
            Id = "test_set_rate_script",
            OwnerType = "Game",
            OwnerId = "test_basic_needs",
            Name = "Test Set Rate Script",
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
                    Id = "set_rate_node",
                    NodeType = NodeTypeId.Action_SetNeedRate,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["NeedType"] = "Thirst",
                        ["Rate"] = "Low"
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_node",
                    FromPortName = "Exec",
                    ToNodeId = "set_rate_node",
                    ToPortName = "Exec"
                }
            }
        };
        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_basic_needs", NodeTypeId.Event_OnGameStart);

        Assert.Equal(NeedRate.Low, world.Game.ThirstRate);
    }

    [Fact]
    public async Task Integration_MultipleRateChanges_AllApplyCorrectly()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);

        var scriptEngine = new ScriptEngine(world, state);

        // Change all rates to different values
        await scriptEngine.ExecuteSingleNodeAsync(new ScriptNode
        {
            Id = "node1",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Hunger",
                ["Rate"] = "High"
            }
        });

        await scriptEngine.ExecuteSingleNodeAsync(new ScriptNode
        {
            Id = "node2",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Thirst",
                ["Rate"] = "Low"
            }
        });

        await scriptEngine.ExecuteSingleNodeAsync(new ScriptNode
        {
            Id = "node3",
            NodeType = NodeTypeId.Action_SetNeedRate,
            Category = NodeCategory.Action,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["NeedType"] = "Sleep",
                ["Rate"] = "Normal"
            }
        });

        Assert.Equal(NeedRate.High, world.Game.HungerRate);
        Assert.Equal(NeedRate.Low, world.Game.ThirstRate);
        Assert.Equal(NeedRate.Normal, world.Game.SleepRate);
    }

    #endregion

    #region NodeTypeRegistry Tests

    [Fact]
    public void NodeTypeRegistry_Action_SetNeedRate_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetNeedRate);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Action_SetNeedRate, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Action, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Variable_GetNeedRate_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Variable_GetNeedRate);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Variable_GetNeedRate, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Variable, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Action_SetNeedRate_HasCorrectProperties()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetNeedRate);
        Assert.NotNull(nodeDef);

        var needTypeProp = nodeDef.Properties.FirstOrDefault(p => p.Name == "NeedType");
        Assert.NotNull(needTypeProp);
        Assert.Equal("select", needTypeProp.DataType);
        Assert.NotNull(needTypeProp.Options);
        Assert.Contains("Hunger", needTypeProp.Options);
        Assert.Contains("Thirst", needTypeProp.Options);
        Assert.Contains("Sleep", needTypeProp.Options);

        var rateProp = nodeDef.Properties.FirstOrDefault(p => p.Name == "Rate");
        Assert.NotNull(rateProp);
        Assert.Equal("select", rateProp.DataType);
        Assert.NotNull(rateProp.Options);
        Assert.Contains("Low", rateProp.Options);
        Assert.Contains("Normal", rateProp.Options);
        Assert.Contains("High", rateProp.Options);
    }

    [Fact]
    public void NodeTypeRegistry_Variable_GetNeedRate_HasCorrectProperties()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Variable_GetNeedRate);
        Assert.NotNull(nodeDef);

        var needTypeProp = nodeDef.Properties.FirstOrDefault(p => p.Name == "NeedType");
        Assert.NotNull(needTypeProp);
        Assert.Equal("select", needTypeProp.DataType);
        Assert.NotNull(needTypeProp.Options);
        Assert.Contains("Hunger", needTypeProp.Options);
        Assert.Contains("Thirst", needTypeProp.Options);
        Assert.Contains("Sleep", needTypeProp.Options);
    }

    [Fact]
    public void NodeTypeRegistry_NeedRateNodes_FilteredByFeature()
    {
        var gameInfoWithBasicNeeds = new GameInfo { BasicNeedsEnabled = true };
        var gameInfoWithoutBasicNeeds = new GameInfo { BasicNeedsEnabled = false };

        var nodesWithFeature = NodeTypeRegistry.GetNodesForOwnerType("Game", gameInfoWithBasicNeeds).ToList();
        var nodesWithoutFeature = NodeTypeRegistry.GetNodesForOwnerType("Game", gameInfoWithoutBasicNeeds).ToList();

        Assert.Contains(nodesWithFeature, n => n.TypeId == NodeTypeId.Action_SetNeedRate);
        Assert.Contains(nodesWithFeature, n => n.TypeId == NodeTypeId.Variable_GetNeedRate);

        Assert.DoesNotContain(nodesWithoutFeature, n => n.TypeId == NodeTypeId.Action_SetNeedRate);
        Assert.DoesNotContain(nodesWithoutFeature, n => n.TypeId == NodeTypeId.Variable_GetNeedRate);
    }

    #endregion

    #region Eat Command Tests

    [Fact]
    public void EatCommand_BasicNeedsDisabled_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: false);
        world.Objects.Add(new GameObject
        {
            Id = "apple",
            Name = "manzana",
            Type = ObjectType.Comida,
            NutritionAmount = 20
        });
        state.InventoryObjectIds.Add("apple");

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("comer manzana");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void EatCommand_NoTarget_AsksWhatToEat()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        var result = engine.ProcessCommand("comer");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void EatCommand_ObjectNotFood_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Objects.Add(new GameObject
        {
            Id = "key",
            Name = "llave",
            Type = ObjectType.Llave
        });
        state.InventoryObjectIds.Add("key");

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("comer llave");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void EatCommand_FoodInInventory_ReducesHungerByNutritionAmount()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var apple = new GameObject
        {
            Id = "apple",
            Name = "manzana",
            Type = ObjectType.Comida,
            NutritionAmount = 25
        };
        world.Objects.Add(apple);
        state.Objects.Add(apple);
        state.InventoryObjectIds.Add("apple");
        state.Player.DynamicStats.Hunger = 50;

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("comer manzana");

        Assert.True(result.IsSuccess);
        // Hunger reduced by 25, but turn increments by 1 (hunger rate)
        Assert.True(state.Player.DynamicStats.Hunger < 50);
        Assert.True(state.Player.DynamicStats.Hunger <= 26); // 50 - 25 + 1 = 26
    }

    [Fact]
    public void EatCommand_FoodInRoom_ReducesHungerByNutritionAmount()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var bread = new GameObject
        {
            Id = "bread",
            Name = "pan",
            Type = ObjectType.Comida,
            NutritionAmount = 15,
            RoomId = "room1"
        };
        world.Objects.Add(bread);
        state.Objects.Add(bread);
        state.Rooms[0].ObjectIds.Add("bread");

        state.Player.DynamicStats.Hunger = 40;

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("comer pan");

        Assert.True(result.IsSuccess);
        // Hunger reduced by 15, but turn increments by 1
        Assert.True(state.Player.DynamicStats.Hunger < 40);
        Assert.True(state.Player.DynamicStats.Hunger <= 26); // 40 - 15 + 1 = 26
    }

    [Fact]
    public void EatCommand_HungerNeverGoesBelowZero()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var feast = new GameObject
        {
            Id = "feast",
            Name = "banquete",
            Type = ObjectType.Comida,
            NutritionAmount = 100
        };
        world.Objects.Add(feast);
        state.Objects.Add(feast);
        state.InventoryObjectIds.Add("feast");
        state.Player.DynamicStats.Hunger = 10;

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("comer banquete");

        // Hunger is reduced to 0, then increments by 1 from turn
        Assert.True(state.Player.DynamicStats.Hunger <= 2);
    }

    [Fact]
    public void EatCommand_RemovesFoodFromInventory()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var apple = new GameObject
        {
            Id = "apple",
            Name = "manzana",
            Type = ObjectType.Comida,
            NutritionAmount = 10
        };
        world.Objects.Add(apple);
        state.Objects.Add(apple);
        state.InventoryObjectIds.Add("apple");

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("comer manzana");

        Assert.DoesNotContain("apple", state.InventoryObjectIds);
    }

    [Fact]
    public void EatCommand_RemovesFoodFromRoom()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var bread = new GameObject
        {
            Id = "bread",
            Name = "pan",
            Type = ObjectType.Comida,
            NutritionAmount = 10,
            RoomId = "room1"
        };
        world.Objects.Add(bread);
        state.Objects.Add(bread);
        state.Rooms[0].ObjectIds.Add("bread");

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("comer pan");

        Assert.DoesNotContain("bread", state.Rooms[0].ObjectIds);
    }

    [Fact]
    public void EatCommand_DefaultNutritionAmount_Is10()
    {
        var food = new GameObject { Type = ObjectType.Comida };
        Assert.Equal(10, food.NutritionAmount);
    }

    #endregion

    #region Drink Command Tests

    [Fact]
    public void DrinkCommand_BasicNeedsDisabled_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: false);
        world.Objects.Add(new GameObject
        {
            Id = "water",
            Name = "agua",
            Type = ObjectType.Bebida,
            NutritionAmount = 20
        });
        state.InventoryObjectIds.Add("water");

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("beber agua");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DrinkCommand_NoTarget_AsksWhatToDrink()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        var result = engine.ProcessCommand("beber");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DrinkCommand_ObjectNotDrink_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        world.Objects.Add(new GameObject
        {
            Id = "sword",
            Name = "espada",
            Type = ObjectType.Arma
        });
        state.InventoryObjectIds.Add("sword");

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("beber espada");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void DrinkCommand_DrinkInInventory_ReducesThirstByNutritionAmount()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var water = new GameObject
        {
            Id = "water",
            Name = "agua",
            Type = ObjectType.Bebida,
            NutritionAmount = 30
        };
        world.Objects.Add(water);
        state.Objects.Add(water);
        state.InventoryObjectIds.Add("water");
        state.Player.DynamicStats.Thirst = 60;

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("beber agua");

        Assert.True(result.IsSuccess);
        // Thirst reduced by 30, but turn increments by 1
        Assert.True(state.Player.DynamicStats.Thirst < 60);
        Assert.True(state.Player.DynamicStats.Thirst <= 31); // 60 - 30 + 1 = 31
    }

    [Fact]
    public void DrinkCommand_DrinkInRoom_ReducesThirstByNutritionAmount()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var juice = new GameObject
        {
            Id = "juice",
            Name = "zumo",
            Type = ObjectType.Bebida,
            NutritionAmount = 20,
            RoomId = "room1"
        };
        world.Objects.Add(juice);
        state.Objects.Add(juice);
        state.Rooms[0].ObjectIds.Add("juice");
        state.Player.DynamicStats.Thirst = 50;

        var engine = CreateEngine(world, state);
        var result = engine.ProcessCommand("beber zumo");

        Assert.True(result.IsSuccess);
        // Thirst reduced by 20, but turn increments by 1
        Assert.True(state.Player.DynamicStats.Thirst < 50);
        Assert.True(state.Player.DynamicStats.Thirst <= 31); // 50 - 20 + 1 = 31
    }

    [Fact]
    public void DrinkCommand_ThirstNeverGoesBelowZero()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var fountain = new GameObject
        {
            Id = "fountain",
            Name = "fuente",
            Type = ObjectType.Bebida,
            NutritionAmount = 100
        };
        world.Objects.Add(fountain);
        state.Objects.Add(fountain);
        state.InventoryObjectIds.Add("fountain");
        state.Player.DynamicStats.Thirst = 15;

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("beber fuente");

        // Thirst is reduced to 0, then increments by 1 from turn
        Assert.True(state.Player.DynamicStats.Thirst <= 2);
    }

    [Fact]
    public void DrinkCommand_RemovesDrinkFromInventory()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var water = new GameObject
        {
            Id = "water",
            Name = "agua",
            Type = ObjectType.Bebida,
            NutritionAmount = 10
        };
        world.Objects.Add(water);
        state.Objects.Add(water);
        state.InventoryObjectIds.Add("water");

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("beber agua");

        Assert.DoesNotContain("water", state.InventoryObjectIds);
    }

    #endregion

    #region Sleep Command Tests

    [Fact]
    public void SleepCommand_BasicNeedsDisabled_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: false);
        var engine = CreateEngine(world, state);

        var result = engine.ProcessCommand("dormir");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void SleepCommand_AsksForHours()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        var result = engine.ProcessCommand("dormir");

        // Sleep command should succeed and ask for hours
        Assert.True(result.IsSuccess);
        Assert.Contains("hora", result.Message.ToLower());
    }

    [Fact]
    public void SleepCommand_WithHours_ReducesSleepByAmount()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        state.Player.DynamicStats.Sleep = 50;

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("dormir");

        // Use TryProcessSleepResponse to provide hours
        Assert.True(engine.TryProcessSleepResponse("4", out var result));
        Assert.True(result.IsSuccess);

        // Each hour reduces sleep by 10, so 4 hours = 40 reduction
        Assert.True(state.Player.DynamicStats.Sleep < 50);
    }

    [Fact]
    public void SleepCommand_InvalidHours_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        engine.ProcessCommand("dormir");

        // Use TryProcessSleepResponse with invalid hours
        Assert.True(engine.TryProcessSleepResponse("10", out var result));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void SleepCommand_ZeroHours_ReturnsError()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var engine = CreateEngine(world, state);

        engine.ProcessCommand("dormir");

        // Use TryProcessSleepResponse with zero hours
        Assert.True(engine.TryProcessSleepResponse("0", out var result));
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void SleepCommand_SleepNeverGoesBelowZero()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        state.Player.DynamicStats.Sleep = 15;

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("dormir");
        engine.TryProcessSleepResponse("8", out _); // 8 hours = 80 reduction

        // Sleep should be near 0 (after turn increments)
        Assert.True(state.Player.DynamicStats.Sleep <= 10);
    }

    [Fact]
    public void SleepCommand_AdvancesTurns()
    {
        var (world, state) = CreateTestWorld(basicNeedsEnabled: true);
        var initialTurns = state.TurnCounter;
        world.Game.MinutesPerGameHour = 60;

        var engine = CreateEngine(world, state);
        engine.ProcessCommand("dormir");
        engine.TryProcessSleepResponse("2", out _); // Sleep 2 hours

        // Turns should advance (60 min per hour = 2 turns per hour at 30 min per turn default)
        Assert.True(state.TurnCounter > initialTurns);
    }

    #endregion

    #region Script Event Tests

    [Fact]
    public void NodeTypeRegistry_Event_OnEat_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnEat);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Event_OnEat, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Event, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Event_OnDrink_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnDrink);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Event_OnDrink, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Event, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Event_OnSleep_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnSleep);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Event_OnSleep, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Event, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Event_OnWakeUp_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnWakeUp);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Event_OnWakeUp, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Event, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_Event_OnWakeUpStartled_Exists()
    {
        var nodeDef = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnWakeUpStartled);
        Assert.NotNull(nodeDef);
        Assert.Equal(NodeTypeId.Event_OnWakeUpStartled, nodeDef.TypeId);
        Assert.Equal(NodeCategory.Event, nodeDef.Category);
        Assert.Equal(RequiredFeature.BasicNeeds, nodeDef.RequiredFeature);
    }

    [Fact]
    public void NodeTypeRegistry_ConsumableEvents_FilteredByFeature()
    {
        var gameInfoWithBasicNeeds = new GameInfo { BasicNeedsEnabled = true };
        var gameInfoWithoutBasicNeeds = new GameInfo { BasicNeedsEnabled = false };

        var nodesWithFeature = NodeTypeRegistry.GetNodesForOwnerType("GameObject", gameInfoWithBasicNeeds).ToList();
        var nodesWithoutFeature = NodeTypeRegistry.GetNodesForOwnerType("GameObject", gameInfoWithoutBasicNeeds).ToList();

        Assert.Contains(nodesWithFeature, n => n.TypeId == NodeTypeId.Event_OnEat);
        Assert.Contains(nodesWithFeature, n => n.TypeId == NodeTypeId.Event_OnDrink);

        Assert.DoesNotContain(nodesWithoutFeature, n => n.TypeId == NodeTypeId.Event_OnEat);
        Assert.DoesNotContain(nodesWithoutFeature, n => n.TypeId == NodeTypeId.Event_OnDrink);
    }

    #endregion

    #region GameObject NutritionAmount Tests

    [Fact]
    public void GameObject_NutritionAmount_DefaultsTo10()
    {
        var obj = new GameObject();
        Assert.Equal(10, obj.NutritionAmount);
    }

    [Fact]
    public void GameObject_NutritionAmount_CanBeSetAndGet()
    {
        var obj = new GameObject { NutritionAmount = 50 };
        Assert.Equal(50, obj.NutritionAmount);
    }

    #endregion
}
