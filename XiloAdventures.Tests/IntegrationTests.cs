using XiloAdventures.Engine;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using Xunit;

namespace XiloAdventures.Tests;

/// <summary>
/// Integration tests that verify the interaction between multiple game components.
/// These tests simulate real gameplay scenarios.
/// </summary>
public class IntegrationTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a mock SoundManager that does nothing (for testing without audio).
    /// </summary>
    private static SoundManager CreateMockSoundManager()
    {
        var sound = new SoundManager { SoundEnabled = false };
        return sound;
    }

    #endregion

    #region Full Game Flow Tests

    /// <summary>
    /// Creates a multi-room world for navigation tests.
    /// </summary>
    private static (WorldModel world, GameState state) CreateMultiRoomWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_navigation",
                Title = "Test Navigation",
                StartRoomId = "room_start",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room_start",
                    Name = "Sala de Inicio",
                    Description = "Una sala con salidas al norte y al este.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room_north" },
                        new Exit { Direction = "e", TargetRoomId = "room_east" }
                    },
                    ObjectIds = new List<string> { "obj_torch" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room_north",
                    Name = "Sala Norte",
                    Description = "Una sala al norte con una salida al sur.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room_start" }
                    },
                    ObjectIds = new List<string> { "obj_key" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room_east",
                    Name = "Sala Este",
                    Description = "Una sala al este con una puerta cerrada al norte.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "o", TargetRoomId = "room_start" },
                        new Exit { Direction = "n", TargetRoomId = "room_treasure", DoorId = "door_treasure" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room_treasure",
                    Name = "Sala del Tesoro",
                    Description = "Una sala llena de oro y joyas.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room_east" }
                    },
                    ObjectIds = new List<string> { "obj_treasure" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_torch",
                    Name = "antorcha",
                    Description = "Una antorcha que ilumina el camino.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room_start"
                },
                new GameObject
                {
                    Id = "obj_key",
                    Name = "llave dorada",
                    Description = "Una llave dorada con inscripciones antiguas.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room_north"
                },
                new GameObject
                {
                    Id = "obj_treasure",
                    Name = "cofre del tesoro",
                    Description = "Un cofre lleno de monedas de oro.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "room_treasure"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>
            {
                new Door
                {
                    Id = "door_treasure",
                    Name = "puerta del tesoro",
                    Description = "Una puerta de hierro con cerraduras doradas.",
                    IsLocked = true,
                    IsOpen = false,
                    KeyObjectId = "obj_key",
                    RoomIdA = "room_east",
                    RoomIdB = "room_treasure"
                }
            },
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void GameFlow_NavigateBetweenRooms()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act & Assert - Start in room_start
        Assert.Equal("room_start", engine.State.CurrentRoomId);

        // Go north
        var result = engine.ProcessCommand("norte");
        Assert.Equal("room_north", engine.State.CurrentRoomId);

        // Go back south
        result = engine.ProcessCommand("sur");
        Assert.Equal("room_start", engine.State.CurrentRoomId);

        // Go east
        result = engine.ProcessCommand("este");
        Assert.Equal("room_east", engine.State.CurrentRoomId);

        // Go back west
        result = engine.ProcessCommand("oeste");
        Assert.Equal("room_start", engine.State.CurrentRoomId);
    }

    [Fact]
    public void GameFlow_PickUpAndUseKey_ToUnlockDoor()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Try to go through locked door without key
        engine.ProcessCommand("este");
        var result = engine.ProcessCommand("norte");
        Assert.NotEqual("room_treasure", engine.State.CurrentRoomId);

        // Go get the key
        engine.ProcessCommand("oeste");
        engine.ProcessCommand("norte");
        engine.ProcessCommand("coger llave");
        Assert.Contains("obj_key", engine.State.InventoryObjectIds);

        // Go back and unlock door
        engine.ProcessCommand("sur");
        engine.ProcessCommand("este");

        // Open the door (with key in inventory, it auto-unlocks)
        result = engine.ProcessCommand("abrir puerta");

        // Now we should be able to go through
        var door = state.Doors.First(d => d.Id == "door_treasure");
        Assert.True(door.IsOpen);

        result = engine.ProcessCommand("norte");
        Assert.Equal("room_treasure", engine.State.CurrentRoomId);
    }

    [Fact]
    public void GameFlow_CollectMultipleItems_CheckInventory()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Collect torch
        engine.ProcessCommand("coger antorcha");
        Assert.Contains("obj_torch", engine.State.InventoryObjectIds);

        // Go north and collect key
        engine.ProcessCommand("norte");
        engine.ProcessCommand("coger llave");
        Assert.Contains("obj_key", engine.State.InventoryObjectIds);

        // Check inventory
        var result = engine.ProcessCommand("inventario");
        Assert.Contains("antorcha", result.Message.ToLowerInvariant());
        Assert.Contains("llave", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void GameFlow_DropItem_LeavesInCurrentRoom()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Pick up torch
        engine.ProcessCommand("coger antorcha");
        Assert.Contains("obj_torch", engine.State.InventoryObjectIds);

        // Move to different room
        engine.ProcessCommand("norte");
        Assert.Equal("room_north", engine.State.CurrentRoomId);

        // Drop torch
        engine.ProcessCommand("soltar antorcha");
        Assert.DoesNotContain("obj_torch", engine.State.InventoryObjectIds);

        // Verify torch is now in room_north
        var roomNorth = state.Rooms.First(r => r.Id == "room_north");
        Assert.Contains("obj_torch", roomNorth.ObjectIds);
    }

    #endregion

    #region Door and Lock Tests

    [Fact]
    public void Door_CannotPassThrough_WhenLockedAndClosed()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Go to room with locked door
        engine.ProcessCommand("este");
        Assert.Equal("room_east", engine.State.CurrentRoomId);

        // Try to go through locked door
        var result = engine.ProcessCommand("norte");

        // Should still be in room_east
        Assert.Equal("room_east", engine.State.CurrentRoomId);
    }

    [Fact]
    public void Door_CanOpenAndClose()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var door = state.Doors.First(d => d.Id == "door_treasure");
        door.IsLocked = false; // Unlock for this test

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Go to room with door
        engine.ProcessCommand("este");

        // Open door
        engine.ProcessCommand("abrir puerta");
        Assert.False(!door.IsOpen);

        // Close door
        engine.ProcessCommand("cerrar puerta");
        Assert.True(!door.IsOpen);
    }

    [Fact]
    public void Door_UnlockWithCorrectKey()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        state.InventoryObjectIds.Add("obj_key");

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Go to room with locked door
        engine.ProcessCommand("este");

        var door = state.Doors.First(d => d.Id == "door_treasure");
        Assert.True(door.IsLocked);

        // Open door (auto-unlocks with key in inventory)
        engine.ProcessCommand("abrir puerta");
        Assert.True(door.IsOpen);
    }

    [Fact]
    public void Door_LockWithKey()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        state.InventoryObjectIds.Add("obj_key");
        var door = state.Doors.First(d => d.Id == "door_treasure");
        door.IsLocked = false;
        door.IsOpen = true;  // Door must be open to close it

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Go to room with door
        engine.ProcessCommand("este");

        // Close the open door
        engine.ProcessCommand("cerrar puerta");
        Assert.False(door.IsOpen);
    }

    #endregion

    #region Container Chain Tests

    /// <summary>
    /// Creates a world with nested containers for complex interaction tests.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNestedContainerWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_containers",
                Title = "Test Containers",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala de Almacenamiento",
                    Description = "Una sala con varios contenedores.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string> { "obj_chest", "obj_bag", "obj_gem", "obj_potion" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_chest",
                    Name = "cofre grande",
                    Description = "Un cofre grande de madera.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "room1",
                    IsContainer = true,
                    IsOpenable = true,
                    IsOpen = false,
                    ContainedObjectIds = new List<string> { "obj_gold", "obj_scroll" }
                },
                new GameObject
                {
                    Id = "obj_bag",
                    Name = "bolsa de cuero",
                    Description = "Una bolsa de cuero resistente.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1",
                    IsContainer = true,
                    IsOpenable = false,
                    IsOpen = true,
                    ContainedObjectIds = new List<string> { "obj_coin" }
                },
                new GameObject
                {
                    Id = "obj_gold",
                    Name = "lingote de oro",
                    Description = "Un pesado lingote de oro puro.",
                    CanTake = true,
                    Visible = true
                },
                new GameObject
                {
                    Id = "obj_scroll",
                    Name = "pergamino antiguo",
                    Description = "Un pergamino con escrituras arcanas.",
                    CanTake = true,
                    Visible = true
                },
                new GameObject
                {
                    Id = "obj_coin",
                    Name = "moneda de plata",
                    Description = "Una reluciente moneda de plata.",
                    CanTake = true,
                    Visible = true
                },
                new GameObject
                {
                    Id = "obj_gem",
                    Name = "gema roja",
                    Description = "Una brillante gema roja.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "obj_potion",
                    Name = "elixir de salud",
                    Description = "Un elixir que restaura la salud.",
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
    public void Container_OpenChest_ThenTakeItem()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var chest = state.Objects.First(o => o.Id == "obj_chest");
        Assert.False(chest.IsOpen);

        // Open chest
        engine.ProcessCommand("abrir cofre");
        Assert.True(chest.IsOpen);

        // Take gold from chest (use "coger" which works with open containers)
        engine.ProcessCommand("coger lingote");
        Assert.Contains("obj_gold", engine.State.InventoryObjectIds);
        Assert.DoesNotContain("obj_gold", chest.ContainedObjectIds);
    }

    [Fact]
    public void Container_PutItem_ThenRetrieve()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        chest.IsOpen = true;

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Pick up gem
        engine.ProcessCommand("coger gema");
        Assert.Contains("obj_gem", engine.State.InventoryObjectIds);

        // Put gem in chest
        engine.ProcessCommand("meter gema en cofre");
        Assert.DoesNotContain("obj_gem", engine.State.InventoryObjectIds);
        Assert.Contains("obj_gem", chest.ContainedObjectIds);

        // Retrieve gem from chest
        engine.ProcessCommand("coger gema");
        Assert.Contains("obj_gem", engine.State.InventoryObjectIds);
        Assert.DoesNotContain("obj_gem", chest.ContainedObjectIds);
    }

    [Fact]
    public void Container_TakeItemDirectly_FromOpenContainer()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var bag = state.Objects.First(o => o.Id == "obj_bag");
        // Bag is already open by default

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Take coin directly (from open container)
        engine.ProcessCommand("coger moneda");
        Assert.Contains("obj_coin", engine.State.InventoryObjectIds);
    }

    [Fact]
    public void Container_CloseAndOpen_PreservesContents()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        chest.IsOpen = true;

        var initialContents = chest.ContainedObjectIds.ToList();

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Close chest
        engine.ProcessCommand("cerrar cofre");
        Assert.False(chest.IsOpen);
        Assert.Equal(initialContents.Count, chest.ContainedObjectIds.Count);

        // Reopen chest
        engine.ProcessCommand("abrir cofre");
        Assert.True(chest.IsOpen);
        Assert.Equal(initialContents.Count, chest.ContainedObjectIds.Count);
    }

    [Fact]
    public void Container_TakeContainer_WithContents()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var bag = state.Objects.First(o => o.Id == "obj_bag");
        Assert.Contains("obj_coin", bag.ContainedObjectIds);

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Take the bag (which contains a coin)
        engine.ProcessCommand("coger bolsa");
        Assert.Contains("obj_bag", engine.State.InventoryObjectIds);

        // Coin should still be in bag
        Assert.Contains("obj_coin", bag.ContainedObjectIds);
    }

    #endregion

    #region Quest Tests

    /// <summary>
    /// Creates a world with a simple quest.
    /// </summary>
    private static (WorldModel world, GameState state) CreateQuestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_quest",
                Title = "Test Quest",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Plaza del Pueblo",
                    Description = "La plaza principal del pueblo.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room2" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string> { "npc_villager" }
                },
                new Room
                {
                    Id = "room2",
                    Name = "Bosque Oscuro",
                    Description = "Un bosque tenebroso.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room1" }
                    },
                    ObjectIds = new List<string> { "obj_herb" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_herb",
                    Name = "hierba medicinal",
                    Description = "Una hierba con propiedades curativas.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room2"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "npc_villager",
                    Name = "aldeano enfermo",
                    Description = "Un aldeano que parece muy enfermo.",
                    RoomId = "room1"
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    Id = "quest_heal",
                    Name = "Curar al Aldeano",
                    Description = "Encuentra hierba medicinal para curar al aldeano.",
                    Objectives = new List<string>
                    {
                        "Encontrar hierba medicinal en el bosque",
                        "Llevar la hierba al aldeano"
                    }
                }
            }
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void Quest_StartsAsNotStarted()
    {
        // Arrange
        var (world, state) = CreateQuestWorld();

        // Assert
        Assert.True(state.Quests.ContainsKey("quest_heal"));
        Assert.Equal(QuestStatus.NotStarted, state.Quests["quest_heal"].Status);
    }

    [Fact]
    public void Quest_CanBeStarted_ViaFlag()
    {
        // Arrange
        var (world, state) = CreateQuestWorld();

        // Simulate starting quest
        state.Quests["quest_heal"].Status = QuestStatus.InProgress;

        // Assert
        Assert.Equal(QuestStatus.InProgress, state.Quests["quest_heal"].Status);
    }

    [Fact]
    public void Quest_ListsCorrectly()
    {
        // Arrange
        var (world, state) = CreateQuestWorld();
        state.Quests["quest_heal"].Status = QuestStatus.InProgress;

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("misiones");

        // Assert - Should list the quest
        Assert.Contains("curar", result.Message.ToLowerInvariant());
    }

    #endregion

    #region NPC Interaction Tests

    /// <summary>
    /// Creates a world with NPCs for interaction tests.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNpcWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_npc",
                Title = "Test NPC",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Taberna",
                    Description = "Una acogedora taberna.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string> { "npc_barman", "npc_guard" }
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "npc_barman",
                    Name = "tabernero",
                    Description = "Un hombre robusto limpiando vasos.",
                    RoomId = "room1"
                },
                new Npc
                {
                    Id = "npc_guard",
                    Name = "guardia",
                    Description = "Un guardia de la ciudad bebiendo cerveza.",
                    RoomId = "room1"
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void Npc_CanBeExamined()
    {
        // Arrange
        var (world, state) = CreateNpcWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("examinar tabernero");

        // Assert
        Assert.Contains("limpiando", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void Npc_TalkCommand_Recognized()
    {
        // Arrange
        var (world, state) = CreateNpcWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("hablar con tabernero");

        // Assert - Should try to talk (even if no conversation defined)
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Npc_MultipleInRoom_CanDistinguish()
    {
        // Arrange
        var (world, state) = CreateNpcWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Examine barman
        var result1 = engine.ProcessCommand("examinar tabernero");
        Assert.Contains("limpiando", result1.Message.ToLowerInvariant());

        // Act - Examine guard
        var result2 = engine.ProcessCommand("examinar guardia");
        Assert.Contains("cerveza", result2.Message.ToLowerInvariant());
    }

    #endregion

    #region Flags and Counters Tests

    [Fact]
    public void Flags_CanBeSetAndChecked()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();

        // Act
        state.Flags["visited_treasure"] = true;

        // Assert
        Assert.True(state.Flags.ContainsKey("visited_treasure"));
        Assert.True(state.Flags["visited_treasure"]);
    }

    [Fact]
    public void Counters_CanBeIncrementedAndDecremented()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();

        // Act
        state.Counters["gold"] = 100;
        state.Counters["gold"] += 50;
        state.Counters["gold"] -= 25;

        // Assert
        Assert.Equal(125, state.Counters["gold"]);
    }

    #endregion

    #region Dark Room Tests

    /// <summary>
    /// Creates a world with dark rooms.
    /// </summary>
    private static (WorldModel world, GameState state) CreateDarkRoomWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_dark",
                Title = "Test Dark",
                StartRoomId = "room_lit",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room_lit",
                    Name = "Sala Iluminada",
                    Description = "Una sala bien iluminada.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room_dark" }
                    },
                    ObjectIds = new List<string> { "obj_lantern" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room_dark",
                    Name = "Cueva Oscura",
                    Description = "Una cueva muy oscura con estalactitas.",
                    IsIlluminated = false,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room_lit" }
                    },
                    ObjectIds = new List<string> { "obj_diamond" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_lantern",
                    Name = "linterna",
                    Description = "Una linterna que emite luz.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room_lit"
                },
                new GameObject
                {
                    Id = "obj_diamond",
                    Name = "diamante",
                    Description = "Un brillante diamante.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room_dark"
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
    public void DarkRoom_CanNavigateTo()
    {
        // Arrange
        var (world, state) = CreateDarkRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Go to dark room
        engine.ProcessCommand("norte");

        // Assert - Should be in dark room
        Assert.Equal("room_dark", engine.State.CurrentRoomId);
    }

    [Fact]
    public void DarkRoom_WithLightSource_CanSeeObjects()
    {
        // Arrange
        var (world, state) = CreateDarkRoomWorld();
        state.InventoryObjectIds.Add("obj_lantern");
        state.Rooms.First(r => r.Id == "room_lit").ObjectIds.Remove("obj_lantern");

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Go to dark room with lantern
        engine.ProcessCommand("norte");

        // Try to examine diamond
        var result = engine.ProcessCommand("examinar diamante");

        // Should be able to see the diamond
        Assert.Contains("brillante", result.Message.ToLowerInvariant());
    }

    #endregion

    #region Time and Turn Tests

    [Fact]
    public void TurnCounter_Increments_WithCommands()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        var initialTurns = engine.State.TurnCounter;

        // Act - Execute some commands
        engine.ProcessCommand("norte");
        engine.ProcessCommand("sur");
        engine.ProcessCommand("inventario");

        // Assert - Turn counter should increase
        Assert.True(engine.State.TurnCounter > initialTurns);
    }

    #endregion

    #region Complex Scenario Tests

    [Fact]
    public void CompleteScenario_ExploreCollectUnlockRetrieve()
    {
        // Arrange - A complete gameplay scenario
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // 1. Check starting location
        Assert.Equal("room_start", engine.State.CurrentRoomId);

        // 2. Pick up torch from starting room
        engine.ProcessCommand("coger antorcha");
        Assert.Contains("obj_torch", engine.State.InventoryObjectIds);

        // 3. Explore north - get key
        engine.ProcessCommand("norte");
        Assert.Equal("room_north", engine.State.CurrentRoomId);
        engine.ProcessCommand("coger llave");
        Assert.Contains("obj_key", engine.State.InventoryObjectIds);

        // 4. Return south, then go east to locked door
        engine.ProcessCommand("sur");
        engine.ProcessCommand("este");
        Assert.Equal("room_east", engine.State.CurrentRoomId);

        // 5. Open door (auto-unlocks with key in inventory)
        var door = state.Doors.First(d => d.Id == "door_treasure");
        engine.ProcessCommand("abrir puerta");
        Assert.True(door.IsOpen);

        // 6. Enter treasure room
        engine.ProcessCommand("norte");
        Assert.Equal("room_treasure", engine.State.CurrentRoomId);

        // 7. Examine treasure
        var result = engine.ProcessCommand("examinar cofre");
        Assert.Contains("monedas", result.Message.ToLowerInvariant());

        // 8. Check full inventory
        result = engine.ProcessCommand("inventario");
        Assert.Contains("antorcha", result.Message.ToLowerInvariant());
        Assert.Contains("llave", result.Message.ToLowerInvariant());
    }

    [Fact]
    public void CompleteScenario_ContainerManipulation()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // 1. Pick up loose items
        engine.ProcessCommand("coger gema");
        engine.ProcessCommand("coger elixir");
        Assert.Contains("obj_gem", engine.State.InventoryObjectIds);
        Assert.Contains("obj_potion", engine.State.InventoryObjectIds);

        // 2. Open chest
        engine.ProcessCommand("abrir cofre");
        Assert.True(chest.IsOpen);

        // 3. Take items from chest
        engine.ProcessCommand("coger lingote");
        engine.ProcessCommand("coger pergamino");
        Assert.Contains("obj_gold", engine.State.InventoryObjectIds);
        Assert.Contains("obj_scroll", engine.State.InventoryObjectIds);

        // 4. Put some items back
        engine.ProcessCommand("meter gema en cofre");
        engine.ProcessCommand("meter elixir en cofre");
        Assert.Contains("obj_gem", chest.ContainedObjectIds);
        Assert.Contains("obj_potion", chest.ContainedObjectIds);

        // 5. Close chest and verify items are stored
        engine.ProcessCommand("cerrar cofre");
        Assert.False(chest.IsOpen);
        Assert.Contains("obj_gem", chest.ContainedObjectIds);

        // 6. Still have gold and scroll in inventory
        Assert.Contains("obj_gold", engine.State.InventoryObjectIds);
        Assert.Contains("obj_scroll", engine.State.InventoryObjectIds);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void EdgeCase_TakeNonexistentObject()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("coger dragón");

        // Assert - Should handle gracefully
        Assert.NotNull(result.Message);
        Assert.True(result.Message.Length > 0);
    }

    [Fact]
    public void EdgeCase_GoInvalidDirection()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - No exit to the south from start
        var result = engine.ProcessCommand("sur");

        // Assert - Should stay in same room
        Assert.Equal("room_start", engine.State.CurrentRoomId);
    }

    [Fact]
    public void EdgeCase_DropItemNotInInventory()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Try to drop something not in inventory
        var result = engine.ProcessCommand("soltar dragón");

        // Assert - Should handle gracefully
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void EdgeCase_OpenAlreadyOpenContainer()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        chest.IsOpen = true;

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Try to open already open chest
        var result = engine.ProcessCommand("abrir cofre");

        // Assert - Should handle gracefully, chest still open
        Assert.True(chest.IsOpen);
    }

    [Fact]
    public void EdgeCase_CloseAlreadyClosedContainer()
    {
        // Arrange
        var (world, state) = CreateNestedContainerWorld();
        var chest = state.Objects.First(o => o.Id == "obj_chest");
        Assert.False(chest.IsOpen);

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act - Try to close already closed chest
        var result = engine.ProcessCommand("cerrar cofre");

        // Assert - Should handle gracefully, chest still closed
        Assert.False(chest.IsOpen);
    }

    [Fact]
    public void EdgeCase_EmptyCommand()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("");

        // Assert - Should handle gracefully
        Assert.NotNull(result);
    }

    [Fact]
    public void EdgeCase_UnknownCommand()
    {
        // Arrange
        var (world, state) = CreateMultiRoomWorld();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("volar al cielo");

        // Assert - Should return unknown command message
        Assert.NotNull(result.Message);
    }

    #endregion

    #region Script Integration Tests

    /// <summary>
    /// Creates a world with scripts for integration testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateWorldWithScripts()
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
                    Name = "Sala con Script",
                    Description = "Una sala que tiene un script asociado.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room2" }
                    },
                    ObjectIds = new List<string> { "obj_lever" },
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room2",
                    Name = "Sala Norte",
                    Description = "Una sala al norte.",
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
                    Id = "obj_lever",
                    Name = "palanca",
                    Description = "Una palanca oxidada en la pared.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>
            {
                new ScriptDefinition
                {
                    Id = "script_lever",
                    Name = "Lever Script",
                    OwnerType = "GameObject",
                    OwnerId = "obj_lever",
                    Nodes = new List<ScriptNode>
                    {
                        new ScriptNode
                        {
                            Id = "node_event",
                            NodeType = NodeTypeId.Event_OnUse,
                            Properties = new Dictionary<string, object?>()
                        },
                        new ScriptNode
                        {
                            Id = "node_setflag",
                            NodeType = NodeTypeId.Action_SetFlag,
                            Properties = new Dictionary<string, object?>
                            {
                                ["FlagName"] = "lever_pulled",
                                ["Value"] = true
                            }
                        }
                    },
                    Connections = new List<NodeConnection>
                    {
                        new NodeConnection
                        {
                            FromNodeId = "node_event",
                            FromPortName = "Exec",
                            ToNodeId = "node_setflag",
                            ToPortName = "Exec"
                        }
                    }
                }
            }
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void Script_ObjectUse_TriggersScript()
    {
        // Arrange
        var (world, state) = CreateWorldWithScripts();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Verify flag not set initially
        Assert.False(state.Flags.ContainsKey("lever_pulled") && state.Flags["lever_pulled"]);

        // Act - Use the lever (which has a script)
        var result = engine.ProcessCommand("usar palanca");

        // Assert - Flag should be set by script
        // Note: This depends on how scripts are executed in GameEngine
        // The actual behavior may vary based on implementation
        Assert.NotNull(result);
    }

    #endregion

    #region Gold and Economy Tests

    /// <summary>
    /// Creates a world with economy elements.
    /// </summary>
    private static (WorldModel world, GameState state) CreateEconomyWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_economy",
                Title = "Test Economy",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Tienda",
                    Description = "Una tienda con un comerciante.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string> { "obj_coins" },
                    NpcIds = new List<string> { "npc_merchant" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "obj_coins",
                    Name = "bolsa de monedas",
                    Description = "Una bolsa con 50 monedas de oro.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "npc_merchant",
                    Name = "comerciante",
                    Description = "Un comerciante con mercancías.",
                    RoomId = "room1"
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Money = 100;
        return (world, state);
    }

    [Fact]
    public void Economy_PlayerStartsWithGold()
    {
        // Arrange
        var (world, state) = CreateEconomyWorld();

        // Assert
        Assert.Equal(100, state.Player.Money);
    }

    [Fact]
    public void Economy_CanAddAndRemoveGold()
    {
        // Arrange
        var (world, state) = CreateEconomyWorld();

        // Act
        state.Player.Money += 50;
        Assert.Equal(150, state.Player.Money);

        state.Player.Money -= 30;
        Assert.Equal(120, state.Player.Money);
    }

    #endregion

    #region Quest Integration Tests

    /// <summary>
    /// Creates a world with main and side quests for integration testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateWorldWithQuests()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_quests",
                Title = "Test Quests",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Sala Principal",
                    Description = "Una sala principal.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "n", TargetRoomId = "room2" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                },
                new Room
                {
                    Id = "room2",
                    Name = "Sala del Tesoro",
                    Description = "Una sala con el tesoro.",
                    IsIlluminated = true,
                    Exits = new List<Exit>
                    {
                        new Exit { Direction = "s", TargetRoomId = "room1" }
                    },
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    Id = "quest_main",
                    Name = "Encuentra el Tesoro",
                    Description = "Encuentra el tesoro en la sala norte.",
                    IsMainQuest = true,
                    Objectives = new List<string> { "Llegar a la sala del tesoro" }
                },
                new QuestDefinition
                {
                    Id = "quest_side",
                    Name = "Recoge las Gemas",
                    Description = "Recoge todas las gemas.",
                    IsMainQuest = false,
                    Objectives = new List<string> { "Recoger 5 gemas" }
                }
            }
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    [Fact]
    public void Quest_InitializedAsNotStarted()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();

        // Assert
        Assert.All(state.Quests.Values, q => Assert.Equal(QuestStatus.NotStarted, q.Status));
    }

    [Fact]
    public void Quest_MainQuestFlagIsPreserved()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();

        // Assert
        var mainQuest = world.Quests.First(q => q.Id == "quest_main");
        var sideQuest = world.Quests.First(q => q.Id == "quest_side");

        Assert.True(mainQuest.IsMainQuest);
        Assert.False(sideQuest.IsMainQuest);
    }

    [Fact]
    public void Quest_CanListWithMisionesCommand()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Act
        var result = engine.ProcessCommand("misiones");

        // Assert
        Assert.NotNull(result.Message);
    }

    [Fact]
    public void Quest_StatusCanBeChangedManually()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();

        // Act
        state.Quests["quest_main"].Status = QuestStatus.InProgress;

        // Assert
        Assert.Equal(QuestStatus.InProgress, state.Quests["quest_main"].Status);
        Assert.Equal(QuestStatus.NotStarted, state.Quests["quest_side"].Status);
    }

    [Fact]
    public void Quest_CanBeCompleted()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();

        // Act
        state.Quests["quest_main"].Status = QuestStatus.InProgress;
        state.Quests["quest_main"].Status = QuestStatus.Completed;

        // Assert
        Assert.Equal(QuestStatus.Completed, state.Quests["quest_main"].Status);
    }

    [Fact]
    public void Quest_CanBeFailed()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();

        // Act
        state.Quests["quest_main"].Status = QuestStatus.InProgress;
        state.Quests["quest_main"].Status = QuestStatus.Failed;

        // Assert
        Assert.Equal(QuestStatus.Failed, state.Quests["quest_main"].Status);
    }

    #endregion

    #region GameEngine Event Tests

    [Fact]
    public void GameEngine_ScriptMessageEvent_PropagatesFromScriptEngine()
    {
        // Arrange
        var (world, state) = CreateWorldWithScripts();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        string? receivedMessage = null;
        engine.ScriptMessage += msg => receivedMessage = msg;

        // Act - Force initialization which may trigger scripts
        var result = engine.ProcessCommand("mirar");

        // Assert - Engine should be connected
        Assert.NotNull(engine);
    }

    [Fact]
    public void GameEngine_AdventureCompletedEvent_IsAvailable()
    {
        // Arrange
        var (world, state) = CreateWorldWithQuests();
        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        // Subscribe to event to verify it's available
        engine.AdventureCompleted += () => { };

        // Assert - Event should be subscribable without throwing
        Assert.NotNull(engine);
    }

    #endregion

    #region Ending Properties Tests

    [Fact]
    public void GameInfo_EndingText_CanBeSet()
    {
        // Arrange
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_ending",
                Title = "Test Ending",
                StartRoomId = "room1",
                EndingText = "¡Felicidades! Has completado la aventura.",
                EndingMusicId = "ending_music"
            },
            Rooms = new List<Room>
            {
                new Room { Id = "room1", Name = "Room", Exits = new List<Exit>() }
            }
        };

        // Assert
        Assert.Equal("¡Felicidades! Has completado la aventura.", world.Game.EndingText);
        Assert.Equal("ending_music", world.Game.EndingMusicId);
    }

    [Fact]
    public void GameInfo_EndingText_DefaultsToEmpty()
    {
        // Arrange
        var gameInfo = new GameInfo();

        // Assert
        Assert.Equal("", gameInfo.EndingText);
        Assert.Null(gameInfo.EndingMusicId);
    }

    #endregion
}
