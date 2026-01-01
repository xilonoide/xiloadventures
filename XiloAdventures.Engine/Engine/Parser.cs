using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Engine;

/// <summary>
/// Types of prepositions recognized by the parser.
/// </summary>
public enum PrepositionKind
{
    None,
    To,
    With,
    From,
    In
}

/// <summary>
/// Compiled regex patterns for performance optimization.
/// </summary>
file static class ParserRegex
{
    public static readonly Regex MultiSpace = new(@"\s+", RegexOptions.Compiled);
    public static readonly Regex Punctuation = new("[.,;:!?¡¿\"'()]", RegexOptions.Compiled);
    // Matches verbs with attached pronouns: meterlo, cogerla, darselos, etc.
    public static readonly Regex VerbWithPronoun = new(@"^(.+?)(l[oa]s?|me|te|se|nos|os|les?)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
}

/// <summary>
/// Represents a parsed player command with verb, objects, and preposition.
/// </summary>
public readonly struct ParsedCommand
{
    /// <summary>The canonical verb (e.g., "go", "take", "look").</summary>
    public string Verb { get; }

    /// <summary>The direct object of the command (e.g., "sword", "north"), normalized by noun aliases.</summary>
    public string? DirectObject { get; }

    /// <summary>The indirect object (e.g., "with key" -> "key"), normalized by noun aliases.</summary>
    public string? IndirectObject { get; }

    /// <summary>The preposition used in the command.</summary>
    public PrepositionKind Preposition { get; }

    /// <summary>The original direct object before noun alias normalization.</summary>
    public string? OriginalDirectObject { get; }

    /// <summary>The original indirect object before noun alias normalization.</summary>
    public string? OriginalIndirectObject { get; }

    public ParsedCommand(string verb, string? directObject, string? indirectObject, PrepositionKind preposition,
        string? originalDirectObject = null, string? originalIndirectObject = null)
    {
        Verb = verb;
        DirectObject = directObject;
        IndirectObject = indirectObject;
        Preposition = preposition;
        OriginalDirectObject = originalDirectObject;
        OriginalIndirectObject = originalIndirectObject;
    }
}

internal sealed class ParserDictionaryDto
{
    public Dictionary<string, string[]?>? verbs { get; set; }
    public Dictionary<string, string[]?>? nouns { get; set; }
}

/// <summary>
/// Natural language parser for adventure game commands.
/// Supports Spanish input with verb/noun aliases and direction shortcuts.
/// </summary>
/// <remarks>
/// The parser normalizes player input by:
/// - Converting verb synonyms to canonical forms (e.g., "examinar" -> "examine")
/// - Handling prepositions (a, con, de, en)
/// - Recognizing direction shortcuts (n, s, e, o, etc.)
/// - Supporting per-world custom dictionaries
/// </remarks>
public static class Parser
{
    // Diccionarios globales (base + recursos embebidos)
    private static readonly Dictionary<string, string> GlobalVerbAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> GlobalNounAliases = new(StringComparer.OrdinalIgnoreCase);

    // Diccionarios específicos del mundo actual (se rellenan con GameInfo.ParserDictionaryJson)
    private static readonly Dictionary<string, string> WorldVerbAliases = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> WorldNounAliases = new(StringComparer.OrdinalIgnoreCase);

    // Último objeto referenciado (para resolución de pronombres)
    private static string? _lastReferencedObject;

    // Pronombres de objeto directo que pueden referenciar al último objeto
    private static readonly HashSet<string> DirectObjectPronouns = new(StringComparer.OrdinalIgnoreCase)
    {
        "lo", "la", "los", "las"
    };

    private static readonly Dictionary<string, PrepositionKind> Prepositions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["a"] = PrepositionKind.To,
        ["al"] = PrepositionKind.To,
        ["hacia"] = PrepositionKind.To,
        ["con"] = PrepositionKind.With,
        ["de"] = PrepositionKind.From,
        ["desde"] = PrepositionKind.From,
        ["en"] = PrepositionKind.In,
        ["sobre"] = PrepositionKind.In
    };

    private static readonly HashSet<string> IgnoredNounPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "el","la","los","las",
        "un","una","unos","unas",
        "al","del",
        "mi","mis","tu","tus","su","sus",
        "este","esta","estos","estas",
        "ese","esa","esos","esas",
        "aquel","aquella","aquellos","aquellas"
    };

    static Parser()
    {
        InitializeDictionaries();
    }

    private static void InitializeDictionaries()
    {
        // Verbos base en castellano -> verbos canónicos
        AddVerbAlias("examine", "examinar", "examina", "x");
        AddVerbAlias("go", "ir", "ve", "andar", "caminar");
        AddVerbAlias("inventory", "inventario", "inv", "i");
        AddVerbAlias("take", "coger", "toma", "coge", "tomar", "agarrar", "recoger");
        AddVerbAlias("drop", "soltar", "dejar", "tirar");
        AddVerbAlias("open", "abrir", "abre");
        AddVerbAlias("close", "cerrar", "cierra");
        AddVerbAlias("unlock", "desbloquear", "abrir con llave", "abrir con");
        AddVerbAlias("lock", "bloquear", "cerrar con llave", "cerrar con");
        AddVerbAlias("put", "meter", "poner", "colocar", "guardar");
        AddVerbAlias("get_from", "sacar", "quitar", "extraer");
        AddVerbAlias("look_in", "ver en", "ver dentro", "mirar", "mirar en");
        AddVerbAlias("talk", "hablar", "habla", "charlar", "conversar", "decir", "di");
        AddVerbAlias("say", "responder", "contestar");
        AddVerbAlias("option", "opcion", "opción");
        AddVerbAlias("use", "usar", "utilizar", "emplear");
        AddVerbAlias("give", "dar", "entregar");
        AddVerbAlias("quests", "misiones", "mision", "misión", "quest");
        AddVerbAlias("save", "guardar", "salvar");
        AddVerbAlias("load", "cargar");
        AddVerbAlias("help", "ayuda");
        AddVerbAlias("read", "leer", "lee");
        AddVerbAlias("commands", "?", "verbos", "comandos");
        AddVerbAlias("wait", "esperar", "espera", "z");

        // Verbos de combate y equipamiento
        AddVerbAlias("attack", "atacar", "ataca", "golpear", "golpea", "luchar", "pelear");
        AddVerbAlias("equip", "equipar", "equipa", "empuñar", "empuña", "vestir", "viste", "ponerse");
        AddVerbAlias("unequip", "desequipar", "desequipa", "quitar", "quitarse", "desvestir");
        AddVerbAlias("loot", "saquear", "saquea", "registrar", "registra", "desvalijar");
        AddVerbAlias("equipment", "equipo", "equipamiento");

        // Verbos de iluminación
        AddVerbAlias("ignite", "encender", "enciende", "prender", "prende");
        AddVerbAlias("extinguish", "apagar", "apaga");

        // Verbos de fabricación
        AddVerbAlias("craft", "fabricar", "fabrica", "crear", "crea", "construir", "construye");

        // Verbos de necesidades básicas
        AddVerbAlias("eat", "comer", "come", "devorar", "devora", "masticar", "mastica", "tragar", "zampar");
        AddVerbAlias("drink", "beber", "bebe", "tomar", "sorber", "sorbe");
        AddVerbAlias("sleep", "dormir", "duerme", "descansar", "descansa", "echarse", "acostarse");

        // Algunos sinónimos globales de nombres
        AddNounAlias("espada", "hoja", "sable", "mandoble");
        AddNounAlias("enano", "enano borracho", "minero", "barbudo");
        AddNounAlias("posada", "taberna", "mesón", "meson");
        AddNounAlias("oro", "moneda", "monedas", "dinero");

        // Intentar cargar ParserDictionary.json embebido (si existe)
        try
        {
            var asm = typeof(Parser).Assembly;
            var resourceName = asm
                .GetManifestResourceNames()
                .FirstOrDefault(n => n.EndsWith("ParserDictionary.json", StringComparison.OrdinalIgnoreCase));

            if (resourceName != null)
            {
                using var stream = asm.GetManifestResourceStream(resourceName);

                if (stream != null)
                {
                    using var reader = new StreamReader(stream);
                    var json = reader.ReadToEnd();
                    var dto = JsonSerializer.Deserialize<ParserDictionaryDto>(json);
                    ApplyDtoToAliases(dto, GlobalVerbAliases, GlobalNounAliases);
                }
            }
        }
        catch
        {
            // Si falla el recurso embebido, seguimos con los diccionarios base.
        }
    }

    private static void AddVerbAlias(string canonical, params string[] synonyms)
    {
        if (string.IsNullOrWhiteSpace(canonical))
            return;

        GlobalVerbAliases[canonical] = canonical;
        foreach (var syn in synonyms)
        {
            var s = syn?.Trim();
            if (string.IsNullOrEmpty(s)) continue;
            // Guardar tanto la versión original como la versión sin acentos
            GlobalVerbAliases[s] = canonical;
            var normalized = RemoveDiacritics(s).ToLowerInvariant();
            if (normalized != s.ToLowerInvariant())
                GlobalVerbAliases[normalized] = canonical;
        }
    }

    private static void AddNounAlias(string canonical, params string[] synonyms)
    {
        if (string.IsNullOrWhiteSpace(canonical))
            return;

        canonical = canonical.Trim().ToLowerInvariant();
        GlobalNounAliases[canonical] = canonical;
        // También guardar versión sin acentos del canónico
        var canonicalNormalized = RemoveDiacritics(canonical);
        if (canonicalNormalized != canonical)
            GlobalNounAliases[canonicalNormalized] = canonical;

        foreach (var syn in synonyms)
        {
            var s = syn?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(s)) continue;
            // Guardar tanto la versión original como sin acentos
            GlobalNounAliases[s] = canonical;
            var normalized = RemoveDiacritics(s);
            if (normalized != s)
                GlobalNounAliases[normalized] = canonical;
        }
    }

    private static void ApplyDtoToAliases(
        ParserDictionaryDto? dto,
        Dictionary<string, string> verbAliases,
        Dictionary<string, string> nounAliases)
    {
        if (dto == null) return;

        if (dto.verbs != null)
        {
            foreach (var kvp in dto.verbs)
            {
                var canonical = (kvp.Key ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(canonical))
                    continue;

                verbAliases[canonical] = canonical;

                if (kvp.Value == null) continue;
                foreach (var rawSyn in kvp.Value)
                {
                    var syn = (rawSyn ?? string.Empty).Trim();
                    if (string.IsNullOrEmpty(syn)) continue;
                    // Guardar tanto con como sin acentos
                    verbAliases[syn] = canonical;
                    var normalized = RemoveDiacritics(syn).ToLowerInvariant();
                    if (normalized != syn.ToLowerInvariant())
                        verbAliases[normalized] = canonical;
                }
            }
        }

        if (dto.nouns != null)
        {
            foreach (var kvp in dto.nouns)
            {
                var canonical = (kvp.Key ?? string.Empty).Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(canonical))
                    continue;

                nounAliases[canonical] = canonical;
                // También versión sin acentos del canónico
                var canonicalNorm = RemoveDiacritics(canonical);
                if (canonicalNorm != canonical)
                    nounAliases[canonicalNorm] = canonical;

                if (kvp.Value == null) continue;
                foreach (var rawSyn in kvp.Value)
                {
                    var syn = (rawSyn ?? string.Empty).Trim().ToLowerInvariant();
                    if (string.IsNullOrEmpty(syn)) continue;
                    // Guardar tanto con como sin acentos
                    nounAliases[syn] = canonical;
                    var normalized = RemoveDiacritics(syn);
                    if (normalized != syn)
                        nounAliases[normalized] = canonical;
                }
            }
        }
    }

    /// <summary>
    /// Sets a world-specific dictionary for verb and noun aliases.
    /// </summary>
    /// <param name="json">JSON string containing verb/noun mappings, or null to clear.</param>
    public static void SetWorldDictionary(string? json)
    {
        WorldVerbAliases.Clear();
        WorldNounAliases.Clear();

        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            var dto = JsonSerializer.Deserialize<ParserDictionaryDto>(json);
            ApplyDtoToAliases(dto, WorldVerbAliases, WorldNounAliases);
        }
        catch
        {
            // Si el JSON está mal, ignoramos el diccionario del mundo.
        }
    }

    /// <summary>
    /// Parses a compound command string into multiple ParsedCommand objects.
    /// Handles commands joined by "y" (and) and resolves pronouns.
    /// </summary>
    /// <param name="input">The raw command string from the player (may contain multiple commands).</param>
    /// <returns>An array of ParsedCommand objects.</returns>
    /// <example>
    /// "coger el vaso y meterlo en el baul" returns two commands:
    /// 1. take vaso
    /// 2. put vaso in baul
    /// </example>
    public static ParsedCommand[] ParseAll(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return [new ParsedCommand(string.Empty, null, null, PrepositionKind.None)];

        input = input.Trim();
        input = ParserRegex.MultiSpace.Replace(input, " ");

        // Dividir por " y " para comandos compuestos
        // Usamos un split más cuidadoso para no dividir "blanco y negro" como objeto
        var subCommands = SplitCompoundCommands(input);
        var results = new List<ParsedCommand>();

        foreach (var subCommand in subCommands)
        {
            var cmd = ParseSingle(subCommand, resolvePronouns: true);
            results.Add(cmd);

            // Actualizar el último objeto referenciado para el siguiente comando
            if (!string.IsNullOrEmpty(cmd.DirectObject))
                _lastReferencedObject = cmd.DirectObject;
        }

        return results.ToArray();
    }

    /// <summary>
    /// Splits a compound command by " y " intelligently.
    /// Only splits when " y " appears to join two commands (verb ... y verb ...).
    /// </summary>
    private static List<string> SplitCompoundCommands(string input)
    {
        var result = new List<string>();

        // Buscar " y " como separador de comandos
        var parts = input.Split(new[] { " y " }, StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            result.Add(input);
            return result;
        }

        // Verificar que después de " y " hay un verbo (o verbo+pronombre)
        // Si no, probablemente es un objeto compuesto como "blanco y negro"
        var accumulated = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            var nextPart = parts[i].Trim();
            var firstWord = nextPart.Split(' ', 2)[0];

            // Extraer verbo base si tiene pronombre pegado
            var verbBase = ExtractVerbFromPronounForm(firstWord) ?? firstWord;

            // Si el primer token es un verbo conocido, es un nuevo comando
            if (HasVerbAlias(verbBase) || HasVerbAlias(firstWord))
            {
                result.Add(accumulated.Trim());
                accumulated = nextPart;
            }
            else
            {
                // No es un verbo, es parte del objeto anterior
                accumulated += " y " + nextPart;
            }
        }

        result.Add(accumulated.Trim());
        return result;
    }

    /// <summary>
    /// Parses a player command string into structured components.
    /// </summary>
    /// <param name="input">The raw command string from the player.</param>
    /// <returns>A ParsedCommand with verb, direct/indirect objects, and preposition.</returns>
    public static ParsedCommand Parse(string input)
    {
        var commands = ParseAll(input);
        return commands.Length > 0 ? commands[0] : new ParsedCommand(string.Empty, null, null, PrepositionKind.None);
    }

    /// <summary>
    /// Parses a single command (no compound splitting).
    /// </summary>
    private static ParsedCommand ParseSingle(string input, bool resolvePronouns = false)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ParsedCommand(string.Empty, null, null, PrepositionKind.None);

        input = input.Trim();

        // Normalizar espacios
        input = ParserRegex.MultiSpace.Replace(input, " ");

        var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var verbToken = parts[0];
        var rest = parts.Length > 1 ? parts[1] : string.Empty;

        // Detectar pronombre pegado al verbo (meterlo, cogerla, etc.)
        string? attachedPronoun = null;
        var pronounMatch = ParserRegex.VerbWithPronoun.Match(verbToken);
        if (pronounMatch.Success)
        {
            var potentialVerb = pronounMatch.Groups[1].Value;
            // Verificar que la raíz es un verbo conocido
            if (HasVerbAlias(potentialVerb))
            {
                verbToken = potentialVerb;
                attachedPronoun = pronounMatch.Groups[2].Value;
            }
        }

        // Dirección sola: "n", "norte", "arriba", etc.
        if (!HasVerbAlias(verbToken) && IsDirection(verbToken))
        {
            var dirToken = NormalizeDirectionToken(verbToken);
            return new ParsedCommand("go", dirToken, null, PrepositionKind.None);
        }

        var canonicalVerb = NormalizeVerb(verbToken);

        if (string.IsNullOrEmpty(rest))
            return new ParsedCommand(canonicalVerb, null, null, PrepositionKind.None);

        // Partir objeto directo / indirecto según preposición
        string? direct = null;
        string? indirect = null;
        var prepKind = PrepositionKind.None;

        var tokens = rest.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var prepIndex = -1;
        PrepositionKind foundPrepKind = PrepositionKind.None;

        for (int i = 0; i < tokens.Length; i++)
        {
            var t = tokens[i];
            if (Prepositions.TryGetValue(t, out var pk))
            {
                prepIndex = i;
                foundPrepKind = pk;
                break;
            }
        }

        if (prepIndex >= 0)
        {
            direct = string.Join(' ', tokens.Take(prepIndex));
            indirect = string.Join(' ', tokens.Skip(prepIndex + 1));
            prepKind = foundPrepKind;
        }
        else
        {
            direct = rest;
        }

        // Guardar valores limpios (sin artículos, etc.) antes de aplicar aliases de sustantivos
        var originalDirect = CleanNoun(direct);
        var originalIndirect = CleanNoun(indirect);

        // Normalizar con aliases de sustantivos
        direct = NormalizeNoun(direct);
        indirect = NormalizeNoun(indirect);

        // Heurísticas específicas por verbo

        // "ir al norte", "ve a la puerta"
        if (canonicalVerb == "go" && string.IsNullOrEmpty(direct) && !string.IsNullOrEmpty(indirect))
        {
            direct = indirect;
            indirect = null;
            originalDirect = originalIndirect;
            originalIndirect = null;
        }

        // "hablar con el enano", "decir al guardia"
        if ((canonicalVerb == "talk" || canonicalVerb == "say" || canonicalVerb == "option")
            && string.IsNullOrEmpty(direct) && !string.IsNullOrEmpty(indirect))
        {
            direct = indirect;
            indirect = null;
            originalDirect = originalIndirect;
            originalIndirect = null;
        }

        // Resolución de pronombres: si hay un pronombre pegado al verbo y no hay objeto directo,
        // usar el último objeto referenciado
        if (resolvePronouns && attachedPronoun != null && DirectObjectPronouns.Contains(attachedPronoun))
        {
            if (string.IsNullOrEmpty(direct) && !string.IsNullOrEmpty(_lastReferencedObject))
            {
                direct = _lastReferencedObject;
                originalDirect = _lastReferencedObject;
            }
        }

        return new ParsedCommand(
            canonicalVerb,
            string.IsNullOrWhiteSpace(direct) ? null : direct,
            string.IsNullOrWhiteSpace(indirect) ? null : indirect,
            prepKind,
            string.IsNullOrWhiteSpace(originalDirect) ? null : originalDirect,
            string.IsNullOrWhiteSpace(originalIndirect) ? null : originalIndirect);
    }

    private static bool HasVerbAlias(string token)
    {
        return WorldVerbAliases.ContainsKey(token) || GlobalVerbAliases.ContainsKey(token);
    }

    /// <summary>
    /// Extracts the verb root from a verb+pronoun form (e.g., "meterlo" -> "meter").
    /// Returns null if not a valid verb+pronoun form.
    /// </summary>
    private static string? ExtractVerbFromPronounForm(string token)
    {
        var match = ParserRegex.VerbWithPronoun.Match(token);
        if (match.Success)
        {
            var potentialVerb = match.Groups[1].Value;
            if (HasVerbAlias(potentialVerb))
                return potentialVerb;
        }
        return null;
    }

    /// <summary>
    /// Clears the last referenced object. Call this when starting a new game or loading.
    /// </summary>
    public static void ClearPronounContext()
    {
        _lastReferencedObject = null;
    }

    /// <summary>
    /// Gets all default verb aliases (verb -> synonyms).
    /// </summary>
    public static IReadOnlyDictionary<string, List<string>> GetDefaultVerbs()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in GlobalVerbAliases)
        {
            var canonical = kvp.Value;
            if (!result.ContainsKey(canonical))
                result[canonical] = new List<string>();
            if (!canonical.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                result[canonical].Add(kvp.Key);
        }
        return result;
    }

    /// <summary>
    /// Gets all default noun aliases (noun -> synonyms).
    /// </summary>
    public static IReadOnlyDictionary<string, List<string>> GetDefaultNouns()
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in GlobalNounAliases)
        {
            var canonical = kvp.Value;
            if (!result.ContainsKey(canonical))
                result[canonical] = new List<string>();
            if (!canonical.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                result[canonical].Add(kvp.Key);
        }
        return result;
    }

    /// <summary>
    /// Gets the default ignored words (articles, determiners, etc.).
    /// </summary>
    public static IReadOnlyCollection<string> GetDefaultIgnoredWords()
    {
        return IgnoredNounPrefixes;
    }

    private static string NormalizeVerb(string verb)
    {
        var token = NormalizeToken(verb);
        if (string.IsNullOrEmpty(token))
            return string.Empty;

        if (WorldVerbAliases.TryGetValue(token, out var vWorld))
            return vWorld;

        if (GlobalVerbAliases.TryGetValue(token, out var vGlobal))
            return vGlobal;

        return token.ToLowerInvariant();
    }

    private static string? NormalizeNoun(string? noun)
    {
        if (string.IsNullOrWhiteSpace(noun))
            return null;

        // minúsculas
        var s = noun.ToLowerInvariant();

        // quitar puntuación sencilla
        s = ParserRegex.Punctuation.Replace(s, "");

        // eliminar acentos
        s = RemoveDiacritics(s);

        // normalizar espacios
        s = ParserRegex.MultiSpace.Replace(s, " ").Trim();
        if (string.IsNullOrEmpty(s))
            return null;

        // eliminar artículos / determinantes iniciales
        // pero NO eliminar si es una dirección válida (este = east vs este = this)
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (parts.Count > 0 && IgnoredNounPrefixes.Contains(parts[0]) && !IsDirection(parts[0]))
        {
            parts.RemoveAt(0);
        }
        s = string.Join(' ', parts);
        if (string.IsNullOrWhiteSpace(s))
            return null;

        // Intentar mapear por diccionario de mundo (buscar con y sin acentos)
        if (WorldNounAliases.TryGetValue(s, out var nWorld))
            return nWorld;

        // Intentar mapear por diccionario global
        if (GlobalNounAliases.TryGetValue(s, out var nGlobal))
            return nGlobal;

        return s;
    }

    /// <summary>
    /// Limpia un sustantivo (quita artículos, puntuación, acentos) SIN aplicar aliases.
    /// Usado para guardar el valor original antes de normalizar.
    /// </summary>
    private static string? CleanNoun(string? noun)
    {
        if (string.IsNullOrWhiteSpace(noun))
            return null;

        // minúsculas
        var s = noun.ToLowerInvariant();

        // quitar puntuación sencilla
        s = ParserRegex.Punctuation.Replace(s, "");

        // eliminar acentos
        s = RemoveDiacritics(s);

        // normalizar espacios
        s = ParserRegex.MultiSpace.Replace(s, " ").Trim();
        if (string.IsNullOrEmpty(s))
            return null;

        // eliminar artículos / determinantes iniciales
        // pero NO eliminar si es una dirección válida (este = east vs este = this)
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (parts.Count > 0 && IgnoredNounPrefixes.Contains(parts[0]) && !IsDirection(parts[0]))
        {
            parts.RemoveAt(0);
        }
        s = string.Join(' ', parts);

        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    private static string NormalizeToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        token = token.Trim();
        token = ParserRegex.Punctuation.Replace(token, "");
        token = RemoveDiacritics(token);
        return token.ToLowerInvariant();
    }

    /// <summary>
    /// Elimina los diacríticos (acentos) de un string.
    /// Ejemplo: "poción" -> "pocion", "adiós" -> "adios"
    /// </summary>
    private static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // Normalizar a FormD separa las letras base de sus diacríticos
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);

        foreach (var c in normalized)
        {
            // Mantener solo caracteres que no sean diacríticos (NonSpacingMark)
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        // Volver a FormC para tener un string normalizado
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    private static bool IsDirection(string token)
    {
        token = token.ToLowerInvariant();
        return token is "n" or "s" or "e" or "o" or "ne" or "no" or "se" or "so"
            or "norte" or "sur" or "este" or "oeste"
            or "noreste" or "noroeste" or "sureste" or "suroeste"
            or "arriba" or "abajo" or "subir" or "bajar";
    }

    private static string NormalizeDirectionToken(string dir)
    {
        dir = dir.ToLowerInvariant();
        return dir switch
        {
            "norte" or "n" => "n",
            "sur" or "s" => "s",
            "este" or "e" => "e",
            "oeste" or "o" => "o",
            "noreste" or "ne" => "ne",
            "noroeste" or "no" => "no",
            "sureste" or "se" => "se",
            "suroeste" or "so" => "so",
            "arriba" or "subir" => "up",
            "abajo" or "bajar" => "down",
            _ => dir
        };
    }
}
