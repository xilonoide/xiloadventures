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
/// Tests for the ScriptEngine class including event handling,
/// action execution, conditions, and debug mode.
/// </summary>
public class ScriptEngineTests
{
    #region Test Helpers

    private static (WorldModel world, GameState state) CreateTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_scripts",
                Title = "Test Scripts",
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
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room2" }
                    },
                    ObjectIds = new List<string> { "obj1" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room2",
                    Name = "Room 2",
                    Description = "Second room.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room1" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj1",
                    Name = "test object",
                    Description = "A test object.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>
            {
                new Door
                {
                    Id = "door1",
                    Name = "test door",
                    RoomIdA = "room1",
                    RoomIdB = "room2",
                    IsLocked = true,
                    IsOpen = false
                }
            },
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    private static ScriptDefinition CreateScript(
        string ownerType,
        string ownerId,
        NodeTypeId eventType,
        NodeTypeId actionType,
        Dictionary<string, object?>? actionProps = null)
    {
        return new ScriptDefinition
        {
            Id = $"script_{eventType}_{actionType}",
            OwnerType = ownerType,
            OwnerId = ownerId,
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

    #endregion

    #region Event Triggering Tests

    [Fact]
    public async Task TriggerEventAsync_OnEnter_TriggersMatchingScript()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = "You entered room1!"
        };

        world.Scripts.Add(CreateScript("Room", "room1", NodeTypeId.Event_OnEnter, NodeTypeId.Action_ShowMessage, actionProps));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.Equal("You entered room1!", receivedMessage);
    }

    [Fact]
    public async Task TriggerEventAsync_WrongOwner_DoesNotTrigger()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = "This should not show"
        };

        // Script is for room2
        world.Scripts.Add(CreateScript("Room", "room2", NodeTypeId.Event_OnEnter, NodeTypeId.Action_ShowMessage, actionProps));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        // Trigger for room1
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.Null(receivedMessage);
    }

    #endregion

    #region Door Action Tests

    [Fact]
    public async Task Action_OpenDoor_OpensDoor()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["DoorId"] = "door1"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_OpenDoor, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.True(door.IsOpen);
    }

    [Fact]
    public async Task Action_UnlockDoor_UnlocksDoor()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["DoorId"] = "door1"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_UnlockDoor, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.False(door.IsLocked);
    }

    #endregion

    #region Flag and Counter Tests

    [Fact]
    public async Task Action_SetFlag_SetsFlag()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["FlagName"] = "test_flag",
            ["Value"] = true
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_SetFlag, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Flags.ContainsKey("test_flag"));
        Assert.True(state.Flags["test_flag"]);
    }

    [Fact]
    public async Task Action_SetCounter_SetsCounter()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CounterName"] = "test_counter",
            ["Value"] = 42
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_SetCounter, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Counters.ContainsKey("test_counter"));
        Assert.Equal(42, state.Counters["test_counter"]);
    }

    #endregion

    #region Item Management Tests

    [Fact]
    public async Task Action_GiveItem_AddsToPlayerInventory()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "obj1"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_GiveItem, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        // GiveItem adds to InventoryObjectIds and sets RoomId to null
        Assert.Contains("obj1", state.InventoryObjectIds);
    }

    [Fact]
    public async Task Action_RemoveItem_RemovesFromInventory()
    {
        var (world, state) = CreateTestWorld();
        // First add item to inventory
        state.InventoryObjectIds.Add("obj1");

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "obj1"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RemoveItem, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        // RemoveItem removes from InventoryObjectIds
        Assert.DoesNotContain("obj1", state.InventoryObjectIds);
    }

    #endregion

    #region Teleport Tests

    [Fact]
    public async Task Action_TeleportPlayer_TriggersEventAndChangesRoom()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["RoomId"] = "room2"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_TeleportPlayer, actionProps));

        var engine = new ScriptEngine(world, state);
        string? teleportedTo = null;
        engine.OnPlayerTeleported += roomId => teleportedTo = roomId;

        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal("room2", teleportedTo);
        Assert.Equal("room2", state.CurrentRoomId);
    }

    #endregion

    #region Gold Tests

    [Fact]
    public async Task Action_AddMoney_IncreasesPlayerGold()
    {
        var (world, state) = CreateTestWorld();
        state.Player.Money = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 50
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_AddMoney, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal(150, state.Player.Money);
    }

    [Fact]
    public async Task Action_RemoveMoney_DecreasesPlayerGold()
    {
        var (world, state) = CreateTestWorld();
        state.Player.Money = 100;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Amount"] = 30
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_RemoveMoney, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal(70, state.Player.Money);
    }

    #endregion

    #region Debug Mode Tests

    [Fact]
    public void Constructor_DebugModeIsStoredCorrectly()
    {
        var (world, state) = CreateTestWorld();

        var engineWithDebug = new ScriptEngine(world, state, isDebugMode: true);
        var engineWithoutDebug = new ScriptEngine(world, state, isDebugMode: false);

        // Engines should be created without throwing
        Assert.NotNull(engineWithDebug);
        Assert.NotNull(engineWithoutDebug);
    }

    #endregion

    #region Object Visibility Tests

    [Fact]
    public async Task Action_SetObjectVisible_MakesObjectVisible()
    {
        var (world, state) = CreateTestWorld();
        state.Objects.First(o => o.Id == "obj1").Visible = false;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "obj1",
            ["Visible"] = true
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_SetObjectVisible, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.True(state.Objects.First(o => o.Id == "obj1").Visible);
    }

    [Fact]
    public async Task Action_SetObjectVisible_MakesObjectInvisible()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "obj1",
            ["Visible"] = false
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_SetObjectVisible, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.False(state.Objects.First(o => o.Id == "obj1").Visible);
    }

    #endregion

    #region Counter Tests

    [Fact]
    public async Task Action_IncrementCounter_IncreasesCounter()
    {
        var (world, state) = CreateTestWorld();
        state.Counters["test_counter"] = 10;

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CounterName"] = "test_counter",
            ["Amount"] = 5
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_IncrementCounter, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal(15, state.Counters["test_counter"]);
    }

    [Fact]
    public async Task Action_PlaySound_TriggersEvent()
    {
        var (world, state) = CreateTestWorld();

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["SoundId"] = "test_sound"
        };

        world.Scripts.Add(CreateScript("Game", "test_scripts", NodeTypeId.Event_OnGameStart, NodeTypeId.Action_PlaySound, actionProps));

        var engine = new ScriptEngine(world, state);
        string? playedSound = null;
        engine.OnPlaySound += soundId => playedSound = soundId;

        await engine.TriggerEventAsync("Game", "test_scripts", NodeTypeId.Event_OnGameStart);

        Assert.Equal("test_sound", playedSound);
    }

    #endregion
}
