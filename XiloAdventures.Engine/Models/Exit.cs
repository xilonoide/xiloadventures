using System;
using System.Collections.Generic;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa una salida de una sala hacia otra.
/// Las salidas conectan salas en el mundo del juego y pueden tener restricciones
/// de acceso basadas en llaves, misiones u otros requisitos.
/// </summary>
/// <remarks>
/// Las salidas pueden estar asociadas a puertas físicas (Door) o ser simplemente
/// pasajes que se controlan mediante IsLocked/KeyObjectId.
/// </remarks>
public class Exit
{
    /// <summary>
    /// Dirección de la salida (norte, sur, este, oeste, arriba, abajo, etc.).
    /// Esta es la dirección que el jugador usa para moverse.
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// ID de la sala destino.
    /// Debe coincidir con el Id de una Room existente en el mundo.
    /// </summary>
    public string TargetRoomId { get; set; } = string.Empty;

    /// <summary>
    /// Indica si la salida está bloqueada.
    /// Si es true, el jugador no puede pasar sin la llave correspondiente.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// ID del objeto (tipo Llave) necesario para abrir esta salida.
    /// Si es null, la salida no requiere llave (pero puede estar bloqueada).
    /// </summary>
    public string? KeyObjectId { get; set; }

    /// <summary>
    /// Si esta salida está asociada a una puerta física del mundo, su ID.
    /// Las puertas son objetos interactivos que pueden abrirse/cerrarse.
    /// Si es null, la salida funciona solo con IsLocked/KeyObjectId.
    /// </summary>
    public string? DoorId { get; set; }

    /// <summary>
    /// Lista de requisitos de misiones para usar esta salida.
    /// Permite restringir el acceso basándose en el progreso del jugador.
    /// </summary>
    public List<QuestRequirement> RequiredQuests { get; set; } = new();

    /// <summary>
    /// ID de la misión requerida para usar esta salida.
    /// Obsoleto: usar RequiredQuests en su lugar.
    /// </summary>
    [Browsable(false)]
    [Obsolete("Use RequiredQuests instead")]
    public string? RequiredQuestId { get; set; }

    /// <summary>
    /// Estado requerido de la misión para usar esta salida.
    /// Obsoleto: usar RequiredQuests en su lugar.
    /// </summary>
    [Browsable(false)]
    [Obsolete("Use RequiredQuests instead")]
    public QuestStatus? RequiredQuestStatus { get; set; }
}
