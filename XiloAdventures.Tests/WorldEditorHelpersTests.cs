using System.Collections.Generic;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Windows;
using Xunit;

namespace XiloAdventures.Tests;

public class WorldEditorHelpersTests
{
    [Fact]
    public void DeleteDoor_RemovesDoorAndKeyDefinitions_ButKeepsObjects()
    {
        var door = new Door { Id = "door-1", KeyObjectId = "obj-key", IsLocked = true };
        var room = new Room
        {
            Id = "room-1",
            Exits = new List<Exit>
            {
                new Exit { DoorId = door.Id, TargetRoomId = "room-2", Direction = "norte" }
            }
        };

        var keyObject = new GameObject { Id = "obj-key", Name = "Llave", Type = ObjectType.Llave };

        var world = new WorldModel
        {
            Doors = new List<Door> { door },
            Objects = new List<GameObject> { keyObject },
            Rooms = new List<Room> { room }
        };

        var removed = WorldEditorHelpers.DeleteDoor(world, door);

        Assert.True(removed);
        Assert.Empty(world.Doors);
        Assert.Single(world.Objects); // el objeto físico de la llave sigue existiendo
        Assert.Null(world.Rooms[0].Exits[0].DoorId);
    }

    [Fact]
    public void DeleteObject_RemovesKeyDefinitionsAndUnlocksDoors()
    {
        var door1 = new Door { Id = "door-1", KeyObjectId = "obj-key", IsLocked = true };
        var door2 = new Door { Id = "door-2", KeyObjectId = "obj-other", IsLocked = true };

        var keyObj = new GameObject { Id = "obj-key", Type = ObjectType.Llave };
        var otherObj = new GameObject { Id = "obj-other", Type = ObjectType.Llave };

        var room = new Room
        {
            Id = "room-1",
            ObjectIds = new List<string> { keyObj.Id, otherObj.Id }
        };

        var world = new WorldModel
        {
            Doors = new List<Door> { door1, door2 },
            Objects = new List<GameObject> { keyObj, otherObj },
            Rooms = new List<Room> { room },
            Npcs = new List<Npc>()
        };

        var removed = WorldEditorHelpers.DeleteObject(world, keyObj);

        Assert.True(removed);
        Assert.DoesNotContain(keyObj, world.Objects);

        Assert.False(door1.IsLocked);
        Assert.Null(door1.KeyObjectId);

        Assert.True(door2.IsLocked); // puerta con otro lock no se toca
        Assert.Equal("obj-other", door2.KeyObjectId);

        Assert.DoesNotContain(keyObj.Id, world.Rooms[0].ObjectIds);
        Assert.Contains(otherObj.Id, world.Rooms[0].ObjectIds);
    }
}
