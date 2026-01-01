using System;
using System.Collections.Generic;
using System.Reflection;
using XiloAdventures.Engine;
using Xunit;

public class SoundManagerTests : IDisposable
{
    private readonly SoundManager _sound;

    public SoundManagerTests()
    {
        _sound = new SoundManager();
    }

    [Fact]
    public void StopMethods_DoNotThrow_WhenNothingPlaying()
    {
        _sound.StopWorldMusic();
        _sound.StopRoomMusic();
        _sound.StopVoice();
        _sound.StopMusic();
    }

    [Fact]
    public void RefreshVolumes_DoesNotThrow_WhenNoPlayers()
    {
        _sound.RefreshVolumes();
    }

    [Fact]
    public void RemoveVoiceFromCache_RemovesEntryIfPresent()
    {
        var cacheField = typeof(SoundManager)
            .GetField("_voiceCache", BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(cacheField);

        var cache = cacheField!.GetValue(_sound) as Dictionary<string, byte[]>;
        Assert.NotNull(cache);

        var key = "room_test";
        cache![key] = Array.Empty<byte>();

        _sound.RemoveVoiceFromCache(key);

        Assert.False(cache.ContainsKey(key));
    }

    public void Dispose()
    {
        _sound.Dispose();
    }
}
