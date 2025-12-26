using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de ejecutar un script manualmente desde el editor.
/// Contiene información sobre el éxito de la ejecución y los mensajes generados.
/// </summary>
/// <remarks>
/// Esta clase se usa principalmente para el modo de prueba del editor de scripts,
/// permitiendo al diseñador ver qué haría el script sin ejecutarlo en el juego real.
/// </remarks>
public class ScriptExecutionResult
{
    /// <summary>
    /// Indica si el script se ejecutó sin errores.
    /// False si hubo algún error durante la ejecución.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Lista de mensajes generados durante la ejecución.
    /// Incluye mensajes de ShowMessage, logs de debug, y otros outputs.
    /// </summary>
    public List<string> Messages { get; set; } = new();

    /// <summary>
    /// Mensaje de error si la ejecución falló.
    /// Null si Success es true.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Crea un resultado exitoso con mensajes opcionales.
    /// </summary>
    /// <param name="messages">Lista de mensajes generados, o null para lista vacía.</param>
    /// <returns>Resultado con Success = true.</returns>
    public static ScriptExecutionResult Ok(List<string>? messages = null) =>
        new() { Success = true, Messages = messages ?? new() };

    /// <summary>
    /// Crea un resultado de error con el mensaje especificado.
    /// </summary>
    /// <param name="message">Descripción del error ocurrido.</param>
    /// <returns>Resultado con Success = false y ErrorMessage establecido.</returns>
    public static ScriptExecutionResult Error(string message) =>
        new() { Success = false, ErrorMessage = message };
}
