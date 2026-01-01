using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using Xunit;

namespace XiloAdventures.Tests;

public class SaveManagerTests
{
    [Fact]
    public void SaveAndLoadRoundtrip_PreservesStateAndIndexes()
    {
        var roomA = new Room { Id = "room-a", Name = "A" };
        var roomB = new Room { Id = "room-b", Name = "B" };
        var obj = new GameObject { Id = "obj-1", RoomId = roomA.Id, Name = "Objeto" };
        var npc = new Npc { Id = "npc-1", RoomId = roomB.Id, Name = "NPC" };

        var world = new WorldModel
        {
            Game = new GameInfo { Id = "world-1", StartRoomId = roomA.Id },
            Rooms = new List<Room> { roomA, roomB },
            Objects = new List<GameObject> { obj },
            Npcs = new List<Npc> { npc }
        };

        var state = WorldLoader.CreateInitialState(world);
        state.CurrentRoomId = roomB.Id;
        state.InventoryObjectIds.Add(obj.Id);
        state.Flags["door.open"] = true;

        var tempFile = Path.Combine(Path.GetTempPath(), $"save_test_{Guid.NewGuid():N}.xas");

        try
        {
            SaveManager.SaveToPath(state, tempFile);

            var loaded = SaveManager.LoadFromPath(tempFile, world);

            Assert.Equal(state.WorldId, loaded.WorldId);
            Assert.Equal(roomB.Id, loaded.CurrentRoomId);
            Assert.Contains(obj.Id, loaded.InventoryObjectIds);
            Assert.True(loaded.Flags["door.open"]);

            var loadedRoomA = loaded.Rooms.Single(r => r.Id == roomA.Id);
            var loadedRoomB = loaded.Rooms.Single(r => r.Id == roomB.Id);

            Assert.Contains(obj.Id, loadedRoomA.ObjectIds);
            Assert.Contains(npc.Id, loadedRoomB.NpcIds);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void AutoSave_CreatesFile()
    {
        var room = new Room { Id = "r", Name = "Room" };
        var world = new WorldModel
        {
            Game = new GameInfo { Id = "auto_world", StartRoomId = room.Id },
            Rooms = new List<Room> { room }
        };
        var state = WorldLoader.CreateInitialState(world);

        var savesFolder = Path.Combine(Path.GetTempPath(), $"xilo_autosave_{Guid.NewGuid():N}");
        Directory.CreateDirectory(savesFolder);

        try
        {
            SaveManager.AutoSave(state, savesFolder);

            var expected = Path.Combine(savesFolder, "autosave.xas");
            Assert.True(File.Exists(expected));
        }
        finally
        {
            if (Directory.Exists(savesFolder))
            {
                Directory.Delete(savesFolder, true);
            }
        }
    }
}
