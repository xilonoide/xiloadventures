using System;
using System.Collections.Generic;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Engine.Interfaces;

/// <summary>
/// Interface for the trade system engine.
/// Handles buying and selling items between player and NPC merchants.
/// </summary>
public interface ITradeEngine
{
    /// <summary>
    /// Gets whether a trade session is currently active.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Gets the current trade session state, if any.
    /// </summary>
    TradeState? CurrentTrade { get; }

    /// <summary>
    /// Event fired when a log entry is added to the trade log.
    /// </summary>
    event EventHandler<TradeLogEntry>? LogEntryAdded;

    /// <summary>
    /// Event fired when the trade session ends.
    /// </summary>
    event EventHandler? TradeEnded;

    /// <summary>
    /// Event fired when the player buys an item.
    /// </summary>
    event EventHandler<TradeItem>? ItemBought;

    /// <summary>
    /// Event fired when the player sells an item.
    /// </summary>
    event EventHandler<TradeItem>? ItemSold;

    /// <summary>
    /// Starts a trade session with the specified merchant NPC.
    /// </summary>
    /// <param name="merchant">The NPC merchant to trade with.</param>
    void StartTrade(Npc merchant);

    /// <summary>
    /// Attempts to buy an item from the merchant.
    /// </summary>
    /// <param name="objectId">The ID of the object to buy.</param>
    /// <param name="quantity">The quantity to buy.</param>
    /// <returns>Result of the buy operation.</returns>
    TradeResult BuyItem(string objectId, int quantity = 1);

    /// <summary>
    /// Attempts to sell an item to the merchant.
    /// </summary>
    /// <param name="objectId">The ID of the object to sell.</param>
    /// <param name="quantity">The quantity to sell.</param>
    /// <returns>Result of the sell operation.</returns>
    TradeResult SellItem(string objectId, int quantity = 1);

    /// <summary>
    /// Closes the current trade session.
    /// </summary>
    void CloseTrade();

    /// <summary>
    /// Gets the items available from the NPC merchant.
    /// </summary>
    /// <returns>List of trade items from the NPC.</returns>
    List<TradeItem> GetNpcItems();

    /// <summary>
    /// Gets the items the player can sell.
    /// </summary>
    /// <returns>List of trade items from the player.</returns>
    List<TradeItem> GetPlayerItems();

    /// <summary>
    /// Gets the player's current money amount.
    /// </summary>
    /// <returns>Player's money.</returns>
    int GetPlayerMoney();

    /// <summary>
    /// Gets the NPC merchant's current money amount.
    /// </summary>
    /// <returns>NPC's money, or -1 for infinite.</returns>
    int GetNpcMoney();

    /// <summary>
    /// Checks if the NPC has infinite money.
    /// </summary>
    /// <returns>True if NPC has infinite money.</returns>
    bool NpcHasInfiniteMoney();

    /// <summary>
    /// Calculates the maximum quantity of an item the player can buy.
    /// </summary>
    /// <param name="objectId">The ID of the object.</param>
    /// <returns>Maximum purchasable quantity.</returns>
    int GetMaxBuyQuantity(string objectId);

    /// <summary>
    /// Calculates the maximum quantity of an item the player can sell.
    /// </summary>
    /// <param name="objectId">The ID of the object.</param>
    /// <returns>Maximum sellable quantity.</returns>
    int GetMaxSellQuantity(string objectId);
}
