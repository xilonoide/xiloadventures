using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Motor de ejecución de conversaciones con NPCs.
/// Maneja el estado de diálogos activos y la interacción con el jugador.
/// </summary>
public class ConversationEngine
{
    private readonly WorldModel _world;
    private readonly GameState _gameState;
    private readonly bool _isDebugMode;

    /// <summary>Evento cuando hay texto de diálogo para mostrar.</summary>
    public event Action<ConversationMessage>? OnDialogue;

    /// <summary>Evento cuando hay opciones para el jugador.</summary>
    public event Action<List<DialogueOption>>? OnPlayerOptions;

    /// <summary>Evento cuando se abre el comercio con un NPC.</summary>
    public event Action<Npc>? OnTradeOpen;

    /// <summary>Evento cuando termina la conversación.</summary>
    public event Action? OnConversationEnded;

    /// <summary>Evento cuando se debe mostrar un mensaje del sistema.</summary>
    public event Action<string>? OnSystemMessage;

    /// <summary>Indica si hay una conversación activa.</summary>
    public bool IsConversationActive => _gameState.ActiveConversation?.IsActive == true;

    public ConversationEngine(WorldModel world, GameState gameState, bool isDebugMode = false)
    {
        _world = world;
        _gameState = gameState;
        _isDebugMode = isDebugMode;
    }

    /// <summary>Emite un mensaje de depuración solo en modo debug.</summary>
    private void DebugMessage(string message)
    {
        if (_isDebugMode)
            OnSystemMessage?.Invoke(message);
    }

    /// <summary>
    /// Inicia una conversación con un NPC.
    /// </summary>
    public async Task StartConversationAsync(string npcId)
    {
        var npc = _gameState.Npcs.FirstOrDefault(n =>
            string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
        if (npc == null)
        {
            DebugMessage($"[Error] NPC '{npcId}' no encontrado.");
            return;
        }

        // Crear conversación desde el script del NPC
        var npcScript = _world.Scripts.FirstOrDefault(s =>
            string.Equals(s.OwnerType, "Npc", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, npcId, StringComparison.OrdinalIgnoreCase));

        var conversation = CreateConversationFromScript(npcScript, npcId);

        if (conversation == null || conversation.Nodes.Count == 0)
        {
            DebugMessage($"[Error] No hay conversación disponible para {npc.Name}.");
            return;
        }

        // Buscar el nodo de inicio
        var startNode = FindStartNode(conversation);
        if (startNode == null)
        {
            DebugMessage($"[Error] No se encontró nodo Conversation_Start en la conversación.");
            return;
        }

        // Inicializar estado
        _gameState.ActiveConversation = new ConversationState
        {
            ConversationId = conversation.Id,
            NpcId = npcId,
            CurrentNodeId = startNode.Id,
            IsActive = true
        };

        // Mensaje visible para confirmar que la conversación se inicia (solo en debug)
        DebugMessage($"[Conversación con {npc.Name}]");

        // Ejecutar desde el nodo de inicio
        await ExecuteNodeAsync(conversation, startNode.Id);
    }

    /// <summary>
    /// Crea una ConversationDefinition a partir del script del NPC.
    /// Extrae solo los nodos de tipo Conversation_*.
    /// </summary>
    private ConversationDefinition? CreateConversationFromScript(ScriptDefinition? script, string npcId)
    {
        if (script == null) return null;

        // Filtrar solo los nodos de conversación
        var conversationNodes = script.Nodes
            .Where(n => n.Category == NodeCategory.Dialogue)
            .ToList();

        if (conversationNodes.Count == 0) return null;

        // Filtrar las conexiones que involucran nodos de conversación
        var nodeIds = new HashSet<string>(conversationNodes.Select(n => n.Id), StringComparer.OrdinalIgnoreCase);
        var conversationConnections = script.Connections
            .Where(c => nodeIds.Contains(c.FromNodeId) || nodeIds.Contains(c.ToNodeId))
            .ToList();

        return new ConversationDefinition
        {
            Id = npcId,
            Name = $"Conversación de {npcId}",
            Nodes = conversationNodes,
            Connections = conversationConnections
        };
    }

    /// <summary>
    /// El jugador selecciona una opción de diálogo.
    /// </summary>
    public async Task SelectOptionAsync(int optionIndex)
    {
        if (_gameState.ActiveConversation == null || !_gameState.ActiveConversation.IsActive) return;

        var conversation = GetCurrentConversation();
        if (conversation == null) return;

        // Verificar que hay opciones disponibles
        if (_gameState.ActiveConversation.CurrentOptions.Count == 0) return;
        if (optionIndex < 0 || optionIndex >= _gameState.ActiveConversation.CurrentOptions.Count) return;

        var selectedOption = _gameState.ActiveConversation.CurrentOptions[optionIndex];

        // Buscar la conexión para esta opción
        var currentNode = GetCurrentNode(conversation);
        if (currentNode == null) return;

        var connection = conversation.Connections.FirstOrDefault(c =>
            string.Equals(c.FromNodeId, currentNode.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.FromPortName, selectedOption.OutputPort, StringComparison.OrdinalIgnoreCase));

        if (connection != null)
        {
            _gameState.ActiveConversation.CurrentNodeId = connection.ToNodeId;
            _gameState.ActiveConversation.CurrentOptions.Clear();
            await ExecuteNodeAsync(conversation, connection.ToNodeId);
        }
    }

    /// <summary>
    /// Continúa la conversación (para nodos sin opciones, como después de NpcSay).
    /// </summary>
    public async Task ContinueAsync()
    {
        if (_gameState.ActiveConversation == null || !_gameState.ActiveConversation.IsActive)
            return;

        var conversation = GetCurrentConversation();
        if (conversation == null)
        {
            DebugMessage("[Error] ContinueAsync: No se pudo obtener la conversación actual.");
            return;
        }

        var currentNode = GetCurrentNode(conversation);
        if (currentNode == null)
        {
            DebugMessage($"[Error] ContinueAsync: Nodo actual no encontrado (ID: {_gameState.ActiveConversation.CurrentNodeId}).");
            return;
        }

        // Buscar siguiente nodo por conexión "Exec"
        var connection = conversation.Connections.FirstOrDefault(c =>
            string.Equals(c.FromNodeId, currentNode.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.FromPortName, "Exec", StringComparison.OrdinalIgnoreCase));

        if (connection != null)
        {
            _gameState.ActiveConversation.CurrentNodeId = connection.ToNodeId;
            await ExecuteNodeAsync(conversation, connection.ToNodeId);
        }
    }

    /// <summary>
    /// Termina la conversación forzadamente.
    /// </summary>
    public void EndConversation()
    {
        if (_gameState.ActiveConversation != null)
        {
            _gameState.ActiveConversation.IsActive = false;
            _gameState.ActiveConversation = null;
        }
        OnConversationEnded?.Invoke();
    }

    /// <summary>
    /// Cierra la tienda y continúa la conversación (por la conexión "OnClose").
    /// </summary>
    public async Task CloseShopAsync()
    {
        if (_gameState.ActiveConversation == null)
            return;

        // Continuar la conversación después de cerrar la tienda
        var conversation = GetCurrentConversation();
        if (conversation != null)
        {
            var currentNode = GetCurrentNode(conversation);
            if (currentNode != null)
            {
                // Seguir por la conexión "OnClose" si existe
                await FollowConnectionAsync(conversation, currentNode, "OnClose");
            }
        }
    }

    private async Task ExecuteNodeAsync(ConversationDefinition conversation, string nodeId)
    {
        var node = conversation.Nodes.FirstOrDefault(n =>
            string.Equals(n.Id, nodeId, StringComparison.OrdinalIgnoreCase));
        if (node == null)
        {
            DebugMessage($"[Error] Nodo '{nodeId}' no encontrado en conversación.");
            return;
        }

        // Marcar como visitado
        _gameState.ActiveConversation?.VisitedNodeIds.Add(nodeId);

        switch (node.NodeType)
        {
            case NodeTypeId.Conversation_Start:
                await ContinueAsync();
                break;

            case NodeTypeId.Conversation_NpcSay:
                HandleNpcSay(node);
                // Continuar automáticamente al siguiente nodo
                await ContinueAsync();
                break;

            case NodeTypeId.Conversation_PlayerChoice:
                HandlePlayerChoice(conversation, node);
                break;

            case NodeTypeId.Conversation_Branch:
                await HandleBranchAsync(conversation, node);
                break;

            case NodeTypeId.Conversation_Shop:
                HandleShop(node);
                break;

            case NodeTypeId.Conversation_BuyItem:
                await HandleBuyItemAsync(conversation, node);
                break;

            case NodeTypeId.Conversation_SellItem:
                await HandleSellItemAsync(conversation, node);
                break;

            case NodeTypeId.Conversation_Action:
                await HandleActionAsync(conversation, node);
                break;

            case NodeTypeId.Conversation_End:
                EndConversation();
                break;
        }
    }

    private void HandleNpcSay(ScriptNode node)
    {
        var text = GetProperty<string>(node, "Text", "");
        var speakerName = GetProperty<string>(node, "SpeakerName", "");
        var emotion = GetProperty<string>(node, "Emotion", "Neutral");

        var message = new ConversationMessage
        {
            Text = text,
            SpeakerName = string.IsNullOrEmpty(speakerName) ? GetCurrentNpcName() : speakerName,
            Emotion = emotion,
            IsNpc = true
        };

        if (OnDialogue == null)
        {
            DebugMessage($"[Error] OnDialogue no está conectado. Mensaje: {text}");
        }
        else
        {
            OnDialogue.Invoke(message);
        }
    }

    private void HandlePlayerChoice(ConversationDefinition conversation, ScriptNode node)
    {
        var options = new List<DialogueOption>();

        for (int i = 1; i <= 4; i++)
        {
            var text = GetProperty<string>(node, $"Text{i}", "");
            if (!string.IsNullOrWhiteSpace(text))
            {
                var outputPort = $"Option{i}";
                // Verificar si hay conexión para esta opción
                var hasConnection = conversation.Connections.Any(c =>
                    string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(c.FromPortName, outputPort, StringComparison.OrdinalIgnoreCase));

                if (hasConnection)
                {
                    options.Add(new DialogueOption
                    {
                        Index = options.Count,
                        Text = text,
                        IsEnabled = true,
                        OutputPort = outputPort
                    });
                }
            }
        }

        if (_gameState.ActiveConversation != null)
        {
            _gameState.ActiveConversation.CurrentOptions = options;
        }

        OnPlayerOptions?.Invoke(options);
    }

    private async Task HandleBranchAsync(ConversationDefinition conversation, ScriptNode node)
    {
        var conditionType = GetProperty<string>(node, "ConditionType", "");
        var result = EvaluateCondition(node, conditionType);

        var outputPort = result ? "True" : "False";
        await FollowConnectionAsync(conversation, node, outputPort);
    }

    private bool EvaluateCondition(ScriptNode node, string conditionType)
    {
        return conditionType switch
        {
            "HasFlag" => EvaluateHasFlag(node),
            "HasItem" => EvaluateHasItem(node),
            "HasMoney" => EvaluateHasMoney(node),
            "QuestStatus" => EvaluateQuestStatus(node),
            "VisitedNode" => _gameState.ActiveConversation?.VisitedNodeIds.Contains(node.Id) ?? false,
            _ => false
        };
    }

    private bool EvaluateHasFlag(ScriptNode node)
    {
        var flagName = GetProperty<string>(node, "FlagName", "");
        return !string.IsNullOrEmpty(flagName) &&
               _gameState.Flags.TryGetValue(flagName, out var value) && value;
    }

    private bool EvaluateHasItem(ScriptNode node)
    {
        var itemId = GetProperty<string>(node, "ItemId", "");
        return !string.IsNullOrEmpty(itemId) &&
               _gameState.InventoryObjectIds.Any(id =>
                   string.Equals(id, itemId, StringComparison.OrdinalIgnoreCase));
    }

    private bool EvaluateHasMoney(ScriptNode node)
    {
        var amount = GetProperty<int>(node, "MoneyAmount", 0);
        return _gameState.Player.Money >= amount;
    }

    private bool EvaluateQuestStatus(ScriptNode node)
    {
        var questId = GetProperty<string>(node, "QuestId", "");
        var expectedStatus = GetProperty<string>(node, "QuestStatus", "NotStarted");

        if (string.IsNullOrEmpty(questId)) return false;

        var currentStatus = _gameState.Quests.TryGetValue(questId, out var state)
            ? state.Status
            : QuestStatus.NotStarted;

        return Enum.TryParse<QuestStatus>(expectedStatus, out var expected) &&
               currentStatus == expected;
    }

    private void HandleShop(ScriptNode node)
    {
        var npc = GetCurrentNpc();
        if (npc == null) return;

        // Abrir ventana de comercio visual
        OnTradeOpen?.Invoke(npc);
    }

    private async Task HandleBuyItemAsync(ConversationDefinition conversation, ScriptNode node)
    {
        var objectId = GetProperty<string>(node, "ObjectId", "");
        var price = GetProperty<int>(node, "Price", 0);

        if (_gameState.Player.Money >= price)
        {
            _gameState.Player.Money -= price;
            if (!_gameState.InventoryObjectIds.Contains(objectId))
                _gameState.InventoryObjectIds.Add(objectId);

            var obj = _gameState.Objects.FirstOrDefault(o =>
                string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
            OnSystemMessage?.Invoke($"Has comprado {obj?.Name ?? objectId} por {price}.");

            await FollowConnectionAsync(conversation, node, "Success");
        }
        else
        {
            OnSystemMessage?.Invoke("No tienes suficiente dinero.");
            await FollowConnectionAsync(conversation, node, "NotEnoughMoney");
        }
    }

    private async Task HandleSellItemAsync(ConversationDefinition conversation, ScriptNode node)
    {
        var objectId = GetProperty<string>(node, "ObjectId", "");
        var price = GetProperty<int>(node, "Price", 0);

        if (_gameState.InventoryObjectIds.Any(id =>
            string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase)))
        {
            _gameState.InventoryObjectIds.RemoveAll(id =>
                string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
            _gameState.Player.Money += price;

            var obj = _gameState.Objects.FirstOrDefault(o =>
                string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
            OnSystemMessage?.Invoke($"Has vendido {obj?.Name ?? objectId} por {price}.");

            await FollowConnectionAsync(conversation, node, "Success");
        }
        else
        {
            OnSystemMessage?.Invoke("No tienes ese objeto.");
            await FollowConnectionAsync(conversation, node, "NoItem");
        }
    }

    private async Task HandleActionAsync(ConversationDefinition conversation, ScriptNode node)
    {
        var actionType = GetProperty<string>(node, "ActionType", "");

        switch (actionType)
        {
            case "GiveItem":
                var giveObjectId = GetProperty<string>(node, "ObjectId", "");
                if (!string.IsNullOrEmpty(giveObjectId) &&
                    !_gameState.InventoryObjectIds.Contains(giveObjectId))
                {
                    _gameState.InventoryObjectIds.Add(giveObjectId);
                    var obj = _gameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, giveObjectId, StringComparison.OrdinalIgnoreCase));
                    OnSystemMessage?.Invoke($"Has recibido: {obj?.Name ?? giveObjectId}");
                }
                break;

            case "RemoveItem":
                var removeId = GetProperty<string>(node, "ObjectId", "");
                _gameState.InventoryObjectIds.RemoveAll(id =>
                    string.Equals(id, removeId, StringComparison.OrdinalIgnoreCase));
                break;

            case "AddMoney":
                var addAmount = GetProperty<int>(node, "Amount", 0);
                _gameState.Player.Money += addAmount;
                if (addAmount > 0)
                    OnSystemMessage?.Invoke($"Has recibido {addAmount} de dinero.");
                break;

            case "RemoveMoney":
                var removeAmount = GetProperty<int>(node, "Amount", 0);
                _gameState.Player.Money = Math.Max(0, _gameState.Player.Money - removeAmount);
                break;

            case "SetFlag":
                var flagName = GetProperty<string>(node, "FlagName", "");
                if (!string.IsNullOrEmpty(flagName))
                    _gameState.Flags[flagName] = true;
                break;

            case "StartQuest":
                var startQuestId = GetProperty<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(startQuestId))
                {
                    _gameState.Quests[startQuestId] = new QuestState
                    {
                        QuestId = startQuestId,
                        Status = QuestStatus.InProgress
                    };
                    var quest = _world.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, startQuestId, StringComparison.OrdinalIgnoreCase));
                    OnSystemMessage?.Invoke($"Nueva misión: {quest?.Name ?? startQuestId}");
                }
                break;

            case "CompleteQuest":
                var completeQuestId = GetProperty<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(completeQuestId) &&
                    _gameState.Quests.TryGetValue(completeQuestId, out var qState))
                {
                    qState.Status = QuestStatus.Completed;
                    var quest = _world.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, completeQuestId, StringComparison.OrdinalIgnoreCase));
                    OnSystemMessage?.Invoke($"Misión completada: {quest?.Name ?? completeQuestId}");
                }
                break;

            case "ShowMessage":
                var message = GetProperty<string>(node, "Message", "");
                if (!string.IsNullOrEmpty(message))
                    OnSystemMessage?.Invoke(message);
                break;
        }

        await ContinueAsync();
    }

    private async Task FollowConnectionAsync(ConversationDefinition conversation, ScriptNode node, string outputPort)
    {
        var connection = conversation.Connections.FirstOrDefault(c =>
            string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.FromPortName, outputPort, StringComparison.OrdinalIgnoreCase));

        if (connection != null && _gameState.ActiveConversation != null)
        {
            _gameState.ActiveConversation.CurrentNodeId = connection.ToNodeId;
            await ExecuteNodeAsync(conversation, connection.ToNodeId);
        }
    }

    private ConversationDefinition? GetCurrentConversation()
    {
        if (_gameState.ActiveConversation == null) return null;

        // Primero buscar en las conversaciones del mundo por ConversationId
        var conversationId = _gameState.ActiveConversation.ConversationId;
        var conversation = _world.Conversations.FirstOrDefault(c =>
            string.Equals(c.Id, conversationId, StringComparison.OrdinalIgnoreCase));

        if (conversation != null) return conversation;

        // Si no se encuentra, intentar crear desde el script del NPC
        var npcId = _gameState.ActiveConversation.NpcId;
        var npcScript = _world.Scripts.FirstOrDefault(s =>
            string.Equals(s.OwnerType, "Npc", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, npcId, StringComparison.OrdinalIgnoreCase));

        return CreateConversationFromScript(npcScript, npcId);
    }

    private ScriptNode? GetCurrentNode(ConversationDefinition conversation)
    {
        if (_gameState.ActiveConversation == null) return null;
        return conversation.Nodes.FirstOrDefault(n =>
            string.Equals(n.Id, _gameState.ActiveConversation.CurrentNodeId, StringComparison.OrdinalIgnoreCase));
    }

    private ScriptNode? FindStartNode(ConversationDefinition conversation)
    {
        return conversation.Nodes.FirstOrDefault(n =>
            n.NodeType == NodeTypeId.Conversation_Start);
    }

    private Npc? GetCurrentNpc()
    {
        if (_gameState.ActiveConversation == null) return null;
        return _gameState.Npcs.FirstOrDefault(n =>
            string.Equals(n.Id, _gameState.ActiveConversation.NpcId, StringComparison.OrdinalIgnoreCase));
    }

    private string GetCurrentNpcName()
    {
        return GetCurrentNpc()?.Name ?? "???";
    }

    private T GetProperty<T>(ScriptNode node, string name, T defaultValue)
    {
        if (node.Properties.TryGetValue(name, out var value) && value != null)
        {
            // Si es JsonElement (deserializado de JSON)
            if (value is System.Text.Json.JsonElement jsonElement)
            {
                try
                {
                    if (typeof(T) == typeof(string))
                        return (T)(object)(jsonElement.GetString() ?? defaultValue?.ToString() ?? "");
                    if (typeof(T) == typeof(int))
                        return (T)(object)jsonElement.GetInt32();
                    if (typeof(T) == typeof(double))
                        return (T)(object)jsonElement.GetDouble();
                    if (typeof(T) == typeof(bool))
                        return (T)(object)jsonElement.GetBoolean();
                }
                catch
                {
                    return defaultValue;
                }
            }

            if (value is T typedValue)
                return typedValue;

            // Intentar conversión para tipos numéricos
            if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value.ToString(), out var intVal))
                    return (T)(object)intVal;
            }
            if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value.ToString(), out var doubleVal))
                    return (T)(object)doubleVal;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Normaliza un string para comparación: elimina acentos y convierte a minúsculas.
    /// </summary>
    private static string NormalizeForComparison(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalizar a FormD separa las letras base de sus diacríticos
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            // Mantener solo caracteres que no sean diacríticos (NonSpacingMark)
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        // Volver a FormC y a minúsculas
        return sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
    }
}
