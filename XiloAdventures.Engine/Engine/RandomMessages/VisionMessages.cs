using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando está demasiado oscuro para ver.
    /// </summary>
    public static string TooDarkToSee => Pick(
        "Está demasiado oscuro para ver nada.",
        "La oscuridad es absoluta, no ves nada.",
        "No puedes ver nada en esta penumbra.",
        "Todo está sumido en tinieblas.",
        "La negrura te rodea por completo.",
        "Necesitas luz para poder ver algo.",
        "Es imposible distinguir nada en esta oscuridad.",
        "Tus ojos no logran adaptarse a la oscuridad.",
        "Sin luz, estás completamente a ciegas.",
        "La oscuridad te impide ver lo que hay alrededor.",
        "Todo es negro como la boca de un lobo.",
        "No hay suficiente luz para ver."
    );
}
