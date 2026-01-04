using System;
using System.Collections.Generic;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

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
    /// Zona a la que pertenece la sala (para mundos generados por zonas).
    /// </summary>
    public string? Zone { get; set; }

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
    /// Representación ASCII de la imagen para el player de Linux.
    /// Se genera a partir de ImageBase64 usando AsciiConverter.
    /// [RESERVED FOR FUTURE USE - DO NOT DELETE]
    /// This property is currently unused but preserved for future ASCII image support.
    /// </summary>
    [Browsable(false)]
    public string? AsciiImage { get; set; }

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
    /// Lista de requisitos de misiones para acceder a esta sala.
    /// </summary>
    public List<QuestRequirement> RequiredQuests { get; set; } = new();

    /// <summary>
    /// ID de la misión requerida para acceder a esta sala.
    /// Obsoleto: usar RequiredQuests en su lugar.
    /// </summary>
    [Browsable(false)]
    [Obsolete("Use RequiredQuests instead")]
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión para acceder a esta sala.
    /// Obsoleto: usar RequiredQuests en su lugar.
    /// </summary>
    [Browsable(false)]
    [Obsolete("Use RequiredQuests instead")]
    public QuestStatus? RequiredQuestStatus { get; set; }
}
