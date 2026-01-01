using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Manifest para mundos generados por zonas.
/// Define la estructura del mundo y cómo se conectan las zonas entre sí.
/// </summary>
public class ZoneManifest
{
    /// <summary>
    /// Nombre del mundo.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Versión del formato del manifest.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Sala inicial del juego en formato "zona:sala".
    /// </summary>
    public string StartRoom { get; set; } = string.Empty;

    /// <summary>
    /// Lista de nombres de zonas incluidas en el mundo.
    /// </summary>
    public List<string> Zones { get; set; } = new();

    /// <summary>
    /// Conexiones entre zonas (cómo se enlazan las salas de diferentes zonas).
    /// </summary>
    public List<ZoneConnection> Connections { get; set; } = new();
}

/// <summary>
/// Define una conexión entre dos salas de diferentes zonas.
/// </summary>
public class ZoneConnection
{
    /// <summary>
    /// Sala de origen en formato "zona:sala".
    /// </summary>
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// Sala de destino en formato "zona:sala".
    /// </summary>
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// Dirección de la salida desde la sala origen (norte, sur, este, oeste, arriba, abajo).
    /// </summary>
    public string Direction { get; set; } = string.Empty;
}
