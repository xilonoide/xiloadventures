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

    /// <summary>
    /// Obtiene nodos filtrados por tipo de propietario y características habilitadas.
    /// </summary>
    public static IEnumerable<NodeTypeDefinition> GetNodesForOwnerType(string ownerType, GameInfo gameInfo)
    {
        return _types.Values.Where(n =>
            (n.OwnerTypes.Contains("*") || n.OwnerTypes.Contains(ownerType)) &&
            IsFeatureEnabled(n.RequiredFeature, gameInfo));
    }

    /// <summary>
    /// Verifica si una característica requerida está habilitada.
    /// </summary>
    private static bool IsFeatureEnabled(string? requiredFeature, GameInfo? gameInfo)
    {
        if (string.IsNullOrEmpty(requiredFeature) || gameInfo == null)
            return true;

        return requiredFeature switch
        {
            "Combat" => gameInfo.CombatEnabled,
            "BasicNeeds" => gameInfo.BasicNeedsEnabled,
            _ => true
        };
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
            DisplayName = "Juego: Al Iniciar",
            Description = "Se ejecuta cuando el jugador inicia una nueva partida",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnGameEnd",
            DisplayName = "Juego: Al Terminar",
            Description = "Se ejecuta cuando el jugador termina la partida (victoria o derrota)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_EveryMinute",
            DisplayName = "Juego: Cada Minuto",
            Description = "Se ejecuta cada minuto de tiempo de juego",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_EveryHour",
            DisplayName = "Juego: Cada Hora",
            Description = "Se ejecuta cada hora de tiempo de juego",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTurnStart",
            DisplayName = "Juego: Al Inicio del Turno",
            Description = "Se ejecuta al inicio de cada turno del jugador",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "TurnNumber", PortType = PortType.Data, DataType = "int", Label = "Turno" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnWeatherChange",
            DisplayName = "Juego: Al Cambiar Clima",
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
            DisplayName = "Salas: Al Entrar",
            Description = "Se ejecuta cuando el jugador entra en la sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Room" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnExit",
            DisplayName = "Salas: Al Salir",
            Description = "Se ejecuta cuando el jugador sale de la sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Room" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Direction", PortType = PortType.Data, DataType = "string", Label = "Direccion" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnLook",
            DisplayName = "Salas: Al Mirar",
            Description = "Se ejecuta cuando el jugador mira/examina la sala (comando 'mirar')",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Room" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === DOOR EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorOpen",
            DisplayName = "Puertas: Al Abrir",
            Description = "Se ejecuta cuando se abre la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorClose",
            DisplayName = "Puertas: Al Cerrar",
            Description = "Se ejecuta cuando se cierra la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorLock",
            DisplayName = "Puertas: Al Bloquear",
            Description = "Se ejecuta cuando se bloquea la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDoorUnlock",
            DisplayName = "Puertas: Al Desbloquear",
            Description = "Se ejecuta cuando se desbloquea la puerta",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Door" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === NPC EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTalk",
            DisplayName = "NPC: Al Hablar",
            Description = "Se ejecuta cuando el jugador habla con el NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcAttack",
            DisplayName = "Combate: Al Atacar NPC",
            Description = "Se ejecuta cuando el jugador ataca al NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcDeath",
            DisplayName = "Combate: Al Morir NPC",
            Description = "Se ejecuta cuando el NPC muere",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcSee",
            DisplayName = "NPC: Al Ver Jugador",
            Description = "Se ejecuta cuando el NPC ve al jugador entrar en su sala",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === COMBAT EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnCombatStart",
            DisplayName = "Combate: Al Iniciar",
            Description = "Se ejecuta cuando el jugador inicia combate con este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnCombatVictory",
            DisplayName = "Combate: Al Ganar",
            Description = "Se ejecuta cuando el jugador vence a este NPC en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnCombatDefeat",
            DisplayName = "Combate: Al Perder",
            Description = "Se ejecuta cuando el NPC vence al jugador en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnCombatFlee",
            DisplayName = "Combate: Al Huir",
            Description = "Se ejecuta cuando el jugador huye del combate con este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnPlayerAttack",
            DisplayName = "Combate: Al Atacar Jugador",
            Description = "Se ejecuta cuando el jugador realiza un ataque en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc", "Game" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Damage", PortType = PortType.Data, DataType = "int", Label = "Daño" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnNpcTurn",
            DisplayName = "Combate: Al Turno del NPC",
            Description = "Se ejecuta cuando es el turno del NPC en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Round", PortType = PortType.Data, DataType = "int", Label = "Ronda" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnPlayerDefend",
            DisplayName = "Combate: Al Defender Jugador",
            Description = "Se ejecuta cuando el jugador elige defenderse en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc", "Game" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnCriticalHit",
            DisplayName = "Combate: Al Golpe Crítico",
            Description = "Se ejecuta cuando ocurre un golpe crítico en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc", "Game" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Damage", PortType = PortType.Data, DataType = "int", Label = "Daño" },
                new NodePort { Name = "IsPlayerCrit", PortType = PortType.Data, DataType = "bool", Label = "EsDelJugador" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnMiss",
            DisplayName = "Combate: Al Fallar Ataque",
            Description = "Se ejecuta cuando un ataque falla en combate",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc", "Game" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "IsPlayerMiss", PortType = PortType.Data, DataType = "bool", Label = "EsDelJugador" }
            }
        });

        // === TRADE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTradeStart",
            DisplayName = "Dinero: Al Iniciar Comercio",
            Description = "Se ejecuta cuando el jugador inicia comercio con este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTradeEnd",
            DisplayName = "Dinero: Al Cerrar Comercio",
            Description = "Se ejecuta cuando el jugador cierra el comercio con este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnItemBought",
            DisplayName = "Dinero: Al Comprar",
            Description = "Se ejecuta cuando el jugador compra un item de este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectId", PortType = PortType.Data, DataType = "string", Label = "Objeto" },
                new NodePort { Name = "Price", PortType = PortType.Data, DataType = "int", Label = "Precio" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnItemSold",
            DisplayName = "Dinero: Al Vender",
            Description = "Se ejecuta cuando el jugador vende un item a este NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Npc" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectId", PortType = PortType.Data, DataType = "string", Label = "Objeto" },
                new NodePort { Name = "Price", PortType = PortType.Data, DataType = "int", Label = "Precio" }
            }
        });

        // === OBJECT EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnTake",
            DisplayName = "Objetos: Al Coger",
            Description = "Se ejecuta cuando el jugador coge el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDrop",
            DisplayName = "Objetos: Al Soltar",
            Description = "Se ejecuta cuando el jugador suelta el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnUse",
            DisplayName = "Objetos: Al Usar",
            Description = "Se ejecuta cuando el jugador usa el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnGive",
            DisplayName = "Objetos: Al Dar",
            Description = "Se ejecuta cuando el jugador da el objeto a un NPC",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnExamine",
            DisplayName = "Objetos: Al Examinar",
            Description = "Se ejecuta cuando el jugador examina el objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnContainerOpen",
            DisplayName = "Objetos: Al Abrir Contenedor",
            Description = "Se ejecuta cuando el jugador abre el contenedor",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnContainerClose",
            DisplayName = "Objetos: Al Cerrar Contenedor",
            Description = "Se ejecuta cuando el jugador cierra el contenedor",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === CONSUMABLE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnEat",
            DisplayName = "Necesidades: Al Comer",
            Description = "Se ejecuta cuando el jugador come este objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NutritionAmount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnDrink",
            DisplayName = "Necesidades: Al Beber",
            Description = "Se ejecuta cuando el jugador bebe este objeto",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "GameObject" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NutritionAmount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        // === EVENTOS DE SUEÑO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnSleep",
            DisplayName = "Necesidades: Al Dormir",
            Description = "Se ejecuta cuando el jugador comienza a dormir",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Hours", PortType = PortType.Data, DataType = "int", Label = "Horas" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnWakeUp",
            DisplayName = "Necesidades: Al Despertar",
            Description = "Se ejecuta cuando el jugador despierta normalmente",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "HoursSlept", PortType = PortType.Data, DataType = "int", Label = "Horas dormidas" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnWakeUpStartled",
            DisplayName = "Necesidades: Al Despertar Sobresaltado",
            Description = "Se ejecuta cuando el jugador despierta abruptamente (NPC entró, necesidad alta)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Reason", PortType = PortType.Data, DataType = "string", Label = "Razón" }
            }
        });

        // === QUEST EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestStart",
            DisplayName = "Juego: Al Iniciar Misión",
            Description = "Se ejecuta cuando se inicia la misión",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest", "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestComplete",
            DisplayName = "Juego: Al Completar Misión",
            Description = "Se ejecuta cuando se completa la misión",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest", "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnQuestFail",
            DisplayName = "Juego: Al Fallar Misión",
            Description = "Se ejecuta cuando se falla la misión",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest", "Game" },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnObjectiveComplete",
            DisplayName = "Juego: Al Completar Objetivo",
            Description = "Se ejecuta cuando se completa un objetivo de la misión",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Quest", "Game" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectiveIndex", PortType = PortType.Data, DataType = "int", Label = "Indice" }
            }
        });

        // === PLAYER STATE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnPlayerDeath",
            DisplayName = "Jugador: Al Morir",
            Description = "Se ejecuta cuando el jugador muere (salud llega a 0)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnHealthLow",
            DisplayName = "Jugador: Al Bajar Salud",
            Description = "Se ejecuta cuando la salud baja de un umbral (por defecto 25%)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentHealth", PortType = PortType.Data, DataType = "int", Label = "Salud" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral %", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnHealthCritical",
            DisplayName = "Jugador: Al Salud Crítica",
            Description = "Se ejecuta cuando la salud llega a nivel crítico (por defecto 10%)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentHealth", PortType = PortType.Data, DataType = "int", Label = "Salud" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral %", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnHungerHigh",
            DisplayName = "Necesidades: Al Tener Hambre",
            Description = "Se ejecuta cuando el hambre supera un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentHunger", PortType = PortType.Data, DataType = "int", Label = "Hambre" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 75 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnThirstHigh",
            DisplayName = "Necesidades: Al Tener Sed",
            Description = "Se ejecuta cuando la sed supera un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentThirst", PortType = PortType.Data, DataType = "int", Label = "Sed" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 75 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnEnergyLow",
            DisplayName = "Jugador: Al Estar Cansado",
            Description = "Se ejecuta cuando la energía baja de un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentEnergy", PortType = PortType.Data, DataType = "int", Label = "Energía" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnSleepHigh",
            DisplayName = "Necesidades: Al Necesitar Dormir",
            Description = "Se ejecuta cuando el nivel de cansancio supera un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "BasicNeeds",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentSleep", PortType = PortType.Data, DataType = "int", Label = "Sueño" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 75 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnSanityLow",
            DisplayName = "Jugador: Al Perder Cordura",
            Description = "Se ejecuta cuando la cordura baja de un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentSanity", PortType = PortType.Data, DataType = "int", Label = "Cordura" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnManaLow",
            DisplayName = "Jugador: Al Quedar Sin Mana",
            Description = "Se ejecuta cuando el mana baja de un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentMana", PortType = PortType.Data, DataType = "int", Label = "Mana" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnStateThreshold",
            DisplayName = "Jugador: Al Cruzar Umbral de Estado",
            Description = "Se ejecuta cuando cualquier estado cruza un umbral (genérico)",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentValue", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 50 },
                new NodePropertyDefinition { Name = "Direction", DisplayName = "Dirección", DataType = "select",
                    Options = new[] { "Below", "Above" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnModifierApplied",
            DisplayName = "Jugador: Al Aplicar Modificador",
            Description = "Se ejecuta cuando se aplica un modificador al jugador",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ModifierName", PortType = PortType.Data, DataType = "string", Label = "Nombre" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnModifierExpired",
            DisplayName = "Jugador: Al Expirar Modificador",
            Description = "Se ejecuta cuando un modificador expira",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            RequiredFeature = "Combat",
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ModifierName", PortType = PortType.Data, DataType = "string", Label = "Nombre" }
            }
        });

        // === MONEY EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnMoneyGained",
            DisplayName = "Dinero: Al Ganar",
            Description = "Se ejecuta cuando el jugador gana dinero",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnMoneyLost",
            DisplayName = "Dinero: Al Perder",
            Description = "Se ejecuta cuando el jugador pierde dinero",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnMoneyThreshold",
            DisplayName = "Dinero: Al Cruzar Umbral",
            Description = "Se ejecuta cuando el dinero cruza un umbral",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "CurrentMoney", PortType = PortType.Data, DataType = "int", Label = "Dinero" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 100 },
                new NodePropertyDefinition { Name = "Direction", DisplayName = "Dirección", DataType = "select",
                    Options = new[] { "Below", "Above" } }
            }
        });

        // === EVENTO DE CAMBIO DE PROPIEDAD ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Event_OnPropertyChanged",
            DisplayName = "Juego: Al Cambiar Propiedad",
            Description = "Se ejecuta cuando cambia el valor de una propiedad de una entidad",
            Category = NodeCategory.Event,
            OwnerTypes = new[] { "Game", "*" },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "EntityId", PortType = PortType.Data, DataType = "string", Label = "ID Entidad" },
                new NodePort { Name = "OldValue", PortType = PortType.Data, DataType = "string", Label = "Valor Anterior" },
                new NodePort { Name = "NewValue", PortType = PortType.Data, DataType = "string", Label = "Valor Nuevo" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "EntityType", DisplayName = "Tipo Entidad", DataType = "select",
                    Options = new[] { "Room", "Door", "Npc", "GameObject", "Player", "Game" } },
                new NodePropertyDefinition { Name = "PropertyName", DisplayName = "Propiedad", DataType = "string" }
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
            TypeId = "Condition_IsMainQuest",
            DisplayName = "Juego: Es Misión Principal",
            Description = "Verifica si una mision es principal o secundaria",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Principal" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "Secundaria" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" }
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
            TypeId = "Condition_IsDoorVisible",
            DisplayName = "Juego: Puerta Visible",
            Description = "Verifica si una puerta es visible para el jugador (considera Visible y requisitos de misiones)",
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
            TypeId = "Condition_IsObjectVisible",
            DisplayName = "Juego: Objeto Visible",
            Description = "Verifica si un objeto es visible",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
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
            TypeId = "Condition_IsObjectTakeable",
            DisplayName = "Juego: Objeto Cogible",
            Description = "Verifica si un objeto se puede coger",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
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
            TypeId = "Condition_IsContainerOpen",
            DisplayName = "Juego: Contenedor Abierto",
            Description = "Verifica si un contenedor está abierto",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsContainerLocked",
            DisplayName = "Juego: Contenedor Bloqueado",
            Description = "Verifica si un contenedor está bloqueado",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsWeather",
            DisplayName = "Juego: Es Clima",
            Description = "Verifica si el clima actual es el especificado",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Weather", DisplayName = "Clima", DataType = "select", Options = new[] { "Despejado", "Nublado", "Lluvia", "Tormenta", "Nieve", "Niebla" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_ObjectInContainer",
            DisplayName = "Juego: Objeto en Contenedor",
            Description = "Verifica si un objeto está dentro de un contenedor",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "ContainerId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_ObjectInRoom",
            DisplayName = "Juego: Objeto en Sala",
            Description = "Verifica si un objeto está en una sala específica",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_NpcInRoom",
            DisplayName = "Juego: NPC en Sala",
            Description = "Verifica si un NPC está en una sala específica",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Si" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" }
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

        // === SUBGRUPO: ESTADOS DEL JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerStateAbove",
            DisplayName = "Estado: Mayor Que",
            Description = "Verifica si un estado del jugador está por encima de un umbral",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 50 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerStateBelow",
            DisplayName = "Estado: Menor Que",
            Description = "Verifica si un estado del jugador está por debajo de un umbral",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerStateEquals",
            DisplayName = "Estado: Igual A",
            Description = "Verifica si un estado del jugador es igual a un valor",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerStateBetween",
            DisplayName = "Estado: Entre Valores",
            Description = "Verifica si un estado del jugador está entre dos valores",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "MinValue", DisplayName = "Mínimo", DataType = "int", DefaultValue = 25 },
                new NodePropertyDefinition { Name = "MaxValue", DisplayName = "Máximo", DataType = "int", DefaultValue = 75 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_HasModifier",
            DisplayName = "Estado: Tiene Modificador",
            Description = "Verifica si el jugador tiene un modificador activo por nombre",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "ModifierName", DisplayName = "Nombre del Modificador", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_HasModifierForState",
            DisplayName = "Estado: Tiene Modificador de Tipo",
            Description = "Verifica si el jugador tiene un modificador activo que afecte a un estado específico",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsPlayerAlive",
            DisplayName = "Estado: Jugador Vivo",
            Description = "Verifica si el jugador está vivo (salud > 0)",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Vivo" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "Muerto" }
            }
        });

        // === NPC COMBAT CONDITIONS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsNpcAlive",
            DisplayName = "Combate: NPC Vivo",
            Description = "Verifica si el NPC está vivo (salud > 0)",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Vivo" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "Muerto" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_NpcHealthBelow",
            DisplayName = "Combate: Salud NPC Baja",
            Description = "Verifica si la salud del NPC está por debajo de un umbral",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral (%)", DataType = "int", DefaultValue = 50 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsInCombat",
            DisplayName = "Combate: En Combate",
            Description = "Verifica si hay un combate activo",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Sí" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            }
        });

        // === SUBGRUPO: COMBATE ADICIONAL ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerHealthBelow",
            DisplayName = "Combate: Salud Jugador Baja",
            Description = "Verifica si la salud del jugador está por debajo de un porcentaje",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral (%)", DataType = "int", DefaultValue = 50 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerHealthAbove",
            DisplayName = "Combate: Salud Jugador Alta",
            Description = "Verifica si la salud del jugador está por encima de un porcentaje",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "Threshold", DisplayName = "Umbral (%)", DataType = "int", DefaultValue = 50 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerHasWeaponType",
            DisplayName = "Combate: Tiene Tipo de Arma",
            Description = "Verifica si el jugador tiene equipada un arma del tipo especificado",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "DamageType", DisplayName = "Tipo de Daño", DataType = "select", Options = new[] { "Physical", "Magical" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerHasArmor",
            DisplayName = "Combate: Tiene Armadura",
            Description = "Verifica si el jugador tiene armadura equipada",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Sí" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsCombatRound",
            DisplayName = "Combate: Es Ronda X",
            Description = "Verifica si es la ronda especificada del combate",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "Round", DisplayName = "Ronda", DataType = "int", DefaultValue = 1 }
            }
        });

        // === SUBGRUPO: COMERCIO ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsInTrade",
            DisplayName = "Comercio: En Comercio",
            Description = "Verifica si hay un comercio activo",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Sí" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerHasMoney",
            DisplayName = "Comercio: Jugador Tiene Dinero",
            Description = "Verifica si el jugador tiene al menos X cantidad de dinero",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_NpcHasMoney",
            DisplayName = "Comercio: NPC Tiene Dinero",
            Description = "Verifica si el NPC tiene al menos X cantidad de dinero",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_NpcHasInfiniteMoney",
            DisplayName = "Comercio: NPC Tiene Dinero Infinito",
            Description = "Verifica si el NPC tiene dinero infinito",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_PlayerOwnsItem",
            DisplayName = "Comercio: Jugador Posee Items",
            Description = "Verifica si el jugador tiene al menos X unidades de un objeto",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "entity", EntityType = "GameObject", IsRequired = true },
                new NodePropertyDefinition { Name = "Quantity", DisplayName = "Cantidad", DataType = "int", DefaultValue = 1 }
            }
        });

        // === COMPARACIÓN GENÉRICA DE PROPIEDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_CompareProperty",
            DisplayName = "Operadores: Comparar Propiedad",
            Description = "Compara el valor de una propiedad de cualquier entidad",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "EntityType", DisplayName = "Tipo Entidad", DataType = "select",
                    Options = new[] { "Room", "Door", "Npc", "GameObject", "Player", "Game" } },
                new NodePropertyDefinition { Name = "EntityId", DisplayName = "Entidad", DataType = "string" },
                new NodePropertyDefinition { Name = "PropertyName", DisplayName = "Propiedad", DataType = "string" },
                new NodePropertyDefinition { Name = "Operator", DisplayName = "Operador", DataType = "select",
                    Options = new[] { "==", "!=", "<", "<=", ">", ">=" }, DefaultValue = "==" },
                new NodePropertyDefinition { Name = "CompareValue", DisplayName = "Valor a Comparar", DataType = "string" }
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
            DisplayName = "Juego: Mostrar Mensaje",
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
            DisplayName = "Jugador: Dar Objeto",
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
            DisplayName = "Jugador: Quitar Objeto",
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
            DisplayName = "Jugador: Teletransportar",
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

        // === ROOM ACTIONS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetRoomIllumination",
            DisplayName = "Iluminación: Sala",
            Description = "Enciende o apaga la iluminación de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" },
                new NodePropertyDefinition { Name = "IsIlluminated", DisplayName = "Iluminada", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetRoomMusic",
            DisplayName = "Juego: Música de Sala",
            Description = "Cambia la música de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" },
                new NodePropertyDefinition { Name = "MusicId", DisplayName = "Música", DataType = "string", EntityType = "Music" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetRoomDescription",
            DisplayName = "Juego: Descripción Sala",
            Description = "Cambia la descripción de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala", DataType = "string", EntityType = "Room" },
                new NodePropertyDefinition { Name = "Description", DisplayName = "Descripción", DataType = "multiline" }
            }
        });

        // === GAME STATE ACTIONS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetWeather",
            DisplayName = "Juego: Cambiar Clima",
            Description = "Cambia el clima del juego",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Weather", DisplayName = "Clima", DataType = "select", Options = new[] { "Despejado", "Nublado", "Lluvia", "Tormenta", "Nieve", "Niebla" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetGameHour",
            DisplayName = "Juego: Establecer Hora",
            Description = "Establece la hora del juego",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Hour", DisplayName = "Hora (0-23)", DataType = "int", DefaultValue = 12 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AdvanceTime",
            DisplayName = "Juego: Avanzar Tiempo",
            Description = "Avanza el tiempo del juego",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Hours", DisplayName = "Horas", DataType = "int", DefaultValue = 1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_MoveNpc",
            DisplayName = "NPC: Mover",
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
            DisplayName = "Juego: Establecer Flag",
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
            DisplayName = "Juego: Establecer Contador",
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
            DisplayName = "Juego: Incrementar Contador",
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
            DisplayName = "Juego: Reproducir Sonido",
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
            DisplayName = "Juego: Iniciar Misión",
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
            DisplayName = "Juego: Completar Misión",
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
            DisplayName = "Juego: Fallar Misión",
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
            TypeId = "Action_SetQuestStatus",
            DisplayName = "Juego: Cambiar Estado Misión",
            Description = "Cambia el estado de una mision a cualquier valor",
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
                new NodePropertyDefinition { Name = "QuestId", DisplayName = "Mision", DataType = "string", EntityType = "Quest" },
                new NodePropertyDefinition { Name = "Status", DisplayName = "Estado", DataType = "select", Options = new[] { "NotStarted", "InProgress", "Completed", "Failed" }, DefaultValue = "InProgress" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AdvanceObjective",
            DisplayName = "Juego: Avanzar Objetivo",
            Description = "Avanza al siguiente objetivo de una mision",
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
            DisplayName = "Juego: Abrir Puerta",
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
            DisplayName = "Juego: Cerrar Puerta",
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
            DisplayName = "Juego: Bloquear Puerta",
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
            DisplayName = "Juego: Desbloquear Puerta",
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
            TypeId = "Action_SetDoorVisible",
            DisplayName = "Juego: Visibilidad Puerta",
            Description = "Muestra u oculta una puerta y sus salidas asociadas",
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
                new NodePropertyDefinition { Name = "DoorId", DisplayName = "Puerta", DataType = "string", EntityType = "Door" },
                new NodePropertyDefinition { Name = "Visible", DisplayName = "Visible", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcVisible",
            DisplayName = "NPC: Visibilidad",
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
            DisplayName = "Objetos: Visibilidad",
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
            TypeId = "Action_SetObjectTakeable",
            DisplayName = "Objetos: Cogible",
            Description = "Permite o impide coger un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "CanTake", DisplayName = "Se puede coger", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_OpenContainer",
            DisplayName = "Objetos: Abrir Contenedor",
            Description = "Abre un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_CloseContainer",
            DisplayName = "Objetos: Cerrar Contenedor",
            Description = "Cierra un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_LockContainer",
            DisplayName = "Objetos: Bloquear Contenedor",
            Description = "Bloquea un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_UnlockContainer",
            DisplayName = "Objetos: Desbloquear Contenedor",
            Description = "Desbloquea un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetContentsVisible",
            DisplayName = "Objetos: Visibilidad Contenido",
            Description = "Muestra u oculta el contenido de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Visible", DisplayName = "Visible", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetObjectPrice",
            DisplayName = "Objetos: Precio",
            Description = "Establece el precio de un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Price", DisplayName = "Precio", DataType = "int", DefaultValue = 0 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetObjectDurability",
            DisplayName = "Objetos: Durabilidad",
            Description = "Establece la durabilidad actual de un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Durability", DisplayName = "Durabilidad", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_MoveObjectToRoom",
            DisplayName = "Objetos: Mover a Sala",
            Description = "Mueve un objeto a una sala específica",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "RoomId", DisplayName = "Sala destino", DataType = "string", EntityType = "Room" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_PutObjectInContainer",
            DisplayName = "Objetos: Poner en Contenedor",
            Description = "Pone un objeto dentro de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "ContainerId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveObjectFromContainer",
            DisplayName = "Objetos: Sacar de Contenedor",
            Description = "Saca un objeto de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "ContainerId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        // === NODOS DE ILUMINACIÓN ===

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetObjectLit",
            DisplayName = "Iluminación: Encender/Apagar Objeto",
            Description = "Enciende o apaga un objeto luminoso",
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
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto luminoso", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "IsLit", DisplayName = "Encendido", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetLightTurns",
            DisplayName = "Iluminación: Turnos de Luz",
            Description = "Establece los turnos de luz restantes de un objeto luminoso (-1 = infinito)",
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
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto luminoso", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Turns", DisplayName = "Turnos (-1 = infinito)", DataType = "int", DefaultValue = -1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsObjectLit",
            DisplayName = "Iluminación: Objeto Encendido",
            Description = "Comprueba si un objeto luminoso está encendido",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
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
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto luminoso", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Condition_IsRoomLit",
            DisplayName = "Iluminación: Sala Iluminada",
            Description = "Comprueba si la sala actual está iluminada (por la sala misma o por fuentes de luz)",
            Category = NodeCategory.Condition,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "True", PortType = PortType.Execution, Label = "Sí" },
                new NodePort { Name = "False", PortType = PortType.Execution, Label = "No" }
            },
            Properties = Array.Empty<NodePropertyDefinition>()
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddMoney",
            DisplayName = "Dinero: Dar Oro",
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
            TypeId = "Action_RemoveMoney",
            DisplayName = "Dinero: Quitar Oro",
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
            DisplayName = "NPC: Rutas: Iniciar Patrulla",
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
            DisplayName = "NPC: Rutas: Detener Patrulla",
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
            DisplayName = "NPC: Rutas: Paso de Patrulla",
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
            DisplayName = "NPC: Rutas: Modo de Patrulla",
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
            DisplayName = "NPC: Rutas: Seguir Jugador",
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
            DisplayName = "NPC: Rutas: Dejar de Seguir",
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
            DisplayName = "NPC: Rutas: Modo de Seguimiento",
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

        // === ESTADOS DEL JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetPlayerState",
            DisplayName = "Jugador: Establecer Estado",
            Description = "Establece el valor de un estado del jugador",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ModifyPlayerState",
            DisplayName = "Jugador: Modificar Estado",
            Description = "Añade o resta al valor de un estado del jugador",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_HealPlayer",
            DisplayName = "Jugador: Curar",
            Description = "Restaura salud al jugador (sin exceder el máximo)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_DamagePlayer",
            DisplayName = "Jugador: Dañar",
            Description = "Inflige daño al jugador (reduce salud)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "PlayerDied", PortType = PortType.Execution, Label = "Murió" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Daño", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RestoreMana",
            DisplayName = "Jugador: Restaurar Mana",
            Description = "Restaura mana al jugador (sin exceder el máximo)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ConsumeMana",
            DisplayName = "Jugador: Consumir Mana",
            Description = "Consume mana del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NotEnough", PortType = PortType.Execution, Label = "Insuficiente" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 10 }
            }
        });

        // === NPC COMBAT ACTIONS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StartCombat",
            DisplayName = "Combate: Iniciar",
            Description = "Inicia un combate con el NPC especificado",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_DamageNpc",
            DisplayName = "Combate: Dañar NPC",
            Description = "Causa daño a un NPC (reduce su salud)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "OnDeath", PortType = PortType.Execution, Label = "Si muere" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Daño", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_HealNpc",
            DisplayName = "Combate: Curar NPC",
            Description = "Restaura salud de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Curación", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcMaxHealth",
            DisplayName = "Combate: Salud Máxima NPC",
            Description = "Establece la salud máxima de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "MaxHealth", DisplayName = "Salud Máxima", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ReviveNpc",
            DisplayName = "Combate: Revivir NPC",
            Description = "Revive un NPC muerto",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "HealthPercent", DisplayName = "% Salud", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_KillNpc",
            DisplayName = "Combate: Matar NPC",
            Description = "Mata instantáneamente un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetPatrolRoute",
            DisplayName = "NPC: Rutas: Ruta de Patrulla",
            Description = "Establece la ruta de patrulla de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Route", DisplayName = "Ruta (IDs separados por coma)", DataType = "string" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddItemToNpcInventory",
            DisplayName = "NPC: Dar Item",
            Description = "Añade un objeto al inventario de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveItemFromNpcInventory",
            DisplayName = "NPC: Quitar Item",
            Description = "Quita un objeto del inventario de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetPlayerMaxHealth",
            DisplayName = "Combate: Establecer Salud Máxima",
            Description = "Establece la salud máxima del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "MaxHealth", DisplayName = "Salud Máxima", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcAttack",
            DisplayName = "Combate: Cambiar Ataque NPC",
            Description = "Cambia el valor de ataque de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Attack", DisplayName = "Ataque", DataType = "int", DefaultValue = 10 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcDefense",
            DisplayName = "Combate: Cambiar Defensa NPC",
            Description = "Cambia el valor de defensa de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Defense", DisplayName = "Defensa", DataType = "int", DefaultValue = 5 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_EndCombatVictory",
            DisplayName = "Combate: Forzar Victoria",
            Description = "Termina el combate actual con victoria del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_EndCombatDefeat",
            DisplayName = "Combate: Forzar Derrota",
            Description = "Termina el combate actual con derrota del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ForceFlee",
            DisplayName = "Combate: Forzar Huida",
            Description = "Fuerza la huida del combate actual",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === TRADE ACTIONS ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_OpenTrade",
            DisplayName = "Comercio: Abrir",
            Description = "Abre una sesión de comercio con el NPC especificado",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_CloseTrade",
            DisplayName = "Comercio: Cerrar",
            Description = "Cierra la sesión de comercio actual",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddPlayerMoney",
            DisplayName = "Dinero: Dar al Jugador",
            Description = "Añade dinero al jugador",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemovePlayerMoney",
            DisplayName = "Dinero: Quitar al Jugador",
            Description = "Quita dinero al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "OnInsufficient", PortType = PortType.Execution, Label = "Si no tiene" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 100 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNpcMoney",
            DisplayName = "Dinero: Establecer a NPC",
            Description = "Establece el dinero del NPC (-1 para infinito)",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Money", DisplayName = "Dinero", DataType = "int", DefaultValue = -1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddNpcItem",
            DisplayName = "Comercio: Añadir Item a NPC",
            Description = "Añade un item al inventario de la tienda del NPC",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "entity", EntityType = "GameObject", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveNpcItem",
            DisplayName = "Comercio: Quitar Item de NPC",
            Description = "Quita un item del inventario de la tienda del NPC",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "entity", EntityType = "GameObject", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetBuyMultiplier",
            DisplayName = "Comercio: Cambiar Multiplicador Compra",
            Description = "Cambia el multiplicador de compra del NPC (lo que paga al jugador)",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Multiplier", DisplayName = "Multiplicador", DataType = "float", DefaultValue = 0.5 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetSellMultiplier",
            DisplayName = "Comercio: Cambiar Multiplicador Venta",
            Description = "Cambia el multiplicador de venta del NPC (lo que cobra al jugador)",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "Multiplier", DisplayName = "Multiplicador", DataType = "float", DefaultValue = 1.0 }
            }
        });

        // === HABILIDADES DE COMBATE ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddAbility",
            DisplayName = "Habilidad: Añadir al Jugador",
            Description = "Otorga una habilidad de combate al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "AbilityId", DisplayName = "Habilidad", DataType = "entity", EntityType = "Ability", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveAbility",
            DisplayName = "Habilidad: Quitar al Jugador",
            Description = "Quita una habilidad de combate del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "AbilityId", DisplayName = "Habilidad", DataType = "entity", EntityType = "Ability", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_AddAbilityToNpc",
            DisplayName = "Habilidad: Añadir a NPC",
            Description = "Otorga una habilidad de combate a un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "AbilityId", DisplayName = "Habilidad", DataType = "entity", EntityType = "Ability", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveAbilityFromNpc",
            DisplayName = "Habilidad: Quitar a NPC",
            Description = "Quita una habilidad de combate de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "entity", EntityType = "Npc", IsRequired = true },
                new NodePropertyDefinition { Name = "AbilityId", DisplayName = "Habilidad", DataType = "entity", EntityType = "Ability", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_FeedPlayer",
            DisplayName = "Necesidades: Alimentar Jugador",
            Description = "Reduce el hambre del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Reducción Hambre", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_HydratePlayer",
            DisplayName = "Necesidades: Hidratar Jugador",
            Description = "Reduce la sed del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Reducción Sed", DataType = "int", DefaultValue = 25 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RestPlayer",
            DisplayName = "Necesidades: Descansar Jugador",
            Description = "Reduce el cansancio del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
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
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Energía Restaurada", DataType = "int", DefaultValue = 50 }
            }
        });

        // === VELOCIDAD DE NECESIDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetNeedRate",
            DisplayName = "Necesidades: Cambiar Velocidad",
            Description = "Cambia la velocidad de incremento de una necesidad (hambre, sed o sueño)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
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
                new NodePropertyDefinition { Name = "NeedType", DisplayName = "Necesidad", DataType = "select",
                    Options = new[] { "Hunger", "Thirst", "Sleep" } },
                new NodePropertyDefinition { Name = "Rate", DisplayName = "Velocidad", DataType = "select",
                    Options = new[] { "Low", "Normal", "High" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RestoreAllStats",
            DisplayName = "Jugador: Restaurar Todo",
            Description = "Restaura todos los estados del jugador a sus valores máximos/óptimos",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === MODIFICADORES TEMPORALES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ApplyModifier",
            DisplayName = "Modificador: Aplicar",
            Description = "Aplica un modificador temporal a un estado del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "ModifierName", DisplayName = "Nombre", DataType = "string", IsRequired = true },
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Estado a Modificar", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "int", DefaultValue = 5 },
                new NodePropertyDefinition { Name = "DurationType", DisplayName = "Tipo Duración", DataType = "select",
                    Options = new[] { "Turns", "Seconds", "Permanent" } },
                new NodePropertyDefinition { Name = "Duration", DisplayName = "Duración", DataType = "int", DefaultValue = 5 },
                new NodePropertyDefinition { Name = "IsRecurring", DisplayName = "Se Aplica Cada Turno/Segundo", DataType = "bool", DefaultValue = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveModifier",
            DisplayName = "Modificador: Eliminar por Nombre",
            Description = "Elimina un modificador temporal específico por nombre",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "ModifierName", DisplayName = "Nombre del Modificador", DataType = "string", IsRequired = true }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveModifiersByState",
            DisplayName = "Modificador: Eliminar por Estado",
            Description = "Elimina todos los modificadores que afectan a un estado específico",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
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
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_RemoveAllModifiers",
            DisplayName = "Modificador: Eliminar Todos",
            Description = "Elimina todos los modificadores temporales activos",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ProcessModifiers",
            DisplayName = "Modificador: Procesar Tick",
            Description = "Procesa todos los modificadores activos (aplica efectos recurrentes y elimina expirados)",
            Category = NodeCategory.Action,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            },
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "PlayerDied", PortType = PortType.Execution, Label = "Murió" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_StartConversation",
            DisplayName = "Juego: Iniciar Conversación",
            Description = "Inicia la conversación con un NPC",
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

        // === ACCESO GENÉRICO A PROPIEDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Action_SetProperty",
            DisplayName = "Operadores: Establecer Propiedad",
            Description = "Establece el valor de una propiedad de cualquier entidad",
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
                new NodePropertyDefinition { Name = "EntityType", DisplayName = "Tipo Entidad", DataType = "select",
                    Options = new[] { "Room", "Door", "Npc", "GameObject", "Player", "Game" } },
                new NodePropertyDefinition { Name = "EntityId", DisplayName = "Entidad", DataType = "string" },
                new NodePropertyDefinition { Name = "PropertyName", DisplayName = "Propiedad", DataType = "string" },
                new NodePropertyDefinition { Name = "Value", DisplayName = "Valor", DataType = "string" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Action_ModifyProperty",
            DisplayName = "Operadores: Modificar Propiedad Numérica",
            Description = "Modifica el valor numérico de una propiedad (suma, resta, multiplica o divide)",
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
                new NodePropertyDefinition { Name = "EntityType", DisplayName = "Tipo Entidad", DataType = "select",
                    Options = new[] { "Room", "Door", "Npc", "GameObject", "Player", "Game" } },
                new NodePropertyDefinition { Name = "EntityId", DisplayName = "Entidad", DataType = "string" },
                new NodePropertyDefinition { Name = "PropertyName", DisplayName = "Propiedad", DataType = "string" },
                new NodePropertyDefinition { Name = "Operation", DisplayName = "Operación", DataType = "select",
                    Options = new[] { "Add", "Subtract", "Multiply", "Divide" }, DefaultValue = "Add" },
                new NodePropertyDefinition { Name = "Amount", DisplayName = "Cantidad", DataType = "double", DefaultValue = 1.0 }
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
            TypeId = "Variable_GetPlayerMoney",
            DisplayName = "Juego: Oro del Jugador",
            Description = "Obtiene el dinero actual del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Money", PortType = PortType.Data, DataType = "int", Label = "Dinero" }
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

        // === SUBGRUPO: ESTADOS DINÁMICOS DEL JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerHealth",
            DisplayName = "Estado: Salud",
            Description = "Obtiene la salud actual del jugador (0-100)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Salud" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerMaxHealth",
            DisplayName = "Estado: Salud Máxima",
            Description = "Obtiene la salud máxima del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Salud Máx" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerHunger",
            DisplayName = "Estado: Hambre",
            Description = "Obtiene el nivel de hambre del jugador (0=lleno, 100=muriendo)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Hambre" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerThirst",
            DisplayName = "Estado: Sed",
            Description = "Obtiene el nivel de sed del jugador (0=hidratado, 100=deshidratado)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Sed" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerEnergy",
            DisplayName = "Estado: Energía",
            Description = "Obtiene el nivel de energía del jugador (0=exhausto, 100=descansado)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Energía" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerSleep",
            DisplayName = "Estado: Sueño",
            Description = "Obtiene el nivel de sueño/cansancio del jugador (0=descansado, 100=agotado)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Sueño" }
            }
        });

        // === VELOCIDAD DE NECESIDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetNeedRate",
            DisplayName = "Necesidades: Obtener Velocidad",
            Description = "Obtiene la velocidad de incremento de una necesidad (0=Lento, 1=Normal, 2=Rápido)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "BasicNeeds",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Velocidad" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NeedType", DisplayName = "Necesidad", DataType = "select",
                    Options = new[] { "Hunger", "Thirst", "Sleep" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerSanity",
            DisplayName = "Estado: Cordura",
            Description = "Obtiene el nivel de cordura del jugador (0=locura, 100=cuerdo)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Cordura" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerMana",
            DisplayName = "Estado: Mana",
            Description = "Obtiene el nivel de mana del jugador (0-100)",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Mana" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerMaxMana",
            DisplayName = "Estado: Mana Máximo",
            Description = "Obtiene el mana máximo del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Mana Máx" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetPlayerState",
            DisplayName = "Estado: Obtener Estado (Genérico)",
            Description = "Obtiene el valor de cualquier estado del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            RequiredFeature = "Combat",
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "StateType", DisplayName = "Tipo de Estado", DataType = "select",
                    Options = new[] { "Health", "MaxHealth", "Hunger", "Thirst", "Energy", "Sanity", "Mana", "MaxMana",
                                      "Strength", "Constitution", "Intelligence", "Dexterity", "Charisma", "Money" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetActiveModifiersCount",
            DisplayName = "Estado: Número de Modificadores",
            Description = "Obtiene el número de modificadores temporales activos",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_HasModifier",
            DisplayName = "Estado: Tiene Modificador",
            Description = "Verifica si el jugador tiene un modificador activo por nombre",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "bool", Label = "Tiene" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ModifierName", DisplayName = "Nombre del Modificador", DataType = "string", IsRequired = true }
            }
        });

        // === ACCESO GENÉRICO A PROPIEDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = "Variable_GetProperty",
            DisplayName = "Operadores: Obtener Propiedad",
            Description = "Obtiene el valor de cualquier propiedad de una entidad",
            Category = NodeCategory.Variable,
            OwnerTypes = new[] { "*" },
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "string", Label = "Valor" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "EntityType", DisplayName = "Tipo Entidad", DataType = "select",
                    Options = new[] { "Room", "Door", "Npc", "GameObject", "Player", "Game" } },
                new NodePropertyDefinition { Name = "EntityId", DisplayName = "Entidad", DataType = "string" },
                new NodePropertyDefinition { Name = "PropertyName", DisplayName = "Propiedad", DataType = "string" }
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
            TypeId = "Compare_PlayerMoney",
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
                    Options = new[] { "HasFlag", "HasItem", "HasMoney", "QuestStatus", "VisitedNode" }
                },
                new NodePropertyDefinition { Name = "FlagName", DisplayName = "Nombre del Flag", DataType = "string", DefaultValue = "" },
                new NodePropertyDefinition { Name = "ItemId", DisplayName = "Objeto", DataType = "string", DefaultValue = "", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "MoneyAmount", DisplayName = "Cantidad de Dinero", DataType = "int", DefaultValue = 0 },
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
                    Options = new[] { "GiveItem", "RemoveItem", "AddMoney", "RemoveMoney", "SetFlag", "StartQuest", "CompleteQuest", "ShowMessage" }
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
                new NodePort { Name = "NotEnoughMoney", PortType = PortType.Execution, Label = "Sin Dinero" },
                new NodePort { Name = "Cancelled", PortType = PortType.Execution, Label = "Cancelado" }
            },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", DefaultValue = "", EntityType = "GameObject", IsRequired = true },
                new NodePropertyDefinition { Name = "Price", DisplayName = "Precio", DataType = "int", DefaultValue = 10, IsRequired = true },
                new NodePropertyDefinition { Name = "ConfirmText", DisplayName = "Texto Confirmación", DataType = "string", DefaultValue = "¿Comprar por {precio}?" }
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
    }

    #endregion
}
