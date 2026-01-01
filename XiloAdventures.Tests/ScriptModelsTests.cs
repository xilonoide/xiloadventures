using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Tests for script model classes including serialization,
/// default values, and computed properties.
/// </summary>
public class ScriptModelsTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    #region ScriptDefinition Tests

    [Fact]
    public void ScriptDefinition_DefaultValues_AreCorrect()
    {
        var script = new ScriptDefinition();

        Assert.NotNull(script.Id);
        Assert.NotEmpty(script.Id);
        Assert.Equal("Nuevo Script", script.Name);
        Assert.Equal(string.Empty, script.OwnerType);
        Assert.Equal(string.Empty, script.OwnerId);
        Assert.NotNull(script.Nodes);
        Assert.Empty(script.Nodes);
        Assert.NotNull(script.Connections);
        Assert.Empty(script.Connections);
    }

    [Fact]
    public void ScriptDefinition_GeneratesUniqueIds()
    {
        var script1 = new ScriptDefinition();
        var script2 = new ScriptDefinition();

        Assert.NotEqual(script1.Id, script2.Id);
    }

    [Fact]
    public void ScriptDefinition_CanBeConfigured()
    {
        var script = new ScriptDefinition
        {
            Id = "custom_id",
            Name = "Mi Script",
            OwnerType = "Room",
            OwnerId = "room_entrada"
        };

        Assert.Equal("custom_id", script.Id);
        Assert.Equal("Mi Script", script.Name);
        Assert.Equal("Room", script.OwnerType);
        Assert.Equal("room_entrada", script.OwnerId);
    }

    [Fact]
    public void ScriptDefinition_NodesAndConnections_CanBeModified()
    {
        var script = new ScriptDefinition();
        var node = new ScriptNode { Id = "node1" };
        var connection = new NodeConnection { FromNodeId = "node1", ToNodeId = "node2" };

        script.Nodes.Add(node);
        script.Connections.Add(connection);

        Assert.Single(script.Nodes);
        Assert.Single(script.Connections);
        Assert.Equal("node1", script.Nodes[0].Id);
    }

    [Fact]
    public void ScriptDefinition_SerializesAndDeserializesCorrectly()
    {
        var script = new ScriptDefinition
        {
            Id = "test_script",
            Name = "Test Script",
            OwnerType = "Game",
            OwnerId = "game_id",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event1",
                    NodeType = NodeTypeId.Event_OnGameStart,
                    Category = NodeCategory.Event,
                    X = 100,
                    Y = 200
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    Id = "conn1",
                    FromNodeId = "event1",
                    FromPortName = "Exec",
                    ToNodeId = "action1",
                    ToPortName = "Exec"
                }
            }
        };

        var json = JsonSerializer.Serialize(script, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptDefinition>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(script.Id, deserialized.Id);
        Assert.Equal(script.Name, deserialized.Name);
        Assert.Equal(script.OwnerType, deserialized.OwnerType);
        Assert.Equal(script.OwnerId, deserialized.OwnerId);
        Assert.Single(deserialized.Nodes);
        Assert.Single(deserialized.Connections);
    }

    #endregion

    #region ScriptNode Tests

    [Fact]
    public void ScriptNode_DefaultValues_AreCorrect()
    {
        var node = new ScriptNode();

        Assert.NotNull(node.Id);
        Assert.NotEmpty(node.Id);
        Assert.Equal(NodeTypeId.Event_OnGameStart, node.NodeType);  // First enum value is default
        Assert.Equal(NodeCategory.Event, node.Category);
        Assert.Equal(0, node.X);
        Assert.Equal(0, node.Y);
        Assert.NotNull(node.Properties);
        Assert.Empty(node.Properties);
        Assert.Null(node.Comment);
    }

    [Fact]
    public void ScriptNode_GeneratesUniqueIds()
    {
        var node1 = new ScriptNode();
        var node2 = new ScriptNode();

        Assert.NotEqual(node1.Id, node2.Id);
    }

    [Fact]
    public void ScriptNode_Properties_AreCaseInsensitive()
    {
        var node = new ScriptNode();
        node.Properties["Message"] = "Hello";

        Assert.True(node.Properties.ContainsKey("message"));
        Assert.True(node.Properties.ContainsKey("MESSAGE"));
        Assert.True(node.Properties.ContainsKey("Message"));
        Assert.Equal("Hello", node.Properties["message"]);
        Assert.Equal("Hello", node.Properties["MESSAGE"]);
    }

    [Fact]
    public void ScriptNode_Properties_CanStoreDifferentTypes()
    {
        var node = new ScriptNode();
        node.Properties["StringValue"] = "test";
        node.Properties["IntValue"] = 42;
        node.Properties["BoolValue"] = true;
        node.Properties["NullValue"] = null;
        node.Properties["DoubleValue"] = 3.14;

        Assert.Equal("test", node.Properties["stringvalue"]);
        Assert.Equal(42, node.Properties["intvalue"]);
        Assert.Equal(true, node.Properties["boolvalue"]);
        Assert.Null(node.Properties["nullvalue"]);
        Assert.Equal(3.14, node.Properties["doublevalue"]);
    }

    [Fact]
    public void ScriptNode_CanSetPosition()
    {
        var node = new ScriptNode
        {
            X = 150.5,
            Y = 200.75
        };

        Assert.Equal(150.5, node.X);
        Assert.Equal(200.75, node.Y);
    }

    [Fact]
    public void ScriptNode_CanSetComment()
    {
        var node = new ScriptNode
        {
            Comment = "Este nodo muestra un mensaje al jugador"
        };

        Assert.Equal("Este nodo muestra un mensaje al jugador", node.Comment);
    }

    [Fact]
    public void ScriptNode_AllCategories_CanBeAssigned()
    {
        foreach (NodeCategory category in Enum.GetValues<NodeCategory>())
        {
            var node = new ScriptNode { Category = category };
            Assert.Equal(category, node.Category);
        }
    }

    [Fact]
    public void ScriptNode_SerializesAndDeserializesCorrectly()
    {
        var node = new ScriptNode
        {
            Id = "node_test",
            NodeType = NodeTypeId.Action_ShowMessage,
            Category = NodeCategory.Action,
            X = 100,
            Y = 200,
            Comment = "Test comment",
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Message"] = "Hello World",
                ["Duration"] = 5
            }
        };

        var json = JsonSerializer.Serialize(node, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptNode>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(node.Id, deserialized.Id);
        Assert.Equal(node.NodeType, deserialized.NodeType);
        Assert.Equal(node.Category, deserialized.Category);
        Assert.Equal(node.X, deserialized.X);
        Assert.Equal(node.Y, deserialized.Y);
        Assert.Equal(node.Comment, deserialized.Comment);
    }

    #endregion

    #region NodePort Tests

    [Fact]
    public void NodePort_DefaultValues_AreCorrect()
    {
        var port = new NodePort();

        Assert.Equal(string.Empty, port.Name);
        Assert.Equal(PortType.Execution, port.PortType);
        Assert.Null(port.DataType);
        Assert.Null(port.DefaultValue);
        Assert.Null(port.Label);
    }

    [Fact]
    public void NodePort_ExecutionPort_CanBeCreated()
    {
        var port = new NodePort
        {
            Name = "Exec",
            PortType = PortType.Execution,
            Label = "Entrada"
        };

        Assert.Equal("Exec", port.Name);
        Assert.Equal(PortType.Execution, port.PortType);
        Assert.Equal("Entrada", port.Label);
    }

    [Fact]
    public void NodePort_DataPort_CanBeCreated()
    {
        var port = new NodePort
        {
            Name = "Value",
            PortType = PortType.Data,
            DataType = "string",
            DefaultValue = "default text",
            Label = "Valor"
        };

        Assert.Equal("Value", port.Name);
        Assert.Equal(PortType.Data, port.PortType);
        Assert.Equal("string", port.DataType);
        Assert.Equal("default text", port.DefaultValue);
        Assert.Equal("Valor", port.Label);
    }

    [Fact]
    public void NodePort_SerializesAndDeserializesCorrectly()
    {
        var port = new NodePort
        {
            Name = "TestPort",
            PortType = PortType.Data,
            DataType = "int",
            DefaultValue = 10,
            Label = "Test"
        };

        var json = JsonSerializer.Serialize(port, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NodePort>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(port.Name, deserialized.Name);
        Assert.Equal(port.PortType, deserialized.PortType);
        Assert.Equal(port.DataType, deserialized.DataType);
        Assert.Equal(port.Label, deserialized.Label);
    }

    #endregion

    #region NodeConnection Tests

    [Fact]
    public void NodeConnection_DefaultValues_AreCorrect()
    {
        var connection = new NodeConnection();

        Assert.NotNull(connection.Id);
        Assert.NotEmpty(connection.Id);
        Assert.Equal(string.Empty, connection.FromNodeId);
        Assert.Equal(string.Empty, connection.FromPortName);
        Assert.Equal(string.Empty, connection.ToNodeId);
        Assert.Equal(string.Empty, connection.ToPortName);
    }

    [Fact]
    public void NodeConnection_GeneratesUniqueIds()
    {
        var conn1 = new NodeConnection();
        var conn2 = new NodeConnection();

        Assert.NotEqual(conn1.Id, conn2.Id);
    }

    [Fact]
    public void NodeConnection_CanBeConfigured()
    {
        var connection = new NodeConnection
        {
            Id = "conn_custom",
            FromNodeId = "event1",
            FromPortName = "Exec",
            ToNodeId = "action1",
            ToPortName = "Exec"
        };

        Assert.Equal("conn_custom", connection.Id);
        Assert.Equal("event1", connection.FromNodeId);
        Assert.Equal("Exec", connection.FromPortName);
        Assert.Equal("action1", connection.ToNodeId);
        Assert.Equal("Exec", connection.ToPortName);
    }

    [Fact]
    public void NodeConnection_DataConnection_CanBeCreated()
    {
        var connection = new NodeConnection
        {
            FromNodeId = "variable1",
            FromPortName = "Value",
            ToNodeId = "action1",
            ToPortName = "Message"
        };

        Assert.Equal("variable1", connection.FromNodeId);
        Assert.Equal("Value", connection.FromPortName);
        Assert.Equal("action1", connection.ToNodeId);
        Assert.Equal("Message", connection.ToPortName);
    }

    [Fact]
    public void NodeConnection_SerializesAndDeserializesCorrectly()
    {
        var connection = new NodeConnection
        {
            Id = "test_conn",
            FromNodeId = "from",
            FromPortName = "Out",
            ToNodeId = "to",
            ToPortName = "In"
        };

        var json = JsonSerializer.Serialize(connection, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NodeConnection>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(connection.Id, deserialized.Id);
        Assert.Equal(connection.FromNodeId, deserialized.FromNodeId);
        Assert.Equal(connection.FromPortName, deserialized.FromPortName);
        Assert.Equal(connection.ToNodeId, deserialized.ToNodeId);
        Assert.Equal(connection.ToPortName, deserialized.ToPortName);
    }

    #endregion

    #region NodeCategory Enum Tests

    [Fact]
    public void NodeCategory_HasAllExpectedValues()
    {
        var values = Enum.GetValues<NodeCategory>();

        Assert.Contains(NodeCategory.Event, values);
        Assert.Contains(NodeCategory.Condition, values);
        Assert.Contains(NodeCategory.Action, values);
        Assert.Contains(NodeCategory.Flow, values);
        Assert.Contains(NodeCategory.Variable, values);
        Assert.Contains(NodeCategory.Dialogue, values);
        Assert.Equal(6, values.Length);
    }

    [Fact]
    public void NodeCategory_SerializesAsCamelCase()
    {
        var node = new ScriptNode { Category = NodeCategory.Event };
        var json = JsonSerializer.Serialize(node, JsonOptions);

        Assert.Contains("\"event\"", json.ToLower());
    }

    [Fact]
    public void NodeCategory_DeserializesFromCamelCase()
    {
        var json = "{\"Id\":\"test\",\"NodeType\":\"Action_ShowMessage\",\"Category\":\"action\",\"X\":0,\"Y\":0}";
        var node = JsonSerializer.Deserialize<ScriptNode>(json, JsonOptions);

        Assert.NotNull(node);
        Assert.Equal(NodeCategory.Action, node.Category);
        Assert.Equal(NodeTypeId.Action_ShowMessage, node.NodeType);
    }

    #endregion

    #region PortType Enum Tests

    [Fact]
    public void PortType_HasAllExpectedValues()
    {
        var values = Enum.GetValues<PortType>();

        Assert.Contains(PortType.Execution, values);
        Assert.Contains(PortType.Data, values);
        Assert.Equal(2, values.Length);
    }

    [Fact]
    public void PortType_SerializesAsCamelCase()
    {
        var port = new NodePort { PortType = PortType.Execution };
        var json = JsonSerializer.Serialize(port, JsonOptions);

        Assert.Contains("\"execution\"", json.ToLower());
    }

    #endregion

    #region NodeTypeDefinition Tests

    [Fact]
    public void NodeTypeDefinition_DefaultValues_AreCorrect()
    {
        var typeDef = new NodeTypeDefinition();

        Assert.Equal(NodeTypeId.Event_OnGameStart, typeDef.TypeId);  // First enum value is default
        Assert.Equal(string.Empty, typeDef.DisplayName);
        Assert.Null(typeDef.Description);
        Assert.Equal(NodeCategory.Event, typeDef.Category);
        Assert.Equal(NodeOwnerType.None, typeDef.OwnerTypes);
        Assert.Equal(RequiredFeature.None, typeDef.RequiredFeature);
        Assert.NotNull(typeDef.InputPorts);
        Assert.Empty(typeDef.InputPorts);
        Assert.NotNull(typeDef.OutputPorts);
        Assert.Empty(typeDef.OutputPorts);
        Assert.NotNull(typeDef.Properties);
        Assert.Empty(typeDef.Properties);
    }

    [Fact]
    public void NodeTypeDefinition_CanBeFullyConfigured()
    {
        var typeDef = new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_ShowMessage,
            DisplayName = "Mostrar Mensaje",
            Description = "Muestra un mensaje al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.Room | NodeOwnerType.Npc,
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution }
            },
            Properties = new[]
            {
                new NodePropertyDefinition
                {
                    Name = "Message",
                    DisplayName = "Mensaje",
                    DataType = "string",
                    IsRequired = true
                }
            }
        };

        Assert.Equal(NodeTypeId.Action_ShowMessage, typeDef.TypeId);
        Assert.Equal("Mostrar Mensaje", typeDef.DisplayName);
        Assert.Equal(NodeCategory.Action, typeDef.Category);
        Assert.True(typeDef.OwnerTypes.HasFlag(NodeOwnerType.Game));
        Assert.True(typeDef.OwnerTypes.HasFlag(NodeOwnerType.Room));
        Assert.True(typeDef.OwnerTypes.HasFlag(NodeOwnerType.Npc));
        Assert.Single(typeDef.InputPorts);
        Assert.Single(typeDef.OutputPorts);
        Assert.Single(typeDef.Properties);
    }

    [Fact]
    public void NodeTypeDefinition_AllOwnerType_IndicatesAllOwners()
    {
        var typeDef = new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_SetFlag,
            OwnerTypes = NodeOwnerType.All
        };

        Assert.True(typeDef.OwnerTypes.HasFlag(NodeOwnerType.All));
        Assert.True(typeDef.OwnerTypes.Matches("Game"));
        Assert.True(typeDef.OwnerTypes.Matches("Room"));
        Assert.True(typeDef.OwnerTypes.Matches("Npc"));
    }

    [Fact]
    public void NodeTypeDefinition_SerializesAndDeserializesCorrectly()
    {
        var typeDef = new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnEnter,
            DisplayName = "Al Entrar",
            Category = NodeCategory.Event,
            OwnerTypes = NodeOwnerType.Room,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution }
            }
        };

        var json = JsonSerializer.Serialize(typeDef, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NodeTypeDefinition>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(typeDef.TypeId, deserialized.TypeId);
        Assert.Equal(typeDef.DisplayName, deserialized.DisplayName);
        Assert.Equal(typeDef.Category, deserialized.Category);
    }

    #endregion

    #region NodePropertyDefinition Tests

    [Fact]
    public void NodePropertyDefinition_DefaultValues_AreCorrect()
    {
        var prop = new NodePropertyDefinition();

        Assert.Equal(string.Empty, prop.Name);
        Assert.Equal(string.Empty, prop.DisplayName);
        Assert.Equal("string", prop.DataType);
        Assert.Null(prop.DefaultValue);
        Assert.Null(prop.Options);
        Assert.Null(prop.EntityType);
        Assert.False(prop.IsRequired);
        Assert.False(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_RequiresValue_TrueWhenIsRequired()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "Message",
            IsRequired = true
        };

        Assert.True(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_RequiresValue_TrueWhenHasEntityType()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "RoomId",
            EntityType = "Room",
            IsRequired = false
        };

        Assert.True(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_RequiresValue_TrueWhenBothSet()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "ObjectId",
            EntityType = "GameObject",
            IsRequired = true
        };

        Assert.True(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_RequiresValue_FalseWhenNeitherSet()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "OptionalComment",
            IsRequired = false,
            EntityType = null
        };

        Assert.False(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_RequiresValue_FalseWhenEmptyEntityType()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "Description",
            EntityType = "",
            IsRequired = false
        };

        Assert.False(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_WithSelectOptions()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "Direction",
            DisplayName = "Dirección",
            DataType = "select",
            Options = new[] { "Norte", "Sur", "Este", "Oeste" }
        };

        Assert.Equal("select", prop.DataType);
        Assert.NotNull(prop.Options);
        Assert.Equal(4, prop.Options.Length);
        Assert.Contains("Norte", prop.Options);
    }

    [Fact]
    public void NodePropertyDefinition_WithEntityReference()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "TargetNpcId",
            DisplayName = "NPC Objetivo",
            DataType = "string",
            EntityType = "Npc"
        };

        Assert.Equal("Npc", prop.EntityType);
        Assert.True(prop.RequiresValue);
    }

    [Fact]
    public void NodePropertyDefinition_WithDefaultValue()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "Duration",
            DataType = "int",
            DefaultValue = 5
        };

        Assert.Equal(5, prop.DefaultValue);
    }

    [Fact]
    public void NodePropertyDefinition_SerializesAndDeserializesCorrectly()
    {
        var prop = new NodePropertyDefinition
        {
            Name = "Amount",
            DisplayName = "Cantidad",
            DataType = "int",
            DefaultValue = 10,
            IsRequired = true
        };

        var json = JsonSerializer.Serialize(prop, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<NodePropertyDefinition>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(prop.Name, deserialized.Name);
        Assert.Equal(prop.DisplayName, deserialized.DisplayName);
        Assert.Equal(prop.DataType, deserialized.DataType);
        Assert.Equal(prop.IsRequired, deserialized.IsRequired);
    }

    #endregion

    #region ScriptExecutionResult Tests

    [Fact]
    public void ScriptExecutionResult_DefaultValues_AreCorrect()
    {
        var result = new ScriptExecutionResult();

        Assert.False(result.Success);
        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ScriptExecutionResult_Ok_ReturnsSuccessfulResult()
    {
        var result = ScriptExecutionResult.Ok();

        Assert.True(result.Success);
        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ScriptExecutionResult_Ok_WithMessages_IncludesMessages()
    {
        var messages = new List<string>
        {
            "Mensaje 1",
            "Mensaje 2",
            "Mensaje 3"
        };

        var result = ScriptExecutionResult.Ok(messages);

        Assert.True(result.Success);
        Assert.Equal(3, result.Messages.Count);
        Assert.Contains("Mensaje 1", result.Messages);
        Assert.Contains("Mensaje 2", result.Messages);
        Assert.Contains("Mensaje 3", result.Messages);
    }

    [Fact]
    public void ScriptExecutionResult_Ok_WithNullMessages_CreatesEmptyList()
    {
        var result = ScriptExecutionResult.Ok(null);

        Assert.True(result.Success);
        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void ScriptExecutionResult_Error_ReturnsFailedResult()
    {
        var result = ScriptExecutionResult.Error("Error de prueba");

        Assert.False(result.Success);
        Assert.Equal("Error de prueba", result.ErrorMessage);
    }

    [Fact]
    public void ScriptExecutionResult_Error_HasEmptyMessagesList()
    {
        var result = ScriptExecutionResult.Error("Algo falló");

        Assert.NotNull(result.Messages);
        Assert.Empty(result.Messages);
    }

    [Fact]
    public void ScriptExecutionResult_Error_WithEmptyMessage()
    {
        var result = ScriptExecutionResult.Error("");

        Assert.False(result.Success);
        Assert.Equal("", result.ErrorMessage);
    }

    [Fact]
    public void ScriptExecutionResult_CanBeModifiedAfterCreation()
    {
        var result = ScriptExecutionResult.Ok();
        result.Messages.Add("Nuevo mensaje");
        result.Success = false;
        result.ErrorMessage = "Error añadido";

        Assert.False(result.Success);
        Assert.Single(result.Messages);
        Assert.Equal("Error añadido", result.ErrorMessage);
    }

    [Fact]
    public void ScriptExecutionResult_SerializesAndDeserializesCorrectly()
    {
        var result = ScriptExecutionResult.Ok(new List<string> { "Test message" });

        var json = JsonSerializer.Serialize(result, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptExecutionResult>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.Success);
        Assert.Single(deserialized.Messages);
        Assert.Equal("Test message", deserialized.Messages[0]);
    }

    [Fact]
    public void ScriptExecutionResult_ErrorResult_SerializesCorrectly()
    {
        var result = ScriptExecutionResult.Error("Test error");

        var json = JsonSerializer.Serialize(result, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptExecutionResult>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.False(deserialized.Success);
        Assert.Equal("Test error", deserialized.ErrorMessage);
    }

    #endregion

    #region Complex Script Serialization Tests

    [Fact]
    public void CompleteScript_SerializesAndDeserializesCorrectly()
    {
        var script = new ScriptDefinition
        {
            Id = "complete_script",
            Name = "Script Completo",
            OwnerType = "Room",
            OwnerId = "sala_principal",
            Nodes = new List<ScriptNode>
            {
                new ScriptNode
                {
                    Id = "event_enter",
                    NodeType = NodeTypeId.Event_OnEnter,
                    Category = NodeCategory.Event,
                    X = 100,
                    Y = 100,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                },
                new ScriptNode
                {
                    Id = "condition_flag",
                    NodeType = NodeTypeId.Condition_HasFlag,
                    Category = NodeCategory.Condition,
                    X = 300,
                    Y = 100,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["FlagName"] = "puerta_abierta"
                    }
                },
                new ScriptNode
                {
                    Id = "action_message",
                    NodeType = NodeTypeId.Action_ShowMessage,
                    Category = NodeCategory.Action,
                    X = 500,
                    Y = 50,
                    Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["Message"] = "¡Bienvenido de nuevo!"
                    },
                    Comment = "Mensaje para visitantes recurrentes"
                }
            },
            Connections = new List<NodeConnection>
            {
                new NodeConnection
                {
                    Id = "conn1",
                    FromNodeId = "event_enter",
                    FromPortName = "Exec",
                    ToNodeId = "condition_flag",
                    ToPortName = "Exec"
                },
                new NodeConnection
                {
                    Id = "conn2",
                    FromNodeId = "condition_flag",
                    FromPortName = "True",
                    ToNodeId = "action_message",
                    ToPortName = "Exec"
                }
            }
        };

        var json = JsonSerializer.Serialize(script, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptDefinition>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(3, deserialized.Nodes.Count);
        Assert.Equal(2, deserialized.Connections.Count);
        Assert.Equal("sala_principal", deserialized.OwnerId);

        var conditionNode = deserialized.Nodes.Find(n => n.Id == "condition_flag");
        Assert.NotNull(conditionNode);
        Assert.Equal(NodeCategory.Condition, conditionNode.Category);

        var actionNode = deserialized.Nodes.Find(n => n.Id == "action_message");
        Assert.NotNull(actionNode);
        Assert.Equal("Mensaje para visitantes recurrentes", actionNode.Comment);
    }

    [Fact]
    public void Script_WithNegativeCoordinates_SerializesCorrectly()
    {
        var node = new ScriptNode
        {
            X = -100.5,
            Y = -200.75
        };

        var json = JsonSerializer.Serialize(node, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptNode>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(-100.5, deserialized.X);
        Assert.Equal(-200.75, deserialized.Y);
    }

    [Fact]
    public void Script_WithLargeNumberOfNodes_SerializesCorrectly()
    {
        var script = new ScriptDefinition();

        for (int i = 0; i < 100; i++)
        {
            script.Nodes.Add(new ScriptNode
            {
                Id = $"node_{i}",
                NodeType = NodeTypeId.Action_ShowMessage,
                Category = NodeCategory.Action,
                X = i * 100,
                Y = (i % 10) * 50
            });
        }

        var json = JsonSerializer.Serialize(script, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptDefinition>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(100, deserialized.Nodes.Count);
    }

    [Fact]
    public void Script_WithSpecialCharactersInStrings_SerializesCorrectly()
    {
        var node = new ScriptNode
        {
            Comment = "Mensaje con \"comillas\" y caracteres especiales: áéíóú ñ ¿? ¡!",
            Properties = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Message"] = "Línea 1\nLínea 2\tTabulado"
            }
        };

        var json = JsonSerializer.Serialize(node, JsonOptions);
        var deserialized = JsonSerializer.Deserialize<ScriptNode>(json, JsonOptions);

        Assert.NotNull(deserialized);
        Assert.Contains("comillas", deserialized.Comment);
        Assert.Contains("áéíóú", deserialized.Comment);
    }

    #endregion
}
