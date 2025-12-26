using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for the ConversationEngine class.
/// Tests conversation mechanics including dialogue, player choices, and NPC interactions.
/// </summary>
public class ConversationEngineTests
{
    /// <summary>
    /// Creates a test world with NPCs and conversation scripts for testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateConversationTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "conversation_test",
                Title = "Conversation Test World",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Plaza del pueblo",
                    Description = "Una plaza con varios personajes.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "merchant", "guard" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "key",
                    Name = "llave",
                    Description = "Una llave dorada.",
                    CanTake = true,
                    Visible = true
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "merchant",
                    Name = "Comerciante",
                    Description = "Un comerciante amigable.",
                    RoomId = "room1",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 500
                },
                new Npc
                {
                    Id = "guard",
                    Name = "Guardia",
                    Description = "Un guardia vigilante.",
                    RoomId = "room1",
                    Visible = true
                }
            },
            Scripts = new List<ScriptDefinition>
            {
                CreateMerchantScript(),
                CreateGuardScript()
            },
            Quests = new List<QuestDefinition>
            {
                new QuestDefinition
                {
                    Id = "find_key",
                    Name = "Encontrar la llave",
                    Description = "Busca la llave perdida."
                }
            }
        };

        var state = new GameState
        {
            CurrentRoomId = "room1",
            Player = new PlayerStats
            {
                Name = "Héroe",
                Money = 100,
                DynamicStats = new PlayerDynamicStats { Health = 100, MaxHealth = 100 }
            },
            Rooms = world.Rooms.ToList(),
            Npcs = world.Npcs.ToList(),
            Objects = world.Objects.ToList()
        };

        return (world, state);
    }

    private static ScriptDefinition CreateMerchantScript()
    {
        return new ScriptDefinition
        {
            Id = "merchant_script",
            Name = "Script del comerciante",
            OwnerType = "Npc",
            OwnerId = "merchant",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "start",
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
                        { "Text", "¡Bienvenido a mi tienda! ¿En qué puedo ayudarte?" },
                        { "SpeakerName", "Comerciante" },
                        { "Emotion", "Feliz" }
                    }
                },
                new ScriptNode
                {
                    Id = "choice1",
                    NodeType = NodeTypeId.Conversation_PlayerChoice,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text1", "Quiero ver tus productos." },
                        { "Text2", "Adiós." }
                    }
                },
                new ScriptNode
                {
                    Id = "shop",
                    NodeType = NodeTypeId.Conversation_Shop,
                    Category = NodeCategory.Dialogue
                },
                new ScriptNode
                {
                    Id = "farewell",
                    NodeType = NodeTypeId.Conversation_NpcSay,
                    Category = NodeCategory.Dialogue,
                    Properties = new Dictionary<string, object?>
                    {
                        { "Text", "¡Hasta luego! Vuelve pronto." },
                        { "SpeakerName", "Comerciante" }
                    }
                },
                new ScriptNode
                {
                    Id = "end",
                    NodeType = NodeTypeId.Conversation_End,
                    Category = NodeCategory.Dialogue
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection { FromNodeId = "start", FromPortName = "Exec", ToNodeId = "greeting", ToPortName = "In" },
                new NodeConnection { FromNodeId = "greeting", FromPortName = "Exec", ToNodeId = "choice1", ToPortName = "In" },
                new NodeConnection { FromNodeId = "choice1", FromPortName = "Option1", ToNodeId = "shop", ToPortName = "In" },
                new NodeConnection { FromNodeId = "choice1", FromPortName = "Option2", ToNodeId = "farewell", ToPortName = "In" },
                new NodeConnection { FromNodeId = "shop", FromPortName = "OnClose", ToNodeId = "farewell", ToPortName = "In" },
                new NodeConnection { FromNodeId = "farewell", FromPortName = "Exec", ToNodeId = "end", ToPortName = "In" }
            }
        };
    }

    private static ScriptDefinition CreateGuardScript()
    {
        return new ScriptDefinition
        {
            Id = "guard_script",
            Name = "Script del guardia",
            OwnerType = "Npc",
            OwnerId = "guard",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "start",
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
                        { "Text", "Alto ahí, ciudadano. ¿Qué necesitas?" },
                        { "SpeakerName", "Guardia" }
                    }
                },
                new ScriptNode
                {
                    Id = "end",
                    NodeType = NodeTypeId.Conversation_End,
                    Category = NodeCategory.Dialogue
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection { FromNodeId = "start", FromPortName = "Exec", ToNodeId = "greeting", ToPortName = "In" },
                new NodeConnection { FromNodeId = "greeting", FromPortName = "Exec", ToNodeId = "end", ToPortName = "In" }
            }
        };
    }

    [Fact]
    public async Task StartConversationAsync_ValidNpc_InitiatesConversation()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);

        // Act
        await engine.StartConversationAsync("merchant");

        // Assert
        Assert.True(engine.IsConversationActive);
        Assert.NotNull(state.ActiveConversation);
        Assert.Equal("merchant", state.ActiveConversation.NpcId);
    }

    [Fact]
    public async Task StartConversationAsync_InvalidNpc_DoesNotStart()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);

        // Act
        await engine.StartConversationAsync("nonexistent");

        // Assert
        Assert.False(engine.IsConversationActive);
        Assert.Null(state.ActiveConversation);
    }

    [Fact]
    public async Task StartConversationAsync_NpcWithDialogue_ShowsGreeting()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        string? dialogueText = null;
        engine.OnDialogue += msg => dialogueText = msg.Text;

        // Act
        await engine.StartConversationAsync("guard");

        // Assert
        Assert.NotNull(dialogueText);
        Assert.Contains("Alto ahí", dialogueText);
    }

    [Fact]
    public async Task OnDialogue_FiredWithCorrectSpeaker()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        ConversationMessage? message = null;
        engine.OnDialogue += msg => message = msg;

        // Act
        await engine.StartConversationAsync("merchant");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("Comerciante", message.SpeakerName);
        Assert.True(message.IsNpc);
    }

    [Fact]
    public async Task OnPlayerOptions_FiredWithChoices()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        List<DialogueOption>? options = null;
        engine.OnPlayerOptions += opts => options = opts;

        // Act
        await engine.StartConversationAsync("merchant");

        // Assert
        Assert.NotNull(options);
        Assert.Equal(2, options.Count);
    }

    [Fact]
    public async Task SelectOptionAsync_ValidChoice_AdvancesConversation()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        string? lastDialogue = null;
        engine.OnDialogue += msg => lastDialogue = msg.Text;

        // Act
        await engine.StartConversationAsync("merchant");
        await engine.SelectOptionAsync(1); // Adiós

        // Assert
        Assert.Contains("Hasta luego", lastDialogue);
    }

    [Fact]
    public async Task Conversation_EndsAfterFarewell()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        bool conversationEnded = false;
        engine.OnConversationEnded += () => conversationEnded = true;

        // Act
        await engine.StartConversationAsync("merchant");
        await engine.SelectOptionAsync(1); // Adiós

        // Assert
        Assert.True(conversationEnded);
        Assert.False(engine.IsConversationActive);
    }

    [Fact]
    public async Task NpcWithNoDialogue_DoesNotStartConversation()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        // Add an NPC without dialogue script
        world.Npcs.Add(new Npc
        {
            Id = "silent",
            Name = "NPC Silencioso",
            RoomId = "room1",
            Visible = true
        });
        state.Npcs = world.Npcs.ToList();

        var engine = new ConversationEngine(world, state);

        // Act
        await engine.StartConversationAsync("silent");

        // Assert
        Assert.False(engine.IsConversationActive);
    }

    [Fact]
    public async Task MultipleConversations_CanStartDifferentNpcs()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        string? lastSpeaker = null;
        engine.OnDialogue += msg => lastSpeaker = msg.SpeakerName;

        // Act - Start with merchant
        await engine.StartConversationAsync("merchant");
        Assert.Equal("Comerciante", lastSpeaker);

        await engine.SelectOptionAsync(1); // End conversation

        // Start with guard (ends immediately after greeting)
        await engine.StartConversationAsync("guard");

        // Assert - Guard conversation started (dialogue was shown)
        Assert.Equal("Guardia", lastSpeaker);
    }

    [Fact]
    public async Task ConversationState_TracksVisitedNodes()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);

        // Act - Use merchant since it stays active waiting for player choice
        await engine.StartConversationAsync("merchant");

        // Assert
        Assert.NotNull(state.ActiveConversation);
        Assert.NotEmpty(state.ActiveConversation.VisitedNodeIds);
    }

    [Fact]
    public async Task SelectOptionAsync_InvalidIndex_DoesNotCrash()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);

        await engine.StartConversationAsync("merchant");

        // Act & Assert - Should not throw
        await engine.SelectOptionAsync(99);
    }

    [Fact]
    public async Task EndConversation_ClearsActiveState()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);

        await engine.StartConversationAsync("merchant");
        Assert.True(engine.IsConversationActive);

        // Act
        engine.EndConversation();

        // Assert
        Assert.False(engine.IsConversationActive);
        Assert.Null(state.ActiveConversation);
    }

    [Fact]
    public async Task ShopNode_OpensShopInterface()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        bool shopOpened = false;
        engine.OnTradeOpen += _ => shopOpened = true;

        // Act
        await engine.StartConversationAsync("merchant");
        await engine.SelectOptionAsync(0); // Ver productos

        // Assert
        Assert.True(shopOpened);
    }

    [Fact]
    public async Task Conversation_WithEmotion_IncludesEmotionInMessage()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        ConversationMessage? message = null;
        engine.OnDialogue += msg => message = msg;

        // Act
        await engine.StartConversationAsync("merchant");

        // Assert
        Assert.NotNull(message);
        Assert.Equal("Feliz", message.Emotion);
    }

    [Fact]
    public async Task SimpleConversation_StartsAndEndsCorrectly()
    {
        // Arrange
        var (world, state) = CreateConversationTestWorld();
        var engine = new ConversationEngine(world, state);
        bool ended = false;
        engine.OnConversationEnded += () => ended = true;

        // Act
        await engine.StartConversationAsync("guard");

        // Assert - Guard conversation ends immediately after greeting
        Assert.True(ended);
        Assert.False(engine.IsConversationActive);
    }
}
