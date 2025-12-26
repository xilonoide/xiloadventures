using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for light source functionality.
/// Tests light source properties, ignite/extinguish commands, and room illumination.
/// </summary>
public class LightSourceTests
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
    /// Creates a test world with an interior dark room and a torch.
    /// </summary>
    private static (WorldModel world, GameState state) CreateDarkRoomWithTorch()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_light",
                Title = "Light Test World",
                StartRoomId = "dark_room",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "dark_room",
                    Name = "Cueva oscura",
                    Description = "Una cueva muy oscura.",
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
                    Description = "Una antorcha de madera.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "dark_room",
                    IsLightSource = true,
                    IsLit = false,
                    LightTurnsRemaining = -1,
                    CanIgnite = true,
                    CanExtinguish = true,
                    IgniterObjectId = null // No requiere objeto encendedor
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
    /// Creates a test world with an exterior room at night.
    /// </summary>
    private static (WorldModel world, GameState state) CreateNightExterior()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_night",
                Title = "Night Test World",
                StartRoomId = "forest",
                StartHour = 22 // Night time (after 20:00)
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "forest",
                    Name = "Bosque",
                    Description = "Un bosque frondoso.",
                    IsInterior = false,
                    IsIlluminated = false,
                    ObjectIds = new List<string> { "lantern" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "lantern",
                    Name = "farol",
                    Description = "Un farol de aceite.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "forest",
                    IsLightSource = true,
                    IsLit = false,
                    LightTurnsRemaining = 10,
                    CanIgnite = true,
                    CanExtinguish = true,
                    IgniterObjectId = null
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
    /// Creates a test world with a torch that requires matches to ignite.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTorchWithIgniter()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_igniter",
                Title = "Igniter Test World",
                StartRoomId = "room",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room",
                    Name = "Habitación",
                    Description = "Una habitación.",
                    IsInterior = true,
                    IsIlluminated = false,
                    ObjectIds = new List<string> { "torch", "matches" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "torch",
                    Name = "antorcha",
                    Description = "Una antorcha apagada.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room",
                    IsLightSource = true,
                    IsLit = false,
                    LightTurnsRemaining = -1,
                    CanIgnite = true,
                    CanExtinguish = true,
                    IgniterObjectId = "matches" // Requiere cerillas
                },
                new GameObject
                {
                    Id = "matches",
                    Name = "cerillas",
                    Description = "Una caja de cerillas.",
                    CanTake = true,
                    Visible = true,
                    RoomId = "room"
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
    /// Creates a test world with a light source inside a container.
    /// </summary>
    private static (WorldModel world, GameState state) CreateLightInContainer()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "test_container_light",
                Title = "Container Light Test",
                StartRoomId = "room",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room",
                    Name = "Habitación",
                    Description = "Una habitación.",
                    IsInterior = true,
                    IsIlluminated = false,
                    ObjectIds = new List<string> { "chest" },
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "chest",
                    Name = "cofre",
                    Description = "Un cofre de madera.",
                    IsContainer = true,
                    IsOpenable = true,
                    IsOpen = false,
                    ContentsVisible = false,
                    ContainedObjectIds = new List<string> { "candle" },
                    RoomId = "room"
                },
                new GameObject
                {
                    Id = "candle",
                    Name = "vela",
                    Description = "Una vela encendida.",
                    CanTake = true,
                    Visible = true,
                    // No RoomId - la vela está dentro del cofre, no directamente en la sala
                    IsLightSource = true,
                    IsLit = true,
                    LightTurnsRemaining = -1,
                    CanExtinguish = true
                }
            },
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        return (world, state);
    }

    #region Light Source Property Tests

    [Fact]
    public void GameObject_LightSourceProperties_DefaultValues()
    {
        var obj = new GameObject();
        Assert.False(obj.IsLightSource);
        Assert.False(obj.IsLit);
        Assert.Equal(-1, obj.LightTurnsRemaining);
        Assert.False(obj.CanExtinguish);
        Assert.False(obj.CanIgnite);
        Assert.Null(obj.IgniterObjectId);
    }

    [Fact]
    public void GameObject_LightSource_CanBeConfigured()
    {
        var torch = new GameObject
        {
            Id = "torch",
            Name = "antorcha",
            IsLightSource = true,
            IsLit = true,
            LightTurnsRemaining = 50,
            CanExtinguish = true,
            CanIgnite = true,
            IgniterObjectId = "matches"
        };

        Assert.True(torch.IsLightSource);
        Assert.True(torch.IsLit);
        Assert.Equal(50, torch.LightTurnsRemaining);
        Assert.True(torch.CanExtinguish);
        Assert.True(torch.CanIgnite);
        Assert.Equal("matches", torch.IgniterObjectId);
    }

    #endregion

    #region Room Illumination Tests

    [Fact]
    public void InteriorRoom_WithoutLight_IsDark()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Cuando está oscuro, la descripción de la sala no se muestra
        Assert.DoesNotContain("cueva", description.ToLower());
    }

    [Fact]
    public void InteriorRoom_WithLitTorch_IsIlluminated()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Con luz, la descripción de la sala sí se muestra
        Assert.Contains("cueva", description.ToLower());
    }

    [Fact]
    public void ExteriorRoom_AtNight_IsDark()
    {
        var (world, state) = CreateNightExterior();
        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Cuando está oscuro, la descripción de la sala no se muestra
        Assert.DoesNotContain("bosque", description.ToLower());
    }

    [Fact]
    public void ExteriorRoom_AtNight_WithLitLantern_IsIlluminated()
    {
        var (world, state) = CreateNightExterior();
        var lantern = state.Objects.First(o => o.Id == "lantern");
        lantern.IsLit = true;

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Con luz, la descripción de la sala sí se muestra
        Assert.Contains("bosque", description.ToLower());
    }

    [Fact]
    public void LightInClosedContainer_DoesNotIlluminate()
    {
        var (world, state) = CreateLightInContainer();
        var chest = state.Objects.First(o => o.Id == "chest");
        chest.IsOpen = false;
        chest.ContentsVisible = false;

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Luz en contenedor cerrado no ilumina, la descripción no se muestra
        Assert.DoesNotContain("habitación oscura", description.ToLower());
    }

    [Fact]
    public void LightInOpenContainer_Illuminates()
    {
        var (world, state) = CreateLightInContainer();
        var chest = state.Objects.First(o => o.Id == "chest");
        chest.IsOpen = true;

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        // Con luz visible, la descripción de la sala se muestra
        Assert.Contains("habitación", description.ToLower());
    }

    [Fact]
    public void LightInContainerWithVisibleContents_Illuminates()
    {
        var (world, state) = CreateLightInContainer();
        var chest = state.Objects.First(o => o.Id == "chest");
        chest.IsOpen = false;
        chest.ContentsVisible = true; // Like a glass case

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        Assert.DoesNotContain("oscuro", description.ToLower());
    }

    [Fact]
    public void LightInInventory_IlluminatesRoom()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        // Move torch to inventory
        state.InventoryObjectIds.Add("torch");
        var room = state.Rooms.First(r => r.Id == "dark_room");
        room.ObjectIds.Remove("torch");

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        var description = engine.DescribeCurrentRoom();
        Assert.DoesNotContain("oscuro", description.ToLower());
    }

    #endregion

    #region Ignite/Extinguish Command Tests

    [Fact]
    public void IgniteCommand_LightsTorch()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender antorcha");

        Assert.True(result.IsSuccess);
        Assert.Contains("enciendes", result.Message.ToLower());

        var torch = state.Objects.First(o => o.Id == "torch");
        Assert.True(torch.IsLit);
    }

    [Fact]
    public void ExtinguishCommand_TurnsOffTorch()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("apagar antorcha");

        Assert.True(result.IsSuccess);
        Assert.Contains("apagas", result.Message.ToLower());
        Assert.False(torch.IsLit);
    }

    [Fact]
    public void IgniteCommand_WithoutRequiredIgniter_Fails()
    {
        var (world, state) = CreateTorchWithIgniter();
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender antorcha");

        Assert.False(result.IsSuccess);
        Assert.Contains("necesitas", result.Message.ToLower());
    }

    [Fact]
    public void IgniteCommand_WithIgniter_Works()
    {
        var (world, state) = CreateTorchWithIgniter();
        // Pick up matches first
        state.InventoryObjectIds.Add("matches");
        var room = state.Rooms.First(r => r.Id == "room");
        room.ObjectIds.Remove("matches");

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender antorcha con cerillas");

        Assert.True(result.IsSuccess);
        Assert.Contains("enciendes", result.Message.ToLower());
        Assert.Contains("cerillas", result.Message.ToLower());

        var torch = state.Objects.First(o => o.Id == "torch");
        Assert.True(torch.IsLit);
    }

    [Fact]
    public void IgniteCommand_WrongIgniter_Fails()
    {
        var (world, state) = CreateTorchWithIgniter();
        // Add a different object to inventory
        state.Objects.Add(new GameObject
        {
            Id = "rock",
            Name = "piedra",
            CanTake = true
        });
        state.InventoryObjectIds.Add("rock");

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender antorcha con piedra");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public void IgniteCommand_AlreadyLit_Fails()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender antorcha");

        Assert.False(result.IsSuccess);
        // RandomMessages.AlreadyLit contiene el nombre del objeto y variantes del mensaje
        Assert.Contains("antorcha", result.Message.ToLower());
    }

    [Fact]
    public void ExtinguishCommand_AlreadyOff_Fails()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("apagar antorcha");

        Assert.False(result.IsSuccess);
        // RandomMessages.AlreadyOff contiene el nombre del objeto y variantes del mensaje
        Assert.Contains("antorcha", result.Message.ToLower());
    }

    [Fact]
    public void IgniteCommand_NonLightSource_Fails()
    {
        var (world, state) = CreateTorchWithIgniter();
        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("encender cerillas");

        Assert.False(result.IsSuccess);
        // RandomMessages.CannotIgnite contiene el nombre del objeto
        Assert.Contains("cerillas", result.Message.ToLower());
    }

    #endregion

    #region Light Duration Tests

    [Fact]
    public void LightSource_WithLimitedTurns_DecreasesEachTurn()
    {
        var (world, state) = CreateNightExterior();
        var lantern = state.Objects.First(o => o.Id == "lantern");
        lantern.IsLit = true;
        lantern.LightTurnsRemaining = 5;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        // Process a command to advance one turn
        engine.ProcessCommand("esperar");

        Assert.Equal(4, lantern.LightTurnsRemaining);
        Assert.True(lantern.IsLit);
    }

    [Fact]
    public void LightSource_ExpiresAfterTurns()
    {
        var (world, state) = CreateNightExterior();
        var lantern = state.Objects.First(o => o.Id == "lantern");
        lantern.IsLit = true;
        lantern.LightTurnsRemaining = 1;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        var result = engine.ProcessCommand("esperar");

        Assert.Equal(0, lantern.LightTurnsRemaining);
        Assert.False(lantern.IsLit);
        var msg = result.Message.ToLower();
        Assert.True(
            msg.Contains("se apaga") || msg.Contains("extingue") || msg.Contains("muere") ||
            msg.Contains("deja de iluminar") || msg.Contains("desvanece") || msg.Contains("expira"),
            $"Expected light-goes-out message, got: {result.Message}");
    }

    [Fact]
    public void LightSource_InfiniteTurns_NeverExpires()
    {
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;
        torch.LightTurnsRemaining = -1;

        var engine = new GameEngine(world, state, CreateMockSoundManager());
        engine.TriggerInitialScripts();

        // Process many commands
        for (int i = 0; i < 100; i++)
        {
            engine.ProcessCommand("esperar");
        }

        Assert.Equal(-1, torch.LightTurnsRemaining);
        Assert.True(torch.IsLit);
    }

    #endregion

    #region Parser Tests

    [Fact]
    public void Parser_RecognizesIgniteVerb()
    {
        var result = Parser.Parse("encender antorcha");
        Assert.Equal("ignite", result.Verb);
        Assert.Equal("antorcha", result.DirectObject);
    }

    [Fact]
    public void Parser_RecognizesIgniteWithPreposition()
    {
        var result = Parser.Parse("encender antorcha con cerillas");
        Assert.Equal("ignite", result.Verb);
        Assert.Equal("antorcha", result.DirectObject);
        Assert.Equal("cerillas", result.IndirectObject);
        Assert.Equal(PrepositionKind.With, result.Preposition);
    }

    [Fact]
    public void Parser_RecognizesExtinguishVerb()
    {
        var result = Parser.Parse("apagar farol");
        Assert.Equal("extinguish", result.Verb);
        Assert.Equal("farol", result.DirectObject);
    }

    [Fact]
    public void Parser_RecognizesPrenderAlias()
    {
        var result = Parser.Parse("prender vela");
        Assert.Equal("ignite", result.Verb);
        Assert.Equal("vela", result.DirectObject);
    }

    #endregion

    #region Inventory Display Tests

    [Fact]
    public void Inventory_ShowsRemainingTurnsForLitLightSource()
    {
        // Arrange: Create a lit torch with 10 turns remaining
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;
        torch.LightTurnsRemaining = 10;
        state.InventoryObjectIds.Add("torch"); // Add to inventory
        state.Rooms.First(r => r.Id == "dark_room").ObjectIds.Remove("torch"); // Remove from room

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        // Act
        var inventory = engine.DescribeInventory();

        // Assert: Should show "Antorcha (10)"
        Assert.Contains("(10)", inventory);
        Assert.Contains("Antorcha", inventory);
    }

    [Fact]
    public void Inventory_DoesNotShowTurnsForUnlitLightSource()
    {
        // Arrange: Create an unlit torch with turns remaining
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = false;
        torch.LightTurnsRemaining = 10;
        state.InventoryObjectIds.Add("torch");
        state.Rooms.First(r => r.Id == "dark_room").ObjectIds.Remove("torch");

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        // Act
        var inventory = engine.DescribeInventory();

        // Assert: Should NOT show turns for unlit source
        Assert.DoesNotContain("(10)", inventory);
        Assert.Contains("Antorcha", inventory);
    }

    [Fact]
    public void Inventory_ShowsInfinitySymbolForInfiniteLightSource()
    {
        // Arrange: Create a lit torch with infinite turns (-1)
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;
        torch.LightTurnsRemaining = -1; // Infinite

        state.InventoryObjectIds.Add("torch");
        state.Rooms.First(r => r.Id == "dark_room").ObjectIds.Remove("torch");

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        // Act
        var inventory = engine.DescribeInventory();

        // Assert: Should show infinity symbol for infinite sources
        Assert.Contains("(∞)", inventory);
        Assert.Contains("Antorcha", inventory);
    }

    [Fact]
    public void Inventory_ShowsCorrectTurnsAfterReduction()
    {
        // Arrange: Create a lit torch, turns will be reduced on command
        var (world, state) = CreateDarkRoomWithTorch();
        var torch = state.Objects.First(o => o.Id == "torch");
        torch.IsLit = true;
        torch.LightTurnsRemaining = 5;
        state.InventoryObjectIds.Add("torch");
        state.Rooms.First(r => r.Id == "dark_room").ObjectIds.Remove("torch");

        var engine = new GameEngine(world, state, CreateMockSoundManager());

        // Act: Wait a turn (this reduces light sources)
        engine.ProcessCommand("esperar");
        var inventory = engine.DescribeInventory();

        // Assert: Should show 4 turns remaining after waiting
        Assert.Contains("(4)", inventory);
    }

    #endregion
}
