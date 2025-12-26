using System;
using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Motor de fabricacion para gestionar la creacion de objetos a partir de ingredientes.
/// </summary>
public class CraftEngine
{
    private readonly GameState _gameState;
    private string? _currentRoomId;

    /// <summary>
    /// Estado actual de la fabricacion (null si no hay fabricacion activa).
    /// </summary>
    public CraftState? CurrentCraft { get; private set; }

    /// <summary>
    /// Indica si hay una fabricacion activa.
    /// </summary>
    public bool IsActive => CurrentCraft?.IsActive == true;

    /// <summary>
    /// Evento disparado cuando la fabricacion termina.
    /// </summary>
    public event EventHandler? CraftEnded;

    /// <summary>
    /// Evento disparado cuando se fabrica un objeto.
    /// </summary>
    public event EventHandler<CraftResult>? ItemCrafted;

    /// <summary>
    /// Evento disparado cuando cambia la receta que coincide con los ingredientes.
    /// </summary>
    public event EventHandler<GameObject?>? RecipeMatched;

    public CraftEngine(GameState gameState)
    {
        _gameState = gameState;
    }

    /// <summary>
    /// Inicia una sesion de fabricacion.
    /// </summary>
    /// <param name="currentRoomId">ID de la sala donde esta el jugador.</param>
    public void StartCraft(string currentRoomId)
    {
        _currentRoomId = currentRoomId;

        CurrentCraft = new CraftState
        {
            IsActive = true,
            AvailableItems = BuildAvailableItems(currentRoomId),
            SelectedIngredients = new List<CraftItem>(),
            MatchingRecipe = null,
            CraftQuantity = 1,
            MaxCraftQuantity = 0
        };
    }

    private List<CraftItem> BuildAvailableItems(string roomId)
    {
        var items = new List<CraftItem>();
        var room = _gameState.Rooms.FirstOrDefault(r =>
            r.Id.Equals(roomId, StringComparison.OrdinalIgnoreCase));

        // Objetos del inventario (agrupados por ID)
        var inventoryGroups = _gameState.InventoryObjectIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase);

        foreach (var group in inventoryGroups)
        {
            var obj = _gameState.Objects.FirstOrDefault(o =>
                o.Id.Equals(group.Key, StringComparison.OrdinalIgnoreCase));
            if (obj == null) continue;

            items.Add(new CraftItem
            {
                ObjectId = obj.Id,
                Name = obj.Name,
                Quantity = group.Count(),
                Weight = obj.Weight,
                Volume = obj.Volume,
                Location = CraftItemLocation.Inventory
            });
        }

        // Objetos de la sala (cogibles, visibles, no en contenedores cerrados)
        if (room != null)
        {
            foreach (var objId in room.ObjectIds)
            {
                var obj = _gameState.Objects.FirstOrDefault(o =>
                    o.Id.Equals(objId, StringComparison.OrdinalIgnoreCase));
                if (obj == null || !obj.CanTake || !obj.Visible) continue;

                // Verificar que no esta ya en items (por si esta en inventario tambien)
                var existing = items.FirstOrDefault(i =>
                    i.ObjectId.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    // Si ya existe en inventario, aumentar cantidad
                    existing.Quantity++;
                }
                else
                {
                    items.Add(new CraftItem
                    {
                        ObjectId = obj.Id,
                        Name = obj.Name,
                        Quantity = 1,
                        Weight = obj.Weight,
                        Volume = obj.Volume,
                        Location = CraftItemLocation.Room
                    });
                }
            }

            // Objetos en contenedores abiertos de la sala
            var openContainers = _gameState.Objects
                .Where(o => o.IsContainer && room.ObjectIds.Contains(o.Id, StringComparer.OrdinalIgnoreCase))
                .Where(o => o.IsOpen || o.ContentsVisible || !o.IsOpenable);

            foreach (var container in openContainers)
            {
                foreach (var containedId in container.ContainedObjectIds)
                {
                    var obj = _gameState.Objects.FirstOrDefault(o =>
                        o.Id.Equals(containedId, StringComparison.OrdinalIgnoreCase));
                    if (obj == null || !obj.CanTake || !obj.Visible) continue;

                    var existing = items.FirstOrDefault(i =>
                        i.ObjectId.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));

                    if (existing != null)
                    {
                        existing.Quantity++;
                    }
                    else
                    {
                        items.Add(new CraftItem
                        {
                            ObjectId = obj.Id,
                            Name = obj.Name,
                            Quantity = 1,
                            Weight = obj.Weight,
                            Volume = obj.Volume,
                            Location = CraftItemLocation.Room
                        });
                    }
                }
            }
        }

        return items.OrderBy(i => i.Name).ToList();
    }

    /// <summary>
    /// Anade un ingrediente a la lista de seleccionados.
    /// </summary>
    public void AddIngredient(string objectId, int quantity = 1)
    {
        if (CurrentCraft == null || !IsActive) return;

        var available = CurrentCraft.AvailableItems.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
        if (available == null) return;

        // Calcular cuanto ya esta seleccionado
        var alreadySelected = CurrentCraft.SelectedIngredients
            .FirstOrDefault(i => i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
        var currentSelected = alreadySelected?.SelectedQuantity ?? 0;

        // No permitir seleccionar mas de lo disponible
        var maxCanAdd = available.Quantity - currentSelected;
        if (maxCanAdd <= 0) return;

        var toAdd = Math.Min(quantity, maxCanAdd);

        if (alreadySelected != null)
        {
            alreadySelected.SelectedQuantity += toAdd;
        }
        else
        {
            CurrentCraft.SelectedIngredients.Add(new CraftItem
            {
                ObjectId = available.ObjectId,
                Name = available.Name,
                Quantity = available.Quantity,
                SelectedQuantity = toAdd,
                Weight = available.Weight,
                Volume = available.Volume,
                Location = available.Location
            });
        }

        UpdateMatchingRecipe();
    }

    /// <summary>
    /// Quita un ingrediente de la lista de seleccionados.
    /// </summary>
    public void RemoveIngredient(string objectId, int quantity = 1)
    {
        if (CurrentCraft == null || !IsActive) return;

        var existing = CurrentCraft.SelectedIngredients.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
        if (existing == null) return;

        existing.SelectedQuantity -= quantity;
        if (existing.SelectedQuantity <= 0)
        {
            CurrentCraft.SelectedIngredients.Remove(existing);
        }

        UpdateMatchingRecipe();
    }

    /// <summary>
    /// Limpia todos los ingredientes seleccionados.
    /// </summary>
    public void ClearIngredients()
    {
        if (CurrentCraft == null) return;
        CurrentCraft.SelectedIngredients.Clear();
        UpdateMatchingRecipe();
    }

    private void UpdateMatchingRecipe()
    {
        if (CurrentCraft == null) return;

        CurrentCraft.MatchingRecipe = GetMatchingRecipe();
        CurrentCraft.MaxCraftQuantity = CalculateMaxCraftQuantity();
        CurrentCraft.CraftQuantity = Math.Min(CurrentCraft.CraftQuantity,
            Math.Max(1, CurrentCraft.MaxCraftQuantity));

        RecipeMatched?.Invoke(this, CurrentCraft.MatchingRecipe);
    }

    /// <summary>
    /// Obtiene el objeto que se puede fabricar con los ingredientes actuales.
    /// </summary>
    public GameObject? GetMatchingRecipe()
    {
        if (CurrentCraft == null || !CurrentCraft.SelectedIngredients.Any())
            return null;

        // Buscar objetos que tengan receta de fabricacion
        var craftableObjects = _gameState.Objects
            .Where(o => o.CraftingRecipe != null && o.CraftingRecipe.Any());

        foreach (var obj in craftableObjects)
        {
            if (RecipeMatches(obj.CraftingRecipe))
                return obj;
        }

        return null;
    }

    private bool RecipeMatches(List<CraftingIngredient> recipe)
    {
        if (CurrentCraft == null) return false;

        // La receta debe tener exactamente los mismos ingredientes
        if (recipe.Count != CurrentCraft.SelectedIngredients.Count)
            return false;

        foreach (var ingredient in recipe)
        {
            var selected = CurrentCraft.SelectedIngredients.FirstOrDefault(s =>
                s.ObjectId.Equals(ingredient.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (selected == null || selected.SelectedQuantity != ingredient.Quantity)
                return false;
        }

        return true;
    }

    private int CalculateMaxCraftQuantity()
    {
        if (CurrentCraft?.MatchingRecipe == null) return 0;

        int maxQuantity = int.MaxValue;

        foreach (var ingredient in CurrentCraft.MatchingRecipe.CraftingRecipe)
        {
            var available = CurrentCraft.AvailableItems.FirstOrDefault(a =>
                a.ObjectId.Equals(ingredient.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (available == null) return 0;

            var possibleCrafts = available.Quantity / ingredient.Quantity;
            maxQuantity = Math.Min(maxQuantity, possibleCrafts);
        }

        return maxQuantity == int.MaxValue ? 0 : maxQuantity;
    }

    /// <summary>
    /// Establece la cantidad a fabricar.
    /// </summary>
    public void SetCraftQuantity(int quantity)
    {
        if (CurrentCraft == null) return;
        CurrentCraft.CraftQuantity = Math.Max(1, Math.Min(quantity, CurrentCraft.MaxCraftQuantity));
    }

    /// <summary>
    /// Intenta fabricar el objeto con la receta actual.
    /// </summary>
    public CraftResult Craft()
    {
        if (CurrentCraft == null || !IsActive)
            return new CraftResult { Success = false, Message = "No hay fabricacion activa." };

        var recipe = CurrentCraft.MatchingRecipe;
        if (recipe == null)
            return new CraftResult { Success = false, Message = "No hay receta valida." };

        var quantity = CurrentCraft.CraftQuantity;
        if (quantity <= 0)
            return new CraftResult { Success = false, Message = "Cantidad invalida." };

        // Verificar y consumir ingredientes
        foreach (var ingredient in recipe.CraftingRecipe)
        {
            var totalNeeded = ingredient.Quantity * quantity;
            var removed = 0;

            // Primero intentar remover del inventario
            while (removed < totalNeeded &&
                   _gameState.InventoryObjectIds.Contains(ingredient.ObjectId, StringComparer.OrdinalIgnoreCase))
            {
                _gameState.InventoryObjectIds.Remove(ingredient.ObjectId);
                removed++;
            }

            // Luego remover de la sala
            var room = _gameState.Rooms.FirstOrDefault(r =>
                r.Id.Equals(_currentRoomId, StringComparison.OrdinalIgnoreCase));

            while (removed < totalNeeded && room != null)
            {
                if (room.ObjectIds.Contains(ingredient.ObjectId, StringComparer.OrdinalIgnoreCase))
                {
                    room.ObjectIds.Remove(ingredient.ObjectId);
                    removed++;
                }
                else
                {
                    // Buscar en contenedores abiertos
                    var container = _gameState.Objects
                        .FirstOrDefault(o => o.IsContainer &&
                                           (o.IsOpen || o.ContentsVisible || !o.IsOpenable) &&
                                           o.ContainedObjectIds.Contains(ingredient.ObjectId, StringComparer.OrdinalIgnoreCase));
                    if (container != null)
                    {
                        container.ContainedObjectIds.Remove(ingredient.ObjectId);
                        removed++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        // Crear objetos fabricados
        var addedToInventory = true;
        for (int i = 0; i < quantity; i++)
        {
            // Verificar si cabe en inventario
            if (CanAddToPlayerInventory(recipe))
            {
                _gameState.InventoryObjectIds.Add(recipe.Id);
            }
            else
            {
                // Dejar en la sala
                var room = _gameState.Rooms.FirstOrDefault(r =>
                    r.Id.Equals(_currentRoomId, StringComparison.OrdinalIgnoreCase));
                room?.ObjectIds.Add(recipe.Id);
                addedToInventory = false;
            }
        }

        var result = new CraftResult
        {
            Success = true,
            Message = quantity > 1
                ? $"Has fabricado {quantity}x {recipe.Name}."
                : $"Has fabricado {recipe.Name}.",
            CreatedObjectId = recipe.Id,
            QuantityCreated = quantity,
            AddedToInventory = addedToInventory
        };

        if (!addedToInventory)
        {
            result.Message += " (No cabia en tu inventario, lo has dejado en el suelo)";
        }

        ItemCrafted?.Invoke(this, result);

        // Actualizar items disponibles
        CurrentCraft.AvailableItems = BuildAvailableItems(_currentRoomId!);
        CurrentCraft.SelectedIngredients.Clear();
        UpdateMatchingRecipe();

        return result;
    }

    private bool CanAddToPlayerInventory(GameObject obj)
    {
        var player = _gameState.Player;

        // Si no hay limites, siempre cabe
        if (player.MaxInventoryWeight < 0 && player.MaxInventoryVolume < 0)
            return true;

        // Calcular peso/volumen actual
        var currentWeight = _gameState.InventoryObjectIds
            .Select(id => _gameState.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o != null)
            .Sum(o => o!.Weight);

        var currentVolume = _gameState.InventoryObjectIds
            .Select(id => _gameState.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o != null)
            .Sum(o => o!.Volume);

        // Verificar peso
        if (player.MaxInventoryWeight >= 0 && currentWeight + obj.Weight > player.MaxInventoryWeight)
            return false;

        // Verificar volumen
        if (player.MaxInventoryVolume >= 0 && currentVolume + obj.Volume > player.MaxInventoryVolume)
            return false;

        return true;
    }

    /// <summary>
    /// Cierra la sesion de fabricacion.
    /// </summary>
    public void CloseCraft()
    {
        if (CurrentCraft != null)
        {
            CurrentCraft.IsActive = false;
        }
        CurrentCraft = null;
        _currentRoomId = null;
        CraftEnded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Obtiene la lista de items disponibles para fabricar.
    /// </summary>
    public List<CraftItem> GetAvailableItems() => CurrentCraft?.AvailableItems ?? new List<CraftItem>();

    /// <summary>
    /// Obtiene la lista de ingredientes seleccionados.
    /// </summary>
    public List<CraftItem> GetSelectedIngredients() => CurrentCraft?.SelectedIngredients ?? new List<CraftItem>();
}
