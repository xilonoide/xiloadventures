using System.Collections.Generic;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using DoorService = XiloAdventures.Engine.DoorService;
using Xunit;

namespace XiloAdventures.Tests;

public class DoorServiceTests
{
    [Fact]
    public void DoorService_TryOpen_WithKeyFromAllowedSide_Succeeds()
    {
        var keyObject = new GameObject
        {
            Id = "key-1",
            Name = "Llave dorada",
            Type = ObjectType.Llave
        };

        var door = new Door
        {
            Id = "d1",
            RoomIdA = "a",
            RoomIdB = "b",
            IsLocked = true,
            KeyObjectId = "key-1",
            IsOpen = false
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject> { keyObject });

        var result = service.TryOpenDoor("d1", "a", new[] { "key-1" });

        Assert.True(result.Success);
        Assert.False(result.MissingKey);
        Assert.True(door.IsOpen);
    }

    [Fact]
    public void DoorService_TryOpen_WrongSide_Fails()
    {
        var door = new Door
        {
            Id = "d2",
            RoomIdA = "a",
            RoomIdB = "b",
            OpenFromSide = DoorOpenSide.FromAOnly
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        var result = service.TryOpenDoor("d2", "b", new[] { "anything" });

        Assert.False(result.Success);
        Assert.True(result.WrongSide);
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void DoorService_TryOpen_MissingKey_Fails()
    {
        var keyObject = new GameObject
        {
            Id = "key-3",
            Name = "Llave plateada",
            Type = ObjectType.Llave
        };

        var door = new Door
        {
            Id = "d3",
            RoomIdA = "a",
            RoomIdB = "b",
            IsLocked = true,
            KeyObjectId = "key-3"
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject> { keyObject });

        var result = service.TryOpenDoor("d3", "a", new string[0]);

        Assert.False(result.Success);
        Assert.True(result.MissingKey);
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void DoorService_TryOpen_AlreadyOpen_ReportsDesiredState()
    {
        var door = new Door
        {
            Id = "d4",
            RoomIdA = "a",
            RoomIdB = "b",
            IsOpen = true
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        var result = service.TryOpenDoor("d4", "a", new[] { "any" });

        Assert.False(result.Success);
        Assert.True(result.AlreadyInDesiredState);
    }

    [Fact]
    public void DoorService_TryClose_WhenClosed_ReportsDesiredState()
    {
        var door = new Door
        {
            Id = "d5",
            RoomIdA = "a",
            RoomIdB = "b",
            IsOpen = false
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        var result = service.TryCloseDoor("d5", "a", new[] { "any" });

        Assert.False(result.Success);
        Assert.True(result.AlreadyInDesiredState);
    }

    [Fact]
    public void DoorService_TryOpen_NotFound_ReturnsNotFound()
    {
        var service = new DoorService(new List<Door>(), new List<GameObject>());

        var result = service.TryOpenDoor("missing", "a", new[] { "any" });

        Assert.False(result.Success);
        Assert.True(result.NotFoundDoor);
    }

    [Fact]
    public void DoorService_TryClose_WhenOpen_Succeeds()
    {
        var door = new Door
        {
            Id = "d6",
            RoomIdA = "a",
            RoomIdB = "b",
            IsOpen = true
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        var result = service.TryCloseDoor("d6", "a", new string[0]);

        Assert.True(result.Success);
        Assert.False(door.IsOpen);
    }

    [Fact]
    public void DoorService_CanOperateFromRoom_BothSides_AllowsEither()
    {
        var door = new Door
        {
            Id = "d7",
            RoomIdA = "roomA",
            RoomIdB = "roomB",
            OpenFromSide = DoorOpenSide.Both
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        Assert.True(service.CanOperateFromRoom(door, "roomA"));
        Assert.True(service.CanOperateFromRoom(door, "roomB"));
        Assert.False(service.CanOperateFromRoom(door, "roomC"));
    }

    [Fact]
    public void DoorService_CanOperateFromRoom_FromBOnly_OnlyAllowsB()
    {
        var door = new Door
        {
            Id = "d8",
            RoomIdA = "roomA",
            RoomIdB = "roomB",
            OpenFromSide = DoorOpenSide.FromBOnly
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        Assert.False(service.CanOperateFromRoom(door, "roomA"));
        Assert.True(service.CanOperateFromRoom(door, "roomB"));
    }

    [Fact]
    public void DoorService_HasRequiredKey_NoLockRequired_ReturnsTrue()
    {
        var door = new Door
        {
            Id = "d9",
            RoomIdA = "a",
            RoomIdB = "b",
            IsLocked = false
        };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        Assert.True(service.HasRequiredKey(door, new string[0]));
    }

    [Fact]
    public void DoorService_GetDoor_ReturnsCorrectDoor()
    {
        var door1 = new Door { Id = "door1", Name = "Puerta Norte" };
        var door2 = new Door { Id = "door2", Name = "Puerta Sur" };

        var service = new DoorService(new List<Door> { door1, door2 }, new List<GameObject>());

        var found = service.GetDoor("door2");

        Assert.NotNull(found);
        Assert.Equal("Puerta Sur", found!.Name);
    }

    [Fact]
    public void DoorService_GetDoor_CaseInsensitive()
    {
        var door = new Door { Id = "MyDoor", Name = "Test" };

        var service = new DoorService(new List<Door> { door }, new List<GameObject>());

        var found = service.GetDoor("mydoor");

        Assert.NotNull(found);
        Assert.Equal("MyDoor", found!.Id);
    }
}
