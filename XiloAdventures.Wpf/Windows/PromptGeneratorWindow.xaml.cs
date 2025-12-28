using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class PromptGeneratorWindow : Window
{
    private bool _isUpdatingFromRoomCount = false;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: true) }
    };

    private const string PromptTemplate = @"Necesito que generes un JSON para un motor de aventuras de texto/gráficas. El JSON debe representar un mundo completo de una aventura con temática: **{THEME}**.

## ESTRUCTURA DEL JSON

```json
{
  ""Game"": {
    ""Id"": ""game"",
    ""Title"": ""Título acorde a la temática {THEME}"",
    ""Theme"": ""{THEME}"",
    ""StartRoomId"": ""id_sala_inicial"",
    ""StartHour"": 9,
    ""StartWeather"": ""Despejado"",
    ""MinutesPerGameHour"": 6,
    ""ParserDictionaryJson"": null,
    ""EncryptionKey"": ""XXXXXXXX"",
    ""EndingText"": ""Texto de felicitación por completar la aventura, acorde a la temática"",
    ""CraftingEnabled"": {CRAFTING_ENABLED},
    ""CombatEnabled"": {COMBAT_ENABLED},
    ""MagicEnabled"": {MAGIC_ENABLED},
    ""BasicNeedsEnabled"": {BASIC_NEEDS_ENABLED}
  },
  ""Player"": {
    ""Name"": ""Nombre aleatorio del protagonista"",
    ""Age"": 25,
    ""Weight"": 70,
    ""Height"": 170,
    ""Strength"": 20,
    ""Constitution"": 20,
    ""Intelligence"": 20,
    ""Dexterity"": 20,
    ""Charisma"": 20,
    ""InitialMoney"": 50,
    ""MaxInventoryWeight"": -1,
    ""MaxInventoryVolume"": -1,
    ""InitialInventory"": [
      { ""ObjectId"": ""obj_linterna"", ""Quantity"": 1 }{PLAYER_INITIAL_INVENTORY_EXTRA}
    ],
    ""InitialRightHandId"": {PLAYER_INITIAL_RIGHT_HAND},
    ""InitialLeftHandId"": null,
    ""InitialTorsoId"": {PLAYER_INITIAL_TORSO},
    ""AbilityIds"": [{PLAYER_ABILITY_IDS}]
  },
  ""Rooms"": [
    {
      ""Id"": ""room_id"",
      ""Name"": ""Nombre visible"",
      ""Description"": ""Descripción de la sala"",
      ""IsInterior"": false,
      ""IsIlluminated"": true,
      ""MusicId"": null,
      ""Exits"": [
        { ""TargetRoomId"": ""otra_sala"", ""Direction"": ""norte"", ""DoorId"": ""door_id o null"" }
      ]
    }
  ],
  ""Objects"": [
    {
      ""Id"": ""obj_id"",
      ""Name"": ""Nombre"",
      ""Description"": ""Descripción"",
      ""Type"": ""ninguno|arma|armadura|casco|escudo|comida|bebida|llave"",
      ""CanRead"": false,
      ""TextContent"": ""Contenido legible (solo si CanRead=true)"",
      ""Gender"": ""Masculine|Feminine"",
      ""IsPlural"": false,
      ""RoomId"": ""room_id o null si está en inventario/contenedor"",
      ""Visible"": true,
      ""CanTake"": true,
      ""IsContainer"": false,
      ""IsOpenable"": false,
      ""IsOpen"": true,
      ""IsLocked"": false,
      ""KeyId"": ""obj_llave_id si IsLocked=true, sino null"",
      ""ContentsVisible"": true,
      ""ContainedObjectIds"": [],
      ""MaxCapacity"": 50000,
      ""Volume"": 10,
      ""Weight"": 100,
      ""Price"": 10,
      ""AttackBonus"": 5,
      ""DefenseBonus"": 3,
      ""HandsRequired"": 1,
      ""DamageType"": ""Physical"",
      ""InitiativeBonus"": 0,
      ""MaxDurability"": -1,
      ""CurrentDurability"": -1,
      ""NutritionAmount"": 10,
      ""IsLightSource"": false,
      ""IsLit"": false,
      ""LightTurnsRemaining"": -1,
      ""CanIgnite"": true,
      ""CanExtinguish"": true,
      ""IgniterObjectId"": null
    }
  ],
  ""Npcs"": [
    {
      ""Id"": ""npc_id"",
      ""Name"": ""Nombre"",
      ""Description"": ""Descripción del NPC y su personalidad"",
      ""RoomId"": ""room_id"",
      ""Visible"": true,
      ""Inventory"": [
        { ""ObjectId"": ""obj_id"", ""Quantity"": 1 }
      ],
      ""EquippedRightHandId"": null,
      ""EquippedLeftHandId"": null,
      ""EquippedTorsoId"": null,
      ""IsShopkeeper"": false,
      ""ShopInventory"": [],
      ""BuyPriceMultiplier"": 0.5,
      ""SellPriceMultiplier"": 1.0,
      ""Money"": 0,
      ""PatrolRoute"": [""room_1"", ""room_2"", ""room_3""],
      ""IsPatrolling"": true,
      ""PatrolMovementMode"": ""Turns"",
      ""PatrolSpeed"": 1,
      ""PatrolTimeInterval"": 3.0,
      ""IsFollowingPlayer"": false,
      ""FollowMovementMode"": ""Turns"",
      ""FollowSpeed"": 1,
      ""FollowTimeInterval"": 3.0,
      ""Stats"": {
        ""Strength"": 5,
        ""Dexterity"": 5,
        ""Intelligence"": 5,
        ""MaxHealth"": 10,
        ""CurrentHealth"": 10
      },
      ""AbilityIds"": [],
      ""MagicEnabled"": false,
      ""IsCorpse"": false
    }
  ],
  ""Doors"": [
    {
      ""Id"": ""door_id"",
      ""Name"": ""Nombre de la puerta"",
      ""Description"": ""Descripción opcional"",
      ""Gender"": ""Feminine"",
      ""IsPlural"": false,
      ""RoomIdA"": ""sala_1"",
      ""RoomIdB"": ""sala_2"",
      ""IsOpen"": false,
      ""IsLocked"": true,
      ""KeyObjectId"": ""obj_llave o null"",
      ""OpenFromSide"": ""Both"",
      ""Visible"": true
    }
  ],
  ""Quests"": [
    {
      ""Id"": ""quest_principal"",
      ""Name"": ""Misión Principal"",
      ""Description"": ""Descripción de la misión principal"",
      ""IsMainQuest"": true,
      ""Objectives"": [""Objetivo 1"", ""Objetivo 2"", ""Objetivo final""]
    },
    {
      ""Id"": ""quest_secundaria"",
      ""Name"": ""Misión Secundaria"",
      ""Description"": ""Descripción de misión opcional"",
      ""IsMainQuest"": false,
      ""Objectives"": [""Objetivo opcional""]
    }
  ],
  ""Conversations"": [
    {
      ""Id"": ""conv_comerciante"",
      ""Name"": ""Diálogo con comerciante (EJEMPLO COMPLETO)"",
      ""Nodes"": [
        { ""Id"": ""c1"", ""NodeType"": ""Conversation_Start"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
        { ""Id"": ""c2"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 300, ""Y"": 100, ""Properties"": { ""Text"": ""¡Bienvenido a mi tienda! ¿En qué puedo ayudarte?"", ""SpeakerName"": """", ""Emotion"": ""Feliz"" } },
        { ""Id"": ""c3"", ""NodeType"": ""Conversation_PlayerChoice"", ""X"": 500, ""Y"": 100, ""Properties"": { ""Text1"": ""Quiero ver tu mercancía"", ""Text2"": ""Busco información"", ""Text3"": ""Adiós"", ""Text4"": """" } },
        { ""Id"": ""c4"", ""NodeType"": ""Conversation_Shop"", ""X"": 700, ""Y"": 50, ""Properties"": { ""ShopTitle"": ""Tienda"", ""WelcomeMessage"": ""¡Echa un vistazo!"" } },
        { ""Id"": ""c5"", ""NodeType"": ""Conversation_Branch"", ""X"": 700, ""Y"": 150, ""Properties"": { ""ConditionType"": ""HasFlag"", ""FlagName"": ""info_obtenida"" } },
        { ""Id"": ""c6"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 900, ""Y"": 100, ""Properties"": { ""Text"": ""Ya te conté todo lo que sé."", ""SpeakerName"": """", ""Emotion"": ""Neutral"" } },
        { ""Id"": ""c7"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 900, ""Y"": 200, ""Properties"": { ""Text"": ""Dicen que hay un tesoro escondido en la cueva del norte..."", ""SpeakerName"": """", ""Emotion"": ""Sorprendido"" } },
        { ""Id"": ""c8"", ""NodeType"": ""Conversation_Action"", ""X"": 1100, ""Y"": 200, ""Properties"": { ""ActionType"": ""SetFlag"", ""FlagName"": ""info_obtenida"" } },
        { ""Id"": ""c9"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 700, ""Y"": 250, ""Properties"": { ""Text"": ""¡Vuelve pronto!"", ""SpeakerName"": """", ""Emotion"": ""Feliz"" } },
        { ""Id"": ""c10"", ""NodeType"": ""Conversation_End"", ""X"": 1300, ""Y"": 150, ""Properties"": {} }
      ],
      ""Connections"": [
        { ""FromNodeId"": ""c1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c2"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c2"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c3"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c3"", ""FromPortName"": ""Option1"", ""ToNodeId"": ""c4"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c3"", ""FromPortName"": ""Option2"", ""ToNodeId"": ""c5"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c3"", ""FromPortName"": ""Option3"", ""ToNodeId"": ""c9"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c4"", ""FromPortName"": ""OnClose"", ""ToNodeId"": ""c2"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c5"", ""FromPortName"": ""True"", ""ToNodeId"": ""c6"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c5"", ""FromPortName"": ""False"", ""ToNodeId"": ""c7"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c6"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c3"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c7"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c8"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c8"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c3"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""c9"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c10"", ""ToPortName"": ""Exec"" }
      ],
      ""StartNodeId"": ""c1""
    }
  ],
  ""Scripts"": [
    {
      ""Id"": ""script_game_start"",
      ""Name"": ""Inicio del juego"",
      ""OwnerType"": ""Game"",
      ""OwnerId"": ""game"",
      ""Nodes"": [
        { ""Id"": ""node_event_1"", ""NodeType"": ""Event_OnGameStart"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
        { ""Id"": ""node_quest_1"", ""NodeType"": ""Action_StartQuest"", ""X"": 300, ""Y"": 100, ""Properties"": { ""QuestId"": ""quest_principal"" } },
        { ""Id"": ""node_msg_1"", ""NodeType"": ""Action_ShowMessage"", ""X"": 500, ""Y"": 100, ""Properties"": { ""Message"": ""¡Bienvenido a la aventura!"" } }
      ],
      ""Connections"": [
        { ""FromNodeId"": ""node_event_1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""node_quest_1"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""node_quest_1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""node_msg_1"", ""ToPortName"": ""Exec"" }
      ]
    },
    {
      ""Id"": ""script_npc_give_item"",
      ""Name"": ""NPC da objeto oculto (EJEMPLO CORRECTO)"",
      ""OwnerType"": ""Npc"",
      ""OwnerId"": ""npc_ejemplo"",
      ""Nodes"": [
        { ""Id"": ""n1"", ""NodeType"": ""Event_OnTalk"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
        { ""Id"": ""n2"", ""NodeType"": ""Condition_HasFlag"", ""X"": 300, ""Y"": 100, ""Properties"": { ""FlagName"": ""obj_entregado"" } },
        { ""Id"": ""n3"", ""NodeType"": ""Action_ShowMessage"", ""X"": 500, ""Y"": 50, ""Properties"": { ""Message"": ""Ya te lo di."" } },
        { ""Id"": ""n4"", ""NodeType"": ""Action_SetObjectVisible"", ""X"": 500, ""Y"": 150, ""Properties"": { ""ObjectId"": ""obj_oculto"", ""Visible"": true } },
        { ""Id"": ""n5"", ""NodeType"": ""Action_GiveItem"", ""X"": 700, ""Y"": 150, ""Properties"": { ""ObjectId"": ""obj_oculto"" } },
        { ""Id"": ""n6"", ""NodeType"": ""Action_SetFlag"", ""X"": 900, ""Y"": 150, ""Properties"": { ""FlagName"": ""obj_entregado"", ""Value"": true } },
        { ""Id"": ""n7"", ""NodeType"": ""Action_ShowMessage"", ""X"": 1100, ""Y"": 150, ""Properties"": { ""Message"": ""Toma, te doy esto."" } }
      ],
      ""Connections"": [
        { ""FromNodeId"": ""n1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""n2"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""n2"", ""FromPortName"": ""True"", ""ToNodeId"": ""n3"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""n2"", ""FromPortName"": ""False"", ""ToNodeId"": ""n4"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""n4"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""n5"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""n5"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""n6"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""n6"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""n7"", ""ToPortName"": ""Exec"" }
      ]
    }
  ],
  ""Abilities"": [
    {
      ""Id"": ""habilidad_bola_fuego"",
      ""Name"": ""Bola de Fuego"",
      ""Description"": ""Lanza una bola de fuego que causa daño mágico"",
      ""AbilityType"": ""Attack"",
      ""ManaCost"": 15,
      ""AttackValue"": 5,
      ""DefenseValue"": 0,
      ""Damage"": 20,
      ""Healing"": 0,
      ""DamageType"": ""Magical"",
      ""StatusEffect"": null,
      ""StatusEffectDuration"": 0,
      ""TargetsSelf"": false
    },
    {
      ""Id"": ""habilidad_curacion"",
      ""Name"": ""Curación Menor"",
      ""Description"": ""Restaura salud al usuario"",
      ""AbilityType"": ""Defense"",
      ""ManaCost"": 10,
      ""AttackValue"": 0,
      ""DefenseValue"": 0,
      ""Damage"": 0,
      ""Healing"": 25,
      ""DamageType"": ""Magical"",
      ""StatusEffect"": null,
      ""StatusEffectDuration"": 0,
      ""TargetsSelf"": true
    }
  ],
  ""RoomPositions"": {
    ""room_id"": { ""X"": 80, ""Y"": 45 },
    ""otra_sala"": { ""X"": 80, ""Y"": -135 }
  }
}
```

## TIPOS DE NODOS DISPONIBLES PARA SCRIPTS

**IMPORTANTE**: Usa EXACTAMENTE estos TypeId. Si usas nombres incorrectos, los nodos no funcionarán.

### Eventos (inician el script, solo puerto de salida ""Exec""):

**Eventos de Juego (OwnerType: Game):**
- `Event_OnGameStart` - Al iniciar el juego
- `Event_OnGameEnd` - Al finalizar el juego
- `Event_EveryMinute` - Cada minuto de juego
- `Event_EveryHour` - Cada hora de juego
- `Event_OnTurnStart` - Al inicio de cada turno del jugador
- `Event_OnWeatherChange` - Al cambiar el clima
- `Event_OnPlayerDeath` - Al morir el jugador

**Eventos de Sala (OwnerType: Room):**
- `Event_OnEnter` - Al entrar a la sala
- `Event_OnExit` - Al salir de la sala
- `Event_OnLook` - Al mirar/examinar la sala

**Eventos de NPC (OwnerType: Npc):**
- `Event_OnTalk` - Al hablar con NPC
- `Event_OnNpcSee` - Cuando el NPC ve al jugador entrar
- `Event_OnNpcAttack` - Al atacar al NPC
- `Event_OnNpcDeath` - Al morir el NPC
- `Event_OnNpcTurn` - En el turno del NPC en combate

**Eventos de Objeto (OwnerType: GameObject):**
- `Event_OnTake` - Al coger objeto
- `Event_OnDrop` - Al soltar objeto
- `Event_OnUse` - Al usar objeto
- `Event_OnGive` - Al dar objeto a NPC
- `Event_OnExamine` - Al examinar objeto
- `Event_OnEquip` - Al equipar objeto
- `Event_OnUnequip` - Al desequipar objeto
- `Event_OnContainerOpen` - Al abrir contenedor
- `Event_OnContainerClose` - Al cerrar contenedor
- `Event_OnEat` - Al comer objeto (Type=comida)
- `Event_OnDrink` - Al beber objeto (Type=bebida)

**Eventos de Puerta (OwnerType: Door):**
- `Event_OnDoorOpen` - Al abrir puerta
- `Event_OnDoorClose` - Al cerrar puerta
- `Event_OnDoorLock` - Al bloquear puerta
- `Event_OnDoorUnlock` - Al desbloquear puerta

**Eventos de Misión (OwnerType: Quest):**
- `Event_OnQuestStart` - Al iniciar misión
- `Event_OnQuestComplete` - Al completar misión
- `Event_OnQuestFail` - Al fallar misión
- `Event_OnObjectiveComplete` - Al completar un objetivo

**Eventos de Combate (OwnerType: Game/Npc):**
- `Event_OnCombatStart` - Al iniciar combate
- `Event_OnCombatVictory` - Al ganar combate
- `Event_OnCombatDefeat` - Al perder combate
- `Event_OnCombatFlee` - Al huir de combate
- `Event_OnPlayerAttack` - Cuando el jugador ataca
- `Event_OnPlayerDefend` - Cuando el jugador defiende
- `Event_OnCriticalHit` - Al conseguir golpe crítico
- `Event_OnMiss` - Al fallar ataque

**Eventos de Comercio (OwnerType: Npc):**
- `Event_OnTradeStart` - Al iniciar comercio
- `Event_OnTradeEnd` - Al terminar comercio
- `Event_OnItemBought` - Al comprar objeto
- `Event_OnItemSold` - Al vender objeto

**Eventos de Estado del Jugador (OwnerType: Game):**
- `Event_OnHealthLow` - Cuando la salud baja
- `Event_OnHealthCritical` - Cuando la salud es crítica
- `Event_OnHungerHigh` - Cuando el hambre es alta
- `Event_OnThirstHigh` - Cuando la sed es alta
- `Event_OnEnergyLow` - Cuando la energía es baja
- `Event_OnSleepHigh` - Cuando el sueño es alto
- `Event_OnSanityLow` - Cuando la cordura es baja
- `Event_OnManaLow` - Cuando el maná es bajo
- `Event_OnStateThreshold` - Cuando un estado cruza umbral. Properties: { ""StateName"": ""Health"", ""Threshold"": 50 }
- `Event_OnMoneyGained` - Al ganar dinero
- `Event_OnMoneyLost` - Al perder dinero
- `Event_OnMoneyThreshold` - Al cruzar umbral de dinero. Properties: { ""Threshold"": 100 }

**Eventos de Modificadores (OwnerType: Game):**
- `Event_OnModifierApplied` - Cuando se aplica un modificador
- `Event_OnModifierExpired` - Cuando expira un modificador

**Eventos de Sueño (OwnerType: Game/GameObject):**
- `Event_OnSleep` - Al dormir (ej: al usar una cama)
- `Event_OnWakeUp` - Al despertar normalmente
- `Event_OnWakeUpStartled` - Al despertar sobresaltado

### Condiciones (puerto entrada ""Exec"", puertos salida ""True""/""False""):

**Condiciones de Variables:**
- `Condition_HasFlag` - Properties: { ""FlagName"": ""nombre"" }
- `Condition_CompareCounter` - Properties: { ""CounterName"": ""nombre"", ""Operator"": "">="", ""Value"": 5 } (Operators: ==, !=, <, <=, >, >=)
- `Condition_CompareProperty` - Compara propiedad de entidad. Properties: { ""EntityType"": ""Npc"", ""EntityId"": ""npc_id"", ""PropertyName"": ""Money"", ""Operator"": "">="", ""Value"": 100 }
- `Condition_Random` - Properties: { ""Probability"": 50 } (0-100)

**Condiciones de Inventario:**
- `Condition_HasItem` - Jugador tiene objeto. Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_PlayerOwnsItem` - Jugador posee objeto (en inventario o equipado). Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_NpcHasItem` - NPC tiene objeto. Properties: { ""NpcId"": ""npc_id"", ""ObjectId"": ""obj_id"" }

**Condiciones de Ubicación:**
- `Condition_IsInRoom` - Jugador está en sala. Properties: { ""RoomId"": ""room_id"" }
- `Condition_ObjectInRoom` - Objeto está en sala. Properties: { ""ObjectId"": ""obj_id"", ""RoomId"": ""room_id"" }
- `Condition_ObjectInContainer` - Objeto está en contenedor. Properties: { ""ObjectId"": ""obj_id"", ""ContainerId"": ""container_id"" }
- `Condition_NpcInRoom` - NPC está en sala. Properties: { ""NpcId"": ""npc_id"", ""RoomId"": ""room_id"" }

**Condiciones de Puertas:**
- `Condition_IsDoorOpen` - Properties: { ""DoorId"": ""door_id"" }
- `Condition_IsDoorVisible` - Properties: { ""DoorId"": ""door_id"" }

**Condiciones de Contenedores:**
- `Condition_IsContainerOpen` - Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_IsContainerLocked` - Properties: { ""ObjectId"": ""obj_id"" }

**Condiciones de Objetos:**
- `Condition_IsObjectVisible` - Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_IsObjectTakeable` - Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_IsObjectLit` - Objeto luminoso encendido. Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_IsRoomLit` - Sala iluminada. Properties: { ""RoomId"": ""room_id"" }

**Condiciones de NPCs:**
- `Condition_IsNpcVisible` - Properties: { ""NpcId"": ""npc_id"" }
- `Condition_IsPatrolling` - NPC está patrullando. Properties: { ""NpcId"": ""npc_id"" }
- `Condition_IsFollowingPlayer` - NPC sigue al jugador. Properties: { ""NpcId"": ""npc_id"" }
- `Condition_IsNpcAlive` - NPC está vivo. Properties: { ""NpcId"": ""npc_id"" }
- `Condition_NpcHealthBelow` - Salud NPC bajo umbral. Properties: { ""NpcId"": ""npc_id"", ""Threshold"": 50 }
- `Condition_NpcHasEquipped` - NPC tiene objeto equipado. Properties: { ""NpcId"": ""npc_id"", ""Slot"": ""RightHand"" } (Slots: RightHand, LeftHand, Torso)
- `Condition_IsNpcSlotEmpty` - Slot de NPC vacío. Properties: { ""NpcId"": ""npc_id"", ""Slot"": ""RightHand"" }
- `Condition_NpcHasMoney` - NPC tiene dinero. Properties: { ""NpcId"": ""npc_id"", ""Amount"": 100 }
- `Condition_NpcHasInfiniteMoney` - NPC tiene dinero infinito. Properties: { ""NpcId"": ""npc_id"" }

**Condiciones de Jugador:**
- `Condition_IsPlayerAlive` - Jugador está vivo. Sin properties.
- `Condition_PlayerHealthBelow` - Salud bajo umbral. Properties: { ""Threshold"": 50 }
- `Condition_PlayerHealthAbove` - Salud sobre umbral. Properties: { ""Threshold"": 50 }
- `Condition_PlayerHasMoney` - Jugador tiene dinero. Properties: { ""Amount"": 100 }
- `Condition_PlayerHasEquipped` - Jugador tiene objeto equipado. Properties: { ""Slot"": ""RightHand"" } (Slots: RightHand, LeftHand, Torso)
- `Condition_IsPlayerSlotEmpty` - Slot de jugador vacío. Properties: { ""Slot"": ""RightHand"" }
- `Condition_PlayerHasWeaponType` - Jugador tiene tipo de arma. Properties: { ""WeaponType"": ""arma"" }
- `Condition_PlayerHasArmor` - Jugador tiene armadura equipada. Sin properties.
- `Condition_PlayerStateAbove` - Estado sobre umbral. Properties: { ""StateName"": ""Health"", ""Threshold"": 50 }
- `Condition_PlayerStateBelow` - Estado bajo umbral. Properties: { ""StateName"": ""Health"", ""Threshold"": 50 }
- `Condition_PlayerStateBetween` - Estado entre umbrales. Properties: { ""StateName"": ""Health"", ""MinThreshold"": 25, ""MaxThreshold"": 75 }
- `Condition_PlayerStateEquals` - Estado igual a valor. Properties: { ""StateName"": ""Health"", ""Value"": 100 }
- `Condition_HasModifier` - Jugador tiene modificador. Properties: { ""ModifierId"": ""mod_id"" }
- `Condition_HasModifierForState` - Tiene modificador para estado. Properties: { ""StateName"": ""Health"" }

**Condiciones de Misiones:**
- `Condition_IsQuestStatus` - Properties: { ""QuestId"": ""quest_id"", ""Status"": ""InProgress"" } (Status: NotStarted, InProgress, Completed, Failed)
- `Condition_IsMainQuest` - Es misión principal. Properties: { ""QuestId"": ""quest_id"" }

**Condiciones de Tiempo y Clima:**
- `Condition_IsTimeOfDay` - Properties: { ""TimeRange"": ""Manana"" } (valores: Manana, Tarde, Noche, Madrugada)
- `Condition_IsWeather` - Properties: { ""Weather"": ""Despejado"" } (valores: Despejado, Lluvioso, Nublado, Tormenta)

**Condiciones de Combate:**
- `Condition_IsInCombat` - Jugador está en combate. Sin properties.
- `Condition_IsCombatRound` - Es ronda específica. Properties: { ""Round"": 1 }
- `Condition_IsInTrade` - Jugador está comerciando. Sin properties.

### Acciones (puerto entrada ""Exec"", puerto salida ""Exec""):

**Acciones de Mensajes:**
- `Action_ShowMessage` - Properties: { ""Message"": ""texto"" }
- `Action_PlaySound` - Properties: { ""SoundId"": ""fx_id"" }
- `Action_StartConversation` - Inicia conversación. Properties: { ""NpcId"": ""npc_id"" }

**Acciones de Variables:**
- `Action_SetFlag` - Properties: { ""FlagName"": ""nombre"", ""Value"": true }
- `Action_SetCounter` - Properties: { ""CounterName"": ""nombre"", ""Value"": 0 }
- `Action_IncrementCounter` - Properties: { ""CounterName"": ""nombre"", ""Amount"": 1 }

**Acciones de Inventario:**
- `Action_GiveItem` - Da objeto al jugador. Properties: { ""ObjectId"": ""obj_id"" } **IMPORTANTE: El objeto NO debe estar en Inventory del NPC**
- `Action_RemoveItem` - Quita objeto del jugador. Properties: { ""ObjectId"": ""obj_id"" }
- `Action_AddItemToNpcInventory` - Añade objeto a NPC. Properties: { ""NpcId"": ""npc_id"", ""ObjectId"": ""obj_id"" }
- `Action_RemoveItemFromNpcInventory` - Quita objeto de NPC. Properties: { ""NpcId"": ""npc_id"", ""ObjectId"": ""obj_id"" }

**Acciones de Equipamiento:**
- `Action_EquipPlayerItem` - Equipa objeto en slot. Properties: { ""ObjectId"": ""obj_id"", ""Slot"": ""RightHand"" } (Slots: RightHand, LeftHand, Torso)
- `Action_UnequipPlayerSlot` - Desequipa slot. Properties: { ""Slot"": ""RightHand"" }
- `Action_EquipNpcItem` - Equipa objeto en NPC. Properties: { ""NpcId"": ""npc_id"", ""ObjectId"": ""obj_id"", ""Slot"": ""RightHand"" }
- `Action_UnequipNpcSlot` - Desequipa slot de NPC. Properties: { ""NpcId"": ""npc_id"", ""Slot"": ""RightHand"" }

**Acciones de Puertas:**
- `Action_OpenDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_CloseDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_LockDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_UnlockDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_SetDoorVisible` - Properties: { ""DoorId"": ""door_id"", ""Visible"": true }

**Acciones de Contenedores:**
- `Action_OpenContainer` - Properties: { ""ObjectId"": ""obj_id"" }
- `Action_CloseContainer` - Properties: { ""ObjectId"": ""obj_id"" }
- `Action_LockContainer` - Properties: { ""ObjectId"": ""obj_id"" }
- `Action_UnlockContainer` - Properties: { ""ObjectId"": ""obj_id"" }
- `Action_SetContentsVisible` - Properties: { ""ObjectId"": ""obj_id"", ""Visible"": true }
- `Action_PutObjectInContainer` - Properties: { ""ObjectId"": ""obj_id"", ""ContainerId"": ""container_id"" }
- `Action_RemoveObjectFromContainer` - Properties: { ""ObjectId"": ""obj_id"", ""ContainerId"": ""container_id"" }

**Acciones de Objetos:**
- `Action_SetObjectVisible` - Properties: { ""ObjectId"": ""obj_id"", ""Visible"": true }
- `Action_SetObjectTakeable` - Properties: { ""ObjectId"": ""obj_id"", ""CanTake"": true }
- `Action_SetObjectPrice` - Properties: { ""ObjectId"": ""obj_id"", ""Price"": 100 }
- `Action_SetObjectDurability` - Properties: { ""ObjectId"": ""obj_id"", ""Durability"": 50 }
- `Action_MoveObjectToRoom` - Properties: { ""ObjectId"": ""obj_id"", ""RoomId"": ""room_id"" }

**Acciones de Fuentes de Luz:**
- `Action_SetObjectLit` - Enciende/apaga objeto. Properties: { ""ObjectId"": ""obj_id"", ""IsLit"": true }
- `Action_SetLightTurns` - Establece turnos de luz. Properties: { ""ObjectId"": ""obj_id"", ""Turns"": 10 }

**Acciones de Movimiento:**
- `Action_TeleportPlayer` - Properties: { ""RoomId"": ""room_id"" }
- `Action_MoveNpc` - Properties: { ""NpcId"": ""npc_id"", ""RoomId"": ""room_id"" }
- `Action_StartPatrol` - Inicia patrulla. Properties: { ""NpcId"": ""npc_id"" }
- `Action_StopPatrol` - Detiene patrulla. Properties: { ""NpcId"": ""npc_id"" }
- `Action_PatrolStep` - Mueve NPC al siguiente punto. Properties: { ""NpcId"": ""npc_id"" }
- `Action_SetPatrolMode` - Properties: { ""NpcId"": ""npc_id"", ""Mode"": ""Turns"", ""TurnSpeed"": 1, ""TimeInterval"": 3.0 }
- `Action_SetPatrolRoute` - Establece ruta. Properties: { ""NpcId"": ""npc_id"", ""Route"": [""room1"", ""room2""] }
- `Action_FollowPlayer` - NPC sigue al jugador. Properties: { ""NpcId"": ""npc_id"", ""Speed"": 1 }
- `Action_StopFollowing` - Properties: { ""NpcId"": ""npc_id"" }
- `Action_SetFollowMode` - Properties: { ""NpcId"": ""npc_id"", ""Mode"": ""Turns"", ""TurnSpeed"": 1, ""TimeInterval"": 3.0 }

**Acciones de NPCs:**
- `Action_SetNpcVisible` - Properties: { ""NpcId"": ""npc_id"", ""Visible"": true }
- `Action_KillNpc` - Mata al NPC. Properties: { ""NpcId"": ""npc_id"" }
- `Action_ReviveNpc` - Revive al NPC. Properties: { ""NpcId"": ""npc_id"" }
- `Action_DamageNpc` - Daña al NPC. Properties: { ""NpcId"": ""npc_id"", ""Amount"": 10 }
- `Action_HealNpc` - Cura al NPC. Properties: { ""NpcId"": ""npc_id"", ""Amount"": 10 }
- `Action_SetNpcMaxHealth` - Properties: { ""NpcId"": ""npc_id"", ""MaxHealth"": 100 }
- `Action_SetNpcAttack` - Properties: { ""NpcId"": ""npc_id"", ""Attack"": 10 }
- `Action_SetNpcDefense` - Properties: { ""NpcId"": ""npc_id"", ""Defense"": 5 }
- `Action_SetNpcMoney` - Properties: { ""NpcId"": ""npc_id"", ""Amount"": 100 }

**Acciones de Misiones:**
- `Action_StartQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_CompleteQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_FailQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_SetQuestStatus` - Properties: { ""QuestId"": ""quest_id"", ""Status"": ""InProgress"" }
- `Action_AdvanceObjective` - Properties: { ""QuestId"": ""quest_id"" }

**Acciones de Dinero:**
- `Action_AddMoney` - Añade dinero al jugador. Properties: { ""Amount"": 10 }
- `Action_RemoveMoney` - Quita dinero al jugador. Properties: { ""Amount"": 10 }
- `Action_AddPlayerMoney` - Properties: { ""Amount"": 10 }
- `Action_RemovePlayerMoney` - Properties: { ""Amount"": 10 }

**Acciones de Estado del Jugador:**
- `Action_HealPlayer` - Cura salud. Properties: { ""Amount"": 10 }
- `Action_DamagePlayer` - Daña al jugador. Properties: { ""Amount"": 10 }
- `Action_SetPlayerMaxHealth` - Properties: { ""MaxHealth"": 100 }
- `Action_SetPlayerState` - Establece estado. Properties: { ""StateName"": ""Health"", ""Value"": 100 }
- `Action_ModifyPlayerState` - Modifica estado. Properties: { ""StateName"": ""Health"", ""Amount"": -10 }
- `Action_RestoreMana` - Properties: { ""Amount"": 10 }
- `Action_ConsumeMana` - Properties: { ""Amount"": 10 }
- `Action_FeedPlayer` - Reduce hambre. Properties: { ""Amount"": 20 }
- `Action_HydratePlayer` - Reduce sed. Properties: { ""Amount"": 20 }
- `Action_RestPlayer` - Reduce cansancio. Properties: { ""Amount"": 20 }
- `Action_RestoreAllStats` - Restaura todos los estados. Sin properties.
- `Action_SetNeedRate` - Velocidad de necesidad. Properties: { ""NeedType"": ""Hunger"", ""Rate"": ""Normal"" } (Rates: Off, Slow, Normal, Fast)

**Acciones de Modificadores:**
- `Action_ApplyModifier` - Aplica modificador. Properties: { ""ModifierId"": ""mod_id"", ""Duration"": 10 }
- `Action_RemoveModifier` - Quita modificador. Properties: { ""ModifierId"": ""mod_id"" }
- `Action_RemoveModifiersByState` - Properties: { ""StateName"": ""Health"" }
- `Action_RemoveAllModifiers` - Quita todos los modificadores. Sin properties.

**Acciones de Combate:**
- `Action_StartCombat` - Inicia combate. Properties: { ""NpcId"": ""npc_id"" }
- `Action_EndCombatVictory` - Termina combate con victoria. Sin properties.
- `Action_EndCombatDefeat` - Termina combate con derrota. Sin properties.
- `Action_ForceFlee` - Fuerza huida. Sin properties.

**Acciones de Comercio:**
- `Action_OpenTrade` - Abre comercio. Properties: { ""NpcId"": ""npc_id"" }
- `Action_CloseTrade` - Cierra comercio. Sin properties.
- `Action_SetBuyMultiplier` - Multiplicador compra. Properties: { ""NpcId"": ""npc_id"", ""Multiplier"": 0.5 }
- `Action_SetSellMultiplier` - Multiplicador venta. Properties: { ""NpcId"": ""npc_id"", ""Multiplier"": 1.0 }

**Acciones de Habilidades:**
- `Action_AddAbility` - Da habilidad al jugador. Properties: { ""AbilityId"": ""ability_id"" }
- `Action_RemoveAbility` - Quita habilidad. Properties: { ""AbilityId"": ""ability_id"" }
- `Action_AddAbilityToNpc` - Da habilidad a NPC. Properties: { ""NpcId"": ""npc_id"", ""AbilityId"": ""ability_id"" }
- `Action_RemoveAbilityFromNpc` - Quita habilidad de NPC. Properties: { ""NpcId"": ""npc_id"", ""AbilityId"": ""ability_id"" }

**Acciones de Sala:**
- `Action_SetRoomIllumination` - Properties: { ""RoomId"": ""room_id"", ""IsIlluminated"": true }
- `Action_SetRoomMusic` - Properties: { ""RoomId"": ""room_id"", ""MusicId"": ""music_id"" }
- `Action_SetRoomDescription` - Properties: { ""RoomId"": ""room_id"", ""Description"": ""nueva descripción"" }

**Acciones de Propiedades (avanzado):**
- `Action_SetProperty` - Establece propiedad de entidad. Properties: { ""EntityType"": ""Npc"", ""EntityId"": ""npc_id"", ""PropertyName"": ""Money"", ""Value"": 100 }
- `Action_ModifyProperty` - Modifica propiedad numérica. Properties: { ""EntityType"": ""Npc"", ""EntityId"": ""npc_id"", ""PropertyName"": ""Money"", ""Amount"": -50 }

**Acciones de Tiempo y Clima:**
- `Action_SetWeather` - Properties: { ""Weather"": ""Lluvioso"" } (valores: Despejado, Lluvioso, Nublado, Tormenta)
- `Action_SetGameHour` - Properties: { ""Hour"": 12 }
- `Action_AdvanceTime` - Properties: { ""Hours"": 6 }

### Control de flujo:
- `Flow_Sequence` - Ejecuta múltiples ramas simultáneamente (salidas: ""Then0"", ""Then1"", ""Then2""). **NOTA: Prefiere encadenar acciones secuencialmente (A→B→C) en lugar de usar Flow_Sequence. Úsalo solo cuando necesites ejecutar ramas paralelas independientes.**
- `Flow_Branch` - Bifurca según booleano (entrada ""Condition"", salidas ""True""/""False"")
- `Flow_RandomBranch` - Elige salida aleatoria (salidas: ""Out0"", ""Out1"", ""Out2"")
- `Flow_Delay` - Espera antes de continuar. Properties: { ""Seconds"": 5.0 }

### Nodos de conversación (para el array Conversations, NO para Scripts):
- `Conversation_Start` - Inicio de conversación (sin propiedades, solo puerto salida ""Exec"")
- `Conversation_NpcSay` - NPC dice algo. Properties: { ""Text"": ""Hola aventurero"", ""SpeakerName"": ""NPC"", ""Emotion"": ""Neutral"" } (Emotion: Neutral, Feliz, Triste, Enfadado, Sorprendido)
- `Conversation_PlayerChoice` - Opciones del jugador. Properties: { ""Text1"": ""Opción 1"", ""Text2"": ""Opción 2"", ""Text3"": """", ""Text4"": """" } (salidas: Option1, Option2, Option3, Option4)
- `Conversation_Branch` - Bifurcación condicional (salidas: True, False). Properties según ConditionType:
  - HasFlag: { ""ConditionType"": ""HasFlag"", ""FlagName"": ""nombre_flag"" }
  - HasItem: { ""ConditionType"": ""HasItem"", ""ItemId"": ""obj_id"" }
  - HasMoney: { ""ConditionType"": ""HasMoney"", ""MoneyAmount"": 50 }
  - QuestStatus: { ""ConditionType"": ""QuestStatus"", ""QuestId"": ""quest_id"", ""QuestStatus"": ""InProgress"" }
  - VisitedNode: { ""ConditionType"": ""VisitedNode"" } (verifica si ya se visitó este nodo en la conversación)
- `Conversation_End` - Fin de conversación (sin propiedades, solo puerto entrada ""Exec"")
- `Conversation_Action` - Ejecuta acción dentro de conversación. Properties según ActionType:
  - GiveItem: { ""ActionType"": ""GiveItem"", ""ObjectId"": ""obj_id"" }
  - RemoveItem: { ""ActionType"": ""RemoveItem"", ""ObjectId"": ""obj_id"" }
  - AddMoney: { ""ActionType"": ""AddMoney"", ""Amount"": 10 }
  - RemoveMoney: { ""ActionType"": ""RemoveMoney"", ""Amount"": 10 }
  - SetFlag: { ""ActionType"": ""SetFlag"", ""FlagName"": ""nombre_flag"" }
  - StartQuest: { ""ActionType"": ""StartQuest"", ""QuestId"": ""quest_id"" }
  - CompleteQuest: { ""ActionType"": ""CompleteQuest"", ""QuestId"": ""quest_id"" }
  - ShowMessage: { ""ActionType"": ""ShowMessage"", ""Message"": ""texto"" }
- `Conversation_Shop` - Abre la tienda del NPC. Properties: { ""ShopTitle"": ""Mi Tienda"", ""WelcomeMessage"": ""¡Bienvenido!"" } (salidas: OnClose, OnBuy, OnSell)
- `Conversation_BuyItem` - Comprar objeto específico. Properties: { ""ObjectId"": ""obj_id"", ""Price"": 10, ""ConfirmText"": ""¿Comprar por {precio}?"" } (salidas: Success, NotEnoughMoney, Cancelled)
- `Conversation_SellItem` - Vender objeto específico. Properties: { ""ObjectId"": ""obj_id"", ""Price"": 5 } (salidas: Success, NoItem, Cancelled)

{SYSTEMS_INSTRUCTIONS}

## REQUISITOS DEL MUNDO

Genera un mundo con temática ""{THEME}"" que contenga:
1. **Aproximadamente {ROOM_COUNT} salas** conectadas formando un mapa coherente
   - **⚠️ CRÍTICO: TODAS las salas DEBEN ser accesibles desde la sala inicial (StartRoomId)**
   - **NO puede haber salas desconectadas o ""islas"" de salas aisladas del resto**
   - Cada sala debe tener al menos una salida que conecte con el resto del mapa
   - Verifica que siguiendo las conexiones desde StartRoomId se pueda llegar a TODAS las salas
   - Usa IsInterior=true para interiores (cuevas, edificios) y false para exteriores
   - Usa IsIlluminated=false para salas oscuras que requieren luz (antorcha, linterna)
   - MusicId=null por defecto (sin música) o ID de música si se definieron en Musics
2. **{DOOR_COUNT} puertas** - al menos una cerrada con llave si hay llaves disponibles
   - OpenFromSide: ""Both"" (ambos lados), ""FromAOnly"" (solo desde sala A), ""FromBOnly"" (solo desde sala B)
3. **{CONTAINER_COUNT} contenedores** (cofres, cajas, armarios...) con objetos dentro
   - IsOpenable=true para contenedores que se abren/cierran
   - IsLocked=true + KeyId para contenedores con cerradura
   - ContentsVisible=false para ocultar contenido hasta abrir
4. **{TOTAL_OBJECTS} objetos** distribuidos así:
   - {WEAPON_COUNT} armas (Type=""arma"") - espadas, dagas, arcos... con AttackBonus y HandsRequired (1 o 2 manos)
   - {ARMOR_COUNT} armaduras/escudos - armaduras (Type=""armadura"") para torso, escudos (Type=""escudo"") para mano izquierda, con DefenseBonus
   - {HELMET_COUNT} cascos (Type=""casco"") - yelmos, gorros de mago, capuchas... con DefenseBonus
   - {FOOD_COUNT} comida (Type=""comida"") - pan, manzana, carne... con NutritionAmount
   - {DRINK_COUNT} bebidas (Type=""bebida"") - pociones, agua, vino... con NutritionAmount
   - {KEY_COUNT} llaves (Type=""llave"") - para abrir puertas/contenedores
   - {TEXT_COUNT} documentos legibles (Type=""ninguno"" con CanRead=true) - cartas, diarios, pergaminos... con TextContent
   - {OTHER_COUNT} objetos genéricos (Type=""ninguno"") - gemas, joyas, herramientas, fuentes de luz (IsLightSource=true)...
   - Usa Visible=false para objetos ocultos que aparecen mediante scripts
5. **{NPC_COUNT} NPCs** con personalidad acorde a la temática:
   - Si es comerciante: IsShopkeeper=true y añade IDs de objetos a ShopInventory
   - Si lleva objetos: usa Inventory con lista de { ObjectId, Quantity } (Quantity=-1 para infinito)
   - Equipamiento: EquippedRightHandId (arma/escudo), EquippedLeftHandId (arma 1 mano/escudo), EquippedTorsoId (armadura)
   - Si tiene diálogo: crea un Script para el NPC con nodos Conversation_* (ver punto 8)
   - **Stats de combate**: Strength, Dexterity, Intelligence, MaxHealth, CurrentHealth (dentro de Stats). Money va fuera de Stats.
   - **Patrulla por turnos**: PatrolRoute es un array SIMPLE de strings con IDs de salas, ej: [""sala_1"", ""sala_2"", ""sala_3""]. NO usar objetos. PatrolMovementMode=""Turns"", PatrolSpeed=1 (cada turno), 2 (lento), 3 (muy lento). IsPatrolling=true para empezar patrullando.
   - **Patrulla por tiempo**: PatrolMovementMode=""Time"", PatrolTimeInterval=3.0 (segundos entre movimientos)
   - **Seguimiento por turnos**: FollowMovementMode=""Turns"", FollowSpeed=1/2/3. Actívalo con `Action_FollowPlayer`.
   - **Seguimiento por tiempo**: FollowMovementMode=""Time"", FollowTimeInterval=3.0 (segundos)
   - Usa Visible=false para NPCs ocultos que aparecen mediante scripts
   - **Al menos 1 NPC con ruta de patrulla definida** (guardia, explorador, etc.)
6. **{QUEST_COUNT} misiones** con objetivos definidos en el array Objectives:
   - **Al menos 1 misión principal** (IsMainQuest=true): el objetivo principal de la aventura
   - **Misiones secundarias** (IsMainQuest=false): objetivos opcionales que dan recompensas o enriquecen la historia
   - Cada misión debe tener varios Objectives que marquen el progreso
   - **⚠️ OBLIGATORIO: TODA misión (principal y secundaria) debe tener un `Action_StartQuest` y un `Action_CompleteQuest`** en algún script o conversación. No definas misiones que no se puedan iniciar o completar
7. **Scripts variados** que demuestren (usa los TypeId EXACTOS de la lista anterior):
   - **OBLIGATORIO: Un script en Game con `Event_OnGameStart`** que inicie la misión principal (`Action_StartQuest`) y muestre mensaje introductorio
   - Un objeto que al examinarlo (`Event_OnExamine`) muestra mensaje (`Action_ShowMessage`)
   - Un NPC que da un objeto (`Action_GiveItem`) si tienes cierto item (`Condition_HasItem`)
   - Un contenedor que al abrirlo (`Event_OnContainerOpen`) muestra mensaje
   - Un evento al entrar a cierta sala (`Event_OnEnter` en Room)
   - Un puzzle con contador (`Action_IncrementCounter` + `Condition_CompareCounter`)
   - Uso de flags para recordar acciones (`Action_SetFlag` + `Condition_HasFlag`)
   - Un NPC patrullero con IsPatrolling=true desde el inicio
   - Un script que haga que un NPC siga al jugador (`Action_FollowPlayer`) cuando se cumpla alguna condición (ej: hablar con él, darle un objeto)
   - **Scripts o conversaciones que inicien y completen CADA misión** - recuerda que toda misión necesita `Action_StartQuest` y `Action_CompleteQuest`
8. **Diálogos de NPCs** mediante el array Conversations:
   - Cada NPC con diálogo debe tener una entrada en el array **Conversations** con su diálogo completo
   - Los nodos de conversación van en Conversations, NO en Scripts
   - Usa Conversation_Start → Conversation_NpcSay → Conversation_PlayerChoice → Conversation_End
   - **NO crees scripts con Event_OnTalk solo para iniciar conversaciones** - el motor lo hace automáticamente. Sí puedes usar Event_OnTalk para otras acciones adicionales (dar objetos, activar flags, etc.)
   - **Connections entre nodos de conversación**: Usa los nombres de puerto correctos:
     - Conversation_Start/NpcSay/Action/End: puerto ""Exec""
     - Conversation_PlayerChoice: puertos ""Option1"", ""Option2"", ""Option3"", ""Option4""
     - Conversation_Branch: puertos ""True"", ""False""
     - Conversation_Shop: puertos ""OnClose"", ""OnBuy"", ""OnSell""
   - Ver ejemplo ""conv_comerciante"" en la plantilla JSON para un diálogo completo con tienda, condiciones y acciones

## NOTAS IMPORTANTES
- Los IDs deben ser snake_case únicos
- **Game.StartRoomId DEBE coincidir con el Id de una sala existente** - El jugador empieza ahí
- **⚠️ TODAS las salas DEBEN estar conectadas entre sí** - Desde StartRoomId se debe poder llegar a cualquier sala del mapa siguiendo las salidas. NO generes grupos de salas desconectados.
- Direcciones válidas: norte, sur, este, oeste, arriba, abajo

### Coordenadas en RoomPositions para visualización del mapa
**IMPORTANTE**: Las posiciones van en `RoomPositions` (NO dentro de cada Room).
- Las celdas del grid miden **160x90 píxeles**. Las posiciones deben ser **centros de celdas**.
- Usa **incrementos de 320 en X y 180 en Y** para dejar espacio entre salas para las conexiones.
- La sala inicial debe estar en **(80, 45)** (centro de la primera celda).
- Para cada dirección desde una sala en (X, Y):
  - **Norte**: (X, Y - 180)
  - **Sur**: (X, Y + 180)
  - **Este**: (X + 320, Y)
  - **Oeste**: (X - 320, Y)
  - **Arriba**: (X, Y - 180) — para subir pisos/escaleras, usa misma posición visual que norte
  - **Abajo**: (X, Y + 180) — para bajar pisos/sótanos, usa misma posición visual que sur
- Ejemplo para 5 salas en cruz: central (80,45), norte (80,-135), sur (80,225), este (400,45), oeste (-240,45)
- **Type de objetos SOLO puede ser uno de estos valores exactos**: ninguno, arma, armadura, casco, escudo, comida, bebida, llave
- **Los objetos que son llaves DEBEN tener Type=""llave""**
- **Para objetos legibles** (cartas, diarios, pergaminos...): usa Type=""ninguno"" con CanRead=true y TextContent con el texto. El jugador usará el comando ""leer"". **NO crees scripts para leer objetos** - el motor lo maneja automáticamente con el verbo ""leer"".

### Sistema de Equipamiento
- **3 slots de equipamiento**: RightHand (mano derecha), LeftHand (mano izquierda), Torso
- **Armas de 1 mano** (HandsRequired=1): pueden ir en RightHand o LeftHand
- **Armas de 2 manos** (HandsRequired=2): ocupan RightHand y LeftHand automáticamente
- **Escudos** (Type=""escudo""): solo pueden ir en LeftHand
- **Armaduras** (Type=""armadura""): solo pueden ir en Torso
- **InitialRightHandId, InitialLeftHandId, InitialTorsoId**: equipamiento inicial del jugador
- **EquippedRightHandId, EquippedLeftHandId, EquippedTorsoId**: equipamiento de NPCs

### Fuentes de Luz
- **IsLightSource=true**: el objeto es una fuente de luz (antorcha, vela, lámpara...)
- **IsLit**: si está encendido actualmente
- **LightTurnsRemaining**: turnos de luz restantes (-1 = infinito)
- **CanIgnite/CanExtinguish**: si se puede encender/apagar
- **IgniterObjectId**: objeto necesario para encender (null = se enciende sin nada)
- Las salas con IsIlluminated=false requieren luz para ver

### Inventario con Cantidades
- **InitialInventory del jugador**: lista de { ObjectId, Quantity } para inventario inicial
- **Inventory de NPCs**: lista de { ObjectId, Quantity } - objetos que el NPC posee (puede dar/intercambiar)
- **Quantity=-1**: cantidad infinita (para objetos que no se agotan)

### Sistema de Comercio (NPCs comerciantes)
- **IsShopkeeper=true**: el NPC es un comerciante con tienda
- **ShopInventory**: lista de { ObjectId, Quantity } - objetos a la VENTA (stock de tienda, NO en Inventory)
- **Inventory**: objetos propios del NPC (NO se venden, puede darlos/intercambiarlos)
- **BuyPriceMultiplier**: precio al que COMPRA al jugador (0.5 = 50% del precio base)
- **SellPriceMultiplier**: precio al que VENDE al jugador (1.0 = precio base, 1.5 = 150%)
- **Money**: dinero del comerciante (-1 = infinito, 0+ = cantidad limitada)
- Ejemplo: Un herrero vende espadas (en ShopInventory) pero NO vende su propio martillo (en Inventory)
- **Puerta cerrada con llave**: `IsLocked=true` + `KeyObjectId` con el Id de un objeto llave existente
- **Puerta que se abre con puzzle/script**: `IsLocked=false` + `IsOpen=false`. Usa `Action_OpenDoor` en el script cuando se resuelva el puzzle. **NO uses IsLocked=true sin KeyObjectId** - el motor no lo permite
- **Si un contenedor tiene IsLocked=true, DEBE tener KeyId con el Id de un objeto llave existente** (contenedor bloqueado requiere llave)
- **Varía las llaves**: No uses la misma llave para abrir muchas cosas. Crea llaves específicas para cada cerradura importante

### ⚠️ Objetos dentro de contenedores (MUY IMPORTANTE)
- **Si un objeto está en ContainedObjectIds de un contenedor, DEBE tener RoomId=null**
- El motor usa RoomId para determinar si el objeto está suelto en una sala. Si tiene RoomId, aparecerá en la sala aunque esté ""dentro"" del contenedor
- Ejemplo correcto: cofre en sala_1 con llave dentro → cofre.RoomId=""sala_1"", llave.RoomId=null, cofre.ContainedObjectIds=[""llave""]

### ⚠️ Objetos que un NPC ""da"" al jugador (MUY IMPORTANTE)
- Si un NPC debe dar un objeto mediante script (Action_GiveItem), el objeto debe tener **RoomId=null** y **Visible=false** y **NO estar en Inventory del NPC**
- **⚠️ OBLIGATORIO: Si el objeto tiene Visible=false, SIEMPRE debes llamar Action_SetObjectVisible(obj_id, true) ANTES de Action_GiveItem**. Sin esto el objeto no funcionará correctamente.
- **Secuencia OBLIGATORIA para dar objetos ocultos** (ver ejemplo ""script_npc_give_item"" en la plantilla JSON):
  1. Condition_HasFlag(""objeto_entregado"") → Si True: mostrar ""Ya te lo di""
  2. Si False → **Action_SetObjectVisible(obj_id, true)** ← NUNCA OMITIR ESTE PASO
  3. Action_GiveItem(obj_id)
  4. Action_SetFlag(""objeto_entregado"", true)
  5. Action_ShowMessage con diálogo
- **Inventory** de los NPCs es solo para objetos que el NPC TIENE permanentemente (ej: un tendero con su mercancía)

### ⚠️ VALORES EXACTOS DE ENUMERACIONES (OBLIGATORIO)
Usa EXACTAMENTE estos valores en inglés. El parser fallará si usas otros valores:

- **Gender**: `Masculine` o `Feminine` (NUNCA ""Masculino""/""Femenino"")
  - Ejemplos: ""espada"" → Feminine, ""libro"" → Masculine, ""llave"" → Feminine
- **DamageType**: `Physical`, `Magical` o `Piercing` (NUNCA ""Físico""/""Mágico"")
- **ObjectType**: `Ninguno`, `Arma`, `Armadura`, `Casco`, `Escudo`, `Comida`, `Bebida`, `Llave`
- **StartWeather**: `Despejado`, `Lluvioso`, `Nublado`, `Tormenta`
- **AbilityType**: `Attack` o `Defense`

### Configuración de tiempo y clima (Game)
- **StartHour**: Hora inicial del juego (0-23). Ej: 9 para las 9:00, 21 para las 21:00
- **StartWeather**: Clima inicial. Valores EXACTOS: ""Despejado"", ""Lluvioso"", ""Nublado"", ""Tormenta""
- **MinutesPerGameHour**: Minutos reales por hora de juego (1-10). Ej: 6 = cada 6 min reales pasa 1 hora en el juego

### Diccionario del parser (Game.ParserDictionaryJson)
El motor tiene un parser de lenguaje natural que entiende comandos del jugador. Tienes a tu disposición un diccionario para añadir sinónimos específicos de tu aventura:

- **El motor YA reconoce**: verbos básicos (coger, examinar, abrir, ir, atacar, hablar...), artículos (el, la, un...), preposiciones (con, en, a, de...)
- **Usa el diccionario para**: sinónimos específicos de tu temática (ej: ""orbe"" como sinónimo de ""gema"", ""portal"" como sinónimo de ""puerta"")
- Si no necesitas sinónimos extra, deja **null**
- Formato JSON (como string escapado):
```json
""ParserDictionaryJson"": ""{\""verbs\"": {\""atacar\"": [\""golpear\"", \""apuñalar\""]}, \""nouns\"": {\""gema\"": [\""joya\"", \""piedra\"", \""orbe\""]}}""
```

### Clave de cifrado (Game.EncryptionKey)
- Clave de 8 caracteres para cifrar las partidas guardadas
- Usa caracteres alfanuméricos aleatorios (ej: ""X7kM9pL2"")
- Cada mundo debe tener una clave única

### Texto de finalización (Game.EndingText)
- Texto que se muestra al completar la aventura (tras Action_CompleteQuest de la misión principal)
- Crea un mensaje de felicitación acorde a la temática
- Ejemplo horror: ""Has sobrevivido a la mansión maldita. Pocos pueden decir lo mismo...""
- Ejemplo fantasía: ""¡Héroe! Tu nombre será recordado en las crónicas del reino.""

### Estadísticas de objetos
- **Volume**: Volumen en centímetros cúbicos (cm³). Ejemplos: llave=10, libro=1000, espada=500, cofre=50000
- **Weight**: Peso en gramos. Ejemplos: llave=50, libro=500, espada=1500, cofre=5000
- **Price**: Precio del objeto. Asigna valores coherentes con la temática (objetos valiosos más caros)
- **MaxCapacity**: Solo para contenedores (IsContainer=true). Capacidad máxima en cm³. Ej: cofre=100000, bolsa=20000. Usa -1 para ilimitado

### Configuración del jugador (Player)
- **Name**: Inventa un nombre aleatorio acorde a la temática (medieval, sci-fi, etc.)
- **Físico del personaje** (randomiza según la temática y tipo de protagonista):
  - **Age**: Edad en años (mínimo 10, máximo 90)
  - **Weight**: Peso en kg (mínimo 50, máximo 150, incrementos de 5)
  - **Height**: Altura en cm (mínimo 50, máximo 220, incrementos de 5)
  - Ejemplos: guerrero corpulento (Age=35, Weight=95, Height=185), mago anciano (Age=70, Weight=60, Height=165), joven ágil (Age=18, Weight=65, Height=175)
- **Estadísticas** (Strength, Constitution, Intelligence, Dexterity, Charisma):
  - **Mínimo por estadística**: 10
  - **Máximo por estadística**: 100
  - **IMPORTANTE: La suma de las 5 DEBE ser exactamente 100**
  - Randomiza los valores para crear un personaje con personalidad única
  - Ejemplo guerrero: Strength=35, Constitution=25, Intelligence=12, Dexterity=18, Charisma=10 (suma=100)
  - Ejemplo mago: Strength=10, Constitution=15, Intelligence=40, Dexterity=20, Charisma=15 (suma=100)
  - Ejemplo equilibrado: Strength=20, Constitution=20, Intelligence=20, Dexterity=20, Charisma=20 (suma=100)
- **InitialMoney**: Dinero inicial. Calcula un valor razonable basándote en los precios de los objetos (que pueda comprar 1-2 objetos baratos)
- **MaxInventoryWeight**: Peso máximo del inventario en gramos (-1 = ilimitado)
- **MaxInventoryVolume**: Volumen máximo del inventario en cm³ (-1 = ilimitado)
- **InitialInventory**: Lista de objetos iniciales con cantidad: [{ ""ObjectId"": ""obj_id"", ""Quantity"": 1 }]
- **Equipamiento inicial**: InitialRightHandId, InitialLeftHandId, InitialTorsoId para armas/escudos/armaduras equipados al inicio
  - **⚠️ OBLIGATORIO si CombatEnabled=true**: El jugador DEBE empezar con al menos un arma equipada (InitialRightHandId)
  - Crea un arma y/o armadura inicial específica para el jugador (ej: ""espada_inicial"", ""tunica_inicial"")
  - Estos objetos deben estar en InitialInventory Y en el slot correspondiente
- **AbilityIds**: Lista de IDs de habilidades de combate iniciales
  - **⚠️ OBLIGATORIO si MagicEnabled=true**: El jugador DEBE tener al menos 1-2 habilidades iniciales
  - Los IDs deben coincidir con habilidades definidas en el array Abilities
- Los nodos de scripts necesitan posiciones X,Y para visualización (separados ~200px)
- Conecta los nodos: evento → condiciones/acciones mediante puerto ""Exec""
- El puerto de salida de eventos y acciones es ""Exec"", el de entrada también es ""Exec""

## ⚠️ ERRORES COMUNES A EVITAR

1. **IDs inexistentes**: Cada Id referenciado (ObjectId, RoomId, NpcId, DoorId, QuestId) DEBE existir en su array correspondiente
2. **StartRoomId inválido**: Game.StartRoomId DEBE coincidir exactamente con el Id de una sala en el array Rooms
3. **StartNodeId inválido**: Cada Conversation DEBE tener un StartNodeId que apunte al nodo Conversation_Start. Scripts NO necesitan StartNodeId (empiezan desde nodos Event)
4. **Conexiones rotas**: Cada Connection debe referenciar FromNodeId y ToNodeId que existan en el mismo Script/Conversation
5. **Nombres de puerto incorrectos**: Usa EXACTAMENTE los nombres documentados (Exec, True, False, Option1, etc.)
6. **Puertas sin referencia bidireccional**: Si una puerta conecta dos salas, AMBAS salidas deben tener el mismo DoorId
7. **Llaves inaccesibles**: NUNCA pongas una llave detrás de la puerta que abre
8. **Puertas/Contenedores bloqueados sin llave**: Si IsLocked=true, DEBE existir un KeyId/KeyObjectId válido. Para puzzles usa IsLocked=false + IsOpen=false + Action_OpenDoor
9. **Objetos dados por NPC en Inventory**: Los objetos que se dan con Action_GiveItem NO deben estar en el Inventory del NPC
10. **Puzzle de puerta mal configurado**: Para puzzles usa IsLocked=false + IsOpen=false + Action_OpenDoor. NUNCA uses IsLocked=true sin KeyObjectId
11. **⚠️ CRÍTICO - Action_GiveItem sin Action_SetObjectVisible**: Si el objeto tiene Visible=false, SIEMPRE debes añadir Action_SetObjectVisible(obj, true) ANTES de Action_GiveItem. Sin esto, el objeto no aparecerá en el inventario correctamente. Secuencia obligatoria:
    - Condition_HasItem/HasFlag (verificar)
    - **Action_SetObjectVisible(obj_id, true)** ← NO OLVIDES ESTE PASO
    - Action_GiveItem(obj_id)
    - Action_SetFlag (evitar duplicados)
    - Action_ShowMessage
12. **MagicEnabled=true sin Abilities**: Si activas magia, DEBES crear habilidades en el array Abilities y asignarlas al jugador
13. **CombatEnabled=true sin equipamiento**: Si activas combate, el jugador DEBE tener arma inicial (InitialRightHandId)
14. **Abilities referenciadas sin definir**: Cada AbilityId en Player/Npc debe existir en el array Abilities
15. **OwnerType y OwnerId incoherentes**: El OwnerId DEBE ser el Id de una entidad del tipo OwnerType:
    - OwnerType=""Game"" → OwnerId=""game""
    - OwnerType=""Room"" → OwnerId=Id de una sala existente
    - OwnerType=""Npc"" → OwnerId=Id de un NPC existente
    - OwnerType=""GameObject"" → OwnerId=Id de un objeto existente
    - OwnerType=""Door"" → OwnerId=Id de una puerta existente
    - OwnerType=""Quest"" → OwnerId=Id de una misión existente
16. **Eventos incompatibles con OwnerType**: Cada evento solo funciona con ciertos OwnerTypes (ver lista de eventos). Ej: Event_OnEnter solo funciona con OwnerType=""Room""
17. **⚠️ Objetos dentro de contenedor con RoomId**: Si un objeto está en ContainedObjectIds de un contenedor, DEBE tener RoomId=null. Si tiene RoomId, aparecerá suelto en la sala aunque esté ""dentro"" del contenedor

## FORMATO DE SALIDA

**OBLIGATORIO: Tu respuesta debe ser ÚNICAMENTE un archivo descargable llamado `nuevo_mundo.xaw`**

### Reglas estrictas:
❌ NO escribas NADA de texto antes del archivo
❌ NO escribas NADA de texto después del archivo
❌ NO uses markdown code blocks
❌ NO ofrezcas ""continuar en el siguiente mensaje""
❌ NO digas que el contenido está ""truncado"" o ""compactado""
❌ NO hagas preguntas ni ofrezcas opciones
✅ Genera el archivo .xaw COMPLETO directamente para descargar
✅ Incluye TODO el contenido solicitado en UN SOLO archivo

### Requisitos del archivo:
- JSON válido y parseable
- COMPLETO con todas las salas, objetos, NPCs, scripts y misiones solicitados
- **NO uses caracteres especiales invisibles** (soft hyphens, zero-width spaces, etc.)
- Usa solo comillas rectas ("") nunca tipográficas ("")

### Si tienes límites de longitud:
Los archivos descargables pueden ser más largos que las respuestas de texto. Usa esa capacidad para generar el archivo COMPLETO.
Si aún así no cabe todo, reduce ligeramente las descripciones pero NUNCA omitas elementos estructurales (salas, objetos, NPCs, scripts).

**NUNCA pidas confirmación ni ofrezcas dividir el trabajo. Genera el archivo completo directamente.**

## CONSISTENCIA DE PUERTAS

Cuando una puerta conecta dos salas, **AMBAS salidas deben referenciar la misma puerta**:
- Si `door_X` conecta `room_A` con `room_B`:
  - La salida de `room_A` hacia `room_B` debe tener `DoorId: ""door_X""`
  - La salida de `room_B` hacia `room_A` debe tener `DoorId: ""door_X""`
- Si una puerta tiene llave, **no debe haber rutas alternativas** para saltársela

## ⚠️ REGLA CRÍTICA: ACCESIBILIDAD DE LLAVES

**NUNCA pongas una llave detrás de la puerta que abre.** Esto haría el juego imposible.

### Regla de oro:
La llave SIEMPRE debe estar en una zona accesible SIN pasar por la puerta que abre.

### Ejemplo INCORRECTO (imposible de resolver):
```
Sala_Inicial ──[puerta_cerrada]── Sala_Tesoro (contiene llave_puerta)
```
❌ El jugador empieza en Sala_Inicial pero la llave está en Sala_Tesoro, que está bloqueada. ¡IMPOSIBLE!

### Ejemplo CORRECTO:
```
Sala_Inicial ── Sala_Biblioteca (contiene llave_puerta)
      │
[puerta_cerrada]
      │
Sala_Tesoro
```
✅ El jugador puede ir a Sala_Biblioteca, coger la llave, y luego abrir la puerta.

### Verificación obligatoria:
Antes de finalizar, para CADA puerta cerrada con llave:
1. Identifica dónde está la llave (RoomId del objeto llave)
2. Traza el camino desde Game.StartRoomId hasta esa sala
3. Verifica que ese camino NO pase por la puerta que esa llave abre
4. Si no es posible llegar a la llave, MUEVE la llave a una sala accesible

## IMPORTANTE: SIN SPOILERS

**NO reveles al jugador detalles de la aventura.** Solo proporciona una breve descripción temática (1-2 frases) sin mencionar puzzles, soluciones, ubicación de objetos o secretos. El jugador quiere descubrir la aventura por sí mismo.

Genera el mundo con la temática ""{THEME}"" y puzzles lógicos acordes a esa ambientación.";

    public PromptGeneratorWindow()
    {
        InitializeComponent();
        UpdateSlidersFromRoomCount();
        UpdatePrompt();
    }

    private void UpdateSlidersFromRoomCount()
    {
        // Evitar ejecución durante InitializeComponent
        if (DoorsSlider == null || NpcsSlider == null || QuestsSlider == null)
            return;

        var roomCountText = RoomCountTextBox?.Text ?? "6";
        if (!int.TryParse(roomCountText, out var roomCount) || roomCount < 1)
            roomCount = 6;

        _isUpdatingFromRoomCount = true;

        // Fórmulas basadas en el número de salas
        DoorsSlider.Value = Math.Max(1, roomCount / 3);           // 1 puerta cada 3 salas
        NpcsSlider.Value = Math.Max(1, roomCount / 4);            // 1 NPC cada 4 salas (25% menos)
        QuestsSlider.Value = Math.Max(1, roomCount / 8);          // 1 misión cada 8 salas (25% menos)
        ContainersSlider.Value = Math.Max(1, roomCount / 5);      // 1 contenedor cada 5 salas (25% menos)

        // Tipos de objetos según salas (respetando estado de checkboxes)
        var combatEnabled = CombatCheckBox?.IsChecked == true;
        var basicNeedsEnabled = BasicNeedsCheckBox?.IsChecked == true;

        if (combatEnabled)
        {
            WeaponsSlider.Value = Math.Max(1, roomCount / 5);         // 1 arma cada 5 salas
            ArmorsSlider.Value = Math.Max(0, (roomCount - 5) / 6);    // armaduras solo en mundos grandes
            HelmetsSlider.Value = Math.Max(0, (roomCount - 8) / 12);  // cascos solo en mundos grandes (50% menos)
        }

        if (basicNeedsEnabled)
        {
            FoodSlider.Value = Math.Max(0, roomCount / 8);            // 1 comida cada 8 salas (50% menos)
            DrinksSlider.Value = Math.Max(0, roomCount / 10);         // 1 bebida cada 10 salas (50% menos)
        }

        KeysSlider.Value = Math.Max(1, roomCount / 6);            // 1 llave cada 6 salas
        TextsSlider.Value = Math.Max(0, roomCount / 10);          // 1 texto cada 10 salas (50% menos)
        OtherObjectsSlider.Value = Math.Max(1, roomCount / 6);    // objetos genéricos (50% menos)

        _isUpdatingFromRoomCount = false;
    }


    private void UpdatePrompt()
    {
        // Evitar ejecución durante InitializeComponent
        if (PromptTextBox == null)
            return;

        var theme = ThemeTextBox?.Text ?? "mansión embrujada";
        var roomCountText = RoomCountTextBox?.Text ?? "6";

        if (string.IsNullOrWhiteSpace(theme))
            theme = "mansión embrujada";

        if (!int.TryParse(roomCountText, out var roomCount) || roomCount < 1)
            roomCount = 6;

        // Obtener valores de los sliders generales
        var doorCount = DoorsSlider != null ? (int)DoorsSlider.Value : 2;
        var npcCount = NpcsSlider != null ? (int)NpcsSlider.Value : 2;
        var questCount = QuestsSlider != null ? (int)QuestsSlider.Value : 1;
        var containerCount = ContainersSlider != null ? (int)ContainersSlider.Value : 1;

        // Obtener valores de tipos de objetos
        var weaponCount = WeaponsSlider != null ? (int)WeaponsSlider.Value : 1;
        var armorCount = ArmorsSlider != null ? (int)ArmorsSlider.Value : 0;
        var foodCount = FoodSlider != null ? (int)FoodSlider.Value : 1;
        var drinkCount = DrinksSlider != null ? (int)DrinksSlider.Value : 1;
        var helmetCount = HelmetsSlider != null ? (int)HelmetsSlider.Value : 0;
        var keyCount = KeysSlider != null ? (int)KeysSlider.Value : 1;
        var textCount = TextsSlider != null ? (int)TextsSlider.Value : 1;
        var otherCount = OtherObjectsSlider != null ? (int)OtherObjectsSlider.Value : 2;

        // Calcular total de objetos
        var totalObjects = weaponCount + armorCount + foodCount + drinkCount +
                          helmetCount + keyCount + textCount + otherCount;

        // Obtener valores de los checkboxes de sistemas
        var craftingEnabled = CraftingCheckBox?.IsChecked == true;
        var combatEnabled = CombatCheckBox?.IsChecked == true;
        var magicEnabled = MagicCheckBox?.IsChecked == true;
        var basicNeedsEnabled = BasicNeedsCheckBox?.IsChecked == true;

        // Preparar valores condicionales del jugador según sistemas activos
        var playerInventoryExtra = combatEnabled ? ",\n      { \"ObjectId\": \"obj_espada_inicial\", \"Quantity\": 1 }" : "";
        var playerRightHand = combatEnabled ? "\"obj_espada_inicial\"" : "null";
        var playerTorso = combatEnabled ? "\"obj_armadura_inicial\"" : "null";
        var playerAbilities = magicEnabled ? "\"habilidad_magia_inicial\"" : "";

        // Generar instrucciones de sistemas según checkboxes
        var systemsInstructions = GenerateSystemsInstructions(craftingEnabled, combatEnabled, magicEnabled, basicNeedsEnabled);

        var prompt = PromptTemplate
            .Replace("{THEME}", theme)
            .Replace("{ROOM_COUNT}", roomCount.ToString())
            .Replace("{DOOR_COUNT}", doorCount.ToString())
            .Replace("{NPC_COUNT}", npcCount.ToString())
            .Replace("{QUEST_COUNT}", questCount.ToString())
            .Replace("{CONTAINER_COUNT}", containerCount.ToString())
            .Replace("{WEAPON_COUNT}", weaponCount.ToString())
            .Replace("{ARMOR_COUNT}", armorCount.ToString())
            .Replace("{FOOD_COUNT}", foodCount.ToString())
            .Replace("{DRINK_COUNT}", drinkCount.ToString())
            .Replace("{HELMET_COUNT}", helmetCount.ToString())
            .Replace("{KEY_COUNT}", keyCount.ToString())
            .Replace("{TEXT_COUNT}", textCount.ToString())
            .Replace("{OTHER_COUNT}", otherCount.ToString())
            .Replace("{TOTAL_OBJECTS}", totalObjects.ToString())
            // Sistemas
            .Replace("{CRAFTING_ENABLED}", craftingEnabled.ToString().ToLower())
            .Replace("{COMBAT_ENABLED}", combatEnabled.ToString().ToLower())
            .Replace("{MAGIC_ENABLED}", magicEnabled.ToString().ToLower())
            .Replace("{BASIC_NEEDS_ENABLED}", basicNeedsEnabled.ToString().ToLower())
            // Jugador condicional
            .Replace("{PLAYER_INITIAL_INVENTORY_EXTRA}", playerInventoryExtra)
            .Replace("{PLAYER_INITIAL_RIGHT_HAND}", playerRightHand)
            .Replace("{PLAYER_INITIAL_TORSO}", playerTorso)
            .Replace("{PLAYER_ABILITY_IDS}", playerAbilities)
            // Instrucciones de sistemas
            .Replace("{SYSTEMS_INSTRUCTIONS}", systemsInstructions);

        PromptTextBox.Text = prompt;
    }

    private string GenerateSystemsInstructions(bool crafting, bool combat, bool magic, bool basicNeeds)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("## SISTEMAS ACTIVOS EN ESTE MUNDO");
        sb.AppendLine();

        if (!crafting && !combat && !magic && !basicNeeds)
        {
            sb.AppendLine("**No hay sistemas especiales activos.** Este es un mundo de aventura simple sin combate ni necesidades.");
            sb.AppendLine("- NO crees objetos de tipo arma, armadura o casco");
            sb.AppendLine("- NO crees objetos de tipo comida o bebida");
            sb.AppendLine("- NO crees habilidades mágicas");
            sb.AppendLine("- El array Abilities debe estar vacío: `\"Abilities\": []`");
            return sb.ToString();
        }

        if (crafting)
        {
            sb.AppendLine("### ✅ Sistema de Fabricación (CraftingEnabled=true)");
            sb.AppendLine("- El jugador puede crear objetos combinando ingredientes");
            sb.AppendLine("- Crea objetos que puedan usarse como ingredientes");
            sb.AppendLine();
        }

        if (combat)
        {
            sb.AppendLine("### ✅ Sistema de Combate (CombatEnabled=true)");
            sb.AppendLine("- Activa: salud, combate por turnos con NPCs hostiles");
            sb.AppendLine("- **OBLIGATORIO**: Crea objetos de tipo arma y armadura");
            sb.AppendLine("- **OBLIGATORIO**: Equipa al jugador con arma inicial (InitialRightHandId)");
            sb.AppendLine("- Los NPCs pueden tener equipo y estadísticas de combate");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("### ❌ Sistema de Combate DESACTIVADO");
            sb.AppendLine("- NO crees objetos de tipo arma, armadura ni casco");
            sb.AppendLine("- NO asignes equipamiento al jugador (InitialRightHandId, InitialTorsoId = null)");
            sb.AppendLine();
        }

        if (magic)
        {
            sb.AppendLine("### ✅ Sistema de Magia (MagicEnabled=true)");
            sb.AppendLine("- Activa: uso de maná y habilidades mágicas");
            sb.AppendLine("- **OBLIGATORIO**: Crea habilidades en el array Abilities (mínimo 3-5 habilidades)");
            sb.AppendLine("- **OBLIGATORIO**: Asigna habilidades iniciales al jugador con AbilityIds");
            sb.AppendLine("- Los NPCs mágicos también pueden tener AbilityIds");
            sb.AppendLine();
            sb.AppendLine("#### Estructura de habilidades:");
            sb.AppendLine("```json");
            sb.AppendLine("{");
            sb.AppendLine("  \"Id\": \"habilidad_id\",");
            sb.AppendLine("  \"Name\": \"Nombre de la habilidad\",");
            sb.AppendLine("  \"Description\": \"Descripción\",");
            sb.AppendLine("  \"AbilityType\": \"Attack\",  // Attack o Defense");
            sb.AppendLine("  \"ManaCost\": 15,");
            sb.AppendLine("  \"AttackValue\": 5,");
            sb.AppendLine("  \"DefenseValue\": 0,");
            sb.AppendLine("  \"Damage\": 20,");
            sb.AppendLine("  \"Healing\": 0,");
            sb.AppendLine("  \"DamageType\": \"Magical\",  // Physical o Magical");
            sb.AppendLine("  \"StatusEffect\": null,");
            sb.AppendLine("  \"StatusEffectDuration\": 0,");
            sb.AppendLine("  \"TargetsSelf\": false");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();
        }
        else if (combat)
        {
            sb.AppendLine("### ❌ Sistema de Magia DESACTIVADO");
            sb.AppendLine("- NO crees habilidades mágicas");
            sb.AppendLine("- El array Abilities debe estar vacío: `\"Abilities\": []`");
            sb.AppendLine("- El combate será solo físico (armas y armaduras)");
            sb.AppendLine();
        }

        if (basicNeeds)
        {
            sb.AppendLine("### ✅ Sistema de Necesidades Básicas (BasicNeedsEnabled=true)");
            sb.AppendLine("- Activa: hambre, sed, sueño");
            sb.AppendLine("- El jugador debe comer, beber y dormir para sobrevivir");
            sb.AppendLine("- **OBLIGATORIO**: Crea objetos de tipo comida y bebida distribuidos por el mundo");
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine("### ❌ Sistema de Necesidades Básicas DESACTIVADO");
            sb.AppendLine("- NO crees objetos de tipo comida ni bebida");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void ThemeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdatePrompt();
    }

    private void RoomCountTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        UpdateSlidersFromRoomCount();
        UpdatePrompt();
    }

    private void RoomCountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Solo permitir números
        e.Handled = !int.TryParse(e.Text, out _);
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(PromptTextBox.Text);

        // Visual feedback
        var originalContent = CopyButton.Content;
        CopyButton.Content = "✓ Copiado!";
        CopyButton.IsEnabled = false;

        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = System.TimeSpan.FromSeconds(2)
        };
        timer.Tick += (s, args) =>
        {
            CopyButton.Content = originalContent;
            CopyButton.IsEnabled = true;
            timer.Stop();
        };
        timer.Start();
    }

    private async void GenerateLocalButton_Click(object sender, RoutedEventArgs e)
    {
        // Mostrar popup de servicios de IA y esperar a que Docker esté listo
        // Usamos llama3.1:70b porque es más capaz de seguir instrucciones complejas
        var progressWindow = new DockerProgressWindow
        {
            Owner = this,
            IncludeTts = false,
            IncludeStableDiffusion = false,
            IncludeOllama = true,
            OllamaModel = "llama3.1:70b"
        };

        var dockerResult = await progressWindow.RunAsync();
        if (!dockerResult.Success)
        {
            if (!dockerResult.Canceled)
            {
                DarkErrorDialog.Show("Error", "Error al iniciar los servicios de Docker.", this);
            }
            return; // Usuario canceló o hubo error
        }

        // Deshabilitar botón y mostrar progress bar mientras se genera
        GenerateLocalButton.IsEnabled = false;
        var originalContent = GenerateLocalButton.Content;
        GenerateLocalButton.Content = "⏳ Generando mundo...";
        GeneratingProgressBar.Visibility = Visibility.Visible;

        // Iniciar cronómetro para medir el tiempo de generación
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Enviar prompt a llama3.1:70b
            var jsonResponse = await GenerateWorldWithLocalLlmAsync(PromptTextBox.Text);

            if (string.IsNullOrWhiteSpace(jsonResponse))
            {
                DarkErrorDialog.Show("Error", "No se recibió respuesta del modelo de IA.", this);
                return;
            }

            // Extraer solo el JSON del response (puede venir con texto extra)
            var cleanJson = ExtractJsonFromResponse(jsonResponse);
            if (string.IsNullOrWhiteSpace(cleanJson))
            {
                var preview = jsonResponse.Length > 500
                    ? jsonResponse.Substring(0, 500) + "..."
                    : jsonResponse;
                DarkErrorDialog.Show("Error", $"La respuesta del modelo no contiene un JSON válido.\n\nRespuesta recibida:\n{preview}", this);
                return;
            }

            // Deserializar el JSON a WorldModel
            WorldModel? world;
            try
            {
                world = JsonSerializer.Deserialize<WorldModel>(cleanJson, _jsonOptions);
                if (world == null)
                {
                    DarkErrorDialog.Show("Error", "Error al deserializar el mundo generado.", this);
                    return;
                }
            }
            catch (JsonException ex)
            {
                DarkErrorDialog.Show("Error", $"JSON inválido:\n{ex.Message}", this);
                return;
            }

            // Determinar nombre de archivo basado en la temática
            var worldsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "worlds");
            if (!Directory.Exists(worldsFolder))
            {
                Directory.CreateDirectory(worldsFolder);
            }

            // Usar la temática como nombre base
            var baseFileName = ThemeTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                baseFileName = "nuevo_mundo";
            }

            // Limpiar caracteres inválidos y espacios
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                baseFileName = baseFileName.Replace(c.ToString(), "");
            }
            baseFileName = baseFileName.Replace(" ", "_").ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(baseFileName))
            {
                baseFileName = "nuevo_mundo";
            }

            // Auto-incrementar si ya existe
            var fileName = baseFileName;
            var filePath = Path.Combine(worldsFolder, $"{fileName}.xaw");
            var counter = 2;

            while (File.Exists(filePath))
            {
                fileName = $"{baseFileName}_{counter}";
                filePath = Path.Combine(worldsFolder, $"{fileName}.xaw");
                counter++;
            }

            // Guardar el archivo usando el mismo formato que el editor (base64+zip)
            await Task.Run(() => WorldLoader.SaveWorldModel(world, filePath));

            // Detener cronómetro y formatear tiempo
            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed;
            string timeText;
            if (elapsed.TotalHours >= 1)
            {
                timeText = $"{(int)elapsed.TotalHours}h {elapsed.Minutes}m {elapsed.Seconds}s";
            }
            else if (elapsed.TotalMinutes >= 1)
            {
                timeText = $"{elapsed.Minutes}m {elapsed.Seconds}s";
            }
            else
            {
                timeText = $"{elapsed.Seconds}s";
            }

            // Ocultar barra de progreso antes de mostrar la alerta
            GeneratingProgressBar.Visibility = Visibility.Collapsed;

            AlertWindow.Show("Mundo generado", $"Mundo guardado como:\n{fileName}.xaw\n\nTiempo de generación: {timeText}", this);

            // Cerrar la ventana
            DialogResult = true;
            Close();
        }
        catch (HttpRequestException ex)
        {
            if (IsLoaded)
                DarkErrorDialog.Show("Error", $"Error de conexión con el servicio de IA:\n{ex.Message}", this);
        }
        catch (TaskCanceledException)
        {
            // No mostrar alerta si se canceló porque el usuario cerró la ventana
            if (IsLoaded)
                DarkErrorDialog.Show("Cancelado", "La generación fue cancelada o excedió el tiempo límite.", this);
        }
        catch (Exception ex)
        {
            if (IsLoaded)
                DarkErrorDialog.Show("Error", $"Error al generar el mundo:\n{ex.Message}", this);
        }
        finally
        {
            if (IsLoaded)
            {
                GenerateLocalButton.Content = originalContent;
                GenerateLocalButton.IsEnabled = true;
                GeneratingProgressBar.Visibility = Visibility.Collapsed;
            }
        }
    }

    private static readonly HttpClient _ollamaClient = new()
    {
        BaseAddress = new Uri("http://localhost:11434/"),
        Timeout = TimeSpan.FromMinutes(10) // Dar tiempo suficiente para generar
    };

    private async Task<string?> GenerateWorldWithLocalLlmAsync(string prompt)
    {
        var requestBody = new
        {
            model = "llama3.1:70b",
            prompt = prompt,
            stream = false,
            options = new
            {
                num_ctx = 32768 // Aumentar contexto para el prompt largo (~17K tokens)
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _ollamaClient.PostAsync("api/generate", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("response", out var respElement))
        {
            return respElement.GetString();
        }

        return null;
    }

    private string? ExtractJsonFromResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return null;

        var text = response;

        // Intentar extraer JSON de bloques de código markdown (```json o ```)
        var codeBlockPatterns = new[]
        {
            @"```json\s*([\s\S]*?)\s*```",
            @"```\s*([\s\S]*?)\s*```"
        };

        foreach (var pattern in codeBlockPatterns)
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                var blockContent = match.Groups[1].Value.Trim();
                if (blockContent.StartsWith("{") && blockContent.EndsWith("}"))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(blockContent);
                        return blockContent;
                    }
                    catch { /* Continuar con otros métodos */ }
                }
            }
        }

        // Buscar el inicio del JSON (primera llave)
        var startIndex = text.IndexOf('{');
        if (startIndex < 0)
            return null;

        // Buscar el final del JSON (última llave)
        var endIndex = text.LastIndexOf('}');
        if (endIndex < 0 || endIndex <= startIndex)
            return null;

        var jsonCandidate = text.Substring(startIndex, endIndex - startIndex + 1);

        // Validar que es JSON válido
        try
        {
            using var doc = JsonDocument.Parse(jsonCandidate);
            return jsonCandidate;
        }
        catch
        {
            return null;
        }
    }

    private void DoorsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingFromRoomCount)
            UpdatePrompt();
    }

    private void NpcsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingFromRoomCount)
            UpdatePrompt();
    }

    private void QuestsSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingFromRoomCount)
            UpdatePrompt();
    }

    private void ContainersSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingFromRoomCount)
            UpdatePrompt();
    }

    private void ObjectTypeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!_isUpdatingFromRoomCount)
            UpdatePrompt();
    }

    private void SystemCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        UpdatePrompt();
    }

    private void CombatCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (MagicCheckBox == null || WeaponsSlider == null)
            return;

        var combatEnabled = CombatCheckBox.IsChecked == true;

        // Magia solo disponible si combate está activo
        MagicCheckBox.IsEnabled = combatEnabled;
        if (!combatEnabled)
            MagicCheckBox.IsChecked = false;

        // Habilitar/deshabilitar sliders de combate
        UpdateCombatSlidersState(combatEnabled);

        UpdatePrompt();
    }

    private void BasicNeedsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (FoodSlider == null)
            return;

        var basicNeedsEnabled = BasicNeedsCheckBox.IsChecked == true;

        // Habilitar/deshabilitar sliders de necesidades básicas
        UpdateBasicNeedsSlidersState(basicNeedsEnabled);

        UpdatePrompt();
    }

    private void UpdateCombatSlidersState(bool enabled)
    {
        // Color para estado deshabilitado
        var disabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));
        var enabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xBB, 0xBB, 0xBB));

        WeaponsSlider.IsEnabled = enabled;
        ArmorsSlider.IsEnabled = enabled;
        HelmetsSlider.IsEnabled = enabled;
        WeaponsLabel.Foreground = enabled ? enabledColor : disabledColor;
        ArmorsLabel.Foreground = enabled ? enabledColor : disabledColor;
        HelmetsLabel.Foreground = enabled ? enabledColor : disabledColor;

        if (!enabled)
        {
            WeaponsSlider.Value = 0;
            ArmorsSlider.Value = 0;
            HelmetsSlider.Value = 0;
        }
    }

    private void UpdateBasicNeedsSlidersState(bool enabled)
    {
        // Color para estado deshabilitado
        var disabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));
        var enabledColor = new System.Windows.Media.SolidColorBrush(
            System.Windows.Media.Color.FromRgb(0xBB, 0xBB, 0xBB));

        FoodSlider.IsEnabled = enabled;
        DrinksSlider.IsEnabled = enabled;
        FoodLabel.Foreground = enabled ? enabledColor : disabledColor;
        DrinksLabel.Foreground = enabled ? enabledColor : disabledColor;

        if (!enabled)
        {
            FoodSlider.Value = 0;
            DrinksSlider.Value = 0;
        }
    }
}
