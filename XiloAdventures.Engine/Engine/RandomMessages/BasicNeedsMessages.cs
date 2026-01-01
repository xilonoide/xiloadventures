using XiloAdventures.Engine.Models.Enums;
using static XiloAdventures.Engine.Engine.RandomMessageHelper;
using static XiloAdventures.Engine.Engine.GrammarHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Pregunta qué quieres comer.
    /// </summary>
    public static string WhatToEat => Pick(
        "¿Qué quieres comer?",
        "¿Qué deseas comer?",
        "¿Qué te gustaría comer?",
        "¿Qué pretendes comer?",
        "¿Qué vas a comer?",
        "¿Qué piensas comer?",
        "¿Qué quieres llevarte a la boca?",
        "¿Qué te apetece comer?",
        "Dime, ¿qué quieres comer?",
        "¿Qué alimento deseas?"
    );

    /// <summary>
    /// Pregunta qué quieres beber.
    /// </summary>
    public static string WhatToDrink => Pick(
        "¿Qué quieres beber?",
        "¿Qué deseas beber?",
        "¿Qué te gustaría beber?",
        "¿Qué pretendes beber?",
        "¿Qué vas a beber?",
        "¿Qué piensas beber?",
        "¿Qué bebida quieres?",
        "¿Qué te apetece beber?",
        "Dime, ¿qué quieres beber?",
        "¿Qué líquido deseas?"
    );

    /// <summary>
    /// Mensaje cuando algo no se puede comer.
    /// </summary>
    public static string GetCannotEat(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
            "No puedes comer {0}.",
            "{0} no es comestible.",
            "Es imposible comer {0}.",
            "No te puedes comer {0}.",
            "{0} no se puede comer.",
            "Eso no es algo que puedas comer.",
            "{0} no es apto para el consumo.",
            "No creo que {0} sea comestible.",
            "Mejor no te comas {0}.",
            "Tu estómago rechaza la idea de comer {0}."
        );
        return string.Format(template, name);
    }

    /// <summary>
    /// Mensaje cuando algo no se puede beber.
    /// </summary>
    public static string GetCannotDrink(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
            "No puedes beber {0}.",
            "{0} no es bebible.",
            "Es imposible beber {0}.",
            "No te puedes beber {0}.",
            "{0} no se puede beber.",
            "Eso no es algo que puedas beber.",
            "{0} no es potable.",
            "No creo que {0} sea bebible.",
            "Mejor no te bebas {0}.",
            "Tu garganta rechaza la idea de beber {0}."
        );
        return string.Format(template, name);
    }

    /// <summary>
    /// Mensaje al comer algo exitosamente.
    /// </summary>
    public static string GetEatSuccess(string name, GrammaticalGender gender, bool isPlural)
    {
        var e = Ending(gender, isPlural);
        var template = Pick(
            "Te comes {0}. ¡Delicioso!",
            "Devoras {0} con gusto.",
            "Comes {0} con apetito.",
            "Te alimentas con {0}.",
            "Saboreas {0} lentamente.",
            "Engulles {0} sin dudarlo.",
            "Te llevas {0} a la boca y l{1} disfrutas.",
            "Masticas {0} con satisfacción.",
            "Disfrutas comiendo {0}.",
            "Te zampas {0} de un bocado."
        );
        return string.Format(template, name, e);
    }

    /// <summary>
    /// Mensaje al beber algo exitosamente.
    /// </summary>
    public static string GetDrinkSuccess(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
            "Te bebes {0}. ¡Refrescante!",
            "Bebes {0} de un trago.",
            "Saboreas {0} con gusto.",
            "Te hidratas con {0}.",
            "Tomas un sorbo de {0}.",
            "Bebes {0} con avidez.",
            "Te refrescas con {0}.",
            "Disfrutas bebiendo {0}.",
            "Vacías {0} de un trago.",
            "Bebes {0} lentamente."
        );
        return string.Format(template, name);
    }

    /// <summary>
    /// Mensaje cuando el sistema de necesidades básicas no está activo.
    /// </summary>
    public static string BasicNeedsNotEnabled => Pick(
        "Eso no tiene sentido en esta aventura.",
        "No necesitas hacer eso aquí.",
        "Esa acción no está disponible en este mundo.",
        "Aquí no tienes esa necesidad.",
        "Eso no aplica en esta aventura.",
        "No es necesario hacer eso.",
        "Esta aventura no requiere eso.",
        "No tienes por qué hacer eso aquí.",
        "Esa mecánica no está activa.",
        "No hace falta que hagas eso."
    );

    /// <summary>
    /// Pregunta cuántas horas quieres dormir.
    /// </summary>
    public static string HowManyHoursToSleep => Pick(
        "¿Cuántas horas quieres dormir? (1-8)",
        "¿Cuánto tiempo deseas descansar? (1-8 horas)",
        "¿Cuántas horas de sueño necesitas? (1-8)",
        "¿Por cuántas horas te echarás a dormir? (1-8)",
        "Dime, ¿cuántas horas dormirás? (1-8)",
        "¿Cuántas horas de descanso quieres? (1-8)",
        "¿Cuánto quieres dormir? (1-8 horas)",
        "¿Cuántas horas vas a descansar? (1-8)",
        "Indica las horas de sueño (1-8):",
        "¿Cuántas horas piensas dormir? (1-8)"
    );

    /// <summary>
    /// Mensaje de progreso de sueño.
    /// </summary>
    public static string SleepProgress(int current, int total) => Pick(
        $"Duermes {current}/{total} horas...",
        $"Zzz... ({current}/{total} horas)",
        $"Descansas placidamente... ({current}/{total})",
        $"Sueño profundo... {current} de {total} horas",
        $"Hora {current} de {total} de descanso...",
        $"Continúas durmiendo... ({current}/{total})"
    );

    /// <summary>
    /// Mensaje al despertarse normalmente.
    /// </summary>
    public static string WakeUpNormal => Pick(
        "Despiertas descansado.",
        "Te levantas sintiéndote renovado.",
        "Abres los ojos, has dormido bien.",
        "El sueño te ha sentado de maravilla.",
        "Te despiertas con energías renovadas.",
        "El descanso ha sido reparador.",
        "Despiertas sintiendo frescura.",
        "Te levantas de buen humor.",
        "Has tenido un sueño reparador.",
        "Te despiertas lleno de vitalidad."
    );

    /// <summary>
    /// Mensaje al despertarse sobresaltado.
    /// </summary>
    public static string WakeUpStartled => Pick(
        "¡Te despiertas sobresaltado!",
        "¡Algo te despierta de golpe!",
        "¡Un ruido te arranca del sueño!",
        "¡Despiertas bruscamente!",
        "¡Te sobresaltas y abres los ojos!",
        "¡Algo interrumpe tu descanso!",
        "¡Te despiertas de un salto!",
        "¡Un sobresalto te despierta!",
        "¡Saltas de la cama alarmado!",
        "¡Despiertas con el corazón acelerado!"
    );

    /// <summary>
    /// Mensaje cuando alguien entra mientras duermes.
    /// </summary>
    public static string SomeoneEnteredWhileSleeping => Pick(
        "Alguien ha entrado en la habitación.",
        "Oyes pasos acercándose.",
        "Una presencia te despierta.",
        "Alguien se acerca mientras duermes.",
        "Notas movimiento cerca de ti.",
        "Un ruido de pisadas te alerta.",
        "Sientes que no estás solo.",
        "Hay alguien más en la sala.",
        "Una figura se acerca.",
        "Percibes una presencia cercana."
    );

    /// <summary>
    /// Mensaje cuando te atacan mientras duermes.
    /// </summary>
    public static string AttackedWhileSleeping => Pick(
        "¡Te atacan mientras duermes!",
        "¡Un ataque interrumpe tu sueño!",
        "¡Alguien te agrede mientras descansabas!",
        "¡Despiertas bajo ataque!",
        "¡Te asaltan mientras dormías!",
        "¡Un golpe te despierta!",
        "¡Eres atacado mientras duermes!",
        "¡Tu sueño es interrumpido por un ataque!",
        "¡Un enemigo aprovecha tu vulnerabilidad!",
        "¡Te emboscan mientras descansabas!"
    );

    /// <summary>
    /// Mensaje cuando una necesidad crítica te despierta.
    /// </summary>
    public static string NeedWokeYouUp => Pick(
        "Tu cuerpo te obliga a despertar.",
        "Una necesidad urgente te despierta.",
        "No puedes seguir durmiendo así.",
        "Tu organismo te despierta.",
        "Es imposible seguir durmiendo.",
        "Una molestia corporal te despierta.",
        "Tu cuerpo reclama atención.",
        "No puedes ignorar tus necesidades.",
        "Una sensación te arranca del sueño.",
        "Tu cuerpo dice basta."
    );

    /// <summary>
    /// Mensaje cuando no puedes dormir por alguna razón.
    /// </summary>
    public static string CannotSleepHere => Pick(
        "No puedes dormir aquí.",
        "Este no es un buen lugar para dormir.",
        "Sería peligroso dormir en este sitio.",
        "No te sientes seguro para descansar.",
        "Mejor busca otro lugar para dormir.",
        "Aquí no es posible descansar.",
        "El lugar no invita al sueño.",
        "No logras conciliar el sueño aquí.",
        "Estás demasiado nervioso para dormir.",
        "No es momento ni lugar para dormir."
    );

    /// <summary>
    /// Mensaje cuando ya no tienes hambre.
    /// </summary>
    public static string NotHungry => Pick(
        "No tienes hambre ahora mismo.",
        "Tu estómago está satisfecho.",
        "No necesitas comer más.",
        "Ya has comido suficiente.",
        "No tienes apetito.",
        "Tu hambre está saciada.",
        "No te apetece comer nada.",
        "Estás lleno.",
        "No necesitas más comida.",
        "Tu barriga está contenta."
    );

    /// <summary>
    /// Mensaje cuando ya no tienes sed.
    /// </summary>
    public static string NotThirsty => Pick(
        "No tienes sed ahora mismo.",
        "Ya estás bien hidratado.",
        "No necesitas beber más.",
        "Ya has bebido suficiente.",
        "No tienes ganas de beber.",
        "Tu sed está saciada.",
        "No te apetece beber nada.",
        "Estás bien de líquidos.",
        "No necesitas más bebida.",
        "Tu cuerpo no pide agua."
    );

    /// <summary>
    /// Mensaje cuando ya no tienes sueño.
    /// </summary>
    public static string NotTired => Pick(
        "No tienes sueño ahora mismo.",
        "Estás completamente despierto.",
        "No necesitas descansar.",
        "Ya has dormido suficiente.",
        "No tienes ganas de dormir.",
        "Tu cuerpo está descansado.",
        "No te apetece dormir.",
        "Estás lleno de energía.",
        "No necesitas más descanso.",
        "Tu mente está alerta."
    );

    /// <summary>
    /// Mensaje de respuesta inválida para horas de sueño.
    /// </summary>
    public static string InvalidSleepHours => Pick(
        "Debes indicar un número entre 1 y 8.",
        "Introduce un número del 1 al 8.",
        "El número debe estar entre 1 y 8.",
        "Solo puedes dormir entre 1 y 8 horas.",
        "Indica un número válido (1-8).",
        "¿Cuántas horas? Debe ser entre 1 y 8.",
        "El valor debe ser de 1 a 8 horas.",
        "Número inválido, usa del 1 al 8.",
        "Elige entre 1 y 8 horas.",
        "Solo acepto números del 1 al 8."
    );

    // Propiedades legacy para compatibilidad (deprecadas)
    [System.Obsolete("Use GetCannotEat(name, gender, isPlural) instead")]
    public static string CannotEat => Pick(
        "No puedes comer {0}.",
        "{0} no es comestible.",
        "Es imposible comer {0}.",
        "No te puedes comer {0}.",
        "{0} no se puede comer.",
        "Eso no es algo que puedas comer.",
        "{0} no es apto para el consumo.",
        "No creo que {0} sea comestible.",
        "Mejor no te comas {0}.",
        "Tu estómago rechaza la idea de comer {0}."
    );

    [System.Obsolete("Use GetCannotDrink(name, gender, isPlural) instead")]
    public static string CannotDrink => Pick(
        "No puedes beber {0}.",
        "{0} no es bebible.",
        "Es imposible beber {0}.",
        "No te puedes beber {0}.",
        "{0} no se puede beber.",
        "Eso no es algo que puedas beber.",
        "{0} no es potable.",
        "No creo que {0} sea bebible.",
        "Mejor no te bebas {0}.",
        "Tu garganta rechaza la idea de beber {0}."
    );

    [System.Obsolete("Use GetEatSuccess(name, gender, isPlural) instead")]
    public static string EatSuccess => Pick(
        "Te comes {0}. ¡Delicioso!",
        "Devoras {0} con gusto.",
        "Comes {0} con apetito.",
        "Te alimentas con {0}.",
        "Saboreas {0} lentamente.",
        "Engulles {0} sin dudarlo.",
        "Te llevas {0} a la boca y lo disfrutas.",
        "Masticas {0} con satisfacción.",
        "Disfrutas comiendo {0}.",
        "Te zampas {0} de un bocado."
    );

    [System.Obsolete("Use GetDrinkSuccess(name, gender, isPlural) instead")]
    public static string DrinkSuccess => Pick(
        "Te bebes {0}. ¡Refrescante!",
        "Bebes {0} de un trago.",
        "Saboreas {0} con gusto.",
        "Te hidratas con {0}.",
        "Tomas un sorbo de {0}.",
        "Bebes {0} con avidez.",
        "Te refrescas con {0}.",
        "Disfrutas bebiendo {0}.",
        "Vacías {0} de un trago.",
        "Bebes {0} lentamente."
    );
}
