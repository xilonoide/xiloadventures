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

    private const string PromptTemplate = @"Genera un JSON para motor de aventuras texto/gráficas. Temática: **{THEME}**.

## ESTRUCTURA JSON

```json
{
  ""Game"": {
    ""Id"": ""xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"",
    ""Title"": ""Título {THEME}"",
    ""Theme"": ""{THEME}"",
    ""StartRoomId"": ""id_sala_inicial"",
    ""StartHour"": 9,
    ""StartWeather"": ""Despejado"",
    ""MinutesPerGameHour"": 6,
    ""ParserDictionaryJson"": null,
    ""EncryptionKey"": ""XXXXXXXX"",
    ""EndingText"": ""Felicitación al completar"",
    ""CraftingEnabled"": {CRAFTING_ENABLED},
    ""CombatEnabled"": {COMBAT_ENABLED},
    ""MagicEnabled"": {MAGIC_ENABLED},
    ""BasicNeedsEnabled"": {BASIC_NEEDS_ENABLED}
  },
  ""Player"": {
    ""Name"": ""Nombre protagonista"",
    ""Age"": 25, ""Weight"": 70, ""Height"": 170,
    ""Strength"": 20, ""Constitution"": 20, ""Intelligence"": 20, ""Dexterity"": 20, ""Charisma"": 20,
    ""InitialMoney"": 50,
    ""MaxInventoryWeight"": -1, ""MaxInventoryVolume"": -1,
    ""InitialInventory"": [{ ""ObjectId"": ""obj_linterna"", ""Quantity"": 1 }{PLAYER_INITIAL_INVENTORY_EXTRA}],
    ""InitialRightHandId"": {PLAYER_INITIAL_RIGHT_HAND},
    ""InitialLeftHandId"": null,
    ""InitialTorsoId"": {PLAYER_INITIAL_TORSO},
    ""InitialHeadId"": null,
    ""AbilityIds"": [{PLAYER_ABILITY_IDS}]
  },
  ""Rooms"": [{
    ""Id"": ""room_id"", ""Zone"": ""nombre_zona"", ""Name"": ""Nombre"", ""Description"": ""Descripción"",
    ""IsInterior"": false, ""IsIlluminated"": true, ""MusicId"": null,
    ""Exits"": [{ ""TargetRoomId"": ""otra_sala"", ""Direction"": ""norte"", ""DoorId"": null }]
  }],
  ""Objects"": [{
    ""Id"": ""obj_id"", ""Name"": ""Nombre"", ""Description"": ""Descripción"",
    ""Type"": ""ninguno|arma|armadura|casco|escudo|comida|bebida|llave"",
    ""Gender"": ""Masculine|Feminine"", ""IsPlural"": false,
    ""RoomId"": ""room_id o null"", ""Visible"": true, ""CanTake"": true,
    ""CanRead"": false, ""TextContent"": null,
    ""IsContainer"": false, ""IsOpenable"": false, ""IsOpen"": true, ""IsLocked"": false,
    ""KeyId"": null, ""ContentsVisible"": true, ""ContainedObjectIds"": [], ""MaxCapacity"": 50000,
    ""Volume"": 10, ""Weight"": 100, ""Price"": 10,
    ""AttackBonus"": 0, ""DefenseBonus"": 0, ""HandsRequired"": 1, ""DamageType"": ""Physical"",
    ""NutritionAmount"": 0,
    ""IsLightSource"": false, ""IsLit"": false, ""LightTurnsRemaining"": -1
  }],
  ""Npcs"": [{
    ""Id"": ""npc_id"", ""Name"": ""Nombre"", ""Description"": ""Descripción"",
    ""RoomId"": ""room_id"", ""Visible"": true,
    ""Inventory"": [], ""EquippedRightHandId"": null, ""EquippedLeftHandId"": null, ""EquippedTorsoId"": null, ""EquippedHeadId"": null,
    ""IsShopkeeper"": false, ""ShopInventory"": [], ""BuyPriceMultiplier"": 0.5, ""SellPriceMultiplier"": 1.0, ""Money"": 0,
    ""PatrolRoute"": [], ""IsPatrolling"": false, ""PatrolMovementMode"": ""Turns"", ""PatrolSpeed"": 1,
    ""IsFollowingPlayer"": false, ""FollowMovementMode"": ""Turns"", ""FollowSpeed"": 1,
    ""Stats"": { ""Strength"": 5, ""Dexterity"": 5, ""Intelligence"": 5, ""MaxHealth"": 10, ""CurrentHealth"": 10 },
    ""AbilityIds"": [], ""IsCorpse"": false
  }],
  ""Doors"": [{
    ""Id"": ""door_id"", ""Name"": ""Puerta"", ""Description"": """",
    ""Gender"": ""Feminine"", ""IsPlural"": false,
    ""RoomIdA"": ""sala_1"", ""RoomIdB"": ""sala_2"",
    ""IsOpen"": false, ""IsLocked"": true, ""KeyObjectId"": ""obj_llave"",
    ""OpenFromSide"": ""Both"", ""Visible"": true
  }],
  ""Quests"": [{
    ""Id"": ""quest_id"", ""Name"": ""Misión"", ""Description"": ""Descripción"",
    ""IsMainQuest"": true, ""Objectives"": [""Objetivo 1"", ""Objetivo 2""]
  }],
  ""Conversations"": [{
    ""Id"": ""conv_id"", ""Name"": ""Diálogo NPC"", ""StartNodeId"": ""c1"",
    ""Nodes"": [
      { ""Id"": ""c1"", ""NodeType"": ""Conversation_Start"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
      { ""Id"": ""c2"", ""NodeType"": ""Conversation_NpcSay"", ""X"": 300, ""Y"": 100, ""Properties"": { ""Text"": ""Hola"", ""Emotion"": ""Neutral"" } },
      { ""Id"": ""c3"", ""NodeType"": ""Conversation_End"", ""X"": 500, ""Y"": 100, ""Properties"": {} }
    ],
    ""Connections"": [
      { ""FromNodeId"": ""c1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c2"", ""ToPortName"": ""Exec"" },
      { ""FromNodeId"": ""c2"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""c3"", ""ToPortName"": ""Exec"" }
    ]
  }],
  ""Scripts"": [{
    ""Id"": ""script_id"", ""Name"": ""Script"", ""OwnerType"": ""Game"", ""OwnerId"": ""game"",
    ""Nodes"": [
      { ""Id"": ""n1"", ""NodeType"": ""Event_OnGameStart"", ""X"": 100, ""Y"": 100, ""Properties"": {} },
      { ""Id"": ""n2"", ""NodeType"": ""Action_StartQuest"", ""X"": 300, ""Y"": 100, ""Properties"": { ""QuestId"": ""quest_principal"" } }
    ],
    ""Connections"": [{ ""FromNodeId"": ""n1"", ""FromPortName"": ""Exec"", ""ToNodeId"": ""n2"", ""ToPortName"": ""Exec"" }]
  }],
  ""Abilities"": [{
    ""Id"": ""hab_id"", ""Name"": ""Habilidad"", ""Description"": ""Desc"",
    ""AbilityType"": ""Attack"", ""ManaCost"": 15, ""Damage"": 20, ""Healing"": 0,
    ""DamageType"": ""Magical"", ""TargetsSelf"": false
  }],
  ""RoomPositions"": { ""room_id"": { ""X"": 80, ""Y"": 45 } }
}
```

## NODOS DE SCRIPTS (usa TypeId EXACTOS)

### Eventos (puerto salida ""Exec""):
**Game:** Event_OnGameStart, Event_OnGameEnd, Event_EveryMinute, Event_EveryHour, Event_OnTurnStart, Event_OnWeatherChange, Event_OnPlayerDeath, Event_OnCombatStart/Victory/Defeat/Flee, Event_OnHealthLow/Critical, Event_OnHungerHigh, Event_OnThirstHigh, Event_OnManaLow, Event_OnStateThreshold{StateName,Threshold}, Event_OnMoneyGained/Lost
**Room:** Event_OnEnter, Event_OnExit, Event_OnLook
**Npc:** Event_OnTalk, Event_OnNpcSee, Event_OnNpcAttack, Event_OnNpcDeath, Event_OnNpcTurn, Event_OnTradeStart/End, Event_OnItemBought/Sold
**GameObject:** Event_OnTake, Event_OnDrop, Event_OnUse, Event_OnGive, Event_OnExamine, Event_OnEquip, Event_OnUnequip, Event_OnContainerOpen/Close, Event_OnEat, Event_OnDrink
**Door:** Event_OnDoorOpen/Close/Lock/Unlock
**Quest:** Event_OnQuestStart/Complete/Fail, Event_OnObjectiveComplete

### Condiciones (entrada ""Exec"", salidas ""True""/""False""):
**Variables:** Condition_HasFlag{FlagName}, Condition_CompareCounter{CounterName,Operator,Value}, Condition_Random{Probability:0-100}
**Inventario:** Condition_HasItem{ObjectId}, Condition_PlayerOwnsItem{ObjectId}, Condition_NpcHasItem{NpcId,ObjectId}
**Ubicación:** Condition_IsInRoom{RoomId}, Condition_ObjectInRoom{ObjectId,RoomId}, Condition_NpcInRoom{NpcId,RoomId}
**Puertas/Contenedores:** Condition_IsDoorOpen{DoorId}, Condition_IsContainerOpen{ObjectId}, Condition_IsContainerLocked{ObjectId}
**Objetos:** Condition_IsObjectVisible{ObjectId}, Condition_IsObjectLit{ObjectId}, Condition_IsRoomLit{RoomId}
**NPCs:** Condition_IsNpcVisible{NpcId}, Condition_IsPatrolling{NpcId}, Condition_IsFollowingPlayer{NpcId}, Condition_IsNpcAlive{NpcId}, Condition_NpcHealthBelow{NpcId,Threshold}
**Jugador:** Condition_IsPlayerAlive, Condition_PlayerHealthBelow/Above{Threshold}, Condition_PlayerHasMoney{Amount}, Condition_PlayerHasEquipped{Slot}, Condition_PlayerStateAbove/Below{StateName,Threshold}
**Misiones:** Condition_IsQuestStatus{QuestId,Status:NotStarted|InProgress|Completed|Failed}
**Tiempo:** Condition_IsTimeOfDay{TimeRange:Manana|Tarde|Noche|Madrugada}, Condition_IsWeather{Weather}
**Combate:** Condition_IsInCombat, Condition_IsInTrade

### Acciones (entrada ""Exec"", salida ""Exec""):
**Mensajes:** Action_ShowMessage{Message}, Action_PlaySound{SoundId}, Action_StartConversation{NpcId}
**Variables:** Action_SetFlag{FlagName,Value}, Action_SetCounter{CounterName,Value}, Action_IncrementCounter{CounterName,Amount}
**Inventario:** Action_GiveItem{ObjectId}, Action_RemoveItem{ObjectId}, Action_AddItemToNpcInventory{NpcId,ObjectId}
**Equipamiento:** Action_EquipPlayerItem{ObjectId,Slot}, Action_UnequipPlayerSlot{Slot} (Slots: RightHand,LeftHand,Torso,Head)
**Puertas:** Action_OpenDoor{DoorId}, Action_CloseDoor{DoorId}, Action_LockDoor{DoorId}, Action_UnlockDoor{DoorId}
**Contenedores:** Action_OpenContainer{ObjectId}, Action_CloseContainer{ObjectId}, Action_PutObjectInContainer{ObjectId,ContainerId}
**Objetos:** Action_SetObjectVisible{ObjectId,Visible}, Action_MoveObjectToRoom{ObjectId,RoomId}, Action_SetObjectLit{ObjectId,IsLit}
**Movimiento:** Action_TeleportPlayer{RoomId}, Action_MoveNpc{NpcId,RoomId}, Action_StartPatrol{NpcId}, Action_StopPatrol{NpcId}, Action_FollowPlayer{NpcId,Speed}, Action_StopFollowing{NpcId}
**NPCs:** Action_SetNpcVisible{NpcId,Visible}, Action_KillNpc{NpcId}, Action_DamageNpc{NpcId,Amount}, Action_HealNpc{NpcId,Amount}
**Misiones:** Action_StartQuest{QuestId}, Action_CompleteQuest{QuestId}, Action_FailQuest{QuestId}, Action_AdvanceObjective{QuestId}
**Dinero:** Action_AddMoney{Amount}, Action_RemoveMoney{Amount}
**Jugador:** Action_HealPlayer{Amount}, Action_DamagePlayer{Amount}, Action_SetPlayerState{StateName,Value}, Action_ModifyPlayerState{StateName,Amount}
**Combate:** Action_StartCombat{NpcId}, Action_EndCombatVictory, Action_EndCombatDefeat
**Comercio:** Action_OpenTrade{NpcId}, Action_CloseTrade
**Habilidades:** Action_AddAbility{AbilityId}, Action_RemoveAbility{AbilityId}
**Sala:** Action_SetRoomIllumination{RoomId,IsIlluminated}, Action_SetRoomDescription{RoomId,Description}
**Tiempo:** Action_SetWeather{Weather}, Action_SetGameHour{Hour}, Action_AdvanceTime{Hours}

### Control flujo:
Flow_Sequence (salidas: Then0,Then1,Then2), Flow_Branch (salidas: True,False), Flow_RandomBranch (salidas: Out0,Out1,Out2), Flow_Delay{Seconds}

### Nodos conversación (array Conversations):
- `Conversation_Start` - Inicio (puerto: Exec)
- `Conversation_NpcSay` - {Text, Emotion:Neutral|Feliz|Triste|Enfadado|Sorprendido}
- `Conversation_PlayerChoice` - {Text1,Text2,Text3,Text4} (salidas: Option1-4)
- `Conversation_Branch` - ConditionType: HasFlag{FlagName}, HasItem{ItemId}, HasMoney{MoneyAmount}, QuestStatus{QuestId,QuestStatus}, VisitedNode
- `Conversation_Action` - ActionType: GiveItem{ObjectId}, RemoveItem{ObjectId}, AddMoney{Amount}, SetFlag{FlagName}, StartQuest{QuestId}, CompleteQuest{QuestId}
- `Conversation_Shop` - {ShopTitle,WelcomeMessage} (salidas: OnClose,OnBuy,OnSell)
- `Conversation_End` - Fin

{SYSTEMS_INSTRUCTIONS}

## REQUISITOS DEL MUNDO (temática ""{THEME}"")

1. **{ROOM_COUNT} salas** conectadas - ⚠️ TODAS accesibles desde StartRoomId, sin islas aisladas. Zone agrupa salas temáticamente. IsInterior=true para interiores, IsIlluminated=false para oscuras.
2. **{DOOR_COUNT} puertas** - Al menos una con llave. OpenFromSide: Both/FromAOnly/FromBOnly
3. **{CONTAINER_COUNT} contenedores** - IsOpenable=true, IsLocked=true+KeyId para cerrados, ContentsVisible=false
4. **{TOTAL_OBJECTS} objetos**: {WEAPON_COUNT} armas, {ARMOR_COUNT} armaduras, {HELMET_COUNT} cascos, {FOOD_COUNT} comida, {DRINK_COUNT} bebidas, {KEY_COUNT} llaves, {TEXT_COUNT} textos (CanRead=true+TextContent), {OTHER_COUNT} otros
5. **{NPC_COUNT} NPCs**: Comerciantes con IsShopkeeper+ShopInventory. PatrolRoute=[""sala1"",""sala2""], PatrolSpeed=1-3. Al menos 1 patrullando.
{NPC_SPECIAL_TYPES}6. **{QUEST_COUNT} misiones** - ⚠️ TODA misión necesita Action_StartQuest Y Action_CompleteQuest en script/conversación
7. **Scripts**: OBLIGATORIO Event_OnGameStart con Action_StartQuest. Incluye Event_OnExamine, Event_OnEnter, puzzles con flags/contadores, Action_FollowPlayer
8. **Conversaciones**: Van en array Conversations. Puertos: Exec, Option1-4, True/False, OnClose/OnBuy/OnSell

## REGLAS CRÍTICAS

**IDs**: Game.Id=GUID válido. Otros IDs en snake_case. StartRoomId debe existir.
**Direcciones**: norte, sur, este, oeste, arriba, abajo
**RoomPositions**: Sala inicial en (80,45). Norte: Y-180, Sur: Y+180, Este: X+320, Oeste: X-320
**Types objeto**: ninguno, arma, armadura, casco, escudo, comida, bebida, llave
**Enums**: Gender=Masculine/Feminine, DamageType=Physical/Magical/Piercing, Weather=Despejado/Lluvioso/Nublado/Tormenta

**Equipamiento**: Slots: RightHand, LeftHand, Torso, Head. Armas 2 manos ocupan ambas. Escudos solo LeftHand.
**Luz**: IsLightSource=true, IsLit, LightTurnsRemaining (-1=infinito). Salas con IsIlluminated=false requieren luz.
**Inventarios**: {ObjectId, Quantity}. Quantity=-1=infinito. ShopInventory=venta, Inventory=posesión.
**Comerciantes**: Si NO tienen diálogo, al hablar se abre tienda directamente. Con diálogo, usar Conversation_Shop.

⚠️ **Objetos en contenedores**: DEBEN tener RoomId=null
⚠️ **Dar objetos ocultos**: Action_SetObjectVisible(true) ANTES de Action_GiveItem. Objeto con RoomId=null, Visible=false, NO en Inventory del NPC.
⚠️ **Puertas con puzzle**: IsLocked=false + IsOpen=false + Action_OpenDoor. NUNCA IsLocked=true sin KeyObjectId.
⚠️ **Llaves accesibles**: NUNCA pongas llave detrás de la puerta que abre.

**Player**: Stats suman 100 (min 10, max 100 cada una). Si CombatEnabled: arma inicial. Si MagicEnabled: habilidades iniciales.
**EncryptionKey**: 8 caracteres alfanuméricos aleatorios.
**ParserDictionaryJson**: null o sinónimos temáticos: ""{\""nouns\"": {\""gema\"": [\""orbe\""]}}""

## ZONAS (mundos 50+ salas)
Archivos .json separados: zona_1 con Game+Player, zona_2+ solo entidades. Conexiones inter-zona: TargetRoomId=""@zona:sala"". Max 15 salas/archivo.

{OUTPUT_FORMAT_INSTRUCTIONS}

## VALIDACIÓN FINAL
- Puertas: AMBAS salidas con mismo DoorId. Llaves accesibles SIN pasar por puerta que abren.
- IDs: Todos referenciados deben existir. OwnerType+OwnerId coherentes.
- Conversations: StartNodeId apunta a Conversation_Start. Scripts empiezan en Event.
- Sin spoilers: Solo descripción temática breve.";

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

        // Generar instrucciones de formato de salida según número de salas
        var outputFormatInstructions = GenerateOutputFormatInstructions(roomCount);

        // Tipos especiales de NPC solo para mundos medianos/grandes (12+ salas)
        var npcSpecialTypes = roomCount >= 12
            ? @"   - **~5% comerciantes**: IsShopkeeper=true, ShopInventory con objetos a la venta, Money=-1 (infinito) o cantidad limitada. Crea conversación con nodo Conversation_Shop para abrir tienda
   - **~5% compañeros**: NPCs que siguen al jugador. Configura FollowMovementMode=""Turns"", FollowSpeed=1. Puede seguir desde el inicio (IsFollowingPlayer=true) o activarse luego con Action_FollowPlayer
   - **~5% cadáveres**: IsCorpse=true, opcionalmente con Inventory de objetos saqueables. Los cadáveres no hablan ni se mueven
"
            : "";

        var prompt = PromptTemplate
            .Replace("{THEME}", theme)
            .Replace("{ROOM_COUNT}", roomCount.ToString())
            .Replace("{DOOR_COUNT}", doorCount.ToString())
            .Replace("{NPC_COUNT}", npcCount.ToString())
            .Replace("{NPC_SPECIAL_TYPES}", npcSpecialTypes)
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
            .Replace("{SYSTEMS_INSTRUCTIONS}", systemsInstructions)
            // Instrucciones de formato de salida
            .Replace("{OUTPUT_FORMAT_INSTRUCTIONS}", outputFormatInstructions);

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
            sb.AppendLine("- NO asignes equipamiento al jugador (InitialRightHandId, InitialTorsoId, InitialHeadId = null)");
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

    private string GenerateOutputFormatInstructions(int roomCount)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("## FORMATO DE SALIDA");
        sb.AppendLine();

        if (roomCount <= 20)
        {
            // Mundo pequeño: un solo archivo
            sb.AppendLine("**OBLIGATORIO: Tu respuesta debe ser ÚNICAMENTE un archivo descargable llamado `nuevo_mundo.xaw`**");
            sb.AppendLine();
            sb.AppendLine("### Reglas estrictas:");
            sb.AppendLine("❌ NO escribas NADA de texto antes del archivo");
            sb.AppendLine("❌ NO escribas NADA de texto después del archivo");
            sb.AppendLine("❌ NO uses markdown code blocks");
            sb.AppendLine("❌ NO ofrezcas \"continuar en el siguiente mensaje\"");
            sb.AppendLine("❌ NO digas que el contenido está \"truncado\" o \"compactado\"");
            sb.AppendLine("❌ NO hagas preguntas ni ofrezcas opciones");
            sb.AppendLine("✅ Genera el archivo .xaw COMPLETO directamente para descargar");
            sb.AppendLine("✅ Incluye TODO el contenido solicitado en UN SOLO archivo");
            sb.AppendLine();
            sb.AppendLine("### Requisitos del archivo:");
            sb.AppendLine("- JSON válido y parseable");
            sb.AppendLine("- COMPLETO con todas las salas, objetos, NPCs, scripts y misiones solicitados");
            sb.AppendLine("- **NO uses caracteres especiales invisibles** (soft hyphens, zero-width spaces, etc.)");
            sb.AppendLine("- Usa solo comillas rectas (\"\") nunca tipográficas (curly quotes)");
            sb.AppendLine();
            sb.AppendLine("**NUNCA pidas confirmación ni ofrezcas dividir el trabajo. Genera el archivo completo directamente.**");
        }
        else
        {
            // Mundo grande: múltiples archivos de zona
            int zonesNeeded = (int)Math.Ceiling(roomCount / 15.0);
            int roomsPerZone = (int)Math.Ceiling((double)roomCount / zonesNeeded);

            sb.AppendLine($"**MUNDO GRANDE ({roomCount} salas): Debes generar {zonesNeeded} ARCHIVOS SEPARADOS**");
            sb.AppendLine();
            sb.AppendLine($"Este mundo tiene {roomCount} salas, lo cual excede el límite para un solo archivo.");
            sb.AppendLine($"Debes dividirlo en {zonesNeeded} zonas de aproximadamente {roomsPerZone} salas cada una.");
            sb.AppendLine();
            sb.AppendLine("### PROCESO OBLIGATORIO:");
            sb.AppendLine();
            sb.AppendLine("**⚠️ GENERA TODOS LOS ARCHIVOS DE UNA VEZ, NO ESPERES A QUE TE PIDA CADA UNO**");
            sb.AppendLine();
            sb.AppendLine("**Archivo 1** (zona inicial):");
            sb.AppendLine("- Nombre: `zona_1_[nombre_zona].json`");
            sb.AppendLine($"- Contenido: ~{roomsPerZone} salas completas con todos sus objetos, NPCs, scripts");
            sb.AppendLine("- Esta zona DEBE incluir Game.StartRoomId (la sala inicial)");
            sb.AppendLine("- Incluye las propiedades Game y Player SOLO en este primer archivo");
            sb.AppendLine();
            sb.AppendLine($"**Archivos 2-{zonesNeeded}** (zonas adicionales):");
            sb.AppendLine("- Nombres: `zona_2_[nombre].json`, `zona_3_[nombre].json`, etc.");
            sb.AppendLine($"- Cada archivo con ~{roomsPerZone} salas");
            sb.AppendLine("- NO incluyas Game ni Player en estos archivos (solo Rooms, Objects, Npcs, etc.)");
            sb.AppendLine();
            sb.AppendLine("### CONEXIONES ENTRE ZONAS:");
            sb.AppendLine("- Usa `TargetRoomId: \"@zona_siguiente:sala_entrada\"` para conexiones inter-zona");
            sb.AppendLine("- Documenta en cada archivo qué conexiones espera de otras zonas");
            sb.AppendLine();
            sb.AppendLine("### REGLAS POR ARCHIVO:");
            sb.AppendLine("❌ NO escribas texto explicativo, solo el archivo descargable");
            sb.AppendLine("❌ NO uses markdown code blocks");
            sb.AppendLine("❌ NO anuncies lo que vas a hacer (\"voy a generar...\"), simplemente HAZLO");
            sb.AppendLine("✅ Cada archivo debe ser JSON válido e independiente");
            sb.AppendLine();
            sb.AppendLine("### Requisitos de cada archivo:");
            sb.AppendLine("- JSON válido y parseable");
            sb.AppendLine("- **NO uses caracteres especiales invisibles**");
            sb.AppendLine("- Usa solo comillas rectas (\"\") nunca tipográficas (curly quotes)");
            sb.AppendLine();
            sb.AppendLine("**⚠️ GENERA TODOS LOS ARCHIVOS PARA DESCARGAR DE UNA SOLA VEZ. NO me hagas pedirte zona por zona. NO anuncies lo que vas a hacer, HAZLO DIRECTAMENTE.**");
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
        var theme = ThemeTextBox?.Text?.Trim() ?? "mundo";
        if (string.IsNullOrWhiteSpace(theme))
            theme = "mundo";

        // Sanitizar el nombre del archivo (quitar caracteres no válidos)
        var fileName = string.Join("_", theme.Split(System.IO.Path.GetInvalidFileNameChars()));

        // Mundo pequeño usa .xaw, mundo grande usa .json (para zonas)
        int.TryParse(RoomCountTextBox?.Text, out int roomCount);
        var extension = roomCount <= 20 ? "xaw" : "json";

        var additionalInstructions = $@"

IMPORTANTE: Cuando generes el JSON, ofrécemelo como un archivo descargable con el nombre '{fileName}.{extension}'.";

        Clipboard.SetText(PromptTextBox.Text + additionalInstructions);

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
        // Mostrar aviso sobre requisitos antes de continuar
        var warningMessage = @"⚠️ AVISO: REQUISITOS MUY ALTOS

La generación de mundos con IA local requiere hardware potente:

📋 REQUISITOS MÍNIMOS:
• RAM: 48 GB
• Espacio en disco: ~50 GB
• Docker Desktop instalado

📋 RECOMENDADO:
• RAM: 64 GB
• GPU NVIDIA con 48+ GB VRAM

⏱️ TIEMPO ESTIMADO:
• Primera vez: hasta 1 hora (descarga de 40GB)
• Generación: varios minutos

💡 ALTERNATIVA RECOMENDADA:
A día de hoy (2025), ejecutar modelos grandes localmente es muy exigente. En el futuro será más accesible, pero ahora mismo puede ser mejor:

1. Haz click en 'Copiar prompt'
2. Pégalo en ChatGPT, Claude u otro servicio de IA
3. Descarga el archivo .xaw que te dará como respuesta y ponlo en la carpeta worlds";

        var confirmDialog = new AlertWindow(warningMessage, "Generación local de mundos")
        {
            Owner = this
        };
        confirmDialog.ShowCancelButton(true);
        confirmDialog.SetOkButtonText("Continuar");

        if (confirmDialog.ShowDialog() != true)
        {
            return;
        }

        // Mostrar popup de servicios de IA y esperar a que Docker esté listo
        // Usamos llama3.1:70b - modelo grande con mejor comprensión (~40GB)
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
