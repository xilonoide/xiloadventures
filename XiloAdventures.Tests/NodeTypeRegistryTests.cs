using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
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
        var result = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnEnter);

        Assert.NotNull(result);
        Assert.Equal(NodeTypeId.Event_OnEnter, result.TypeId);
        Assert.Equal(NodeCategory.Event, result.Category);
    }

    [Fact]
    public void GetNodeType_InvalidTypeId_ReturnsNull()
    {
        var result = NodeTypeRegistry.GetNodeType((NodeTypeId)99999);

        Assert.Null(result);
    }

    [Fact]
    public void GetNodesForOwnerType_Room_ReturnsRoomNodes()
    {
        var roomNodes = NodeTypeRegistry.GetNodesForOwnerType("Room").ToList();

        Assert.True(roomNodes.Count > 0);
        Assert.Contains(roomNodes, n => n.TypeId == NodeTypeId.Event_OnEnter);
        Assert.Contains(roomNodes, n => n.TypeId == NodeTypeId.Event_OnExit);
    }

    [Fact]
    public void GetNodesForOwnerType_IncludesWildcardNodes()
    {
        var roomNodes = NodeTypeRegistry.GetNodesForOwnerType("Room").ToList();

        // Wildcard nodes (OwnerTypes contains "*") should be included
        Assert.Contains(roomNodes, n => n.TypeId == NodeTypeId.Action_ShowMessage);
        Assert.Contains(roomNodes, n => n.TypeId == NodeTypeId.Condition_HasItem);
    }

    [Fact]
    public void GetNodesForOwnerType_Npc_ReturnsNpcNodes()
    {
        var npcNodes = NodeTypeRegistry.GetNodesForOwnerType("Npc").ToList();

        Assert.True(npcNodes.Count > 0);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Event_OnTalk);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Event_OnNpcAttack);
    }

    [Fact]
    public void GetNodesForOwnerType_GameObject_ReturnsObjectNodes()
    {
        var objectNodes = NodeTypeRegistry.GetNodesForOwnerType("GameObject").ToList();

        Assert.True(objectNodes.Count > 0);
        Assert.Contains(objectNodes, n => n.TypeId == NodeTypeId.Event_OnTake);
        Assert.Contains(objectNodes, n => n.TypeId == NodeTypeId.Event_OnDrop);
        Assert.Contains(objectNodes, n => n.TypeId == NodeTypeId.Event_OnUse);
        Assert.Contains(objectNodes, n => n.TypeId == NodeTypeId.Event_OnExamine);
    }

    [Fact]
    public void GetNodesForOwnerType_Npc_ReturnsDialogueNodes()
    {
        // Los nodos de conversación están asociados a NPCs (no a "Conversation")
        var npcNodes = NodeTypeRegistry.GetNodesForOwnerType("Npc").ToList();

        Assert.True(npcNodes.Count > 0);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Conversation_Start);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Conversation_NpcSay);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Conversation_PlayerChoice);
        Assert.Contains(npcNodes, n => n.TypeId == NodeTypeId.Conversation_End);
    }

    [Fact]
    public void GetNodesByCategory_Event_ReturnsEventNodes()
    {
        var eventNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Event).ToList();

        Assert.True(eventNodes.Count > 0);
        Assert.All(eventNodes, n => Assert.Equal(NodeCategory.Event, n.Category));
        Assert.Contains(eventNodes, n => n.TypeId == NodeTypeId.Event_OnEnter);
        Assert.Contains(eventNodes, n => n.TypeId == NodeTypeId.Event_OnGameStart);
    }

    [Fact]
    public void GetNodesByCategory_Action_ReturnsActionNodes()
    {
        var actionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Action).ToList();

        Assert.True(actionNodes.Count > 0);
        Assert.All(actionNodes, n => Assert.Equal(NodeCategory.Action, n.Category));
        Assert.Contains(actionNodes, n => n.TypeId == NodeTypeId.Action_ShowMessage);
        Assert.Contains(actionNodes, n => n.TypeId == NodeTypeId.Action_GiveItem);
    }

    [Fact]
    public void GetNodesByCategory_Condition_ReturnsConditionNodes()
    {
        var conditionNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Condition).ToList();

        Assert.True(conditionNodes.Count > 0);
        Assert.All(conditionNodes, n => Assert.Equal(NodeCategory.Condition, n.Category));
        Assert.Contains(conditionNodes, n => n.TypeId == NodeTypeId.Condition_HasItem);
        Assert.Contains(conditionNodes, n => n.TypeId == NodeTypeId.Condition_HasFlag);
    }

    [Fact]
    public void GetNodesByCategory_Flow_ReturnsFlowNodes()
    {
        var flowNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Flow).ToList();

        Assert.True(flowNodes.Count > 0);
        Assert.All(flowNodes, n => Assert.Equal(NodeCategory.Flow, n.Category));
        Assert.Contains(flowNodes, n => n.TypeId == NodeTypeId.Flow_Branch);
        Assert.Contains(flowNodes, n => n.TypeId == NodeTypeId.Flow_Sequence);
    }

    [Fact]
    public void GetNodesByCategory_Variable_ReturnsVariableNodes()
    {
        var variableNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Variable).ToList();

        Assert.True(variableNodes.Count > 0);
        Assert.All(variableNodes, n => Assert.Equal(NodeCategory.Variable, n.Category));
        Assert.Contains(variableNodes, n => n.TypeId == NodeTypeId.Variable_GetFlag);
        Assert.Contains(variableNodes, n => n.TypeId == NodeTypeId.Variable_GetCounter);
    }

    [Fact]
    public void GetNodesByCategory_Dialogue_ReturnsDialogueNodes()
    {
        var dialogueNodes = NodeTypeRegistry.GetNodesByCategory(NodeCategory.Dialogue).ToList();

        Assert.True(dialogueNodes.Count > 0);
        Assert.All(dialogueNodes, n => Assert.Equal(NodeCategory.Dialogue, n.Category));
        Assert.Contains(dialogueNodes, n => n.TypeId == NodeTypeId.Conversation_Start);
        Assert.Contains(dialogueNodes, n => n.TypeId == NodeTypeId.Conversation_NpcSay);
    }

    #region Node Definition Property Tests

    [Fact]
    public void ActionShowMessage_HasRequiredMessageProperty()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_ShowMessage);

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var messageProp = node.Properties.FirstOrDefault(p => p.Name == "Message");
        Assert.NotNull(messageProp);
        Assert.True(messageProp.IsRequired);
    }

    [Fact]
    public void EventOnEnter_HasExecutionOutput()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnEnter);

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
    }

    [Fact]
    public void ConditionHasItem_HasTrueAndFalseOutputs()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Condition_HasItem);

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "True" && p.PortType == PortType.Execution);
        Assert.Contains(node.OutputPorts, p => p.Name == "False" && p.PortType == PortType.Execution);
    }

    [Fact]
    public void FlowBranch_HasConditionDataInput()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Flow_Branch);

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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Math_Add);

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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Conversation_PlayerChoice);

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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Conversation_Shop);

        Assert.NotNull(node);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "OnClose");
        Assert.Contains(node.OutputPorts, p => p.Name == "OnBuy");
        Assert.Contains(node.OutputPorts, p => p.Name == "OnSell");
    }

    #endregion

    #region Specific Node Type Existence Tests

    [Theory]
    [InlineData(NodeTypeId.Event_OnGameStart)]
    [InlineData(NodeTypeId.Event_OnEnter)]
    [InlineData(NodeTypeId.Event_OnExit)]
    [InlineData(NodeTypeId.Event_OnTalk)]
    [InlineData(NodeTypeId.Event_OnTake)]
    [InlineData(NodeTypeId.Event_OnDrop)]
    [InlineData(NodeTypeId.Event_OnUse)]
    [InlineData(NodeTypeId.Event_OnExamine)]
    [InlineData(NodeTypeId.Event_OnQuestStart)]
    [InlineData(NodeTypeId.Event_OnQuestComplete)]
    public void EventNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Action_ShowMessage)]
    [InlineData(NodeTypeId.Action_GiveItem)]
    [InlineData(NodeTypeId.Action_RemoveItem)]
    [InlineData(NodeTypeId.Action_TeleportPlayer)]
    [InlineData(NodeTypeId.Action_SetFlag)]
    [InlineData(NodeTypeId.Action_SetCounter)]
    [InlineData(NodeTypeId.Action_StartQuest)]
    [InlineData(NodeTypeId.Action_CompleteQuest)]
    [InlineData(NodeTypeId.Action_AddMoney)]
    [InlineData(NodeTypeId.Action_RemoveMoney)]
    public void ActionNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Condition_HasItem)]
    [InlineData(NodeTypeId.Condition_IsInRoom)]
    [InlineData(NodeTypeId.Condition_HasFlag)]
    [InlineData(NodeTypeId.Condition_CompareCounter)]
    [InlineData(NodeTypeId.Condition_Random)]
    public void ConditionNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Flow_Branch)]
    [InlineData(NodeTypeId.Flow_Sequence)]
    [InlineData(NodeTypeId.Flow_Delay)]
    [InlineData(NodeTypeId.Flow_RandomBranch)]
    public void FlowNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Flow, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Conversation_Start)]
    [InlineData(NodeTypeId.Conversation_NpcSay)]
    [InlineData(NodeTypeId.Conversation_PlayerChoice)]
    [InlineData(NodeTypeId.Conversation_Branch)]
    [InlineData(NodeTypeId.Conversation_End)]
    [InlineData(NodeTypeId.Conversation_Shop)]
    public void ConversationNodes_Exist(NodeTypeId typeId)
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_StartQuest);

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var questIdProp = node.Properties.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Action_CompleteQuest_HasQuestIdProperty()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_CompleteQuest);

        Assert.NotNull(node);
        Assert.NotNull(node.Properties);
        var questIdProp = node.Properties.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Action_FailQuest_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_FailQuest);

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var questIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "QuestId");
        Assert.NotNull(questIdProp);
    }

    [Fact]
    public void Event_OnQuestStart_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnQuestStart);

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        // Event nodes don't have Properties, they have OutputPorts
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec");
    }

    [Fact]
    public void Event_OnQuestComplete_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Event_OnQuestComplete);

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        // Event nodes don't have Properties, they have OutputPorts
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec");
    }

    #endregion

    #region Door Action Node Tests

    [Theory]
    [InlineData(NodeTypeId.Action_OpenDoor)]
    [InlineData(NodeTypeId.Action_CloseDoor)]
    [InlineData(NodeTypeId.Action_LockDoor)]
    [InlineData(NodeTypeId.Action_UnlockDoor)]
    public void DoorActionNodes_Exist(NodeTypeId typeId)
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetObjectVisible);
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetNpcVisible);
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_IncrementCounter);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var counterNameProp = node?.Properties?.FirstOrDefault(p => p.Name == "CounterName");
        Assert.NotNull(counterNameProp);
    }

    [Fact]
    public void Action_SetCounter_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_SetCounter);
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
    [InlineData(NodeTypeId.Event_OnGameEnd)]
    [InlineData(NodeTypeId.Event_EveryMinute)]
    [InlineData(NodeTypeId.Event_EveryHour)]
    [InlineData(NodeTypeId.Event_OnTurnStart)]
    [InlineData(NodeTypeId.Event_OnWeatherChange)]
    [InlineData(NodeTypeId.Event_OnQuestFail)]
    [InlineData(NodeTypeId.Event_OnObjectiveComplete)]
    public void AdditionalEventNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
        Assert.NotNull(node.OutputPorts);
        Assert.Contains(node.OutputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
    }

    [Theory]
    [InlineData(NodeTypeId.Event_OnDoorOpen)]
    [InlineData(NodeTypeId.Event_OnDoorClose)]
    [InlineData(NodeTypeId.Event_OnDoorLock)]
    [InlineData(NodeTypeId.Event_OnDoorUnlock)]
    public void DoorEventNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Event_OnNpcDeath)]
    [InlineData(NodeTypeId.Event_OnNpcSee)]
    public void NpcEventNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Event_OnContainerOpen)]
    [InlineData(NodeTypeId.Event_OnContainerClose)]
    public void ContainerEventNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Event, node.Category);
    }

    #endregion

    #region Additional Condition Nodes Tests

    [Theory]
    [InlineData(NodeTypeId.Condition_IsQuestStatus)]
    [InlineData(NodeTypeId.Condition_IsTimeOfDay)]
    [InlineData(NodeTypeId.Condition_IsDoorOpen)]
    [InlineData(NodeTypeId.Condition_IsNpcVisible)]
    [InlineData(NodeTypeId.Condition_IsPatrolling)]
    [InlineData(NodeTypeId.Condition_IsFollowingPlayer)]
    public void AdditionalConditionNodes_Exist(NodeTypeId typeId)
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
    [InlineData(NodeTypeId.Action_MoveNpc)]
    [InlineData(NodeTypeId.Action_StartPatrol)]
    [InlineData(NodeTypeId.Action_StopPatrol)]
    [InlineData(NodeTypeId.Action_PatrolStep)]
    [InlineData(NodeTypeId.Action_SetPatrolMode)]
    [InlineData(NodeTypeId.Action_FollowPlayer)]
    [InlineData(NodeTypeId.Action_StopFollowing)]
    [InlineData(NodeTypeId.Action_SetFollowMode)]
    [InlineData(NodeTypeId.Action_StartConversation)]
    public void NpcActionNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Action_SetCounter)]
    [InlineData(NodeTypeId.Action_IncrementCounter)]
    [InlineData(NodeTypeId.Action_AddPlayerMoney)]
    [InlineData(NodeTypeId.Action_RemovePlayerMoney)]
    public void DataDrivenActionNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
    }

    [Fact]
    public void Action_PlaySound_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Action_PlaySound);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        var soundIdProp = node?.Properties?.FirstOrDefault(p => p.Name == "SoundId");
        Assert.NotNull(soundIdProp);
    }

    #endregion

    #region Variable Nodes Tests

    [Theory]
    [InlineData(NodeTypeId.Variable_GetGameHour)]
    [InlineData(NodeTypeId.Variable_GetPlayerMoney)]
    [InlineData(NodeTypeId.Variable_GetCurrentRoom)]
    [InlineData(NodeTypeId.Variable_GetCurrentWeather)]
    public void GameStateVariableNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.OutputPorts);
        Assert.True(node.OutputPorts.Length > 0);
    }

    [Theory]
    [InlineData(NodeTypeId.Variable_GetPlayerStrength)]
    [InlineData(NodeTypeId.Variable_GetPlayerConstitution)]
    [InlineData(NodeTypeId.Variable_GetPlayerIntelligence)]
    [InlineData(NodeTypeId.Variable_GetPlayerDexterity)]
    [InlineData(NodeTypeId.Variable_GetPlayerCharisma)]
    [InlineData(NodeTypeId.Variable_GetPlayerWeight)]
    [InlineData(NodeTypeId.Variable_GetPlayerAge)]
    [InlineData(NodeTypeId.Variable_GetPlayerHeight)]
    [InlineData(NodeTypeId.Variable_GetPlayerInitialMoney)]
    public void PlayerStatVariableNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Variable_ConstantInt)]
    [InlineData(NodeTypeId.Variable_ConstantBool)]
    public void ConstantVariableNodes_Exist(NodeTypeId typeId)
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
    [InlineData(NodeTypeId.Compare_Int)]
    [InlineData(NodeTypeId.Compare_PlayerMoney)]
    [InlineData(NodeTypeId.Compare_Counter)]
    public void CompareNodes_Exist(NodeTypeId typeId)
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
    [InlineData(NodeTypeId.Math_Add)]
    [InlineData(NodeTypeId.Math_Subtract)]
    [InlineData(NodeTypeId.Math_Multiply)]
    [InlineData(NodeTypeId.Math_Divide)]
    [InlineData(NodeTypeId.Math_Modulo)]
    public void BinaryMathNodes_Exist(NodeTypeId typeId)
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
    [InlineData(NodeTypeId.Math_Negate)]
    [InlineData(NodeTypeId.Math_Abs)]
    public void UnaryMathNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Math_Min)]
    [InlineData(NodeTypeId.Math_Max)]
    public void MinMaxMathNodes_Exist(NodeTypeId typeId)
    {
        var node = NodeTypeRegistry.GetNodeType(typeId);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
    }

    [Fact]
    public void Math_Clamp_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Math_Clamp);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.InputPorts);
        // Clamp should have Value, Min, Max inputs
        Assert.True(node.InputPorts.Length >= 3);
    }

    [Fact]
    public void Math_Random_Exists()
    {
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Math_Random);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Variable, node.Category);
        Assert.NotNull(node.OutputPorts);
    }

    #endregion

    #region Logic Nodes Tests

    [Theory]
    [InlineData(NodeTypeId.Logic_And)]
    [InlineData(NodeTypeId.Logic_Or)]
    [InlineData(NodeTypeId.Logic_Xor)]
    public void BinaryLogicNodes_Exist(NodeTypeId typeId)
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Logic_Not);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Condition, node.Category);
        Assert.NotNull(node.InputPorts);
        Assert.Single(node.InputPorts);
        Assert.NotNull(node.OutputPorts);
    }

    #endregion

    #region Select Nodes Tests

    [Theory]
    [InlineData(NodeTypeId.Select_Int)]
    [InlineData(NodeTypeId.Select_Bool)]
    public void SelectNodes_Exist(NodeTypeId typeId)
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
        var node = NodeTypeRegistry.GetNodeType(NodeTypeId.Conversation_Action);
        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Dialogue, node.Category);
    }

    [Theory]
    [InlineData(NodeTypeId.Conversation_BuyItem)]
    [InlineData(NodeTypeId.Conversation_SellItem)]
    public void ConversationTradeNodes_Exist(NodeTypeId typeId)
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
        Assert.Contains(gameNodes, n => n.TypeId == NodeTypeId.Event_OnGameStart);
        Assert.Contains(gameNodes, n => n.TypeId == NodeTypeId.Event_OnGameEnd);
    }

    [Fact]
    public void GetNodesForOwnerType_Door_ReturnsDoorNodes()
    {
        var doorNodes = NodeTypeRegistry.GetNodesForOwnerType("Door").ToList();

        Assert.True(doorNodes.Count > 0);
        Assert.Contains(doorNodes, n => n.TypeId == NodeTypeId.Event_OnDoorOpen);
        Assert.Contains(doorNodes, n => n.TypeId == NodeTypeId.Event_OnDoorClose);
    }

    [Fact]
    public void GetNodesForOwnerType_Quest_ReturnsQuestNodes()
    {
        var questNodes = NodeTypeRegistry.GetNodesForOwnerType("Quest").ToList();

        Assert.True(questNodes.Count > 0);
        Assert.Contains(questNodes, n => n.TypeId == NodeTypeId.Event_OnQuestStart);
        Assert.Contains(questNodes, n => n.TypeId == NodeTypeId.Event_OnQuestComplete);
        Assert.Contains(questNodes, n => n.TypeId == NodeTypeId.Event_OnQuestFail);
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

        // Should only return wildcard nodes (those with All owner type)
        // These are common nodes like Action_ShowMessage
        Assert.True(unknownNodes.Count > 0);
        Assert.All(unknownNodes, n => Assert.True(n.OwnerTypes.HasFlag(NodeOwnerType.All)));
    }

    #endregion

    #region Edge Cases Tests


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
        var flowControlTypeIds = new[] { NodeTypeId.Flow_Branch, NodeTypeId.Flow_Sequence, NodeTypeId.Flow_Delay, NodeTypeId.Flow_RandomBranch };

        foreach (var typeId in flowControlTypeIds)
        {
            var node = NodeTypeRegistry.GetNodeType(typeId);
            Assert.NotNull(node);
            Assert.NotNull(node.InputPorts);
            Assert.Contains(node.InputPorts, p => p.Name == "Exec" && p.PortType == PortType.Execution);
        }
    }

    [Fact]
    public void Types_ContainsExpectedKeys()
    {
        var types = NodeTypeRegistry.Types;

        // Should contain expected keys
        Assert.True(types.ContainsKey(NodeTypeId.Event_OnEnter));
        Assert.True(types.ContainsKey(NodeTypeId.Action_ShowMessage));
        Assert.True(types.ContainsKey(NodeTypeId.Condition_HasItem));
    }

    #endregion
}
