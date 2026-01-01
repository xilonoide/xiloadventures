using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
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
}
