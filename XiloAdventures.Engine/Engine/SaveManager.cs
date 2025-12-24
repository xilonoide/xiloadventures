using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Data transfer object for saved game state.
/// Contains all serializable game data for persistence.
/// </summary>
public class SaveData
{
    public string WorldId { get; set; } = string.Empty;
    public string CurrentRoomId { get; set; } = string.Empty;
    public PlayerStats Player { get; set; } = new();

    // Inventario: solo guardamos los Ids de objetos que lleva el jugador.
    public List<string> InventoryObjectIds { get; set; } = new();

    // Estado del mundo
    public List<Room>? Rooms { get; set; }
    public List<GameObject>? Objects { get; set; }
    public List<Npc>? Npcs { get; set; }

    public List<Door>? Doors { get; set; }

    public Dictionary<string, QuestState>? Quests { get; set; }
    public List<UseRule>? UseRules { get; set; }
    public List<TradeRule>? TradeRules { get; set; }
    public List<EventRule>? Events { get; set; }

    public Dictionary<string, bool>? Flags { get; set; }

    public int TurnCounter { get; set; }
    public string TimeOfDay { get; set; } = "día";
    public WeatherType Weather { get; set; } = WeatherType.Despejado;
    public DateTime GameTime { get; set; }

    public string? WorldMusicId { get; set; }
}


/// <summary>
/// Handles saving and loading game state to/from .xas files.
/// Supports encrypted saves and automatic autosave functionality.
/// </summary>
public static class SaveManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Saves the current game state to a file.
    /// </summary>
    /// <param name="state">The game state to save.</param>
    /// <param name="path">The file path to save to.</param>
    /// <param name="encryptionKey">Optional encryption key for the save file.</param>
    public static void SaveToPath(GameState state, string path, string? encryptionKey = null)
    {
        var data = new SaveData
        {
            WorldId = state.WorldId,
            CurrentRoomId = state.CurrentRoomId,
            Player = state.Player,
            InventoryObjectIds = new List<string>(state.InventoryObjectIds),

            Rooms = new List<Room>(state.Rooms),
            Objects = new List<GameObject>(state.Objects),
            Npcs = new List<Npc>(state.Npcs),
            Doors = new List<Door>(state.Doors),

            Quests = new Dictionary<string, QuestState>(state.Quests),
            UseRules = new List<UseRule>(state.UseRules),
            TradeRules = new List<TradeRule>(state.TradeRules),
            Events = new List<EventRule>(state.Events),

            TurnCounter = state.TurnCounter,
            TimeOfDay = state.TimeOfDay,
            Weather = state.Weather,
            GameTime = state.GameTime,

            Flags = new Dictionary<string, bool>(state.Flags),
            WorldMusicId = state.WorldMusicId
        };

        var json = JsonSerializer.Serialize(data, Options);
        var effectiveKey = CryptoUtil.GetEffectiveSaveKey(encryptionKey);
        CryptoUtil.EncryptToFile(path, json, "xas", effectiveKey);
    }

    /// <summary>
    /// Loads a game state from a saved file.
    /// </summary>
    /// <param name="path">The path to the save file.</param>
    /// <param name="world">The world model to use for missing data.</param>
    /// <returns>The loaded game state.</returns>
    public static GameState LoadFromPath(string path, WorldModel world)
    {
        var effectiveKey = CryptoUtil.GetEffectiveSaveKey(world.Game.EncryptionKey);
        var json = CryptoUtil.DecryptFromFile(path, effectiveKey);
        var data = JsonSerializer.Deserialize<SaveData>(json, Options) ?? new SaveData();

        // Crear diccionarios con comparador case-insensitive (JSON deserializa sin comparador)
        var quests = new Dictionary<string, QuestState>(data.Quests ?? [], StringComparer.OrdinalIgnoreCase);
        var flags = new Dictionary<string, bool>(data.Flags ?? [], StringComparer.OrdinalIgnoreCase);

        var state = new GameState
        {
            WorldId = data.WorldId,
            WorldMusicId = data.WorldMusicId,
            CurrentRoomId = data.CurrentRoomId,
            Player = data.Player,
            Rooms = data.Rooms!,
            Objects = data.Objects!,
            Npcs = data.Npcs!,
            Doors = data.Doors!,
            Quests = quests,
            UseRules = data.UseRules!,
            TradeRules = data.TradeRules!,
            Events = data.Events!,
            InventoryObjectIds = data.InventoryObjectIds ?? new List<string>(),
            TurnCounter = data.TurnCounter,
            TimeOfDay = data.TimeOfDay,
            Weather = data.Weather,
            GameTime = data.GameTime,
            Flags = flags
        };

        WorldLoader.RebuildRoomIndexes(state);
        return state;
    }

    /// <summary>
    /// Automatically saves the game state to a standard autosave file.
    /// </summary>
    /// <param name="state">The game state to save.</param>
    /// <param name="savesFolder">The folder to save to.</param>
    /// <param name="encryptionKey">Optional encryption key.</param>
    public static void AutoSave(GameState state, string savesFolder, string? encryptionKey = null)
    {
        Directory.CreateDirectory(savesFolder);
        var fileName = $"autosave_{state.WorldId}.xas";
        var path = Path.Combine(savesFolder, fileName);
        SaveToPath(state, path, encryptionKey);
    }
}
