using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Motor de ejecución de scripts visuales.
/// </summary>
public class ScriptEngine
{
    private readonly GameState _gameState;
    private readonly WorldModel _world;
    private readonly Dictionary<string, Func<ScriptNode, ScriptContext, Task>> _nodeHandlers;
    private readonly Random _random = new();

    /// <summary>
    /// Evento disparado cuando se debe mostrar un mensaje al jugador.
    /// </summary>
    public event Action<string>? OnMessage;

    /// <summary>
    /// Evento disparado cuando se debe reproducir un sonido.
    /// </summary>
    public event Action<string>? OnPlaySound;

    /// <summary>
    /// Evento disparado cuando el jugador es teletransportado.
    /// </summary>
    public event Action<string>? OnPlayerTeleported;

    /// <summary>
    /// Evento disparado cuando un script quiere iniciar una conversación con un NPC.
    /// </summary>
    public event Action<string>? OnStartConversation;

    public ScriptEngine(WorldModel world, GameState gameState)
    {
        _world = world;
        _gameState = gameState;
        _nodeHandlers = RegisterNodeHandlers();
    }

    /// <summary>
    /// Ejecuta todos los scripts asociados a un evento específico.
    /// </summary>
    /// <param name="ownerType">Tipo de entidad (Room, Npc, GameObject, Door, Quest, Game)</param>
    /// <param name="ownerId">ID de la entidad</param>
    /// <param name="eventType">Tipo de evento (Event_OnEnter, Event_OnTake, etc.)</param>
    public async Task TriggerEventAsync(string ownerType, string ownerId, string eventType)
    {
        var scripts = _world.Scripts.Where(s =>
            string.Equals(s.OwnerType, ownerType, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(s.OwnerId, ownerId, StringComparison.OrdinalIgnoreCase));

        foreach (var script in scripts)
        {
            await ExecuteEventAsync(script, eventType);
        }
    }

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
        var eventNode = script.Nodes.FirstOrDefault(n =>
            string.Equals(n.NodeType, eventType, StringComparison.OrdinalIgnoreCase));
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
    private Dictionary<string, Func<ScriptNode, ScriptContext, Task>> RegisterNodeHandlers()
    {
        return new Dictionary<string, Func<ScriptNode, ScriptContext, Task>>(StringComparer.OrdinalIgnoreCase)
        {
            // === EVENTOS (no hacen nada, solo son entry points) ===
            ["Event_OnGameStart"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnGameEnd"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_EveryMinute"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_EveryHour"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnWeatherChange"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnEnter"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnExit"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnLook"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDoorOpen"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDoorClose"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDoorLock"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDoorUnlock"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDoorKnock"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnTalk"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnNpcAttack"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnNpcDeath"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnNpcSee"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnTake"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnDrop"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnUse"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnExamine"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnContainerOpen"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnContainerClose"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnQuestStart"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnQuestComplete"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnQuestFail"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnObjectiveComplete"] = async (node, ctx) => { await Task.CompletedTask; },

            // === CONDICIONES ===
            ["Condition_HasItem"] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var hasItem = !string.IsNullOrEmpty(objectId) &&
                              ctx.GameState.InventoryObjectIds.Any(id =>
                                  string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                ctx.NextOutputPort = hasItem ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsInRoom"] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                var isInRoom = string.Equals(ctx.GameState.CurrentRoomId, roomId, StringComparison.OrdinalIgnoreCase);
                ctx.NextOutputPort = isInRoom ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsQuestStatus"] = async (node, ctx) =>
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

            ["Condition_HasFlag"] = async (node, ctx) =>
            {
                var flagName = GetPropertyValue<string>(node, "FlagName", "");
                var hasFlag = !string.IsNullOrEmpty(flagName) &&
                              ctx.GameState.Flags.TryGetValue(flagName, out var value) && value;
                ctx.NextOutputPort = hasFlag ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_CompareCounter"] = async (node, ctx) =>
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

            ["Condition_IsTimeOfDay"] = async (node, ctx) =>
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

            ["Condition_IsDoorOpen"] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                var isOpen = door?.IsOpen ?? false;
                ctx.NextOutputPort = isOpen ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsNpcVisible"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isVisible = npc?.Visible ?? false;
                ctx.NextOutputPort = isVisible ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsPatrolling"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isPatrolling = npc?.IsPatrolling ?? false;
                ctx.NextOutputPort = isPatrolling ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsFollowingPlayer"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isFollowing = npc?.IsFollowingPlayer ?? false;
                ctx.NextOutputPort = isFollowing ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_Random"] = async (node, ctx) =>
            {
                var probability = GetPropertyValue<int>(node, "Probability", 50);
                var roll = _random.Next(100);
                ctx.NextOutputPort = roll < probability ? "True" : "False";
                await Task.CompletedTask;
            },

            // === ACCIONES ===
            ["Action_ShowMessage"] = async (node, ctx) =>
            {
                var message = GetPropertyValue<string>(node, "Message", "");
                if (!string.IsNullOrEmpty(message))
                {
                    OnMessage?.Invoke(message);
                }
                await Task.CompletedTask;
            },

            ["Action_GiveItem"] = async (node, ctx) =>
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

            ["Action_RemoveItem"] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                if (!string.IsNullOrEmpty(objectId))
                {
                    ctx.GameState.InventoryObjectIds.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },

            ["Action_TeleportPlayer"] = async (node, ctx) =>
            {
                var roomId = GetPropertyValue<string>(node, "RoomId", "");
                if (!string.IsNullOrEmpty(roomId))
                {
                    ctx.GameState.CurrentRoomId = roomId;
                    OnPlayerTeleported?.Invoke(roomId);
                }
                await Task.CompletedTask;
            },

            ["Action_MoveNpc"] = async (node, ctx) =>
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

            ["Action_SetFlag"] = async (node, ctx) =>
            {
                var flagName = GetPropertyValue<string>(node, "FlagName", "");
                var value = GetPropertyValue<bool>(node, "Value", true);

                if (!string.IsNullOrEmpty(flagName))
                {
                    ctx.GameState.Flags[flagName] = value;
                }
                await Task.CompletedTask;
            },

            ["Action_SetCounter"] = async (node, ctx) =>
            {
                var counterName = GetPropertyValue<string>(node, "CounterName", "");
                var value = GetPropertyValue<int>(node, "Value", 0);

                if (!string.IsNullOrEmpty(counterName))
                {
                    ctx.GameState.Counters[counterName] = value;
                }
                await Task.CompletedTask;
            },

            ["Action_IncrementCounter"] = async (node, ctx) =>
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

            ["Action_PlaySound"] = async (node, ctx) =>
            {
                var soundId = GetPropertyValue<string>(node, "SoundId", "");
                if (!string.IsNullOrEmpty(soundId))
                {
                    OnPlaySound?.Invoke(soundId);
                }
                await Task.CompletedTask;
            },

            ["Action_StartQuest"] = async (node, ctx) =>
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
                }
                await Task.CompletedTask;
            },

            ["Action_CompleteQuest"] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Completed;
                }
                await Task.CompletedTask;
            },

            ["Action_FailQuest"] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Failed;
                }
                await Task.CompletedTask;
            },

            ["Action_OpenDoor"] = async (node, ctx) =>
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

            ["Action_CloseDoor"] = async (node, ctx) =>
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

            ["Action_LockDoor"] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    door.IsLocked = true;
                }
                await Task.CompletedTask;
            },

            ["Action_UnlockDoor"] = async (node, ctx) =>
            {
                var doorId = GetPropertyValue<string>(node, "DoorId", "");
                var door = ctx.GameState.Doors.FirstOrDefault(d =>
                    string.Equals(d.Id, doorId, StringComparison.OrdinalIgnoreCase));
                if (door != null)
                {
                    door.IsLocked = false;
                }
                await Task.CompletedTask;
            },

            ["Action_SetNpcVisible"] = async (node, ctx) =>
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

            ["Action_SetObjectVisible"] = async (node, ctx) =>
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

            ["Action_AddGold"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 0);
                ctx.GameState.Player.Gold += amount;
                await Task.CompletedTask;
            },

            ["Action_RemoveGold"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 0);
                ctx.GameState.Player.Gold = Math.Max(0, ctx.GameState.Player.Gold - amount);
                await Task.CompletedTask;
            },

            // === NPC PATROL HANDLERS ===
            ["Action_StartPatrol"] = async (node, ctx) =>
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

            ["Action_StopPatrol"] = async (node, ctx) =>
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

            ["Action_PatrolStep"] = async (node, ctx) =>
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

            ["Action_SetPatrolMode"] = async (node, ctx) =>
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
            ["Action_FollowPlayer"] = async (node, ctx) =>
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

            ["Action_StopFollowing"] = async (node, ctx) =>
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

            ["Action_SetFollowMode"] = async (node, ctx) =>
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

            ["Action_StartConversation"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                if (!string.IsNullOrEmpty(npcId))
                {
                    OnStartConversation?.Invoke(npcId);
                }
                await Task.CompletedTask;
            },

            // === CONTROL DE FLUJO ===
            ["Flow_Branch"] = async (node, ctx) =>
            {
                // La condición se obtiene de un puerto de datos conectado
                // Por ahora, usamos True por defecto
                ctx.NextOutputPort = "True";
                await Task.CompletedTask;
            },

            ["Flow_Sequence"] = async (node, ctx) =>
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

            ["Flow_Delay"] = async (node, ctx) =>
            {
                var seconds = GetPropertyValue<float>(node, "Seconds", 1.0f);
                await Task.Delay(TimeSpan.FromSeconds(seconds));
            },

            ["Flow_RandomBranch"] = async (node, ctx) =>
            {
                var randomOutput = _random.Next(3);
                ctx.NextOutputPort = $"Out{randomOutput}";
                await Task.CompletedTask;
            },

            // === VARIABLES ===
            ["Variable_GetFlag"] = async (node, ctx) =>
            {
                // Las variables no afectan el flujo directamente
                await Task.CompletedTask;
            },

            ["Variable_GetCounter"] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            ["Variable_GetCurrentRoom"] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            ["Variable_GetGameHour"] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            },

            ["Variable_GetPlayerGold"] = async (node, ctx) =>
            {
                await Task.CompletedTask;
            }
        };
    }
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
