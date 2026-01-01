using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando el parser no entiende el comando.
    /// </summary>
    public static string UnknownCommand => Pick(
        "No entiendo ese comando.",
        "¿Qué quieres decir con eso?",
        "No sé cómo hacer eso.",
        "Eso no tiene sentido para mí.",
        "No comprendo lo que quieres hacer.",
        "Intenta expresarlo de otra manera.",
        "¿Perdona? No te he entendido.",
        "Hmm, eso no significa nada para mí.",
        "No sé interpretar eso.",
        "¿Podrías ser más claro?",
        "No reconozco ese comando.",
        "Eso me supera, prueba otra cosa."
    );
}
