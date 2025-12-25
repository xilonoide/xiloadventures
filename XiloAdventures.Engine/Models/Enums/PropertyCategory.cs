namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Categorías de propiedades en el editor de propiedades.
/// </summary>
public enum PropertyCategory
{
    /// <summary>Propiedades de identificación (Id, Nombre, etc.).</summary>
    Identificacion,
    /// <summary>Propiedades de descripción y textos.</summary>
    Descripcion,
    /// <summary>Propiedades de sistemas del juego (combate, necesidades, etc.).</summary>
    Sistemas,
    /// <summary>Propiedades multimedia (imágenes, música, etc.).</summary>
    Multimedia,
    /// <summary>Propiedades de comportamiento del objeto.</summary>
    Comportamiento,
    /// <summary>Propiedades de combate (armas, armaduras).</summary>
    Combate,
    /// <summary>Propiedades estadísticas (vida, dinero, etc.).</summary>
    Estadisticas,
    /// <summary>Estadísticas de combate de NPCs.</summary>
    EstadisticasCombate,
    /// <summary>Características del jugador (fuerza, destreza, etc.).</summary>
    Caracteristicas,
    /// <summary>Propiedades de seguridad (encriptación).</summary>
    Seguridad,
    /// <summary>Propiedades de fabricación.</summary>
    Fabricacion,
    /// <summary>Otras propiedades no categorizadas.</summary>
    Otros
}

/// <summary>
/// Extensiones para PropertyCategory.
/// </summary>
public static class PropertyCategoryExtensions
{
    /// <summary>
    /// Obtiene el texto de visualización con emoji para la categoría.
    /// </summary>
    public static string ToDisplayString(this PropertyCategory category) => category switch
    {
        PropertyCategory.Identificacion => "🔖 Identificación",
        PropertyCategory.Descripcion => "📝 Descripción",
        PropertyCategory.Sistemas => "🎮 Sistemas",
        PropertyCategory.Multimedia => "🎵 Multimedia",
        PropertyCategory.Comportamiento => "⚙️ Comportamiento",
        PropertyCategory.Combate => "⚔️ Combate",
        PropertyCategory.Estadisticas => "📊 Estadísticas",
        PropertyCategory.EstadisticasCombate => "📊 Estadísticas de combate",
        PropertyCategory.Caracteristicas => "⚔️ Características",
        PropertyCategory.Seguridad => "🔒 Seguridad",
        PropertyCategory.Fabricacion => "🔧 Fabricación",
        PropertyCategory.Otros => "🏷️ Otros",
        _ => "🏷️ Otros"
    };
}
