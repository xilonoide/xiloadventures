using XiloAdventures.Engine.Models.Enums;
using static XiloAdventures.Engine.Engine.RandomMessageHelper;
using static XiloAdventures.Engine.Engine.GrammarHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
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
    public static string GetAlreadyDead(string name, GrammaticalGender gender, bool isPlural)
    {
        var e = Ending(gender, isPlural);
        var template = Pick(
            "{0} ya está muert{1}.",
            "{0} ya no respira.",
            "No tiene sentido, {0} ya ha caído.",
            "{0} yace sin vida en el suelo.",
            "{0} ya ha dejado este mundo.",
            "Es inútil, {0} ya está muert{1}.",
            "{0} ya no representa una amenaza.",
            "No puedes atacar a {0}, ya está muert{1}.",
            "{0} ya ha perecido.",
            "El cadáver de {0} yace inerte."
        );
        return string.Format(template, name, e);
    }

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
    /// Mensaje al examinar un cadáver (con pista sobre saquear).
    /// </summary>
    public static string GetCorpseExamine(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
            "El cuerpo sin vida de {0} yace en el suelo. Podrías saquearlo.",
            "{0} está muerto. Quizás deberías registrar el cadáver.",
            "Es el cadáver de {0}. Puedes intentar saquearlo.",
            "{0} ya no respira. Su cuerpo aún podría contener algo útil.",
            "Observas el cuerpo inerte de {0}. Podrías usar 'saquear' para ver qué llevaba.",
            "El cadáver de {0} reposa inmóvil. Tal vez tenga algo de valor.",
            "{0} ha dejado de existir. Puedes saquear sus pertenencias.",
            "Solo queda el cuerpo frío de {0}. ¿Quieres registrarlo?",
            "Los ojos sin vida de {0} miran al vacío. Podrías revisar sus bolsillos.",
            "{0} ya no es una amenaza. Puedes saquear lo que llevaba encima.",
            "Es un cadáver. {0} está definitivamente muerto. Usa 'saquear' para buscar botín.",
            "El cuerpo de {0} yace aquí. Quizás tenga algo que te sea útil."
        );
        return string.Format(template, name);
    }

    /// <summary>
    /// Mensaje cuando el cadáver no tiene nada.
    /// </summary>
    public static string GetCorpseEmpty(string name, GrammaticalGender gender, bool isPlural)
    {
        var template = Pick(
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
        return string.Format(template, name);
    }

    // Propiedades legacy para compatibilidad (deprecadas)
    [System.Obsolete("Use GetAlreadyDead(name, gender, isPlural) instead")]
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

    [System.Obsolete("Use GetCorpseEmpty(name, gender, isPlural) instead")]
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
}
