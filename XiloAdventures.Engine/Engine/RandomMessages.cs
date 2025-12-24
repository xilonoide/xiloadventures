namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Proporciona variaciones aleatorias de mensajes para evitar repetición.
/// Cada categoría de mensaje tiene múltiples variantes que se seleccionan al azar.
/// </summary>
public static class RandomMessages
{
    private static readonly Random _random = new();

    /// <summary>
    /// Selecciona un mensaje aleatorio de la lista proporcionada.
    /// </summary>
    private static string Pick(params string[] messages) => messages[_random.Next(messages.Length)];

    #region Parser / Command Errors

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

    #endregion

    #region Vision / Darkness

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

    #endregion

    #region Object Not Found

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

    #endregion

    #region Navigation

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
    /// Mensaje cuando la puerta está cerrada con llave.
    /// </summary>
    public static string DoorIsLocked => Pick(
        "La puerta está cerrada con llave.",
        "Esta puerta está echada con llave.",
        "La cerradura impide que abras la puerta.",
        "La puerta no cede, está cerrada con llave.",
        "Necesitas una llave para abrir esta puerta.",
        "La puerta está firmemente cerrada.",
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

    #endregion

    #region Inventory

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

    #endregion

    #region Action Prompts

    /// <summary>
    /// Mensaje preguntando hacia dónde ir.
    /// </summary>
    public static string WhereToGo => Pick(
        "¿Hacia dónde quieres ir?",
        "¿En qué dirección quieres moverte?",
        "¿Adónde te diriges?",
        "¿Por dónde quieres ir?",
        "Especifica una dirección.",
        "¿Qué camino tomas?",
        "¿Hacia dónde?",
        "Indica una dirección.",
        "¿Norte, sur, este, oeste...?",
        "¿Qué rumbo tomas?"
    );

    /// <summary>
    /// Mensaje preguntando qué examinar.
    /// </summary>
    public static string WhatToExamine => Pick(
        "¿Qué quieres examinar?",
        "¿Qué deseas observar más de cerca?",
        "¿Qué quieres mirar?",
        "¿Qué objeto te interesa examinar?",
        "¿Qué quieres inspeccionar?",
        "¿A qué le echas un vistazo?",
        "¿Qué deseas examinar?",
        "Especifica qué quieres mirar.",
        "¿Qué cosa quieres observar?",
        "¿Qué te llama la atención?"
    );

    /// <summary>
    /// Mensaje preguntando qué coger.
    /// </summary>
    public static string WhatToTake => Pick(
        "¿Qué quieres coger?",
        "¿Qué deseas tomar?",
        "¿Qué objeto quieres llevarte?",
        "¿Qué quieres recoger?",
        "Especifica qué quieres coger.",
        "¿Qué cosa quieres tomar?",
        "¿Qué te llevas?",
        "¿Qué deseas coger?",
        "Indica qué quieres coger.",
        "¿Qué objeto quieres coger?"
    );

    /// <summary>
    /// Mensaje preguntando qué soltar.
    /// </summary>
    public static string WhatToDrop => Pick(
        "¿Qué quieres soltar?",
        "¿Qué deseas dejar?",
        "¿Qué objeto quieres soltar?",
        "¿Qué quieres tirar?",
        "Especifica qué quieres dejar.",
        "¿Qué cosa sueltas?",
        "¿Qué dejas en el suelo?",
        "¿Qué deseas soltar?",
        "Indica qué quieres soltar.",
        "¿De qué te desprendes?"
    );

    /// <summary>
    /// Mensaje preguntando qué abrir.
    /// </summary>
    public static string WhatToOpen => Pick(
        "¿Qué quieres abrir?",
        "¿Qué deseas abrir?",
        "Especifica qué quieres abrir.",
        "¿Qué cosa quieres abrir?",
        "Indica qué quieres abrir.",
        "¿Qué abres?",
        "¿Qué objeto quieres abrir?",
        "¿Qué te gustaría abrir?",
        "Abre... ¿qué exactamente?",
        "¿Qué es lo que quieres abrir?"
    );

    /// <summary>
    /// Mensaje preguntando qué cerrar.
    /// </summary>
    public static string WhatToClose => Pick(
        "¿Qué quieres cerrar?",
        "¿Qué deseas cerrar?",
        "Especifica qué quieres cerrar.",
        "¿Qué cosa quieres cerrar?",
        "Indica qué quieres cerrar.",
        "¿Qué cierras?",
        "¿Qué objeto quieres cerrar?",
        "¿Qué te gustaría cerrar?",
        "Cierra... ¿qué exactamente?",
        "¿Qué es lo que quieres cerrar?"
    );

    /// <summary>
    /// Mensaje preguntando qué usar.
    /// </summary>
    public static string WhatToUse => Pick(
        "¿Qué quieres usar?",
        "¿Qué deseas utilizar?",
        "Especifica qué quieres usar.",
        "¿Qué objeto quieres usar?",
        "Indica qué quieres usar.",
        "¿Qué usas?",
        "¿Qué cosa quieres utilizar?",
        "¿Qué te gustaría usar?",
        "Usa... ¿qué exactamente?",
        "¿Qué es lo que quieres usar?"
    );

    /// <summary>
    /// Mensaje preguntando qué dar.
    /// </summary>
    public static string WhatToGive => Pick(
        "¿Qué quieres dar?",
        "¿Qué deseas entregar?",
        "Especifica qué quieres dar.",
        "¿Qué objeto quieres dar?",
        "Indica qué quieres dar.",
        "¿Qué das?",
        "¿Qué cosa quieres entregar?",
        "¿Qué te gustaría dar?",
        "Da... ¿qué exactamente?",
        "¿Qué es lo que quieres dar?"
    );

    /// <summary>
    /// Mensaje preguntando a quién dar algo.
    /// </summary>
    public static string WhoToGiveTo => Pick(
        "¿A quién quieres dárselo?",
        "¿A quién se lo das?",
        "Especifica a quién quieres dárselo.",
        "¿A quién le entregas eso?",
        "¿Quién lo recibe?",
        "¿A quién?",
        "¿Para quién es?",
        "¿A quién se lo ofreces?",
        "Indica a quién se lo das.",
        "¿Quién es el destinatario?"
    );

    /// <summary>
    /// Mensaje preguntando qué leer.
    /// </summary>
    public static string WhatToRead => Pick(
        "¿Qué quieres leer?",
        "¿Qué deseas leer?",
        "Especifica qué quieres leer.",
        "¿Qué objeto quieres leer?",
        "Indica qué quieres leer.",
        "¿Qué lees?",
        "¿Qué texto quieres leer?",
        "¿Qué te gustaría leer?",
        "Lee... ¿qué exactamente?",
        "¿Qué es lo que quieres leer?"
    );

    /// <summary>
    /// Mensaje preguntando con quién hablar.
    /// </summary>
    public static string WhoToTalkTo => Pick(
        "¿Con quién quieres hablar?",
        "¿A quién te diriges?",
        "Especifica con quién quieres hablar.",
        "¿Con quién deseas conversar?",
        "¿A quién le hablas?",
        "¿Con quién?",
        "¿Quién es tu interlocutor?",
        "¿Con quién inicias conversación?",
        "Indica con quién quieres hablar.",
        "¿A quién te gustaría hablarle?"
    );

    /// <summary>
    /// Mensaje preguntando qué meter.
    /// </summary>
    public static string WhatToPutIn => Pick(
        "¿Qué quieres meter?",
        "¿Qué deseas introducir?",
        "Especifica qué quieres meter.",
        "¿Qué objeto quieres meter?",
        "Indica qué quieres meter.",
        "¿Qué metes?",
        "¿Qué cosa quieres introducir?",
        "Mete... ¿qué exactamente?",
        "¿Qué es lo que quieres meter?",
        "¿Qué quieres guardar ahí?"
    );

    /// <summary>
    /// Mensaje preguntando dónde meter algo.
    /// </summary>
    public static string WhereToPutIt => Pick(
        "¿Dónde quieres meterlo?",
        "¿En qué lo metes?",
        "Especifica dónde quieres meterlo.",
        "¿En qué contenedor?",
        "¿Dónde lo guardas?",
        "¿En qué?",
        "¿Dónde lo introduces?",
        "Indica dónde quieres meterlo.",
        "¿En qué sitio lo colocas?",
        "¿Dónde lo pones?"
    );

    /// <summary>
    /// Mensaje preguntando qué sacar.
    /// </summary>
    public static string WhatToGetFrom => Pick(
        "¿Qué quieres sacar?",
        "¿Qué deseas extraer?",
        "Especifica qué quieres sacar.",
        "¿Qué objeto quieres sacar?",
        "Indica qué quieres sacar.",
        "¿Qué sacas?",
        "¿Qué cosa quieres extraer?",
        "Saca... ¿qué exactamente?",
        "¿Qué es lo que quieres sacar?",
        "¿Qué coges de ahí?"
    );

    /// <summary>
    /// Mensaje preguntando de dónde sacar algo.
    /// </summary>
    public static string WhereToGetFrom => Pick(
        "¿De dónde quieres sacarlo?",
        "¿De qué lo sacas?",
        "Especifica de dónde quieres sacarlo.",
        "¿De qué contenedor?",
        "¿De dónde lo extraes?",
        "¿De qué?",
        "¿De dónde lo coges?",
        "Indica de dónde quieres sacarlo.",
        "¿De qué sitio lo sacas?",
        "¿De dónde lo tomas?"
    );

    /// <summary>
    /// Mensaje preguntando qué mirar dentro.
    /// </summary>
    public static string WhatToLookIn => Pick(
        "¿Dentro de qué quieres ver?",
        "¿Qué contenedor quieres examinar?",
        "Especifica dentro de qué quieres mirar.",
        "¿En qué miras?",
        "¿Qué abres para ver dentro?",
        "¿Dentro de qué?",
        "¿Qué contenedor inspeccionas?",
        "Indica qué quieres abrir para ver.",
        "¿Qué examinas por dentro?",
        "¿En qué te asomas?"
    );

    /// <summary>
    /// Mensaje preguntando qué desbloquear.
    /// </summary>
    public static string WhatToUnlock => Pick(
        "¿Qué quieres desbloquear?",
        "¿Qué deseas abrir con llave?",
        "Especifica qué quieres desbloquear.",
        "¿Qué cerradura quieres abrir?",
        "Indica qué quieres desbloquear.",
        "¿Qué desbloqueas?",
        "¿Qué cosa quieres abrir?",
        "Desbloquea... ¿qué exactamente?",
        "¿Qué es lo que quieres desbloquear?",
        "¿Qué abres con la llave?"
    );

    /// <summary>
    /// Mensaje preguntando qué bloquear.
    /// </summary>
    public static string WhatToLock => Pick(
        "¿Qué quieres bloquear?",
        "¿Qué deseas cerrar con llave?",
        "Especifica qué quieres bloquear.",
        "¿Qué cerradura quieres cerrar?",
        "Indica qué quieres bloquear.",
        "¿Qué bloqueas?",
        "¿Qué cosa quieres cerrar?",
        "Bloquea... ¿qué exactamente?",
        "¿Qué es lo que quieres bloquear?",
        "¿Qué cierras con llave?"
    );

    /// <summary>
    /// Mensaje preguntando qué encender.
    /// </summary>
    public static string WhatToIgnite => Pick(
        "¿Qué quieres encender?",
        "¿Qué deseas prender?",
        "Especifica qué quieres encender.",
        "¿Qué objeto quieres encender?",
        "Indica qué quieres encender.",
        "¿Qué enciendes?",
        "¿Qué cosa quieres prender?",
        "Enciende... ¿qué exactamente?",
        "¿Qué es lo que quieres encender?",
        "¿Qué prendes fuego?"
    );

    /// <summary>
    /// Mensaje preguntando qué apagar.
    /// </summary>
    public static string WhatToExtinguish => Pick(
        "¿Qué quieres apagar?",
        "¿Qué deseas extinguir?",
        "Especifica qué quieres apagar.",
        "¿Qué objeto quieres apagar?",
        "Indica qué quieres apagar.",
        "¿Qué apagas?",
        "¿Qué cosa quieres extinguir?",
        "Apaga... ¿qué exactamente?",
        "¿Qué es lo que quieres apagar?",
        "¿Qué luz apagas?"
    );

    #endregion

    #region Wait / Time

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

    #endregion

    #region Containers

    /// <summary>
    /// Mensaje cuando el contenedor está cerrado.
    /// </summary>
    public static string ContainerIsClosed => Pick(
        "{0} está cerrado.",
        "{0} no se puede abrir, está cerrado.",
        "Primero tendrás que abrir {0}.",
        "{0} permanece cerrado.",
        "El acceso a {0} está bloqueado.",
        "{0} está firmemente cerrado.",
        "No puedes acceder a {0}, está cerrado.",
        "{0} tiene la tapa cerrada.",
        "Necesitas abrir {0} primero.",
        "{0} está sellado."
    );

    /// <summary>
    /// Mensaje cuando el contenedor está vacío.
    /// </summary>
    public static string ContainerEmpty => Pick(
        "{0} está vacío.",
        "No hay nada dentro de {0}.",
        "{0} no contiene nada.",
        "El interior de {0} está vacío.",
        "No encuentras nada en {0}.",
        "{0} está completamente vacío.",
        "Dentro de {0} no hay nada.",
        "{0} no tiene nada dentro.",
        "El contenido de {0} brilla por su ausencia.",
        "No hay nada que ver en {0}."
    );

    /// <summary>
    /// Mensaje cuando no hay contenedor con ese nombre.
    /// </summary>
    public static string NoSuchContainer => Pick(
        "No hay ningún contenedor con ese nombre.",
        "No ves ningún contenedor así.",
        "¿Qué contenedor? No veo ninguno con ese nombre.",
        "Aquí no hay nada donde meter o sacar cosas con ese nombre.",
        "No encuentras ningún contenedor así.",
        "No existe tal contenedor por aquí.",
        "No hay nada así que pueda contener objetos.",
        "No ves ningún receptáculo con ese nombre.",
        "¿Un contenedor? No veo ninguno así.",
        "Aquí no hay nada parecido a eso."
    );

    #endregion

    #region Doors

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

    #endregion

    #region Combat

    /// <summary>
    /// Mensaje preguntando a quién atacar.
    /// </summary>
    public static string WhoToAttack => Pick(
        "¿A quién quieres atacar?",
        "¿A quién deseas golpear?",
        "Especifica a quién quieres atacar.",
        "¿Contra quién arremetes?",
        "¿A quién te enfrentas?",
        "¿A quién?",
        "¿Quién es tu objetivo?",
        "¿A quién atacas?",
        "Indica a quién quieres atacar.",
        "¿Contra quién luchas?"
    );

    /// <summary>
    /// Mensaje cuando el NPC ya está muerto.
    /// </summary>
    public static string AlreadyDead => Pick(
        "{0} ya está muerto.",
        "{0} ya no respira.",
        "No tiene sentido, {0} ya ha caído.",
        "{0} yace sin vida en el suelo.",
        "{0} ya ha dejado este mundo.",
        "Es inútil, {0} ya está muerto.",
        "{0} ya no representa una amenaza.",
        "No puedes atacar a {0}, ya está muerto.",
        "{0} ya ha perecido.",
        "El cadáver de {0} yace inerte."
    );

    /// <summary>
    /// Mensaje preguntando qué saquear.
    /// </summary>
    public static string WhatToLoot => Pick(
        "¿Qué quieres saquear?",
        "¿Qué cadáver deseas registrar?",
        "Especifica qué quieres saquear.",
        "¿Qué cuerpo quieres revisar?",
        "¿A quién despojas?",
        "¿Qué saqueas?",
        "¿Qué cadáver revisas?",
        "Saquea... ¿qué exactamente?",
        "¿Qué cuerpo inspeccionas?",
        "¿De quién tomas el botín?"
    );

    /// <summary>
    /// Mensaje cuando el cadáver no tiene nada.
    /// </summary>
    public static string CorpseEmpty => Pick(
        "El cadáver de {0} no tiene nada de valor.",
        "{0} no llevaba nada encima.",
        "No encuentras nada útil en el cuerpo de {0}.",
        "El cuerpo de {0} está vacío.",
        "{0} no tenía nada que mereciera la pena.",
        "Registras el cadáver pero no hay nada.",
        "El cadáver de {0} no tiene botín.",
        "No hay nada que saquear de {0}.",
        "{0} murió sin posesiones.",
        "Sus bolsillos están vacíos."
    );

    #endregion

    #region Equipment

    /// <summary>
    /// Mensaje preguntando qué equipar.
    /// </summary>
    public static string WhatToEquip => Pick(
        "¿Qué quieres equipar?",
        "¿Qué deseas empuñar?",
        "Especifica qué quieres equipar.",
        "¿Qué objeto quieres equipar?",
        "¿Qué te pones?",
        "¿Qué equipas?",
        "¿Qué cosa quieres empuñar?",
        "Equipa... ¿qué exactamente?",
        "¿Qué es lo que quieres equipar?",
        "¿Qué arma o armadura?"
    );

    /// <summary>
    /// Mensaje preguntando qué desequipar.
    /// </summary>
    public static string WhatToUnequip => Pick(
        "¿Qué quieres desequipar?",
        "¿Qué deseas quitarte?",
        "Especifica qué quieres desequipar.",
        "¿Qué objeto quieres guardar?",
        "¿Qué te quitas?",
        "¿Qué desequipas?",
        "¿Qué cosa quieres quitarte?",
        "Desequipa... ¿qué exactamente?",
        "¿Qué es lo que quieres quitarte?",
        "¿Qué guardas?"
    );

    #endregion

    #region Light Sources

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
        "Ya está encendido, no es necesario.",
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

    #endregion

    #region Conversation

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

    #endregion

    #region Reading

    /// <summary>
    /// Mensaje cuando algo no se puede leer.
    /// </summary>
    public static string CannotRead => Pick(
        "No puedes leer {0}.",
        "{0} no tiene texto que leer.",
        "No hay nada escrito en {0}.",
        "{0} no es legible.",
        "No encuentras texto en {0}.",
        "{0} no contiene nada que leer.",
        "Es imposible leer {0}.",
        "No hay escritura en {0}.",
        "{0} no tiene nada escrito.",
        "No se puede leer {0}."
    );

    /// <summary>
    /// Mensaje cuando algo está en blanco.
    /// </summary>
    public static string IsBlank => Pick(
        "{0} está en blanco.",
        "Las páginas de {0} están vacías.",
        "No hay nada escrito en {0}.",
        "{0} no contiene texto alguno.",
        "{0} está completamente en blanco.",
        "El contenido de {0} brilla por su ausencia.",
        "{0} no tiene nada que leer.",
        "Las hojas de {0} están vírgenes.",
        "{0} está vacío de contenido.",
        "No hay texto en {0}."
    );

    #endregion

    #region Examine

    /// <summary>
    /// Mensaje cuando no hay nada especial que ver.
    /// </summary>
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

    #endregion
}
