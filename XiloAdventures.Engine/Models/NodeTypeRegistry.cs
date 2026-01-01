using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Registro de tipos de nodos disponibles en el editor de scripts.
/// </summary>
public static class NodeTypeRegistry
{
    private static readonly Dictionary<NodeTypeId, NodeTypeDefinition> _types = new();

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

    public static IReadOnlyDictionary<NodeTypeId, NodeTypeDefinition> Types => _types;

    public static NodeTypeDefinition? GetNodeType(NodeTypeId typeId)
    {
        return _types.TryGetValue(typeId, out var def) ? def : null;
    }

    /// <summary>
    /// Obtiene un tipo de nodo por su nombre (string). Retrocompatibilidad.
    /// </summary>
    public static NodeTypeDefinition? GetNodeType(string typeIdString)
    {
        if (Enum.TryParse<NodeTypeId>(typeIdString, true, out var typeId))
            return GetNodeType(typeId);
        return null;
    }

    public static IEnumerable<NodeTypeDefinition> GetNodesForOwnerType(string ownerType)
    {
        return _types.Values.Where(n => n.OwnerTypes.Matches(ownerType));
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
            n.OwnerTypes.Matches(ownerType) &&
            IsFeatureEnabled(n.RequiredFeature, gameInfo));
    }

    /// <summary>
    /// Verifica si una característica requerida está habilitada.
    /// </summary>
    private static bool IsFeatureEnabled(RequiredFeature requiredFeature, GameInfo? gameInfo)
    {
        if (requiredFeature == RequiredFeature.None || gameInfo == null)
            return true;

        return requiredFeature switch
        {
            RequiredFeature.Combat => gameInfo.CombatEnabled,
            RequiredFeature.BasicNeeds => gameInfo.BasicNeedsEnabled,
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
            TypeId = NodeTypeId.Event_OnGameStart,
            DisplayName = "Al Iniciar",
            Description = "Se ejecuta cuando el jugador inicia una nueva partida",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnGameEnd,
            DisplayName = "Al Terminar",
            Description = "Se ejecuta cuando el jugador termina la partida (victoria o derrota)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_EveryMinute,
            DisplayName = "Cada Minuto",
            Description = "Se ejecuta cada minuto de tiempo de juego",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_EveryHour,
            DisplayName = "Cada Hora",
            Description = "Se ejecuta cada hora de tiempo de juego",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnTurnStart,
            DisplayName = "Al Inicio del Turno",
            Description = "Se ejecuta al inicio de cada turno del jugador",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "TurnNumber", PortType = PortType.Data, DataType = "int", Label = "Turno" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnWeatherChange,
            DisplayName = "Al Cambiar Clima",
            Description = "Se ejecuta cuando cambia el clima",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NewWeather", PortType = PortType.Data, DataType = "string", Label = "Clima" }
            }
        });

        // === ROOM EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnEnter,
            DisplayName = "Al Entrar",
            Description = "Se ejecuta cuando el jugador entra en la sala",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Salas,
            OwnerTypes = NodeOwnerType.Room,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnExit,
            DisplayName = "Al Salir",
            Description = "Se ejecuta cuando el jugador sale de la sala",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Salas,
            OwnerTypes = NodeOwnerType.Room,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Direction", PortType = PortType.Data, DataType = "string", Label = "Direccion" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnLook,
            DisplayName = "Al Mirar",
            Description = "Se ejecuta cuando el jugador mira/examina la sala (comando 'mirar')",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Salas,
            OwnerTypes = NodeOwnerType.Room,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === DOOR EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDoorOpen,
            DisplayName = "Al Abrir",
            Description = "Se ejecuta cuando se abre la puerta",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Puertas,
            OwnerTypes = NodeOwnerType.Door,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDoorClose,
            DisplayName = "Al Cerrar",
            Description = "Se ejecuta cuando se cierra la puerta",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Puertas,
            OwnerTypes = NodeOwnerType.Door,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDoorLock,
            DisplayName = "Al Bloquear",
            Description = "Se ejecuta cuando se bloquea la puerta",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Puertas,
            OwnerTypes = NodeOwnerType.Door,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDoorUnlock,
            DisplayName = "Al Desbloquear",
            Description = "Se ejecuta cuando se desbloquea la puerta",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Puertas,
            OwnerTypes = NodeOwnerType.Door,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === NPC EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnTalk,
            DisplayName = "Al Hablar",
            Description = "Se ejecuta cuando el jugador habla con el NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.NPC,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnNpcAttack,
            DisplayName = "Al Atacar NPC",
            Description = "Se ejecuta cuando el jugador ataca al NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnNpcDeath,
            DisplayName = "Al Morir NPC",
            Description = "Se ejecuta cuando el NPC muere",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnNpcSee,
            DisplayName = "Al Ver Jugador",
            Description = "Se ejecuta cuando el NPC ve al jugador entrar en su sala",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.NPC,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === COMBAT EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnCombatStart,
            DisplayName = "Al Iniciar",
            Description = "Se ejecuta cuando el jugador inicia combate con este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnCombatVictory,
            DisplayName = "Al Ganar",
            Description = "Se ejecuta cuando el jugador vence a este NPC en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnCombatDefeat,
            DisplayName = "Al Perder",
            Description = "Se ejecuta cuando el NPC vence al jugador en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnCombatFlee,
            DisplayName = "Al Huir",
            Description = "Se ejecuta cuando el jugador huye del combate con este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnPlayerAttack,
            DisplayName = "Al Atacar Jugador",
            Description = "Se ejecuta cuando el jugador realiza un ataque en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc | NodeOwnerType.Game,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Damage", PortType = PortType.Data, DataType = "int", Label = "Daño" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnNpcTurn,
            DisplayName = "Al Turno del NPC",
            Description = "Se ejecuta cuando es el turno del NPC en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Round", PortType = PortType.Data, DataType = "int", Label = "Ronda" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnPlayerDefend,
            DisplayName = "Al Defender Jugador",
            Description = "Se ejecuta cuando el jugador elige defenderse en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc | NodeOwnerType.Game,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnCriticalHit,
            DisplayName = "Al Golpe Crítico",
            Description = "Se ejecuta cuando ocurre un golpe crítico en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc | NodeOwnerType.Game,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Damage", PortType = PortType.Data, DataType = "int", Label = "Daño" },
                new NodePort { Name = "IsPlayerCrit", PortType = PortType.Data, DataType = "bool", Label = "EsDelJugador" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnMiss,
            DisplayName = "Al Fallar Ataque",
            Description = "Se ejecuta cuando un ataque falla en combate",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Combate,
            OwnerTypes = NodeOwnerType.Npc | NodeOwnerType.Game,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "IsPlayerMiss", PortType = PortType.Data, DataType = "bool", Label = "EsDelJugador" }
            }
        });

        // === TRADE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnTradeStart,
            DisplayName = "Al Iniciar Comercio",
            Description = "Se ejecuta cuando el jugador inicia comercio con este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnTradeEnd,
            DisplayName = "Al Cerrar Comercio",
            Description = "Se ejecuta cuando el jugador cierra el comercio con este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnItemBought,
            DisplayName = "Al Comprar",
            Description = "Se ejecuta cuando el jugador compra un item de este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectId", PortType = PortType.Data, DataType = "string", Label = "Objeto" },
                new NodePort { Name = "Price", PortType = PortType.Data, DataType = "int", Label = "Precio" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnItemSold,
            DisplayName = "Al Vender",
            Description = "Se ejecuta cuando el jugador vende un item a este NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Event_OnTake,
            DisplayName = "Al Coger",
            Description = "Se ejecuta cuando el jugador coge el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDrop,
            DisplayName = "Al Soltar",
            Description = "Se ejecuta cuando el jugador suelta el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnUse,
            DisplayName = "Al Usar",
            Description = "Se ejecuta cuando el jugador usa el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnGive,
            DisplayName = "Al Dar",
            Description = "Se ejecuta cuando el jugador da el objeto a un NPC",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnEquip,
            DisplayName = "Al Equipar",
            Description = "Se ejecuta cuando el jugador equipa el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnUnequip,
            DisplayName = "Al Desequipar",
            Description = "Se ejecuta cuando el jugador desequipa el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnExamine,
            DisplayName = "Al Examinar",
            Description = "Se ejecuta cuando el jugador examina el objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnContainerOpen,
            DisplayName = "Al Abrir Contenedor",
            Description = "Se ejecuta cuando el jugador abre el contenedor",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnContainerClose,
            DisplayName = "Al Cerrar Contenedor",
            Description = "Se ejecuta cuando el jugador cierra el contenedor",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Objetos,
            OwnerTypes = NodeOwnerType.GameObject,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        // === CONSUMABLE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnEat,
            DisplayName = "Al Comer",
            Description = "Se ejecuta cuando el jugador come este objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.GameObject,
            RequiredFeature = RequiredFeature.BasicNeeds,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NutritionAmount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnDrink,
            DisplayName = "Al Beber",
            Description = "Se ejecuta cuando el jugador bebe este objeto",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.GameObject,
            RequiredFeature = RequiredFeature.BasicNeeds,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "NutritionAmount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        // === EVENTOS DE SUEÑO ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnSleep,
            DisplayName = "Al Dormir",
            Description = "Se ejecuta cuando el jugador comienza a dormir",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Hours", PortType = PortType.Data, DataType = "int", Label = "Horas" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnWakeUp,
            DisplayName = "Al Despertar",
            Description = "Se ejecuta cuando el jugador despierta normalmente",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "HoursSlept", PortType = PortType.Data, DataType = "int", Label = "Horas dormidas" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnWakeUpStartled,
            DisplayName = "Al Despertar Sobresaltado",
            Description = "Se ejecuta cuando el jugador despierta abruptamente (NPC entró, necesidad alta)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Reason", PortType = PortType.Data, DataType = "string", Label = "Razón" }
            }
        });

        // === QUEST EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnQuestStart,
            DisplayName = "Al Iniciar Misión",
            Description = "Se ejecuta cuando se inicia la misión",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Quest | NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnQuestComplete,
            DisplayName = "Al Completar Misión",
            Description = "Se ejecuta cuando se completa la misión",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Quest | NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnQuestFail,
            DisplayName = "Al Fallar Misión",
            Description = "Se ejecuta cuando se falla la misión",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Quest | NodeOwnerType.Game,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnObjectiveComplete,
            DisplayName = "Al Completar Objetivo",
            Description = "Se ejecuta cuando se completa un objetivo de la misión",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Quest | NodeOwnerType.Game,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ObjectiveIndex", PortType = PortType.Data, DataType = "int", Label = "Indice" }
            }
        });

        // === PLAYER STATE EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnPlayerDeath,
            DisplayName = "Al Morir",
            Description = "Se ejecuta cuando el jugador muere (salud llega a 0)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnHealthLow,
            DisplayName = "Al Bajar Salud",
            Description = "Se ejecuta cuando la salud baja de un umbral (por defecto 25%)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Event_OnHealthCritical,
            DisplayName = "Al Salud Crítica",
            Description = "Se ejecuta cuando la salud llega a nivel crítico (por defecto 10%)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Event_OnHungerHigh,
            DisplayName = "Al Tener Hambre",
            Description = "Se ejecuta cuando el hambre supera un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Event_OnThirstHigh,
            DisplayName = "Al Tener Sed",
            Description = "Se ejecuta cuando la sed supera un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Event_OnEnergyLow,
            DisplayName = "Al Estar Cansado",
            Description = "Se ejecuta cuando la energía baja de un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Event_OnSleepHigh,
            DisplayName = "Al Necesitar Dormir",
            Description = "Se ejecuta cuando el nivel de cansancio supera un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Necesidades,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Event_OnSanityLow,
            DisplayName = "Al Perder Cordura",
            Description = "Se ejecuta cuando la cordura baja de un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Event_OnManaLow,
            DisplayName = "Al Quedar Sin Mana",
            Description = "Se ejecuta cuando el mana baja de un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Event_OnStateThreshold,
            DisplayName = "Al Cruzar Umbral de Estado",
            Description = "Se ejecuta cuando cualquier estado cruza un umbral (genérico)",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
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
            TypeId = NodeTypeId.Event_OnModifierApplied,
            DisplayName = "Al Aplicar Modificador",
            Description = "Se ejecuta cuando se aplica un modificador al jugador",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ModifierName", PortType = PortType.Data, DataType = "string", Label = "Nombre" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnModifierExpired,
            DisplayName = "Al Expirar Modificador",
            Description = "Se ejecuta cuando un modificador expira",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "ModifierName", PortType = PortType.Data, DataType = "string", Label = "Nombre" }
            }
        });

        // === MONEY EVENTS ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnMoneyGained,
            DisplayName = "Al Ganar",
            Description = "Se ejecuta cuando el jugador gana dinero",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnMoneyLost,
            DisplayName = "Al Perder",
            Description = "Se ejecuta cuando el jugador pierde dinero",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" },
                new NodePort { Name = "Amount", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Event_OnMoneyThreshold,
            DisplayName = "Al Cruzar Umbral",
            Description = "Se ejecuta cuando el dinero cruza un umbral",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
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
            TypeId = NodeTypeId.Event_OnPropertyChanged,
            DisplayName = "Al Cambiar Propiedad",
            Description = "Se ejecuta cuando cambia el valor de una propiedad de una entidad",
            Category = NodeCategory.Event,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.Game | NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_HasItem,
            DisplayName = "Tiene Objeto",
            Description = "Verifica si el jugador tiene un objeto en su inventario",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_IsInRoom,
            DisplayName = "Está en Sala",
            Description = "Verifica si el jugador esta en una sala especifica",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Juego,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_IsQuestStatus,
            DisplayName = "Estado de Misión",
            Description = "Verifica el estado de una mision",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsMainQuest,
            DisplayName = "Es Misión Principal",
            Description = "Verifica si una mision es principal o secundaria",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_HasFlag,
            DisplayName = "Tiene Flag",
            Description = "Verifica si un flag esta activo",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_CompareCounter,
            DisplayName = "Comparar Contador",
            Description = "Compara el valor de un contador",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsTimeOfDay,
            DisplayName = "Es Hora del Día",
            Description = "Verifica la hora del juego",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsDoorOpen,
            DisplayName = "Puerta Abierta",
            Description = "Verifica si una puerta esta abierta",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsDoorVisible,
            DisplayName = "Puerta Visible",
            Description = "Verifica si una puerta es visible para el jugador (considera Visible y requisitos de misiones)",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsNpcVisible,
            DisplayName = "NPC Visible",
            Description = "Verifica si un NPC es visible",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsObjectVisible,
            DisplayName = "Objeto Visible",
            Description = "Verifica si un objeto es visible",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsObjectTakeable,
            DisplayName = "Objeto Cogible",
            Description = "Verifica si un objeto se puede coger",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsContainerOpen,
            DisplayName = "Contenedor Abierto",
            Description = "Verifica si un contenedor está abierto",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsContainerLocked,
            DisplayName = "Contenedor Bloqueado",
            Description = "Verifica si un contenedor está bloqueado",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsWeather,
            DisplayName = "Es Clima",
            Description = "Verifica si el clima actual es el especificado",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_ObjectInContainer,
            DisplayName = "Objeto en Contenedor",
            Description = "Verifica si un objeto está dentro de un contenedor",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_ObjectInRoom,
            DisplayName = "Objeto en Sala",
            Description = "Verifica si un objeto está en una sala específica",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_NpcInRoom,
            DisplayName = "NPC en Sala",
            Description = "Verifica si un NPC está en una sala específica",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsPatrolling,
            DisplayName = "NPC Patrullando",
            Description = "Verifica si un NPC está patrullando",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_IsFollowingPlayer,
            DisplayName = "NPC Siguiendo",
            Description = "Verifica si un NPC está siguiendo al jugador",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Condition_Random,
            DisplayName = "Probabilidad",
            Description = "Se cumple con una probabilidad dada",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Condition_PlayerStateAbove,
            DisplayName = "Mayor Que",
            Description = "Verifica si un estado del jugador está por encima de un umbral",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerStateBelow,
            DisplayName = "Menor Que",
            Description = "Verifica si un estado del jugador está por debajo de un umbral",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerStateEquals,
            DisplayName = "Igual A",
            Description = "Verifica si un estado del jugador es igual a un valor",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerStateBetween,
            DisplayName = "Entre Valores",
            Description = "Verifica si un estado del jugador está entre dos valores",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_HasModifier,
            DisplayName = "Tiene Modificador",
            Description = "Verifica si el jugador tiene un modificador activo por nombre",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_HasModifierForState,
            DisplayName = "Tiene Modificador de Tipo",
            Description = "Verifica si el jugador tiene un modificador activo que afecte a un estado específico",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_IsPlayerAlive,
            DisplayName = "Jugador Vivo",
            Description = "Verifica si el jugador está vivo (salud > 0)",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_IsNpcAlive,
            DisplayName = "NPC Vivo",
            Description = "Verifica si el NPC está vivo (salud > 0)",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_NpcHealthBelow,
            DisplayName = "Salud NPC Baja",
            Description = "Verifica si la salud del NPC está por debajo de un umbral",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_IsInCombat,
            DisplayName = "En Combate",
            Description = "Verifica si hay un combate activo",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerHealthBelow,
            DisplayName = "Salud Jugador Baja",
            Description = "Verifica si la salud del jugador está por debajo de un porcentaje",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerHealthAbove,
            DisplayName = "Salud Jugador Alta",
            Description = "Verifica si la salud del jugador está por encima de un porcentaje",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerHasWeaponType,
            DisplayName = "Tiene Tipo de Arma",
            Description = "Verifica si el jugador tiene equipada un arma del tipo especificado",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_PlayerHasArmor,
            DisplayName = "Tiene Armadura",
            Description = "Verifica si el jugador tiene armadura equipada",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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

        // === SUBGRUPO: EQUIPAMIENTO ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_PlayerHasEquipped,
            DisplayName = "Jugador Tiene Equipado",
            Description = "Verifica si el jugador tiene un objeto específico equipado en un slot",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
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
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "object" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso", "Any" }, DefaultValue = "Any" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_NpcHasEquipped,
            DisplayName = "NPC Tiene Equipado",
            Description = "Verifica si un NPC tiene un objeto específico equipado en un slot",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "object" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso", "Any" }, DefaultValue = "Any" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_IsPlayerSlotEmpty,
            DisplayName = "Slot Jugador Vacío",
            Description = "Verifica si un slot de equipamiento del jugador está vacío",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
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
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_IsNpcSlotEmpty,
            DisplayName = "Slot NPC Vacío",
            Description = "Verifica si un slot de equipamiento de un NPC está vacío",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "npc" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_NpcHasItem,
            DisplayName = "NPC Tiene Objeto",
            Description = "Verifica si un NPC tiene un objeto específico en su inventario",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
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
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "object" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Condition_IsCombatRound,
            DisplayName = "Es Ronda X",
            Description = "Verifica si es la ronda especificada del combate",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Condition_IsInTrade,
            DisplayName = "En Comercio",
            Description = "Verifica si hay un comercio activo",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_PlayerHasMoney,
            DisplayName = "Jugador Tiene Dinero",
            Description = "Verifica si el jugador tiene al menos X cantidad de dinero",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_NpcHasMoney,
            DisplayName = "NPC Tiene Dinero",
            Description = "Verifica si el NPC tiene al menos X cantidad de dinero",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_NpcHasInfiniteMoney,
            DisplayName = "NPC Tiene Dinero Infinito",
            Description = "Verifica si el NPC tiene dinero infinito",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_PlayerOwnsItem,
            DisplayName = "Jugador Posee Items",
            Description = "Verifica si el jugador tiene al menos X unidades de un objeto",
            Category = NodeCategory.Condition,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Condition_CompareProperty,
            DisplayName = "Comparar Propiedad",
            Description = "Compara el valor de una propiedad de cualquier entidad",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Action_ShowMessage,
            DisplayName = "Mostrar Mensaje",
            Description = "Muestra un mensaje al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_GiveItem,
            DisplayName = "Dar Objeto",
            Description = "Da un objeto al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_RemoveItem,
            DisplayName = "Quitar Objeto",
            Description = "Quita un objeto del inventario del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_TeleportPlayer,
            DisplayName = "Teletransportar",
            Description = "Mueve al jugador a otra sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_SetRoomIllumination,
            DisplayName = "Sala",
            Description = "Enciende o apaga la iluminación de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Iluminacion,
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
            TypeId = NodeTypeId.Action_SetRoomMusic,
            DisplayName = "Música de Sala",
            Description = "Cambia la música de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetRoomDescription,
            DisplayName = "Descripción Sala",
            Description = "Cambia la descripción de una sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetWeather,
            DisplayName = "Cambiar Clima",
            Description = "Cambia el clima del juego",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Weather", DisplayName = "Clima", DataType = "select", Options = new[] { "Despejado", "Nublado", "Lluvia", "Tormenta", "Nieve", "Niebla" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_SetGameHour,
            DisplayName = "Establecer Hora",
            Description = "Establece la hora del juego",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Hour", DisplayName = "Hora (0-23)", DataType = "int", DefaultValue = 12 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_AdvanceTime,
            DisplayName = "Avanzar Tiempo",
            Description = "Avanza el tiempo del juego",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Hours", DisplayName = "Horas", DataType = "int", DefaultValue = 1 }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_MoveNpc,
            DisplayName = "Mover",
            Description = "Mueve un NPC a otra sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_SetFlag,
            DisplayName = "Establecer Flag",
            Description = "Activa o desactiva un flag",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetCounter,
            DisplayName = "Establecer Contador",
            Description = "Establece el valor de un contador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_IncrementCounter,
            DisplayName = "Incrementar Contador",
            Description = "Incrementa o decrementa un contador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_PlaySound,
            DisplayName = "Reproducir Sonido",
            Description = "Reproduce un efecto de sonido",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_StartQuest,
            DisplayName = "Iniciar Misión",
            Description = "Inicia una mision",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_CompleteQuest,
            DisplayName = "Completar Misión",
            Description = "Marca una mision como completada",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_FailQuest,
            DisplayName = "Fallar Misión",
            Description = "Marca una mision como fallida",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetQuestStatus,
            DisplayName = "Cambiar Estado Misión",
            Description = "Cambia el estado de una mision a cualquier valor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_AdvanceObjective,
            DisplayName = "Avanzar Objetivo",
            Description = "Avanza al siguiente objetivo de una mision",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_OpenDoor,
            DisplayName = "Abrir Puerta",
            Description = "Abre una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_CloseDoor,
            DisplayName = "Cerrar Puerta",
            Description = "Cierra una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_LockDoor,
            DisplayName = "Bloquear Puerta",
            Description = "Bloquea una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_UnlockDoor,
            DisplayName = "Desbloquear Puerta",
            Description = "Desbloquea una puerta",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetDoorVisible,
            DisplayName = "Visibilidad Puerta",
            Description = "Muestra u oculta una puerta y sus salidas asociadas",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetNpcVisible,
            DisplayName = "Visibilidad",
            Description = "Muestra u oculta un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_SetObjectVisible,
            DisplayName = "Visibilidad",
            Description = "Muestra u oculta un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_SetObjectTakeable,
            DisplayName = "Cogible",
            Description = "Permite o impide coger un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_OpenContainer,
            DisplayName = "Abrir Contenedor",
            Description = "Abre un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_CloseContainer,
            DisplayName = "Cerrar Contenedor",
            Description = "Cierra un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_LockContainer,
            DisplayName = "Bloquear Contenedor",
            Description = "Bloquea un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_UnlockContainer,
            DisplayName = "Desbloquear Contenedor",
            Description = "Desbloquea un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Contenedor", DataType = "string", EntityType = "GameObject" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_SetContentsVisible,
            DisplayName = "Visibilidad Contenido",
            Description = "Muestra u oculta el contenido de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_SetObjectPrice,
            DisplayName = "Precio",
            Description = "Establece el precio de un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_SetObjectDurability,
            DisplayName = "Durabilidad",
            Description = "Establece la durabilidad actual de un objeto",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_MoveObjectToRoom,
            DisplayName = "Mover a Sala",
            Description = "Mueve un objeto a una sala específica",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_PutObjectInContainer,
            DisplayName = "Poner en Contenedor",
            Description = "Pone un objeto dentro de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_RemoveObjectFromContainer,
            DisplayName = "Sacar de Contenedor",
            Description = "Saca un objeto de un contenedor",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Objetos,
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
            TypeId = NodeTypeId.Action_SetObjectLit,
            DisplayName = "Encender/Apagar Objeto",
            Description = "Enciende o apaga un objeto luminoso",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Iluminacion,
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
            TypeId = NodeTypeId.Action_SetLightTurns,
            DisplayName = "Turnos de Luz",
            Description = "Establece los turnos de luz restantes de un objeto luminoso (-1 = infinito)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Iluminacion,
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
            TypeId = NodeTypeId.Condition_IsObjectLit,
            DisplayName = "Objeto Encendido",
            Description = "Comprueba si un objeto luminoso está encendido",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Iluminacion,
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
            TypeId = NodeTypeId.Condition_IsRoomLit,
            DisplayName = "Sala Iluminada",
            Description = "Comprueba si la sala actual está iluminada (por la sala misma o por fuentes de luz)",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Iluminacion,
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
            TypeId = NodeTypeId.Action_AddMoney,
            DisplayName = "Dar Oro",
            Description = "Da oro al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Dinero,
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
            TypeId = NodeTypeId.Action_RemoveMoney,
            DisplayName = "Quitar Oro",
            Description = "Quita oro al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Dinero,
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
            TypeId = NodeTypeId.Action_StartPatrol,
            DisplayName = "Iniciar Patrulla",
            Description = "Hace que un NPC comience a patrullar su ruta definida",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_StopPatrol,
            DisplayName = "Detener Patrulla",
            Description = "Detiene la patrulla de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_PatrolStep,
            DisplayName = "Paso de Patrulla",
            Description = "Mueve manualmente un NPC al siguiente punto de su ruta",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_SetPatrolMode,
            DisplayName = "Modo de Patrulla",
            Description = "Configura el modo de movimiento de patrulla (por turnos o por tiempo)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_FollowPlayer,
            DisplayName = "Seguir Jugador",
            Description = "Hace que un NPC siga al jugador cuando cambie de sala",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_StopFollowing,
            DisplayName = "Dejar de Seguir",
            Description = "Hace que un NPC deje de seguir al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_SetFollowMode,
            DisplayName = "Modo de Seguimiento",
            Description = "Configura el modo de movimiento de seguimiento (por turnos o por tiempo)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_SetPlayerState,
            DisplayName = "Establecer Estado",
            Description = "Establece el valor de un estado del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_ModifyPlayerState,
            DisplayName = "Modificar Estado",
            Description = "Añade o resta al valor de un estado del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_HealPlayer,
            DisplayName = "Curar",
            Description = "Restaura salud al jugador (sin exceder el máximo)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_DamagePlayer,
            DisplayName = "Dañar",
            Description = "Inflige daño al jugador (reduce salud)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RestoreMana,
            DisplayName = "Restaurar Mana",
            Description = "Restaura mana al jugador (sin exceder el máximo)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_ConsumeMana,
            DisplayName = "Consumir Mana",
            Description = "Consume mana del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_StartCombat,
            DisplayName = "Iniciar",
            Description = "Inicia un combate con el NPC especificado",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_DamageNpc,
            DisplayName = "Dañar NPC",
            Description = "Causa daño a un NPC (reduce su salud)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_HealNpc,
            DisplayName = "Curar NPC",
            Description = "Restaura salud de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_SetNpcMaxHealth,
            DisplayName = "Salud Máxima NPC",
            Description = "Establece la salud máxima de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_ReviveNpc,
            DisplayName = "Revivir NPC",
            Description = "Revive un NPC muerto",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_KillNpc,
            DisplayName = "Matar NPC",
            Description = "Mata instantáneamente un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_SetPatrolRoute,
            DisplayName = "Ruta de Patrulla",
            Description = "Establece la ruta de patrulla de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_AddItemToNpcInventory,
            DisplayName = "Dar Item",
            Description = "Añade un objeto al inventario de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
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
            TypeId = NodeTypeId.Action_RemoveItemFromNpcInventory,
            DisplayName = "Quitar Item",
            Description = "Quita un objeto del inventario de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.NPC,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" }
            }
        });

        // === SUBGRUPO: EQUIPAMIENTO (Acciones) ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_EquipPlayerItem,
            DisplayName = "Equipar Jugador",
            Description = "Equipa un objeto al jugador en el slot especificado",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_UnequipPlayerSlot,
            DisplayName = "Desequipar Jugador",
            Description = "Desequipa el objeto del slot especificado del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_EquipNpcItem,
            DisplayName = "Equipar NPC",
            Description = "Equipa un objeto a un NPC en el slot especificado",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "ObjectId", DisplayName = "Objeto", DataType = "string", EntityType = "GameObject" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_UnequipNpcSlot,
            DisplayName = "Desequipar NPC",
            Description = "Desequipa el objeto del slot especificado de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Equipamiento,
            InputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            OutputPorts = new[] { new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" } },
            Properties = new[]
            {
                new NodePropertyDefinition { Name = "NpcId", DisplayName = "NPC", DataType = "string", EntityType = "Npc" },
                new NodePropertyDefinition { Name = "Slot", DisplayName = "Slot", DataType = "select", Options = new[] { "RightHand", "LeftHand", "Torso" } }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Action_SetPlayerMaxHealth,
            DisplayName = "Establecer Salud Máxima",
            Description = "Establece la salud máxima del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_SetNpcAttack,
            DisplayName = "Cambiar Ataque NPC",
            Description = "Cambia el valor de ataque de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_SetNpcDefense,
            DisplayName = "Cambiar Defensa NPC",
            Description = "Cambia el valor de defensa de un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_EndCombatVictory,
            DisplayName = "Forzar Victoria",
            Description = "Termina el combate actual con victoria del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_EndCombatDefeat,
            DisplayName = "Forzar Derrota",
            Description = "Termina el combate actual con derrota del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_ForceFlee,
            DisplayName = "Forzar Huida",
            Description = "Fuerza la huida del combate actual",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Combate,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_OpenTrade,
            DisplayName = "Abrir",
            Description = "Abre una sesión de comercio con el NPC especificado",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_CloseTrade,
            DisplayName = "Cerrar",
            Description = "Cierra la sesión de comercio actual",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_AddPlayerMoney,
            DisplayName = "Dar al Jugador",
            Description = "Añade dinero al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Dinero,
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
            TypeId = NodeTypeId.Action_RemovePlayerMoney,
            DisplayName = "Quitar al Jugador",
            Description = "Quita dinero al jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Dinero,
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
            TypeId = NodeTypeId.Action_SetNpcMoney,
            DisplayName = "Establecer a NPC",
            Description = "Establece el dinero del NPC (-1 para infinito)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Dinero,
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
            TypeId = NodeTypeId.Action_AddNpcItem,
            DisplayName = "Añadir Item a NPC",
            Description = "Añade un item al inventario de la tienda del NPC",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_RemoveNpcItem,
            DisplayName = "Quitar Item de NPC",
            Description = "Quita un item del inventario de la tienda del NPC",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_SetBuyMultiplier,
            DisplayName = "Cambiar Multiplicador Compra",
            Description = "Cambia el multiplicador de compra del NPC (lo que paga al jugador)",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_SetSellMultiplier,
            DisplayName = "Cambiar Multiplicador Venta",
            Description = "Cambia el multiplicador de venta del NPC (lo que cobra al jugador)",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Dinero,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Action_AddAbility,
            DisplayName = "Añadir al Jugador",
            Description = "Otorga una habilidad de combate al jugador",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RemoveAbility,
            DisplayName = "Quitar al Jugador",
            Description = "Quita una habilidad de combate del jugador",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_AddAbilityToNpc,
            DisplayName = "Añadir a NPC",
            Description = "Otorga una habilidad de combate a un NPC",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RemoveAbilityFromNpc,
            DisplayName = "Quitar a NPC",
            Description = "Quita una habilidad de combate de un NPC",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_FeedPlayer,
            DisplayName = "Alimentar Jugador",
            Description = "Reduce el hambre del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Necesidades,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Action_HydratePlayer,
            DisplayName = "Hidratar Jugador",
            Description = "Reduce la sed del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Necesidades,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Action_RestPlayer,
            DisplayName = "Descansar Jugador",
            Description = "Reduce el cansancio del jugador",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Necesidades,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Action_SetNeedRate,
            DisplayName = "Cambiar Velocidad",
            Description = "Cambia la velocidad de incremento de una necesidad (hambre, sed o sueño)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Necesidades,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Action_RestoreAllStats,
            DisplayName = "Restaurar Todo",
            Description = "Restaura todos los estados del jugador a sus valores máximos/óptimos",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
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
            TypeId = NodeTypeId.Action_ApplyModifier,
            DisplayName = "Aplicar",
            Description = "Aplica un modificador temporal a un estado del jugador",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RemoveModifier,
            DisplayName = "Eliminar por Nombre",
            Description = "Elimina un modificador temporal específico por nombre",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RemoveModifiersByState,
            DisplayName = "Eliminar por Estado",
            Description = "Elimina todos los modificadores que afectan a un estado específico",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_RemoveAllModifiers,
            DisplayName = "Eliminar Todos",
            Description = "Elimina todos los modificadores temporales activos",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_ProcessModifiers,
            DisplayName = "Procesar Tick",
            Description = "Procesa todos los modificadores activos (aplica efectos recurrentes y elimina expirados)",
            Category = NodeCategory.Action,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Action_StartConversation,
            DisplayName = "Iniciar Conversación",
            Description = "Inicia la conversación con un NPC",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Action_SetProperty,
            DisplayName = "Establecer Propiedad",
            Description = "Establece el valor de una propiedad de cualquier entidad",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Action_ModifyProperty,
            DisplayName = "Modificar Propiedad Numérica",
            Description = "Modifica el valor numérico de una propiedad (suma, resta, multiplica o divide)",
            Category = NodeCategory.Action,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Flow_Branch,
            DisplayName = "Bifurcacion",
            Description = "Bifurca el flujo segun una condicion (usar con nodos de condicion)",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Flow_Sequence,
            DisplayName = "Secuencia",
            Description = "Ejecuta multiples salidas en orden",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Flow_Delay,
            DisplayName = "Esperar",
            Description = "Espera un tiempo antes de continuar",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Flow_RandomBranch,
            DisplayName = "Rama Aleatoria",
            Description = "Elige una salida aleatoriamente",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Variable_GetGameHour,
            DisplayName = "Hora",
            Description = "Obtiene la hora actual del juego",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Hour", PortType = PortType.Data, DataType = "int", Label = "Hora" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerMoney,
            DisplayName = "Oro del Jugador",
            Description = "Obtiene el dinero actual del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Money", PortType = PortType.Data, DataType = "int", Label = "Dinero" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetCurrentRoom,
            DisplayName = "Sala Actual",
            Description = "Obtiene la sala actual del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "RoomId", PortType = PortType.Data, DataType = "string", Label = "Sala" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetCurrentWeather,
            DisplayName = "Clima Actual",
            Description = "Obtiene el clima actual del juego",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Weather", PortType = PortType.Data, DataType = "string", Label = "Clima" }
            }
        });

        // === SUBGRUPO: JUGADOR ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerStrength,
            DisplayName = "Fuerza",
            Description = "Obtiene la fuerza del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Fuerza" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerConstitution,
            DisplayName = "Constitución",
            Description = "Obtiene la constitución del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Constitución" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerIntelligence,
            DisplayName = "Inteligencia",
            Description = "Obtiene la inteligencia del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Inteligencia" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerDexterity,
            DisplayName = "Destreza",
            Description = "Obtiene la destreza del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Destreza" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerCharisma,
            DisplayName = "Carisma",
            Description = "Obtiene el carisma del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Carisma" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerWeight,
            DisplayName = "Peso",
            Description = "Obtiene el peso del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Peso" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerAge,
            DisplayName = "Edad",
            Description = "Obtiene la edad del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Edad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerHeight,
            DisplayName = "Altura",
            Description = "Obtiene la altura del jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Altura" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerInitialMoney,
            DisplayName = "Dinero Inicial",
            Description = "Obtiene el dinero inicial configurado para el jugador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Jugador,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Dinero Inicial" }
            }
        });

        // === SUBGRUPO: OPERADORES ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetFlag,
            DisplayName = "Obtener Flag",
            Description = "Obtiene el valor de un flag",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Variable_GetCounter,
            DisplayName = "Obtener Contador",
            Description = "Obtiene el valor de un contador",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Variable_ConstantInt,
            DisplayName = "Entero Constante",
            Description = "Un valor entero constante",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Variable_ConstantBool,
            DisplayName = "Booleano Constante",
            Description = "Un valor booleano constante (verdadero/falso)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Variable_GetPlayerHealth,
            DisplayName = "Salud",
            Description = "Obtiene la salud actual del jugador (0-100)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Salud" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerMaxHealth,
            DisplayName = "Salud Máxima",
            Description = "Obtiene la salud máxima del jugador",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Salud Máx" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerHunger,
            DisplayName = "Hambre",
            Description = "Obtiene el nivel de hambre del jugador (0=lleno, 100=muriendo)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Hambre" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerThirst,
            DisplayName = "Sed",
            Description = "Obtiene el nivel de sed del jugador (0=hidratado, 100=deshidratado)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Sed" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerEnergy,
            DisplayName = "Energía",
            Description = "Obtiene el nivel de energía del jugador (0=exhausto, 100=descansado)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Energía" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerSleep,
            DisplayName = "Sueño",
            Description = "Obtiene el nivel de sueño/cansancio del jugador (0=descansado, 100=agotado)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.BasicNeeds,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Sueño" }
            }
        });

        // === VELOCIDAD DE NECESIDADES ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetNeedRate,
            DisplayName = "Obtener Velocidad",
            Description = "Obtiene la velocidad de incremento de una necesidad (0=Lento, 1=Normal, 2=Rápido)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Necesidades,
            RequiredFeature = RequiredFeature.BasicNeeds,
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
            TypeId = NodeTypeId.Variable_GetPlayerSanity,
            DisplayName = "Cordura",
            Description = "Obtiene el nivel de cordura del jugador (0=locura, 100=cuerdo)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Cordura" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerMana,
            DisplayName = "Mana",
            Description = "Obtiene el nivel de mana del jugador (0-100)",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Mana" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerMaxMana,
            DisplayName = "Mana Máximo",
            Description = "Obtiene el mana máximo del jugador",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Mana Máx" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_GetPlayerState,
            DisplayName = "Obtener Estado (Genérico)",
            Description = "Obtiene el valor de cualquier estado del jugador",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            RequiredFeature = RequiredFeature.Combat,
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
            TypeId = NodeTypeId.Variable_GetActiveModifiersCount,
            DisplayName = "Número de Modificadores",
            Description = "Obtiene el número de modificadores temporales activos",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
            InputPorts = Array.Empty<NodePort>(),
            OutputPorts = new[]
            {
                new NodePort { Name = "Value", PortType = PortType.Data, DataType = "int", Label = "Cantidad" }
            }
        });

        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Variable_HasModifier,
            DisplayName = "Tiene Modificador",
            Description = "Verifica si el jugador tiene un modificador activo por nombre",
            Category = NodeCategory.Variable,
            Subgroup = NodeSubgroup.Jugador,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Variable_GetProperty,
            DisplayName = "Obtener Propiedad",
            Description = "Obtiene el valor de cualquier propiedad de una entidad",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Compare_Int,
            DisplayName = "Comparar Enteros",
            Description = "Compara dos valores enteros y produce un resultado booleano",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Compare_PlayerMoney,
            DisplayName = "Comparar Oro",
            Description = "Compara el oro del jugador con un valor",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Compare_Counter,
            DisplayName = "Comparar Contador (Data)",
            Description = "Compara un contador con un valor (entrada de datos)",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Juego,
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
            TypeId = NodeTypeId.Math_Add,
            DisplayName = "Sumar",
            Description = "Suma dos valores enteros",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Subtract,
            DisplayName = "Restar",
            Description = "Resta dos valores enteros (A - B)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Multiply,
            DisplayName = "Multiplicar",
            Description = "Multiplica dos valores enteros",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Divide,
            DisplayName = "Dividir",
            Description = "Divide dos valores enteros (A / B)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Modulo,
            DisplayName = "Módulo",
            Description = "Obtiene el resto de la division (A % B)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Negate,
            DisplayName = "Negar",
            Description = "Cambia el signo de un valor entero",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Abs,
            DisplayName = "Valor Absoluto",
            Description = "Obtiene el valor absoluto de un entero",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Min,
            DisplayName = "Mínimo",
            Description = "Obtiene el menor de dos valores",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Max,
            DisplayName = "Máximo",
            Description = "Obtiene el mayor de dos valores",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Clamp,
            DisplayName = "Limitar",
            Description = "Limita un valor entre un minimo y un maximo",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Math_Random,
            DisplayName = "Aleatorio",
            Description = "Genera un numero aleatorio entre Min y Max (inclusive)",
            Category = NodeCategory.Variable,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Logic_And,
            DisplayName = "Y (AND)",
            Description = "Devuelve verdadero si ambas entradas son verdaderas",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Logic_Or,
            DisplayName = "O (OR)",
            Description = "Devuelve verdadero si al menos una entrada es verdadera",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Logic_Not,
            DisplayName = "No (NOT)",
            Description = "Invierte el valor booleano",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Logic_Xor,
            DisplayName = "O Exclusivo (XOR)",
            Description = "Devuelve verdadero si exactamente una entrada es verdadera",
            Category = NodeCategory.Condition,
            OwnerTypes = NodeOwnerType.All,
            Subgroup = NodeSubgroup.Operadores,
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
            TypeId = NodeTypeId.Select_Int,
            DisplayName = "Seleccionar Entero",
            Description = "Selecciona entre dos valores enteros segun una condicion",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Select_Bool,
            DisplayName = "Seleccionar Booleano",
            Description = "Selecciona entre dos valores booleanos segun una condicion",
            Category = NodeCategory.Flow,
            OwnerTypes = NodeOwnerType.All,
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
            TypeId = NodeTypeId.Conversation_Start,
            DisplayName = "Inicio de Conversación",
            Description = "Punto de entrada de una conversación con NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
            OutputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === NPC DICE ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Conversation_NpcSay,
            DisplayName = "NPC Dice",
            Description = "El NPC dice un texto al jugador",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_PlayerChoice,
            DisplayName = "Opciones del Jugador",
            Description = "Presenta opciones de diálogo al jugador (hasta 4)",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_Branch,
            DisplayName = "Bifurcación de Diálogo",
            Description = "Elige un camino según una condición",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_End,
            DisplayName = "Fin de Conversación",
            Description = "Termina la conversación y devuelve el control al juego",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
            InputPorts = new[]
            {
                new NodePort { Name = "Exec", PortType = PortType.Execution, Label = "" }
            }
        });

        // === ACCIÓN EN CONVERSACIÓN ===
        Register(new NodeTypeDefinition
        {
            TypeId = NodeTypeId.Conversation_Action,
            DisplayName = "Ejecutar Acción",
            Description = "Ejecuta una acción dentro de la conversación",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_Shop,
            DisplayName = "Abrir Tienda",
            Description = "Abre la interfaz de compra/venta con el NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_BuyItem,
            DisplayName = "Comprar Objeto",
            Description = "Permite comprar un objeto específico con precio fijo",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
            TypeId = NodeTypeId.Conversation_SellItem,
            DisplayName = "Vender Objeto",
            Description = "Permite vender un objeto específico al NPC",
            Category = NodeCategory.Dialogue,
            OwnerTypes = NodeOwnerType.Npc,
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
