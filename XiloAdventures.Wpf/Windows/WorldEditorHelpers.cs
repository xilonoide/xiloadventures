using System;
using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Windows;

internal static class WorldEditorHelpers
{
    public static bool DeleteDoor(WorldModel? world, Door? door)
    {
        if (world == null || door == null)
            return false;

        // Quitar referencias a esta puerta desde las salidas
        foreach (var room in world.Rooms)
        {
            foreach (var ex in room.Exits)
            {
                if (string.Equals(ex.DoorId, door.Id, StringComparison.OrdinalIgnoreCase))
                {
                    ex.DoorId = null;
                }
            }
        }

        world.Doors?.Remove(door);

        return true;
    }

    public static bool DeleteObject(WorldModel? world, GameObject? obj)
    {
        if (world == null || obj == null)
            return false;

        world.Objects.Remove(obj);

        // Si el objeto era una llave (Type == Llave), limpiar las puertas y contenedores que lo usan
        if (obj.Type == ObjectType.Llave)
        {
            // Limpiar puertas que usan esta llave
            if (world.Doors != null)
            {
                foreach (var door in world.Doors)
                {
                    if (string.Equals(door.KeyObjectId, obj.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        door.IsLocked = false;
                        door.KeyObjectId = null;
                    }
                }
            }

            // Limpiar contenedores que usan esta llave
            foreach (var container in world.Objects.Where(o => o.IsContainer))
            {
                if (string.Equals(container.KeyId, obj.Id, StringComparison.OrdinalIgnoreCase))
                {
                    container.IsLocked = false;
                    container.KeyId = null;
                }
            }

            // Limpiar salidas que usan esta llave
            foreach (var room in world.Rooms)
            {
                foreach (var exit in room.Exits)
                {
                    if (string.Equals(exit.KeyObjectId, obj.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        exit.IsLocked = false;
                        exit.KeyObjectId = null;
                    }
                }
            }
        }

        // Quitar referencias desde salas y otros objetos / NPCs
        foreach (var room in world.Rooms)
        {
            room.ObjectIds.Remove(obj.Id);
        }

        foreach (var other in world.Objects)
        {
            other.ContainedObjectIds.Remove(obj.Id);
        }

        foreach (var npc in world.Npcs)
        {
            npc.Inventory.RemoveAll(i => i.ObjectId == obj.Id);
        }

        return true;
    }
}
