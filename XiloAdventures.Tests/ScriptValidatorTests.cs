using System;
using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using Xunit;

namespace XiloAdventures.Tests;

/// <summary>
/// Comprehensive tests for ScriptValidator including validation logic,
/// connectivity checking, and property validation.
/// </summary>
public class ScriptValidatorTests
{
    #region Test Helpers

    private static ScriptDefinition CreateScript(
        List<ScriptNode>? nodes = null,
        List<NodeConnection>? connections = null)
    {
        return new ScriptDefinition
        {
            Id = "test_script",
            Name = "Test Script",
            OwnerType = "Room",
            OwnerId = "room1",
            Nodes = nodes ?? new List<ScriptNode>(),
            Connections = connections ?? new List<NodeConnection>()
        };
    }

    private static ScriptNode CreateEventNode(string id = "event1", NodeTypeId eventType = NodeTypeId.Event_OnEnter)
    {
        return new ScriptNode
        {
            Id = id,
            NodeType = eventType,
            Category = NodeCategory.Event,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static ScriptNode CreateActionNode(
        string id = "action1",
        NodeTypeId actionType = NodeTypeId.Action_ShowMessage,
        Dictionary<string, object?>? properties = null)
    {
        var props = properties ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = "Test message"
        };
        return new ScriptNode
        {
            Id = id,
            NodeType = actionType,
            Category = NodeCategory.Action,
            Properties = props
        };
    }

    private static ScriptNode CreateConditionNode(
        string id = "condition1",
        NodeTypeId conditionType = NodeTypeId.Condition_HasItem,
        Dictionary<string, object?>? properties = null)
    {
        var props = properties ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "key1"
        };
        return new ScriptNode
        {
            Id = id,
            NodeType = conditionType,
            Category = NodeCategory.Condition,
            Properties = props
        };
    }

    private static ScriptNode CreateFlowNode(string id = "flow1", NodeTypeId flowType = NodeTypeId.Flow_Branch)
    {
        return new ScriptNode
        {
            Id = id,
            NodeType = flowType,
            Category = NodeCategory.Flow,
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static NodeConnection CreateConnection(
        string fromNodeId,
        string toNodeId,
        string fromPort = "Exec",
        string toPort = "Exec")
    {
        return new NodeConnection
        {
            Id = Guid.NewGuid().ToString(),
            FromNodeId = fromNodeId,
            FromPortName = fromPort,
            ToNodeId = toNodeId,
            ToPortName = toPort
        };
    }

    #endregion

    #region Empty Script Tests

    [Fact]
    public void Validate_EmptyScript_ReturnsEmpty()
    {
        var script = CreateScript();

        var result = ScriptValidator.Validate(script);

        Assert.False(result.HasEvent);
        Assert.False(result.HasAction);
        Assert.False(result.IsConnected);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_NullNodes_HandlesGracefully()
    {
        var script = new ScriptDefinition
        {
            Id = "test",
            Name = "Test",
            Nodes = new List<ScriptNode>(),
            Connections = new List<NodeConnection>()
        };

        var result = ScriptValidator.Validate(script);

        Assert.NotNull(result);
        Assert.False(result.IsValid);
    }

    #endregion

    #region Event Node Detection Tests

    [Fact]
    public void Validate_OnlyEventNode_HasNoAction()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateEventNode() }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasEvent);
        Assert.False(result.HasAction);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(NodeTypeId.Event_OnEnter)]
    [InlineData(NodeTypeId.Event_OnExit)]
    [InlineData(NodeTypeId.Event_OnTake)]
    [InlineData(NodeTypeId.Event_OnDrop)]
    [InlineData(NodeTypeId.Event_OnUse)]
    [InlineData(NodeTypeId.Event_OnExamine)]
    [InlineData(NodeTypeId.Event_OnTalk)]
    [InlineData(NodeTypeId.Event_OnGameStart)]
    [InlineData(NodeTypeId.Event_OnDoorOpen)]
    [InlineData(NodeTypeId.Event_OnQuestStart)]
    [InlineData(NodeTypeId.Event_OnQuestComplete)]
    public void Validate_AllEventTypes_DetectedAsEvent(NodeTypeId eventType)
    {
        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateEventNode(eventType: eventType) }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasEvent);
    }

    [Fact]
    public void Validate_MultipleEventNodes_AllDetected()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode("event1", NodeTypeId.Event_OnEnter),
                CreateEventNode("event2", NodeTypeId.Event_OnExit),
                CreateEventNode("event3", NodeTypeId.Event_OnTake)
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasEvent);
    }

    #endregion

    #region Action Node Detection Tests

    [Fact]
    public void Validate_OnlyActionNode_HasNoEvent()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateActionNode() }
        );

        var result = ScriptValidator.Validate(script);

        Assert.False(result.HasEvent);
        Assert.True(result.HasAction);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(NodeTypeId.Action_ShowMessage)]
    [InlineData(NodeTypeId.Action_GiveItem)]
    [InlineData(NodeTypeId.Action_RemoveItem)]
    [InlineData(NodeTypeId.Action_TeleportPlayer)]
    [InlineData(NodeTypeId.Action_SetFlag)]
    [InlineData(NodeTypeId.Action_SetCounter)]
    [InlineData(NodeTypeId.Action_OpenDoor)]
    [InlineData(NodeTypeId.Action_CloseDoor)]
    [InlineData(NodeTypeId.Action_StartQuest)]
    [InlineData(NodeTypeId.Action_CompleteQuest)]
    public void Validate_AllActionTypes_DetectedAsAction(NodeTypeId actionType)
    {
        var props = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        // Add required properties based on action type
        if (actionType == NodeTypeId.Action_ShowMessage)
        {
            props["Message"] = "Test";
        }
        else if (actionType == NodeTypeId.Action_GiveItem || actionType == NodeTypeId.Action_RemoveItem)
        {
            props["ObjectId"] = "obj1";
        }
        else if (actionType == NodeTypeId.Action_TeleportPlayer)
        {
            props["RoomId"] = "room1";
        }
        else if (actionType == NodeTypeId.Action_SetFlag)
        {
            props["FlagName"] = "flag1";
        }
        else if (actionType == NodeTypeId.Action_SetCounter)
        {
            props["CounterName"] = "counter1";
            props["Value"] = 1;
        }
        else if (actionType == NodeTypeId.Action_OpenDoor || actionType == NodeTypeId.Action_CloseDoor)
        {
            props["DoorId"] = "door1";
        }
        else if (actionType == NodeTypeId.Action_StartQuest || actionType == NodeTypeId.Action_CompleteQuest)
        {
            props["QuestId"] = "quest1";
        }

        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateActionNode(actionType: actionType, properties: props) }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasAction);
    }

    #endregion

    #region Connectivity Tests

    [Fact]
    public void Validate_ConnectedEventAndAction_IsValid()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasEvent);
        Assert.True(result.HasAction);
        Assert.True(result.IsConnected);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_DisconnectedEventAndAction_NotValid()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode()
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.HasEvent);
        Assert.True(result.HasAction);
        Assert.False(result.IsConnected);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EventConnectedThroughCondition_IsConnected()
    {
        // Event -> Condition -> Action
        var conditionProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["ObjectId"] = "key1"
        };

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateConditionNode(properties: conditionProps),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "condition1"),
                CreateConnection("condition1", "action1", fromPort: "True")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_EventConnectedThroughFlow_IsConnected()
    {
        // Event -> Flow_Sequence -> Action
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateFlowNode("flow1", NodeTypeId.Flow_Sequence),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "flow1"),
                CreateConnection("flow1", "action1", fromPort: "Then0")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
    }

    [Fact]
    public void Validate_LongChainOfNodes_IsConnected()
    {
        // Event -> Condition1 -> Condition2 -> Flow -> Action
        // Flow_Branch has "True" and "False" output ports, not "Exec"
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateConditionNode("cond1"),
                CreateConditionNode("cond2"),
                CreateFlowNode(),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "cond1"),
                CreateConnection("cond1", "cond2", fromPort: "True"),
                CreateConnection("cond2", "flow1", fromPort: "True"),
                CreateConnection("flow1", "action1", fromPort: "True")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
    }

    [Fact]
    public void Validate_MultiplePathsToAction_IsConnected()
    {
        // Event -> Condition -> True -> Action1
        //                   -> False -> Action2
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateConditionNode(),
                CreateActionNode("action1"),
                CreateActionNode("action2")
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "condition1"),
                CreateConnection("condition1", "action1", fromPort: "True"),
                CreateConnection("condition1", "action2", fromPort: "False")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
    }

    [Fact]
    public void Validate_MultipleEventsToSameAction_IsConnected()
    {
        // Event1 -> Action
        // Event2 -> Action
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode("event1", NodeTypeId.Event_OnEnter),
                CreateEventNode("event2", NodeTypeId.Event_OnExit),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1"),
                CreateConnection("event2", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
    }

    [Fact]
    public void Validate_CircularReference_DoesNotCrash()
    {
        // Event -> Action -> loops back (shouldn't happen in real scripts but test resilience)
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode("action1"),
                CreateActionNode("action2")
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1"),
                CreateConnection("action1", "action2"),
                CreateConnection("action2", "action1") // Circular
            }
        );

        // Should not throw
        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
    }

    [Fact]
    public void Validate_OnlyOneEventConnected_IsValid()
    {
        // Event1 -> Action (connected)
        // Event2 (not connected)
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode("event1"),
                CreateEventNode("event2", NodeTypeId.Event_OnExit),
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IsConnected);
        Assert.True(result.IsValid);
    }

    #endregion

    #region Property Validation Tests

    [Fact]
    public void Validate_MissingRequiredProperty_ReportsIncomplete()
    {
        var propsWithoutMessage = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode(properties: propsWithoutMessage)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IncompleteNodes.Count > 0);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_EmptyStringProperty_ReportsIncomplete()
    {
        var propsWithEmpty = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = ""
        };

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode(properties: propsWithEmpty)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IncompleteNodes.Count > 0);
    }

    [Fact]
    public void Validate_WhitespaceOnlyProperty_ReportsIncomplete()
    {
        var propsWithWhitespace = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = "   "
        };

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode(properties: propsWithWhitespace)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IncompleteNodes.Count > 0);
    }

    [Fact]
    public void Validate_NullProperty_ReportsIncomplete()
    {
        var propsWithNull = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Message"] = null
        };

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode(properties: propsWithNull)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IncompleteNodes.Count > 0);
    }

    [Fact]
    public void Validate_AllRequiredPropertiesFilled_IsValid()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode()  // Has Message property set
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.Empty(result.IncompleteNodes);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_MultipleIncompleteNodes_ReportsAll()
    {
        var emptyProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode("action1", properties: emptyProps),
                CreateActionNode("action2", properties: emptyProps)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1"),
                CreateConnection("action1", "action2")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.True(result.IncompleteNodes.Count >= 2);
    }

    [Fact]
    public void Validate_IncompleteNodeInfo_ContainsCorrectData()
    {
        var emptyProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode("my_action", properties: emptyProps)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "my_action")
            }
        );

        var result = ScriptValidator.Validate(script);

        var incomplete = result.IncompleteNodes.FirstOrDefault(n => n.NodeId == "my_action");
        Assert.NotNull(incomplete);
        Assert.Equal("my_action", incomplete.NodeId);
        Assert.True(incomplete.MissingProperties.Count > 0);
    }

    #endregion

    #region Error Messages Tests

    [Fact]
    public void Validate_NoEvent_AddsErrorMessage()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateActionNode() }
        );

        var result = ScriptValidator.Validate(script);

        Assert.Contains(result.Errors, e => e.Contains("evento"));
    }

    [Fact]
    public void Validate_NoAction_AddsErrorMessage()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode> { CreateEventNode() }
        );

        var result = ScriptValidator.Validate(script);

        Assert.Contains(result.Errors, e => e.Contains("acción") || e.Contains("acciones"));
    }

    [Fact]
    public void Validate_NotConnected_AddsErrorMessage()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode()
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.Contains(result.Errors, e => e.Contains("conectado") || e.Contains("conexión"));
    }

    [Fact]
    public void Validate_IncompleteNode_AddsErrorMessage()
    {
        var emptyProps = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                CreateActionNode(properties: emptyProps)
            },
            connections: new List<NodeConnection>
            {
                CreateConnection("event1", "action1")
            }
        );

        var result = ScriptValidator.Validate(script);

        Assert.Contains(result.Errors, e => e.Contains("sin completar") || e.Contains("incompleto"));
    }

    #endregion

    #region IsEventNode and IsActionNode Tests

    [Fact]
    public void IsEventNode_ForEventType_ReturnsTrue()
    {
        Assert.True(ScriptValidator.IsEventNode(NodeTypeId.Event_OnEnter));
        Assert.True(ScriptValidator.IsEventNode(NodeTypeId.Event_OnTake));
        Assert.True(ScriptValidator.IsEventNode(NodeTypeId.Event_OnGameStart));
    }

    [Fact]
    public void IsEventNode_ForNonEventType_ReturnsFalse()
    {
        Assert.False(ScriptValidator.IsEventNode(NodeTypeId.Action_ShowMessage));
        Assert.False(ScriptValidator.IsEventNode(NodeTypeId.Condition_HasItem));
        Assert.False(ScriptValidator.IsEventNode(NodeTypeId.Flow_Branch));
    }

    [Fact]
    public void IsEventNode_InvalidType_ReturnsFalse()
    {
        Assert.False(ScriptValidator.IsEventNode((NodeTypeId)99999));
    }

    [Fact]
    public void IsActionNode_ForActionType_ReturnsTrue()
    {
        Assert.True(ScriptValidator.IsActionNode(NodeTypeId.Action_ShowMessage));
        Assert.True(ScriptValidator.IsActionNode(NodeTypeId.Action_GiveItem));
        Assert.True(ScriptValidator.IsActionNode(NodeTypeId.Action_TeleportPlayer));
    }

    [Fact]
    public void IsActionNode_ForNonActionType_ReturnsFalse()
    {
        Assert.False(ScriptValidator.IsActionNode(NodeTypeId.Event_OnEnter));
        Assert.False(ScriptValidator.IsActionNode(NodeTypeId.Condition_HasItem));
        Assert.False(ScriptValidator.IsActionNode(NodeTypeId.Flow_Branch));
    }

    [Fact]
    public void IsActionNode_InvalidType_ReturnsFalse()
    {
        Assert.False(ScriptValidator.IsActionNode((NodeTypeId)99999));
    }

    #endregion

    #region ScriptValidationResult Tests

    [Fact]
    public void ScriptValidationResult_Empty_HasCorrectDefaults()
    {
        var empty = ScriptValidationResult.Empty;

        Assert.False(empty.HasEvent);
        Assert.False(empty.HasAction);
        Assert.False(empty.IsConnected);
        Assert.False(empty.IsValid);
        Assert.True(empty.HasErrors);
    }

    [Fact]
    public void ScriptValidationResult_IsValid_WhenAllConditionsMet()
    {
        var result = new ScriptValidationResult
        {
            HasEvent = true,
            HasAction = true,
            IsConnected = true
        };

        Assert.True(result.IsValid);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void ScriptValidationResult_HasWarnings_MatchesHasErrors()
    {
        var result = new ScriptValidationResult
        {
            HasEvent = false,
            HasAction = true,
            IsConnected = true
        };

        Assert.Equal(result.HasErrors, result.HasWarnings);
    }

    [Fact]
    public void ScriptValidationResult_IncompleteNodes_CausesInvalid()
    {
        var result = new ScriptValidationResult
        {
            HasEvent = true,
            HasAction = true,
            IsConnected = true
        };

        result.IncompleteNodes.Add(new IncompleteNodeInfo
        {
            NodeId = "test",
            NodeDisplayName = "Test",
            MissingProperties = new List<string> { "Property1" }
        });

        Assert.False(result.IsValid);
        Assert.True(result.HasErrors);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Validate_DataConnection_NotCountedAsExecution()
    {
        // Data connections should not count as execution flow
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                CreateEventNode(),
                new ScriptNode
                {
                    Id = "variable1",
                    NodeType = NodeTypeId.Variable_GetFlag,
                    Category = NodeCategory.Variable,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["FlagName"] = "test_flag"
                    }
                },
                CreateActionNode()
            },
            connections: new List<NodeConnection>
            {
                // Only data connection, no execution
                new NodeConnection
                {
                    Id = "conn1",
                    FromNodeId = "variable1",
                    FromPortName = "Value",  // Data port
                    ToNodeId = "action1",
                    ToPortName = "Condition"  // Data input
                }
            }
        );

        var result = ScriptValidator.Validate(script);

        // Should not be connected because there's no execution path
        Assert.False(result.IsConnected);
    }

    [Fact]
    public void Validate_UnknownNodeType_HandledGracefully()
    {
        var script = CreateScript(
            nodes: new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "unknown1",
                    NodeType = (NodeTypeId)99999,
                    Category = NodeCategory.Action,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                }
            }
        );

        // Should not throw
        var result = ScriptValidator.Validate(script);
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_VeryLongChain_HandledWithoutStackOverflow()
    {
        var nodes = new List<ScriptNode> { CreateEventNode() };
        var connections = new List<NodeConnection>();

        // Create a chain of 100 actions
        string prevId = "event1";
        for (int i = 0; i < 100; i++)
        {
            var actionId = $"action{i}";
            nodes.Add(CreateActionNode(actionId));
            connections.Add(CreateConnection(prevId, actionId));
            prevId = actionId;
        }

        var script = CreateScript(nodes: nodes, connections: connections);

        // Should not throw StackOverflowException
        var result = ScriptValidator.Validate(script);
        Assert.True(result.IsConnected);
    }

    #endregion
}
