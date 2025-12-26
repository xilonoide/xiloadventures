using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

/// <summary>
/// Unit tests for the CraftEngine class.
/// Tests crafting mechanics including recipe matching, ingredient management, and item creation.
/// </summary>
public class CraftEngineTests
{
    /// <summary>
    /// Creates a test world with crafting materials and recipes for testing.
    /// </summary>
    private static GameState CreateCraftingTestState()
    {
        var state = new GameState
        {
            CurrentRoomId = "workshop",
            Player = new PlayerStats
            {
                Name = "Artesano",
                Money = 100,
                MaxInventoryWeight = -1, // Unlimited
                MaxInventoryVolume = -1,  // Unlimited
                DynamicStats = new PlayerDynamicStats { Health = 100, MaxHealth = 100 }
            },
            Rooms = new List<Room>
            {
                new Room
                {
                    Id = "workshop",
                    Name = "Taller",
                    Description = "Un taller con herramientas de trabajo.",
                    IsIlluminated = true,
                    ObjectIds = new List<string> { "wood", "iron_ore" }
                }
            },
            Objects = new List<GameObject>
            {
                // Raw materials
                new GameObject
                {
                    Id = "wood",
                    Name = "madera",
                    Description = "Un trozo de madera.",
                    CanTake = true,
                    Visible = true,
                    Weight = 100,
                    Volume = 50,
                    RoomId = "workshop"
                },
                new GameObject
                {
                    Id = "iron_ore",
                    Name = "mineral de hierro",
                    Description = "Un trozo de mineral de hierro.",
                    CanTake = true,
                    Visible = true,
                    Weight = 200,
                    Volume = 30,
                    RoomId = "workshop"
                },
                new GameObject
                {
                    Id = "leather",
                    Name = "cuero",
                    Description = "Un trozo de cuero.",
                    CanTake = true,
                    Visible = true,
                    Weight = 50,
                    Volume = 20
                },
                new GameObject
                {
                    Id = "string",
                    Name = "cuerda",
                    Description = "Una cuerda resistente.",
                    CanTake = true,
                    Visible = true,
                    Weight = 10,
                    Volume = 5
                },
                // Craftable items
                new GameObject
                {
                    Id = "wooden_sword",
                    Name = "espada de madera",
                    Description = "Una espada de madera para entrenamiento.",
                    CanTake = true,
                    Visible = true,
                    Weight = 150,
                    Volume = 60,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "wood", Quantity = 2 }
                    }
                },
                new GameObject
                {
                    Id = "iron_sword",
                    Name = "espada de hierro",
                    Description = "Una espada de hierro afilada.",
                    CanTake = true,
                    Visible = true,
                    Weight = 300,
                    Volume = 70,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "iron_ore", Quantity = 2 },
                        new CraftingIngredient { ObjectId = "wood", Quantity = 1 }
                    }
                },
                new GameObject
                {
                    Id = "bow",
                    Name = "arco",
                    Description = "Un arco de madera.",
                    CanTake = true,
                    Visible = true,
                    Weight = 100,
                    Volume = 40,
                    CraftingRecipe = new List<CraftingIngredient>
                    {
                        new CraftingIngredient { ObjectId = "wood", Quantity = 1 },
                        new CraftingIngredient { ObjectId = "string", Quantity = 1 }
                    }
                }
            },
            InventoryObjectIds = new List<string> { "leather", "string", "wood", "wood" }
        };

        return state;
    }

    [Fact]
    public void StartCraft_CreatesActiveSession()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        Assert.True(engine.IsActive);
        Assert.NotNull(engine.CurrentCraft);
        Assert.True(engine.CurrentCraft.IsActive);
    }

    [Fact]
    public void StartCraft_BuildsAvailableItemsList()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        var items = engine.GetAvailableItems();
        Assert.NotEmpty(items);
        // Should include items from inventory and room
        Assert.Contains(items, i => i.ObjectId == "wood");
        Assert.Contains(items, i => i.ObjectId == "leather");
        Assert.Contains(items, i => i.ObjectId == "iron_ore");
    }

    [Fact]
    public void StartCraft_IncludesCorrectQuantities()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        var items = engine.GetAvailableItems();
        var woodItem = items.First(i => i.ObjectId == "wood");
        // 2 from inventory + 1 from room = 3
        Assert.Equal(3, woodItem.Quantity);
    }

    [Fact]
    public void AddIngredient_AddsToSelectedList()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Act
        engine.AddIngredient("wood", 2);

        // Assert
        var selected = engine.GetSelectedIngredients();
        Assert.Single(selected);
        Assert.Equal("wood", selected[0].ObjectId);
        Assert.Equal(2, selected[0].SelectedQuantity);
    }

    [Fact]
    public void AddIngredient_DoesNotExceedAvailable()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Act
        engine.AddIngredient("wood", 100); // Try to add more than available

        // Assert
        var selected = engine.GetSelectedIngredients();
        var woodItem = selected.First(i => i.ObjectId == "wood");
        Assert.Equal(3, woodItem.SelectedQuantity); // Should be capped at available (3)
    }

    [Fact]
    public void RemoveIngredient_RemovesFromSelectedList()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.RemoveIngredient("wood", 1);

        // Assert
        var selected = engine.GetSelectedIngredients();
        Assert.Single(selected);
        Assert.Equal(1, selected[0].SelectedQuantity);
    }

    [Fact]
    public void RemoveIngredient_RemovesCompletely_WhenQuantityZero()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.RemoveIngredient("wood", 2);

        // Assert
        var selected = engine.GetSelectedIngredients();
        Assert.Empty(selected);
    }

    [Fact]
    public void ClearIngredients_ClearsAllSelected()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);
        engine.AddIngredient("leather", 1);

        // Act
        engine.ClearIngredients();

        // Assert
        var selected = engine.GetSelectedIngredients();
        Assert.Empty(selected);
    }

    [Fact]
    public void GetMatchingRecipe_ReturnsMatch_WhenIngredientsMatch()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Add ingredients for wooden sword (2 wood)
        engine.AddIngredient("wood", 2);

        // Act
        var recipe = engine.GetMatchingRecipe();

        // Assert
        Assert.NotNull(recipe);
        Assert.Equal("wooden_sword", recipe.Id);
    }

    [Fact]
    public void GetMatchingRecipe_ReturnsNull_WhenNoMatch()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Add random combination
        engine.AddIngredient("wood", 1);
        engine.AddIngredient("leather", 1);

        // Act
        var recipe = engine.GetMatchingRecipe();

        // Assert
        Assert.Null(recipe);
    }

    [Fact]
    public void GetMatchingRecipe_MatchesMultiIngredientRecipes()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Add ingredients for bow (1 wood + 1 string)
        engine.AddIngredient("wood", 1);
        engine.AddIngredient("string", 1);

        // Act
        var recipe = engine.GetMatchingRecipe();

        // Assert
        Assert.NotNull(recipe);
        Assert.Equal("bow", recipe.Id);
    }

    [Fact]
    public void RecipeMatched_EventFires_OnMatchChange()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        GameObject? matchedRecipe = null;
        engine.RecipeMatched += (s, e) => matchedRecipe = e;

        // Act
        engine.AddIngredient("wood", 2);

        // Assert
        Assert.NotNull(matchedRecipe);
        Assert.Equal("wooden_sword", matchedRecipe.Id);
    }

    [Fact]
    public void Craft_WithValidRecipe_ProducesOutput()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        var result = engine.Craft();

        // Assert
        Assert.True(result.Success);
        Assert.Equal("wooden_sword", result.CreatedObjectId);
        Assert.Equal(1, result.QuantityCreated);
        Assert.Contains("espada de madera", result.Message);
    }

    [Fact]
    public void Craft_WithoutRecipe_Fails()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("leather", 1); // No recipe matches just leather

        // Act
        var result = engine.Craft();

        // Assert
        Assert.False(result.Success);
        Assert.Contains("receta", result.Message.ToLower());
    }

    [Fact]
    public void Craft_ConsumesIngredients_FromInventory()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var initialWoodCount = state.InventoryObjectIds.Count(id => id == "wood");
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.Craft();

        // Assert
        var finalWoodCount = state.InventoryObjectIds.Count(id => id == "wood");
        Assert.Equal(initialWoodCount - 2, finalWoodCount);
    }

    [Fact]
    public void Craft_AddsResultToInventory()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.Craft();

        // Assert
        Assert.Contains("wooden_sword", state.InventoryObjectIds);
    }

    [Fact]
    public void ItemCrafted_EventFires_OnSuccess()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);
        CraftResult? eventResult = null;
        engine.ItemCrafted += (s, e) => eventResult = e;

        // Act
        engine.Craft();

        // Assert
        Assert.NotNull(eventResult);
        Assert.True(eventResult.Success);
        Assert.Equal("wooden_sword", eventResult.CreatedObjectId);
    }

    [Fact]
    public void CloseCraft_EndsSession()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");

        // Act
        engine.CloseCraft();

        // Assert
        Assert.False(engine.IsActive);
        Assert.Null(engine.CurrentCraft);
    }

    [Fact]
    public void CraftEnded_EventFires_OnClose()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        bool eventFired = false;
        engine.CraftEnded += (s, e) => eventFired = true;

        // Act
        engine.CloseCraft();

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void IsActive_ReturnsTrue_DuringCraft()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);

        // Assert before
        Assert.False(engine.IsActive);

        // Act
        engine.StartCraft("workshop");

        // Assert after
        Assert.True(engine.IsActive);
    }

    [Fact]
    public void SetCraftQuantity_SetsValidQuantity()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.SetCraftQuantity(1);

        // Assert
        Assert.Equal(1, engine.CurrentCraft?.CraftQuantity);
    }

    [Fact]
    public void SetCraftQuantity_ClampsToMaxCraftQuantity()
    {
        // Arrange
        var state = CreateCraftingTestState();
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        engine.SetCraftQuantity(100); // Try to set more than possible

        // Assert
        Assert.Equal(engine.CurrentCraft?.MaxCraftQuantity ?? 0, engine.CurrentCraft?.CraftQuantity);
    }

    [Fact]
    public void AvailableItems_IncludesItemsFromOpenContainers()
    {
        // Arrange
        var state = CreateCraftingTestState();
        // Add a container with items
        var chest = new GameObject
        {
            Id = "chest",
            Name = "cofre",
            IsContainer = true,
            IsOpen = true,
            ContainedObjectIds = new List<string> { "extra_wood" }
        };
        state.Objects.Add(chest);
        state.Objects.Add(new GameObject
        {
            Id = "extra_wood",
            Name = "madera extra",
            CanTake = true,
            Visible = true
        });
        state.Rooms[0].ObjectIds.Add("chest");

        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        var items = engine.GetAvailableItems();
        Assert.Contains(items, i => i.ObjectId == "extra_wood");
    }

    [Fact]
    public void Craft_WithMultipleQuantity_CreatesMultipleItems()
    {
        // Arrange
        var state = CreateCraftingTestState();
        // Add more wood to make multiple swords
        state.InventoryObjectIds.Add("wood");
        state.InventoryObjectIds.Add("wood");
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);
        engine.SetCraftQuantity(2);

        // Act
        var result = engine.Craft();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, result.QuantityCreated);
        Assert.Equal(2, state.InventoryObjectIds.Count(id => id == "wooden_sword"));
    }

    [Fact]
    public void GetAvailableItems_ExcludesNonTakeableObjects()
    {
        // Arrange
        var state = CreateCraftingTestState();
        state.Objects.Add(new GameObject
        {
            Id = "anvil",
            Name = "yunque",
            CanTake = false,
            Visible = true,
            RoomId = "workshop"
        });
        state.Rooms[0].ObjectIds.Add("anvil");

        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        var items = engine.GetAvailableItems();
        Assert.DoesNotContain(items, i => i.ObjectId == "anvil");
    }

    [Fact]
    public void GetAvailableItems_ExcludesInvisibleObjects()
    {
        // Arrange
        var state = CreateCraftingTestState();
        state.Objects.Add(new GameObject
        {
            Id = "hidden_gem",
            Name = "gema oculta",
            CanTake = true,
            Visible = false,
            RoomId = "workshop"
        });
        state.Rooms[0].ObjectIds.Add("hidden_gem");

        var engine = new CraftEngine(state);

        // Act
        engine.StartCraft("workshop");

        // Assert
        var items = engine.GetAvailableItems();
        Assert.DoesNotContain(items, i => i.ObjectId == "hidden_gem");
    }

    [Fact]
    public void Craft_WithLimitedInventory_DropsItemInRoom()
    {
        // Arrange
        var state = CreateCraftingTestState();
        state.Player.MaxInventoryWeight = 1; // Very limited inventory
        var engine = new CraftEngine(state);
        engine.StartCraft("workshop");
        engine.AddIngredient("wood", 2);

        // Act
        var result = engine.Craft();

        // Assert
        Assert.True(result.Success);
        Assert.False(result.AddedToInventory);
        Assert.Contains("suelo", result.Message.ToLower());
        Assert.Contains("wooden_sword", state.Rooms[0].ObjectIds);
    }
}
