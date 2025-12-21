using XiloAdventures.Engine.Models;
using Xunit;

namespace XiloAdventures.Tests;

public class NodeTypeRegistryTests
{
    [Fact]
    public void Types_ReturnsNonEmptyDictionary()
    {
        var types = NodeTypeRegistry.Types;

        Assert.NotNull(types);
        Assert.True(types.Count > 0);
    }

    [Fact]
    public void GetNodeType_ValidTypeId_ReturnsDefinition()
    {
        var result = NodeTypeRegistry.GetNodeType("Event_OnEnter");

        Assert.NotNull(result);
        Assert.Equal("Event_OnEnter", result.TypeId);
        Assert.Equal(NodeCategory.Event, result.Category);
    }

    [Fact]
    public void GetNodeType_InvalidTypeId_ReturnsNull()
    {
        var result = NodeTypeRegistry.GetNodeType("NonExistent_Node");

        Assert.Null(result);
    }

    [Fact]
    public void GetNodeType_CaseInsensitive()
    {
        var result = NodeTypeRegistry.GetNodeType("event_onenter");

        Assert.NotNull(result);
        Assert.Equal("Event_OnEnter", result.TypeId);
    }

    [Fact]
    public void GetNodesForOwnerType_Room_ReturnsRoomNodes()
    {
        var roomNodes = NodeTypeRegistry.GetNodesForOwnerType("Room").ToList();

        Assert.True(roomNodes.Count > 0);
        Assert.Contains(roomNodes, n => n.TypeId == "Event_OnEnter");
        Assert.Contains(roomNodes, n => n.TypeId == "Event_OnExit");
    }

    [Fact]
    public void GetNodesForOwnerType_IncludesWildcardNodes()
    {
        var roomNodes = NodeTypeRegistry.GetNodesForOwnerType("Room").ToList();

        // Wildcard nodes (OwnerTypes contains "*") should be included
        Assert.Contains(roomNodes, n => n.TypeId == "Action_ShowMessage");
        Assert.Contains(roomNodes, n => n.TypeId == "Condition_HasItem");
    }

    [Fact]
    public void GetNodesForOwnerType_Npc_ReturnsNpcNodes()
    {
        var npcNodes = NodeTypeRegistry.GetNodesForOwnerType("Npc").ToList();

        Assert.True(npcNodes.Count > 0);
        Assert.Contains(npcNodes, n => n.TypeId == "Event_OnTalk");
        Assert.Contains(npcNodes, n => n.TypeId == "Event_OnNpcAttack");
    }

    [Fact]
    public void GetNodesForOwnerType_GameObject_ReturnsObjectNodes()
    {
        var objectNodes = NodeTypeRegistry.GetNodesForOwnerType("GameObject").ToList();

        Assert.True(objectNodes.Count > 0);
        Assert.Contains(objectNodes, n => n.TypeId == "Event_OnTake");
        Assert.Contains(objectNodes, n => n.TypeId == "Event_OnDrop");
        Assert.Contains(objectNodes, n => n.TypeId == "Event_OnUse");
        Assert.Contains(objectNodes, n => n.TypeId == "Event_OnExamine");
    }

    [Fact]
    public void GetNodesForOwnerType_Npc_ReturnsDialogueNodes()
    {
        // Los nodos de conversación están asociados a NPCs (no a "Conversation")
        var npcNodes = NodeTypeRegistry.GetNodesForOwnerType("Npc").ToList();

        Assert.True(npcNodes.Count > 0);
        Assert.Contains(npcNodes, n => n.TypeId == "Conversation_Start");
        Assert.Contains(npcNodes, n => n.TypeId == "Conversation_NpcSay");
        Assert.Contains(npcNodes, n => n.TypeId == "Conversation_PlayerChoice");
        Assert.Contains(npcNodes, n => n.TypeId == "Conversation_End");
    }

    [Fact]
    public void GetNodesByCategory_Event_ReturnsEventNodes()
    {
        var eventNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Event).ToList();

        Assert.True(eventNodes.Count > 0);
        Assert.All(eventNodes, n => Assert.Equal(NodeCategory.Event, n.Category));
        Assert.Contains(eventNodes, n => n.TypeId == "Event_OnEnter");
        Assert.Contains(eventNodes, n => n.TypeId == "Event_OnGameStart");
    }

    [Fact]
    public void GetNodesByCategory_Action_ReturnsActionNodes()
    {
        var actionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Action).ToList();

        Assert.True(actionNodes.Count > 0);
        Assert.All(actionNodes, n => Assert.Equal(NodeCategory.Action, n.Category));
        Assert.Contains(actionNodes, n => n.TypeId == "Action_ShowMessage");
        Assert.Contains(actionNodes, n => n.TypeId == "Action_GiveItem");
    }

    [Fact]
    public void GetNodesByCategory_Condition_ReturnsConditionNodes()
    {
        var conditionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Condition).ToList();

        Assert.True(conditionNodes.Count > 0);
        Assert.All(conditionNodes, n => Assert.Equal(NodeCategory.Condition, n.Category));
        Assert.Contains(conditionNodes, n => n.TypeId == "Condition_HasItem");
        Assert.Contains(conditionNodes, n => n.TypeId == "Condition_HasFlag");
    }

    [Fact]
    public void GetNodesByCategory_Flow_ReturnsFlowNodes()
    {
        var flowNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Flow).ToList();

        Assert.True(flowNodes.Count > 0);
        Assert.All(flowNodes, n => Assert.Equal(NodeCategory.Flow, n.Category));
        Assert.Contains(flowNodes, n => n.TypeId == "Flow_Branch");
        Assert.Contains(flowNodes, n => n.TypeId == "Flow_Sequence");
    }

    [Fact]
    public void GetNodesByCategory_Variable_ReturnsVariableNodes()
    {
        var variableNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Variable).ToList();

        Assert.True(variableNodes.Count > 0);
        Assert.All(variableNodes, n => Assert.Equal(NodeCategory.Variable, n.Category));
        Assert.Contains(variableNodes, n => n.TypeId == "Variable_GetFlag");
        Assert.Contains(variableNodes, n => n.TypeId == "Variable_GetCounter");
    }

    [Fact]
    public void GetNodesByCategory_Dialogue_ReturnsDialogueNodes()
    {
        var dialogueNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Dialogue).ToList();

        Assert.True(dialogueNodes.Count > 0);
        Assert.All(dialogueNodes, n => Assert.Equal(NodeCategory.Dialogue, n.Category));
        Assert.Contains(dialogueNodes, n => n.TypeId == "Conversation_Start");
        Assert.Contains(dialogueNodes, n => n.TypeId == "Conversation_NpcSay");
    }

    #region Node Definition Property Tests

    [Fact]
    public void ActionShowMessage_HasRequiredMessageProperty()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_ShowMessage");

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var messageProp = node.Properties.FirstOrDefault(p => p.Name == "Message");
        Assert.NotNull(messageProp);
        Assert.True(messageProp.IsRequired);
    }

    [Fact]
    public void EventOnEnter_HasExecutionOutput()
    {
        var node = NodeTypeRegistry.GetNodeType("Event_OnEnter");

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
    }

    [Fact]
    public void ConditionHasItem_HasTrueAndFalseOutputs()
    {
        var node = NodeTypeRegistry.GetNodeType("Condition_HasItem");

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "True" && p.PortType == PortType.Execution);
        Assert.Contains(node.OutputPorts, p => p.Name == "False" && p.PortType == PortType.Execution);
    }

    [Fact]
    public void FlowBranch_HasConditionDataInput()
    {
        var node = NodeTypeRegistry.GetNodeType("Flow_Branch");

        Assert.NotNull(node);
        Assert.NotNull(node.InputPorts);
        var conditionPort = node.InputPorts.FirstOrDefault(p => p.Name == "Condition");
        Assert.NotNull(conditionPort);
        Assert.Equal(PortType.Data, conditionPort.PortType);
        Assert.Equal("bool", conditionPort.DataType);
    }

    [Fact]
    public void MathAdd_HasTwoIntInputsAndIntOutput()
    {
        var node = NodeTypeRegistry.GetNodeType("Math_Add");

        Assert.NotNull(node);
        Assert.NotNull(node.InputPorts);
        Assert.NotNull(node.OutputPorts);

        var inputA = node.InputPorts.FirstOrDefault(p => p.Name == "A");
        var inputB = node.InputPorts.FirstOrDefault(p => p.Name == "B");
        var output = node.OutputPorts.FirstOrDefault(p => p.Name == "Result");

        Assert.NotNull(inputA);
        Assert.NotNull(inputB);
        Assert.NotNull(output);
        Assert.Equal("int", inputA.DataType);
        Assert.Equal("int", inputB.DataType);
        Assert.Equal("int", output.DataType);
    }

    [Fact]
    public void ConversationPlayerChoice_HasFourOptionOutputs()
    {
        var node = NodeTypeRegistry.GetNodeType("Conversation_PlayerChoice");

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Option1");
        Assert.Contains(node.OutputPorts, p => p.Name == "Option2");
        Assert.Contains(node.OutputPorts, p => p.Name == "Option3");
        Assert.Contains(node.OutputPorts, p => p.Name == "Option4");
    }

    [Fact]
    public void ConversationShop_HasShopOutputs()
    {
        var node = NodeTypeRegistry.GetNodeType("Conversation_Shop");

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "OnClose");
        Assert.Contains(node.OutputPorts, p => p.Name == "OnBuy");
        Assert.Contains(node.OutputPorts, p => p.Name == "OnSell");
    }

    #endregion

    #region Specific Node Type Existence Tests

    [Theory]
    [InlineData("Event_OnGameStart")]
    [InlineData("Event_OnEnter")]
    [InlineData("Event_OnExit")]
    [InlineData("Event_OnTalk")]
    [InlineData("Event_OnTake")]
    [InlineData("Event_OnDrop")]
    [InlineData("Event_OnUse")]
    [InlineData("Event_OnExamine")]
    [InlineData("Event_OnQuestStart")]
    [InlineData("Event_OnQuestComplete")]
    public void EventNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData("Action_ShowMessage")]
    [InlineData("Action_GiveItem")]
    [InlineData("Action_RemoveItem")]
    [InlineData("Action_TeleportPlayer")]
    [InlineData("Action_SetFlag")]
    [InlineData("Action_SetCounter")]
    [InlineData("Action_StartQuest")]
    [InlineData("Action_CompleteQuest")]
    [InlineData("Action_AddGold")]
    [InlineData("Action_RemoveGold")]
    public void ActionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Theory]
    [InlineData("Condition_HasItem")]
    [InlineData("Condition_IsInRoom")]
    [InlineData("Condition_HasFlag")]
    [InlineData("Condition_CompareCounter")]
    [InlineData("Condition_Random")]
    public void ConditionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
    }

    [Theory]
    [InlineData("Flow_Branch")]
    [InlineData("Flow_Sequence")]
    [InlineData("Flow_Delay")]
    [InlineData("Flow_RandomBranch")]
    public void FlowNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Flow, node.Category);
    }

    [Theory]
    [InlineData("Conversation_Start")]
    [InlineData("Conversation_NpcSay")]
    [InlineData("Conversation_PlayerChoice")]
    [InlineData("Conversation_Branch")]
    [InlineData("Conversation_End")]
    [InlineData("Conversation_Shop")]
    public void ConversationNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Dialogue, node.Category);
    }

    #endregion

    #region Quest Node Tests

    [Fact]
    public void Action_StartQuest_HasQuestIdProperty()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_StartQuest");

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var questIdProp = node.Properties.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Action_CompleteQuest_HasQuestIdProperty()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_CompleteQuest");

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var questIdProp = node.Properties.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Action_FailQuest_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_FailQuest");

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var questIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Event_OnQuestStart_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Event_OnQuestStart");

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        // Event nodes don't have Properties, they have OutputPorts
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec");
    }

    [Fact]
    public void Event_OnQuestComplete_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Event_OnQuestComplete");

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        // Event nodes don't have Properties, they have OutputPorts
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec");
    }

    #endregion

    #region Door Action Node Tests

    [Theory]
    [InlineData("Action_OpenDoor")]
    [InlineData("Action_CloseDoor")]
    [InlineData("Action_LockDoor")]
    [InlineData("Action_UnlockDoor")]
    public void DoorActionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        // All door actions should have a DoorId property
        Assert.NotNull(node.Properties);
        Assert.True(node.Properties.Length > 0, $"{typeId} should have at least one property");
        var doorIdProp = node.Properties.FirstOrDefault(p => p.Name == "DoorId");
        Assert.NotNull(doorIdProp);
    }

    #endregion

    #region Object Visibility Node Tests

    [Fact]
    public void Action_SetObjectVisible_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_SetObjectVisible");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var objectIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "ObjectId");
        Assert.NotNull(objectIdProp);
        var visibleProp = node?.Properties?.FirstOrDefault(p => p.Name == "Visible");
        Assert.NotNull(visibleProp);
    }

    [Fact]
    public void Action_SetNpcVisible_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_SetNpcVisible");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var npcIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "NpcId");
        Assert.NotNull(npcIdProp);
    }

    #endregion

    #region Counter Node Tests

    [Fact]
    public void Action_IncrementCounter_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_IncrementCounter");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var counterNameProp = node?.Properties?.FirstOrDefault(p => p.Name == "CounterName");
        Assert.NotNull(counterNameProp);
    }

    [Fact]
    public void Action_SetCounter_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_SetCounter");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var counterNameProp = node?.Properties?.FirstOrDefault(p => p.Name == "CounterName");
        Assert.NotNull(counterNameProp);
        var valueProp = node?.Properties?.FirstOrDefault(p => p.Name == "Value");
        Assert.NotNull(valueProp);
    }

    #endregion

    #region Additional Event Nodes Tests

    [Theory]
    [InlineData("Event_OnGameEnd")]
    [InlineData("Event_EveryMinute")]
    [InlineData("Event_EveryHour")]
    [InlineData("Event_OnTurnStart")]
    [InlineData("Event_OnWeatherChange")]
    [InlineData("Event_OnQuestFail")]
    [InlineData("Event_OnObjectiveComplete")]
    public void AdditionalEventNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
    }

    [Theory]
    [InlineData("Event_OnDoorOpen")]
    [InlineData("Event_OnDoorClose")]
    [InlineData("Event_OnDoorLock")]
    [InlineData("Event_OnDoorUnlock")]
    [InlineData("Event_OnDoorKnock")]
    public void DoorEventNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData("Event_OnNpcDeath")]
    [InlineData("Event_OnNpcSee")]
    public void NpcEventNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData("Event_OnContainerOpen")]
    [InlineData("Event_OnContainerClose")]
    public void ContainerEventNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    #endregion

    #region Additional Condition Nodes Tests

    [Theory]
    [InlineData("Condition_IsQuestStatus")]
    [InlineData("Condition_IsTimeOfDay")]
    [InlineData("Condition_IsDoorOpen")]
    [InlineData("Condition_IsNpcVisible")]
    [InlineData("Condition_IsPatrolling")]
    [InlineData("Condition_IsFollowingPlayer")]
    public void AdditionalConditionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "True" && p.PortType == PortType.Execution);
        Assert.Contains(node.OutputPorts, p => p.Name == "False" && p.PortType == PortType.Execution);
    }

    #endregion

    #region Additional Action Nodes Tests

    [Theory]
    [InlineData("Action_MoveNpc")]
    [InlineData("Action_StartPatrol")]
    [InlineData("Action_StopPatrol")]
    [InlineData("Action_PatrolStep")]
    [InlineData("Action_SetPatrolMode")]
    [InlineData("Action_FollowPlayer")]
    [InlineData("Action_StopFollowing")]
    [InlineData("Action_SetFollowMode")]
    [InlineData("Action_StartConversation")]
    public void NpcActionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Theory]
    [InlineData("Action_SetGold")]
    [InlineData("Action_AddGoldData")]
    [InlineData("Action_RemoveGoldData")]
    [InlineData("Action_SetCounterData")]
    [InlineData("Action_IncrementCounterData")]
    public void DataDrivenActionNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Fact]
    public void Action_PlaySound_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Action_PlaySound");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var soundIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "SoundId");
        Assert.NotNull(soundIdProp);
    }

    #endregion

    #region Variable Nodes Tests

    [Theory]
    [InlineData("Variable_GetGameHour")]
    [InlineData("Variable_GetPlayerGold")]
    [InlineData("Variable_GetCurrentRoom")]
    [InlineData("Variable_GetCurrentWeather")]
    public void GameStateVariableNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.OutputPorts);
        Assert.True(node.OutputPorts.Length > 0);
    }

    [Theory]
    [InlineData("Variable_GetPlayerStrength")]
    [InlineData("Variable_GetPlayerConstitution")]
    [InlineData("Variable_GetPlayerIntelligence")]
    [InlineData("Variable_GetPlayerDexterity")]
    [InlineData("Variable_GetPlayerCharisma")]
    [InlineData("Variable_GetPlayerWeight")]
    [InlineData("Variable_GetPlayerAge")]
    [InlineData("Variable_GetPlayerHeight")]
    [InlineData("Variable_GetPlayerInitialMoney")]
    public void PlayerStatVariableNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Theory]
    [InlineData("Variable_ConstantInt")]
    [InlineData("Variable_ConstantBool")]
    public void ConstantVariableNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.Properties);
        Assert.True(node.Properties.Length > 0);
    }

    #endregion

    #region Compare Nodes Tests

    [Theory]
    [InlineData("Compare_Int")]
    [InlineData("Compare_PlayerGold")]
    [InlineData("Compare_Counter")]
    public void CompareNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
        Assert.NotNull(node.OutputPorts);
        var resultPort = node.OutputPorts.FirstOrDefault(p => p.DataType == "bool");
        Assert.NotNull(resultPort);
    }

    #endregion

    #region Math Nodes Tests

    [Theory]
    [InlineData("Math_Add")]
    [InlineData("Math_Subtract")]
    [InlineData("Math_Multiply")]
    [InlineData("Math_Divide")]
    [InlineData("Math_Modulo")]
    public void BinaryMathNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.InputPorts);
        Assert.True(node.InputPorts.Length >= 2);
        Assert.NotNull(node.OutputPorts);
        Assert.True(node.OutputPorts.Length >= 1);
    }

    [Theory]
    [InlineData("Math_Negate")]
    [InlineData("Math_Abs")]
    public void UnaryMathNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Theory]
    [InlineData("Math_Min")]
    [InlineData("Math_Max")]
    public void MinMaxMathNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Fact]
    public void Math_Clamp_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Math_Clamp");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.InputPorts);
        // Clamp should have Value, Min, Max inputs
        Assert.True(node.InputPorts.Length >= 3);
    }

    [Fact]
    public void Math_Random_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Math_Random");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.OutputPorts);
    }

    #endregion

    #region Logic Nodes Tests

    [Theory]
    [InlineData("Logic_And")]
    [InlineData("Logic_Or")]
    [InlineData("Logic_Xor")]
    public void BinaryLogicNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
        Assert.NotNull(node.InputPorts);
        Assert.True(node.InputPorts.Length >= 2);
        Assert.NotNull(node.OutputPorts);
        var resultPort = node.OutputPorts.FirstOrDefault(p => p.DataType == "bool");
        Assert.NotNull(resultPort);
    }

    [Fact]
    public void Logic_Not_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Logic_Not");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
        Assert.NotNull(node.InputPorts);
        Assert.Single(node.InputPorts);
        Assert.NotNull(node.OutputPorts);
    }

    #endregion

    #region Select Nodes Tests

    [Theory]
    [InlineData("Select_Int")]
    [InlineData("Select_Bool")]
    public void SelectNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Flow, node.Category);
        Assert.NotNull(node.InputPorts);
        // Select nodes should have Condition input
        var conditionPort = node.InputPorts.FirstOrDefault(p => p.Name == "Condition");
        Assert.NotNull(conditionPort);
    }

    #endregion

    #region Additional Conversation Nodes Tests

    [Fact]
    public void Conversation_Action_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType("Conversation_Action");
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Dialogue, node.Category);
    }

    [Theory]
    [InlineData("Conversation_BuyItem")]
    [InlineData("Conversation_SellItem")]
    public void ConversationTradeNodes_Exist(string typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Dialogue, node.Category);
    }

    #endregion

    #region Owner Type Tests

    [Fact]
    public void GetNodesForOwnerType_Game_ReturnsGameNodes()
    {
        var gameNodes = NodeTypeRegistry.GetNodesForOwnerType("Game").ToList();

        Assert.True(gameNodes.Count > 0);
        Assert.Contains(gameNodes, n => n.TypeId == "Event_OnGameStart");
        Assert.Contains(gameNodes, n => n.TypeId == "Event_OnGameEnd");
    }

    [Fact]
    public void GetNodesForOwnerType_Door_ReturnsDoorNodes()
    {
        var doorNodes = NodeTypeRegistry.GetNodesForOwnerType("Door").ToList();

        Assert.True(doorNodes.Count > 0);
        Assert.Contains(doorNodes, n => n.TypeId == "Event_OnDoorOpen");
        Assert.Contains(doorNodes, n => n.TypeId == "Event_OnDoorClose");
    }

    [Fact]
    public void GetNodesForOwnerType_Quest_ReturnsQuestNodes()
    {
        var questNodes = NodeTypeRegistry.GetNodesForOwnerType("Quest").ToList();

        Assert.True(questNodes.Count > 0);
        Assert.Contains(questNodes, n => n.TypeId == "Event_OnQuestStart");
        Assert.Contains(questNodes, n => n.TypeId == "Event_OnQuestComplete");
        Assert.Contains(questNodes, n => n.TypeId == "Event_OnQuestFail");
    }

    [Fact]
    public void GetNodesForOwnerType_AllOwnerTypes_ReturnNodes()
    {
        var ownerTypes = new[] { "Game", "Room", "Door", "Npc", "GameObject", "Quest" };

        foreach (var ownerType in ownerTypes)
        {
            var nodes = NodeTypeRegistry.GetNodesForOwnerType(ownerType).ToList();
            Assert.True(nodes.Count > 0, $"OwnerType '{ownerType}' should have nodes");
        }
    }

    [Fact]
    public void GetNodesForOwnerType_Unknown_ReturnsOnlyWildcardNodes()
    {
        var unknownNodes = NodeTypeRegistry.GetNodesForOwnerType("UnknownType").ToList();

        // Should only return wildcard nodes (those with "*" owner type)
        // These are common nodes like Action_ShowMessage
        Assert.True(unknownNodes.Count > 0);
        Assert.All(unknownNodes, n => Assert.Contains("*", n.OwnerTypes));
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public void GetNodeType_EmptyString_ReturnsNull()
    {
        var result = NodeTypeRegistry.GetNodeType("");
        Assert.Null(result);
    }

    [Fact]
    public void GetNodeType_WhitespaceOnly_ReturnsNull()
    {
        var result = NodeTypeRegistry.GetNodeType("   ");
        Assert.Null(result);
    }

    [Fact]
    public void AllNodeTypes_HaveValidCategory()
    {
        var allNodes = NodeTypeRegistry.Types.Values;

        foreach (var node in allNodes)
        {
            Assert.True(Enum.IsDefined(typeof(NodeCategory), node.Category),
                $"Node {node.TypeId} has invalid category");
        }
    }

    [Fact]
    public void AllNodeTypes_HaveDisplayName()
    {
        var allNodes = NodeTypeRegistry.Types.Values;

        foreach (var node in allNodes)
        {
            Assert.False(string.IsNullOrWhiteSpace(node.DisplayName),
                $"Node {node.TypeId} should have a display name");
        }
    }

    [Fact]
    public void AllEventNodes_HaveExecOutputPort()
    {
        var eventNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Event).ToList();

        foreach (var node in eventNodes)
        {
            Assert.NotNull(node.OutputPorts);
            Assert.Contains(node.OutputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
        }
    }

    [Fact]
    public void AllActionNodes_HaveExecInputPort()
    {
        var actionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Action).ToList();

        foreach (var node in actionNodes)
        {
            Assert.NotNull(node.InputPorts);
            Assert.Contains(node.InputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
        }
    }

    [Fact]
    public void AllConditionNodes_HaveOutputPorts()
    {
        var conditionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Condition).ToList();

        foreach (var node in conditionNodes)
        {
            Assert.NotNull(node.OutputPorts);
            Assert.True(node.OutputPorts.Length > 0, $"Condition node {node.TypeId} should have output ports");
            // Condition nodes either have True/False execution outputs or a Result data output
            var hasTrueFalse = node.OutputPorts.Any(p => p.Name == "True") &&
                               node.OutputPorts.Any(p => p.Name == "False");
            var hasResult = node.OutputPorts.Any(p => p.Name == "Result" && p.DataType == "bool");
            Assert.True(hasTrueFalse || hasResult,
                $"Condition node {node.TypeId} should have True/False or Result output");
        }
    }

    [Fact]
    public void FlowControlNodes_HaveExecInputPort()
    {
        // Only test the core flow control nodes, not data-flow nodes like Select_*
        var flowControlTypeIds = new[] { "Flow_Branch", "Flow_Sequence", "Flow_Delay", "Flow_RandomBranch" };

        foreach (var typeId in flowControlTypeIds)
        {
            var node = NodeTypeRegistry.GetNodeType(typeId);
            Assert.NotNull(node);
            Assert.NotNull(node.InputPorts);
            Assert.Contains(node.InputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
        }
    }

    [Fact]
    public void Types_IsCaseInsensitive()
    {
        var types = NodeTypeRegistry.Types;

        // Should be able to look up by any case
        Assert.True(types.ContainsKey("Event_OnEnter"));
        Assert.True(types.ContainsKey("event_onenter"));
        Assert.True(types.ContainsKey("EVENT_ONENTER"));
    }

    #endregion
}
