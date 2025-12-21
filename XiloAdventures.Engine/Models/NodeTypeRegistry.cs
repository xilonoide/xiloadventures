using System.Collections.Generic;
using System.Linq;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Registro de tipos de nodos disponibles en el editor de scripts.
/// </summary>
public static class NodeTypeRegistry
{
    private static readonly Dictionary<string, NodeTypeDefinition> _types = new(StringComparer.OrdinalIgnoreCase);

    static NodeTypeRegistry()
    {
        RegisterEventNodes();
        RegisterConditionNodes();
        RegisterActionNodes();
        RegisterFlowNodes();
        RegisterVariableNodes();
        RegisterDataComparisonNodes();
        RegisterMathNodes();
        RegisterLogicNodes();
        RegisterDataActionNodes();
        RegisterSelectionNodes();
        RegisterConversationNodes();
    }

    public static IReadOnlyDictionary<string, NodeTypeDefinition> Types => _types;

    public static NodeTypeDefinition? GetNodeType(string typeId)
    {
        return _types.TryGetValue(typeId, out var def) ? def : null;
    }

    public static IEnumerable<NodeTypeDefinition> GetNodesForOwnerType(string ownerType)
    {
        return _types.Values.Where(n =>
            n.OwnerTypes.Contains("*") || n.OwnerTypes.Contains(ownerType));
    }

    public static IEnumerable<NodeTypeDefinition> GetNodesByCategory(NodeCategory category)
    {
        return _types.Values.Where(n => n.Category == category);
    }

    private static void Register(NodeTypeDefinition def)
    {
        _types[def.TypeId] = def;
    }

    #region Event Nodes

    private static void RegisterEventNodes()
    {
        // === GAME EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnGameStart",
            DisplayName = "Al Iniciar Juego",
            Description = "Se ejecuta cuando el jugador inicia una nueva partida",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnGameEnd",
            DisplayName = "Al Terminar Juego",
            Description = "Se ejecuta cuando el jugador termina la partida",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_EveryMinute",
            DisplayName = "Cada Minuto",
            Description = "Se ejecuta cada minuto de tiempo de juego",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_EveryHour",
            DisplayName = "Cada Hora",
            Description = "Se ejecuta cada hora de tiempo de juego",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTurnStart",
            DisplayName = "Al Inicio del Turno",
            Description = "Se ejecuta al inicio de cada turno del jugador",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "TurnNumber", PortType = PortType.Data, DataType = "int", Label = "Turno" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnWeatherChange",
            DisplayName = "Al Cambiar Clima",
            Description = "Se ejecuta cuando cambia el clima",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NewWeather", PortType = PortType.Data, DataType = "string", Label = "Clima" }
            }
        });

        // === ROOM EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnEnter",
            DisplayName = "Al Entrar",
            Description = "Se ejecuta cuando el jugador entra en la sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Room" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnExit",
            DisplayName = "Al Salir",
            Description = "Se ejecuta cuando el jugador sale de la sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Room" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Direction", PortType = PortType.Data, DataType = "string", Label = "Direccion" }
            }
        });

        // === DOOR EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorOpen",
            DisplayName = "Al Abrir Puerta",
            Description = "Se ejecuta cuando se abre la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorClose",
            DisplayName = "Al Cerrar Puerta",
            Description = "Se ejecuta cuando se cierra la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorLock",
            DisplayName = "Al Bloquear Puerta",
            Description = "Se ejecuta cuando se bloquea la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorUnlock",
            DisplayName = "Al Desbloquear Puerta",
            Description = "Se ejecuta cuando se desbloquea la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorKnock",
            DisplayName = "Al Llamar Puerta",
            Description = "Se ejecuta cuando el jugador llama a la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === NPC EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTalk",
            DisplayName = "Al Hablar",
            Description = "Se ejecuta cuando el jugador habla con el NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcAttack",
            DisplayName = "Al Atacar NPC",
            Description = "Se ejecuta cuando el jugador ataca al NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcDeath",
            DisplayName = "Al Morir NPC",
            Description = "Se ejecuta cuando el NPC muere",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcSee",
            DisplayName = "Al Ver Jugador",
            Description = "Se ejecuta cuando el NPC ve al jugador entrar en su sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === OBJECT EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTake",
            DisplayName = "Al Coger",
            Description = "Se ejecuta cuando el jugador coge el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDrop",
            DisplayName = "Al Soltar",
            Description = "Se ejecuta cuando el jugador suelta el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnUse",
            DisplayName = "Al Usar",
            Description = "Se ejecuta cuando el jugador usa el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnExamine",
            DisplayName = "Al Examinar",
            Description = "Se ejecuta cuando el jugador examina el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnContainerOpen",
            DisplayName = "Al Abrir Contenedor",
            Description = "Se ejecuta cuando el jugador abre el contenedor",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnContainerClose",
            DisplayName = "Al Cerrar Contenedor",
            Description = "Se ejecuta cuando el jugador cierra el contenedor",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === QUEST EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestStart",
            DisplayName = "Al Iniciar Mision",
            Description = "Se ejecuta cuando se inicia la mision",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestComplete",
            DisplayName = "Al Completar Mision",
            Description = "Se ejecuta cuando se completa la mision",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestFail",
            DisplayName = "Al Fallar Mision",
            Description = "Se ejecuta cuando se falla la mision",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnObjectiveComplete",
            DisplayName = "Al Completar Objetivo",
            Description = "Se ejecuta cuando se completa un objetivo de la mision",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectiveIndex", PortType = PortType.Data, DataType = "int", Label = "Indice" }
            }
        });
    }

    #endregion

    #region Condition Nodes

    private static void RegisterConditionNodes()
    {
        // === SUBGRUPO: JUEGO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_HasItem",
            DisplayName = "Juego: Tiene Objeto",
            Description = "Verifica si el jugador tiene un objeto en su inventario",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsInRoom",
            DisplayName = "Juego: Está en Sala",
            Description = "Verifica si el jugador esta en una sala especifica",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsQuestStatus",
            DisplayName = "Juego: Estado de Misión",
            Description = "Verifica el estado de una mision",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" },
                new NodePropertyDefinition { Name = "Status", DisplayName = "Estado", DataType = "select", Options = new[] { "NotStarted", "InProgress", "Completed", "Failed" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_HasFlag",
            DisplayName = "Juego: Tiene Flag",
            Description = "Verifica si un flag esta activo",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Nombre del Flag", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_CompareCounter",
            DisplayName = "Juego: Comparar Contador",
            Description = "Compara el valor de un contador",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "Operator", DisplayName = "Operador", DataType = "select", Options = new[] { "==", "!=", "<", "<=", ">", ">=" } },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "int", DefaultValue = 0 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsTimeOfDay",
            DisplayName = "Juego: Es Hora del Día",
            Description = "Verifica la hora del juego",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "TimeRange", DisplayName = "Periodo", DataType = "select", Options = new[] { "Manana", "Tarde", "Noche", "Madrugada" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsDoorOpen",
            DisplayName = "Juego: Puerta Abierta",
            Description = "Verifica si una puerta esta abierta",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsNpcVisible",
            DisplayName = "Juego: NPC Visible",
            Description = "Verifica si un NPC es visible",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsPatrolling",
            DisplayName = "Juego: NPC Patrullando",
            Description = "Verifica si un NPC está patrullando",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsFollowingPlayer",
            DisplayName = "Juego: NPC Siguiendo",
            Description = "Verifica si un NPC está siguiendo al jugador",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        // === SUBGRUPO: OPERADORES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_Random",
            DisplayName = "Operadores: Probabilidad",
            Description = "Se cumple con una probabilidad dada",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Probability", DisplayName = "Probabilidad (%)", DataType = "int", DefaultValue = 50 }
            }
        });
    }

    #endregion

    #region Action Nodes

    private static void RegisterActionNodes()
    {
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ShowMessage",
            DisplayName = "Mostrar Mensaje",
            Description = "Muestra un mensaje al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Message", DisplayName = "Mensaje", DataType = "string", DefaultValue = "", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_GiveItem",
            DisplayName = "Dar Objeto",
            Description = "Da un objeto al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveItem",
            DisplayName = "Quitar Objeto",
            Description = "Quita un objeto del inventario del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_TeleportPlayer",
            DisplayName = "Teletransportar Jugador",
            Description = "Mueve al jugador a otra sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala destino", DataType = "string", EntityType = "Room" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_MoveNpc",
            DisplayName = "Mover NPC",
            Description = "Mueve un NPC a otra sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala destino", DataType = "string", EntityType = "Room" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetFlag",
            DisplayName = "Establecer Flag",
            Description = "Activa o desactiva un flag",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Nombre del Flag", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetCounter",
            DisplayName = "Establecer Contador",
            Description = "Establece el valor de un contador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "int", DefaultValue = 0 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_IncrementCounter",
            DisplayName = "Incrementar Contador",
            Description = "Incrementa o decrementa un contador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_PlaySound",
            DisplayName = "Reproducir Sonido",
            Description = "Reproduce un efecto de sonido",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "SoundId", DisplayName = "Sonido", DataType = "string", EntityType = "Fx" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StartQuest",
            DisplayName = "Iniciar Mision",
            Description = "Inicia una mision",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_CompleteQuest",
            DisplayName = "Completar Mision",
            Description = "Marca una mision como completada",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_FailQuest",
            DisplayName = "Fallar Mision",
            Description = "Marca una mision como fallida",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_OpenDoor",
            DisplayName = "Abrir Puerta",
            Description = "Abre una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_CloseDoor",
            DisplayName = "Cerrar Puerta",
            Description = "Cierra una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_LockDoor",
            DisplayName = "Bloquear Puerta",
            Description = "Bloquea una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_UnlockDoor",
            DisplayName = "Desbloquear Puerta",
            Description = "Desbloquea una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcVisible",
            DisplayName = "Visibilidad NPC",
            Description = "Muestra u oculta un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Visible", DisplayName = "Visible", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetObjectVisible",
            DisplayName = "Visibilidad Objeto",
            Description = "Muestra u oculta un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Visible", DisplayName = "Visible", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddGold",
            DisplayName = "Dar Oro",
            Description = "Da oro al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveGold",
            DisplayName = "Quitar Oro",
            Description = "Quita oro al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 10 }
            }
        });

        // === NPC: PATRULLA ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StartPatrol",
            DisplayName = "NPC: Iniciar Patrulla",
            Description = "Hace que un NPC comience a patrullar su ruta definida",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StopPatrol",
            DisplayName = "NPC: Detener Patrulla",
            Description = "Detiene la patrulla de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_PatrolStep",
            DisplayName = "NPC: Paso de Patrulla",
            Description = "Mueve manualmente un NPC al siguiente punto de su ruta",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetPatrolMode",
            DisplayName = "NPC: Modo de Patrulla",
            Description = "Configura el modo de movimiento de patrulla (por turnos o por tiempo)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Mode", DisplayName = "Modo", DataType = "enum:Turns=Turnos,Time=Tiempo", DefaultValue = "Turns" },
                new NodePropertyDefinition { Name = "TurnSpeed", DisplayName = "Velocidad (turnos)", DataType = "int", DefaultValue = 1 },
                new NodePropertyDefinition { Name = "TimeInterval", DisplayName = "Intervalo (segundos)", DataType = "float", DefaultValue = 3.0f }
            }
        });

        // === NPC: SEGUIR JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_FollowPlayer",
            DisplayName = "NPC: Seguir Jugador",
            Description = "Hace que un NPC siga al jugador cuando cambie de sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Speed", DisplayName = "Velocidad (turnos)", DataType = "int", DefaultValue = 1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StopFollowing",
            DisplayName = "NPC: Dejar de Seguir",
            Description = "Hace que un NPC deje de seguir al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetFollowMode",
            DisplayName = "NPC: Modo de Seguimiento",
            Description = "Configura el modo de movimiento de seguimiento (por turnos o por tiempo)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Mode", DisplayName = "Modo", DataType = "enum:Turns=Turnos,Time=Tiempo", DefaultValue = "Turns" },
                new NodePropertyDefinition { Name = "TurnSpeed", DisplayName = "Velocidad (turnos)", DataType = "int", DefaultValue = 1 },
                new NodePropertyDefinition { Name = "TimeInterval", DisplayName = "Intervalo (segundos)", DataType = "float", DefaultValue = 3.0f }
            }
        });
    }

    #endregion

    #region Flow Nodes

    private static void RegisterFlowNodes()
    {
        Register(new NodeTypeDefinition
        {
            TypeId = "Flow_Branch",
            DisplayName = "Bifurcacion",
            Description = "Bifurca el flujo segun una condicion (usar con nodos de condicion)",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Condition", PortType = PortType.Data, DataType = "bool", Label = "Condicion" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Flow_Sequence",
            DisplayName = "Secuencia",
            Description = "Ejecuta multiples salidas en orden",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Then0", PortType = PortType.Execution, Label = "1" },
                new NodePort { Name = "Then1", PortType = PortType.Execution, Label = "2" },
                new NodePort { Name = "Then2", PortType = PortType.Execution, Label = "3" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Flow_Delay",
            DisplayName = "Esperar",
            Description = "Espera un tiempo antes de continuar",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Seconds", DisplayName = "Segundos", DataType = "float", DefaultValue = 1.0f }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Flow_RandomBranch",
            DisplayName = "Rama Aleatoria",
            Description = "Elige una salida aleatoriamente",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Out0", PortType = PortType.Execution, Label = "1" },
                new NodePort { Name = "Out1", PortType = PortType.Execution, Label = "2" },
                new NodePort { Name = "Out2", PortType = PortType.Execution, Label = "3" }
            }
        });
    }

    #endregion

    #region Variable Nodes

    private static void RegisterVariableNodes()
    {
        // === SUBGRUPO: JUEGO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetGameHour",
            DisplayName = "Juego: Hora",
            Description = "Obtiene la hora actual del juego",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Hour", PortType = PortType.Data, DataType = "int", Label = "Hora" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerGold",
            DisplayName = "Juego: Oro del Jugador",
            Description = "Obtiene el oro actual del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Gold", PortType = PortType.Data, DataType = "int", Label = "Oro" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetCurrentRoom",
            DisplayName = "Juego: Sala Actual",
            Description = "Obtiene la sala actual del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "RoomId", PortType = PortType.Data, DataType = "string", Label = "Sala" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetCurrentWeather",
            DisplayName = "Juego: Clima Actual",
            Description = "Obtiene el clima actual del juego",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Weather", PortType = PortType.Data, DataType = "string", Label = "Clima" }
            }
        });

        // === SUBGRUPO: JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerStrength",
            DisplayName = "Jugador: Fuerza",
            Description = "Obtiene la fuerza del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Fuerza" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerConstitution",
            DisplayName = "Jugador: Constitución",
            Description = "Obtiene la constitución del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Constitución" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerIntelligence",
            DisplayName = "Jugador: Inteligencia",
            Description = "Obtiene la inteligencia del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Inteligencia" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerDexterity",
            DisplayName = "Jugador: Destreza",
            Description = "Obtiene la destreza del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Destreza" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerCharisma",
            DisplayName = "Jugador: Carisma",
            Description = "Obtiene el carisma del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Carisma" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerWeight",
            DisplayName = "Jugador: Peso",
            Description = "Obtiene el peso del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Peso" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerAge",
            DisplayName = "Jugador: Edad",
            Description = "Obtiene la edad del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Edad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerHeight",
            DisplayName = "Jugador: Altura",
            Description = "Obtiene la altura del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Altura" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerInitialMoney",
            DisplayName = "Jugador: Dinero Inicial",
            Description = "Obtiene el dinero inicial configurado para el jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Dinero Inicial" }
            }
        });

        // === SUBGRUPO: OPERADORES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetFlag",
            DisplayName = "Operadores: Obtener Flag",
            Description = "Obtiene el valor de un flag",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "bool", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Nombre del Flag", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetCounter",
            DisplayName = "Operadores: Obtener Contador",
            Description = "Obtiene el valor de un contador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_ConstantInt",
            DisplayName = "Operadores: Entero Constante",
            Description = "Un valor entero constante",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "int", DefaultValue = 0 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_ConstantBool",
            DisplayName = "Operadores: Booleano Constante",
            Description = "Un valor booleano constante (verdadero/falso)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "bool", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "bool", DefaultValue = false }
            }
        });
    }

    #endregion

    #region Comparaciones con entrada de datos

    private static void RegisterDataComparisonNodes()
    {
        // === SUBGRUPO: OPERADORES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Compare_Int",
            DisplayName = "Operadores: Comparar Enteros",
            Description = "Compara dos valores enteros y produce un resultado booleano",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Operator", DisplayName = "Operador", DataType = "select", Options = new[] { "==", "!=", "<", "<=", ">", ">=" } }
            }
        });

        // === SUBGRUPO: JUEGO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Compare_PlayerGold",
            DisplayName = "Juego: Comparar Oro",
            Description = "Compara el oro del jugador con un valor",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "CompareValue", PortType = PortType.Data, DataType = "int", Label = "Comparar con" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Operator", DisplayName = "Operador", DataType = "select", Options = new[] { "==", "!=", "<", "<=", ">", ">=" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Compare_Counter",
            DisplayName = "Juego: Comparar Contador (Data)",
            Description = "Compara un contador con un valor (entrada de datos)",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "CompareValue", PortType = PortType.Data, DataType = "int", Label = "Comparar con" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "Operator", DisplayName = "Operador", DataType = "select", Options = new[] { "==", "!=", "<", "<=", ">", ">=" } }
            }
        });
    }

    #endregion

    #region Operaciones Matematicas

    private static void RegisterMathNodes()
    {
        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Add",
            DisplayName = "Operadores: Sumar",
            Description = "Suma dos valores enteros",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Subtract",
            DisplayName = "Operadores: Restar",
            Description = "Resta dos valores enteros (A - B)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Multiply",
            DisplayName = "Operadores: Multiplicar",
            Description = "Multiplica dos valores enteros",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Divide",
            DisplayName = "Operadores: Dividir",
            Description = "Divide dos valores enteros (A / B)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Modulo",
            DisplayName = "Operadores: Módulo",
            Description = "Obtiene el resto de la division (A % B)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Negate",
            DisplayName = "Operadores: Negar",
            Description = "Cambia el signo de un valor entero",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Abs",
            DisplayName = "Operadores: Valor Absoluto",
            Description = "Obtiene el valor absoluto de un entero",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Min",
            DisplayName = "Operadores: Mínimo",
            Description = "Obtiene el menor de dos valores",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Max",
            DisplayName = "Operadores: Máximo",
            Description = "Obtiene el mayor de dos valores",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "int", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "int", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Clamp",
            DisplayName = "Operadores: Limitar",
            Description = "Limita un valor entre un minimo y un maximo",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" },
                new NodePort { Name = "Min", PortType = PortType.Data, DataType = "int", Label = "Min" },
                new NodePort { Name = "Max", PortType = PortType.Data, DataType = "int", Label = "Max" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Math_Random",
            DisplayName = "Operadores: Aleatorio",
            Description = "Genera un numero aleatorio entre Min y Max (inclusive)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Min", PortType = PortType.Data, DataType = "int", Label = "Min" },
                new NodePort { Name = "Max", PortType = PortType.Data, DataType = "int", Label = "Max" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });
    }

    #endregion

    #region Operaciones Logicas

    private static void RegisterLogicNodes()
    {
        // === SUBGRUPO: OPERADORES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Logic_And",
            DisplayName = "Operadores: Y (AND)",
            Description = "Devuelve verdadero si ambas entradas son verdaderas",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "bool", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "bool", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Logic_Or",
            DisplayName = "Operadores: O (OR)",
            Description = "Devuelve verdadero si al menos una entrada es verdadera",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "bool", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "bool", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Logic_Not",
            DisplayName = "Operadores: No (NOT)",
            Description = "Invierte el valor booleano",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "bool", Label = "Valor" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Logic_Xor",
            DisplayName = "Operadores: O Exclusivo (XOR)",
            Description = "Devuelve verdadero si exactamente una entrada es verdadera",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "A", PortType = PortType.Data, DataType = "bool", Label = "A" },
                new NodePort { Name = "B", PortType = PortType.Data, DataType = "bool", Label = "B" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            }
        });
    }

    #endregion

    #region Acciones con entrada de datos

    private static void RegisterDataActionNodes()
    {
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetGold",
            DisplayName = "Establecer Oro",
            Description = "Establece el oro del jugador a un valor especifico",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddGoldData",
            DisplayName = "Dar Oro (Datos)",
            Description = "Da oro al jugador usando un valor de conexion",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveGoldData",
            DisplayName = "Quitar Oro (Datos)",
            Description = "Quita oro al jugador usando un valor de conexion",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetCounterData",
            DisplayName = "Establecer Contador (Datos)",
            Description = "Establece un contador a un valor especifico usando conexion",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_IncrementCounterData",
            DisplayName = "Incrementar Contador (Datos)",
            Description = "Incrementa un contador usando un valor de conexion",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "CounterName", DisplayName = "Contador", DataType = "string", IsRequired = true }
            }
        });
    }

    #endregion

    #region Nodos de seleccion

    private static void RegisterSelectionNodes()
    {
        Register(new NodeTypeDefinition
        {
            TypeId = "Select_Int",
            DisplayName = "Seleccionar Entero",
            Description = "Selecciona entre dos valores enteros segun una condicion",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Condition", PortType = PortType.Data, DataType = "bool", Label = "Condicion" },
                new NodePort { Name = "True", PortType = PortType.Data, DataType = "int", Label = "Si verdadero" },
                new NodePort { Name = "False", PortType = PortType.Data, DataType = "int", Label = "Si falso" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "int", Label = "Resultado" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Select_Bool",
            DisplayName = "Seleccionar Booleano",
            Description = "Selecciona entre dos valores booleanos segun una condicion",
            Category = NodeCategory.Flow,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Condition", PortType = PortType.Data, DataType = "bool", Label = "Condicion" },
                new NodePort { Name = "True", PortType = PortType.Data, DataType = "bool", Label = "Si verdadero" },
                new NodePort { Name = "False", PortType = PortType.Data, DataType = "bool", Label = "Si falso" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Result", PortType = PortType.Data, DataType = "bool", Label = "Resultado" }
            }
        });
    }

    #endregion

    #region Conversation Nodes

    private static void RegisterConversationNodes()
    {
        // === INICIO DE CONVERSACIÓN ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_Start",
            DisplayName = "Inicio de Conversación",
            Description = "Punto de entrada de una conversación con NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === NPC DICE ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_NpcSay",
            DisplayName = "NPC Dice",
            Description = "El NPC dice un texto al jugador",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition
                {
                    Name = "Text",
                    DisplayName = "Texto",
                    DataType = "string",
                    DefaultValue = "",
                    IsRequired = true
                },
                new NodePropertyDefinition
                {
                    Name = "SpeakerName",
                    DisplayName = "Nombre (opcional)",
                    DataType = "string",
                    DefaultValue = ""
                },
                new NodePropertyDefinition
                {
                    Name = "Emotion",
                    DisplayName = "Emoción",
                    DataType = "select",
                    DefaultValue = "Neutral",
                    Options = new[] { "Neutral", "Feliz", "Triste", "Enfadado", "Sorprendido" }
                }
            }
        });

        // === OPCIONES DEL JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_PlayerChoice",
            DisplayName = "Opciones del Jugador",
            Description = "Presenta opciones de diálogo al jugador (hasta 4)",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Option1", PortType = PortType.Execution, Label = "Opción 1" },
                new NodePort { Name = "Option2", PortType = PortType.Execution, Label = "Opción 2" },
                new NodePort { Name = "Option3", PortType = PortType.Execution, Label = "Opción 3" },
                new NodePort { Name = "Option4", PortType = PortType.Execution, Label = "Opción 4" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Text1", DisplayName = "Texto Opción 1", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "Text2", DisplayName = "Texto Opción 2", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "Text3", DisplayName = "Texto Opción 3", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "Text4", DisplayName = "Texto Opción 4", DataType = "string", DefaultValue = "" }
            }
        });

        // === BIFURCACIÓN DE DIÁLOGO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_Branch",
            DisplayName = "Bifurcación de Diálogo",
            Description = "Elige un camino según una condición",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Sí" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition
                {
                    Name = "ConditionType",
                    DisplayName = "Tipo de Condición",
                    DataType = "select",
                    DefaultValue = "HasFlag",
                    Options = new[] { "HasFlag", "HasItem", "HasGold", "QuestStatus", "VisitedNode" }
                },
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Nombre del Flag", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "ItemId", DisplayName = "Objeto", DataType = "string", DefaultValue = "", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "GoldAmount", DisplayName = "Cantidad de Oro", DataType = "int", DefaultValue = 0 },
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Misión", DataType = "string", DefaultValue = "", EntityType = "Quest" },
                new NodePropertyDefinition
                {
                    Name = "QuestStatus",
                    DisplayName = "Estado de Misión",
                    DataType = "select",
                    DefaultValue = "InProgress",
                    Options = new[] { "NotStarted", "InProgress", "Completed", "Failed" }
                }
            }
        });

        // === FIN DE CONVERSACIÓN ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_End",
            DisplayName = "Fin de Conversación",
            Description = "Termina la conversación y devuelve el control al juego",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === ACCIÓN EN CONVERSACIÓN ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_Action",
            DisplayName = "Ejecutar Acción",
            Description = "Ejecuta una acción dentro de la conversación",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition
                {
                    Name = "ActionType",
                    DisplayName = "Tipo de Acción",
                    DataType = "select",
                    DefaultValue = "ShowMessage",
                    Options = new[] { "GiveItem", "RemoveItem", "AddGold", "RemoveGold", "SetFlag", "StartQuest", "CompleteQuest", "ShowMessage" }
                },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto (si aplica)", DataType = "string", DefaultValue = "", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad (si aplica)", DataType = "int", DefaultValue = 0 },
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Flag (si aplica)", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Misión (si aplica)", DataType = "string", DefaultValue = "", EntityType = "Quest" },
                new NodePropertyDefinition { Name = "Message", DisplayName = "Mensaje (si aplica)", DataType = "string", DefaultValue = "" }
            }
        });

        // === TIENDA ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_Shop",
            DisplayName = "Abrir Tienda",
            Description = "Abre la interfaz de compra/venta con el NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "OnClose", PortType = PortType.Execution, Label = "Al Cerrar" },
                new NodePort { Name = "OnBuy", PortType = PortType.Execution, Label = "Al Comprar" },
                new NodePort { Name = "OnSell", PortType = PortType.Execution, Label = "Al Vender" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ShopTitle", DisplayName = "Título de la Tienda", DataType = "string", DefaultValue = "Tienda" },
                new NodePropertyDefinition { Name = "WelcomeMessage", DisplayName = "Mensaje de Bienvenida", DataType = "string", DefaultValue = "" }
            }
        });

        // === COMPRAR OBJETO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_BuyItem",
            DisplayName = "Comprar Objeto",
            Description = "Permite comprar un objeto específico con precio fijo",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Success", PortType = PortType.Execution, Label = "Éxito" },
                new NodePort { Name = "NotEnoughGold", PortType = PortType.Execution, Label = "Sin Oro" },
                new NodePort { Name = "Cancelled", PortType = PortType.Execution, Label = "Cancelado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", DefaultValue = "", EntityType = "GameObject", IsRequired = true },
                new NodePropertyDefinition { Name = "Price", DisplayName = "Precio", DataType = "int", DefaultValue = 10, IsRequired = true },
                new NodePropertyDefinition { Name = "ConfirmText", DisplayName = "Texto Confirmación", DataType = "string", DefaultValue = "¿Comprar por {precio} monedas?" }
            }
        });

        // === VENDER OBJETO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Conversation_SellItem",
            DisplayName = "Vender Objeto",
            Description = "Permite vender un objeto específico al NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = new[] { "Npc" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Success", PortType = PortType.Execution, Label = "Éxito" },
                new NodePort { Name = "NoItem", PortType = PortType.Execution, Label = "No Tiene" },
                new NodePort { Name = "Cancelled", PortType = PortType.Execution, Label = "Cancelado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", DefaultValue = "", EntityType = "GameObject", IsRequired = true },
                new NodePropertyDefinition { Name = "Price", DisplayName = "Precio", DataType = "int", DefaultValue = 5, IsRequired = true }
            }
        });

        // === ACCIÓN PARA SCRIPTS: INICIAR CONVERSACIÓN ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StartConversation",
            DisplayName = "Iniciar Conversación",
            Description = "Inicia una conversación con un NPC (para usar en scripts)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", DefaultValue = "", EntityType = "Npc", IsRequired = true }
            }
        });
    }

    #endregion
}
