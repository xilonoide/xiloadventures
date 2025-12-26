using System;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Entrada en el registro de combate.
/// Representa un mensaje individual en el historial de combate
/// que se muestra en la interfaz de usuario.
/// </summary>
public class CombatLogEntry
{
    /// <summary>
    /// Mensaje a mostrar en el log de combate.
    /// Puede incluir información sobre la acción realizada, daño causado, etc.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Indica quién realizó la acción.
    /// True: la acción la realizó el jugador.
    /// False: la acción la realizó el NPC enemigo.
    /// </summary>
    public bool IsPlayerAction { get; set; }

    /// <summary>
    /// Momento en que ocurrió la acción.
    /// Usado para ordenar y mostrar el tiempo transcurrido.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// Tipo de entrada para determinar el formateo visual.
    /// Permite colorear o destacar diferentes tipos de mensajes.
    /// </summary>
    public CombatLogType LogType { get; set; } = CombatLogType.Normal;
}
