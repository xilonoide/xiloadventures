using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Motor de ejecución de scripts visuales.
/// </summary>
public class ScriptEngine
{
    private readonly GameState _gameState;
    private readonly WorldModel _world;
    private readonly Dictionary<NodeTypeId, Func<ScriptNode, ScriptContext, Task>> _nodeHandlers;
    private readonly Random _random = new();
    private readonly bool _isDebugMode;

    /// <summary>
    /// Evento disparado cuando se debe mostrar un mensaje al jugador.
    /// </summary>
    public event Action<string>? OnMessage;

    /// <summary>
    /// Evento disparado cuando se debe reproducir un sonido.
    /// </summary>
    public event Action<string>? OnPlaySound;

    /// <summary>
    /// Evento disparado cuando cambia la música de una sala.
    /// Parámetros: roomId, musicId (null para quitar música).
    /// </summary>
    public event Action<string, string?>? OnRoomMusicChanged;

    /// <summary>
    /// Evento disparado cuando el jugador es teletransportado.
    /// </summary>
    public event Action<string>? OnPlayerTeleported;

    /// <summary>
    /// Evento disparado cuando un script quiere iniciar una conversación con un NPC.
    /// </summary>
    public event Action<string>? OnStartConversation;

    /// <summary>
    /// Evento disparado cuando un script quiere iniciar combate con un NPC.
    /// </summary>
    public event Action<string>? OnStartCombat;

    /// <summary>
    /// Evento disparado cuando un script quiere iniciar comercio con un NPC.
    /// </summary>
    public event Action<string>? OnStartTrade;

    /// <summary>
    /// Evento disparado cuando se completan todas las misiones principales.
    /// </summary>
    public event Action? OnAdventureCompleted;

    public ScriptEngine(WorldModel world, GameState gameState, bool isDebugMode = false)
    {
        _world = world;
        _gameState = gameState;
        _isDebugMode = isDebugMode;
        _nodeHandlers = RegisterNodeHandlers();
    }

    private void DebugMessage(string message)
    {
        if (_isDebugMode)
            OnMessage?.Invoke(message);
    }

    /// <summary>
    /// Ejecuta todos los scripts asociados a un evento específico.
    /// </summary>
    /// <param name="ownerType">Tipo de entidad (Room, Npc, GameObject, Door, Quest, Game)</param>
    /// <param name="ownerId">ID de la entidad</param>
    /// <param name="eventType">Tipo de evento (Event_OnEnter, Event_OnTake, etc.)</param>
    public async Task TriggerEventAsync(string ownerType, string ownerId, string eventType)
    {
        DebugMessage($"[Debug] Buscando scripts: {ownerType}/{ownerId}/{eventType}");

        var scripts = _world.Scripts.Where(s =>
            string.Equals(s.OwnerType, ownerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase)).ToList();

        DebugMessage($"[Debug] Encontrados {scripts.Count} scripts para {ownerType}/{ownerId}");

        foreach (var script in scripts)
        {
            DebugMessage($"[Debug] Ejecutando script: {script.Name} ({script.Id})");
            await ExecuteEventAsync(script, eventType);
        }
    }

    /// <summary>
    /// Ejecuta todos los scripts asociados a un evento específico (usando NodeTypeId).
    /// </summary>
    public Task TriggerEventAsync(string ownerType, string ownerId, NodeTypeId eventType)
        => TriggerEventAsync(ownerType, ownerId, eventType.ToString());

    /// <summary>
    /// Ejecuta un nodo de acción individual (para pruebas desde el editor).
    /// </summary>
    public async Task ExecuteSingleNodeAsync(ScriptNode node)
    {
        if (!_nodeHandlers.TryGetValue(node.NodeType, out var handler))
            return;

        // Crear un script temporal para el contexto
        var tempScript = new ScriptDefinition { Nodes = { node } };
        var context = new ScriptContext(tempScript, _gameState, _world);

        // Ejecutar el nodo
        await handler(node, context);
    }

    /// <summary>
    /// Ejecuta un script comenzando desde un nodo de evento específico.
    /// </summary>
    private async Task ExecuteEventAsync(ScriptDefinition script, string eventType)
    {
        if (!Enum.TryParse<NodeTypeId>(eventType, true, out var nodeTypeId))
            return;

        var eventNode = script.Nodes.FirstOrDefault(n => n.NodeType == nodeTypeId);
        if (eventNode == null) return;

        var context = new ScriptContext(script, _gameState, _world);
        await ExecuteFromNodeAsync(eventNode, "Exec", context);
    }

    /// <summary>
    /// Ejecuta un nodo y sigue el flujo de ejecución.
    /// </summary>
    private async Task ExecuteFromNodeAsync(ScriptNode node, string inputPortName, ScriptContext context)
    {
        if (!_nodeHandlers.TryGetValue(node.NodeType, out var handler))
            return;

        // Ejecutar el nodo
        await handler(node, context);

        // Obtener puerto de salida de ejecución por defecto
        var outputPortName = context.NextOutputPort ?? "Exec";
        context.NextOutputPort = null;

        // Buscar conexión desde este puerto
        var nextConnection = context.Script.Connections.FirstOrDefault(c =>
            string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.FromPortName, outputPortName, StringComparison.OrdinalIgnoreCase));

        if (nextConnection == null) return;

        // Encontrar y ejecutar el siguiente nodo
        var nextNode = context.Script.Nodes.FirstOrDefault(n =>
            string.Equals(n.Id, nextConnection.ToNodeId, StringComparison.OrdinalIgnoreCase));
        if (nextNode != null)
        {
            await ExecuteFromNodeAsync(nextNode, nextConnection.ToPortName, context);
        }
    }

    /// <summary>
    /// Obtiene el valor de una propiedad de un nodo.
    /// </summary>
    private T? GetPropertyValue<T>(ScriptNode node, string propertyName, T? defaultValue = default)
    {
        if (node.Properties.TryGetValue(propertyName, out var value))
        {
            if (value == null)
                return defaultValue;

            if (value is T typedValue)
                return typedValue;

            // Manejar JsonElement (cuando se deserializa desde JSON)
            if (value is JsonElement jsonElement)
            {
                try
                {
                    return jsonElement.Deserialize<T>();
                }
                catch
                {
                    // Si la deserialización falla, intentar conversión de string
                    var stringValue = jsonElement.ToString();
                    if (typeof(T) == typeof(string))
                        return (T)(object)stringValue;

                    try
                    {
                        return (T)Convert.ChangeType(stringValue, typeof(T));
                    }
                    catch
                    {
                        return defaultValue;
                    }
                }
            }

            // Intentar conversión directa
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /// <summary>
    /// Registra todos los handlers de nodos.
    /// </summary>
    private Dictionary<NodeTypeId, Func<ScriptNode, ScriptContext, Task>> RegisterNodeHandlers()
    {
        return new Dictionary<NodeTypeId, Func<ScriptNode, ScriptContext, Task>>()
        {
            // === EVENTOS (no hacen nada, solo son entry points) ===
            // Game Events
            [NodeTypeId.Event_OnGameStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnGameEnd] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_EveryMinute] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_EveryHour] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnTurnStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnWeatherChange] = async (node, ctx) => { await Task.CompletedTask; },
            // Room Events
            [NodeTypeId.Event_OnEnter] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnExit] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnLook] = async (node, ctx) => { await Task.CompletedTask; },
            // Door Events
            [NodeTypeId.Event_OnDoorOpen] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDoorClose] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDoorLock] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDoorUnlock] = async (node, ctx) => { await Task.CompletedTask; },
            // NPC Events
            [NodeTypeId.Event_OnTalk] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnNpcAttack] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnNpcDeath] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnNpcSee] = async (node, ctx) => { await Task.CompletedTask; },
            // Combat Events
            [NodeTypeId.Event_OnCombatStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatVictory] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatDefeat] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatFlee] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnPlayerAttack] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnNpcTurn] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnPlayerDefend] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCriticalHit] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnMiss] = async (node, ctx) => { await Task.CompletedTask; },
            // Trade Events
            [NodeTypeId.Event_OnTradeStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnTradeEnd] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnItemBought] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnItemSold] = async (node, ctx) => { await Task.CompletedTask; },
            // Object Events
            [NodeTypeId.Event_OnTake] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDrop] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnUse] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnExamine] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnRead] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnGive] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnContainerOpen] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnContainerClose] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnEat] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDrink] = async (node, ctx) => { await Task.CompletedTask; },
            // Quest Events
            [NodeTypeId.Event_OnQuestStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnQuestComplete] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnQuestFail] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnObjectiveComplete] = async (node, ctx) => { await Task.CompletedTask; },
            // Sleep Events
            [NodeTypeId.Event_OnSleep] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnWakeUp] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnWakeUpStartled] = async (node, ctx) => { await Task.CompletedTask; },
            // Player State Events
            [NodeTypeId.Event_OnPlayerDeath] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHealthLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHealthCritical] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHungerHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnThirstHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnEnergyLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnSleepHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnSanityLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnManaLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnStateThreshold] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnModifierApplied] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnModifierExpired] = async (node, ctx) => { await Task.CompletedTask; },
            // Money Events
            [NodeTypeId.Event_OnMoneyGained] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnMoneyLost] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnMoneyThreshold] = async (node, ctx) => { await Task.CompletedTask; },

            // === CONDICIONES ===
            [NodeTypeId.Condition_HasItem] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var hasItem = !string.IsNullOrEmpty(objectId) &&
                              ctx.GameState.InventoryObjectIds.Any(id =>
                                  string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                DebugMessage($"[Debug] Condition_HasItem({objectId}): {hasItem} (inventario: {string.Join(", ", ctx.GameState.InventoryObjectIds)})");
                ctx.NextOutputPort = hasItem ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsInRoom] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                var isInRoom = string.Equals(ctx.GameState.CurrentRoomId, roomId, StringComparison.OrdinalIgnoreCase);
                ctx.NextOutputPort = isInRoom ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsQuestStatus] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "") ?? "";
                var statusStr = GetPropertyValue<string>(node, "Status", "NotStarted");

                var currentStatus = ctx.GameState.Quests.TryGetValue(questId, out var state)
                    ? state.Status
                    : QuestStatus.NotStarted;

                var expectedStatus = Enum.TryParse<QuestStatus>(statusStr, out var parsed)
                    ? parsed
                    : QuestStatus.NotStarted;

                ctx.NextOutputPort = currentStatus == expectedStatus ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsMainQuest] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "") ?? "";
                var quest = ctx.World.Quests.FirstOrDefault(q =>
                    string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                var isMain = quest?.IsMainQuest ?? false;
                ctx.NextOutputPort = isMain ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_HasFlag] = async (node, ctx) =>
            {
                var flagName = GetPropertyValue<string>(node, "FlagName", "");
                var hasFlag = !string.IsNullOrEmpty(flagName) &&
                              ctx.GameState.Flags.TryGetValue(flagName, out var value) && value;
                ctx.NextOutputPort = hasFlag ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_CompareCounter] = async (node, ctx) =>
            {
                var counterName = GetPropertyValue<string>(node, "CounterName", "") ?? "";
                var op = GetPropertyValue<string>(node, "Operator", "==");
                var compareValue = GetPropertyValue<int>(node, "Value", 0);

                ctx.GameState.Counters.TryGetValue(counterName, out var currentValue);

                var result = op switch
                {
                    "==" => currentValue == compareValue,
                    "!=" => currentValue != compareValue,
                    "<" => currentValue < compareValue,
                    "<=" => currentValue <= compareValue,
                    ">" => currentValue > compareValue,
                    ">=" => currentValue >= compareValue,
                    _ => false
                };

                ctx.NextOutputPort = result ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsTimeOfDay] = async (node, ctx) =>
            {
                var timeRange = GetPropertyValue<string>(node, "TimeRange", "");
                var hour = ctx.GameState.GameTime.Hour;

                var isInRange = timeRange switch
                {
                    "Manana" => hour >= 6 && hour < 12,
                    "Tarde" => hour >= 12 && hour < 20,
                    "Noche" => hour >= 20 || hour < 0,
                    "Madrugada" => hour >= 0 && hour < 6,
                    _ => false
                };

                ctx.NextOutputPort = isInRange ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsDoorOpen] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                var isOpen = door?.IsOpen ?? false;
                ctx.NextOutputPort = isOpen ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsDoorVisible] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));

                // Una puerta es visible si Visible = true y cumple todos los requisitos de misiones
                var isVisible = false;
                if (door != null)
                {
                    if (!door.Visible)
                    {
                        isVisible = false;
                    }
                    else if (door.RequiredQuests.Count == 0)
                    {
                        isVisible = true;
                    }
                    else
                    {
                        isVisible = true;
                        foreach (var requirement in door.RequiredQuests)
                        {
                            var quest = ctx.GameState.Quests.Values.FirstOrDefault(q =>
                                q.QuestId.Equals(requirement.QuestId, StringComparison.OrdinalIgnoreCase));
                            if (quest == null || quest.Status != requirement.RequiredStatus)
                            {
                                isVisible = false;
                                break;
                            }
                        }
                    }
                }
                ctx.NextOutputPort = isVisible ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsNpcVisible] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isVisible = npc?.Visible ?? false;
                ctx.NextOutputPort = isVisible ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsObjectVisible] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var isVisible = obj?.Visible ?? false;
                ctx.NextOutputPort = isVisible ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsObjectTakeable] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var canTake = obj?.CanTake ?? false;
                ctx.NextOutputPort = canTake ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsContainerOpen] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var isOpen = obj != null && obj.IsContainer && obj.IsOpen;
                ctx.NextOutputPort = isOpen ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsContainerLocked] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var isLocked = obj != null && obj.IsContainer && obj.IsLocked;
                ctx.NextOutputPort = isLocked ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsWeather] = async (node, ctx) =>
            {
                var weatherStr = GetPropertyValue<string>(node, "Weather", "Despejado");
                var isMatch = Enum.TryParse<WeatherType>(weatherStr, out var weather) &&
                              ctx.GameState.Weather == weather;
                ctx.NextOutputPort = isMatch ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_ObjectInContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var containerId = GetPropertyValue<string>(node, "ContainerId", "");

                var container = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, containerId, StringComparison.OrdinalIgnoreCase));

                var isInContainer = container != null && container.IsContainer &&
                    container.ContainedObjectIds.Any(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));

                ctx.NextOutputPort = isInContainer ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_ObjectInRoom] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var roomId = GetPropertyValue<string>(node, "RoomId", "");

                var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                    string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));

                var isInRoom = room != null && room.ObjectIds.Any(id =>
                    string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));

                ctx.NextOutputPort = isInRoom ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcInRoom] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var roomId = GetPropertyValue<string>(node, "RoomId", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                var isInRoom = npc != null &&
                    string.Equals(npc.RoomId, roomId, StringComparison.OrdinalIgnoreCase);

                ctx.NextOutputPort = isInRoom ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE ILUMINACIÓN ===
            [NodeTypeId.Condition_IsObjectLit] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var isLit = obj != null && obj.IsLightSource && obj.IsLit;
                ctx.NextOutputPort = isLit ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsRoomLit] = async (node, ctx) =>
            {
                var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                    string.Equals(r.Id, ctx.GameState.CurrentRoomId, StringComparison.OrdinalIgnoreCase));

                if (room == null)
                {
                    ctx.NextOutputPort = "False";
                    await Task.CompletedTask;
                    return;
                }

                var timeOfDay = ctx.GameState.GameTime.TimeOfDay;
                bool isNight = timeOfDay.Hours >= 20 || timeOfDay.Hours < 7;

                // Iluminación base de la sala
                bool baseIllumination;
                if (room.IsInterior)
                {
                    baseIllumination = room.IsIlluminated;
                }
                else
                {
                    baseIllumination = !isNight;
                }

                if (baseIllumination)
                {
                    ctx.NextOutputPort = "True";
                    await Task.CompletedTask;
                    return;
                }

                // Buscar objetos luminosos encendidos en inventario
                foreach (var objId in ctx.GameState.InventoryObjectIds)
                {
                    var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, objId, StringComparison.OrdinalIgnoreCase));
                    if (obj != null && obj.IsLightSource && obj.IsLit)
                    {
                        ctx.NextOutputPort = "True";
                        await Task.CompletedTask;
                        return;
                    }
                }

                // Buscar objetos luminosos encendidos en la sala
                foreach (var objId in room.ObjectIds)
                {
                    var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, objId, StringComparison.OrdinalIgnoreCase));
                    if (obj == null) continue;

                    if (obj.IsLightSource && obj.IsLit)
                    {
                        ctx.NextOutputPort = "True";
                        await Task.CompletedTask;
                        return;
                    }

                    // Revisar contenedores
                    if (obj.IsContainer && (obj.IsOpen || obj.ContentsVisible))
                    {
                        foreach (var containedId in obj.ContainedObjectIds)
                        {
                            var containedObj = ctx.GameState.Objects.FirstOrDefault(o =>
                                string.Equals(o.Id, containedId, StringComparison.OrdinalIgnoreCase));
                            if (containedObj != null && containedObj.IsLightSource && containedObj.IsLit)
                            {
                                ctx.NextOutputPort = "True";
                                await Task.CompletedTask;
                                return;
                            }
                        }
                    }
                }

                ctx.NextOutputPort = "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsPatrolling] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isPatrolling = npc?.IsPatrolling ?? false;
                ctx.NextOutputPort = isPatrolling ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsFollowingPlayer] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isFollowing = npc?.IsFollowingPlayer ?? false;
                ctx.NextOutputPort = isFollowing ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_Random] = async (node, ctx) =>
            {
                var probability = GetPropertyValue<int>(node, "Probability", 50);
                var roll = _random.Next(100);
                ctx.NextOutputPort = roll < probability ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE COMBATE ===
            [NodeTypeId.Condition_IsNpcAlive] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isAlive = npc != null && !npc.IsCorpse && npc.Stats.CurrentHealth > 0;
                ctx.NextOutputPort = isAlive ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcHealthBelow] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isBelow = npc != null && npc.Stats.CurrentHealth < threshold;
                ctx.NextOutputPort = isBelow ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsInCombat] = async (node, ctx) =>
            {
                var isInCombat = ctx.GameState.ActiveCombat?.IsActive ?? false;
                ctx.NextOutputPort = isInCombat ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE COMBATE ADICIONALES ===
            [NodeTypeId.Condition_PlayerHealthBelow] = async (node, ctx) =>
            {
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var player = ctx.GameState.Player;
                var percentHealth = player.DynamicStats.MaxHealth > 0
                    ? (player.DynamicStats.Health * 100 / player.DynamicStats.MaxHealth)
                    : 100;
                ctx.NextOutputPort = percentHealth < threshold ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_PlayerHealthAbove] = async (node, ctx) =>
            {
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var player = ctx.GameState.Player;
                var percentHealth = player.DynamicStats.MaxHealth > 0
                    ? (player.DynamicStats.Health * 100 / player.DynamicStats.MaxHealth)
                    : 100;
                ctx.NextOutputPort = percentHealth > threshold ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_PlayerHasWeaponType] = async (node, ctx) =>
            {
                var damageTypeStr = GetPropertyValue<string>(node, "DamageType", "Physical");
                var expectedType = Enum.TryParse<DamageType>(damageTypeStr, out var dt) ? dt : DamageType.Physical;

                var hasWeaponType = false;
                // Verificar armas en ambas manos
                var rightHandId = ctx.GameState.Player.EquippedRightHandId;
                var leftHandId = ctx.GameState.Player.EquippedLeftHandId;
                if (!string.IsNullOrEmpty(rightHandId))
                {
                    var weapon = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, rightHandId, StringComparison.OrdinalIgnoreCase) && o.Type == ObjectType.Arma);
                    hasWeaponType = weapon?.DamageType == expectedType;
                }
                if (!hasWeaponType && !string.IsNullOrEmpty(leftHandId) && leftHandId != rightHandId)
                {
                    var weapon = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, leftHandId, StringComparison.OrdinalIgnoreCase) && o.Type == ObjectType.Arma);
                    hasWeaponType = weapon?.DamageType == expectedType;
                }
                ctx.NextOutputPort = hasWeaponType ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_PlayerHasArmor] = async (node, ctx) =>
            {
                // Verificar armadura en cualquier slot (incluyendo casco)
                var hasArmor = !string.IsNullOrEmpty(ctx.GameState.Player.EquippedTorsoId) ||
                               !string.IsNullOrEmpty(ctx.GameState.Player.EquippedHeadId) ||
                               (!string.IsNullOrEmpty(ctx.GameState.Player.EquippedRightHandId) &&
                                ctx.GameState.Objects.Any(o => o.Id == ctx.GameState.Player.EquippedRightHandId && o.Type == ObjectType.Armadura)) ||
                               (!string.IsNullOrEmpty(ctx.GameState.Player.EquippedLeftHandId) &&
                                ctx.GameState.Objects.Any(o => o.Id == ctx.GameState.Player.EquippedLeftHandId && o.Type == ObjectType.Armadura));
                ctx.NextOutputPort = hasArmor ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsCombatRound] = async (node, ctx) =>
            {
                var expectedRound = GetPropertyValue<int>(node, "Round", 1);
                var currentRound = ctx.GameState.ActiveCombat?.RoundNumber ?? 0;
                ctx.NextOutputPort = currentRound == expectedRound ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE COMERCIO ===
            [NodeTypeId.Condition_IsInTrade] = async (node, ctx) =>
            {
                // Trade state is managed externally by TradeEngine, not in GameState
                // This condition returns false by default - can be extended if trade state is added to GameState
                ctx.NextOutputPort = "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_PlayerHasMoney] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                var hasEnough = ctx.GameState.Player.Money >= amount;
                ctx.NextOutputPort = hasEnough ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcHasMoney] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                // NPC con Money = -1 tiene infinito
                var hasEnough = npc != null && (npc.Money < 0 || npc.Money >= amount);
                ctx.NextOutputPort = hasEnough ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcHasInfiniteMoney] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var hasInfinite = npc != null && npc.Money < 0;
                ctx.NextOutputPort = hasInfinite ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_PlayerOwnsItem] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var requiredQty = GetPropertyValue<int>(node, "Quantity", 1);
                var ownedQty = ctx.GameState.InventoryObjectIds.Count(id =>
                    string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                ctx.NextOutputPort = ownedQty >= requiredQty ? "True" : "False";
                await Task.CompletedTask;
            },

            // === ACCIONES ===
            [NodeTypeId.Action_ShowMessage] = async (node, ctx) =>
            {
                var message = GetPropertyValue<string>(node, "Message", "");
                if (!string.IsNullOrEmpty(message))
                {
                    OnMessage?.Invoke(message);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_GiveItem] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                if (!string.IsNullOrEmpty(objectId) &&
                    !ctx.GameState.InventoryObjectIds.Any(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase)))
                {
                    ctx.GameState.InventoryObjectIds.Add(objectId);

                    // Quitar de la sala si estaba ahí
                    var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                    if (obj != null && !string.IsNullOrEmpty(obj.RoomId))
                    {
                        var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                            string.Equals(r.Id, obj.RoomId, StringComparison.OrdinalIgnoreCase));
                        room?.ObjectIds.RemoveAll(id =>
                            string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                        obj.RoomId = null;
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_RemoveItem] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                if (!string.IsNullOrEmpty(objectId))
                {
                    ctx.GameState.InventoryObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_TeleportPlayer] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                if (!string.IsNullOrEmpty(roomId))
                {
                    ctx.GameState.CurrentRoomId = roomId;
                    OnPlayerTeleported?.Invoke(roomId);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetRoomIllumination] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                var illuminated = GetPropertyValue<bool>(node, "IsIlluminated", true);

                var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                    string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                if (room != null)
                {
                    room.IsIlluminated = illuminated;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetRoomMusic] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                var musicId = GetPropertyValue<string>(node, "MusicId", "");

                var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                    string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                if (room != null && !string.IsNullOrEmpty(roomId))
                {
                    room.MusicId = string.IsNullOrEmpty(musicId) ? null : musicId;
                    OnRoomMusicChanged?.Invoke(roomId, room.MusicId);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetRoomDescription] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                var description = GetPropertyValue<string>(node, "Description", "");

                var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                    string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                if (room != null && description != null)
                {
                    room.Description = description;
                }
                await Task.CompletedTask;
            },

            // === GAME STATE HANDLERS ===
            [NodeTypeId.Action_SetWeather] = async (node, ctx) =>
            {
                var weatherStr = GetPropertyValue<string>(node, "Weather", "Despejado");
                if (Enum.TryParse<WeatherType>(weatherStr, out var weather))
                {
                    ctx.GameState.Weather = weather;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetGameHour] = async (node, ctx) =>
            {
                var hour = GetPropertyValue<int>(node, "Hour", 12);
                var clampedHour = Math.Clamp(hour, 0, 23);
                var currentTime = ctx.GameState.GameTime;
                ctx.GameState.GameTime = new DateTime(
                    currentTime.Year, currentTime.Month, currentTime.Day,
                    clampedHour, currentTime.Minute, currentTime.Second);
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_AdvanceTime] = async (node, ctx) =>
            {
                var hours = GetPropertyValue<int>(node, "Hours", 1);
                ctx.GameState.GameTime = ctx.GameState.GameTime.AddHours(hours);
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_MoveNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var roomId = GetPropertyValue<string>(node, "RoomId", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(roomId))
                {
                    // Quitar de sala anterior
                    if (!string.IsNullOrEmpty(npc.RoomId))
                    {
                        var oldRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                            string.Equals(r.Id, npc.RoomId, StringComparison.OrdinalIgnoreCase));
                        oldRoom?.NpcIds.RemoveAll(id =>
                            string.Equals(id, npcId, StringComparison.OrdinalIgnoreCase));
                    }

                    // Añadir a nueva sala
                    npc.RoomId = roomId;
                    var newRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                        string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                    if (newRoom != null && !string.IsNullOrEmpty(npcId) && !newRoom.NpcIds.Any(id =>
                        string.Equals(id, npcId, StringComparison.OrdinalIgnoreCase)))
                    {
                        newRoom.NpcIds.Add(npcId);
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetFlag] = async (node, ctx) =>
            {
                var flagName = GetPropertyValue<string>(node, "FlagName", "");
                var value = GetPropertyValue<bool>(node, "Value", true);

                if (!string.IsNullOrEmpty(flagName))
                {
                    ctx.GameState.Flags[flagName] = value;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetCounter] = async (node, ctx) =>
            {
                var counterName = GetPropertyValue<string>(node, "CounterName", "");
                var value = GetPropertyValue<int>(node, "Value", 0);

                if (!string.IsNullOrEmpty(counterName))
                {
                    ctx.GameState.Counters[counterName] = value;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_IncrementCounter] = async (node, ctx) =>
            {
                var counterName = GetPropertyValue<string>(node, "CounterName", "");
                var amount = GetPropertyValue<int>(node, "Amount", 1);

                if (!string.IsNullOrEmpty(counterName))
                {
                    ctx.GameState.Counters.TryGetValue(counterName, out var current);
                    ctx.GameState.Counters[counterName] = current + amount;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_PlaySound] = async (node, ctx) =>
            {
                var soundId = GetPropertyValue<string>(node, "SoundId", "");
                if (!string.IsNullOrEmpty(soundId))
                {
                    OnPlaySound?.Invoke(soundId);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_StartQuest] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId))
                {
                    ctx.GameState.Quests[questId] = new QuestState
                    {
                        QuestId = questId,
                        Status = QuestStatus.InProgress,
                        CurrentObjectiveIndex = 0
                    };
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[Nueva misión: {questName}]");

                    // Disparar evento de inicio de quest
                    await TriggerEventAsync("Quest", questId, "Event_OnQuestStart");
                }
            },

            [NodeTypeId.Action_CompleteQuest] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Completed;
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[¡Misión completada: {questName}!]");

                    // Disparar evento de quest completada
                    await TriggerEventAsync("Quest", questId, "Event_OnQuestComplete");

                    // Verificar si todas las misiones principales están completadas
                    var mainQuests = ctx.World.Quests.Where(q => q.IsMainQuest).ToList();
                    if (mainQuests.Any())
                    {
                        var allMainQuestsCompleted = mainQuests.All(mq =>
                            ctx.GameState.Quests.TryGetValue(mq.Id, out var qs) &&
                            qs.Status == QuestStatus.Completed);
                        if (allMainQuestsCompleted)
                        {
                            OnAdventureCompleted?.Invoke();
                        }
                    }
                }
            },

            [NodeTypeId.Action_FailQuest] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Failed;
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[Misión fallida: {questName}]");

                    // Disparar evento de quest fallida
                    await TriggerEventAsync("Quest", questId, "Event_OnQuestFail");
                }
            },

            [NodeTypeId.Action_SetQuestStatus] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                var statusStr = GetPropertyValue<string>(node, "Status", "InProgress");

                if (!string.IsNullOrEmpty(questId) && Enum.TryParse<QuestStatus>(statusStr, out var newStatus))
                {
                    // Si la misión no existe en el estado, crearla
                    if (!ctx.GameState.Quests.TryGetValue(questId, out var state))
                    {
                        state = new QuestState
                        {
                            QuestId = questId,
                            Status = newStatus,
                            CurrentObjectiveIndex = 0
                        };
                        ctx.GameState.Quests[questId] = state;
                    }
                    else
                    {
                        state.Status = newStatus;
                    }

                    // Mostrar mensaje según el nuevo estado
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;

                    var message = newStatus switch
                    {
                        QuestStatus.InProgress => $"[Nueva misión: {questName}]",
                        QuestStatus.Completed => $"[¡Misión completada: {questName}!]",
                        QuestStatus.Failed => $"[Misión fallida: {questName}]",
                        _ => null
                    };

                    if (message != null)
                        OnMessage?.Invoke(message);

                    // Si se completa, verificar si todas las principales están completadas
                    if (newStatus == QuestStatus.Completed)
                    {
                        var mainQuests = ctx.World.Quests.Where(q => q.IsMainQuest).ToList();
                        if (mainQuests.Any())
                        {
                            var allMainQuestsCompleted = mainQuests.All(mq =>
                                ctx.GameState.Quests.TryGetValue(mq.Id, out var qs) &&
                                qs.Status == QuestStatus.Completed);
                            if (allMainQuestsCompleted)
                            {
                                OnAdventureCompleted?.Invoke();
                            }
                        }
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_AdvanceObjective] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));

                    if (quest != null && state.CurrentObjectiveIndex < quest.Objectives.Count - 1)
                    {
                        state.CurrentObjectiveIndex++;
                        var newObjective = quest.Objectives[state.CurrentObjectiveIndex];
                        OnMessage?.Invoke($"[Nuevo objetivo: {newObjective}]");
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_OpenDoor] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    door.IsOpen = true;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_CloseDoor] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    door.IsOpen = false;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_LockDoor] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null && !string.IsNullOrEmpty(doorId))
                {
                    door.IsLocked = true;
                    await TriggerEventAsync("Door", doorId, "Event_OnDoorLock");
                }
            },

            [NodeTypeId.Action_UnlockDoor] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null && !string.IsNullOrEmpty(doorId))
                {
                    door.IsLocked = false;
                    await TriggerEventAsync("Door", doorId, "Event_OnDoorUnlock");
                }
            },

            [NodeTypeId.Action_SetDoorVisible] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var visible = GetPropertyValue<bool>(node, "Visible", true);

                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    door.Visible = visible;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetNpcVisible] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var visible = GetPropertyValue<bool>(node, "Visible", true);

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.Visible = visible;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetObjectVisible] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var visible = GetPropertyValue<bool>(node, "Visible", true);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null)
                {
                    obj.Visible = visible;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetObjectTakeable] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var canTake = GetPropertyValue<bool>(node, "CanTake", true);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null)
                {
                    obj.CanTake = canTake;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_OpenContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsContainer && obj.IsOpenable)
                {
                    obj.IsOpen = true;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_CloseContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsContainer && obj.IsOpenable)
                {
                    obj.IsOpen = false;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_LockContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsContainer)
                {
                    obj.IsLocked = true;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_UnlockContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsContainer)
                {
                    obj.IsLocked = false;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetContentsVisible] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var visible = GetPropertyValue<bool>(node, "Visible", true);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsContainer)
                {
                    obj.ContentsVisible = visible;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetObjectPrice] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var price = GetPropertyValue<int>(node, "Price", 0);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null)
                {
                    obj.Price = price;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetObjectDurability] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var durability = GetPropertyValue<int>(node, "Durability", 100);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null)
                {
                    obj.CurrentDurability = Math.Clamp(durability, 0, obj.MaxDurability);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_MoveObjectToRoom] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var roomId = GetPropertyValue<string>(node, "RoomId", "");

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));

                if (obj != null && !string.IsNullOrEmpty(roomId))
                {
                    // Quitar de inventario si está ahí
                    ctx.GameState.InventoryObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));

                    // Quitar de sala anterior
                    if (!string.IsNullOrEmpty(obj.RoomId))
                    {
                        var oldRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                            string.Equals(r.Id, obj.RoomId, StringComparison.OrdinalIgnoreCase));
                        oldRoom?.ObjectIds.RemoveAll(id =>
                            string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                    }

                    // Añadir a nueva sala
                    obj.RoomId = roomId;
                    var newRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                        string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                    if (newRoom != null && !string.IsNullOrEmpty(objectId) && !newRoom.ObjectIds.Contains(objectId))
                    {
                        newRoom.ObjectIds.Add(objectId);
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_PutObjectInContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var containerId = GetPropertyValue<string>(node, "ContainerId", "");

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var container = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, containerId, StringComparison.OrdinalIgnoreCase));

                if (obj != null && container != null && container.IsContainer)
                {
                    // Quitar de inventario
                    ctx.GameState.InventoryObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));

                    // Quitar de sala
                    if (!string.IsNullOrEmpty(obj.RoomId))
                    {
                        var room = ctx.GameState.Rooms.FirstOrDefault(r =>
                            string.Equals(r.Id, obj.RoomId, StringComparison.OrdinalIgnoreCase));
                        room?.ObjectIds.RemoveAll(id =>
                            string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                        obj.RoomId = null;
                    }

                    // Añadir al contenedor
                    if (!string.IsNullOrEmpty(objectId) && !container.ContainedObjectIds.Contains(objectId))
                    {
                        container.ContainedObjectIds.Add(objectId);
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_RemoveObjectFromContainer] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var containerId = GetPropertyValue<string>(node, "ContainerId", "");

                var container = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, containerId, StringComparison.OrdinalIgnoreCase));

                if (container != null && container.IsContainer)
                {
                    container.ContainedObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },

            // === LIGHT SOURCE HANDLERS ===
            [NodeTypeId.Action_SetObjectLit] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var isLit = GetPropertyValue<bool>(node, "IsLit", true);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsLightSource)
                {
                    obj.IsLit = isLit;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetLightTurns] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var turns = GetPropertyValue<int>(node, "Turns", -1);

                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                if (obj != null && obj.IsLightSource)
                {
                    obj.LightTurnsRemaining = turns;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_AddMoney] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 0);
                ctx.GameState.Player.Money += amount;
                if (amount > 0)
                {
                    await TriggerEventAsync("Game", ctx.World.Game?.Id ?? "game", "Event_OnMoneyGained");
                }
            },

            [NodeTypeId.Action_RemoveMoney] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 0);
                var hadMoney = ctx.GameState.Player.Money;
                ctx.GameState.Player.Money = Math.Max(0, ctx.GameState.Player.Money - amount);
                if (hadMoney > ctx.GameState.Player.Money)
                {
                    await TriggerEventAsync("Game", ctx.World.Game?.Id ?? "game", "Event_OnMoneyLost");
                }
            },

            // === NPC PATROL HANDLERS ===
            [NodeTypeId.Action_StartPatrol] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.IsPatrolling = true;
                    npc.PatrolTurnCounter = 0;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_StopPatrol] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.IsPatrolling = false;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_PatrolStep] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && npc.PatrolRoute.Count > 1)
                {
                    // Calcular siguiente índice (modo ping-pong)
                    var next = npc.PatrolRouteIndex + npc.PatrolDirection;
                    if (next >= npc.PatrolRoute.Count || next < 0)
                    {
                        npc.PatrolDirection *= -1;
                        next = npc.PatrolRouteIndex + npc.PatrolDirection;
                    }
                    int nextIndex = Math.Clamp(next, 0, npc.PatrolRoute.Count - 1);

                    // Mover NPC
                    var nextRoomId = npc.PatrolRoute[nextIndex];

                    // Quitar de sala anterior
                    if (!string.IsNullOrEmpty(npc.RoomId))
                    {
                        var oldRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                            string.Equals(r.Id, npc.RoomId, StringComparison.OrdinalIgnoreCase));
                        oldRoom?.NpcIds.RemoveAll(id =>
                            string.Equals(id, npcId, StringComparison.OrdinalIgnoreCase));
                    }

                    // Añadir a nueva sala
                    npc.RoomId = nextRoomId;
                    var newRoom = ctx.GameState.Rooms.FirstOrDefault(r =>
                        string.Equals(r.Id, nextRoomId, StringComparison.OrdinalIgnoreCase));
                    if (newRoom != null && !string.IsNullOrEmpty(npcId) && !newRoom.NpcIds.Any(id =>
                        string.Equals(id, npcId, StringComparison.OrdinalIgnoreCase)))
                    {
                        newRoom.NpcIds.Add(npcId);
                    }

                    npc.PatrolRouteIndex = nextIndex;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetPatrolMode] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var modeStr = GetPropertyValue<string>(node, "Mode", "Turns");
                var turnSpeed = GetPropertyValue<int>(node, "TurnSpeed", 1);
                var timeInterval = GetPropertyValue<float>(node, "TimeInterval", 5.0f);

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.PatrolMovementMode = modeStr == "Time" ? MovementMode.Time : MovementMode.Turns;
                    npc.PatrolSpeed = Math.Max(1, turnSpeed);
                    npc.PatrolTimeInterval = Math.Clamp(timeInterval, 0f, 60f);
                    // Resetear contadores
                    npc.PatrolTurnCounter = 0;
                    npc.PatrolLastMoveTime = DateTime.UtcNow;
                }
                await Task.CompletedTask;
            },

            // === NPC FOLLOW HANDLERS ===
            [NodeTypeId.Action_FollowPlayer] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var speed = GetPropertyValue<int>(node, "Speed", 1);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.IsFollowingPlayer = true;
                    npc.FollowSpeed = Math.Clamp(speed, 1, 3);
                    npc.FollowMoveCounter = 0;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_StopFollowing] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.IsFollowingPlayer = false;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetFollowMode] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var modeStr = GetPropertyValue<string>(node, "Mode", "Turns");
                var turnSpeed = GetPropertyValue<int>(node, "TurnSpeed", 1);
                var timeInterval = GetPropertyValue<float>(node, "TimeInterval", 3.0f);

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.FollowMovementMode = modeStr == "Time" ? MovementMode.Time : MovementMode.Turns;
                    npc.FollowSpeed = Math.Clamp(turnSpeed, 1, 3);
                    npc.FollowTimeInterval = Math.Clamp(timeInterval, 0f, 60f);
                    // Resetear contadores
                    npc.FollowMoveCounter = 0;
                    npc.FollowLastMoveTime = DateTime.UtcNow;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_StartConversation] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                if (!string.IsNullOrEmpty(npcId))
                {
                    OnStartConversation?.Invoke(npcId);
                }
                await Task.CompletedTask;
            },

            // === CONTROL DE FLUJO ===
            [NodeTypeId.Flow_Branch] = async (node, ctx) =>
            {
                // La condición se obtiene de un puerto de datos conectado
                // Por ahora, usamos True por defecto
                ctx.NextOutputPort = "True";
                await Task.CompletedTask;
            },

            [NodeTypeId.Flow_Sequence] = async (node, ctx) =>
            {
                // Ejecutar cada salida en orden
                for (int i = 0; i < 3; i++)
                {
                    var outputPort = $"Then{i}";
                    var connection = ctx.Script.Connections.FirstOrDefault(c =>
                        string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(c.FromPortName, outputPort, StringComparison.OrdinalIgnoreCase));

                    if (connection != null)
                    {
                        var nextNode = ctx.Script.Nodes.FirstOrDefault(n =>
                            string.Equals(n.Id, connection.ToNodeId, StringComparison.OrdinalIgnoreCase));
                        if (nextNode != null)
                        {
                            await ExecuteFromNodeAsync(nextNode, connection.ToPortName, ctx);
                        }
                    }
                }
                // No continuar automáticamente después de Sequence
                ctx.NextOutputPort = null;
            },

            [NodeTypeId.Flow_Delay] = async (node, ctx) =>
            {
                var seconds = GetPropertyValue<float>(node, "Seconds", 1.0f);
                await Task.Delay(TimeSpan.FromSeconds(seconds));
            },

            [NodeTypeId.Flow_RandomBranch] = async (node, ctx) =>
            {
                var randomOutput = _random.Next(3);
                ctx.NextOutputPort = $"Out{randomOutput}";
                await Task.CompletedTask;
            },

            // === VARIABLES ===
            [NodeTypeId.Variable_GetFlag] = async (node, ctx) =>
            {
                // Las variables no afectan el flujo directamente
                await Task.CompletedTask;
            },

            [NodeTypeId.Variable_GetCounter] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            [NodeTypeId.Variable_GetCurrentRoom] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            [NodeTypeId.Variable_GetGameHour] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            [NodeTypeId.Variable_GetPlayerMoney] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            // === VARIABLES DE ESTADOS DINÁMICOS ===
            [NodeTypeId.Variable_GetPlayerHealth] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Health);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerMaxHealth] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.MaxHealth);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerHunger] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Hunger);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerThirst] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Thirst);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerEnergy] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Energy);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerSleep] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Sleep);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerSanity] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Sanity);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerMana] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Mana);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerMaxMana] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.MaxMana);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetPlayerState] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.SetOutputValue(node.Id, "Value", value);
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetActiveModifiersCount] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.ActiveModifiers.Count(m => !m.IsExpired));
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_HasModifier] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                var hasIt = ctx.GameState.ActiveModifiers.Any(m =>
                    !m.IsExpired && string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                ctx.SetOutputValue(node.Id, "Value", hasIt);
                await Task.CompletedTask;
            },

            // === CONDICIONES DE ESTADOS ===
            [NodeTypeId.Condition_PlayerStateAbove] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value > threshold ? "True" : "False";
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_PlayerStateBelow] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var threshold = GetPropertyValue<int>(node, "Threshold", 25);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value < threshold ? "True" : "False";
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_PlayerStateEquals] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var targetValue = GetPropertyValue<int>(node, "Value", 100);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value == targetValue ? "True" : "False";
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_PlayerStateBetween] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var minValue = GetPropertyValue<int>(node, "MinValue", 25);
                var maxValue = GetPropertyValue<int>(node, "MaxValue", 75);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value >= minValue && value <= maxValue ? "True" : "False";
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_HasModifier] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                var hasIt = ctx.GameState.ActiveModifiers.Any(m =>
                    !m.IsExpired && string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                ctx.NextOutputPort = hasIt ? "True" : "False";
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_HasModifierForState] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                if (Enum.TryParse<PlayerStateType>(stateType, out var st))
                {
                    var hasIt = ctx.GameState.ActiveModifiers.Any(m => !m.IsExpired && m.StateType == st);
                    ctx.NextOutputPort = hasIt ? "True" : "False";
                }
                else
                {
                    ctx.NextOutputPort = "False";
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Condition_IsPlayerAlive] = async (node, ctx) =>
            {
                ctx.NextOutputPort = ctx.GameState.Player.DynamicStats.Health > 0 ? "True" : "False";
                await Task.CompletedTask;
            },

            // === ACCIONES DE ESTADOS ===
            [NodeTypeId.Action_SetPlayerState] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var value = GetPropertyValue<int>(node, "Value", 100);
                SetPlayerStateValue(ctx.GameState, stateType ?? "Health", value);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_ModifyPlayerState] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var current = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                SetPlayerStateValue(ctx.GameState, stateType ?? "Health", current + amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_HealPlayer] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Health = Math.Min(stats.MaxHealth, stats.Health + amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_DamagePlayer] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Health = Math.Max(0, stats.Health - amount);
                if (stats.Health <= 0)
                {
                    ctx.NextOutputPort = "PlayerDied";
                    OnMessage?.Invoke("[¡El jugador ha muerto!]");
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RestoreMana] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Mana = Math.Min(stats.MaxMana, stats.Mana + amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_ConsumeMana] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var stats = ctx.GameState.Player.DynamicStats;
                if (stats.Mana >= amount)
                {
                    stats.Mana -= amount;
                }
                else
                {
                    ctx.NextOutputPort = "NotEnough";
                }
                await Task.CompletedTask;
            },

            // === NPC COMBAT ACTIONS ===
            [NodeTypeId.Action_StartCombat] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                if (!string.IsNullOrEmpty(npcId))
                {
                    var npc = ctx.GameState.Npcs.FirstOrDefault(n => n.Id == npcId);
                    if (npc != null && !npc.IsCorpse)
                    {
                        OnStartCombat?.Invoke(npcId);
                    }
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_DamageNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n => n.Id == npcId);
                if (npc != null)
                {
                    npc.Stats.CurrentHealth = Math.Max(0, npc.Stats.CurrentHealth - amount);
                    if (npc.Stats.CurrentHealth <= 0)
                    {
                        npc.IsCorpse = true;
                        npc.IsPatrolling = false;
                        npc.IsFollowingPlayer = false;
                        ctx.NextOutputPort = "OnDeath";
                    }
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_HealNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n => n.Id == npcId);
                if (npc != null)
                {
                    npc.Stats.CurrentHealth = Math.Min(npc.Stats.MaxHealth, npc.Stats.CurrentHealth + amount);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetNpcMaxHealth] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var maxHealth = GetPropertyValue<int>(node, "MaxHealth", 100);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.Stats.MaxHealth = maxHealth;
                    if (npc.Stats.CurrentHealth > maxHealth)
                        npc.Stats.CurrentHealth = maxHealth;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_ReviveNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var healthPercent = GetPropertyValue<int>(node, "HealthPercent", 100);

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.IsCorpse = false;
                    npc.Stats.CurrentHealth = (int)(npc.Stats.MaxHealth * healthPercent / 100.0);
                    if (npc.Stats.CurrentHealth < 1) npc.Stats.CurrentHealth = 1;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_KillNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.Stats.CurrentHealth = 0;
                    npc.IsCorpse = true;
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetPatrolRoute] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var routeStr = GetPropertyValue<string>(node, "Route", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    // La ruta viene como IDs separados por coma
                    npc.PatrolRoute = string.IsNullOrEmpty(routeStr)
                        ? new List<string>()
                        : routeStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .ToList();
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_AddItemToNpcInventory] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(objectId))
                {
                    var existingItem = npc.Inventory.FirstOrDefault(i =>
                        string.Equals(i.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
                    if (existingItem != null)
                        existingItem.Quantity++;
                    else
                        npc.Inventory.Add(new InventoryItem { ObjectId = objectId, Quantity = 1 });
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_RemoveItemFromNpcInventory] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    var item = npc.Inventory.FirstOrDefault(i =>
                        string.Equals(i.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                    {
                        if (item.Quantity > 1)
                            item.Quantity--;
                        else
                            npc.Inventory.Remove(item);
                    }
                }
                await Task.CompletedTask;
            },

            // === ACCIONES DE COMBATE ADICIONALES ===
            [NodeTypeId.Action_SetPlayerMaxHealth] = async (node, ctx) =>
            {
                var maxHealth = GetPropertyValue<int>(node, "MaxHealth", 100);
                ctx.GameState.Player.DynamicStats.MaxHealth = maxHealth;
                if (ctx.GameState.Player.DynamicStats.Health > maxHealth)
                    ctx.GameState.Player.DynamicStats.Health = maxHealth;
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_SetNpcAttack] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var attack = GetPropertyValue<int>(node, "Attack", 10);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    // Use Strength as attack stat
                    npc.Stats.Strength = attack;
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_SetNpcDefense] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var defense = GetPropertyValue<int>(node, "Defense", 5);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    // Use Dexterity as defense stat
                    npc.Stats.Dexterity = defense;
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_EndCombatVictory] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat end is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_EndCombatDefeat] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat end is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_ForceFlee] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat flee is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },

            // === ACCIONES DE COMERCIO ===
            [NodeTypeId.Action_OpenTrade] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && npc.IsShopkeeper && !string.IsNullOrEmpty(npcId))
                {
                    // Trigger trade open event - the actual TradeWindow is opened via GameEngine
                    OnStartTrade?.Invoke(npcId);
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_CloseTrade] = async (node, ctx) =>
            {
                // Trade close is handled externally by TradeEngine/TradeWindow
                // This action just signals intent - actual close happens in UI layer
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_AddPlayerMoney] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                ctx.GameState.Player.Money += amount;
                if (amount > 0)
                {
                    await TriggerEventAsync("Game", ctx.World.Game?.Id ?? "game", "Event_OnMoneyGained");
                }
            },
            [NodeTypeId.Action_RemovePlayerMoney] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                if (ctx.GameState.Player.Money >= amount)
                {
                    ctx.GameState.Player.Money -= amount;
                    await TriggerEventAsync("Game", ctx.World.Game?.Id ?? "game", "Event_OnMoneyLost");
                }
                else
                {
                    ctx.NextOutputPort = "OnInsufficient";
                }
            },
            [NodeTypeId.Action_SetNpcMoney] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var money = GetPropertyValue<int>(node, "Money", -1);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.Money = money;
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_AddNpcItem] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var quantity = GetPropertyValue<int>(node, "Quantity", -1);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(objectId) &&
                    !npc.ShopInventory.Any(si => string.Equals(si.ObjectId, objectId, StringComparison.OrdinalIgnoreCase)))
                {
                    npc.ShopInventory.Add(new ShopItem { ObjectId = objectId, Quantity = quantity });
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveNpcItem] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(objectId))
                {
                    npc.ShopInventory.RemoveAll(si =>
                        string.Equals(si.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_SetBuyMultiplier] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var multiplier = GetPropertyValue<double>(node, "Multiplier", 0.5);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.BuyPriceMultiplier = multiplier;
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_SetSellMultiplier] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var multiplier = GetPropertyValue<double>(node, "Multiplier", 1.0);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.SellPriceMultiplier = multiplier;
                }
                await Task.CompletedTask;
            },

            // === HABILIDADES DE COMBATE ===
            [NodeTypeId.Action_AddAbility] = async (node, ctx) =>
            {
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                if (!string.IsNullOrEmpty(abilityId) && !ctx.GameState.Player.AbilityIds.Contains(abilityId))
                {
                    ctx.GameState.Player.AbilityIds.Add(abilityId);
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveAbility] = async (node, ctx) =>
            {
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                if (!string.IsNullOrEmpty(abilityId))
                {
                    ctx.GameState.Player.AbilityIds.Remove(abilityId);
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_AddAbilityToNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n => n.Id == npcId);
                if (npc != null && !string.IsNullOrEmpty(abilityId) && !npc.AbilityIds.Contains(abilityId))
                {
                    npc.AbilityIds.Add(abilityId);
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveAbilityFromNpc] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n => n.Id == npcId);
                if (npc != null && !string.IsNullOrEmpty(abilityId))
                {
                    npc.AbilityIds.Remove(abilityId);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_FeedPlayer] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Hunger = Math.Max(0, stats.Hunger - amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_HydratePlayer] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Thirst = Math.Max(0, stats.Thirst - amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RestPlayer] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 50);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Energy = Math.Min(100, stats.Energy + amount);
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RestoreAllStats] = async (node, ctx) =>
            {
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Health = stats.MaxHealth;
                stats.Mana = stats.MaxMana;
                stats.Hunger = 0;
                stats.Thirst = 0;
                stats.Energy = 100;
                stats.Sanity = 100;
                await Task.CompletedTask;
            },

            // === MODIFICADORES TEMPORALES ===
            [NodeTypeId.Action_ApplyModifier] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                var stateTypeStr = GetPropertyValue<string>(node, "StateType", "Health");
                var amount = GetPropertyValue<int>(node, "Amount", 5);
                var durationTypeStr = GetPropertyValue<string>(node, "DurationType", "Turns");
                var duration = GetPropertyValue<int>(node, "Duration", 5);
                var isRecurring = GetPropertyValue<bool>(node, "IsRecurring", true);

                if (!string.IsNullOrEmpty(name) && Enum.TryParse<PlayerStateType>(stateTypeStr, out var stateType))
                {
                    var durationType = durationTypeStr switch
                    {
                        "Seconds" => ModifierDurationType.Seconds,
                        "Permanent" => ModifierDurationType.Permanent,
                        _ => ModifierDurationType.Turns
                    };

                    var modifier = new TemporaryModifier
                    {
                        Name = name,
                        StateType = stateType,
                        Amount = amount,
                        DurationType = durationType,
                        RemainingDuration = duration,
                        IsRecurring = isRecurring,
                        AppliedAt = DateTime.UtcNow
                    };

                    ctx.GameState.ActiveModifiers.Add(modifier);
                    DebugMessage($"[Debug] Modificador '{name}' aplicado: {stateType} {(amount >= 0 ? "+" : "")}{amount} durante {duration} {durationTypeStr}");
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveModifier] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                if (!string.IsNullOrEmpty(name))
                {
                    ctx.GameState.ActiveModifiers.RemoveAll(m =>
                        string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                    DebugMessage($"[Debug] Modificador '{name}' eliminado");
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveModifiersByState] = async (node, ctx) =>
            {
                var stateTypeStr = GetPropertyValue<string>(node, "StateType", "Health");
                if (Enum.TryParse<PlayerStateType>(stateTypeStr, out var stateType))
                {
                    ctx.GameState.ActiveModifiers.RemoveAll(m => m.StateType == stateType);
                    DebugMessage($"[Debug] Modificadores de {stateType} eliminados");
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_RemoveAllModifiers] = async (node, ctx) =>
            {
                ctx.GameState.ActiveModifiers.Clear();
                DebugMessage("[Debug] Todos los modificadores eliminados");
                await Task.CompletedTask;
            },
            [NodeTypeId.Action_ProcessModifiers] = async (node, ctx) =>
            {
                var playerDied = false;
                var expiredModifiers = new List<TemporaryModifier>();

                foreach (var modifier in ctx.GameState.ActiveModifiers.ToList())
                {
                    if (modifier.IsExpired)
                    {
                        expiredModifiers.Add(modifier);
                        continue;
                    }

                    // Aplicar efecto recurrente
                    if (modifier.IsRecurring)
                    {
                        var current = GetPlayerStateValue(ctx.GameState, modifier.StateType.ToString());
                        SetPlayerStateValue(ctx.GameState, modifier.StateType.ToString(), current + modifier.Amount);

                        // Verificar muerte
                        if (modifier.StateType == PlayerStateType.Health && ctx.GameState.Player.DynamicStats.Health <= 0)
                        {
                            playerDied = true;
                        }
                    }

                    // Decrementar duración para modificadores por turnos
                    if (modifier.DurationType == ModifierDurationType.Turns)
                    {
                        modifier.RemainingDuration--;
                    }
                }

                // Eliminar expirados
                foreach (var expired in expiredModifiers)
                {
                    ctx.GameState.ActiveModifiers.Remove(expired);
                    DebugMessage($"[Debug] Modificador '{expired.Name}' expiró");
                }

                if (playerDied)
                {
                    ctx.NextOutputPort = "PlayerDied";
                    OnMessage?.Invoke("[¡El jugador ha muerto!]");
                }
                await Task.CompletedTask;
            },

            // === EVENTOS DE ESTADOS (entry points) ===
            [NodeTypeId.Event_OnPlayerDeath] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHealthLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHealthCritical] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnHungerHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnThirstHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnEnergyLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnSleepHigh] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnSleep] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnWakeUp] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnWakeUpStartled] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnEat] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnDrink] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnSanityLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnManaLow] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnStateThreshold] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnModifierApplied] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnModifierExpired] = async (node, ctx) => { await Task.CompletedTask; },

            // === EVENTOS DE COMBATE (entry points) ===
            [NodeTypeId.Event_OnCombatStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatVictory] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatDefeat] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCombatFlee] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnPlayerAttack] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnNpcTurn] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnPlayerDefend] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnCriticalHit] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnMiss] = async (node, ctx) => { await Task.CompletedTask; },

            // === EVENTOS DE COMERCIO (entry points) ===
            [NodeTypeId.Event_OnTradeStart] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnTradeEnd] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnItemBought] = async (node, ctx) => { await Task.CompletedTask; },
            [NodeTypeId.Event_OnItemSold] = async (node, ctx) => { await Task.CompletedTask; },

            // === VELOCIDAD DE NECESIDADES ===
            [NodeTypeId.Action_SetNeedRate] = async (node, ctx) =>
            {
                var needType = GetPropertyValue<string>(node, "NeedType", "Hunger");
                var rateStr = GetPropertyValue<string>(node, "Rate", "Normal");

                if (Enum.TryParse<NeedRate>(rateStr, out var rate))
                {
                    switch (needType)
                    {
                        case "Hunger":
                            ctx.World.Game.HungerRate = rate;
                            break;
                        case "Thirst":
                            ctx.World.Game.ThirstRate = rate;
                            break;
                        case "Sleep":
                            ctx.World.Game.SleepRate = rate;
                            break;
                    }
                    DebugMessage($"[Debug] Velocidad de {needType} cambiada a {rateStr}");
                }
                await Task.CompletedTask;
            },
            [NodeTypeId.Variable_GetNeedRate] = async (node, ctx) =>
            {
                var needType = GetPropertyValue<string>(node, "NeedType", "Hunger");
                var rate = needType switch
                {
                    "Hunger" => ctx.World.Game.HungerRate,
                    "Thirst" => ctx.World.Game.ThirstRate,
                    "Sleep" => ctx.World.Game.SleepRate,
                    _ => NeedRate.Normal
                };
                ctx.SetOutputValue(node.Id, "Value", (int)rate);
                await Task.CompletedTask;
            },

            // === ACCESO GENÉRICO A PROPIEDADES ===
            [NodeTypeId.Condition_CompareProperty] = async (node, ctx) =>
            {
                var entityType = GetPropertyValue<string>(node, "EntityType", "");
                var entityId = GetPropertyValue<string>(node, "EntityId", "");
                var propertyName = GetPropertyValue<string>(node, "PropertyName", "");
                var op = GetPropertyValue<string>(node, "Operator", "==");
                var compareValue = GetPropertyValue<string>(node, "CompareValue", "");

                var currentValue = GetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "");
                var result = CompareValues(currentValue, op ?? "==", compareValue ?? "");

                ctx.NextOutputPort = result ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_SetProperty] = async (node, ctx) =>
            {
                var entityType = GetPropertyValue<string>(node, "EntityType", "");
                var entityId = GetPropertyValue<string>(node, "EntityId", "");
                var propertyName = GetPropertyValue<string>(node, "PropertyName", "");
                var value = GetPropertyValue<string>(node, "Value", "");

                var oldValue = GetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "");
                var success = SetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "", value);

                if (success)
                {
                    var newValue = GetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "");
                    // Si el valor cambió, podemos usar puerto OnChanged
                    var changed = !Equals(oldValue, newValue);
                    if (changed)
                    {
                        // Disparar evento de cambio de propiedad
                        await TriggerPropertyChangedEvent(ctx, entityType ?? "", entityId ?? "", propertyName ?? "", oldValue, newValue);
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_ModifyProperty] = async (node, ctx) =>
            {
                var entityType = GetPropertyValue<string>(node, "EntityType", "");
                var entityId = GetPropertyValue<string>(node, "EntityId", "");
                var propertyName = GetPropertyValue<string>(node, "PropertyName", "");
                var operation = GetPropertyValue<string>(node, "Operation", "Add");
                var amount = GetPropertyValue<double>(node, "Amount", 0);

                var currentValue = GetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "");
                if (currentValue != null)
                {
                    var currentNum = Convert.ToDouble(currentValue);
                    var newNum = operation switch
                    {
                        "Add" => currentNum + amount,
                        "Subtract" => currentNum - amount,
                        "Multiply" => currentNum * amount,
                        "Divide" when amount != 0 => currentNum / amount,
                        _ => currentNum
                    };

                    // Determinar si es int o double
                    object newValue = currentValue is int || currentValue is long
                        ? (int)Math.Round(newNum)
                        : newNum;

                    var oldValue = currentValue;
                    SetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "", newValue);

                    // Disparar evento de cambio
                    await TriggerPropertyChangedEvent(ctx, entityType ?? "", entityId ?? "", propertyName ?? "", oldValue, newValue);
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Variable_GetProperty] = async (node, ctx) =>
            {
                var entityType = GetPropertyValue<string>(node, "EntityType", "");
                var entityId = GetPropertyValue<string>(node, "EntityId", "");
                var propertyName = GetPropertyValue<string>(node, "PropertyName", "");

                var value = GetEntityPropertyValue(ctx, entityType ?? "", entityId ?? "", propertyName ?? "");
                ctx.SetOutputValue(node.Id, "Value", value);
                await Task.CompletedTask;
            },

            // === EQUIPAMIENTO E INVENTARIO ===
            [NodeTypeId.Condition_PlayerHasEquipped] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "Any");

                bool hasEquipped = false;
                if (!string.IsNullOrEmpty(objectId))
                {
                    var checkSlot = (string slotToCheck) =>
                    {
                        var equippedId = slotToCheck switch
                        {
                            "RightHand" => ctx.GameState.Player.EquippedRightHandId,
                            "LeftHand" => ctx.GameState.Player.EquippedLeftHandId,
                            "Torso" => ctx.GameState.Player.EquippedTorsoId,
                            "Head" => ctx.GameState.Player.EquippedHeadId,
                            _ => null
                        };
                        return string.Equals(equippedId, objectId, StringComparison.OrdinalIgnoreCase);
                    };

                    hasEquipped = slot == "Any"
                        ? checkSlot("RightHand") || checkSlot("LeftHand") || checkSlot("Torso") || checkSlot("Head")
                        : checkSlot(slot ?? "Any");
                }
                ctx.NextOutputPort = hasEquipped ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcHasEquipped] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "Any");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                bool hasEquipped = false;
                if (npc != null && !string.IsNullOrEmpty(objectId))
                {
                    var checkSlot = (string slotToCheck) =>
                    {
                        var equippedId = slotToCheck switch
                        {
                            "RightHand" => npc.EquippedRightHandId,
                            "LeftHand" => npc.EquippedLeftHandId,
                            "Torso" => npc.EquippedTorsoId,
                            "Head" => npc.EquippedHeadId,
                            _ => null
                        };
                        return string.Equals(equippedId, objectId, StringComparison.OrdinalIgnoreCase);
                    };

                    hasEquipped = slot == "Any"
                        ? checkSlot("RightHand") || checkSlot("LeftHand") || checkSlot("Torso") || checkSlot("Head")
                        : checkSlot(slot ?? "Any");
                }
                ctx.NextOutputPort = hasEquipped ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsPlayerSlotEmpty] = async (node, ctx) =>
            {
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                var equippedId = slot switch
                {
                    "RightHand" => ctx.GameState.Player.EquippedRightHandId,
                    "LeftHand" => ctx.GameState.Player.EquippedLeftHandId,
                    "Torso" => ctx.GameState.Player.EquippedTorsoId,
                    "Head" => ctx.GameState.Player.EquippedHeadId,
                    _ => null
                };

                ctx.NextOutputPort = string.IsNullOrEmpty(equippedId) ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_IsNpcSlotEmpty] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                bool isEmpty = true;
                if (npc != null)
                {
                    var equippedId = slot switch
                    {
                        "RightHand" => npc.EquippedRightHandId,
                        "LeftHand" => npc.EquippedLeftHandId,
                        "Torso" => npc.EquippedTorsoId,
                        "Head" => npc.EquippedHeadId,
                        _ => null
                    };
                    isEmpty = string.IsNullOrEmpty(equippedId);
                }
                ctx.NextOutputPort = isEmpty ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Condition_NpcHasItem] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                bool hasItem = npc?.Inventory.Any(i =>
                    string.Equals(i.ObjectId, objectId, StringComparison.OrdinalIgnoreCase) &&
                    i.Quantity > 0) ?? false;

                ctx.NextOutputPort = hasItem ? "True" : "False";
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_EquipPlayerItem] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                if (!string.IsNullOrEmpty(objectId))
                {
                    switch (slot)
                    {
                        case "RightHand":
                            ctx.GameState.Player.EquippedRightHandId = objectId;
                            break;
                        case "LeftHand":
                            ctx.GameState.Player.EquippedLeftHandId = objectId;
                            break;
                        case "Torso":
                            ctx.GameState.Player.EquippedTorsoId = objectId;
                            break;
                        case "Head":
                            ctx.GameState.Player.EquippedHeadId = objectId;
                            break;
                    }
                    // Quitar del inventario si estaba ahí
                    ctx.GameState.InventoryObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_UnequipPlayerSlot] = async (node, ctx) =>
            {
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                string? equippedId = slot switch
                {
                    "RightHand" => ctx.GameState.Player.EquippedRightHandId,
                    "LeftHand" => ctx.GameState.Player.EquippedLeftHandId,
                    "Torso" => ctx.GameState.Player.EquippedTorsoId,
                    "Head" => ctx.GameState.Player.EquippedHeadId,
                    _ => null
                };

                if (!string.IsNullOrEmpty(equippedId))
                {
                    // Devolver al inventario
                    if (!ctx.GameState.InventoryObjectIds.Any(id =>
                        string.Equals(id, equippedId, StringComparison.OrdinalIgnoreCase)))
                    {
                        ctx.GameState.InventoryObjectIds.Add(equippedId);
                    }

                    // Limpiar slot
                    switch (slot)
                    {
                        case "RightHand":
                            ctx.GameState.Player.EquippedRightHandId = null;
                            break;
                        case "LeftHand":
                            ctx.GameState.Player.EquippedLeftHandId = null;
                            break;
                        case "Torso":
                            ctx.GameState.Player.EquippedTorsoId = null;
                            break;
                        case "Head":
                            ctx.GameState.Player.EquippedHeadId = null;
                            break;
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_EquipNpcItem] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                if (npc != null && !string.IsNullOrEmpty(objectId))
                {
                    switch (slot)
                    {
                        case "RightHand":
                            npc.EquippedRightHandId = objectId;
                            break;
                        case "LeftHand":
                            npc.EquippedLeftHandId = objectId;
                            break;
                        case "Torso":
                            npc.EquippedTorsoId = objectId;
                            break;
                        case "Head":
                            npc.EquippedHeadId = objectId;
                            break;
                    }

                    // Quitar del inventario del NPC si estaba ahí
                    var item = npc.Inventory.FirstOrDefault(i =>
                        string.Equals(i.ObjectId, objectId, StringComparison.OrdinalIgnoreCase));
                    if (item != null)
                    {
                        if (item.Quantity > 1)
                            item.Quantity--;
                        else
                            npc.Inventory.Remove(item);
                    }
                }
                await Task.CompletedTask;
            },

            [NodeTypeId.Action_UnequipNpcSlot] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var slot = GetPropertyValue<string>(node, "Slot", "RightHand");

                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));

                if (npc != null)
                {
                    string? equippedId = slot switch
                    {
                        "RightHand" => npc.EquippedRightHandId,
                        "LeftHand" => npc.EquippedLeftHandId,
                        "Torso" => npc.EquippedTorsoId,
                        "Head" => npc.EquippedHeadId,
                        _ => null
                    };

                    if (!string.IsNullOrEmpty(equippedId))
                    {
                        // Devolver al inventario del NPC
                        var existingItem = npc.Inventory.FirstOrDefault(i =>
                            string.Equals(i.ObjectId, equippedId, StringComparison.OrdinalIgnoreCase));
                        if (existingItem != null)
                            existingItem.Quantity++;
                        else
                            npc.Inventory.Add(new InventoryItem { ObjectId = equippedId, Quantity = 1 });

                        // Limpiar slot
                        switch (slot)
                        {
                            case "RightHand":
                                npc.EquippedRightHandId = null;
                                break;
                            case "LeftHand":
                                npc.EquippedLeftHandId = null;
                                break;
                            case "Torso":
                                npc.EquippedTorsoId = null;
                                break;
                            case "Head":
                                npc.EquippedHeadId = null;
                                break;
                        }
                    }
                }
                await Task.CompletedTask;
            },

            // Evento de cambio de propiedad (entry point)
            [NodeTypeId.Event_OnPropertyChanged] = async (node, ctx) => { await Task.CompletedTask; }
        };
    }

    /// <summary>
    /// Dispara el evento de cambio de propiedad para los scripts que lo escuchen.
    /// </summary>
    private async Task TriggerPropertyChangedEvent(ScriptContext ctx, string entityType, string entityId, string propertyName, object? oldValue, object? newValue)
    {
        // Buscar scripts que tengan Event_OnPropertyChanged para este tipo de entidad y propiedad
        var scripts = _world.Scripts.Where(s =>
            s.Nodes.Any(n =>
                n.NodeType == NodeTypeId.Event_OnPropertyChanged &&
                string.Equals(n.Properties.TryGetValue("EntityType", out var et) ? et?.ToString() : "", entityType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(n.Properties.TryGetValue("PropertyName", out var pn) ? pn?.ToString() : "", propertyName, StringComparison.OrdinalIgnoreCase)
            )).ToList();

        foreach (var script in scripts)
        {
            var eventNode = script.Nodes.FirstOrDefault(n =>
                n.NodeType == NodeTypeId.Event_OnPropertyChanged &&
                string.Equals(n.Properties.TryGetValue("EntityType", out var et) ? et?.ToString() : "", entityType, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(n.Properties.TryGetValue("PropertyName", out var pn) ? pn?.ToString() : "", propertyName, StringComparison.OrdinalIgnoreCase));

            if (eventNode != null)
            {
                var newContext = new ScriptContext(script, _gameState, _world);
                // Establecer valores de salida del evento
                newContext.SetOutputValue(eventNode.Id, "EntityId", entityId);
                newContext.SetOutputValue(eventNode.Id, "OldValue", oldValue?.ToString() ?? "");
                newContext.SetOutputValue(eventNode.Id, "NewValue", newValue?.ToString() ?? "");

                await ExecuteFromNodeAsync(eventNode, "Exec", newContext);
            }
        }
    }

    /// <summary>
    /// Obtiene el valor de un estado del jugador por nombre.
    /// </summary>
    private int GetPlayerStateValue(GameState gameState, string stateType)
    {
        return stateType switch
        {
            "Health" => gameState.Player.DynamicStats.Health,
            "MaxHealth" => gameState.Player.DynamicStats.MaxHealth,
            "Hunger" => gameState.Player.DynamicStats.Hunger,
            "Thirst" => gameState.Player.DynamicStats.Thirst,
            "Energy" => gameState.Player.DynamicStats.Energy,
            "Sleep" => gameState.Player.DynamicStats.Sleep,
            "Sanity" => gameState.Player.DynamicStats.Sanity,
            "Mana" => gameState.Player.DynamicStats.Mana,
            "MaxMana" => gameState.Player.DynamicStats.MaxMana,
            "Strength" => gameState.Player.Strength,
            "Constitution" => gameState.Player.Constitution,
            "Intelligence" => gameState.Player.Intelligence,
            "Dexterity" => gameState.Player.Dexterity,
            "Charisma" => gameState.Player.Charisma,
            "Money" => gameState.Player.Money,
            _ => 0
        };
    }

    /// <summary>
    /// Establece el valor de un estado del jugador por nombre.
    /// </summary>
    private void SetPlayerStateValue(GameState gameState, string stateType, int value)
    {
        switch (stateType)
        {
            case "Health":
                gameState.Player.DynamicStats.Health = Math.Clamp(value, 0, gameState.Player.DynamicStats.MaxHealth);
                break;
            case "MaxHealth":
                gameState.Player.DynamicStats.MaxHealth = Math.Max(1, value);
                break;
            case "Hunger":
                gameState.Player.DynamicStats.Hunger = Math.Clamp(value, 0, 100);
                break;
            case "Thirst":
                gameState.Player.DynamicStats.Thirst = Math.Clamp(value, 0, 100);
                break;
            case "Energy":
                gameState.Player.DynamicStats.Energy = Math.Clamp(value, 0, 100);
                break;
            case "Sleep":
                gameState.Player.DynamicStats.Sleep = Math.Clamp(value, 0, 100);
                break;
            case "Sanity":
                gameState.Player.DynamicStats.Sanity = Math.Clamp(value, 0, 100);
                break;
            case "Mana":
                gameState.Player.DynamicStats.Mana = Math.Clamp(value, 0, gameState.Player.DynamicStats.MaxMana);
                break;
            case "MaxMana":
                gameState.Player.DynamicStats.MaxMana = Math.Max(0, value);
                break;
            case "Strength":
                gameState.Player.Strength = Math.Max(0, value);
                break;
            case "Constitution":
                gameState.Player.Constitution = Math.Max(0, value);
                break;
            case "Intelligence":
                gameState.Player.Intelligence = Math.Max(0, value);
                break;
            case "Dexterity":
                gameState.Player.Dexterity = Math.Max(0, value);
                break;
            case "Charisma":
                gameState.Player.Charisma = Math.Max(0, value);
                break;
            case "Money":
                gameState.Player.Money = Math.Max(0, value);
                break;
        }
    }

    #region Property Access Helper

    /// <summary>
    /// Diccionario de propiedades accesibles por tipo de entidad.
    /// </summary>
    private static readonly Dictionary<string, string[]> AccessibleProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Room"] = new[] { "Name", "Description", "IsInterior", "IsIlluminated", "MusicId" },
        ["Door"] = new[] { "Name", "Description", "IsOpen", "IsLocked", "Visible", "KeyObjectId" },
        ["Npc"] = new[] { "Name", "Description", "RoomId", "Visible", "IsPatrolling", "IsFollowingPlayer", "Money", "IsCorpse", "IsShopkeeper", "CurrentHealth", "MaxHealth" },
        ["GameObject"] = new[] { "Name", "Description", "RoomId", "Visible", "CanTake", "IsOpen", "IsLocked", "Price", "Weight", "Volume", "AttackBonus", "DefenseBonus", "IsLit" },
        ["Player"] = new[] { "Name", "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money", "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sleep", "Sanity", "Mana", "MaxMana" },
        ["Game"] = new[] { "Weather", "Title", "GameHour", "GameMinute", "TurnCounter" }
    };

    /// <summary>
    /// Obtiene las propiedades accesibles para un tipo de entidad.
    /// </summary>
    public static string[] GetAccessibleProperties(string entityType)
    {
        return AccessibleProperties.TryGetValue(entityType, out var props) ? props : Array.Empty<string>();
    }

    /// <summary>
    /// Resuelve una entidad por tipo e ID.
    /// </summary>
    private object? ResolveEntity(ScriptContext ctx, string entityType, string entityId)
    {
        return entityType.ToLowerInvariant() switch
        {
            "room" => ctx.GameState.Rooms.FirstOrDefault(r => string.Equals(r.Id, entityId, StringComparison.OrdinalIgnoreCase)),
            "door" => ctx.GameState.Doors.FirstOrDefault(d => string.Equals(d.Id, entityId, StringComparison.OrdinalIgnoreCase)),
            "npc" => ctx.GameState.Npcs.FirstOrDefault(n => string.Equals(n.Id, entityId, StringComparison.OrdinalIgnoreCase)),
            "gameobject" => ctx.GameState.Objects.FirstOrDefault(o => string.Equals(o.Id, entityId, StringComparison.OrdinalIgnoreCase)),
            "player" => ctx.GameState.Player,
            "game" => ctx.World.Game,
            _ => null
        };
    }

    /// <summary>
    /// Obtiene el valor de una propiedad de una entidad.
    /// </summary>
    private object? GetEntityPropertyValue(ScriptContext ctx, string entityType, string entityId, string propertyName)
    {
        var entity = ResolveEntity(ctx, entityType, entityId);
        if (entity == null) return null;

        return entityType.ToLowerInvariant() switch
        {
            "room" => GetRoomPropertyValue((Room)entity, propertyName),
            "door" => GetDoorPropertyValue((Door)entity, propertyName),
            "npc" => GetNpcPropertyValue((Npc)entity, propertyName),
            "gameobject" => GetGameObjectPropertyValue((GameObject)entity, propertyName),
            "player" => GetPlayerPropertyValue(ctx.GameState, propertyName),
            "game" => GetGamePropertyValue(ctx, propertyName),
            _ => null
        };
    }

    /// <summary>
    /// Establece el valor de una propiedad de una entidad.
    /// </summary>
    private bool SetEntityPropertyValue(ScriptContext ctx, string entityType, string entityId, string propertyName, object? value)
    {
        var entity = ResolveEntity(ctx, entityType, entityId);
        if (entity == null) return false;

        return entityType.ToLowerInvariant() switch
        {
            "room" => SetRoomPropertyValue((Room)entity, propertyName, value),
            "door" => SetDoorPropertyValue((Door)entity, propertyName, value),
            "npc" => SetNpcPropertyValue((Npc)entity, propertyName, value),
            "gameobject" => SetGameObjectPropertyValue((GameObject)entity, propertyName, value),
            "player" => SetPlayerPropertyValue(ctx.GameState, propertyName, value),
            "game" => SetGamePropertyValue(ctx, propertyName, value),
            _ => false
        };
    }

    private object? GetRoomPropertyValue(Room room, string propertyName)
    {
        return propertyName switch
        {
            "Name" => room.Name,
            "Description" => room.Description,
            "IsInterior" => room.IsInterior,
            "IsIlluminated" => room.IsIlluminated,
            "MusicId" => room.MusicId,
            _ => null
        };
    }

    private bool SetRoomPropertyValue(Room room, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Name": room.Name = value?.ToString() ?? ""; return true;
            case "Description": room.Description = value?.ToString() ?? ""; return true;
            case "IsInterior": room.IsInterior = ConvertToBool(value); return true;
            case "IsIlluminated": room.IsIlluminated = ConvertToBool(value); return true;
            case "MusicId": room.MusicId = value?.ToString(); return true;
            default: return false;
        }
    }

    private object? GetDoorPropertyValue(Door door, string propertyName)
    {
        return propertyName switch
        {
            "Name" => door.Name,
            "Description" => door.Description,
            "IsOpen" => door.IsOpen,
            "IsLocked" => door.IsLocked,
            "Visible" => door.Visible,
            "KeyObjectId" => door.KeyObjectId,
            _ => null
        };
    }

    private bool SetDoorPropertyValue(Door door, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Name": door.Name = value?.ToString() ?? ""; return true;
            case "Description": door.Description = value?.ToString() ?? ""; return true;
            case "IsOpen": door.IsOpen = ConvertToBool(value); return true;
            case "IsLocked": door.IsLocked = ConvertToBool(value); return true;
            case "Visible": door.Visible = ConvertToBool(value); return true;
            case "KeyObjectId": door.KeyObjectId = value?.ToString(); return true;
            default: return false;
        }
    }

    private object? GetNpcPropertyValue(Npc npc, string propertyName)
    {
        return propertyName switch
        {
            "Name" => npc.Name,
            "Description" => npc.Description,
            "RoomId" => npc.RoomId,
            "Visible" => npc.Visible,
            "IsPatrolling" => npc.IsPatrolling,
            "IsFollowingPlayer" => npc.IsFollowingPlayer,
            "Money" => npc.Money,
            "IsCorpse" => npc.IsCorpse,
            "IsShopkeeper" => npc.IsShopkeeper,
            "CurrentHealth" => npc.Stats.CurrentHealth,
            "MaxHealth" => npc.Stats.MaxHealth,
            _ => null
        };
    }

    private bool SetNpcPropertyValue(Npc npc, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Name": npc.Name = value?.ToString() ?? ""; return true;
            case "Description": npc.Description = value?.ToString() ?? ""; return true;
            case "RoomId": npc.RoomId = value?.ToString() ?? ""; return true;
            case "Visible": npc.Visible = ConvertToBool(value); return true;
            case "IsPatrolling": npc.IsPatrolling = ConvertToBool(value); return true;
            case "IsFollowingPlayer": npc.IsFollowingPlayer = ConvertToBool(value); return true;
            case "Money": npc.Money = ConvertToInt(value); return true;
            case "IsCorpse": npc.IsCorpse = ConvertToBool(value); return true;
            case "IsShopkeeper": npc.IsShopkeeper = ConvertToBool(value); return true;
            case "CurrentHealth": npc.Stats.CurrentHealth = ConvertToInt(value); return true;
            case "MaxHealth": npc.Stats.MaxHealth = ConvertToInt(value); return true;
            default: return false;
        }
    }

    private object? GetGameObjectPropertyValue(GameObject obj, string propertyName)
    {
        return propertyName switch
        {
            "Name" => obj.Name,
            "Description" => obj.Description,
            "RoomId" => obj.RoomId,
            "Visible" => obj.Visible,
            "CanTake" => obj.CanTake,
            "IsOpen" => obj.IsOpen,
            "IsLocked" => obj.IsLocked,
            "Price" => obj.Price,
            "Weight" => obj.Weight,
            "Volume" => obj.Volume,
            "AttackBonus" => obj.AttackBonus,
            "DefenseBonus" => obj.DefenseBonus,
            "IsLit" => obj.IsLit,
            _ => null
        };
    }

    private bool SetGameObjectPropertyValue(GameObject obj, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Name": obj.Name = value?.ToString() ?? ""; return true;
            case "Description": obj.Description = value?.ToString() ?? ""; return true;
            case "RoomId": obj.RoomId = value?.ToString(); return true;
            case "Visible": obj.Visible = ConvertToBool(value); return true;
            case "CanTake": obj.CanTake = ConvertToBool(value); return true;
            case "IsOpen": obj.IsOpen = ConvertToBool(value); return true;
            case "IsLocked": obj.IsLocked = ConvertToBool(value); return true;
            case "Price": obj.Price = ConvertToInt(value); return true;
            case "Weight": obj.Weight = ConvertToInt(value); return true;
            case "Volume": obj.Volume = ConvertToDouble(value); return true;
            case "AttackBonus": obj.AttackBonus = ConvertToInt(value); return true;
            case "DefenseBonus": obj.DefenseBonus = ConvertToInt(value); return true;
            case "IsLit": obj.IsLit = ConvertToBool(value); return true;
            default: return false;
        }
    }

    private object? GetPlayerPropertyValue(GameState gameState, string propertyName)
    {
        return propertyName switch
        {
            "Name" => gameState.Player.Name,
            "Strength" => gameState.Player.Strength,
            "Constitution" => gameState.Player.Constitution,
            "Intelligence" => gameState.Player.Intelligence,
            "Dexterity" => gameState.Player.Dexterity,
            "Charisma" => gameState.Player.Charisma,
            "Money" => gameState.Player.Money,
            "Health" => gameState.Player.DynamicStats.Health,
            "MaxHealth" => gameState.Player.DynamicStats.MaxHealth,
            "Hunger" => gameState.Player.DynamicStats.Hunger,
            "Thirst" => gameState.Player.DynamicStats.Thirst,
            "Energy" => gameState.Player.DynamicStats.Energy,
            "Sleep" => gameState.Player.DynamicStats.Sleep,
            "Sanity" => gameState.Player.DynamicStats.Sanity,
            "Mana" => gameState.Player.DynamicStats.Mana,
            "MaxMana" => gameState.Player.DynamicStats.MaxMana,
            _ => null
        };
    }

    private bool SetPlayerPropertyValue(GameState gameState, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Name": gameState.Player.Name = value?.ToString() ?? ""; return true;
            case "Strength": gameState.Player.Strength = ConvertToInt(value); return true;
            case "Constitution": gameState.Player.Constitution = ConvertToInt(value); return true;
            case "Intelligence": gameState.Player.Intelligence = ConvertToInt(value); return true;
            case "Dexterity": gameState.Player.Dexterity = ConvertToInt(value); return true;
            case "Charisma": gameState.Player.Charisma = ConvertToInt(value); return true;
            case "Money": gameState.Player.Money = ConvertToInt(value); return true;
            case "Health": gameState.Player.DynamicStats.Health = Math.Clamp(ConvertToInt(value), 0, gameState.Player.DynamicStats.MaxHealth); return true;
            case "MaxHealth": gameState.Player.DynamicStats.MaxHealth = Math.Max(1, ConvertToInt(value)); return true;
            case "Hunger": gameState.Player.DynamicStats.Hunger = Math.Clamp(ConvertToInt(value), 0, 100); return true;
            case "Thirst": gameState.Player.DynamicStats.Thirst = Math.Clamp(ConvertToInt(value), 0, 100); return true;
            case "Energy": gameState.Player.DynamicStats.Energy = Math.Clamp(ConvertToInt(value), 0, 100); return true;
            case "Sleep": gameState.Player.DynamicStats.Sleep = Math.Clamp(ConvertToInt(value), 0, 100); return true;
            case "Sanity": gameState.Player.DynamicStats.Sanity = Math.Clamp(ConvertToInt(value), 0, 100); return true;
            case "Mana": gameState.Player.DynamicStats.Mana = Math.Clamp(ConvertToInt(value), 0, gameState.Player.DynamicStats.MaxMana); return true;
            case "MaxMana": gameState.Player.DynamicStats.MaxMana = Math.Max(0, ConvertToInt(value)); return true;
            default: return false;
        }
    }

    private object? GetGamePropertyValue(ScriptContext ctx, string propertyName)
    {
        return propertyName switch
        {
            "Weather" => ctx.GameState.Weather.ToString(),
            "Title" => ctx.World.Game?.Title ?? "",
            "GameHour" => ctx.GameState.GameTime.Hour,
            "GameMinute" => ctx.GameState.GameTime.Minute,
            "TurnCounter" => ctx.GameState.TurnCounter,
            _ => null
        };
    }

    private bool SetGamePropertyValue(ScriptContext ctx, string propertyName, object? value)
    {
        switch (propertyName)
        {
            case "Weather":
                if (Enum.TryParse<WeatherType>(value?.ToString() ?? "Despejado", true, out var weather))
                {
                    ctx.GameState.Weather = weather;
                    return true;
                }
                return false;
            case "GameHour":
                var clampedHour = Math.Clamp(ConvertToInt(value), 0, 23);
                var currentTime = ctx.GameState.GameTime;
                ctx.GameState.GameTime = new DateTime(
                    currentTime.Year, currentTime.Month, currentTime.Day,
                    clampedHour, currentTime.Minute, currentTime.Second);
                return true;
            case "GameMinute":
                var clampedMinute = Math.Clamp(ConvertToInt(value), 0, 59);
                var currentTimeForMinute = ctx.GameState.GameTime;
                ctx.GameState.GameTime = new DateTime(
                    currentTimeForMinute.Year, currentTimeForMinute.Month, currentTimeForMinute.Day,
                    currentTimeForMinute.Hour, clampedMinute, currentTimeForMinute.Second);
                return true;
            default:
                return false;
        }
    }

    private static bool ConvertToBool(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is JsonElement je && je.ValueKind == JsonValueKind.True) return true;
        if (value is JsonElement je2 && je2.ValueKind == JsonValueKind.False) return false;
        var str = value.ToString()?.ToLowerInvariant();
        return str == "true" || str == "1" || str == "yes" || str == "si";
    }

    private static int ConvertToInt(object? value)
    {
        if (value == null) return 0;
        if (value is int i) return i;
        if (value is long l) return (int)l;
        if (value is double d) return (int)d;
        if (value is JsonElement je && je.TryGetInt32(out var jint)) return jint;
        if (int.TryParse(value.ToString(), out var parsed)) return parsed;
        return 0;
    }

    private static double ConvertToDouble(object? value)
    {
        if (value == null) return 0;
        if (value is double d) return d;
        if (value is int i) return i;
        if (value is float f) return f;
        if (value is JsonElement je && je.TryGetDouble(out var jd)) return jd;
        if (double.TryParse(value.ToString(), out var parsed)) return parsed;
        return 0;
    }

    /// <summary>
    /// Compara dos valores según un operador.
    /// </summary>
    private static bool CompareValues(object? leftValue, string op, string rightValueStr)
    {
        if (leftValue == null) return false;

        // Para booleanos
        if (leftValue is bool leftBool)
        {
            var rightBool = rightValueStr.ToLowerInvariant() == "true" || rightValueStr == "1" || rightValueStr.ToLowerInvariant() == "yes";
            return op switch
            {
                "==" => leftBool == rightBool,
                "!=" => leftBool != rightBool,
                _ => false
            };
        }

        // Para números
        if (leftValue is int || leftValue is long || leftValue is double || leftValue is float)
        {
            var leftNum = Convert.ToDouble(leftValue);
            if (!double.TryParse(rightValueStr, out var rightNum)) return false;

            return op switch
            {
                "==" => Math.Abs(leftNum - rightNum) < 0.0001,
                "!=" => Math.Abs(leftNum - rightNum) >= 0.0001,
                "<" => leftNum < rightNum,
                "<=" => leftNum <= rightNum,
                ">" => leftNum > rightNum,
                ">=" => leftNum >= rightNum,
                _ => false
            };
        }

        // Para strings
        var leftStr = leftValue.ToString() ?? "";
        return op switch
        {
            "==" => string.Equals(leftStr, rightValueStr, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(leftStr, rightValueStr, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    #endregion
}

/// <summary>
/// Contexto de ejecución de un script.
/// </summary>
public class ScriptContext
{
    public ScriptDefinition Script { get; }
    public GameState GameState { get; }
    public WorldModel World { get; }

    /// <summary>
    /// Puerto de salida a seguir después de ejecutar el nodo actual.
    /// Si es null, se usa "Exec" por defecto.
    /// </summary>
    public string? NextOutputPort { get; set; }

    /// <summary>
    /// Valores de salida de nodos (para nodos de variables).
    /// </summary>
    public Dictionary<string, object?> NodeOutputs { get; } = new();

    public ScriptContext(ScriptDefinition script, GameState gameState, WorldModel world)
    {
        Script = script;
        GameState = gameState;
        World = world;
    }

    public void SetOutputValue(string nodeId, string portName, object? value)
    {
        NodeOutputs[$"{nodeId}.{portName}"] = value;
    }

    public T? GetOutputValue<T>(string nodeId, string portName)
    {
        var key = $"{nodeId}.{portName}";
        return NodeOutputs.TryGetValue(key, out var value) ? (T?)value : default;
    }
}
