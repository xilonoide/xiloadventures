using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando el jugador espera.
    /// </summary>
    public static string WaitMessage => Pick(
        "Esperas un momento...",
        "El tiempo pasa lentamente...",
        "Aguardas pacientemente...",
        "Dejas pasar el tiempo...",
        "Te quedas quieto un instante...",
        "Contemplas el entorno mientras esperas...",
        "Un momento de calma...",
        "Respiras hondo y esperas...",
        "El silencio te acompaña mientras esperas...",
        "Haces una breve pausa...",
        "Te tomas un momento de descanso...",
        "El tiempo transcurre..."
    );
}
