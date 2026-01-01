using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de validación de un script.
/// Contiene información sobre errores, advertencias y el estado de validez del script.
/// </summary>
/// <remarks>
/// Un script válido debe cumplir:
/// - Tener al menos un nodo de evento (para disparar la ejecución)
/// - Tener al menos una acción (para producir efectos)
/// - El evento debe estar conectado a la acción mediante conexiones de ejecución
/// - Todas las propiedades obligatorias de los nodos deben estar completas
/// </remarks>
public class ScriptValidationResult
{
    /// <summary>
    /// Indica si el script es válido para ejecución.
    /// True solo si no hay errores.
    /// </summary>
    public bool IsValid => !HasErrors;

    /// <summary>
    /// Indica si hay errores que impiden la ejecución del script.
    /// </summary>
    public bool HasErrors => !HasEvent || !HasAction || !IsConnected || IncompleteNodes.Count > 0;

    /// <summary>
    /// Indica si hay advertencias (actualmente equivale a HasErrors).
    /// </summary>
    public bool HasWarnings => HasErrors;

    /// <summary>
    /// Indica si el script contiene al menos un nodo de evento.
    /// Sin eventos, el script nunca se ejecutará.
    /// </summary>
    public bool HasEvent { get; set; }

    /// <summary>
    /// Indica si el script contiene al menos una acción.
    /// Sin acciones, el script no producirá ningún efecto.
    /// </summary>
    public bool HasAction { get; set; }

    /// <summary>
    /// Indica si hay una ruta de ejecución desde un evento hasta una acción.
    /// El flujo debe poder llegar del evento a la acción siguiendo las conexiones.
    /// </summary>
    public bool IsConnected { get; set; }

    /// <summary>
    /// Lista de nodos que tienen propiedades obligatorias sin completar.
    /// Cada entrada contiene información sobre qué propiedades faltan.
    /// </summary>
    public List<IncompleteNodeInfo> IncompleteNodes { get; } = new();

    /// <summary>
    /// Lista de mensajes de error detallados.
    /// Cada error describe un problema que impide la ejecución del script.
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// Lista de mensajes de advertencia.
    /// Las advertencias indican problemas potenciales pero no impiden la ejecución.
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// Crea un resultado de validación vacío para scripts sin nodos.
    /// </summary>
    public static ScriptValidationResult Empty => new()
    {
        HasEvent = false,
        HasAction = false,
        IsConnected = false
    };
}
