using System;
using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Interfaces;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Motor de comercio para gestionar compra/venta entre jugador y NPCs comerciantes.
/// </summary>
public class TradeEngine : ITradeEngine
{
    private readonly GameState _gameState;
    private Npc? _currentMerchant;

    /// <summary>
    /// Estado actual del comercio (null si no hay comercio activo).
    /// </summary>
    public TradeState? CurrentTrade { get; private set; }

    /// <summary>
    /// Indica si hay un comercio activo.
    /// </summary>
    public bool IsActive => CurrentTrade?.IsActive == true;

    /// <summary>
    /// Evento disparado cuando se añade una entrada al log de comercio.
    /// </summary>
    public event EventHandler<TradeLogEntry>? LogEntryAdded;

    /// <summary>
    /// Evento disparado cuando el comercio termina.
    /// </summary>
    public event EventHandler? TradeEnded;

    /// <summary>
    /// Evento disparado cuando el jugador compra un item.
    /// </summary>
    public event EventHandler<TradeItem>? ItemBought;

    /// <summary>
    /// Evento disparado cuando el jugador vende un item.
    /// </summary>
    public event EventHandler<TradeItem>? ItemSold;

    public TradeEngine(GameState gameState)
    {
        _gameState = gameState;
    }

    /// <summary>
    /// Inicia una sesión de comercio con un NPC comerciante.
    /// </summary>
    public void StartTrade(Npc merchant)
    {
        _currentMerchant = merchant;

        CurrentTrade = new TradeState
        {
            IsActive = true,
            NpcId = merchant.Id,
            NpcName = merchant.Name,
            NpcMoney = merchant.Money,
            PlayerMoney = _gameState.Player.Money,
            BuyMultiplier = merchant.BuyPriceMultiplier,
            SellMultiplier = merchant.SellPriceMultiplier,
            NpcItems = BuildNpcItems(merchant),
            PlayerItems = BuildPlayerItems(merchant),
            TradeLog = new List<TradeLogEntry>()
        };

        AddLogEntry($"Comercio iniciado con {merchant.Name}", TradeLogType.Info);
    }

    /// <summary>
    /// Compra un item del NPC (jugador paga al NPC).
    /// </summary>
    public TradeResult BuyItem(string objectId, int quantity = 1)
    {
        if (!IsActive || CurrentTrade == null || _currentMerchant == null)
            return new TradeResult { Success = false, Message = "No hay comercio activo." };

        var item = CurrentTrade.NpcItems.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            return new TradeResult { Success = false, Message = "El comerciante no tiene ese objeto." };

        // Validar cantidad
        if (quantity <= 0)
            return new TradeResult { Success = false, Message = "Cantidad inválida." };

        if (item.Quantity > 0 && quantity > item.Quantity)
            return new TradeResult { Success = false, Message = $"Solo hay {item.Quantity} disponibles." };

        // Calcular precio total
        var totalPrice = item.CalculatedPrice * quantity;

        // Validar oro del jugador
        if (_gameState.Player.Money < totalPrice)
            return new TradeResult { Success = false, Message = $"No tienes suficiente dinero. Necesitas {totalPrice}." };

        // Realizar transacción
        _gameState.Player.Money -= totalPrice;
        CurrentTrade.PlayerMoney = _gameState.Player.Money;

        // Si el NPC tiene oro limitado, añadir
        if (_currentMerchant.Money >= 0)
        {
            _currentMerchant.Money += totalPrice;
            CurrentTrade.NpcMoney = _currentMerchant.Money;
        }

        // Transferir objeto(s) al jugador
        var gameObject = _gameState.Objects.FirstOrDefault(o =>
            o.Id.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        if (gameObject != null)
        {
            for (int i = 0; i < quantity; i++)
            {
                _gameState.InventoryObjectIds.Add(objectId);
            }
        }

        // Actualizar stock del NPC si es limitado
        if (item.Quantity > 0)
        {
            item.Quantity -= quantity;
            var shopItem = _currentMerchant.ShopInventory.FirstOrDefault(si =>
                si.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));
            if (shopItem != null)
            {
                shopItem.Quantity -= quantity;
            }
            if (item.Quantity <= 0)
            {
                CurrentTrade.NpcItems.Remove(item);
                if (shopItem != null)
                {
                    _currentMerchant.ShopInventory.Remove(shopItem);
                }
            }
        }

        // Actualizar lista de items del jugador
        CurrentTrade.PlayerItems = BuildPlayerItems(_currentMerchant);

        var message = quantity > 1
            ? $"Compras {quantity}x {item.Name} por {totalPrice}."
            : $"Compras {item.Name} por {totalPrice}.";

        AddLogEntry(message, TradeLogType.Buy);
        ItemBought?.Invoke(this, item);

        return new TradeResult
        {
            Success = true,
            Message = message,
            MoneyTransferred = totalPrice,
            ItemsTransferred = quantity
        };
    }

    /// <summary>
    /// Vende un item al NPC (jugador recibe oro del NPC).
    /// </summary>
    public TradeResult SellItem(string objectId, int quantity = 1)
    {
        if (!IsActive || CurrentTrade == null || _currentMerchant == null)
            return new TradeResult { Success = false, Message = "No hay comercio activo." };

        var item = CurrentTrade.PlayerItems.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        if (item == null)
            return new TradeResult { Success = false, Message = "No tienes ese objeto para vender." };

        // Validar cantidad
        if (quantity <= 0)
            return new TradeResult { Success = false, Message = "Cantidad inválida." };

        if (quantity > item.Quantity)
            return new TradeResult { Success = false, Message = $"Solo tienes {item.Quantity} para vender." };

        // Calcular precio total
        var totalPrice = item.CalculatedPrice * quantity;

        // Validar oro del NPC (si no es infinito)
        if (_currentMerchant.Money >= 0 && _currentMerchant.Money < totalPrice)
            return new TradeResult { Success = false, Message = $"El comerciante no tiene suficiente dinero." };

        // Realizar transacción
        _gameState.Player.Money += totalPrice;
        CurrentTrade.PlayerMoney = _gameState.Player.Money;

        // Si el NPC tiene oro limitado, restar
        if (_currentMerchant.Money >= 0)
        {
            _currentMerchant.Money -= totalPrice;
            CurrentTrade.NpcMoney = _currentMerchant.Money;
        }

        // Remover objeto(s) del inventario del jugador
        for (int i = 0; i < quantity; i++)
        {
            _gameState.InventoryObjectIds.Remove(objectId);
        }

        // Actualizar cantidad del item
        item.Quantity -= quantity;
        if (item.Quantity <= 0)
        {
            CurrentTrade.PlayerItems.Remove(item);
        }

        // Actualizar lista de items del NPC (el objeto ahora está disponible para comprar)
        CurrentTrade.NpcItems = BuildNpcItems(_currentMerchant);

        var message = quantity > 1
            ? $"Vendes {quantity}x {item.Name} por {totalPrice}."
            : $"Vendes {item.Name} por {totalPrice}.";

        AddLogEntry(message, TradeLogType.Sell);
        ItemSold?.Invoke(this, item);

        return new TradeResult
        {
            Success = true,
            Message = message,
            MoneyTransferred = totalPrice,
            ItemsTransferred = quantity
        };
    }

    /// <summary>
    /// Cierra la sesión de comercio.
    /// </summary>
    public void CloseTrade()
    {
        if (CurrentTrade != null)
        {
            AddLogEntry("Comercio finalizado.", TradeLogType.Info);
            CurrentTrade.IsActive = false;
        }

        _currentMerchant = null;
        CurrentTrade = null;
        TradeEnded?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Obtiene la lista actual de items del NPC.
    /// </summary>
    public List<TradeItem> GetNpcItems() => CurrentTrade?.NpcItems ?? new List<TradeItem>();

    /// <summary>
    /// Obtiene la lista actual de items del jugador.
    /// </summary>
    public List<TradeItem> GetPlayerItems() => CurrentTrade?.PlayerItems ?? new List<TradeItem>();

    /// <summary>
    /// Obtiene el oro actual del jugador.
    /// </summary>
    public int GetPlayerMoney() => CurrentTrade?.PlayerMoney ?? _gameState.Player.Money;

    /// <summary>
    /// Obtiene el oro actual del NPC (-1 si es infinito).
    /// </summary>
    public int GetNpcMoney() => CurrentTrade?.NpcMoney ?? -1;

    /// <summary>
    /// Indica si el NPC tiene oro infinito.
    /// </summary>
    public bool NpcHasInfiniteMoney() => (CurrentTrade?.NpcMoney ?? -1) < 0;

    /// <summary>
    /// Calcula el precio máximo que el jugador puede pagar (considerando su oro).
    /// </summary>
    public int GetMaxBuyQuantity(string objectId)
    {
        if (CurrentTrade == null) return 0;

        var item = CurrentTrade.NpcItems.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        if (item == null || item.CalculatedPrice <= 0) return 0;

        var maxByMoney = _gameState.Player.Money / item.CalculatedPrice;
        var maxByStock = item.Quantity > 0 ? item.Quantity : int.MaxValue;

        return Math.Min(maxByMoney, maxByStock);
    }

    /// <summary>
    /// Calcula la cantidad máxima que el jugador puede vender (considerando el oro del NPC).
    /// </summary>
    public int GetMaxSellQuantity(string objectId)
    {
        if (CurrentTrade == null || _currentMerchant == null) return 0;

        var item = CurrentTrade.PlayerItems.FirstOrDefault(i =>
            i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        if (item == null || item.CalculatedPrice <= 0) return 0;

        var maxByStock = item.Quantity;

        // Si el NPC tiene oro infinito, solo limitar por stock
        if (_currentMerchant.Money < 0)
            return maxByStock;

        var maxByNpcMoney = _currentMerchant.Money / item.CalculatedPrice;
        return Math.Min(maxByStock, maxByNpcMoney);
    }

    private List<TradeItem> BuildNpcItems(Npc merchant)
    {
        var items = new List<TradeItem>();

        foreach (var shopItem in merchant.ShopInventory)
        {
            var gameObject = _gameState.Objects.FirstOrDefault(o =>
                o.Id.Equals(shopItem.ObjectId, StringComparison.OrdinalIgnoreCase));

            if (gameObject == null || gameObject.Price <= 0) continue;

            var calculatedPrice = (int)Math.Ceiling(gameObject.Price * merchant.SellPriceMultiplier);

            var displayName = gameObject.Name;
            if (gameObject.IsLightSource)
            {
                var turnsDisplay = gameObject.LightTurnsRemaining == -1 ? "∞" : gameObject.LightTurnsRemaining.ToString();
                displayName = $"{gameObject.Name} ({turnsDisplay})";
            }

            items.Add(new TradeItem
            {
                ObjectId = gameObject.Id,
                Name = displayName,
                Description = gameObject.Description,
                BasePrice = gameObject.Price,
                CalculatedPrice = calculatedPrice,
                Quantity = shopItem.Quantity, // -1 = infinito, >= 0 = cantidad limitada
                Type = gameObject.Type,
                AttackBonus = gameObject.AttackBonus,
                DefenseBonus = gameObject.DefenseBonus,
                HealthRestore = gameObject.Type == ObjectType.Comida || gameObject.Type == ObjectType.Bebida ? 25 : 0,
                IsMagicWeapon = gameObject.DamageType == DamageType.Magical
            });
        }

        return items;
    }

    private List<TradeItem> BuildPlayerItems(Npc merchant)
    {
        var items = new List<TradeItem>();
        var groupedItems = _gameState.InventoryObjectIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var group in groupedItems)
        {
            var gameObject = _gameState.Objects.FirstOrDefault(o =>
                o.Id.Equals(group.Key, StringComparison.OrdinalIgnoreCase));

            if (gameObject == null || gameObject.Price <= 0) continue;

            var calculatedPrice = (int)Math.Floor(gameObject.Price * merchant.BuyPriceMultiplier);

            // Solo mostrar items con precio > 0
            if (calculatedPrice <= 0) continue;

            var displayName = gameObject.Name;
            if (gameObject.IsLightSource)
            {
                var turnsDisplay = gameObject.LightTurnsRemaining == -1 ? "∞" : gameObject.LightTurnsRemaining.ToString();
                displayName = $"{gameObject.Name} ({turnsDisplay})";
            }

            items.Add(new TradeItem
            {
                ObjectId = gameObject.Id,
                Name = displayName,
                Description = gameObject.Description,
                BasePrice = gameObject.Price,
                CalculatedPrice = calculatedPrice,
                Quantity = group.Count(),
                Type = gameObject.Type,
                AttackBonus = gameObject.AttackBonus,
                DefenseBonus = gameObject.DefenseBonus,
                HealthRestore = gameObject.Type == ObjectType.Comida || gameObject.Type == ObjectType.Bebida ? 25 : 0,
                IsMagicWeapon = gameObject.DamageType == DamageType.Magical
            });
        }

        return items;
    }

    private void AddLogEntry(string message, TradeLogType logType)
    {
        var entry = new TradeLogEntry
        {
            Message = message,
            Timestamp = DateTime.Now,
            LogType = logType
        };

        CurrentTrade?.TradeLog.Add(entry);
        LogEntryAdded?.Invoke(this, entry);
    }
}
