using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Helpers;

/// <summary>
/// Constantes con nombres de propiedades para evitar magic strings.
/// Usar nameof() garantiza seguridad en tiempo de compilación.
/// </summary>
public static class PropertyNames
{
    // === Identificación (comunes) ===
    public static readonly string Id = nameof(Room.Id);
    public static readonly string Name = nameof(Room.Name);
    public static readonly string Title = nameof(GameInfo.Title);
    public static readonly string Theme = nameof(GameInfo.Theme);

    // === Descripción ===
    public static readonly string Description = nameof(Room.Description);
    public const string Dialogue = "Dialogue"; // No existe como propiedad, es nombre de categoría
    public static readonly string TextContent = nameof(GameObject.TextContent);

    // === Sistemas (GameInfo) ===
    public static readonly string CombatEnabled = nameof(GameInfo.CombatEnabled);
    public static readonly string MagicEnabled = nameof(GameInfo.MagicEnabled);
    public static readonly string BasicNeedsEnabled = nameof(GameInfo.BasicNeedsEnabled);
    public static readonly string HungerRate = nameof(GameInfo.HungerRate);
    public static readonly string ThirstRate = nameof(GameInfo.ThirstRate);
    public static readonly string SleepRate = nameof(GameInfo.SleepRate);
    public static readonly string HungerDeathText = nameof(GameInfo.HungerDeathText);
    public static readonly string ThirstDeathText = nameof(GameInfo.ThirstDeathText);
    public static readonly string SleepDeathText = nameof(GameInfo.SleepDeathText);
    public static readonly string HealthDeathText = nameof(GameInfo.HealthDeathText);
    public static readonly string SanityDeathText = nameof(GameInfo.SanityDeathText);
    public static readonly string CraftingEnabled = nameof(GameInfo.CraftingEnabled);

    // === Multimedia ===
    public static readonly string ImageId = nameof(Room.ImageId);
    public static readonly string ImageBase64 = nameof(Room.ImageBase64);
    public static readonly string AsciiImage = nameof(Room.AsciiImage);
    public static readonly string MusicId = nameof(Room.MusicId);
    public static readonly string WorldMusicId = nameof(GameInfo.WorldMusicId);
    public static readonly string EndingMusicId = nameof(GameInfo.EndingMusicId);

    // === Salas ===
    public static readonly string RoomId = nameof(GameObject.RoomId);
    public static readonly string RoomIdA = nameof(Door.RoomIdA);
    public static readonly string RoomIdB = nameof(Door.RoomIdB);
    public static readonly string StartRoomId = nameof(GameInfo.StartRoomId);
    public static readonly string TargetRoomId = nameof(Exit.TargetRoomId);
    public static readonly string Direction = nameof(Exit.Direction);

    // === Comportamiento (GameObject) ===
    public static readonly string Visible = nameof(GameObject.Visible);
    public static readonly string CanTake = nameof(GameObject.CanTake);
    public static readonly string Type = nameof(GameObject.Type);
    public static readonly string CanRead = nameof(GameObject.CanRead);
    public static readonly string Gender = nameof(GameObject.Gender);
    public static readonly string IsPlural = nameof(GameObject.IsPlural);
    public static readonly string IsContainer = nameof(GameObject.IsContainer);
    public static readonly string IsOpenable = nameof(GameObject.IsOpenable);
    public static readonly string IsOpen = nameof(GameObject.IsOpen);
    public static readonly string IsLocked = nameof(GameObject.IsLocked);
    public static readonly string ContentsVisible = nameof(GameObject.ContentsVisible);
    public static readonly string MaxCapacity = nameof(GameObject.MaxCapacity);
    public static readonly string ContainedObjectIds = nameof(GameObject.ContainedObjectIds);
    public static readonly string KeyId = nameof(GameObject.KeyId);
    public static readonly string Volume = nameof(GameObject.Volume);
    public static readonly string Weight = nameof(GameObject.Weight);
    public static readonly string Price = nameof(GameObject.Price);
    public static readonly string NutritionAmount = nameof(GameObject.NutritionAmount);

    // === Iluminación (GameObject) ===
    public static readonly string IsLightSource = nameof(GameObject.IsLightSource);
    public static readonly string IsLit = nameof(GameObject.IsLit);
    public static readonly string LightTurnsRemaining = nameof(GameObject.LightTurnsRemaining);
    public static readonly string CanExtinguish = nameof(GameObject.CanExtinguish);
    public static readonly string CanIgnite = nameof(GameObject.CanIgnite);
    public static readonly string IgniterObjectId = nameof(GameObject.IgniterObjectId);

    // === Combate (GameObject) ===
    public static readonly string AttackBonus = nameof(GameObject.AttackBonus);
    public static readonly string HandsRequired = nameof(GameObject.HandsRequired);
    public static readonly string DefenseBonus = nameof(GameObject.DefenseBonus);
    public static readonly string DamageType = nameof(GameObject.DamageType);
    public static readonly string MaxDurability = nameof(GameObject.MaxDurability);
    public static readonly string CurrentDurability = nameof(GameObject.CurrentDurability);
    public static readonly string InitiativeBonus = nameof(GameObject.InitiativeBonus);

    // === Fabricación (GameObject) ===
    public static readonly string CraftingRecipe = nameof(GameObject.CraftingRecipe);

    // === Room ===
    public static readonly string IsInterior = nameof(Room.IsInterior);
    public static readonly string IsIlluminated = nameof(Room.IsIlluminated);
    public static readonly string RequiredQuests = nameof(Room.RequiredQuests);

    // === GameInfo ===
    public static readonly string DefaultFontFamily = nameof(GameInfo.DefaultFontFamily);
    public static readonly string StartHour = nameof(GameInfo.StartHour);
    public static readonly string StartWeather = nameof(GameInfo.StartWeather);
    public static readonly string MinutesPerGameHour = nameof(GameInfo.MinutesPerGameHour);
    public static readonly string IntroText = nameof(GameInfo.IntroText);
    public static readonly string EndingText = nameof(GameInfo.EndingText);
    public static readonly string EncryptionKey = nameof(GameInfo.EncryptionKey);
    public static readonly string ParserDictionaryJson = nameof(GameInfo.ParserDictionaryJson);
    public static readonly string TestModeAiEnabled = nameof(GameInfo.TestModeAiEnabled);
    public static readonly string TestModeSoundEnabled = nameof(GameInfo.TestModeSoundEnabled);

    // === Door ===
    public static readonly string KeyObjectId = nameof(Door.KeyObjectId);
    public static readonly string OpenFromSide = nameof(Door.OpenFromSide);
    public static readonly string DoorId = nameof(Exit.DoorId);

    // === NPC ===
    public static readonly string IsShopkeeper = nameof(Npc.IsShopkeeper);
    public static readonly string ShopInventory = nameof(Npc.ShopInventory);
    public static readonly string BuyPriceMultiplier = nameof(Npc.BuyPriceMultiplier);
    public static readonly string SellPriceMultiplier = nameof(Npc.SellPriceMultiplier);
    public static readonly string IsPatrolling = nameof(Npc.IsPatrolling);
    public static readonly string PatrolMovementMode = nameof(Npc.PatrolMovementMode);
    public static readonly string PatrolSpeed = nameof(Npc.PatrolSpeed);
    public static readonly string PatrolTimeInterval = nameof(Npc.PatrolTimeInterval);
    public static readonly string IsFollowingPlayer = nameof(Npc.IsFollowingPlayer);
    public static readonly string FollowMovementMode = nameof(Npc.FollowMovementMode);
    public static readonly string FollowSpeed = nameof(Npc.FollowSpeed);
    public static readonly string FollowTimeInterval = nameof(Npc.FollowTimeInterval);
    public static readonly string Inventory = nameof(Npc.Inventory);
    public static readonly string EquippedRightHandId = nameof(Npc.EquippedRightHandId);
    public static readonly string EquippedLeftHandId = nameof(Npc.EquippedLeftHandId);
    public static readonly string EquippedTorsoId = nameof(Npc.EquippedTorsoId);
    public static readonly string EquippedHeadId = nameof(Npc.EquippedHeadId);
    public static readonly string Stats = nameof(Npc.Stats);
    public static readonly string IsCorpse = nameof(Npc.IsCorpse);

    // === PlayerDefinition ===
    public static readonly string Age = nameof(PlayerDefinition.Age);
    public static readonly string Height = nameof(PlayerDefinition.Height);
    public static readonly string InitialMoney = nameof(PlayerDefinition.InitialMoney);
    public static readonly string Strength = nameof(PlayerDefinition.Strength);
    public static readonly string Constitution = nameof(PlayerDefinition.Constitution);
    public static readonly string Intelligence = nameof(PlayerDefinition.Intelligence);
    public static readonly string Dexterity = nameof(PlayerDefinition.Dexterity);
    public static readonly string Charisma = nameof(PlayerDefinition.Charisma);
    public static readonly string MaxInventoryWeight = nameof(PlayerDefinition.MaxInventoryWeight);
    public static readonly string MaxInventoryVolume = nameof(PlayerDefinition.MaxInventoryVolume);
    public static readonly string AbilityIds = nameof(PlayerDefinition.AbilityIds);
    public static readonly string InitialInventory = nameof(PlayerDefinition.InitialInventory);
    public static readonly string InitialRightHandId = nameof(PlayerDefinition.InitialRightHandId);
    public static readonly string InitialLeftHandId = nameof(PlayerDefinition.InitialLeftHandId);
    public static readonly string InitialTorsoId = nameof(PlayerDefinition.InitialTorsoId);
    public static readonly string InitialHeadId = nameof(PlayerDefinition.InitialHeadId);

    // === Stats (CombatStats/PlayerStats) ===
    public static readonly string MaxHealth = nameof(CombatStats.MaxHealth);
    public static readonly string CurrentHealth = nameof(CombatStats.CurrentHealth);
    public static readonly string Money = nameof(PlayerStats.Money);

    // === Quest ===
    public static readonly string Objectives = nameof(QuestDefinition.Objectives);
    public static readonly string IsMainQuest = nameof(QuestDefinition.IsMainQuest);
    public const string ObjectId = "ObjectId"; // Usado en varios contextos
}
