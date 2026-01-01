using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Helper para concordancia gramatical en español (género y número).
/// </summary>
internal static class GrammarHelper
{
    /// <summary>
    /// Retorna la terminación de adjetivo/participio según género y número.
    /// Masculino singular: "o", Femenino singular: "a", Masculino plural: "os", Femenino plural: "as"
    /// </summary>
    internal static string Ending(GrammaticalGender gender, bool isPlural)
    {
        return (gender, isPlural) switch
        {
            (GrammaticalGender.Masculine, false) => "o",
            (GrammaticalGender.Feminine, false) => "a",
            (GrammaticalGender.Masculine, true) => "os",
            (GrammaticalGender.Feminine, true) => "as",
            _ => "o"
        };
    }

    /// <summary>
    /// Retorna el artículo definido según género y número.
    /// </summary>
    internal static string Article(GrammaticalGender gender, bool isPlural)
    {
        return (gender, isPlural) switch
        {
            (GrammaticalGender.Masculine, false) => "el",
            (GrammaticalGender.Feminine, false) => "la",
            (GrammaticalGender.Masculine, true) => "los",
            (GrammaticalGender.Feminine, true) => "las",
            _ => "el"
        };
    }

    /// <summary>
    /// Retorna el artículo indefinido según género y número.
    /// </summary>
    internal static string IndefiniteArticle(GrammaticalGender gender, bool isPlural)
    {
        return (gender, isPlural) switch
        {
            (GrammaticalGender.Masculine, false) => "un",
            (GrammaticalGender.Feminine, false) => "una",
            (GrammaticalGender.Masculine, true) => "unos",
            (GrammaticalGender.Feminine, true) => "unas",
            _ => "un"
        };
    }

    /// <summary>
    /// Retorna "este/esta/estos/estas" según género y número.
    /// </summary>
    internal static string Demonstrative(GrammaticalGender gender, bool isPlural)
    {
        return (gender, isPlural) switch
        {
            (GrammaticalGender.Masculine, false) => "este",
            (GrammaticalGender.Feminine, false) => "esta",
            (GrammaticalGender.Masculine, true) => "estos",
            (GrammaticalGender.Feminine, true) => "estas",
            _ => "este"
        };
    }
}
