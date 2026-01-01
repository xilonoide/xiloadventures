using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando el NPC no tiene nada que decir.
    /// </summary>
    public static string NothingToSay => Pick(
        "{0} no tiene nada que decir.",
        "{0} te ignora.",
        "{0} no parece interesado en hablar.",
        "{0} guarda silencio.",
        "{0} no responde a tus palabras.",
        "{0} te mira pero no dice nada.",
        "{0} no tiene conversación.",
        "{0} permanece en silencio.",
        "{0} no está de humor para hablar.",
        "{0} no tiene nada que contarte."
    );

    /// <summary>
    /// Mensaje cuando terminas una conversación.
    /// </summary>
    public static string EndConversation => Pick(
        "Terminas la conversación.",
        "Das por terminada la charla.",
        "Te despides y acabas la conversación.",
        "La conversación llega a su fin.",
        "Decides dejar de hablar.",
        "Pones fin al diálogo.",
        "Te alejas de la conversación.",
        "Acabas de hablar.",
        "La charla termina.",
        "Finalizas la conversación."
    );

    /// <summary>
    /// Mensaje cuando no hay nadie a quien dar algo.
    /// </summary>
    public static string NoOneToGiveTo => Pick(
        "No hay nadie aquí a quien darle algo.",
        "No ves a nadie a quien entregar eso.",
        "Aquí no hay nadie que pueda recibirlo.",
        "No hay nadie presente para dárselo.",
        "¿A quién se lo das? Aquí no hay nadie.",
        "No hay nadie a la vista para eso.",
        "Miras pero no hay nadie a quien dárselo.",
        "Aquí no hay nadie.",
        "No encuentras a nadie para entregárselo.",
        "No hay persona alguna para recibir eso."
    );
}
