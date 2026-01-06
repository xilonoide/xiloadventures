using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando no se puede ir en una dirección.
    /// </summary>
    public static string CannotGoThatWay => Pick(
        "No puedes ir en esa dirección.",
        "No hay salida por ahí.",
        "Esa dirección está bloqueada.",
        "No puedes avanzar hacia allí.",
        "No hay camino en esa dirección.",
        "Algo te impide ir por ahí.",
        "Ese camino no existe.",
        "No hay forma de ir en esa dirección.",
        "Por ahí no se puede pasar.",
        "Esa ruta está cerrada.",
        "No hay ninguna salida en esa dirección.",
        "No puedes ir por ahí."
    );

    /// <summary>
    /// Mensaje cuando el jugador está perdido.
    /// </summary>
    public static string PlayerLost => Pick(
        "Estás perdido.",
        "No sabes dónde estás.",
        "Te encuentras completamente desorientado.",
        "Has perdido la noción de tu ubicación.",
        "No reconoces este lugar.",
        "Estás desubicado.",
        "No tienes ni idea de dónde te encuentras.",
        "Te has perdido por completo.",
        "La confusión te invade, estás perdido.",
        "No logras orientarte."
    );

    /// <summary>
    /// Mensaje cuando la puerta está cerrada (sin llave).
    /// </summary>
    public static string DoorIsClosed => Pick(
        "La puerta está cerrada.",
        "Esta puerta está cerrada.",
        "La puerta no está abierta.",
        "Hay una puerta cerrada bloqueando el paso.",
        "Tendrás que abrir la puerta primero.",
        "La puerta está cerrada, deberías abrirla.",
        "No puedes pasar, la puerta está cerrada.",
        "Primero tendrás que abrir la puerta.",
        "La puerta está cerrada. Intenta abrirla.",
        "Hay una puerta cerrada en ese camino."
    );

    /// <summary>
    /// Mensaje cuando la puerta está cerrada con llave.
    /// </summary>
    public static string DoorIsLocked => Pick(
        "La puerta está cerrada con llave.",
        "Esta puerta está echada con llave.",
        "La cerradura impide que abras la puerta.",
        "La puerta no cede, está cerrada con llave.",
        "Necesitas una llave para abrir esta puerta.",
        "La puerta está firmemente cerrada con llave.",
        "El cerrojo mantiene la puerta bloqueada.",
        "Esta puerta requiere una llave.",
        "La puerta está atrancada con llave.",
        "No puedes abrirla, está cerrada con llave."
    );

    /// <summary>
    /// Mensaje cuando la salida está bloqueada.
    /// </summary>
    public static string ExitBlocked => Pick(
        "La salida está bloqueada.",
        "Algo bloquea el paso.",
        "No puedes pasar, hay algo en medio.",
        "El camino está obstruido.",
        "Hay un obstáculo que te impide avanzar.",
        "La salida está taponada.",
        "No puedes cruzar, algo lo impide.",
        "El paso está cerrado.",
        "Hay algo que bloquea la salida.",
        "No hay forma de pasar por ahí."
    );
}
