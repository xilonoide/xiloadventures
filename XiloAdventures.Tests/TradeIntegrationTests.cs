using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Integration tests for the trade system.
/// Tests full trade flows combining TradeEngine with game state.
/// </summary>
public class TradeIntegrationTests
{
    /// <summary>
    /// Creates a complete test world for integration testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateIntegrationTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "integration_test",
                Title = "Integration Test World",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Plaza del Mercado",
                    Description = "Una bulliciosa plaza de mercado.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "merchant", "blacksmith" },
                    ObjectIds = new List<string> { "apple", "bread", "sword", "potion", "gem" }
                }
            },
            Objects = new List<GameObject>
            {
                new GameObject
                {
                    Id = "apple",
                    Name = "manzana",
                    Description = "Una manzana roja.",
                    Type = ObjectType.Comida,
                    Price = 5,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "bread",
                    Name = "pan",
                    Description = "Un pan fresco.",
                    Type = ObjectType.Comida,
                    Price = 8,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "sword",
                    Name = "espada",
                    Description = "Una espada afilada.",
                    Type = ObjectType.Arma,
                    AttackBonus = 5,
                    Price = 100,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "potion",
                    Name = "pocion",
                    Description = "Una pocion de salud.",
                    Type = ObjectType.Comida,
                    Price = 25,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "gem",
                    Name = "gema",
                    Description = "Una gema preciosa.",
                    Type = ObjectType.Ninguno,
                    Price = 200,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "rare_artifact",
                    Name = "artefacto",
                    Description = "Un artefacto antiguo.",
                    Type = ObjectType.Ninguno,
                    Price = 500,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "merchant",
                    Name = "comerciante",
                    Description = "Un comerciante general.",
                    RoomId = "room1",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 500,
                    BuyPriceMultiplier = 0.5,
                    SellPriceMultiplier = 1.0,
                    ShopInventory = new List<ShopItem> { new ShopItem { ObjectId = "apple" }, new ShopItem { ObjectId = "bread" }, new ShopItem { ObjectId = "potion" } }
                },
                new Npc
                {
                    Id = "blacksmith",
                    Name = "herrero",
                    Description = "Un herrero experto.",
                    RoomId = "room1",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 1000,
                    BuyPriceMultiplier = 0.6,
                    SellPriceMultiplier = 0.9,
                    ShopInventory = new List<ShopItem> { new ShopItem { ObjectId = "sword" } }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Money = 300;

        return (world, state);
    }

    #region Full Trade Flow Tests

    [Fact]
    public void FullTradeFlow_BuyMultipleItems()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        state.Player.Money = 100;

        // Act - Start trade and buy items
        engine.StartTrade(merchant);

        var result1 = engine.BuyItem("apple", 3);
        var result2 = engine.BuyItem("bread", 2);

        engine.CloseTrade();

        // Assert
        Assert.True(result1.Success);
        Assert.True(result2.Success);
        Assert.Equal(3, state.InventoryObjectIds.Count(id => id == "apple"));
        Assert.Equal(2, state.InventoryObjectIds.Count(id => id == "bread"));
        Assert.Equal(100 - (5 * 3) - (8 * 2), state.Player.Money); // 100 - 15 - 16 = 69
    }

    [Fact]
    public void FullTradeFlow_SellMultipleItems()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();

        // Give player some items to sell
        state.InventoryObjectIds.Add("gem");
        state.InventoryObjectIds.Add("gem");

        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var initialMoney = state.Player.Money;
        var initialMerchantMoney = merchant.Money;

        // Act
        engine.StartTrade(merchant);
        var result = engine.SellItem("gem", 2);
        engine.CloseTrade();

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("gem", state.InventoryObjectIds);
        // Gem price 200 * 0.5 = 100 each, sold 2 = 200
        Assert.Equal(initialMoney + 200, state.Player.Money);
        Assert.Equal(initialMerchantMoney - 200, merchant.Money);
    }

    [Fact]
    public void FullTradeFlow_MixedBuySell()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        state.InventoryObjectIds.Add("gem");
        state.Player.Money = 50;

        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");

        // Act - Sell gem first, then buy items with the money
        engine.StartTrade(merchant);

        var sellResult = engine.SellItem("gem"); // Get 100 gold (200 * 0.5)
        var buyResult1 = engine.BuyItem("potion", 3); // Spend 75 gold (25 * 3)
        var buyResult2 = engine.BuyItem("apple", 5); // Spend 25 gold (5 * 5)

        engine.CloseTrade();

        // Assert
        Assert.True(sellResult.Success);
        Assert.True(buyResult1.Success);
        Assert.True(buyResult2.Success);
        Assert.DoesNotContain("gem", state.InventoryObjectIds);
        Assert.Equal(3, state.InventoryObjectIds.Count(id => id == "potion"));
        Assert.Equal(5, state.InventoryObjectIds.Count(id => id == "apple"));
        // 50 (initial) + 100 (sell gem) - 75 (potions) - 25 (apples) = 50
        Assert.Equal(50, state.Player.Money);
    }

    [Fact]
    public void FullTradeFlow_NpcGoldDepletes()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();

        // Give player valuable items
        state.InventoryObjectIds.Add("rare_artifact"); // Worth 250 at 0.5 multiplier
        state.InventoryObjectIds.Add("rare_artifact");
        state.InventoryObjectIds.Add("rare_artifact");

        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 400; // Limited gold

        // Act
        engine.StartTrade(merchant);

        var result1 = engine.SellItem("rare_artifact"); // 250, success
        var result2 = engine.SellItem("rare_artifact"); // Would be 250, but NPC only has 150 left - should fail

        engine.CloseTrade();

        // Assert
        Assert.True(result1.Success);
        Assert.False(result2.Success);
        Assert.Equal(150, merchant.Money); // 400 - 250 = 150
        Assert.Equal(2, state.InventoryObjectIds.Count(id => id == "rare_artifact")); // Still has 2
    }

    #endregion

    #region Multiple Merchant Tests

    [Fact]
    public void Trade_DifferentMerchants_DifferentPrices()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        state.InventoryObjectIds.Add("sword");

        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var blacksmith = state.Npcs.First(n => n.Id == "blacksmith");

        var merchantEngine = new TradeEngine(state);
        var blacksmithEngine = new TradeEngine(state);

        // Act - Check sell prices at both merchants
        merchantEngine.StartTrade(merchant);
        var merchantPrice = merchantEngine.GetPlayerItems().First(i => i.ObjectId == "sword").CalculatedPrice;
        merchantEngine.CloseTrade();

        blacksmithEngine.StartTrade(blacksmith);
        var blacksmithPrice = blacksmithEngine.GetPlayerItems().First(i => i.ObjectId == "sword").CalculatedPrice;
        blacksmithEngine.CloseTrade();

        // Assert - Blacksmith pays more (0.6 vs 0.5 multiplier)
        Assert.Equal(50, merchantPrice);   // 100 * 0.5
        Assert.Equal(60, blacksmithPrice); // 100 * 0.6
    }

    [Fact]
    public void Trade_BlacksmithHasBetterWeaponDeals()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        state.Player.Money = 200;

        var blacksmith = state.Npcs.First(n => n.Id == "blacksmith");
        var blacksmithEngine = new TradeEngine(state);

        // Act
        blacksmithEngine.StartTrade(blacksmith);
        var swordPrice = blacksmithEngine.GetNpcItems().First(i => i.ObjectId == "sword").CalculatedPrice;
        blacksmithEngine.CloseTrade();

        // Assert - Blacksmith sells at 0.9 multiplier
        Assert.Equal(90, swordPrice); // 100 * 0.9
    }

    #endregion

    #region State Consistency Tests

    [Fact]
    public void Trade_GameStateConsistent_AfterMultipleTrades()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        state.Player.Money = 500;

        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var initialMerchantMoney = merchant.Money;

        // Act - Multiple trade sessions
        var engine1 = new TradeEngine(state);
        engine1.StartTrade(merchant);
        engine1.BuyItem("potion", 2);
        engine1.CloseTrade();

        var engine2 = new TradeEngine(state);
        engine2.StartTrade(merchant);
        engine2.BuyItem("apple", 3);
        engine2.CloseTrade();

        // Assert - Money calculations are consistent
        var expectedPlayerMoney = 500 - (25 * 2) - (5 * 3); // 500 - 50 - 15 = 435
        var expectedMerchantMoney = initialMerchantMoney + (25 * 2) + (5 * 3); // 500 + 50 + 15 = 565

        Assert.Equal(expectedPlayerMoney, state.Player.Money);
        Assert.Equal(expectedMerchantMoney, merchant.Money);
        Assert.Equal(2, state.InventoryObjectIds.Count(id => id == "potion"));
        Assert.Equal(3, state.InventoryObjectIds.Count(id => id == "apple"));
    }

    [Fact]
    public void Trade_ClosedWithoutTransaction_NoChanges()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        var initialPlayerMoney = state.Player.Money;

        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var initialMerchantMoney = merchant.Money;

        // Act - Open and close without buying/selling
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);
        engine.CloseTrade();

        // Assert - No changes
        Assert.Equal(initialPlayerMoney, state.Player.Money);
        Assert.Equal(initialMerchantMoney, merchant.Money);
        Assert.Empty(state.InventoryObjectIds);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Trade_ZeroQuantity_Fails()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("apple", 0);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void Trade_NegativeQuantity_Fails()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("apple", -1);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void Trade_InfiniteGoldMerchant_AlwaysBuys()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();

        // Give player expensive items
        state.InventoryObjectIds.Add("rare_artifact");
        state.InventoryObjectIds.Add("rare_artifact");
        state.InventoryObjectIds.Add("rare_artifact");
        state.InventoryObjectIds.Add("rare_artifact");
        state.InventoryObjectIds.Add("rare_artifact");

        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1; // Infinite

        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act - Sell all artifacts
        var result = engine.SellItem("rare_artifact", 5);

        engine.CloseTrade();

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("rare_artifact", state.InventoryObjectIds);
        Assert.Equal(-1, merchant.Money); // Still infinite
    }

    [Fact]
    public void Trade_PlayerGoldExactAmount_SucceedsAndZerosOut()
    {
        // Arrange
        var (world, state) = CreateIntegrationTestWorld();
        state.Player.Money = 25; // Exactly enough for one potion

        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("potion", 1);

        engine.CloseTrade();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, state.Player.Money);
        Assert.Contains("potion", state.InventoryObjectIds);
    }

    #endregion
}
