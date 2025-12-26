using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Common.Windows;

/// <summary>
/// Ventana de fabricacion de objetos.
/// </summary>
public partial class CraftWindow : Window
{
    private readonly CraftEngine _craftEngine;
    private readonly GameState _gameState;
    private readonly string _currentRoomId;

    private int _addQuantity = 1;

    /// <summary>
    /// Evento disparado cuando se cierra la fabricacion.
    /// </summary>
    public event Action? CraftClosed;

    public CraftWindow(CraftEngine craftEngine, GameState gameState, string currentRoomId)
    {
        InitializeComponent();

        _craftEngine = craftEngine;
        _gameState = gameState;
        _currentRoomId = currentRoomId;

        // Suscribirse a eventos del motor
        _craftEngine.CraftEnded += OnCraftEnded;
        _craftEngine.RecipeMatched += OnRecipeMatched;
        _craftEngine.ItemCrafted += OnItemCrafted;

        // Iniciar fabricacion
        _craftEngine.StartCraft(currentRoomId);

        // Inicializar UI
        InitializeUI();
    }

    private void InitializeUI()
    {
        UpdateAvailableItems();
        UpdateSelectedItems();
        UpdateCraftButton();
    }

    private void UpdateAvailableItems()
    {
        var items = _craftEngine.GetAvailableItems()
            .Select(i => new CraftItemViewModel
            {
                ObjectId = i.ObjectId,
                Name = i.Name,
                Quantity = i.Quantity,
                SelectedQuantity = _craftEngine.GetSelectedIngredients()
                    .FirstOrDefault(s => s.ObjectId.Equals(i.ObjectId, StringComparison.OrdinalIgnoreCase))?.SelectedQuantity ?? 0,
                Location = i.Location,
                Weight = i.Weight,
                Volume = i.Volume
            })
            .ToList();

        AvailableItemsList.ItemsSource = null;
        AvailableItemsList.ItemsSource = items;
    }

    private void UpdateSelectedItems()
    {
        SelectedItemsList.ItemsSource = null;
        SelectedItemsList.ItemsSource = _craftEngine.GetSelectedIngredients();
    }

    private void UpdateCraftButton()
    {
        var recipe = _craftEngine.GetMatchingRecipe();
        CraftButton.IsEnabled = recipe != null;

        if (recipe != null)
        {
            RecipeMatchText.Text = $"Fabricaras: {recipe.Name}";
            RecipeMatchText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x90, 0xEE, 0x90));
            CraftQuantityText.Text = _craftEngine.CurrentCraft?.CraftQuantity.ToString() ?? "1";
        }
        else if (_craftEngine.GetSelectedIngredients().Any())
        {
            RecipeMatchText.Text = "No hay receta para esta combinacion";
            RecipeMatchText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
        }
        else
        {
            RecipeMatchText.Text = "Selecciona ingredientes para fabricar";
            RecipeMatchText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x88, 0x88, 0x88));
        }
    }

    #region Event Handlers - Available Items

    private void AvailableItemsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _addQuantity = 1;
        AddQuantityText.Text = "1";
    }

    private void AvailableItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        AddBtn_Click(sender, e);
    }

    private void AddMinusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_addQuantity > 1)
        {
            _addQuantity--;
            AddQuantityText.Text = _addQuantity.ToString();
        }
    }

    private void AddPlusBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = AvailableItemsList.SelectedItem as CraftItemViewModel;
        if (selectedItem == null) return;

        var maxCanAdd = selectedItem.AvailableQuantity;
        if (_addQuantity < maxCanAdd)
        {
            _addQuantity++;
            AddQuantityText.Text = _addQuantity.ToString();
        }
    }

    private void AddBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = AvailableItemsList.SelectedItem as CraftItemViewModel;
        if (selectedItem == null) return;

        _craftEngine.AddIngredient(selectedItem.ObjectId, _addQuantity);

        _addQuantity = 1;
        AddQuantityText.Text = "1";

        UpdateAvailableItems();
        UpdateSelectedItems();
        UpdateCraftButton();
    }

    #endregion

    #region Event Handlers - Selected Items

    private void SelectedItemsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // Nada especial por ahora
    }

    private void SelectedItemsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        RemoveBtn_Click(sender, e);
    }

    private void RemoveBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = SelectedItemsList.SelectedItem as CraftItem;
        if (selectedItem == null) return;

        _craftEngine.RemoveIngredient(selectedItem.ObjectId, 1);

        UpdateAvailableItems();
        UpdateSelectedItems();
        UpdateCraftButton();
    }

    private void ClearBtn_Click(object sender, RoutedEventArgs e)
    {
        _craftEngine.ClearIngredients();

        UpdateAvailableItems();
        UpdateSelectedItems();
        UpdateCraftButton();
    }

    #endregion

    #region Event Handlers - Craft Controls

    private void CraftMinusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_craftEngine.CurrentCraft != null && _craftEngine.CurrentCraft.CraftQuantity > 1)
        {
            _craftEngine.SetCraftQuantity(_craftEngine.CurrentCraft.CraftQuantity - 1);
            CraftQuantityText.Text = _craftEngine.CurrentCraft.CraftQuantity.ToString();
        }
    }

    private void CraftPlusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_craftEngine.CurrentCraft != null &&
            _craftEngine.CurrentCraft.CraftQuantity < _craftEngine.CurrentCraft.MaxCraftQuantity)
        {
            _craftEngine.SetCraftQuantity(_craftEngine.CurrentCraft.CraftQuantity + 1);
            CraftQuantityText.Text = _craftEngine.CurrentCraft.CraftQuantity.ToString();
        }
    }

    private void CraftButton_Click(object sender, RoutedEventArgs e)
    {
        var result = _craftEngine.Craft();
        MessageText.Text = result.Message;

        if (result.Success)
        {
            MessageText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x90, 0xEE, 0x90));

            UpdateAvailableItems();
            UpdateSelectedItems();
            UpdateCraftButton();
        }
        else
        {
            MessageText.Foreground = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
        }
    }

    #endregion

    #region Engine Events

    private void OnCraftEnded(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() => CraftClosed?.Invoke());
    }

    private void OnRecipeMatched(object? sender, GameObject? recipe)
    {
        Dispatcher.Invoke(UpdateCraftButton);
    }

    private void OnItemCrafted(object? sender, CraftResult result)
    {
        Dispatcher.Invoke(() =>
        {
            MessageText.Text = result.Message;
            MessageText.Foreground = new System.Windows.Media.SolidColorBrush(
                result.Success
                    ? System.Windows.Media.Color.FromRgb(0x90, 0xEE, 0x90)
                    : System.Windows.Media.Color.FromRgb(0xFF, 0x6B, 0x6B));
        });
    }

    #endregion

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Desuscribirse de eventos
        _craftEngine.CraftEnded -= OnCraftEnded;
        _craftEngine.RecipeMatched -= OnRecipeMatched;
        _craftEngine.ItemCrafted -= OnItemCrafted;

        // Cerrar fabricacion si aun esta activa
        if (_craftEngine.IsActive)
        {
            _craftEngine.CloseCraft();
        }

        CraftClosed?.Invoke();
        base.OnClosing(e);
    }
}

/// <summary>
/// ViewModel para mostrar items disponibles con cantidad disponible calculada.
/// </summary>
public class CraftItemViewModel
{
    public string ObjectId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int SelectedQuantity { get; set; }
    public CraftItemLocation Location { get; set; }
    public int Weight { get; set; }
    public double Volume { get; set; }

    /// <summary>
    /// Cantidad disponible para seleccionar (total - ya seleccionado).
    /// </summary>
    public int AvailableQuantity => Quantity - SelectedQuantity;

    /// <summary>
    /// Texto de ubicacion.
    /// </summary>
    public string LocationText => Location == CraftItemLocation.Inventory ? "(inventario)" : "(sala)";
}
