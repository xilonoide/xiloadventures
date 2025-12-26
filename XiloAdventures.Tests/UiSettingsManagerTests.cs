using System;
using System.IO;
using XiloAdventures.Wpf.Common.Ui;
using Xunit;

using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

public class UiSettingsManagerTests
{
    [Fact]
    public void SaveGlobalAndLoadGlobal_RoundtripsSettings()
    {
        var original = Clone(UiSettingsManager.GlobalSettings);

        UiSettingsManager.GlobalSettings.SoundEnabled = false;
        UiSettingsManager.GlobalSettings.FontSize = 18;
        UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands = true;
        UiSettingsManager.GlobalSettings.MusicVolume = 3;
        UiSettingsManager.GlobalSettings.EffectsVolume = 4;
        UiSettingsManager.GlobalSettings.MasterVolume = 5;
        UiSettingsManager.GlobalSettings.VoiceVolume = 6;

        UiSettingsManager.SaveGlobal();

        // Ensuring LoadGlobal really reads from disk
        UiSettingsManager.GlobalSettings.SoundEnabled = true;
        UiSettingsManager.GlobalSettings.FontSize = 10;
        UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands = false;

        UiSettingsManager.LoadGlobal();

        Assert.False(UiSettingsManager.GlobalSettings.SoundEnabled);
        Assert.Equal(18, UiSettingsManager.GlobalSettings.FontSize);
        Assert.True(UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands);
        Assert.Equal(3, UiSettingsManager.GlobalSettings.MusicVolume);
        Assert.Equal(4, UiSettingsManager.GlobalSettings.EffectsVolume);
        Assert.Equal(5, UiSettingsManager.GlobalSettings.MasterVolume);
        Assert.Equal(6, UiSettingsManager.GlobalSettings.VoiceVolume);

        RestoreGlobal(original);
    }

    [Fact]
    public void SaveAndLoadWorldSettings_RoundtripAndFallsBackToGlobal()
    {
        var originalGlobal = Clone(UiSettingsManager.GlobalSettings);
        UiSettingsManager.GlobalSettings.SoundEnabled = true;
        UiSettingsManager.GlobalSettings.FontSize = 12;
        UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands = false;
        UiSettingsManager.GlobalSettings.MusicVolume = 2;
        UiSettingsManager.GlobalSettings.EffectsVolume = 2;
        UiSettingsManager.GlobalSettings.MasterVolume = 2;
        UiSettingsManager.GlobalSettings.VoiceVolume = 2;

        var worldId = $"world_{Guid.NewGuid():N}";
        var worldSettings = new UiSettings
        {
            SoundEnabled = false,
            FontSize = 20,
            UseLlmForUnknownCommands = true,
            MusicVolume = 7,
            EffectsVolume = 8,
            MasterVolume = 9,
            VoiceVolume = 4
        };

        var worldConfigPath = AppPaths.WorldConfigPath(worldId);
        if (File.Exists(worldConfigPath))
        {
            File.Delete(worldConfigPath);
        }

        try
        {
            UiSettingsManager.SaveForWorld(worldId, worldSettings);
            var loaded = UiSettingsManager.LoadForWorld(worldId);

            Assert.Equal(worldSettings.SoundEnabled, loaded.SoundEnabled);
            Assert.Equal(worldSettings.FontSize, loaded.FontSize);
            Assert.Equal(worldSettings.UseLlmForUnknownCommands, loaded.UseLlmForUnknownCommands);
            Assert.Equal(worldSettings.MusicVolume, loaded.MusicVolume);
            Assert.Equal(worldSettings.EffectsVolume, loaded.EffectsVolume);
            Assert.Equal(worldSettings.MasterVolume, loaded.MasterVolume);
            Assert.Equal(worldSettings.VoiceVolume, loaded.VoiceVolume);

            File.Delete(worldConfigPath);

            var fallback = UiSettingsManager.LoadForWorld(worldId);

            Assert.Equal(UiSettingsManager.GlobalSettings.SoundEnabled, fallback.SoundEnabled);
            Assert.Equal(UiSettingsManager.GlobalSettings.FontSize, fallback.FontSize);
            Assert.Equal(UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands, fallback.UseLlmForUnknownCommands);
        }
        finally
        {
            RestoreGlobal(originalGlobal);
            if (File.Exists(worldConfigPath))
            {
                File.Delete(worldConfigPath);
            }
        }
    }

    private static UiSettings Clone(UiSettings source) => new()
    {
        SoundEnabled = source.SoundEnabled,
        FontSize = source.FontSize,
        UseLlmForUnknownCommands = source.UseLlmForUnknownCommands,
        MusicVolume = source.MusicVolume,
        EffectsVolume = source.EffectsVolume,
        MasterVolume = source.MasterVolume,
        VoiceVolume = source.VoiceVolume
    };

    private static void RestoreGlobal(UiSettings source)
    {
        UiSettingsManager.GlobalSettings.SoundEnabled = source.SoundEnabled;
        UiSettingsManager.GlobalSettings.FontSize = source.FontSize;
        UiSettingsManager.GlobalSettings.UseLlmForUnknownCommands = source.UseLlmForUnknownCommands;
        UiSettingsManager.GlobalSettings.MusicVolume = source.MusicVolume;
        UiSettingsManager.GlobalSettings.EffectsVolume = source.EffectsVolume;
        UiSettingsManager.GlobalSettings.MasterVolume = source.MasterVolume;
        UiSettingsManager.GlobalSettings.VoiceVolume = source.VoiceVolume;
    }
}
