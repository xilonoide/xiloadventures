using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for the TradeEngine class.
/// Tests trade mechanics including buy/sell, price calculations, and inventory management.
/// </summary>
public class TradeEngineTests
{
    /// <summary>
    /// Creates a test world with a player and a merchant NPC for trade testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateTradeTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "trade_test",
                Title = "Trade Test World",
                StartRoomId = "room1",
                StartHour = 12
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "room1",
                    Name = "Tienda",
                    Description = "Una tienda de comercio.",
                    IsIlluminated = true,
                    NpcIds = new List<string> { "merchant" },
                    ObjectIds = new List<string> { "sword", "potion", "apple" }
                }
            },
            Objects = new List<GameObject>
            {
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
                    Price = 50,
                    CanTake = true,
                    Visible = true,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "apple",
                    Name = "manzana",
                    Description = "Una manzana roja.",
                    Type = ObjectType.Comida,
                    Price = 10,
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
                }
            },
            Npcs = new List<Npc>
            {
                new Npc
                {
                    Id = "merchant",
                    Name = "comerciante",
                    Description = "Un comerciante amigable.",
                    RoomId = "room1",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 500,
                    BuyPriceMultiplier = 0.5,
                    SellPriceMultiplier = 1.0,
                    ShopInventory = new List<ShopItem> { new ShopItem { ObjectId = "sword" }, new ShopItem { ObjectId = "potion" } }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Money = 200;

        return (world, state);
    }

    #region Trade Session Tests

    [Fact]
    public void StartTrade_CreatesActiveSession()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");

        // Act
        engine.StartTrade(merchant);

        // Assert
        Assert.True(engine.IsActive);
        Assert.NotNull(engine.CurrentTrade);
        Assert.True(engine.CurrentTrade.IsActive);
        Assert.Equal("merchant", engine.CurrentTrade.NpcId);
    }

    [Fact]
    public void StartTrade_PopulatesNpcItems()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");

        // Act
        engine.StartTrade(merchant);

        // Assert
        var npcItems = engine.GetNpcItems();
        Assert.NotEmpty(npcItems);
        Assert.Contains(npcItems, i => i.ObjectId == "sword");
        Assert.Contains(npcItems, i => i.ObjectId == "potion");
    }

    [Fact]
    public void StartTrade_PopulatesPlayerItems()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("gem");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");

        // Act
        engine.StartTrade(merchant);

        // Assert
        var playerItems = engine.GetPlayerItems();
        Assert.NotEmpty(playerItems);
        Assert.Contains(playerItems, i => i.ObjectId == "apple");
        Assert.Contains(playerItems, i => i.ObjectId == "gem");
    }

    [Fact]
    public void CloseTrade_EndsSession()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        engine.CloseTrade();

        // Assert
        Assert.False(engine.IsActive);
        Assert.Null(engine.CurrentTrade);
    }

    [Fact]
    public void CloseTrade_FiresTradeEndedEvent()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        var eventFired = false;
        engine.TradeEnded += (s, e) => eventFired = true;

        // Act
        engine.CloseTrade();

        // Assert
        Assert.True(eventFired);
    }

    #endregion

    #region Buy Item Tests

    [Fact]
    public void BuyItem_Success_TransfersGold()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        var initialPlayerGold = state.Player.Money;

        // Act
        var result = engine.BuyItem("potion");

        // Assert
        Assert.True(result.Success);
        Assert.True(state.Player.Money < initialPlayerGold);
    }

    [Fact]
    public void BuyItem_Success_TransfersItem()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("potion");

        // Assert
        Assert.True(result.Success);
        Assert.Contains("potion", state.InventoryObjectIds);
    }

    [Fact]
    public void BuyItem_Success_FiresItemBoughtEvent()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        TradeItem? boughtItem = null;
        engine.ItemBought += (s, item) => boughtItem = item;

        // Act
        engine.BuyItem("potion");

        // Assert
        Assert.NotNull(boughtItem);
        Assert.Equal("potion", boughtItem.ObjectId);
    }

    [Fact]
    public void BuyItem_InsufficientGold_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.Player.Money = 10; // Not enough for sword (100)
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("sword");

        // Assert
        Assert.False(result.Success);
        Assert.DoesNotContain("sword", state.InventoryObjectIds);
    }

    [Fact]
    public void BuyItem_ItemNotAvailable_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.BuyItem("nonexistent");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void BuyItem_MultipleQuantity_CorrectTotal()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.Player.Money = 500;
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        var potionPrice = engine.GetNpcItems().First(i => i.ObjectId == "potion").CalculatedPrice;

        // Act
        var result = engine.BuyItem("potion", 3);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.ItemsTransferred);
        Assert.Equal(potionPrice * 3, result.MoneyTransferred);
    }

    [Fact]
    public void BuyItem_NoActiveTrade_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        // No trade started

        // Act
        var result = engine.BuyItem("potion");

        // Assert
        Assert.False(result.Success);
    }

    #endregion

    #region Sell Item Tests

    [Fact]
    public void SellItem_Success_TransfersGold()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        var initialPlayerGold = state.Player.Money;

        // Act
        var result = engine.SellItem("gem");

        // Assert
        Assert.True(result.Success);
        Assert.True(state.Player.Money > initialPlayerGold);
    }

    [Fact]
    public void SellItem_Success_TransfersItem()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.SellItem("gem");

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("gem", state.InventoryObjectIds);
    }

    [Fact]
    public void SellItem_Success_FiresItemSoldEvent()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        TradeItem? soldItem = null;
        engine.ItemSold += (s, item) => soldItem = item;

        // Act
        engine.SellItem("gem");

        // Assert
        Assert.NotNull(soldItem);
        Assert.Equal("gem", soldItem.ObjectId);
    }

    [Fact]
    public void SellItem_NpcInsufficientGold_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 10; // Not enough for gem (100 at 0.5 multiplier)
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act
        var result = engine.SellItem("gem");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("gem", state.InventoryObjectIds);
    }

    [Fact]
    public void SellItem_NpcInfiniteGold_AlwaysSucceeds()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1; // Infinite gold
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act
        var result = engine.SellItem("gem");

        // Assert
        Assert.True(result.Success);
        Assert.DoesNotContain("gem", state.InventoryObjectIds);
    }

    [Fact]
    public void SellItem_PlayerDoesntHaveItem_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.SellItem("gem");

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void SellItem_QuantityExceedsInventory_Fails()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var result = engine.SellItem("apple", 5); // Only have 2

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void SellItem_MultipleQuantity_CorrectTotal()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);
        var applePrice = engine.GetPlayerItems().First(i => i.ObjectId == "apple").CalculatedPrice;

        // Act
        var result = engine.SellItem("apple", 2);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.ItemsTransferred);
        Assert.Equal(applePrice * 2, result.MoneyTransferred);
        Assert.Single(state.InventoryObjectIds, id => id == "apple");
    }

    #endregion

    #region Price Calculation Tests

    [Fact]
    public void BuyPrice_AppliesSellMultiplier()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.SellPriceMultiplier = 1.5;
        engine.StartTrade(merchant);

        // Act
        var swordItem = engine.GetNpcItems().First(i => i.ObjectId == "sword");

        // Assert
        // Sword base price is 100, with 1.5 multiplier should be 150
        Assert.Equal(150, swordItem.CalculatedPrice);
    }

    [Fact]
    public void SellPrice_AppliesBuyMultiplier()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.BuyPriceMultiplier = 0.5;
        engine.StartTrade(merchant);

        // Act
        var gemItem = engine.GetPlayerItems().First(i => i.ObjectId == "gem");

        // Assert
        // Gem base price is 200, with 0.5 multiplier should be 100
        Assert.Equal(100, gemItem.CalculatedPrice);
    }

    [Fact]
    public void MaxBuyQuantity_LimitedByGold()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.Player.Money = 125; // Enough for 2 potions at 50 each, plus 25 extra
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Act
        var maxQty = engine.GetMaxBuyQuantity("potion");

        // Assert
        Assert.Equal(2, maxQty); // Can afford 2 potions (100) but not 3 (150)
    }

    [Fact]
    public void MaxSellQuantity_LimitedByInventory()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1; // Infinite gold
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act
        var maxQty = engine.GetMaxSellQuantity("apple");

        // Assert
        Assert.Equal(3, maxQty);
    }

    [Fact]
    public void MaxSellQuantity_LimitedByNpcGold()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        state.InventoryObjectIds.Add("apple");
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 10; // Only enough for 2 apples at 5 each (0.5 * 10)
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act
        var maxQty = engine.GetMaxSellQuantity("apple");

        // Assert
        Assert.Equal(2, maxQty);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Trade_WithEmptyNpcInventory()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.ShopInventory.Clear();
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Assert
        Assert.Empty(engine.GetNpcItems());
    }

    [Fact]
    public void Trade_WithEmptyPlayerInventory()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Clear();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Assert
        Assert.Empty(engine.GetPlayerItems());
    }

    [Fact]
    public void Trade_NpcGoldInfinite_NeverDepletes()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.InventoryObjectIds.Add("gem");
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1; // Infinite
        var engine = new TradeEngine(state);
        engine.StartTrade(merchant);

        // Act
        engine.SellItem("gem");

        // Assert
        Assert.Equal(-1, merchant.Money); // Still infinite
    }

    [Fact]
    public void Trade_ItemWithPriceZero_NotShownInSellList()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var zeroItem = state.Objects.First(o => o.Id == "apple");
        zeroItem.Price = 0;
        state.InventoryObjectIds.Add("apple");
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Assert
        Assert.DoesNotContain(engine.GetPlayerItems(), i => i.ObjectId == "apple");
    }

    [Fact]
    public void GetPlayerGold_ReturnsCorrectAmount()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        state.Player.Money = 999;
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        engine.StartTrade(merchant);

        // Assert
        Assert.Equal(999, engine.GetPlayerMoney());
    }

    [Fact]
    public void GetNpcGold_ReturnsCorrectAmount()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 750;
        engine.StartTrade(merchant);

        // Assert
        Assert.Equal(750, engine.GetNpcMoney());
    }

    [Fact]
    public void NpcHasInfiniteGold_ReturnsTrue_WhenGoldNegative()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1;
        engine.StartTrade(merchant);

        // Assert
        Assert.True(engine.NpcHasInfiniteMoney());
    }

    [Fact]
    public void NpcHasInfiniteGold_ReturnsFalse_WhenGoldPositive()
    {
        // Arrange
        var (world, state) = CreateTradeTestWorld();
        var engine = new TradeEngine(state);
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 500;
        engine.StartTrade(merchant);

        // Assert
        Assert.False(engine.NpcHasInfiniteMoney());
    }

    #endregion
}
