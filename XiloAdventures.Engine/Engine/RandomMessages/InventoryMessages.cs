using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando el inventario está vacío.
    /// </summary>
    public static string InventoryEmpty => Pick(
        "No llevas nada.",
        "Tu inventario está vacío.",
        "No cargas nada encima.",
        "Tus manos están vacías.",
        "No tienes nada en tu posesión.",
        "No llevas nada consigo.",
        "Tu bolsa está vacía.",
        "No portas ningún objeto.",
        "Vas con las manos vacías.",
        "No tienes nada."
    );

    /// <summary>
    /// Mensaje cuando no llevas un objeto.
    /// </summary>
    public static string NotCarryingThat => Pick(
        "No llevas eso.",
        "Eso no está en tu inventario.",
        "No tienes eso contigo.",
        "No llevas nada así.",
        "Eso no lo tienes encima.",
        "No cargas con eso.",
        "Ese objeto no está en tu poder.",
        "No tienes eso en tu posesión.",
        "No llevas nada parecido.",
        "Eso no forma parte de tu equipaje."
    );

    /// <summary>
    /// Mensaje cuando no puedes coger algo.
    /// </summary>
    public static string CannotTakeThat => Pick(
        "No puedes coger eso.",
        "Eso no se puede coger.",
        "Es imposible llevarte eso.",
        "No puedes tomar eso.",
        "Eso no puede cogerse.",
        "No hay forma de llevarte eso.",
        "Eso no es algo que puedas coger.",
        "No puedes cargar con eso.",
        "Eso está fuera de tu alcance.",
        "No puedes apropiarte de eso."
    );

    /// <summary>
    /// Mensaje cuando no hay nada que coger.
    /// </summary>
    public static string NothingToTake => Pick(
        "No hay nada que puedas coger.",
        "Aquí no hay nada interesante que llevarte.",
        "No ves nada que merezca la pena coger.",
        "No hay objetos que puedas tomar.",
        "Aquí no hay nada útil para ti.",
        "No encuentras nada que llevarte.",
        "No hay nada disponible para coger.",
        "Miras pero no hay nada que coger.",
        "Aquí no hay nada que te interese.",
        "No ves nada que puedas llevarte."
    );
}
