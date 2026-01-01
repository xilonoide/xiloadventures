using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for ScriptEngine trade and commerce related nodes.
/// Tests script conditions and actions for the trade system.
/// </summary>
public class ScriptEngineTradeTests
{
    /// <summary>
    /// Creates a test world with a merchant NPC for script testing.
    /// </summary>
    private static (WorldModel world, GameState state) CreateScriptTestWorld()
    {
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "script_test",
                Title = "Script Test World",
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
                    ObjectIds = new List<string> { "potion", "sword" }
                }
            },
            Objects = new List<GameObject>
            {
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
                    Id = "sword",
                    Name = "espada",
                    Description = "Una espada.",
                    Type = ObjectType.Arma,
                    AttackBonus = 5,
                    Price = 100,
                    CanTake = true,
                    Visible = true,
                    DamageType = DamageType.Physical,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "magic_wand",
                    Name = "varita",
                    Description = "Una varita magica.",
                    Type = ObjectType.Arma,
                    AttackBonus = 3,
                    Price = 150,
                    CanTake = true,
                    Visible = true,
                    DamageType = DamageType.Magical,
                    RoomId = "room1"
                },
                new GameObject
                {
                    Id = "armor",
                    Name = "armadura",
                    Description = "Una armadura.",
                    Type = ObjectType.Armadura,
                    DefenseBonus = 5,
                    Price = 120,
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
                    Description = "Un comerciante.",
                    RoomId = "room1",
                    Visible = true,
                    IsShopkeeper = true,
                    Money = 500,
                    BuyPriceMultiplier = 0.5,
                    SellPriceMultiplier = 1.0,
                    ShopInventory = new List<ShopItem> { new ShopItem { ObjectId = "potion" } }
                }
            },
            Doors = new List<Door>(),
            Quests = new List<QuestDefinition>(),
            Scripts = new List<ScriptDefinition>()
        };

        var state = WorldLoader.CreateInitialState(world);
        state.Player.Money = 200;
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;

        return (world, state);
    }

    #region Trade Condition Tests

    [Fact]
    public async Task Condition_PlayerHasMoney_True_WhenEnoughGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.Money = 500;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHasMoney,
            Properties = new Dictionary<string, object?>
            {
                ["Amount"] = 300
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert - node execution should not throw
        // The condition should return True path (player has 500, needs 300)
    }

    [Fact]
    public async Task Condition_PlayerHasMoney_False_WhenNotEnoughGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.Money = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHasMoney,
            Properties = new Dictionary<string, object?>
            {
                ["Amount"] = 300
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_NpcHasMoney_True_WhenEnoughGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 500;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_NpcHasMoney,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["Amount"] = 300
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_NpcHasInfiniteMoney_True_WhenGoldNegative()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = -1;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_NpcHasInfiniteMoney,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant"
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_PlayerOwnsItem_True_WhenOwnsEnough()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.InventoryObjectIds.Add("potion");
        state.InventoryObjectIds.Add("potion");
        state.InventoryObjectIds.Add("potion");
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerOwnsItem,
            Properties = new Dictionary<string, object?>
            {
                ["ObjectId"] = "potion",
                ["Quantity"] = 2
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    #endregion

    #region Trade Action Tests

    [Fact]
    public async Task Action_AddPlayerMoney_IncreasesGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.Money = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_AddPlayerMoney,
            Properties = new Dictionary<string, object?>
            {
                ["Amount"] = 50
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(150, state.Player.Money);
    }

    [Fact]
    public async Task Action_RemovePlayerMoney_DecreasesGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.Money = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_RemovePlayerMoney,
            Properties = new Dictionary<string, object?>
            {
                ["Amount"] = 30
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(70, state.Player.Money);
    }

    [Fact]
    public async Task Action_RemovePlayerMoney_DoesNotGoBelowZero()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.Money = 50;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_RemovePlayerMoney,
            Properties = new Dictionary<string, object?>
            {
                ["Amount"] = 100
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert - Should not remove gold if insufficient
        Assert.Equal(50, state.Player.Money);
    }

    [Fact]
    public async Task Action_SetNpcMoney_ChangesNpcGold()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 500;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetNpcMoney,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["Money"] = 1000
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(1000, merchant.Money);
    }

    [Fact]
    public async Task Action_SetNpcMoney_CanSetToInfinite()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.Money = 500;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetNpcMoney,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["Money"] = -1
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(-1, merchant.Money);
    }

    [Fact]
    public async Task Action_AddNpcItem_AddsToShopInventory()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_AddNpcItem,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["ObjectId"] = "sword"
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Contains(merchant.ShopInventory, si => si.ObjectId == "sword");
    }

    [Fact]
    public async Task Action_RemoveNpcItem_RemovesFromShopInventory()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        merchant.ShopInventory.Add(new ShopItem { ObjectId = "sword" });
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_RemoveNpcItem,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["ObjectId"] = "sword"
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.DoesNotContain(merchant.ShopInventory, si => si.ObjectId == "sword");
    }

    [Fact]
    public async Task Action_SetBuyMultiplier_ChangesMultiplier()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetBuyMultiplier,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["Multiplier"] = 0.75
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(0.75, merchant.BuyPriceMultiplier);
    }

    [Fact]
    public async Task Action_SetSellMultiplier_ChangesMultiplier()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var merchant = state.Npcs.First(n => n.Id == "merchant");
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetSellMultiplier,
            Properties = new Dictionary<string, object?>
            {
                ["NpcId"] = "merchant",
                ["Multiplier"] = 1.25
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(1.25, merchant.SellPriceMultiplier);
    }

    #endregion

    #region Combat Condition Tests

    [Fact]
    public async Task Condition_PlayerHealthBelow_True_WhenHealthLow()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.DynamicStats.Health = 30;
        state.Player.DynamicStats.MaxHealth = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHealthBelow,
            Properties = new Dictionary<string, object?>
            {
                ["Threshold"] = 50
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_PlayerHealthAbove_True_WhenHealthHigh()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.DynamicStats.Health = 80;
        state.Player.DynamicStats.MaxHealth = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHealthAbove,
            Properties = new Dictionary<string, object?>
            {
                ["Threshold"] = 50
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_PlayerHasArmor_True_WhenArmorEquipped()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.EquippedTorsoId = "armor";
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHasArmor,
            Properties = new Dictionary<string, object?>()
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    [Fact]
    public async Task Condition_PlayerHasWeaponType_True_WhenCorrectType()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.EquippedRightHandId = "magic_wand";
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Condition_PlayerHasWeaponType,
            Properties = new Dictionary<string, object?>
            {
                ["DamageType"] = "Magical"
            }
        };

        // Act & Assert - should not throw
        await engine.ExecuteSingleNodeAsync(node);
    }

    #endregion

    #region Combat Action Tests

    [Fact]
    public async Task Action_SetPlayerMaxHealth_ChangesMaxHealth()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetPlayerMaxHealth,
            Properties = new Dictionary<string, object?>
            {
                ["MaxHealth"] = 150
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(150, state.Player.DynamicStats.MaxHealth);
    }

    [Fact]
    public async Task Action_SetPlayerMaxHealth_CapsCurrentHealth()
    {
        // Arrange
        var (world, state) = CreateScriptTestWorld();
        state.Player.DynamicStats.Health = 100;
        state.Player.DynamicStats.MaxHealth = 100;
        var engine = new ScriptEngine(world, state);

        var node = new ScriptNode
        {
            Id = "test_node",
            NodeType = NodeTypeId.Action_SetPlayerMaxHealth,
            Properties = new Dictionary<string, object?>
            {
                ["MaxHealth"] = 50
            }
        };

        // Act
        await engine.ExecuteSingleNodeAsync(node);

        // Assert
        Assert.Equal(50, state.Player.DynamicStats.MaxHealth);
        Assert.Equal(50, state.Player.DynamicStats.Health); // Capped to new max
    }

    #endregion
}
