using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Wpf.Common.Windows;

/// <summary>
/// Ventana de comercio entre jugador y NPC comerciante.
/// </summary>
public partial class TradeWindow : Window
{
    private readonly TradeEngine _tradeEngine;
    private readonly GameState _gameState;
    private readonly Npc _merchant;

    private int _buyQuantity = 1;
    private int _sellQuantity = 1;

    /// <summary>
    /// Evento disparado cuando se cierra el comercio.
    /// </summary>
    public event Action? TradeClosed;

    /// <summary>
    /// Evento disparado cuando el jugador compra un item.
    /// </summary>
    public event Action<string>? ItemBought;

    /// <summary>
    /// Evento disparado cuando el jugador vende un item.
    /// </summary>
    public event Action<string>? ItemSold;

    public TradeWindow(TradeEngine tradeEngine, GameState gameState, Npc merchant)
    {
        InitializeComponent();

        _tradeEngine = tradeEngine;
        _gameState = gameState;
        _merchant = merchant;

        // Suscribirse a eventos del motor
        _tradeEngine.TradeEnded += OnTradeEnded;

        // Iniciar comercio
        _tradeEngine.StartTrade(merchant);

        // Inicializar UI
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Info del jugador
        PlayerNameText.Text = _gameState.Player.Name ?? "Jugador";
        UpdatePlayerMoney();

        // Info del comerciante
        MerchantNameText.Text = _merchant.Name;
        UpdateMerchantMoney();

        // Cargar listas
        UpdatePlayerItems();
        UpdateMerchantItems();

        // Estado inicial de botones
        UpdateBuyButton();
        UpdateSellButton();
    }

    private void UpdatePlayerMoney()
    {
        PlayerMoneyText.Text = _tradeEngine.GetPlayerMoney().ToString("N0");
    }

    private void UpdateMerchantMoney()
    {
        if (_tradeEngine.NpcHasInfiniteMoney())
        {
            MerchantMoneyText.Text = "Infinito";
        }
        else
        {
            MerchantMoneyText.Text = _tradeEngine.GetNpcMoney().ToString("N0");
        }
    }

    private void UpdatePlayerItems()
    {
        PlayerItemsList.ItemsSource = null;
        PlayerItemsList.ItemsSource = _tradeEngine.GetPlayerItems();
    }

    private void UpdateMerchantItems()
    {
        MerchantItemsList.ItemsSource = null;
        MerchantItemsList.ItemsSource = _tradeEngine.GetNpcItems();
    }

    private void UpdateBuyButton()
    {
        var selectedItem = MerchantItemsList.SelectedItem as TradeItem;

        if (selectedItem == null)
        {
            BuyButton.IsEnabled = false;
            BuyButton.Content = "COMPRAR";
            BuyTotalText.Text = "";
            return;
        }

        var totalPrice = selectedItem.CalculatedPrice * _buyQuantity;
        var canAfford = _gameState.Player.Money >= totalPrice;

        BuyButton.IsEnabled = canAfford;
        BuyButton.Content = $"COMPRAR ({totalPrice})";
        BuyTotalText.Text = canAfford ? "" : "Dinero insuficiente";
        BuyTotalText.Foreground = new SolidColorBrush(canAfford ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 107, 107));
    }

    private void UpdateSellButton()
    {
        var selectedItem = PlayerItemsList.SelectedItem as TradeItem;

        if (selectedItem == null)
        {
            SellButton.IsEnabled = false;
            SellButton.Content = "VENDER";
            SellTotalText.Text = "";
            return;
        }

        var totalPrice = selectedItem.CalculatedPrice * _sellQuantity;
        var npcCanAfford = _tradeEngine.NpcHasInfiniteMoney() || _tradeEngine.GetNpcMoney() >= totalPrice;

        SellButton.IsEnabled = npcCanAfford;
        SellButton.Content = $"VENDER ({totalPrice})";
        SellTotalText.Text = npcCanAfford ? "" : "El comerciante no tiene suficiente dinero";
        SellTotalText.Foreground = new SolidColorBrush(npcCanAfford ? Color.FromRgb(255, 215, 0) : Color.FromRgb(255, 107, 107));
    }

    #region Event Handlers

    private void PlayerItemsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _sellQuantity = 1;
        SellQuantityText.Text = "1";
        UpdateSellButton();
    }

    private void MerchantItemsList_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        _buyQuantity = 1;
        BuyQuantityText.Text = "1";
        UpdateBuyButton();
    }

    private void BuyMinusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_buyQuantity > 1)
        {
            _buyQuantity--;
            BuyQuantityText.Text = _buyQuantity.ToString();
            UpdateBuyButton();
        }
    }

    private void BuyPlusBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = MerchantItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        var maxQuantity = _tradeEngine.GetMaxBuyQuantity(selectedItem.ObjectId);
        if (_buyQuantity < maxQuantity)
        {
            _buyQuantity++;
            BuyQuantityText.Text = _buyQuantity.ToString();
            UpdateBuyButton();
        }
    }

    private void SellMinusBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_sellQuantity > 1)
        {
            _sellQuantity--;
            SellQuantityText.Text = _sellQuantity.ToString();
            UpdateSellButton();
        }
    }

    private void SellPlusBtn_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = PlayerItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        var maxQuantity = _tradeEngine.GetMaxSellQuantity(selectedItem.ObjectId);
        if (_sellQuantity < maxQuantity)
        {
            _sellQuantity++;
            SellQuantityText.Text = _sellQuantity.ToString();
            UpdateSellButton();
        }
    }

    private void BuyButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = MerchantItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        var result = _tradeEngine.BuyItem(selectedItem.ObjectId, _buyQuantity);

        if (result.Success)
        {
            UpdatePlayerMoney();
            UpdateMerchantMoney();
            UpdatePlayerItems();
            UpdateMerchantItems();

            _buyQuantity = 1;
            BuyQuantityText.Text = "1";
            MerchantItemsList.SelectedItem = null;
            UpdateBuyButton();

            ItemBought?.Invoke(selectedItem.ObjectId);
        }
    }

    private void SellButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedItem = PlayerItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        var result = _tradeEngine.SellItem(selectedItem.ObjectId, _sellQuantity);

        if (result.Success)
        {
            UpdatePlayerMoney();
            UpdateMerchantMoney();
            UpdatePlayerItems();
            UpdateMerchantItems();

            _sellQuantity = 1;
            SellQuantityText.Text = "1";
            PlayerItemsList.SelectedItem = null;
            UpdateSellButton();

            ItemSold?.Invoke(selectedItem.ObjectId);
        }
    }

    private void PlayerItemsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var selectedItem = PlayerItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        // Vender 1 unidad
        var result = _tradeEngine.SellItem(selectedItem.ObjectId, 1);

        if (result.Success)
        {
            UpdatePlayerMoney();
            UpdateMerchantMoney();
            UpdatePlayerItems();
            UpdateMerchantItems();

            _sellQuantity = 1;
            SellQuantityText.Text = "1";
            UpdateSellButton();

            ItemSold?.Invoke(selectedItem.ObjectId);
        }
    }

    private void MerchantItemsList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var selectedItem = MerchantItemsList.SelectedItem as TradeItem;
        if (selectedItem == null) return;

        // Comprar 1 unidad
        var result = _tradeEngine.BuyItem(selectedItem.ObjectId, 1);

        if (result.Success)
        {
            UpdatePlayerMoney();
            UpdateMerchantMoney();
            UpdatePlayerItems();
            UpdateMerchantItems();

            _buyQuantity = 1;
            BuyQuantityText.Text = "1";
            UpdateBuyButton();

            ItemBought?.Invoke(selectedItem.ObjectId);
        }
    }

    #endregion

    #region Trade Engine Events

    private void OnTradeEnded(object? sender, EventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            TradeClosed?.Invoke();
        });
    }

    #endregion

    private void CloseTrade()
    {
        _tradeEngine.CloseTrade();
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Desuscribirse de eventos
        _tradeEngine.TradeEnded -= OnTradeEnded;

        // Cerrar comercio si aun esta activo
        if (_tradeEngine.IsActive)
        {
            _tradeEngine.CloseTrade();
        }

        TradeClosed?.Invoke();
        base.OnClosing(e);
    }
}
