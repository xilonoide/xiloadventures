using System.ComponentModel;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Posición de una sala en el mapa del editor.
/// </summary>
public class MapPosition
{
    /// <summary>
    /// Coordenada X en el mapa.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Coordenada Y en el mapa.
    /// </summary>
    public double Y { get; set; }
}

/// <summary>
/// Asset de música embebido en el mundo.
/// </summary>
public class MusicAsset
{
    /// <summary>
    /// Nombre del archivo de música (ej: "theme.mp3").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo de música en Base64.
    /// </summary>
    [Browsable(false)]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Duración del archivo en segundos.
    /// </summary>
    public double DurationSeconds { get; set; }
}

/// <summary>
/// Asset de efecto de sonido embebido en el mundo.
/// </summary>
public class FxAsset
{
    /// <summary>
    /// Nombre del archivo de FX (ej: "explosion.wav").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo de FX en Base64.
    /// </summary>
    [Browsable(false)]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo en bytes.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Duración del archivo en segundos.
    /// </summary>
    public double DurationSeconds { get; set; }
}
