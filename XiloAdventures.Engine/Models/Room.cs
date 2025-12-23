using System.Collections.Generic;
using System.ComponentModel;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa una sala o localización en el mundo del juego.
/// </summary>
public class Room
{
    /// <summary>
    /// Identificador único de la sala.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la sala que se muestra al jugador.
    /// </summary>
    public string Name { get; set; } = "Sala sin nombre";

    /// <summary>
    /// Descripción detallada de la sala.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// ID de la imagen asociada a la sala (obsoleto, usar ImageBase64).
    /// </summary>
    public string? ImageId { get; set; }

    /// <summary>
    /// Contenido de la imagen de la sala en Base64 (se guarda dentro del JSON del mundo).
    /// Si es null o vacío, no se mostrará imagen.
    /// </summary>
    [Browsable(false)]
    public string? ImageBase64 { get; set; }

    /// <summary>
    /// ID de la música de fondo de la sala.
    /// </summary>
    public string? MusicId { get; set; }

    /// <summary>
    /// Indica si la sala es un interior (protegida del clima).
    /// </summary>
    public bool IsInterior { get; set; } = false;

    /// <summary>
    /// Indica si la sala está iluminada (afecta visibilidad de noche).
    /// </summary>
    public bool IsIlluminated { get; set; } = true;

    /// <summary>
    /// Lista de salidas disponibles desde esta sala.
    /// </summary>
    public List<Exit> Exits { get; set; } = new();

    /// <summary>
    /// IDs de los objetos presentes en la sala.
    /// </summary>
    [Browsable(false)]
    public List<string> ObjectIds { get; set; } = new();

    /// <summary>
    /// IDs de los NPCs presentes en la sala.
    /// </summary>
    [Browsable(false)]
    public List<string> NpcIds { get; set; } = new();

    /// <summary>
    /// ID de la misión requerida para acceder a esta sala.
    /// </summary>
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión para acceder a esta sala.
    /// </summary>
    public QuestStatus? RequiredQuestStatus { get; set; }

    /// <summary>
    /// Tags arbitrarios para lógica de scripts y eventos.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Representa una salida de una sala hacia otra.
/// </summary>
public class Exit
{
    /// <summary>
    /// Dirección de la salida (norte, sur, este, oeste, arriba, abajo, etc.).
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// ID de la sala destino.
    /// </summary>
    public string TargetRoomId { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la salida está bloqueada.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// ID del objeto (tipo Llave) necesario para abrir esta salida.
    /// </summary>
    public string? KeyObjectId { get; set; }

    /// <summary>
    /// Si esta salida está asociada a una puerta física del mundo, su ID.
    /// Si es null, la salida funciona solo con IsLocked/KeyObjectId.
    /// </summary>
    public string? DoorId { get; set; }

    /// <summary>
    /// ID de la misión requerida para usar esta salida.
    /// </summary>
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión para usar esta salida.
    /// </summary>
    public QuestStatus? RequiredQuestStatus { get; set; }

    /// <summary>
    /// Tags arbitrarios para lógica de scripts y eventos.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
