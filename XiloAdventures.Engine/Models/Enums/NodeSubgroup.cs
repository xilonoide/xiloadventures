namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Subgrupos de nodos en el editor de scripts.
/// Permite organizar los nodos dentro de cada categoría.
/// </summary>
public enum NodeSubgroup
{
    /// <summary>Sin subgrupo específico.</summary>
    None,
    /// <summary>Nodos relacionados con el jugador.</summary>
    Jugador,
    /// <summary>Nodos relacionados con el juego (misiones, clima, tiempo).</summary>
    Juego,
    /// <summary>Operadores y comparadores.</summary>
    Operadores,
    /// <summary>Nodos de necesidades básicas (hambre, sed, sueño).</summary>
    Necesidades,
    /// <summary>Nodos relacionados con dinero y comercio.</summary>
    Dinero,
    /// <summary>Nodos de combate.</summary>
    Combate,
    /// <summary>Nodos de iluminación.</summary>
    Iluminacion,
    /// <summary>Nodos de objetos.</summary>
    Objetos,
    /// <summary>Nodos de NPCs.</summary>
    NPC,
    /// <summary>Nodos de rutas y patrullaje de NPCs.</summary>
    Rutas,
    /// <summary>Nodos de salas.</summary>
    Salas,
    /// <summary>Nodos de puertas.</summary>
    Puertas
}

/// <summary>
/// Extensiones para NodeSubgroup.
/// </summary>
public static class NodeSubgroupExtensions
{
    /// <summary>
    /// Obtiene el icono emoji para el subgrupo.
    /// </summary>
    public static string GetIcon(this NodeSubgroup subgroup) => subgroup switch
    {
        NodeSubgroup.Jugador => "👤",
        NodeSubgroup.Juego => "🎮",
        NodeSubgroup.Operadores => "🔧",
        NodeSubgroup.Necesidades => "🍖",
        NodeSubgroup.Dinero => "💰",
        NodeSubgroup.Combate => "⚔️",
        NodeSubgroup.Iluminacion => "💡",
        NodeSubgroup.Objetos => "📦",
        NodeSubgroup.NPC => "🧑",
        NodeSubgroup.Rutas => "🛤️",
        NodeSubgroup.Salas => "🏠",
        NodeSubgroup.Puertas => "🚪",
        _ => "📁"
    };

    /// <summary>
    /// Obtiene el nombre de visualización para el subgrupo.
    /// </summary>
    public static string GetDisplayName(this NodeSubgroup subgroup) => subgroup switch
    {
        NodeSubgroup.Jugador => "Jugador",
        NodeSubgroup.Juego => "Juego",
        NodeSubgroup.Operadores => "Operadores",
        NodeSubgroup.Necesidades => "Necesidades",
        NodeSubgroup.Dinero => "Dinero",
        NodeSubgroup.Combate => "Combate",
        NodeSubgroup.Iluminacion => "Iluminación",
        NodeSubgroup.Objetos => "Objetos",
        NodeSubgroup.NPC => "NPC",
        NodeSubgroup.Rutas => "Rutas",
        NodeSubgroup.Salas => "Salas",
        NodeSubgroup.Puertas => "Puertas",
        _ => ""
    };
}
