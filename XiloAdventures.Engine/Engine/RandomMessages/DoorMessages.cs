using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando la puerta ya está abierta.
    /// </summary>
    public static string DoorAlreadyOpen => Pick(
        "La puerta ya está abierta.",
        "Esa puerta ya se encuentra abierta.",
        "No hace falta, ya está abierta.",
        "La puerta está abierta de par en par.",
        "Ya puedes pasar, está abierta.",
        "Esa puerta no necesita abrirse, ya lo está.",
        "La puerta ya estaba abierta.",
        "Miras la puerta: ya está abierta.",
        "No es necesario, la puerta está abierta.",
        "Ya está abierta, puedes pasar."
    );

    /// <summary>
    /// Mensaje cuando la puerta ya está cerrada.
    /// </summary>
    public static string DoorAlreadyClosed => Pick(
        "La puerta ya está cerrada.",
        "Esa puerta ya se encuentra cerrada.",
        "No hace falta, ya está cerrada.",
        "La puerta está bien cerrada.",
        "Esa puerta no necesita cerrarse, ya lo está.",
        "La puerta ya estaba cerrada.",
        "Miras la puerta: ya está cerrada.",
        "No es necesario, la puerta está cerrada.",
        "Ya está cerrada.",
        "La puerta permanece cerrada."
    );

    /// <summary>
    /// Mensaje cuando no hay puerta en esa dirección.
    /// </summary>
    public static string NoDoorThere => Pick(
        "Aquí no hay ninguna puerta así.",
        "No ves ninguna puerta en esa dirección.",
        "No hay puerta alguna por ahí.",
        "¿Qué puerta? No veo ninguna.",
        "No existe tal puerta aquí.",
        "No hay ninguna puerta con ese nombre.",
        "Miras pero no ves ninguna puerta así.",
        "No hay puertas en esa zona.",
        "Aquí no hay nada parecido a una puerta.",
        "No encuentras ninguna puerta así."
    );

    /// <summary>
    /// Mensaje cuando no hay ninguna puerta.
    /// </summary>
    public static string NoDoorsHere => Pick(
        "Aquí no hay ninguna puerta.",
        "No hay puertas en este lugar.",
        "Este sitio no tiene puertas.",
        "No ves ninguna puerta por aquí.",
        "No hay puertas a la vista.",
        "Este lugar carece de puertas.",
        "No hay ninguna puerta en esta zona.",
        "Miras a tu alrededor: no hay puertas.",
        "Aquí no existe ninguna puerta.",
        "No encuentras ninguna puerta aquí."
    );

    /// <summary>
    /// Mensaje cuando no tienes la llave.
    /// </summary>
    public static string NoKeyForDoor => Pick(
        "No tienes la llave adecuada.",
        "No llevas la llave correcta.",
        "Ninguna de tus llaves encaja.",
        "No posees la llave necesaria.",
        "Te falta la llave apropiada.",
        "No tienes cómo abrir la cerradura.",
        "Necesitas encontrar la llave correcta.",
        "La llave que necesitas no está en tu poder.",
        "No llevas ninguna llave que funcione.",
        "Sin la llave adecuada no podrás."
    );
}
