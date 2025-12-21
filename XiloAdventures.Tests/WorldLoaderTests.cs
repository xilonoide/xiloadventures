using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using Xunit;

namespace XiloAdventures.Tests;

public class WorldLoaderTests
{
    [Fact]
    public void CreateInitialState_ClonesListsAndInitializesTime()
    {
        var room = new Room { Id = "start", Name = "Start" };
        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "world-id",
                StartRoomId = room.Id,
                StartHour = 15,
                StartWeather = WeatherType.Tormenta
            },
            Rooms = new List<Room> { room }
        };

        var state = WorldLoader.CreateInitialState(world);

        Assert.NotSame(world.Rooms, state.Rooms);
        Assert.Equal(room.Id, state.CurrentRoomId);
        Assert.Equal(15, state.GameTime.Hour);
        Assert.Equal(WeatherType.Tormenta, state.Weather);
    }

    [Fact]
    public void SaveAndLoadWorldModel_RoundtripKeepsDataAndNormalizesEmptyFields()
    {
        var room = new Room
        {
            Id = "r1",
            Name = "Sala",
            ImageId = string.Empty,
            ImageBase64 = "willBeCleared",
            MusicId = string.Empty
        };

        var world = new WorldModel
        {
            Game = new GameInfo
            {
                Id = "world-id",
                Title = "Titulo",
                StartRoomId = room.Id,
                WorldMusicId = " "
            },
            Rooms = new List<Room> { room },
            RoomPositions = new Dictionary<string, MapPosition>
            {
                ["r1"] = new MapPosition { X = 10, Y = 20 }
            }
        };

        var tempFile = Path.Combine(Path.GetTempPath(), $"world_{Guid.NewGuid():N}.xaw");

        try
        {
            WorldLoader.SaveWorldModel(world, tempFile);
            var loaded = WorldLoader.LoadWorldModel(tempFile);

            Assert.Equal(world.Game.Title, loaded.Game.Title);
            Assert.Null(loaded.Game.WorldMusicId);

            var loadedRoom = loaded.Rooms.Single(r => r.Id == room.Id);
            Assert.Null(loadedRoom.ImageId);
            Assert.Null(loadedRoom.ImageBase64);
            Assert.Null(loadedRoom.MusicId);

            Assert.True(loaded.RoomPositions.ContainsKey(room.Id));
            Assert.Equal(10, loaded.RoomPositions[room.Id].X);
            Assert.Equal(20, loaded.RoomPositions[room.Id].Y);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
