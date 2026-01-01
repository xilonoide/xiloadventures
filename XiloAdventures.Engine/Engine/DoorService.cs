using System;
using System.Collections.Generic;
using System.Linq;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Lógica de alto nivel para trabajar con puertas y llaves.
/// Solo depende de listas de Door / GameObject, no del resto del motor.
/// </summary>
public class DoorService
{
    private readonly IList<Door> _doors;
    private readonly IList<GameObject> _objects;

    /// <summary>
    /// Crea un DoorService a partir de las colecciones de puertas y objetos
    /// (normalmente almacenadas en tu GameState).
    /// </summary>
    public DoorService(IList<Door> doors, IList<GameObject> objects)
    {
        _doors = doors;
        _objects = objects;
    }

    /// <summary>Devuelve una puerta por id, o null si no existe.</summary>
    public Door? GetDoor(string doorId)
        => _doors.FirstOrDefault(d => d.Id.Equals(doorId, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Comprueba si desde una sala concreta se puede accionar (abrir/cerrar)
    /// la puerta, teniendo en cuenta la restricción de lado.
    /// </summary>
    public bool CanOperateFromRoom(Door door, string currentRoomId)
    {
        if (door.RoomIdA == null || door.RoomIdB == null)
            return false;

        var isFromA = door.RoomIdA.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase);
        var isFromB = door.RoomIdB.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase);

        return door.OpenFromSide switch
        {
            DoorOpenSide.Both => isFromA || isFromB,
            DoorOpenSide.FromAOnly => isFromA,
            DoorOpenSide.FromBOnly => isFromB,
            _ => false
        };
    }

    /// <summary>
    /// Devuelve true si la puerta no requiere llave, o si el jugador
    /// dispone del objeto llave requerido (KeyObjectId).
    /// availableObjectIds son los ids de los objetos que el jugador
    /// tiene disponibles (inventario, objetos de la sala, etc.).
    /// </summary>
    public bool HasRequiredKey(Door door, IEnumerable<string> availableObjectIds)
    {
        if (!door.IsLocked || string.IsNullOrWhiteSpace(door.KeyObjectId))
            return true; // no requiere llave

        var availableIds = new HashSet<string>(availableObjectIds ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        if (availableIds.Count == 0)
            return false;

        // Verificar si el jugador tiene el objeto llave específico
        return availableIds.Contains(door.KeyObjectId);
    }

    /// <summary>
    /// Intenta abrir una puerta concreta desde una sala, con una colección
    /// de ids de objetos disponibles (inventario del jugador, objetos de la sala, etc.).
    /// </summary>
    public DoorOperationResult TryOpenDoor(string doorId, string currentRoomId, IEnumerable<string> availableObjectIds)
    {
        var door = GetDoor(doorId);
        if (door == null)
            return DoorOperationResult.NotFound();

        if (!CanOperateFromRoom(door, currentRoomId))
            return DoorOperationResult.CannotOperateFromThisSide(door);

        if (!HasRequiredKey(door, availableObjectIds))
            return DoorOperationResult.RequiresKey(door);

        if (door.IsOpen)
            return DoorOperationResult.AlreadyOpen(door);

        door.IsOpen = true;
        return DoorOperationResult.Opened(door);
    }

    /// <summary>Intenta cerrar la puerta (si está abierta).</summary>
    public DoorOperationResult TryCloseDoor(string doorId, string currentRoomId, IEnumerable<string> availableObjectIds)
    {
        var door = GetDoor(doorId);
        if (door == null)
            return DoorOperationResult.NotFound();

        if (!CanOperateFromRoom(door, currentRoomId))
            return DoorOperationResult.CannotOperateFromThisSide(door);

        if (!HasRequiredKey(door, availableObjectIds))
            return DoorOperationResult.RequiresKey(door);

        if (!door.IsOpen)
            return DoorOperationResult.AlreadyClosed(door);

        door.IsOpen = false;
        return DoorOperationResult.Closed(door);
    }
}

/// <summary>
/// Resultado de una operación de abrir/cerrar puerta. Lleva flags y mensajes
/// que puedes mapear fácilmente a textos de narración.
/// </summary>
public class DoorOperationResult
{
    public bool Success { get; private set; }
    public bool NotFoundDoor { get; private set; }
    public bool WrongSide { get; private set; }
    public bool MissingKey { get; private set; }
    public bool AlreadyInDesiredState { get; private set; }
    public Door? Door { get; private set; }

    /// <summary>
    /// Clave simbólica para que tu sistema de mensajes la convierta
    /// en un texto real (por ejemplo usando un diccionario).
    /// </summary>
    public string? MessageKey { get; private set; }

    private DoorOperationResult() { }

    public static DoorOperationResult NotFound()
        => new()
        {
            Success = false,
            NotFoundDoor = true,
            MessageKey = "door_not_found"
        };

    public static DoorOperationResult CannotOperateFromThisSide(Door door)
        => new()
        {
            Success = false,
            WrongSide = true,
            Door = door,
            MessageKey = "door_wrong_side"
        };

    public static DoorOperationResult RequiresKey(Door door)
        => new()
        {
            Success = false,
            MissingKey = true,
            Door = door,
            MessageKey = "door_requires_key"
        };

    public static DoorOperationResult AlreadyOpen(Door door)
        => new()
        {
            Success = false,
            AlreadyInDesiredState = true,
            Door = door,
            MessageKey = "door_already_open"
        };

    public static DoorOperationResult AlreadyClosed(Door door)
        => new()
        {
            Success = false,
            AlreadyInDesiredState = true,
            Door = door,
            MessageKey = "door_already_closed"
        };

    public static DoorOperationResult Opened(Door door)
        => new()
        {
            Success = true,
            Door = door,
            MessageKey = "door_opened"
        };

    public static DoorOperationResult Closed(Door door)
        => new()
        {
            Success = true,
            Door = door,
            MessageKey = "door_closed"
        };
}
