using System.ComponentModel;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Asset de música embebido en el mundo del juego.
/// Permite almacenar archivos de música directamente en el archivo del mundo
/// para distribución sin dependencias externas.
/// </summary>
public class MusicAsset
{
    /// <summary>
    /// Identificador único del asset de música.
    /// Típicamente el nombre del archivo original (ej: "theme.mp3").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Contenido del archivo de música codificado en Base64.
    /// Oculto en el editor de propiedades por su tamaño.
    /// </summary>
    [Browsable(false)]
    public string Base64 { get; set; } = string.Empty;

    /// <summary>
    /// Tamaño del archivo original en bytes.
    /// Usado para mostrar información y gestionar recursos.
    /// </summary>
    public long SizeBytes { get; set; }

    /// <summary>
    /// Duración del archivo de música en segundos.
    /// Permite mostrar la duración sin decodificar el archivo.
    /// </summary>
    public double DurationSeconds { get; set; }
}
