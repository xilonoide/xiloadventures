using System;
using System.Text.Json;
using XiloAdventures.Engine;

namespace XiloAdventures.Wpf.Common.Ui;

public class UiSettings
{
    public bool SoundEnabled { get; set; } = true;
    public double FontSize { get; set; } = 18.0;
    public string FontFamily { get; set; } = "Segoe UI";
    public int Version { get; set; } = 3;
    /// <summary>
    /// Si está activo, al no entender un comando se consultará un LLM local.
    /// </summary>
    public bool UseLlmForUnknownCommands { get; set; } = false;
    /// <summary>
    /// Volumen de la música (0-10).
    /// </summary>
    public double MusicVolume { get; set; } = 10.0;
    /// <summary>
    /// Volumen de los efectos de sonido (0-10).
    /// </summary>
    public double EffectsVolume { get; set; } = 10.0;
    /// <summary>
    /// Volumen maestro (1-10).
    /// </summary>
    public double MasterVolume { get; set; } = 10.0;
    /// <summary>
    /// Volumen de la voz (0-10).
    /// </summary>
    public double VoiceVolume { get; set; } = 10.0;
    /// <summary>
    /// Si está activo, se muestra el mapa de salas visitadas.
    /// </summary>
    public bool MapEnabled { get; set; } = true;
}

public static class UiSettingsManager
{
    private const int CurrentVersion = 3;

    public static UiSettings GlobalSettings { get; private set; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static void LoadGlobal()
    {
        var path = AppPaths.GlobalConfigPath;
        try
        {
            if (System.IO.File.Exists(path))
            {
                var json = CryptoUtil.DecryptFromFile(path);
                GlobalSettings = Upgrade(JsonSerializer.Deserialize<UiSettings>(json, Options) ?? new UiSettings());
            }
        }
        catch
        {
            GlobalSettings = new UiSettings();
        }

        GlobalSettings = Upgrade(GlobalSettings);
    }

    public static void SaveGlobal()
    {
        GlobalSettings = Upgrade(GlobalSettings);
        var path = AppPaths.GlobalConfigPath;
        var json = JsonSerializer.Serialize(GlobalSettings, Options);
        CryptoUtil.EncryptToFile(path, json, "xac");
    }

    public static UiSettings LoadForWorld(string worldId) => LoadForWorld(worldId, null);

    public static UiSettings LoadForWorld(string worldId, string? gameDefaultFontFamily)
    {
        var path = AppPaths.WorldConfigPath(worldId);
        try
        {
            if (System.IO.File.Exists(path))
            {
                var json = CryptoUtil.DecryptFromFile(path);
                return Upgrade(JsonSerializer.Deserialize<UiSettings>(json, Options) ?? new UiSettings());
            }
        }
        catch
        {
            // ignorar y devolver por defecto
        }

        // Por defecto heredamos de las globales, pero usamos la fuente por defecto del juego si está definida
        return Upgrade(new UiSettings
        {
            SoundEnabled = GlobalSettings.SoundEnabled,
            FontSize = GlobalSettings.FontSize,
            FontFamily = gameDefaultFontFamily ?? GlobalSettings.FontFamily,
            UseLlmForUnknownCommands = GlobalSettings.UseLlmForUnknownCommands,
            MusicVolume = GlobalSettings.MusicVolume,
            EffectsVolume = GlobalSettings.EffectsVolume,
            MasterVolume = GlobalSettings.MasterVolume,
            VoiceVolume = GlobalSettings.VoiceVolume,
            MapEnabled = GlobalSettings.MapEnabled
        });
    }

    public static void SaveForWorld(string worldId, UiSettings settings)
    {
        settings = Upgrade(settings);
        var path = AppPaths.WorldConfigPath(worldId);
        var json = JsonSerializer.Serialize(settings, Options);
        CryptoUtil.EncryptToFile(path, json, "xac");
    }

    private static UiSettings Upgrade(UiSettings settings)
    {
        settings ??= new UiSettings();

        if (settings.Version < 2)
        {
            settings.FontSize += 2.0;
        }
        if (settings.Version < 3)
        {
            settings.FontSize = 18.0;
        }

        settings.Version = CurrentVersion;
        return settings;
    }
}
