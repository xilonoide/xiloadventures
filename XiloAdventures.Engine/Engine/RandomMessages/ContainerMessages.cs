using XiloAdventures.Engine.Models.Enums;
using static XiloAdventures.Engine.Engine.RandomMessageHelper;
using static XiloAdventures.Engine.Engine.GrammarHelper;

namespace XiloAdventures.Engine.Engine;

public static partial class RandomMessages
{
    /// <summary>
    /// Mensaje cuando el contenedor está cerrado.
    /// </summary>
    public static string GetContainerIsClosed(string name, GrammaticalGender gender, bool isPlural)
    {
        var e = Ending(gender, isPlural);
        var template = Pick(
            "{0} está cerrad{1}.",
            "{0} no se puede abrir, está cerrad{1}.",
            "Primero tendrás que abrir {0}.",
            "{0} permanece cerrad{1}.",
            "El acceso a {0} está bloqueado.",
            "{0} está firmemente cerrad{1}.",
            "No puedes acceder a {0}, está cerrad{1}.",
            "{0} tiene la tapa cerrada.",
            "Necesitas abrir {0} primero.",
            "{0} está sellad{1}."
        );
        return string.Format(template, name, e);
    }

    /// <summary>
    /// Mensaje cuando el contenedor está vacío.
    /// </summary>
    public static string GetContainerEmpty(string name, GrammaticalGender gender, bool isPlural)
    {
        var e = Ending(gender, isPlural);
        var template = Pick(
            "{0} está vací{1}.",
            "No hay nada dentro de {0}.",
            "{0} no contiene nada.",
            "El interior de {0} está vacío.",
            "No encuentras nada en {0}.",
            "{0} está completamente vací{1}.",
            "Dentro de {0} no hay nada.",
            "{0} no tiene nada dentro.",
            "El contenido de {0} brilla por su ausencia.",
            "No hay nada que ver en {0}."
        );
        return string.Format(template, name, e);
    }

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

    // Propiedades legacy para compatibilidad (deprecadas)
    [System.Obsolete("Use GetContainerIsClosed(name, gender, isPlural) instead")]
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

    [System.Obsolete("Use GetContainerEmpty(name, gender, isPlural) instead")]
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
}
