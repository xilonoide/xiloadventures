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
/// Integration tests for conversation system with the full game engine.
/// Tests end-to-end conversation flows including game state changes.
/// </summary>
public class ConversationIntegrationTests
{
    /// <summary>
    /// Creates a complete test world for integration testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateIntegrationWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "integration_test",
                Title = "Integration Test World",
                StartRoomId = "tavern",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "tavern",
                    Name = "Taberna del Dragón",
                    Description = "Una taberna acogedora llena de aventureros.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "barkeeper", "questgiver" },
                    ObjectIds = new List<string> { "ale" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "ale",
                    Name = "cerveza",
                    Description = "Una jarra de cerveza fresca.",
                    Type = ObjectType.Comida,
                    Price = 5,
                    CanTake = true,
                    Visible = true
                },
                new GameObject
                {
                    Id = "sword",
                    Name = "espada",
                    Description = "Una espada de acero.",
                    Type = ObjectType.Arma,
                    Price = 100,
                    CanTake = true,
                    Visible = true
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "barkeeper",
                    Name = "Tabernero",
                    Description = "El tabernero, un hombre corpulento.",
                    RoomId = "tavern",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 200,
                    Inventory = new List<InventoryItem> { new() { ObjectId = "ale", Quantity = 1 } }
                },
                new Npc
                {
                    Id = "questgiver",
                    Name = "Capitán de la Guardia",
                    Description = "Un veterano soldado.",
                    RoomId = "tavern",
                    Visible = true
                }
            },
            Quests = new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    Id = "main_quest",
                    Name = "Defender la Villa",
                    Description = "Protege la villa de los bandidos.",
                    IsMainQuest = true
                }
            },
            Conversations = new List<ConversationDefinition>(),
            Scripts = new List<ScriptDefinition>
            {
                CreateQuestGiverScript()
            }
        };

        var state = new GameState
        {
            CurrentRoomId = "tavern",
            Player = new PlayerStats
            {
                Name = "Aventurero",
                Money = 50,
                DynamicStats = new PlayerDynamicStats { Health = 100, MaxHealth = 100 }
            },
            Rooms = world.Rooms.ToList(),
            Npcs = world.Npcs.ToList(),
            Objects = world.Objects.ToList()
        };

        return (world, state);
    }

    private static ScriptDefinition CreateQuestGiverScript()
    {
        return new ScriptDefinition
        {
            Id = "questgiver_script",
            Name = "Script del Capitán",
            OwnerType = "Npc",
            OwnerId = "questgiver",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "conv_start",
                    NodeType = NodeTypeId.Conversation_Start,
                    Category = NodeCategory.Dialogue
                },
                new ScriptNode
                {
                    Id = "greeting",
                    NodeType = NodeTypeId.Conversation_NpcSay,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text", "¡Saludos, aventurero! La villa necesita tu ayuda." },
                        { "SpeakerName", "Capitán de la Guardia" }
                    }
                },
                new ScriptNode
                {
                    Id = "choice",
                    NodeType = NodeTypeId.Conversation_PlayerChoice,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text1", "¿Qué puedo hacer por ti?" },
                        { "Text2", "No tengo tiempo ahora." }
                    }
                },
                new ScriptNode
                {
                    Id = "quest_info",
                    NodeType = NodeTypeId.Conversation_NpcSay,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text", "Unos bandidos atacan las caravanas. Necesitamos alguien que los detenga." },
                        { "SpeakerName", "Capitán" }
                    }
                },
                new ScriptNode
                {
                    Id = "give_quest",
                    NodeType = NodeTypeId.Conversation_Action,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "ActionType", "StartQuest" },
                        { "QuestId", "main_quest" }
                    }
                },
                new ScriptNode
                {
                    Id = "reward_info",
                    NodeType = NodeTypeId.Conversation_NpcSay,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text", "Te pagaré 100 monedas de oro si lo consigues." },
                        { "SpeakerName", "Capitán" }
                    }
                },
                new ScriptNode
                {
                    Id = "decline_msg",
                    NodeType = NodeTypeId.Conversation_NpcSay,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text", "Vuelve cuando tengas tiempo." },
                        { "SpeakerName", "Capitán" }
                    }
                },
                new ScriptNode
                {
                    Id = "conv_end",
                    NodeType = NodeTypeId.Conversation_End,
                    Category = NodeCategory.Dialogue
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection { FromNodeId = "conv_start", FromPortName = "Exec", ToNodeId = "greeting", ToPortName = "In" },
                new NodeConnection { FromNodeId = "greeting", FromPortName = "Exec", ToNodeId = "choice", ToPortName = "In" },
                new NodeConnection { FromNodeId = "choice", FromPortName = "Option1", ToNodeId = "quest_info", ToPortName = "In" },
                new NodeConnection { FromNodeId = "choice", FromPortName = "Option2", ToNodeId = "decline_msg", ToPortName = "In" },
                new NodeConnection { FromNodeId = "quest_info", FromPortName = "Exec", ToNodeId = "give_quest", ToPortName = "In" },
                new NodeConnection { FromNodeId = "give_quest", FromPortName = "Exec", ToNodeId = "reward_info", ToPortName = "In" },
                new NodeConnection { FromNodeId = "reward_info", FromPortName = "Exec", ToNodeId = "conv_end", ToPortName = "In" },
                new NodeConnection { FromNodeId = "decline_msg", FromPortName = "Exec", ToNodeId = "conv_end", ToPortName = "In" }
            }
        };
    }

    [Fact]
    public async Task Conversation_WithQuestAction_StartsQuestInGameState()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);
        var messagesReceived = new List<string>();
        engine.OnDialogue += msg => messagesReceived.Add(msg.Text);
        engine.OnSystemMessage += msg => messagesReceived.Add(msg);

        // Act - Start conversation and accept quest
        await engine.StartConversationAsync("questgiver");
        await engine.SelectOptionAsync(0); // "¿Qué puedo hacer por ti?"

        // Assert - Quest should be started
        Assert.True(state.Quests.ContainsKey("main_quest"));
        Assert.Equal(QuestStatus.InProgress, state.Quests["main_quest"].Status);
        Assert.Contains(messagesReceived, m => m.Contains("Nueva misión"));
    }

    [Fact]
    public async Task Conversation_Decline_DoesNotStartQuest()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);

        // Act - Start conversation and decline
        await engine.StartConversationAsync("questgiver");
        await engine.SelectOptionAsync(1); // "No tengo tiempo ahora."

        // Assert - Quest should not be started
        Assert.False(state.Quests.ContainsKey("main_quest"));
    }

    [Fact]
    public async Task Conversation_FullFlow_CompletesSuccessfully()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);
        bool conversationEnded = false;
        engine.OnConversationEnded += () => conversationEnded = true;

        // Act - Complete full conversation flow
        await engine.StartConversationAsync("questgiver");
        await engine.SelectOptionAsync(0);

        // Assert
        Assert.True(conversationEnded);
        Assert.False(engine.IsConversationActive);
    }

    [Fact]
    public async Task Conversation_FromScript_ExtractsDialogueNodes()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);
        var dialogueMessages = new List<ConversationMessage>();
        engine.OnDialogue += msg => dialogueMessages.Add(msg);

        // Act
        await engine.StartConversationAsync("questgiver");

        // Assert - Should have received greeting message
        Assert.NotEmpty(dialogueMessages);
        Assert.Contains(dialogueMessages, m => m.Text.Contains("aventurero"));
    }

    [Fact]
    public async Task Conversation_PlayerOptions_DisplayedCorrectly()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);
        List<DialogueOption>? options = null;
        engine.OnPlayerOptions += o => options = o;

        // Act
        await engine.StartConversationAsync("questgiver");

        // Assert
        Assert.NotNull(options);
        Assert.Equal(2, options.Count);
        Assert.Contains(options, o => o.Text.Contains("Qué puedo hacer"));
        Assert.Contains(options, o => o.Text.Contains("No tengo tiempo"));
    }

    [Fact]
    public async Task Conversation_NpcSay_IncludesSpeakerName()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);
        ConversationMessage? message = null;
        engine.OnDialogue += msg => message = msg;

        // Act
        await engine.StartConversationAsync("questgiver");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("Capitán de la Guardia", message.SpeakerName);
        Assert.True(message.IsNpc);
    }

    [Fact]
    public async Task Conversation_EndNode_TerminatesConversation()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);

        // Act - Decline option leads to end node
        await engine.StartConversationAsync("questgiver");
        await engine.SelectOptionAsync(1);

        // Assert
        Assert.False(engine.IsConversationActive);
        Assert.Null(state.ActiveConversation);
    }

    [Fact]
    public async Task Conversation_WithNonExistentNpc_HandlesGracefully()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);

        // Act
        await engine.StartConversationAsync("nonexistent");

        // Assert
        Assert.False(engine.IsConversationActive);
    }

    [Fact]
    public async Task Conversation_VisitedNodes_Tracked()
    {
        // Arrange
        var (world, state) = CreateIntegrationWorld();
        var engine = new ConversationEngine(world, state);

        // Act
        await engine.StartConversationAsync("questgiver");

        // Assert
        Assert.NotNull(state.ActiveConversation);
        Assert.NotEmpty(state.ActiveConversation.VisitedNodeIds);
        Assert.Contains("conv_start", state.ActiveConversation.VisitedNodeIds);
    }
}
