using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando algo ya está encendido.
    /// </summary>
    public static string AlreadyLit => Pick(
        "{0} ya está encendido.",
        "{0} ya arde con luz propia.",
        "No hace falta, {0} ya está encendido.",
        "{0} ya emite luz.",
        "La llama de {0} ya está viva.",
        "{0} ya ilumina el lugar.",
        "{0} ya está encendido, no es necesario.",
        "{0} ya brilla.",
        "La luz de {0} ya está activa.",
        "{0} ya está prendido."
    );

    /// <summary>
    /// Mensaje cuando algo ya está apagado.
    /// </summary>
    public static string AlreadyOff => Pick(
        "{0} ya está apagado.",
        "{0} no emite ninguna luz.",
        "No hace falta, {0} ya está apagado.",
        "{0} está frío y sin llama.",
        "La luz de {0} ya está extinta.",
        "{0} ya está oscuro.",
        "{0} ya está apagado, no es necesario.",
        "{0} no brilla.",
        "La llama de {0} ya se ha extinguido.",
        "{0} está sin encender."
    );

    /// <summary>
    /// Mensaje cuando algo no se puede encender.
    /// </summary>
    public static string CannotIgnite => Pick(
        "{0} no se puede encender.",
        "Es imposible encender {0}.",
        "{0} no es algo que pueda prenderse.",
        "No hay forma de encender {0}.",
        "{0} no arde.",
        "No puedes encender {0}.",
        "{0} no es inflamable.",
        "Eso no se enciende.",
        "{0} no tiene forma de encenderse.",
        "No es posible prender {0}."
    );

    /// <summary>
    /// Mensaje cuando algo no se puede apagar.
    /// </summary>
    public static string CannotExtinguish => Pick(
        "{0} no se puede apagar.",
        "Es imposible apagar {0}.",
        "{0} no es algo que pueda apagarse.",
        "No hay forma de apagar {0}.",
        "No puedes extinguir {0}.",
        "{0} no puede apagarse así.",
        "Eso no se apaga.",
        "{0} no tiene forma de apagarse.",
        "No es posible extinguir {0}.",
        "Apagar {0} está fuera de tu alcance."
    );

    /// <summary>
    /// Mensaje cuando una luz se apaga por quedarse sin turnos.
    /// </summary>
    public static string LightGoesOut => Pick(
        "{0} se apaga.",
        "La luz de {0} se extingue.",
        "{0} parpadea y se apaga.",
        "La llama de {0} muere.",
        "{0} deja de iluminar.",
        "La luz de {0} se desvanece.",
        "{0} se consume y se apaga.",
        "La llama de {0} se extingue.",
        "{0} pierde su brillo y se apaga.",
        "La luz de {0} expira."
    );
}
