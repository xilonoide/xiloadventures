using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for the GameEngine class.
/// Tests core game mechanics including movement, inventory, room description, and commands.
/// </summary>
public class GameEngineTests
{
    /// <summary>
    /// Creates a minimal test world with two connected rooms.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_world",
                Title = "Test World",
                StartRoomId = "room1",
                StartHour = 12  // Daytime for lighting
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Starting Room",
                    Description = "You are in the starting room.",
                    IsIlluminated = true,
                    IsInterior = false,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "norte", TargetRoomId = "room2" }
                    },
                    ObjectIds = new List<string> { "obj_sword" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room2",
                    Name = "North Room",
                    Description = "You are in the north room.",
                    IsIlluminated = true,
                    IsInterior = false,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "sur", TargetRoomId = "room1" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_sword",
                    Name = "espada oxidada",
                    Description = "Una vieja espada oxidada.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "obj_rock",
                    Name = "roca pesada",
                    Description = "Una roca muy pesada.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    /// <summary>
    /// Creates a mock SoundManager that does nothing (for testing without audio).
    /// </summary>
    private static SoundManager CreateMockSoundManager()
    {
        var sound = new SoundManager { SoundEnabled = false };
        return sound;
    }

    #region Movement Tests

    [Fact]
    public void ProcessCommand_GoNorth_MovesToCorrectRoom()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        Assert.Equal("room1", engine.State.CurrentRoomId);

        // Act
        engine.ProcessCommand("ir norte");

        // Assert
        Assert.Equal("room2", engine.State.CurrentRoomId);
        // Room description is shown in fixed panel, not in message
        Assert.Contains("north room", engine.DescribeCurrentRoom().ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_GoInvalidDirection_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("ir oeste");

        // Assert
        Assert.Equal("room1", engine.State.CurrentRoomId);  // Didn't move
        // RandomMessages.CannotGoThatWay returns various messages
        Assert.True(result.HasError);
    }

    [Fact]
    public void ProcessCommand_GoWithoutDirection_AsksForDirection()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("ir");

        // Assert - RandomMessages.WhereToGo returns various messages
        Assert.True(result.HasError);
    }

    #endregion

    #region Describe Tests

    [Fact]
    public void DescribeCurrentRoom_WithObjects_ListsVisibleObjects()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var description = engine.DescribeCurrentRoom();

        // Assert
        Assert.Contains("espada oxidada", description.ToLowerInvariant());
        Assert.Contains("ves aquí", description.ToLowerInvariant());
    }

    #endregion

    #region Inventory Tests

    [Fact]
    public void ProcessCommand_Take_AddsObjectToInventory()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        Assert.Empty(engine.State.InventoryObjectIds);

        // Act
        var result = engine.ProcessCommand("coger espada");

        // Assert
        Assert.Contains("obj_sword", engine.State.InventoryObjectIds);
        Assert.Contains("coges", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_TakeNonTakeable_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        // Add rock to room1
        state.Rooms.First(r => r.Id == "room1").ObjectIds.Add("obj_rock");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("coger roca");

        // Assert
        Assert.DoesNotContain("obj_rock", engine.State.InventoryObjectIds);
        // RandomMessages.CannotTakeThat - just verify it's an error
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ProcessCommand_TakeNonExistent_ReturnsNotFound()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("coger dragón");

        // Assert
        Assert.Empty(engine.State.InventoryObjectIds);
        // RandomMessages.ObjectNotFound - just verify it's an error
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void ProcessCommand_Drop_RemovesFromInventory()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        state.InventoryObjectIds.Add("obj_sword");
        state.Rooms.First(r => r.Id == "room1").ObjectIds.Remove("obj_sword");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("soltar espada");

        // Assert
        Assert.DoesNotContain("obj_sword", engine.State.InventoryObjectIds);
        Assert.Contains("sueltas", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_DropNotInInventory_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("soltar espada");

        // Assert - RandomMessages.NotCarryingThat returns various messages
        Assert.True(result.HasError);
    }

    [Fact]
    public void DescribeInventory_WhenEmpty_ReturnsEmptyMessage()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.DescribeInventory();

        // Assert - RandomMessages.InventoryEmpty returns various messages
        Assert.False(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void DescribeInventory_WithItems_ListsItems()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        state.InventoryObjectIds.Add("obj_sword");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.DescribeInventory();

        // Assert
        Assert.Contains("espada oxidada", result.ToLowerInvariant());
    }

    #endregion

    #region Help and Other Commands Tests

    [Fact]
    public void ProcessCommand_Help_FiresHelpRequestedEvent()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);
        var helpRequested = false;
        engine.HelpRequested += () => helpRequested = true;

        // Act
        var result = engine.ProcessCommand("ayuda");

        // Assert - El comando "ayuda" dispara el evento HelpRequested
        // y retorna CommandResult.Empty (la UI muestra la ventana de ayuda)
        Assert.True(helpRequested, "HelpRequested event should be fired");
        Assert.True(string.IsNullOrEmpty(result.Message), "Help command returns empty message (UI handles display)");
    }

    [Fact]
    public void ProcessCommand_Inventory_DescribesInventory()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("inventario");

        // Assert - RandomMessages.InventoryEmpty returns various messages
        Assert.False(string.IsNullOrEmpty(result.Message));
    }

    [Fact]
    public void ProcessCommand_UnknownCommand_ReturnsNotUnderstood()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("xyz123");

        // Assert - RandomMessages.UnknownCommand returns various messages
        Assert.True(result.HasError);
    }

    #endregion

    #region State Management Tests

    [Fact]
    public void ProcessCommand_IncrementsTurnCounter()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);
        var initialTurn = engine.State.TurnCounter;

        // Act
        engine.ProcessCommand("inventario");

        // Assert
        Assert.Equal(initialTurn + 1, engine.State.TurnCounter);
    }

    [Fact]
    public void CurrentRoom_ReturnsCorrectRoom()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act & Assert
        Assert.NotNull(engine.CurrentRoom);
        Assert.Equal("room1", engine.CurrentRoom!.Id);
        Assert.Equal("Starting Room", engine.CurrentRoom.Name);
    }

    [Fact]
    public void LoadState_UpdatesEngineState()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var newState = WorldLoader.CreateInitialState(world);
        newState.CurrentRoomId = "room2";

        // Act
        engine.LoadState(newState);

        // Assert
        Assert.Equal("room2", engine.State.CurrentRoomId);
        Assert.Equal("room2", engine.CurrentRoom?.Id);
    }

    #endregion

    #region Door Tests

    /// <summary>
    /// Creates a test world with a door between two rooms.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTestWorldWithDoor(bool doorIsOpen = true, bool doorIsLocked = false)
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_door_world",
                Title = "Test Door World",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala Oeste",
                    Description = "Estás en la sala oeste.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "este", TargetRoomId = "room2", DoorId = "door1" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room2",
                    Name = "Sala Este",
                    Description = "Estás en la sala este.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "oeste", TargetRoomId = "room1", DoorId = "door1" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "key1",
                    Name = "llave dorada",
                    Description = "Una llave dorada brillante.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1",
                    Type = ObjectType.Llave
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>
            {
                new Door
                {
                    Id = "door1",
                    Name = "puerta de madera",
                    Description = "Una puerta de madera.",
                    RoomIdA = "room1",
                    RoomIdB = "room2",
                    IsOpen = doorIsOpen,
                    IsLocked = doorIsLocked,
                    KeyObjectId = doorIsLocked ? "key1" : null
                }
            },
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void ProcessCommand_GoThroughOpenDoor_MovesToRoom()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: true);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        Assert.Equal("room1", engine.State.CurrentRoomId);

        // Act - Usamos "e" en lugar de "este" porque el Parser trata "este"
        // como demostrativos (este/esta/estos) y lo elimina del comando
        engine.ProcessCommand("e");

        // Assert
        Assert.Equal("room2", engine.State.CurrentRoomId);
        // Room description is shown in fixed panel, not in message
        Assert.Contains("sala este", engine.DescribeCurrentRoom().ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_GoThroughClosedDoor_Blocked()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: false);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        Assert.Equal("room1", engine.State.CurrentRoomId);

        // Act - Usamos "e" en lugar de "este"
        var result = engine.ProcessCommand("e");

        // Assert
        Assert.Equal("room1", engine.State.CurrentRoomId); // Didn't move
        // RandomMessages.ExitBlocked returns various messages about blocked exit
        Assert.True(result.HasError);
    }

    [Fact]
    public void ProcessCommand_OpenDoor_OpensClosedDoor()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: false);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.False(door.IsOpen);

        // Act - usamos "e" (dirección normalizada) en lugar de "este"
        var result = engine.ProcessCommand("abrir puerta e");

        // Assert
        Assert.True(door.IsOpen);
    }

    [Fact]
    public void ProcessCommand_CloseDoor_ClosesOpenDoor()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: true);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.True(door.IsOpen);

        // Act - usamos "e" (dirección normalizada) en lugar de "este"
        var result = engine.ProcessCommand("cerrar puerta e");

        // Assert
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void ProcessCommand_OpenLockedDoor_RequiresKey()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: false, doorIsLocked: true);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.True(door.IsLocked);

        // Act - try to open without key
        var result = engine.ProcessCommand("abrir puerta e");

        // Assert
        Assert.False(door.IsOpen);
        // RandomMessages.DoorIsLocked returns various messages
        Assert.True(result.HasError);
    }

    [Fact]
    public void ProcessCommand_OpenLockedDoorWithKey_Opens()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithDoor(doorIsOpen: false, doorIsLocked: true);
        // Add key to inventory
        state.InventoryObjectIds.Add("key1");
        state.Rooms.First(r => r.Id == "room1").ObjectIds.Remove("key1");

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var door = state.Doors.First(d => d.Id == "door1");
        Assert.True(door.IsLocked);
        Assert.False(door.IsOpen);

        // Act
        var result = engine.ProcessCommand("abrir puerta e");

        // Assert
        Assert.True(door.IsOpen);
    }

    #endregion

    #region MatchesName Fallback Tests (Noun Alias Fallback)

    [Fact]
    public void ProcessCommand_TakeWithNounAlias_FindsObjectByOriginalName()
    {
        // Arrange - object named "sable oxidado", but "sable" is aliased to "espada"
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // The object is named "espada oxidada" which contains "espada"
        // This test verifies the normalized name works
        var result = engine.ProcessCommand("coger espada");

        Assert.Contains("obj_sword", engine.State.InventoryObjectIds);
    }

    [Fact]
    public void ProcessCommand_TakeWithOriginalName_FindsObjectWhenAliasedNameFails()
    {
        // Arrange - create object with name that uses synonym
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_alias",
                Title = "Test Alias",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala",
                    Description = "Una sala.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string> { "obj_sable" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_sable",
                    Name = "sable oxidado",  // Uses "sable", not "espada"
                    Description = "Un viejo sable oxidado.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };
        var state = WorldLoader.CreateInitialState(world);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - "sable" is aliased to "espada", but object name is "sable oxidado"
        // The fallback to original name should find it
        var result = engine.ProcessCommand("coger sable");

        // Assert
        Assert.Contains("obj_sable", engine.State.InventoryObjectIds);
        Assert.Contains("coges", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_ExamineWithOriginalName_FindsObject()
    {
        // Arrange
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_examine",
                Title = "Test Examine",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala",
                    Description = "Una sala.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string> { "obj_monedas" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_monedas",
                    Name = "monedas de plata",  // "monedas" aliased to "oro"
                    Description = "Monedas brillantes de plata.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };
        var state = WorldLoader.CreateInitialState(world);
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - "monedas" aliased to "oro", but object is "monedas de plata"
        var result = engine.ProcessCommand("examinar monedas");

        // Assert - Should find via original name fallback
        Assert.Contains("plata", result.Message.ToLowerInvariant());
    }

    #endregion

    #region Container Tests

    /// <summary>
    /// Creates a test world with a container.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTestWorldWithContainer()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_container",
                Title = "Test Container",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala con cofre",
                    Description = "Una sala con un cofre.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string> { "obj_chest", "obj_gem" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_chest",
                    Name = "cofre de madera",
                    Description = "Un viejo cofre de madera.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "room1",
                    IsContainer = true,
                    IsOpenable = true,
                    IsOpen = true,
                    ContainedObjectIds = new List<string> { "obj_coin" }
                },
                new GameObject
                {
                    Id = "obj_coin",
                    Name = "moneda de oro",
                    Description = "Una brillante moneda de oro.",
                    CanTake = true,
                    Visible = true
                },
                new GameObject
                {
                    Id = "obj_gem",
                    Name = "gema roja",
                    Description = "Una gema roja brillante.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void ProcessCommand_LookIn_ShowsContainerContents()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Note: "ver en" is defined as alias for "look_in" but parser only takes first word as verb
        // So we use the English command directly for this test
        // Act
        var result = engine.ProcessCommand("look_in cofre");

        // Assert
        Assert.Contains("moneda", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_PutIn_MovesObjectToContainer()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        state.InventoryObjectIds.Add("obj_gem");
        state.Rooms.First(r => r.Id == "room1").ObjectIds.Remove("obj_gem");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var chest = state.Objects.First(o => o.Id == "obj_chest");
        Assert.DoesNotContain("obj_gem", chest.ContainedObjectIds);

        // Act
        var result = engine.ProcessCommand("meter gema en cofre");

        // Assert
        Assert.Contains("obj_gem", chest.ContainedObjectIds);
        Assert.DoesNotContain("obj_gem", engine.State.InventoryObjectIds);
    }

    [Fact]
    public void ProcessCommand_GetFrom_MovesObjectFromContainer()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var chest = state.Objects.First(o => o.Id == "obj_chest");
        Assert.Contains("obj_coin", chest.ContainedObjectIds);

        // Act
        var result = engine.ProcessCommand("sacar moneda de cofre");

        // Assert
        Assert.DoesNotContain("obj_coin", chest.ContainedObjectIds);
    }

    [Fact]
    public void ProcessCommand_OpenContainer_OpensContainer()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        chest.IsOpen = false;

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("abrir cofre");

        // Assert
        Assert.True(chest.IsOpen);
    }

    [Fact]
    public void ProcessCommand_CloseContainer_ClosesContainer()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        Assert.True(chest.IsOpen);

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("cerrar cofre");

        // Assert
        Assert.False(chest.IsOpen);
    }

    [Fact]
    public void ProcessCommand_TakeFromOpenContainer_Works()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithContainer();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - take object from inside open container
        var result = engine.ProcessCommand("coger moneda");

        // Assert
        Assert.Contains("obj_coin", engine.State.InventoryObjectIds);
    }

    #endregion

    #region Examine Tests

    [Fact]
    public void ProcessCommand_Examine_ReturnsObjectDescription()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("examinar espada");

        // Assert
        Assert.Contains("oxidada", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void ProcessCommand_ExamineNonExistent_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("examinar dragon");

        // Assert - RandomMessages.ObjectNotFound returns various messages
        Assert.True(result.HasError);
    }

    [Fact]
    public void ProcessCommand_ExamineInInventory_Works()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        state.InventoryObjectIds.Add("obj_sword");
        state.Rooms.First(r => r.Id == "room1").ObjectIds.Remove("obj_sword");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("examinar espada");

        // Assert
        Assert.Contains("oxidada", result.Message.ToLowerInvariant());
    }

    #endregion

    #region Use Command Tests

    [Fact]
    public void ProcessCommand_Use_WithNoScript_ReturnsDefaultMessage()
    {
        // Arrange
        var (world, state) = CreateTestWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("usar espada");

        // Assert - Should indicate there's no special use
        Assert.NotNull(result.Message);
        Assert.True(result.Message.Length > 0);
    }

    #endregion

    #region Combat Integration Tests

    /// <summary>
    /// Creates a test world with combat enabled and an NPC to fight.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTestWorldWithCombat()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_combat",
                Title = "Test Combat",
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
                    IsInterior = false,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string> { "npc_goblin" }
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "npc_goblin",
                    Name = "Goblin feroz",
                    Description = "Un pequeño goblin verde con ojos maliciosos.",
                    RoomId = "room1",
                    Visible = true,
                    Money = 10,
                    Stats = new CombatStats
                    {
                        Strength = 5,
                        Dexterity = 8,
                        Intelligence = 3,
                        MaxHealth = 15,
                        CurrentHealth = 15
                    }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void ProcessCommand_Attack_FiresCombatStartedEvent()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        engine.ProcessCommand("atacar goblin");

        // Assert - Event should have been fired with the correct NPC ID
        Assert.NotNull(capturedNpcId);
        Assert.Equal("npc_goblin", capturedNpcId);
    }

    [Fact]
    public void ProcessCommand_Attack_WithCombatDisabled_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        world.Game.CombatEnabled = false; // Disable combat
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        var result = engine.ProcessCommand("atacar goblin");

        // Assert - Should return error and NOT fire event
        Assert.True(result.HasError);
        Assert.Contains("combate no está activo", result.Message.ToLowerInvariant());
        Assert.Null(capturedNpcId);
    }

    [Fact]
    public void ProcessCommand_Attack_NpcNotInRoom_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        var result = engine.ProcessCommand("atacar dragon");

        // Assert - Should return error and NOT fire event
        Assert.True(result.HasError);
        Assert.Contains("no ves", result.Message.ToLowerInvariant());
        Assert.Null(capturedNpcId);
    }

    [Fact]
    public void ProcessCommand_Attack_NpcIsCorpse_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        // Make the NPC a corpse
        state.Npcs.First(n => n.Id == "npc_goblin").IsCorpse = true;
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        var result = engine.ProcessCommand("atacar goblin");

        // Assert - Should return error about NPC being dead
        Assert.True(result.HasError);
        Assert.Contains("muerto", result.Message.ToLowerInvariant());
        Assert.Null(capturedNpcId);
    }

    [Fact]
    public void ProcessCommand_Attack_NoTarget_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        var result = engine.ProcessCommand("atacar");

        // Assert - Should ask who to attack
        Assert.True(result.HasError);
        Assert.Contains("quién", result.Message.ToLowerInvariant());
        Assert.Null(capturedNpcId);
    }

    [Fact]
    public void ProcessCommand_Attack_InvisibleNpc_ReturnsError()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        // Make the NPC invisible
        state.Npcs.First(n => n.Id == "npc_goblin").Visible = false;
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? capturedNpcId = null;
        engine.CombatStarted += npcId => capturedNpcId = npcId;

        // Act
        var result = engine.ProcessCommand("atacar goblin");

        // Assert - Should not find the invisible NPC
        Assert.True(result.HasError);
        Assert.Contains("no ves", result.Message.ToLowerInvariant());
        Assert.Null(capturedNpcId);
    }

    [Fact]
    public void CombatStarted_Event_IsNotNull()
    {
        // Arrange
        var (world, state) = CreateTestWorldWithCombat();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act & Assert - Verify we can subscribe to the event without issues
        bool eventFired = false;
        engine.CombatStarted += _ => eventFired = true;
        engine.ProcessCommand("atacar goblin");

        Assert.True(eventFired, "CombatStarted event should fire when attacking a valid NPC");
    }

    #endregion
}
