using XiloAdventures.Engine.Models.Enums;
using static XiloAdventures.Engine.Engine.RandomMessageHelper;
using static XiloAdventures.Engine.Engine.GrammarHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando no hay nada especial que ver.
    /// </summary>
    public static string GetNothingSpecial(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
            "No ves nada especial en {0}.",
            "{0} no tiene nada destacable.",
            "No hay nada interesante en {0}.",
            "{0} parece bastante normal.",
            "No observas nada fuera de lo común en {0}.",
            "{0} no tiene nada que llame la atención.",
            "Es solo {0}, nada más.",
            "No hay nada notable en {0}.",
            "{0} no revela ningún secreto.",
            "Examinas {0} pero no ves nada especial."
        );
        return string.Format(template, name);
    }

    // Propiedad legacy para compatibilidad (deprecada)
    [System.Obsolete("Use GetNothingSpecial(name, gender, isPlural) instead")]
    public static string NothingSpecial => Pick(
        "No ves nada especial en {0}.",
        "{0} no tiene nada destacable.",
        "No hay nada interesante en {0}.",
        "{0} parece bastante normal.",
        "No observas nada fuera de lo común en {0}.",
        "{0} no tiene nada que llame la atención.",
        "Es solo {0}, nada más.",
        "No hay nada notable en {0}.",
        "{0} no revela ningún secreto.",
        "Examinas {0} pero no ves nada especial."
    );
}
