namespace XiloAdventures.Engine.Models;

/// <summary>
/// Mensaje de diálogo para mostrar en la interfaz de usuario.
/// Representa una línea de texto dicha por un NPC o por el jugador.
/// </summary>
public class ConversationMessage
{
    /// <summary>
    /// Texto del diálogo a mostrar.
    /// Puede incluir variables interpoladas que se resuelven en tiempo de ejecución.
    /// </summary>
    public string Text { get; set; } = "";

    /// <summary>
    /// Nombre del hablante para mostrar en la UI.
    /// Para NPCs es su nombre, para el jugador puede ser "Jugador" o el nombre del personaje.
    /// </summary>
    public string SpeakerName { get; set; } = "";

    /// <summary>
    /// Emoción del hablante para determinar el retrato o animación.
    /// Valores típicos: "Neutral", "Feliz", "Triste", "Enfadado", "Sorprendido".
    /// </summary>
    public string Emotion { get; set; } = "Neutral";

    /// <summary>
    /// Indica si el mensaje es del NPC (true) o del jugador (false).
    /// Determina el estilo visual del mensaje en la UI.
    /// </summary>
    public bool IsNpc { get; set; }
}
