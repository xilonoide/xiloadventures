using static XiloAdventures.Engine.Engine.RandomMessageHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
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
}
