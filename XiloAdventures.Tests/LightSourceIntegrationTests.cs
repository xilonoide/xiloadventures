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
/// Integration tests for light source functionality with scripts.
/// Tests script node handlers for lighting conditions and actions.
/// </summary>
public class LightSourceIntegrationTests
{
    /// <summary>
    /// Creates a mock SoundManager that does nothing (for testing without audio).
    /// </summary>
    private static SoundManager CreateMockSoundManager()
    {
        var sound = new SoundManager { SoundEnabled = false };
        return sound;
    }

    /// <summary>
    /// Creates a test world with scripts for light source testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateWorldWithLightScripts()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_light_scripts",
                Title = "Light Script Test",
                StartRoomId = "room",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room",
                    Name = "Habitación",
                    Description = "Una habitación de prueba.",
                    IsInterior = true,
                    IsIlluminated = false,
                    ObjectIds = new List<string> { "torch" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "torch",
                    Name = "antorcha",
                    Description = "Una antorcha.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room",
                    IsLightSource = true,
                    IsLit = false,
                    LightTurnsRemaining = 10,
                    CanIgnite = true,
                    CanExtinguish = true
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    #region Script Node Registration Tests

    [Fact]
    public void NodeTypeRegistry_HasLightSourceNodes()
    {
        Assert.NotNull(NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetObjectLit));
        Assert.NotNull(NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetLightTurns));
        Assert.NotNull(NodeTypeRegistry.GetNodeType(NodeTypeId.Condition_IsObjectLit));
        Assert.NotNull(NodeTypeRegistry.GetNodeType(NodeTypeId.Condition_IsRoomLit));
    }

    [Fact]
    public void Action_SetObjectLit_HasCorrectProperties()
    {
        var nodeType = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetObjectLit);

        Assert.NotNull(nodeType);
        Assert.Equal(NodeTypeId.Action_SetObjectLit, nodeType.TypeId);
        Assert.Equal(NodeCategory.Action, nodeType.Category);

        var objectIdProp = nodeType.Properties.FirstOrDefault(p => p.Name == "ObjectId");
        Assert.NotNull(objectIdProp);
        Assert.Equal("GameObject", objectIdProp.EntityType);

        var isLitProp = nodeType.Properties.FirstOrDefault(p => p.Name == "IsLit");
        Assert.NotNull(isLitProp);
        Assert.Equal("bool", isLitProp.DataType);
    }

    [Fact]
    public void Action_SetLightTurns_HasCorrectProperties()
    {
        var nodeType = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetLightTurns);

        Assert.NotNull(nodeType);
        Assert.Equal(NodeTypeId.Action_SetLightTurns, nodeType.TypeId);
        Assert.Equal(NodeCategory.Action, nodeType.Category);

        var objectIdProp = nodeType.Properties.FirstOrDefault(p => p.Name == "ObjectId");
        Assert.NotNull(objectIdProp);

        var turnsProp = nodeType.Properties.FirstOrDefault(p => p.Name == "Turns");
        Assert.NotNull(turnsProp);
        Assert.Equal("int", turnsProp.DataType);
    }

    [Fact]
    public void Condition_IsObjectLit_HasCorrectPorts()
    {
        var nodeType = NodeTypeRegistry.GetNodeType(NodeTypeId.Condition_IsObjectLit);

        Assert.NotNull(nodeType);
        Assert.Equal(NodeCategory.Condition, nodeType.Category);

        Assert.Contains(nodeType.OutputPorts, p => p.Name == "True");
        Assert.Contains(nodeType.OutputPorts, p => p.Name == "False");
    }

    [Fact]
    public void Condition_IsRoomLit_HasNoProperties()
    {
        var nodeType = NodeTypeRegistry.GetNodeType(NodeTypeId.Condition_IsRoomLit);

        Assert.NotNull(nodeType);
        Assert.Equal(NodeCategory.Condition, nodeType.Category);
        Assert.Empty(nodeType.Properties);
    }

    #endregion

    #region Script Engine Handler Tests

    [Fact]
    public async Task ScriptEngine_Action_SetObjectLit_TurnsOnLight()
    {
        var (world, state) = CreateWorldWithLightScripts();

        // Create a script that turns on the torch
        var script = new ScriptDefinition
        {
            Id = "test_light_on",
            Name = "Turn On Light",
            OwnerType = "Game",
            OwnerId = "test_light_scripts",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event
                },
                new ScriptNode
                {
                    Id = "action",
                    NodeType = NodeTypeId.Action_SetObjectLit,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>
                    {
                        ["ObjectId"] = "torch",
                        ["IsLit"] = true
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event",
                    FromPortName = "Exec",
                    ToNodeId = "action",
                    ToPortName = "Exec"
                }
            }
        };

        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state, false);
        await engine.TriggerEventAsync("Game", "test_light_scripts", NodeTypeId.Event_OnGameStart);

        var torch = state.Objects.First(o => o.Id == "torch");
        Assert.True(torch.IsLit);
    }

    [Fact]
    public async Task ScriptEngine_Action_SetObjectLit_TurnsOffLight()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        var script = new ScriptDefinition
        {
            Id = "test_light_off",
            Name = "Turn Off Light",
            OwnerType = "Game",
            OwnerId = "test_light_scripts",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event
                },
                new ScriptNode
                {
                    Id = "action",
                    NodeType = NodeTypeId.Action_SetObjectLit,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>
                    {
                        ["ObjectId"] = "torch",
                        ["IsLit"] = false
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event",
                    FromPortName = "Exec",
                    ToNodeId = "action",
                    ToPortName = "Exec"
                }
            }
        };

        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state, false);
        await engine.TriggerEventAsync("Game", "test_light_scripts", NodeTypeId.Event_OnGameStart);

        Assert.False(torch.IsLit);
    }

    [Fact]
    public async Task ScriptEngine_Action_SetLightTurns_SetsRemainingTurns()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.LightTurnsRemaining = 10;

        var script = new ScriptDefinition
        {
            Id = "test_set_turns",
            Name = "Set Light Turns",
            OwnerType = "Game",
            OwnerId = "test_light_scripts",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event
                },
                new ScriptNode
                {
                    Id = "action",
                    NodeType = NodeTypeId.Action_SetLightTurns,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>
                    {
                        ["ObjectId"] = "torch",
                        ["Turns"] = 50
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event",
                    FromPortName = "Exec",
                    ToNodeId = "action",
                    ToPortName = "Exec"
                }
            }
        };

        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state, false);
        await engine.TriggerEventAsync("Game", "test_light_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal(50, torch.LightTurnsRemaining);
    }

    [Fact]
    public async Task ScriptEngine_Condition_IsObjectLit_ReturnsTrueWhenLit()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        // Create a script that checks if torch is lit and sets a flag
        var script = new ScriptDefinition
        {
            Id = "test_condition_lit",
            Name = "Check Light",
            OwnerType = "Game",
            OwnerId = "test_light_scripts",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event
                },
                new ScriptNode
                {
                    Id = "condition",
                    NodeType = NodeTypeId.Condition_IsObjectLit,
                    Category = NodeCategory.Condition,
                    Properties = new Dictionary<string, object?>
                    {
                        ["ObjectId"] = "torch"
                    }
                },
                new ScriptNode
                {
                    Id = "set_flag_true",
                    NodeType = NodeTypeId.Action_SetFlag,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>
                    {
                        ["FlagName"] = "torch_was_lit",
                        ["Value"] = true
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event",
                    FromPortName = "Exec",
                    ToNodeId = "condition",
                    ToPortName = "Exec"
                },
                new NodeConnection
                {
                    FromNodeId = "condition",
                    FromPortName = "True",
                    ToNodeId = "set_flag_true",
                    ToPortName = "Exec"
                }
            }
        };

        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state, false);
        await engine.TriggerEventAsync("Game", "test_light_scripts", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.TryGetValue("torch_was_lit", out var flagValue) && flagValue);
    }

    [Fact]
    public async Task ScriptEngine_Condition_IsObjectLit_ReturnsFalseWhenOff()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = false;

        var script = new ScriptDefinition
        {
            Id = "test_condition_off",
            Name = "Check Light Off",
            OwnerType = "Game",
            OwnerId = "test_light_scripts",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event
                },
                new ScriptNode
                {
                    Id = "condition",
                    NodeType = NodeTypeId.Condition_IsObjectLit,
                    Category = NodeCategory.Condition,
                    Properties = new Dictionary<string, object?>
                    {
                        ["ObjectId"] = "torch"
                    }
                },
                new ScriptNode
                {
                    Id = "set_flag_false",
                    NodeType = NodeTypeId.Action_SetFlag,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>
                    {
                        ["FlagName"] = "torch_was_off",
                        ["Value"] = true
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event",
                    FromPortName = "Exec",
                    ToNodeId = "condition",
                    ToPortName = "Exec"
                },
                new NodeConnection
                {
                    FromNodeId = "condition",
                    FromPortName = "False",
                    ToNodeId = "set_flag_false",
                    ToPortName = "Exec"
                }
            }
        };

        world.Scripts.Add(script);

        var engine = new ScriptEngine(world, state, false);
        await engine.TriggerEventAsync("Game", "test_light_scripts", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.TryGetValue("torch_was_off", out var flagValue) && flagValue);
    }

    #endregion

    #region Full Integration Tests

    [Fact]
    public void FullIntegration_DarkRoomToLit_CompleteFlow()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        // Room should be dark initially (room description not visible)
        var descBefore = engine.DescribeCurrentRoom();
        Assert.DoesNotContain("habitación", descBefore.ToLower());

        // Ignite the torch
        var igniteResult = engine.ProcessCommand("encender antorcha");
        Assert.True(igniteResult.IsSuccess);

        // Room should now be lit (room description visible)
        var descAfter = engine.DescribeCurrentRoom();
        Assert.Contains("habitación", descAfter.ToLower());

        // Extinguish the torch
        var extinguishResult = engine.ProcessCommand("apagar antorcha");
        Assert.True(extinguishResult.IsSuccess);

        // Room should be dark again (room description not visible)
        var descFinal = engine.DescribeCurrentRoom();
        Assert.DoesNotContain("habitación", descFinal.ToLower());
    }

    [Fact]
    public void FullIntegration_TorchInInventory_IlluminatesMultipleRooms()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_portable_light",
                Title = "Portable Light Test",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Primera habitación",
                    Description = "La primera habitación oscura.",
                    IsInterior = true,
                    IsIlluminated = false,
                    ObjectIds = new List<string> { "torch" },
                    NpcIds = new List<string>(),
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "norte", TargetRoomId = "room2" }
                    }
                },
                new Room
                {
                    Id = "room2",
                    Name = "Segunda habitación",
                    Description = "La segunda habitación oscura.",
                    IsInterior = true,
                    IsIlluminated = false,
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>(),
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "sur", TargetRoomId = "room1" }
                    }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "torch",
                    Name = "antorcha",
                    Description = "Una antorcha.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1",
                    IsLightSource = true,
                    IsLit = false,
                    LightTurnsRemaining = -1,
                    CanIgnite = true,
                    CanExtinguish = true
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        // Room 1 is dark (room description not visible)
        Assert.DoesNotContain("primera habitación", engine.DescribeCurrentRoom().ToLower());

        // Pick up and light the torch
        engine.ProcessCommand("coger antorcha");
        engine.ProcessCommand("encender antorcha");

        // Room 1 should now be lit (room description visible)
        Assert.Contains("primera habitación", engine.DescribeCurrentRoom().ToLower());

        // Move to room 2
        engine.ProcessCommand("ir norte");

        // Room 2 should also be lit (torch is in inventory)
        var desc = engine.DescribeCurrentRoom();
        Assert.Contains("segunda habitación", desc.ToLower());
    }

    [Fact]
    public void FullIntegration_LightExpires_DuringExploration()
    {
        var (world, state) = CreateWorldWithLightScripts();
        var torch = state.Objects.First(o => o.Id == "torch");
        // Start with 4 turns because "encender" command counts as a turn too
        torch.LightTurnsRemaining = 4;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        // Light the torch (this is turn 1, reduces to 3)
        engine.ProcessCommand("encender antorcha");
        Assert.True(torch.IsLit);
        Assert.Equal(3, torch.LightTurnsRemaining);

        // Process turns until light expires
        engine.ProcessCommand("esperar"); // Turn 2: 2 turns left
        Assert.True(torch.IsLit);
        Assert.Equal(2, torch.LightTurnsRemaining);

        engine.ProcessCommand("esperar"); // Turn 3: 1 turn left
        Assert.True(torch.IsLit);
        Assert.Equal(1, torch.LightTurnsRemaining);

        var result = engine.ProcessCommand("esperar"); // Turn 4: expires
        Assert.False(torch.IsLit);
        Assert.Equal(0, torch.LightTurnsRemaining);
        // RandomMessages.LightGoesOut contiene el nombre del objeto
        Assert.Contains("antorcha", result.Message.ToLower());

        // Room should now be dark (room description not visible)
        Assert.DoesNotContain("habitación", engine.DescribeCurrentRoom().ToLower());
    }

    #endregion
}
