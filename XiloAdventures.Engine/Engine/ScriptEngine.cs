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
    private readonly Dictionary<string, Func<ScriptNode, ScriptContext, Task>> _nodeHandlers;
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
                DebugMessage($"[Debug] Condition_HasItem({objectId}): {hasItem} (inventario: {string.Join(", ", ctx.GameState.InventoryObjectIds)})");
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

            // === CONDICIONES DE ILUMINACIÓN ===
            ["Condition_IsObjectLit"] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var obj = ctx.GameState.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, objectId, StringComparison.OrdinalIgnoreCase));
                var isLit = obj != null && obj.IsLightSource && obj.IsLit;
                ctx.NextOutputPort = isLit ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsRoomLit"] = async (node, ctx) =>
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

            // === CONDICIONES DE COMBATE ===
            ["Condition_IsNpcAlive"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isAlive = npc != null && !npc.IsCorpse && npc.Stats.CurrentHealth > 0;
                ctx.NextOutputPort = isAlive ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_NpcHealthBelow"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var isBelow = npc != null && npc.Stats.CurrentHealth < threshold;
                ctx.NextOutputPort = isBelow ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsInCombat"] = async (node, ctx) =>
            {
                var isInCombat = ctx.GameState.ActiveCombat?.IsActive ?? false;
                ctx.NextOutputPort = isInCombat ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE COMBATE ADICIONALES ===
            ["Condition_PlayerHealthBelow"] = async (node, ctx) =>
            {
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var player = ctx.GameState.Player;
                var percentHealth = player.DynamicStats.MaxHealth > 0
                    ? (player.DynamicStats.Health * 100 / player.DynamicStats.MaxHealth)
                    : 100;
                ctx.NextOutputPort = percentHealth < threshold ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_PlayerHealthAbove"] = async (node, ctx) =>
            {
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var player = ctx.GameState.Player;
                var percentHealth = player.DynamicStats.MaxHealth > 0
                    ? (player.DynamicStats.Health * 100 / player.DynamicStats.MaxHealth)
                    : 100;
                ctx.NextOutputPort = percentHealth > threshold ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_PlayerHasWeaponType"] = async (node, ctx) =>
            {
                var damageTypeStr = GetPropertyValue<string>(node, "DamageType", "Physical");
                var expectedType = Enum.TryParse<DamageType>(damageTypeStr, out var dt) ? dt : DamageType.Physical;

                var hasWeaponType = false;
                var equippedWeaponId = ctx.GameState.Player.EquippedWeaponId;
                if (!string.IsNullOrEmpty(equippedWeaponId))
                {
                    var weapon = ctx.GameState.Objects.FirstOrDefault(o =>
                        string.Equals(o.Id, equippedWeaponId, StringComparison.OrdinalIgnoreCase));
                    hasWeaponType = weapon?.DamageType == expectedType;
                }
                ctx.NextOutputPort = hasWeaponType ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_PlayerHasArmor"] = async (node, ctx) =>
            {
                var hasArmor = !string.IsNullOrEmpty(ctx.GameState.Player.EquippedArmorId);
                ctx.NextOutputPort = hasArmor ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_IsCombatRound"] = async (node, ctx) =>
            {
                var expectedRound = GetPropertyValue<int>(node, "Round", 1);
                var currentRound = ctx.GameState.ActiveCombat?.RoundNumber ?? 0;
                ctx.NextOutputPort = currentRound == expectedRound ? "True" : "False";
                await Task.CompletedTask;
            },

            // === CONDICIONES DE COMERCIO ===
            ["Condition_IsInTrade"] = async (node, ctx) =>
            {
                // Trade state is managed externally by TradeEngine, not in GameState
                // This condition returns false by default - can be extended if trade state is added to GameState
                ctx.NextOutputPort = "False";
                await Task.CompletedTask;
            },

            ["Condition_PlayerHasGold"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                var hasEnough = ctx.GameState.Player.Gold >= amount;
                ctx.NextOutputPort = hasEnough ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_NpcHasGold"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                // NPC con Gold = -1 tiene infinito
                var hasEnough = npc != null && (npc.Gold < 0 || npc.Gold >= amount);
                ctx.NextOutputPort = hasEnough ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_NpcHasInfiniteGold"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                var hasInfinite = npc != null && npc.Gold < 0;
                ctx.NextOutputPort = hasInfinite ? "True" : "False";
                await Task.CompletedTask;
            },

            ["Condition_PlayerOwnsItem"] = async (node, ctx) =>
            {
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var requiredQty = GetPropertyValue<int>(node, "Quantity", 1);
                var ownedQty = ctx.GameState.InventoryObjectIds.Count(id =>
                    string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                ctx.NextOutputPort = ownedQty >= requiredQty ? "True" : "False";
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
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[Nueva misión: {questName}]");
                }
                await Task.CompletedTask;
            },

            ["Action_CompleteQuest"] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Completed;
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[¡Misión completada: {questName}!]");

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
                await Task.CompletedTask;
            },

            ["Action_FailQuest"] = async (node, ctx) =>
            {
                var questId = GetPropertyValue<string>(node, "QuestId", "");
                if (!string.IsNullOrEmpty(questId) && ctx.GameState.Quests.TryGetValue(questId, out var state))
                {
                    state.Status = QuestStatus.Failed;
                    var quest = ctx.World.Quests.FirstOrDefault(q =>
                        string.Equals(q.Id, questId, StringComparison.OrdinalIgnoreCase));
                    var questName = quest?.Name ?? questId;
                    OnMessage?.Invoke($"[Misión fallida: {questName}]");
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

            // === LIGHT SOURCE HANDLERS ===
            ["Action_SetObjectLit"] = async (node, ctx) =>
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

            ["Action_SetLightTurns"] = async (node, ctx) =>
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
            },

            // === VARIABLES DE ESTADOS DINÁMICOS ===
            ["Variable_GetPlayerHealth"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Health);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerMaxHealth"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.MaxHealth);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerHunger"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Hunger);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerThirst"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Thirst);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerEnergy"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Energy);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerSleep"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Sleep);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerSanity"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Sanity);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerMana"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.Mana);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerMaxMana"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.Player.DynamicStats.MaxMana);
                await Task.CompletedTask;
            },
            ["Variable_GetPlayerState"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.SetOutputValue(node.Id, "Value", value);
                await Task.CompletedTask;
            },
            ["Variable_GetActiveModifiersCount"] = async (node, ctx) =>
            {
                ctx.SetOutputValue(node.Id, "Value", ctx.GameState.ActiveModifiers.Count(m => !m.IsExpired));
                await Task.CompletedTask;
            },
            ["Variable_HasModifier"] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                var hasIt = ctx.GameState.ActiveModifiers.Any(m =>
                    !m.IsExpired && string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                ctx.SetOutputValue(node.Id, "Value", hasIt);
                await Task.CompletedTask;
            },

            // === CONDICIONES DE ESTADOS ===
            ["Condition_PlayerStateAbove"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var threshold = GetPropertyValue<int>(node, "Threshold", 50);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value > threshold ? "True" : "False";
                await Task.CompletedTask;
            },
            ["Condition_PlayerStateBelow"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var threshold = GetPropertyValue<int>(node, "Threshold", 25);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value < threshold ? "True" : "False";
                await Task.CompletedTask;
            },
            ["Condition_PlayerStateEquals"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var targetValue = GetPropertyValue<int>(node, "Value", 100);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value == targetValue ? "True" : "False";
                await Task.CompletedTask;
            },
            ["Condition_PlayerStateBetween"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var minValue = GetPropertyValue<int>(node, "MinValue", 25);
                var maxValue = GetPropertyValue<int>(node, "MaxValue", 75);
                var value = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                ctx.NextOutputPort = value >= minValue && value <= maxValue ? "True" : "False";
                await Task.CompletedTask;
            },
            ["Condition_HasModifier"] = async (node, ctx) =>
            {
                var name = GetPropertyValue<string>(node, "ModifierName", "");
                var hasIt = ctx.GameState.ActiveModifiers.Any(m =>
                    !m.IsExpired && string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
                ctx.NextOutputPort = hasIt ? "True" : "False";
                await Task.CompletedTask;
            },
            ["Condition_HasModifierForState"] = async (node, ctx) =>
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
            ["Condition_IsPlayerAlive"] = async (node, ctx) =>
            {
                ctx.NextOutputPort = ctx.GameState.Player.DynamicStats.Health > 0 ? "True" : "False";
                await Task.CompletedTask;
            },

            // === ACCIONES DE ESTADOS ===
            ["Action_SetPlayerState"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var value = GetPropertyValue<int>(node, "Value", 100);
                SetPlayerStateValue(ctx.GameState, stateType ?? "Health", value);
                await Task.CompletedTask;
            },
            ["Action_ModifyPlayerState"] = async (node, ctx) =>
            {
                var stateType = GetPropertyValue<string>(node, "StateType", "Health");
                var amount = GetPropertyValue<int>(node, "Amount", 10);
                var current = GetPlayerStateValue(ctx.GameState, stateType ?? "Health");
                SetPlayerStateValue(ctx.GameState, stateType ?? "Health", current + amount);
                await Task.CompletedTask;
            },
            ["Action_HealPlayer"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Health = Math.Min(stats.MaxHealth, stats.Health + amount);
                await Task.CompletedTask;
            },
            ["Action_DamagePlayer"] = async (node, ctx) =>
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
            ["Action_RestoreMana"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Mana = Math.Min(stats.MaxMana, stats.Mana + amount);
                await Task.CompletedTask;
            },
            ["Action_ConsumeMana"] = async (node, ctx) =>
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
            ["Action_StartCombat"] = async (node, ctx) =>
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
            ["Action_DamageNpc"] = async (node, ctx) =>
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
            ["Action_HealNpc"] = async (node, ctx) =>
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

            // === ACCIONES DE COMBATE ADICIONALES ===
            ["Action_SetPlayerMaxHealth"] = async (node, ctx) =>
            {
                var maxHealth = GetPropertyValue<int>(node, "MaxHealth", 100);
                ctx.GameState.Player.DynamicStats.MaxHealth = maxHealth;
                if (ctx.GameState.Player.DynamicStats.Health > maxHealth)
                    ctx.GameState.Player.DynamicStats.Health = maxHealth;
                await Task.CompletedTask;
            },
            ["Action_SetNpcAttack"] = async (node, ctx) =>
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
            ["Action_SetNpcDefense"] = async (node, ctx) =>
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
            ["Action_EndCombatVictory"] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat end is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },
            ["Action_EndCombatDefeat"] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat end is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },
            ["Action_ForceFlee"] = async (node, ctx) =>
            {
                if (ctx.GameState.ActiveCombat?.IsActive == true)
                {
                    ctx.GameState.ActiveCombat.IsActive = false;
                    // Combat flee is handled by CombatEngine, this just deactivates the state
                }
                await Task.CompletedTask;
            },

            // === ACCIONES DE COMERCIO ===
            ["Action_OpenTrade"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && npc.IsShopkeeper)
                {
                    // Trigger trade open event - the actual TradeWindow is opened via GameEngine
                    OnStartTrade?.Invoke(npcId);
                }
                await Task.CompletedTask;
            },
            ["Action_CloseTrade"] = async (node, ctx) =>
            {
                // Trade close is handled externally by TradeEngine/TradeWindow
                // This action just signals intent - actual close happens in UI layer
                await Task.CompletedTask;
            },
            ["Action_AddPlayerGold"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                ctx.GameState.Player.Gold += amount;
                await Task.CompletedTask;
            },
            ["Action_RemovePlayerGold"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 100);
                if (ctx.GameState.Player.Gold >= amount)
                {
                    ctx.GameState.Player.Gold -= amount;
                }
                else
                {
                    ctx.NextOutputPort = "OnInsufficient";
                }
                await Task.CompletedTask;
            },
            ["Action_SetNpcGold"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var gold = GetPropertyValue<int>(node, "Gold", -1);
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null)
                {
                    npc.Gold = gold;
                }
                await Task.CompletedTask;
            },
            ["Action_AddNpcItem"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(objectId) &&
                    !npc.ShopInventory.Contains(objectId, StringComparer.OrdinalIgnoreCase))
                {
                    npc.ShopInventory.Add(objectId);
                }
                await Task.CompletedTask;
            },
            ["Action_RemoveNpcItem"] = async (node, ctx) =>
            {
                var npcId = GetPropertyValue<string>(node, "NpcId", "");
                var objectId = GetPropertyValue<string>(node, "ObjectId", "");
                var npc = ctx.GameState.Npcs.FirstOrDefault(n =>
                    string.Equals(n.Id, npcId, StringComparison.OrdinalIgnoreCase));
                if (npc != null && !string.IsNullOrEmpty(objectId))
                {
                    npc.ShopInventory.RemoveAll(id =>
                        string.Equals(id, objectId, StringComparison.OrdinalIgnoreCase));
                }
                await Task.CompletedTask;
            },
            ["Action_SetBuyMultiplier"] = async (node, ctx) =>
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
            ["Action_SetSellMultiplier"] = async (node, ctx) =>
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
            ["Action_AddAbility"] = async (node, ctx) =>
            {
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                if (!string.IsNullOrEmpty(abilityId) && !ctx.GameState.Player.AbilityIds.Contains(abilityId))
                {
                    ctx.GameState.Player.AbilityIds.Add(abilityId);
                }
                await Task.CompletedTask;
            },
            ["Action_RemoveAbility"] = async (node, ctx) =>
            {
                var abilityId = GetPropertyValue<string>(node, "AbilityId", "");
                if (!string.IsNullOrEmpty(abilityId))
                {
                    ctx.GameState.Player.AbilityIds.Remove(abilityId);
                }
                await Task.CompletedTask;
            },
            ["Action_AddAbilityToNpc"] = async (node, ctx) =>
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
            ["Action_RemoveAbilityFromNpc"] = async (node, ctx) =>
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

            ["Action_FeedPlayer"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Hunger = Math.Max(0, stats.Hunger - amount);
                await Task.CompletedTask;
            },
            ["Action_HydratePlayer"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 25);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Thirst = Math.Max(0, stats.Thirst - amount);
                await Task.CompletedTask;
            },
            ["Action_RestPlayer"] = async (node, ctx) =>
            {
                var amount = GetPropertyValue<int>(node, "Amount", 50);
                var stats = ctx.GameState.Player.DynamicStats;
                stats.Energy = Math.Min(100, stats.Energy + amount);
                await Task.CompletedTask;
            },
            ["Action_RestoreAllStats"] = async (node, ctx) =>
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
            ["Action_ApplyModifier"] = async (node, ctx) =>
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
            ["Action_RemoveModifier"] = async (node, ctx) =>
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
            ["Action_RemoveModifiersByState"] = async (node, ctx) =>
            {
                var stateTypeStr = GetPropertyValue<string>(node, "StateType", "Health");
                if (Enum.TryParse<PlayerStateType>(stateTypeStr, out var stateType))
                {
                    ctx.GameState.ActiveModifiers.RemoveAll(m => m.StateType == stateType);
                    DebugMessage($"[Debug] Modificadores de {stateType} eliminados");
                }
                await Task.CompletedTask;
            },
            ["Action_RemoveAllModifiers"] = async (node, ctx) =>
            {
                ctx.GameState.ActiveModifiers.Clear();
                DebugMessage("[Debug] Todos los modificadores eliminados");
                await Task.CompletedTask;
            },
            ["Action_ProcessModifiers"] = async (node, ctx) =>
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
            ["Event_OnPlayerDeath"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnHealthLow"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnHealthCritical"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnHungerHigh"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnThirstHigh"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnEnergyLow"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnSleepHigh"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnSanityLow"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnManaLow"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnStateThreshold"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnModifierApplied"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnModifierExpired"] = async (node, ctx) => { await Task.CompletedTask; },

            // === EVENTOS DE COMBATE (entry points) ===
            ["Event_OnCombatStart"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnCombatVictory"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnCombatDefeat"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnCombatFlee"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnPlayerAttack"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnNpcTurn"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnPlayerDefend"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnCriticalHit"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnMiss"] = async (node, ctx) => { await Task.CompletedTask; },

            // === EVENTOS DE COMERCIO (entry points) ===
            ["Event_OnTradeStart"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnTradeEnd"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnItemBought"] = async (node, ctx) => { await Task.CompletedTask; },
            ["Event_OnItemSold"] = async (node, ctx) => { await Task.CompletedTask; },

            // === VELOCIDAD DE NECESIDADES ===
            ["Action_SetNeedRate"] = async (node, ctx) =>
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
            ["Variable_GetNeedRate"] = async (node, ctx) =>
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
            }
        };
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
            "Gold" => gameState.Player.Gold,
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
            case "Gold":
                gameState.Player.Gold = Math.Max(0, value);
                break;
        }
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
