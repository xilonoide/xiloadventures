using System.Windows;
using System.Windows.Input;

namespace XiloAdventures.Wpf.Windows;

public partial class PromptGeneratorWindow : Window
{
    private bool _isUpdatingFromRoomCount = false;

    private const string PromptTemplate = @"Necesito que generes un JSON para un motor de aventuras de texto/gráficas. El JSON debe representar un mundo completo de una aventura con temática: **{THEME}**.

## ESTRUCTURA DEL JSON

```json
{
  ""Game"": {
    ""Id"": ""game"",
    ""Title"": ""Título acorde a la temática {THEME}"",
    ""StartRoomId"": ""id_sala_inicial"",
    ""StartHour"": 9,
    ""StartWeather"": ""Despejado"",
    ""MinutesPerGameHour"": 6,
    ""ParserDictionaryJson"": null
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
    ""InitialMoney"": 50
  },
  ""Rooms"": [
    {
      ""Id"": ""room_id"",
      ""Name"": ""Nombre visible"",
      ""Description"": ""Descripción de la sala"",
      ""IsInterior"": false,
      ""IsIlluminated"": true,
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
      ""Type"": ""ninguno|arma|armadura|comida|bebida|ropa|llave|texto"",
      ""TextContent"": ""Contenido legible (solo para Type=texto)"",
      ""Gender"": ""Masculine|Feminine"",
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
      ""Price"": 10
    }
  ],
  ""Npcs"": [
    {
      ""Id"": ""npc_id"",
      ""Name"": ""Nombre"",
      ""Description"": ""Descripción del NPC y su personalidad"",
      ""RoomId"": ""room_id"",
      ""Visible"": true,
      ""InventoryObjectIds"": [],
      ""IsShopkeeper"": false,
      ""ShopInventory"": [],
      ""BuyPriceMultiplier"": 0.5,
      ""SellPriceMultiplier"": 1.0,
      ""PatrolRoute"": [],
      ""IsPatrolling"": false,
      ""PatrolMovementMode"": ""Turns"",
      ""PatrolSpeed"": 1,
      ""PatrolTimeInterval"": 3.0,
      ""IsFollowingPlayer"": false,
      ""FollowMovementMode"": ""Turns"",
      ""FollowSpeed"": 1,
      ""FollowTimeInterval"": 3.0,
      ""Stats"": {
        ""Level"": 1,
        ""Strength"": 5,
        ""Dexterity"": 5,
        ""Intelligence"": 5,
        ""MaxHealth"": 10,
        ""CurrentHealth"": 10,
        ""Money"": 0
      }
    }
  ],
  ""Doors"": [
    {
      ""Id"": ""door_id"",
      ""Name"": ""Nombre de la puerta"",
      ""Description"": ""Descripción opcional"",
      ""Gender"": ""Feminine"",
      ""RoomIdA"": ""sala_1"",
      ""RoomIdB"": ""sala_2"",
      ""IsOpen"": false,
      ""IsLocked"": true,
      ""KeyObjectId"": ""obj_llave o null"",
      ""OpenFromSide"": ""Both""
    }
  ],
  ""Quests"": [
    {
      ""Id"": ""quest_id"",
      ""Name"": ""Nombre misión"",
      ""Description"": ""Descripción"",
      ""Objectives"": [""Objetivo 1"", ""Objetivo 2""]
    }
  ],
  ""Conversations"": [
    {
      ""Id"": ""conversation_id"",
      ""Name"": ""Nombre de la conversación"",
      ""Nodes"": [
        { ""Id"": ""conv_start_1"", ""NodeType"": ""Conversation_Start"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
        { ""Id"": ""conv_say_1"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 300, ""Y"": 100, ""Properties"": { ""Text"": ""¡Hola viajero!"", ""SpeakerName"": """", ""Emotion"": ""Neutral"" } },
        { ""Id"": ""conv_end_1"", ""NodeType"": ""Conversation_End"", ""X"": 500, ""Y"": 100, ""Properties"": {} }
      ],
      ""Connections"": [
        { ""FromNodeId"": ""conv_start_1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""conv_say_1"", ""ToPortName"": ""Exec"" },
        { ""FromNodeId"": ""conv_say_1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""conv_end_1"", ""ToPortName"": ""Exec"" }
      ],
      ""StartNodeId"": ""conv_start_1""
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
  ""RoomPositions"": {
    ""room_id"": { ""X"": 80, ""Y"": 45 },
    ""otra_sala"": { ""X"": 80, ""Y"": -135 }
  }
}
```

## TIPOS DE NODOS DISPONIBLES PARA SCRIPTS

**IMPORTANTE**: Usa EXACTAMENTE estos TypeId. Si usas nombres incorrectos, los nodos no funcionarán.

### Eventos (inician el script, solo puerto de salida ""Exec""):
- `Event_OnGameStart` - Al iniciar el juego (OwnerType: Game)
- `Event_OnGameEnd` - Al finalizar el juego (OwnerType: Game)
- `Event_EveryMinute` - Cada minuto de juego (OwnerType: Game)
- `Event_EveryHour` - Cada hora de juego (OwnerType: Game)
- `Event_OnWeatherChange` - Al cambiar el clima (OwnerType: Game)
- `Event_OnEnter` - Al entrar a la sala (OwnerType: Room)
- `Event_OnExit` - Al salir de la sala (OwnerType: Room)
- `Event_OnTalk` - Al hablar con NPC (OwnerType: Npc)
- `Event_OnNpcSee` - Cuando el NPC ve al jugador entrar (OwnerType: Npc)
- `Event_OnNpcAttack` - Al atacar al NPC (OwnerType: Npc)
- `Event_OnNpcDeath` - Al morir el NPC (OwnerType: Npc)
- `Event_OnTake` - Al coger objeto (OwnerType: GameObject)
- `Event_OnDrop` - Al soltar objeto (OwnerType: GameObject)
- `Event_OnUse` - Al usar objeto (OwnerType: GameObject)
- `Event_OnExamine` - Al examinar objeto (OwnerType: GameObject)
- `Event_OnContainerOpen` - Al abrir contenedor (OwnerType: GameObject)
- `Event_OnContainerClose` - Al cerrar contenedor (OwnerType: GameObject)
- `Event_OnDoorOpen` - Al abrir puerta (OwnerType: Door)
- `Event_OnDoorClose` - Al cerrar puerta (OwnerType: Door)
- `Event_OnDoorLock` - Al bloquear puerta (OwnerType: Door)
- `Event_OnDoorUnlock` - Al desbloquear puerta (OwnerType: Door)
- `Event_OnDoorKnock` - Al llamar a la puerta (OwnerType: Door)
- `Event_OnQuestStart` - Al iniciar misión (OwnerType: Quest)
- `Event_OnQuestComplete` - Al completar misión (OwnerType: Quest)
- `Event_OnQuestFail` - Al fallar misión (OwnerType: Quest)
- `Event_OnObjectiveComplete` - Al completar un objetivo de misión (OwnerType: Quest)

### Condiciones (puerto entrada ""Exec"", puertos salida ""True""/""False""):
- `Condition_HasFlag` - Properties: { ""FlagName"": ""nombre"" }
- `Condition_HasItem` - Properties: { ""ObjectId"": ""obj_id"" }
- `Condition_IsDoorOpen` - Properties: { ""DoorId"": ""door_id"" }
- `Condition_CompareCounter` - Properties: { ""CounterName"": ""nombre"", ""Operator"": "">="", ""Value"": 5 } (Operators: ==, !=, <, <=, >, >=)
- `Condition_IsInRoom` - Properties: { ""RoomId"": ""room_id"" }
- `Condition_IsQuestStatus` - Properties: { ""QuestId"": ""quest_id"", ""Status"": ""InProgress"" } (Status: NotStarted, InProgress, Completed, Failed)
- `Condition_IsTimeOfDay` - Properties: { ""TimeRange"": ""Manana"" } (valores: Manana, Tarde, Noche, Madrugada)
- `Condition_IsNpcVisible` - Properties: { ""NpcId"": ""npc_id"" }
- `Condition_IsPatrolling` - Comprueba si NPC está patrullando. Properties: { ""NpcId"": ""npc_id"" }
- `Condition_IsFollowingPlayer` - Comprueba si NPC sigue al jugador. Properties: { ""NpcId"": ""npc_id"" }
- `Condition_Random` - Properties: { ""Probability"": 50 } (0-100)

### Acciones (puerto entrada ""Exec"", puerto salida ""Exec""):
- `Action_ShowMessage` - Properties: { ""Message"": ""texto"" }
- `Action_GiveItem` - Da un objeto al jugador. Properties: { ""ObjectId"": ""obj_id"" } **IMPORTANTE: El objeto NO debe estar en InventoryObjectIds de ningún NPC**
- `Action_RemoveItem` - Properties: { ""ObjectId"": ""obj_id"" }
- `Action_OpenDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_CloseDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_UnlockDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_LockDoor` - Properties: { ""DoorId"": ""door_id"" }
- `Action_SetFlag` - Properties: { ""FlagName"": ""nombre"", ""Value"": true }
- `Action_SetCounter` - Properties: { ""CounterName"": ""nombre"", ""Value"": 0 }
- `Action_IncrementCounter` - Properties: { ""CounterName"": ""nombre"", ""Amount"": 1 }
- `Action_TeleportPlayer` - Properties: { ""RoomId"": ""room_id"" }
- `Action_MoveNpc` - Properties: { ""NpcId"": ""npc_id"", ""RoomId"": ""room_id"" }
- `Action_StartPatrol` - Inicia patrulla del NPC. Properties: { ""NpcId"": ""npc_id"" }
- `Action_StopPatrol` - Detiene patrulla del NPC. Properties: { ""NpcId"": ""npc_id"" }
- `Action_PatrolStep` - Mueve manualmente el NPC al siguiente punto de su ruta. Properties: { ""NpcId"": ""npc_id"" }
- `Action_SetPatrolMode` - Configura modo de patrulla. Properties: { ""NpcId"": ""npc_id"", ""Mode"": ""Turns"", ""TurnSpeed"": 1, ""TimeInterval"": 3.0 } (Mode: Turns/Time)
- `Action_FollowPlayer` - NPC empieza a seguir al jugador. Properties: { ""NpcId"": ""npc_id"", ""Speed"": 1 }
- `Action_StopFollowing` - NPC deja de seguir al jugador. Properties: { ""NpcId"": ""npc_id"" }
- `Action_SetFollowMode` - Configura modo de seguimiento. Properties: { ""NpcId"": ""npc_id"", ""Mode"": ""Turns"", ""TurnSpeed"": 1, ""TimeInterval"": 3.0 } (Mode: Turns/Time)
- `Action_StartQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_CompleteQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_FailQuest` - Properties: { ""QuestId"": ""quest_id"" }
- `Action_SetNpcVisible` - Properties: { ""NpcId"": ""npc_id"", ""Visible"": true }
- `Action_SetObjectVisible` - Properties: { ""ObjectId"": ""obj_id"", ""Visible"": true }
- `Action_AddMoney` - Properties: { ""Amount"": 10 }
- `Action_RemoveMoney` - Properties: { ""Amount"": 10 }
- `Action_PlaySound` - Properties: { ""SoundId"": ""fx_id"" }
- `Action_StartConversation` - Inicia una conversación con un NPC. Properties: { ""NpcId"": ""npc_id"" }

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

## REQUISITOS DEL MUNDO

Genera un mundo con temática ""{THEME}"" que contenga:
1. **Aproximadamente {ROOM_COUNT} salas** conectadas lógicamente formando un mapa coherente
   - Usa IsInterior=true para interiores (cuevas, edificios) y false para exteriores
   - Usa IsIlluminated=false para salas oscuras que requieren luz (antorcha, linterna)
2. **{DOOR_COUNT} puertas** - al menos una cerrada con llave si hay llaves disponibles
   - OpenFromSide: ""Both"" (ambos lados), ""FromAOnly"" (solo desde sala A), ""FromBOnly"" (solo desde sala B)
3. **{CONTAINER_COUNT} contenedores** (cofres, cajas, armarios...) con objetos dentro
   - IsOpenable=true para contenedores que se abren/cierran
   - IsLocked=true + KeyId para contenedores con cerradura
   - ContentsVisible=false para ocultar contenido hasta abrir
4. **{TOTAL_OBJECTS} objetos** distribuidos así:
   - {WEAPON_COUNT} armas (Type=""arma"") - espadas, dagas, arcos...
   - {ARMOR_COUNT} armaduras (Type=""armadura"") - escudos, cascos, corazas...
   - {FOOD_COUNT} comida (Type=""comida"") - pan, manzana, carne...
   - {DRINK_COUNT} bebidas (Type=""bebida"") - pociones, agua, vino...
   - {CLOTHING_COUNT} ropa (Type=""ropa"") - capas, túnicas, botas...
   - {KEY_COUNT} llaves (Type=""llave"") - para abrir puertas/contenedores
   - {TEXT_COUNT} documentos legibles (Type=""texto"") - cartas, diarios, pergaminos... con TextContent
   - {OTHER_COUNT} objetos genéricos (Type=""ninguno"") - gemas, joyas, herramientas, objetos de puzzle...
   - Usa Visible=false para objetos ocultos que aparecen mediante scripts
5. **{NPC_COUNT} NPCs** con personalidad acorde a la temática:
   - Si es comerciante: IsShopkeeper=true y añade IDs de objetos a ShopInventory
   - Si lleva objetos (que puede dar/intercambiar): añade IDs a InventoryObjectIds
   - Si tiene diálogo: crea un Script para el NPC con nodos Conversation_* (ver punto 8)
   - **Stats de combate**: Level, Strength, Dexterity, Intelligence, MaxHealth, CurrentHealth, Money
   - **Patrulla por turnos**: PatrolRoute con lista de IDs de salas conectadas. PatrolMovementMode=""Turns"", PatrolSpeed=1 (cada turno), 2 (lento), 3 (muy lento). IsPatrolling=true para empezar patrullando.
   - **Patrulla por tiempo**: PatrolMovementMode=""Time"", PatrolTimeInterval=3.0 (segundos entre movimientos)
   - **Seguimiento por turnos**: FollowMovementMode=""Turns"", FollowSpeed=1/2/3. Actívalo con `Action_FollowPlayer`.
   - **Seguimiento por tiempo**: FollowMovementMode=""Time"", FollowTimeInterval=3.0 (segundos)
   - Usa Visible=false para NPCs ocultos que aparecen mediante scripts
   - **Al menos 1 NPC con ruta de patrulla definida** (guardia, explorador, etc.)
6. **{QUEST_COUNT} misiones** con objetivos definidos en el array Objectives
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
   - **Al menos un script que complete la misión** (`Action_CompleteQuest`) cuando se cumpla un objetivo final
8. **Diálogos de NPCs** mediante Scripts:
   - Crea un Script con OwnerType=""Npc"" y OwnerId=id_del_npc
   - Usa nodos Conversation_Start → Conversation_NpcSay → Conversation_End
   - Usa Conversation_PlayerChoice para diálogos con opciones
   - **Connections entre nodos de conversación**: Usa los nombres de puerto correctos (Exec, Option1, Option2, True, False, OnClose, etc.)

## NOTAS IMPORTANTES
- Los IDs deben ser snake_case únicos
- **Game.StartRoomId DEBE coincidir con el Id de una sala existente** - El jugador empieza ahí
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
- **Type de objetos SOLO puede ser uno de estos valores exactos**: ninguno, arma, armadura, comida, bebida, ropa, llave, texto
- **Los objetos que son llaves DEBEN tener Type=""llave""**
- **Los objetos de tipo ""texto"" DEBEN tener TextContent** con el texto legible (carta, diario, pergamino, libro, nota...). El jugador usará el comando ""leer"" para ver este contenido.
- **Puerta cerrada con llave**: `IsLocked=true` + `KeyObjectId` con el Id de un objeto llave existente
- **Puerta que se abre con puzzle/script**: `IsLocked=false` + `IsOpen=false`. Usa `Action_OpenDoor` en el script cuando se resuelva el puzzle. **NO uses IsLocked=true sin KeyObjectId** - el motor no lo permite
- **Si un contenedor tiene IsLocked=true, DEBE tener KeyId con el Id de un objeto llave existente** (contenedor bloqueado requiere llave)
- **Varía las llaves**: No uses la misma llave para abrir muchas cosas. Crea llaves específicas para cada cerradura importante

### ⚠️ Objetos dentro de contenedores (MUY IMPORTANTE)
- **Si un objeto está en ContainedObjectIds de un contenedor, DEBE tener RoomId=null**
- El motor usa RoomId para determinar si el objeto está suelto en una sala. Si tiene RoomId, aparecerá en la sala aunque esté ""dentro"" del contenedor
- Ejemplo correcto: cofre en sala_1 con llave dentro → cofre.RoomId=""sala_1"", llave.RoomId=null, cofre.ContainedObjectIds=[""llave""]

### ⚠️ Objetos que un NPC ""da"" al jugador (MUY IMPORTANTE)
- Si un NPC debe dar un objeto mediante script (Action_GiveItem), el objeto debe tener **RoomId=null** y **Visible=false** y **NO estar en InventoryObjectIds del NPC**
- **⚠️ OBLIGATORIO: Si el objeto tiene Visible=false, SIEMPRE debes llamar Action_SetObjectVisible(obj_id, true) ANTES de Action_GiveItem**. Sin esto el objeto no funcionará correctamente.
- **Secuencia OBLIGATORIA para dar objetos ocultos** (ver ejemplo ""script_npc_give_item"" en la plantilla JSON):
  1. Condition_HasFlag(""objeto_entregado"") → Si True: mostrar ""Ya te lo di""
  2. Si False → **Action_SetObjectVisible(obj_id, true)** ← NUNCA OMITIR ESTE PASO
  3. Action_GiveItem(obj_id)
  4. Action_SetFlag(""objeto_entregado"", true)
  5. Action_ShowMessage con diálogo
- **InventoryObjectIds** de los NPCs es solo para objetos que el NPC TIENE permanentemente (ej: un tendero con su mercancía)

### Género gramatical (Gender)
- **Gender** indica el género gramatical en español para artículos (el/la): `Masculine` o `Feminine`
- Ejemplos: ""espada"" → Feminine, ""libro"" → Masculine, ""llave"" → Feminine, ""cofre"" → Masculine
- Puertas también tienen Gender (por defecto Feminine: ""la puerta"")

### Configuración de tiempo y clima (Game)
- **StartHour**: Hora inicial del juego (0-23). Ej: 9 para las 9:00, 21 para las 21:00
- **StartWeather**: Clima inicial. Valores EXACTOS: ""Despejado"", ""Lluvioso"", ""Nublado"", ""Tormenta""
- **MinutesPerGameHour**: Minutos reales por hora de juego (1-10). Ej: 6 = cada 6 min reales pasa 1 hora en el juego

### Diccionario del parser (Game.ParserDictionaryJson)
- Permite añadir sinónimos personalizados para verbos, sustantivos y adjetivos
- Si no necesitas sinónimos extra, deja **null**
- Formato JSON (como string escapado):
```json
""ParserDictionaryJson"": ""{\""verbs\"": {\""atacar\"": [\""golpear\"", \""apuñalar\""]}, \""nouns\"": {\""gema\"": [\""joya\"", \""piedra\""]}}""
```
- **NOTA**: El motor ya reconoce verbos básicos (coger, examinar, abrir, ir...) y artículos (el, la, un...). Solo añade sinónimos específicos de tu aventura.

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
9. **Objetos dados por NPC en InventoryObjectIds**: Los objetos que se dan con Action_GiveItem NO deben estar en InventoryObjectIds
10. **Puzzle de puerta mal configurado**: Para puzzles usa IsLocked=false + IsOpen=false + Action_OpenDoor. NUNCA uses IsLocked=true sin KeyObjectId
11. **⚠️ CRÍTICO - Action_GiveItem sin Action_SetObjectVisible**: Si el objeto tiene Visible=false, SIEMPRE debes añadir Action_SetObjectVisible(obj, true) ANTES de Action_GiveItem. Sin esto, el objeto no aparecerá en el inventario correctamente. Secuencia obligatoria:
    - Condition_HasItem/HasFlag (verificar)
    - **Action_SetObjectVisible(obj_id, true)** ← NO OLVIDES ESTE PASO
    - Action_GiveItem(obj_id)
    - Action_SetFlag (evitar duplicados)
    - Action_ShowMessage
12. **OwnerType y OwnerId incoherentes**: El OwnerId DEBE ser el Id de una entidad del tipo OwnerType:
    - OwnerType=""Game"" → OwnerId=""game""
    - OwnerType=""Room"" → OwnerId=Id de una sala existente
    - OwnerType=""Npc"" → OwnerId=Id de un NPC existente
    - OwnerType=""GameObject"" → OwnerId=Id de un objeto existente
    - OwnerType=""Door"" → OwnerId=Id de una puerta existente
    - OwnerType=""Quest"" → OwnerId=Id de una misión existente
13. **Eventos incompatibles con OwnerType**: Cada evento solo funciona con ciertos OwnerTypes (ver lista de eventos). Ej: Event_OnEnter solo funciona con OwnerType=""Room""
14. **⚠️ Objetos dentro de contenedor con RoomId**: Si un objeto está en ContainedObjectIds de un contenedor, DEBE tener RoomId=null. Si tiene RoomId, aparecerá suelto en la sala aunque esté ""dentro"" del contenedor

## FORMATO DE SALIDA

**IMPORTANTE: Genera el resultado como un archivo descargable con extensión .xaw** (no como texto en el chat).
- El JSON debe ser válido y parseable
- Sin markdown code blocks, solo el JSON puro
- El archivo .xaw se abrirá directamente en el editor XiloAdventures
- **NO uses caracteres especiales invisibles** (soft hyphens, zero-width spaces, etc.) - solo caracteres UTF-8 estándar
- Usa solo comillas rectas ("") nunca comillas tipográficas ("")

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
        NpcsSlider.Value = Math.Max(1, roomCount / 3);            // 1 NPC cada 3 salas
        QuestsSlider.Value = Math.Max(1, roomCount / 6);          // 1 misión cada 6 salas
        ContainersSlider.Value = Math.Max(1, roomCount / 4);      // 1 contenedor cada 4 salas

        // Tipos de objetos según salas
        WeaponsSlider.Value = Math.Max(1, roomCount / 5);         // 1 arma cada 5 salas
        ArmorsSlider.Value = Math.Max(0, (roomCount - 5) / 6);    // armaduras solo en mundos grandes
        FoodSlider.Value = Math.Max(1, roomCount / 4);            // 1 comida cada 4 salas
        DrinksSlider.Value = Math.Max(1, roomCount / 5);          // 1 bebida cada 5 salas
        ClothingSlider.Value = Math.Max(0, (roomCount - 4) / 8);  // ropa solo en mundos medianos+
        KeysSlider.Value = Math.Max(1, roomCount / 6);            // 1 llave cada 6 salas
        TextsSlider.Value = Math.Max(1, roomCount / 5);           // 1 texto cada 5 salas
        OtherObjectsSlider.Value = Math.Max(2, roomCount / 3);    // objetos genéricos

        _isUpdatingFromRoomCount = false;
    }

    private void UpdateSliderValueTexts()
    {
        // Sliders generales
        if (DoorsValueText != null)
            DoorsValueText.Text = ((int)DoorsSlider.Value).ToString();
        if (NpcsValueText != null)
            NpcsValueText.Text = ((int)NpcsSlider.Value).ToString();
        if (QuestsValueText != null)
            QuestsValueText.Text = ((int)QuestsSlider.Value).ToString();
        if (ContainersValueText != null)
            ContainersValueText.Text = ((int)ContainersSlider.Value).ToString();

        // Sliders de tipos de objetos
        if (WeaponsValueText != null)
            WeaponsValueText.Text = ((int)WeaponsSlider.Value).ToString();
        if (ArmorsValueText != null)
            ArmorsValueText.Text = ((int)ArmorsSlider.Value).ToString();
        if (FoodValueText != null)
            FoodValueText.Text = ((int)FoodSlider.Value).ToString();
        if (DrinksValueText != null)
            DrinksValueText.Text = ((int)DrinksSlider.Value).ToString();
        if (ClothingValueText != null)
            ClothingValueText.Text = ((int)ClothingSlider.Value).ToString();
        if (KeysValueText != null)
            KeysValueText.Text = ((int)KeysSlider.Value).ToString();
        if (TextsValueText != null)
            TextsValueText.Text = ((int)TextsSlider.Value).ToString();
        if (OtherObjectsValueText != null)
            OtherObjectsValueText.Text = ((int)OtherObjectsSlider.Value).ToString();
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
        var clothingCount = ClothingSlider != null ? (int)ClothingSlider.Value : 0;
        var keyCount = KeysSlider != null ? (int)KeysSlider.Value : 1;
        var textCount = TextsSlider != null ? (int)TextsSlider.Value : 1;
        var otherCount = OtherObjectsSlider != null ? (int)OtherObjectsSlider.Value : 2;

        // Calcular total de objetos
        var totalObjects = weaponCount + armorCount + foodCount + drinkCount +
                          clothingCount + keyCount + textCount + otherCount;

        UpdateSliderValueTexts();

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
            .Replace("{CLOTHING_COUNT}", clothingCount.ToString())
            .Replace("{KEY_COUNT}", keyCount.ToString())
            .Replace("{TEXT_COUNT}", textCount.ToString())
            .Replace("{OTHER_COUNT}", otherCount.ToString())
            .Replace("{TOTAL_OBJECTS}", totalObjects.ToString());

        PromptTextBox.Text = prompt;
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
}
