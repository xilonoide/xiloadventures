namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipos de entidades que pueden ser propietarias de un nodo de script.
/// Usa [Flags] para permitir múltiples tipos.
/// </summary>
[Flags]
public enum NodeOwnerType
{
    /// <summary>Ningún tipo específico.</summary>
    None = 0,
    /// <summary>Nodo disponible para todos los tipos de entidades.</summary>
    All = 1,
    /// <summary>Nodo para el juego global.</summary>
    Game = 2,
    /// <summary>Nodo para salas.</summary>
    Room = 4,
    /// <summary>Nodo para puertas.</summary>
    Door = 8,
    /// <summary>Nodo para NPCs.</summary>
    Npc = 16,
    /// <summary>Nodo para objetos del juego.</summary>
    GameObject = 32,
    /// <summary>Nodo para misiones.</summary>
    Quest = 64
}

/// <summary>
/// Extensiones para NodeOwnerType.
/// </summary>
public static class NodeOwnerTypeExtensions
{
    /// <summary>
    /// Convierte el enum a su representación de string para compatibilidad.
    /// </summary>
    public static string ToOwnerString(this NodeOwnerType ownerType)
    {
        if (ownerType == NodeOwnerType.All) return "*";
        return ownerType.ToString();
    }

    /// <summary>
    /// Verifica si el tipo de propietario coincide con un string dado.
    /// </summary>
    public static bool Matches(this NodeOwnerType ownerTypes, string ownerType)
    {
        if (ownerTypes.HasFlag(NodeOwnerType.All)) return true;

        return ownerType switch
        {
            "*" => true,
            "Game" => ownerTypes.HasFlag(NodeOwnerType.Game),
            "Room" => ownerTypes.HasFlag(NodeOwnerType.Room),
            "Door" => ownerTypes.HasFlag(NodeOwnerType.Door),
            "Npc" => ownerTypes.HasFlag(NodeOwnerType.Npc),
            "GameObject" => ownerTypes.HasFlag(NodeOwnerType.GameObject),
            "Quest" => ownerTypes.HasFlag(NodeOwnerType.Quest),
            _ => false
        };
    }
}
