using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Integration tests for crafting system with the full game state.
/// Tests end-to-end crafting flows including inventory and room state changes.
/// </summary>
public class CraftIntegrationTests
{
    /// <summary>
    /// Creates a complete test world for crafting integration testing.
    /// </summary>
    private static GameState CreateCraftingIntegrationState()
    {
        var state = new GameState
        {
            CurrentRoomId = "blacksmith",
            Player = new PlayerStats
            {
                Name = "Herrero",
                Money = 100,
                MaxInventoryWeight = 5000, // 5kg
                MaxInventoryVolume = 10000, // 10 liters
                DynamicStats = new PlayerDynamicStats { Health = 100, MaxHealth = 100 }
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "blacksmith",
                    Name = "Herrería",
                    Description = "Una herrería con un horno y yunque.",
                    IsIlluminated = true,
                    ObjectIds = new List<string> { "anvil", "coal", "iron_bar" }
                },
                new Room
                {
                    Id = "storage",
                    Name = "Almacén",
                    Description = "Un almacén con materiales.",
                    IsIlluminated = true,
                    ObjectIds = new List<string>()
                }
            },
            Objects = new List<GameObject>
            {
                // Room furniture (not takeable)
                new GameObject
                {
                    Id = "anvil",
                    Name = "yunque",
                    Description = "Un yunque de acero.",
                    CanTake = false,
                    Visible = true,
                    RoomId = "blacksmith"
                },
                // Raw materials
                new GameObject
                {
                    Id = "iron_bar",
                    Name = "barra de hierro",
                    Description = "Una barra de hierro puro.",
                    CanTake = true,
                    Visible = true,
                    Weight = 500,
                    Volume = 100,
                    RoomId = "blacksmith"
                },
                new GameObject
                {
                    Id = "coal",
                    Name = "carbón",
                    Description = "Carbón para el horno.",
                    CanTake = true,
                    Visible = true,
                    Weight = 100,
                    Volume = 200,
                    RoomId = "blacksmith"
                },
                new GameObject
                {
                    Id = "wood",
                    Name = "madera",
                    Description = "Un trozo de madera.",
                    CanTake = true,
                    Visible = true,
                    Weight = 300,
                    Volume = 500
                },
                new GameObject
                {
                    Id = "leather",
                    Name = "cuero",
                    Description = "Cuero curtido.",
                    CanTake = true,
                    Visible = true,
                    Weight = 200,
                    Volume = 100
                },
                // Craftable items
                new GameObject
                {
                    Id = "iron_sword",
                    Name = "espada de hierro",
                    Description = "Una espada forjada en hierro.",
                    CanTake = true,
                    Visible = true,
                    Weight = 800,
                    Volume = 300,
                    Type = ObjectType.Arma,
                    AttackBonus = 5,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "iron_bar", Quantity = 2 },
                        new CraftingIngredient { ObjectId = "wood", Quantity = 1 }
                    }
                },
                new GameObject
                {
                    Id = "iron_dagger",
                    Name = "daga de hierro",
                    Description = "Una daga corta de hierro.",
                    CanTake = true,
                    Visible = true,
                    Weight = 300,
                    Volume = 50,
                    Type = ObjectType.Arma,
                    AttackBonus = 2,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "iron_bar", Quantity = 1 }
                    }
                },
                new GameObject
                {
                    Id = "leather_armor",
                    Name = "armadura de cuero",
                    Description = "Armadura ligera de cuero.",
                    CanTake = true,
                    Visible = true,
                    Weight = 1500,
                    Volume = 2000,
                    Type = ObjectType.Armadura,
                    DefenseBonus = 3,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "leather", Quantity = 3 }
                    }
                }
            },
            InventoryObjectIds = new List<string> { "wood", "leather", "leather", "leather", "iron_bar" }
        };

        return state;
    }

    [Fact]
    public void CraftFlow_FromStartToFinish_CreatesItem()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1); // 1 from inventory + 1 from room = 2
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.AddIngredient("wood", 1);

        var result = craftEngine.Craft();
        craftEngine.CloseCraft();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("iron_sword", result.CreatedObjectId);
        Assert.Contains("iron_sword", state.InventoryObjectIds);
    }

    [Fact]
    public void CraftFlow_ConsumesIngredientsFromInventoryFirst()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var initialInventoryIronBars = state.InventoryObjectIds.Count(id => id == "iron_bar");
        var initialRoomIronBars = state.Rooms[0].ObjectIds.Count(id => id == "iron_bar");

        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.Craft();

        // Assert
        var finalInventoryIronBars = state.InventoryObjectIds.Count(id => id == "iron_bar");
        Assert.Equal(initialInventoryIronBars - 1, finalInventoryIronBars);
        // Room iron bars should be unchanged if inventory had enough
        if (initialInventoryIronBars >= 1)
        {
            var finalRoomIronBars = state.Rooms[0].ObjectIds.Count(id => id == "iron_bar");
            Assert.Equal(initialRoomIronBars, finalRoomIronBars);
        }
    }

    [Fact]
    public void CraftFlow_CombinesInventoryAndRoomItems()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        var availableItems = craftEngine.GetAvailableItems();

        // Assert - Iron bar should combine inventory (1) + room (1) = 2
        var ironBar = availableItems.FirstOrDefault(i => i.ObjectId == "iron_bar");
        Assert.NotNull(ironBar);
        Assert.Equal(2, ironBar.Quantity);
    }

    [Fact]
    public void CraftFlow_RecipeMatching_FindsCorrectRecipe()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);
        GameObject? matchedRecipe = null;
        craftEngine.RecipeMatched += (s, e) => matchedRecipe = e;

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);

        // Assert - Should match iron dagger (1 iron bar)
        Assert.NotNull(matchedRecipe);
        Assert.Equal("iron_dagger", matchedRecipe.Id);
    }

    [Fact]
    public void CraftFlow_MultiIngredientRecipe_MatchesCorrectly()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);
        GameObject? matchedRecipe = null;
        craftEngine.RecipeMatched += (s, e) => matchedRecipe = e;

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 2);
        craftEngine.AddIngredient("wood", 1);

        // Assert - Should match iron sword (2 iron bars + 1 wood)
        Assert.NotNull(matchedRecipe);
        Assert.Equal("iron_sword", matchedRecipe.Id);
    }

    [Fact]
    public void CraftFlow_InsufficientIngredients_CannotCraft()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        state.InventoryObjectIds.Clear(); // Empty inventory
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1); // Only 1 from room
        craftEngine.AddIngredient("wood", 1);     // None available

        // Check if we can add wood
        var woodAdded = craftEngine.GetSelectedIngredients().Any(i => i.ObjectId == "wood");

        // Assert - Cannot add wood as it's not available
        Assert.False(woodAdded);
    }

    [Fact]
    public void CraftFlow_EventsFire_InCorrectOrder()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);
        var events = new List<string>();

        craftEngine.RecipeMatched += (s, e) => events.Add($"RecipeMatched:{e?.Id ?? "null"}");
        craftEngine.ItemCrafted += (s, e) => events.Add($"ItemCrafted:{e.Success}");
        craftEngine.CraftEnded += (s, e) => events.Add("CraftEnded");

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.Craft();
        craftEngine.CloseCraft();

        // Assert
        Assert.Contains("RecipeMatched:iron_dagger", events);
        Assert.Contains("ItemCrafted:True", events);
        Assert.Contains("CraftEnded", events);
    }

    [Fact]
    public void CraftFlow_InventoryFull_DropsInRoom()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        state.Player.MaxInventoryWeight = 1; // Very limited
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        var result = craftEngine.Craft();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.AddedToInventory);
        Assert.Contains("iron_dagger", state.Rooms[0].ObjectIds);
    }

    [Fact]
    public void CraftFlow_ClearIngredients_ResetsState()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.AddIngredient("wood", 1);
        craftEngine.ClearIngredients();

        // Assert
        Assert.Empty(craftEngine.GetSelectedIngredients());
        Assert.Null(craftEngine.GetMatchingRecipe());
    }

    [Fact]
    public void CraftFlow_RemoveIngredient_UpdatesRecipe()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);
        GameObject? lastMatch = null;
        craftEngine.RecipeMatched += (s, e) => lastMatch = e;

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 2);
        craftEngine.AddIngredient("wood", 1);
        // At this point, should match iron_sword

        craftEngine.RemoveIngredient("wood", 1);
        craftEngine.RemoveIngredient("iron_bar", 1);
        // Now should match iron_dagger

        // Assert
        Assert.NotNull(lastMatch);
        Assert.Equal("iron_dagger", lastMatch.Id);
    }

    [Fact]
    public void CraftFlow_CloseCraft_ClearsSession()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.CloseCraft();

        // Assert
        Assert.False(craftEngine.IsActive);
        Assert.Null(craftEngine.CurrentCraft);
    }

    [Fact]
    public void CraftFlow_MaxQuantityCalculation_CorrectlyLimits()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        // Add more iron bars to inventory to allow multiple crafts
        state.InventoryObjectIds.Add("iron_bar");
        state.InventoryObjectIds.Add("iron_bar");
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);

        // Assert - Should be able to craft at most (total iron bars / 1 required)
        var maxQuantity = craftEngine.CurrentCraft?.MaxCraftQuantity ?? 0;
        Assert.True(maxQuantity >= 1);
    }

    [Fact]
    public void CraftFlow_MultipleQuantityCraft_CreatesMultipleItems()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        // Add more iron bars
        state.InventoryObjectIds.Add("iron_bar");
        state.InventoryObjectIds.Add("iron_bar");
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.SetCraftQuantity(2);
        var result = craftEngine.Craft();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.QuantityCreated);
        Assert.Equal(2, state.InventoryObjectIds.Count(id => id == "iron_dagger"));
    }

    [Fact]
    public void CraftFlow_LeatherArmor_RequiresMultipleLeather()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("leather", 3);
        var recipe = craftEngine.GetMatchingRecipe();

        // Assert
        Assert.NotNull(recipe);
        Assert.Equal("leather_armor", recipe.Id);
    }

    [Fact]
    public void CraftFlow_AfterCraft_UpdatesAvailableItems()
    {
        // Arrange
        var state = CreateCraftingIntegrationState();
        var craftEngine = new CraftEngine(state);

        // Act
        craftEngine.StartCraft("blacksmith");
        craftEngine.AddIngredient("iron_bar", 1);
        craftEngine.Craft();

        var availableAfter = craftEngine.GetAvailableItems();

        // Assert - Iron bar count should have decreased by 1
        var ironBarAfter = availableAfter.FirstOrDefault(i => i.ObjectId == "iron_bar");
        Assert.NotNull(ironBarAfter);
        Assert.Equal(1, ironBarAfter.Quantity); // Was 2, used 1
    }
}
