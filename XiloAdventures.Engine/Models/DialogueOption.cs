namespace XiloAdventures.Engine.Models;

/// <summary>
/// Opción de diálogo seleccionable por el jugador.
/// Representa una de las respuestas posibles en un nodo PlayerChoice.
/// </summary>
public class DialogueOption
{
    /// <summary>
    /// Índice de la opción (0-based).
    /// Usado para identificar la opción cuando el jugador la selecciona.
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Texto de la opción a mostrar al jugador.
    /// Puede ser una pregunta, afirmación o cualquier respuesta.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Indica si la opción está disponible para seleccionar.
    /// Puede deshabilitarse por condiciones no cumplidas.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Razón por la que la opción está deshabilitada.
    /// Se muestra como tooltip cuando IsEnabled es false.
    /// Ejemplo: "Requiere tener el objeto Llave Maestra".
    /// </summary>
    public string? DisabledReason { get; set; }

    /// <summary>
    /// Nombre del puerto de salida asociado en el nodo PlayerChoice.
    /// Determina qué conexión seguir cuando se elige esta opción.
    /// Valores típicos: "Option1", "Option2", "Option3", "Option4".
    /// </summary>
    public string OutputPort { get; set; } = "";
}
