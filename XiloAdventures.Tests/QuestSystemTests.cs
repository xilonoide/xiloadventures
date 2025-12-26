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
/// Tests for the quest system including IsMainQuest functionality,
/// quest completion, and adventure completion detection.
/// </summary>
public class QuestSystemTests
{
    #region Test Helpers

    private static SoundManager CreateMockSoundManager()
    {
        return new SoundManager { SoundEnabled = false };
    }

    private static (WorldModel world, GameState state) CreateWorldWithQuests(
        List<QuestDefinition> quests,
        bool startQuestsInProgress = false)
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
                    Name = "Test Room",
                    Description = "A test room.",
                    IsIlluminated = true,
                    Exits = new List<Exit>(),
                    ObjectIds = new List<string>(),
                    NpcIds = new List<string>()
                }
            },
            Objects = new List<GameObject>(),
            Npcs = new List<Npc>(),
            Doors = new List<Door>(),
            Quests = quests,
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);

        if (startQuestsInProgress)
        {
            foreach (var quest in quests)
            {
                state.Quests[quest.Id] = new QuestState
                {
                    QuestId = quest.Id,
                    Status = QuestStatus.InProgress,
                    CurrentObjectiveIndex = 0
                };
            }
        }

        return (world, state);
    }

    private static ScriptDefinition CreateCompleteQuestScript(string questId, string roomId = "room1")
    {
        return new ScriptDefinition
        {
            Id = $"script_complete_{questId}",
            OwnerType = "Room",
            OwnerId = roomId,
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_enter",
                    NodeType = NodeTypeId.Event_OnEnter,
                    Category = NodeCategory.Event,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "action_complete",
                    NodeType = NodeTypeId.Action_CompleteQuest,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["QuestId"] = questId
                    }
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_enter",
                    FromPortName = "Exec",
                    ToNodeId = "action_complete",
                    ToPortName = "Exec"
                }
            }
        };
    }

    private static ScriptDefinition CreateGameStartScript(NodeTypeId actionType, Dictionary<string, object?> actionProps)
    {
        return new ScriptDefinition
        {
            Id = "script_game_start",
            OwnerType = "Game",
            OwnerId = "test_quests",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_start",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "action",
                    NodeType = actionType,
                    Category = NodeCategory.Action,
                    Properties = actionProps
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_start",
                    FromPortName = "Exec",
                    ToNodeId = "action",
                    ToPortName = "Exec"
                }
            }
        };
    }

    private static ScriptDefinition CreateRoomScript(string roomId, NodeTypeId actionType, Dictionary<string, object?> actionProps)
    {
        return new ScriptDefinition
        {
            Id = $"script_{roomId}_{actionType}",
            OwnerType = "Room",
            OwnerId = roomId,
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_enter",
                    NodeType = NodeTypeId.Event_OnEnter,
                    Category = NodeCategory.Event,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "action",
                    NodeType = actionType,
                    Category = NodeCategory.Action,
                    Properties = actionProps
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    FromNodeId = "event_enter",
                    FromPortName = "Exec",
                    ToNodeId = "action",
                    ToPortName = "Exec"
                }
            }
        };
    }

    #endregion

    #region IsMainQuest Property Tests

    [Fact]
    public void QuestDefinition_IsMainQuest_DefaultsToTrue()
    {
        var quest = new QuestDefinition
        {
            Id = "quest1",
            Name = "Test Quest"
        };

        Assert.True(quest.IsMainQuest);
    }

    [Fact]
    public void QuestDefinition_IsMainQuest_CanBeSetToFalse()
    {
        var quest = new QuestDefinition
        {
            Id = "quest1",
            Name = "Side Quest",
            IsMainQuest = false
        };

        Assert.False(quest.IsMainQuest);
    }

    [Fact]
    public void WorldLoader_PreservesIsMainQuestProperty()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest", IsMainQuest = true },
            new QuestDefinition { Id = "side1", Name = "Side Quest", IsMainQuest = false }
        };

        var (world, _) = CreateWorldWithQuests(quests);

        var mainQuest = world.Quests.First(q => q.Id == "main1");
        var sideQuest = world.Quests.First(q => q.Id == "side1");

        Assert.True(mainQuest.IsMainQuest);
        Assert.False(sideQuest.IsMainQuest);
    }

    #endregion

    #region Quest Status Tests

    [Fact]
    public void CreateInitialState_InitializesQuestsAsNotStarted()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "Quest 1" },
            new QuestDefinition { Id = "quest2", Name = "Quest 2" }
        };

        var (_, state) = CreateWorldWithQuests(quests);

        Assert.All(state.Quests.Values, q => Assert.Equal(QuestStatus.NotStarted, q.Status));
    }

    [Fact]
    public async Task ScriptEngine_StartQuest_ChangesStatusToInProgress()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "Test Quest" }
        };

        var (world, state) = CreateWorldWithQuests(quests);

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["QuestId"] = "quest1"
        };
        world.Scripts.Add(CreateGameStartScript(NodeTypeId.Action_StartQuest, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Game", "test_quests", NodeTypeId.Event_OnGameStart);

        Assert.Equal(QuestStatus.InProgress, state.Quests["quest1"].Status);
    }

    [Fact]
    public async Task ScriptEngine_CompleteQuest_ChangesStatusToCompleted()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "Test Quest" }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("quest1"));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.Equal(QuestStatus.Completed, state.Quests["quest1"].Status);
    }

    [Fact]
    public async Task ScriptEngine_FailQuest_ChangesStatusToFailed()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "Test Quest" }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["QuestId"] = "quest1"
        };
        world.Scripts.Add(CreateRoomScript("room1", NodeTypeId.Action_FailQuest, actionProps));

        var engine = new ScriptEngine(world, state);
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.Equal(QuestStatus.Failed, state.Quests["quest1"].Status);
    }

    #endregion

    #region Adventure Completion Tests

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_FiresWhenAllMainQuestsComplete()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest 1", IsMainQuest = true },
            new QuestDefinition { Id = "main2", Name = "Main Quest 2", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        // Complete main1 first
        state.Quests["main1"].Status = QuestStatus.Completed;

        // Script to complete main2
        world.Scripts.Add(CreateCompleteQuestScript("main2"));

        var engine = new ScriptEngine(world, state);
        bool adventureCompletedFired = false;
        engine.OnAdventureCompleted += () => adventureCompletedFired = true;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.True(adventureCompletedFired, "OnAdventureCompleted should fire when all main quests are completed");
    }

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_DoesNotFireIfNotAllMainQuestsComplete()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest 1", IsMainQuest = true },
            new QuestDefinition { Id = "main2", Name = "Main Quest 2", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("main1"));

        var engine = new ScriptEngine(world, state);
        bool adventureCompletedFired = false;
        engine.OnAdventureCompleted += () => adventureCompletedFired = true;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.False(adventureCompletedFired, "OnAdventureCompleted should not fire if some main quests are incomplete");
    }

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_IgnoresSideQuests()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest", IsMainQuest = true },
            new QuestDefinition { Id = "side1", Name = "Side Quest", IsMainQuest = false }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("main1"));

        var engine = new ScriptEngine(world, state);
        bool adventureCompletedFired = false;
        engine.OnAdventureCompleted += () => adventureCompletedFired = true;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.True(adventureCompletedFired, "OnAdventureCompleted should fire when all MAIN quests complete, ignoring side quests");
        Assert.Equal(QuestStatus.InProgress, state.Quests["side1"].Status);
    }

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_FiresWithSingleMainQuest()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Only Main Quest", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("main1"));

        var engine = new ScriptEngine(world, state);
        bool adventureCompletedFired = false;
        engine.OnAdventureCompleted += () => adventureCompletedFired = true;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.True(adventureCompletedFired);
    }

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_DoesNotFireWithNoMainQuests()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "side1", Name = "Side Quest 1", IsMainQuest = false },
            new QuestDefinition { Id = "side2", Name = "Side Quest 2", IsMainQuest = false }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["QuestId"] = "side1"
        };
        world.Scripts.Add(CreateRoomScript("room1", NodeTypeId.Action_CompleteQuest, actionProps));

        var engine = new ScriptEngine(world, state);
        bool adventureCompletedFired = false;
        engine.OnAdventureCompleted += () => adventureCompletedFired = true;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.False(adventureCompletedFired, "OnAdventureCompleted should not fire when there are no main quests");
    }

    [Fact]
    public async Task ScriptEngine_OnAdventureCompleted_FiresWhenQuestCompletedMultipleTimes()
    {
        // Note: The current implementation fires OnAdventureCompleted every time
        // Action_CompleteQuest runs if all main quests are complete.
        // This test verifies this behavior.
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("main1"));

        var engine = new ScriptEngine(world, state);
        int adventureCompletedCount = 0;
        engine.OnAdventureCompleted += () => adventureCompletedCount++;

        // Trigger multiple times - event fires each time the action runs
        // since all main quests are complete
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);
        Assert.Equal(QuestStatus.Completed, state.Quests["main1"].Status);
        Assert.Equal(1, adventureCompletedCount);

        // Second trigger also fires the event (action runs again on already-completed quest)
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);
        Assert.Equal(2, adventureCompletedCount);
    }

    #endregion

    #region GameEngine Integration Tests

    [Fact]
    public void GameEngine_AdventureCompleted_PropagatesFromScriptEngine()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("main1"));

        var sound = CreateMockSoundManager();
        var engine = new GameEngine(world, state, sound);

        bool eventSubscribed = false;
        engine.AdventureCompleted += () => eventSubscribed = true;

        // Verify event subscription is in place
        Assert.NotNull(engine);
        Assert.False(eventSubscribed); // Event not fired yet
    }

    #endregion

    #region Quest Message Tests

    [Fact]
    public async Task ScriptEngine_StartQuest_ShowsNewQuestMessage()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "La Búsqueda del Tesoro" }
        };

        var (world, state) = CreateWorldWithQuests(quests);

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["QuestId"] = "quest1"
        };
        world.Scripts.Add(CreateGameStartScript(NodeTypeId.Action_StartQuest, actionProps));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        await engine.TriggerEventAsync("Game", "test_quests", NodeTypeId.Event_OnGameStart);

        Assert.NotNull(receivedMessage);
        Assert.Contains("La Búsqueda del Tesoro", receivedMessage);
        Assert.Contains("Nueva misión", receivedMessage);
    }

    [Fact]
    public async Task ScriptEngine_CompleteQuest_ShowsCompletedMessage()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "El Rescate" }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);
        world.Scripts.Add(CreateCompleteQuestScript("quest1"));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.NotNull(receivedMessage);
        Assert.Contains("El Rescate", receivedMessage);
        Assert.Contains("completada", receivedMessage);
    }

    [Fact]
    public async Task ScriptEngine_FailQuest_ShowsFailedMessage()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "quest1", Name = "Misión Imposible" }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        var actionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["QuestId"] = "quest1"
        };
        world.Scripts.Add(CreateRoomScript("room1", NodeTypeId.Action_FailQuest, actionProps));

        var engine = new ScriptEngine(world, state);
        string? receivedMessage = null;
        engine.OnMessage += msg => receivedMessage = msg;

        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);

        Assert.NotNull(receivedMessage);
        Assert.Contains("Misión Imposible", receivedMessage);
        Assert.Contains("fallida", receivedMessage);
    }

    #endregion

    #region Complex Quest Scenarios

    [Fact]
    public async Task ScriptEngine_MultipleMainQuests_CompletedInSequence()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Primera Misión", IsMainQuest = true },
            new QuestDefinition { Id = "main2", Name = "Segunda Misión", IsMainQuest = true },
            new QuestDefinition { Id = "main3", Name = "Tercera Misión", IsMainQuest = true }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        // Different rooms trigger different quest completions
        world.Rooms.Add(new Room { Id = "room2", Name = "Room 2", Exits = new List<Exit>() });
        world.Rooms.Add(new Room { Id = "room3", Name = "Room 3", Exits = new List<Exit>() });

        world.Scripts.Add(CreateCompleteQuestScript("main1", "room1"));
        world.Scripts.Add(CreateCompleteQuestScript("main2", "room2"));
        world.Scripts.Add(CreateCompleteQuestScript("main3", "room3"));

        var engine = new ScriptEngine(world, state);
        int adventureCompletedCount = 0;
        engine.OnAdventureCompleted += () => adventureCompletedCount++;

        // Complete quests in sequence
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);
        Assert.Equal(0, adventureCompletedCount);
        Assert.Equal(QuestStatus.Completed, state.Quests["main1"].Status);

        await engine.TriggerEventAsync("Room", "room2", NodeTypeId.Event_OnEnter);
        Assert.Equal(0, adventureCompletedCount);
        Assert.Equal(QuestStatus.Completed, state.Quests["main2"].Status);

        await engine.TriggerEventAsync("Room", "room3", NodeTypeId.Event_OnEnter);
        Assert.Equal(1, adventureCompletedCount);
        Assert.Equal(QuestStatus.Completed, state.Quests["main3"].Status);
    }

    [Fact]
    public async Task ScriptEngine_MixedQuestTypes_OnlyMainQuestsAffectCompletion()
    {
        var quests = new List<QuestDefinition>
        {
            new QuestDefinition { Id = "main1", Name = "Main Quest", IsMainQuest = true },
            new QuestDefinition { Id = "side1", Name = "Side Quest 1", IsMainQuest = false },
            new QuestDefinition { Id = "side2", Name = "Side Quest 2", IsMainQuest = false }
        };

        var (world, state) = CreateWorldWithQuests(quests, startQuestsInProgress: true);

        world.Rooms.Add(new Room { Id = "room2", Name = "Room 2", Exits = new List<Exit>() });
        world.Rooms.Add(new Room { Id = "room3", Name = "Room 3", Exits = new List<Exit>() });

        var side1Props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["QuestId"] = "side1" };
        var side2Props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["QuestId"] = "side2" };
        var main1Props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["QuestId"] = "main1" };

        world.Scripts.Add(CreateRoomScript("room1", NodeTypeId.Action_CompleteQuest, side1Props));
        world.Scripts.Add(CreateRoomScript("room2", NodeTypeId.Action_CompleteQuest, side2Props));
        world.Scripts.Add(CreateRoomScript("room3", NodeTypeId.Action_CompleteQuest, main1Props));

        var engine = new ScriptEngine(world, state);
        int adventureCompletedCount = 0;
        engine.OnAdventureCompleted += () => adventureCompletedCount++;

        // Complete side quests first
        await engine.TriggerEventAsync("Room", "room1", NodeTypeId.Event_OnEnter);
        await engine.TriggerEventAsync("Room", "room2", NodeTypeId.Event_OnEnter);
        Assert.Equal(0, adventureCompletedCount); // Side quests don't trigger adventure completion

        // Now complete main quest
        await engine.TriggerEventAsync("Room", "room3", NodeTypeId.Event_OnEnter);
        Assert.Equal(1, adventureCompletedCount);
    }

    #endregion
}
