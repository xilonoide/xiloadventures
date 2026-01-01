using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando no se encuentra un objeto.
    /// </summary>
    public static string ObjectNotFound => Pick(
        "No ves eso por aquí.",
        "Aquí no hay nada así.",
        "¿De qué hablas? No veo eso.",
        "No encuentras nada con ese nombre.",
        "Miras a tu alrededor pero no ves eso.",
        "Eso no parece estar aquí.",
        "No hay rastro de eso por ningún lado.",
        "No existe tal cosa en este lugar.",
        "Por más que buscas, no lo encuentras.",
        "Aquí no hay nada parecido.",
        "No ves nada de eso por los alrededores.",
        "Eso no está aquí, que tú sepas."
    );

    /// <summary>
    /// Mensaje cuando no se ve a una persona.
    /// </summary>
    public static string PersonNotFound => Pick(
        "No ves a esa persona aquí.",
        "Aquí no hay nadie con ese nombre.",
        "¿De quién hablas? No veo a nadie así.",
        "No hay nadie llamado así por aquí.",
        "Miras a tu alrededor pero no ves a esa persona.",
        "Esa persona no parece estar aquí.",
        "No encuentras a nadie con esa descripción.",
        "No hay rastro de esa persona.",
        "Aquí no hay nadie así.",
        "¿Quién? No veo a nadie con ese nombre."
    );
}
