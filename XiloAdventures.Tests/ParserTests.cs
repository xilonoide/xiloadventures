using System;
using System.Collections.Generic;
using System.Text.Json;
using XiloAdventures.Engine;
using Xunit;

using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Tests;

public class ParserTests
{
    [Fact]
    public void Parse_SingleDirection_DefaultsToGoVerb()
    {
        var parsed = Parser.Parse("n");

        Assert.Equal("go", parsed.Verb);
        Assert.Equal("n", parsed.DirectObject);
        Assert.Null(parsed.IndirectObject);
        Assert.Equal(PrepositionKind.None, parsed.Preposition);
    }

    [Fact]
    public void Parse_KnownVerbSynonym_NormalizesVerbAndNoun()
    {
        var parsed = Parser.Parse("examinar la espada!");

        Assert.Equal("examine", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
        Assert.Null(parsed.IndirectObject);
    }

    [Fact]
    public void Parse_GoWithPreposition_MovesTargetToDirectObject()
    {
        var parsed = Parser.Parse("ve al norte");

        Assert.Equal("go", parsed.Verb);
        Assert.Equal("norte", parsed.DirectObject);
        Assert.Null(parsed.IndirectObject);
        Assert.Equal(PrepositionKind.To, parsed.Preposition);
    }

    [Fact]
    public void Parse_TalkWithNpc_MovesNpcToDirectObject()
    {
        var parsed = Parser.Parse("hablar con el enano");

        Assert.Equal("talk", parsed.Verb);
        Assert.Equal("enano", parsed.DirectObject);
        Assert.Null(parsed.IndirectObject);
        Assert.Equal(PrepositionKind.With, parsed.Preposition);
    }

    [Fact]
    public void Parse_UseKeepsPreposition_WhenBothObjectsPresent()
    {
        var parsed = Parser.Parse("usar llave con puerta oxidada");

        Assert.Equal("use", parsed.Verb);
        Assert.Equal("llave", parsed.DirectObject);
        Assert.Equal("puerta oxidada", parsed.IndirectObject);
        Assert.Equal(PrepositionKind.With, parsed.Preposition);
    }

    [Fact]
    public void Parse_WorldDictionaryOverridesGlobalAliases()
    {
        var dict = new
        {
            verbs = new Dictionary<string, string[]?>
            {
                ["pescar"] = new[] { "fish" }
            },
            nouns = new Dictionary<string, string[]?>
            {
                ["trucha"] = new[] { "trout" }
            }
        };

        var json = JsonSerializer.Serialize(dict);

        try
        {
            Parser.SetWorldDictionary(json);
            var parsed = Parser.Parse("fish la trout");

            Assert.Equal("pescar", parsed.Verb);
            Assert.Equal("trucha", parsed.DirectObject);
        }
        finally
        {
            Parser.SetWorldDictionary(null);
        }
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsEmptyCommand()
    {
        var parsed = Parser.Parse("");
        Assert.Equal(string.Empty, parsed.Verb);
        Assert.Null(parsed.DirectObject);
    }

    [Fact]
    public void Parse_WhitespaceOnlyInput_ReturnsEmptyCommand()
    {
        var parsed = Parser.Parse("   ");
        Assert.Equal(string.Empty, parsed.Verb);
        Assert.Null(parsed.DirectObject);
    }

    [Theory]
    [InlineData("norte", "n")]
    [InlineData("sur", "s")]
    [InlineData("este", "e")]
    [InlineData("oeste", "o")]
    [InlineData("arriba", "up")]
    [InlineData("abajo", "down")]
    [InlineData("subir", "up")]
    [InlineData("bajar", "down")]
    [InlineData("noreste", "ne")]
    [InlineData("noroeste", "no")]
    [InlineData("sureste", "se")]
    [InlineData("suroeste", "so")]
    public void Parse_DirectionWords_NormalizedCorrectly(string input, string expectedDirection)
    {
        var parsed = Parser.Parse(input);

        Assert.Equal("go", parsed.Verb);
        Assert.Equal(expectedDirection, parsed.DirectObject);
    }

    [Fact]
    public void Parse_OpenDoor_ParsesCorrectly()
    {
        var parsed = Parser.Parse("abrir puerta");

        Assert.Equal("open", parsed.Verb);
        Assert.Equal("puerta", parsed.DirectObject);
    }

    [Fact]
    public void Parse_CloseDoor_ParsesCorrectly()
    {
        var parsed = Parser.Parse("cerrar puerta");

        Assert.Equal("close", parsed.Verb);
        Assert.Equal("puerta", parsed.DirectObject);
    }

    [Fact]
    public void Parse_TakeObject_ParsesCorrectly()
    {
        var parsed = Parser.Parse("coger espada");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
    }

    [Theory]
    [InlineData("coger")]
    [InlineData("toma")]
    [InlineData("coge")]
    [InlineData("tomar")]
    [InlineData("agarrar")]
    [InlineData("recoger")]
    public void Parse_TakeSynonyms_AllMapToTake(string verb)
    {
        var parsed = Parser.Parse($"{verb} objeto");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("objeto", parsed.DirectObject);
    }

    [Fact]
    public void Parse_DropObject_ParsesCorrectly()
    {
        var parsed = Parser.Parse("soltar espada");

        Assert.Equal("drop", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
    }

    [Theory]
    [InlineData("inventario")]
    [InlineData("inv")]
    [InlineData("i")]
    public void Parse_InventorySynonyms_AllMapToInventory(string verb)
    {
        var parsed = Parser.Parse(verb);

        Assert.Equal("inventory", parsed.Verb);
    }

    [Fact]
    public void Parse_ArticlesStripped_FromDirectObject()
    {
        var parsed = Parser.Parse("coger la espada");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
    }

    [Fact]
    public void Parse_MultipleArticlesStripped_FromDirectObject()
    {
        var parsed = Parser.Parse("examinar el viejo libro");

        Assert.Equal("examine", parsed.Verb);
        Assert.Equal("viejo libro", parsed.DirectObject);
    }

    [Fact]
    public void Parse_PunctuationStripped_FromInput()
    {
        var parsed = Parser.Parse("examinar la espada!");

        Assert.Equal("examine", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
    }

    [Fact]
    public void Parse_MultipleSpaces_Normalized()
    {
        var parsed = Parser.Parse("coger    la    espada");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
    }

    [Fact]
    public void Parse_PutObjectIn_ParsesCorrectly()
    {
        var parsed = Parser.Parse("meter espada en cofre");

        Assert.Equal("put", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
        Assert.Equal("cofre", parsed.IndirectObject);
        Assert.Equal(PrepositionKind.In, parsed.Preposition);
    }

    [Fact]
    public void Parse_GetObjectFrom_ParsesCorrectly()
    {
        var parsed = Parser.Parse("sacar espada de cofre");

        Assert.Equal("get_from", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
        Assert.Equal("cofre", parsed.IndirectObject);
        Assert.Equal(PrepositionKind.From, parsed.Preposition);
    }

    [Fact]
    public void Parse_GiveObjectTo_ParsesCorrectly()
    {
        var parsed = Parser.Parse("dar espada a enano");

        Assert.Equal("give", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);
        Assert.Equal("enano", parsed.IndirectObject);
        Assert.Equal(PrepositionKind.To, parsed.Preposition);
    }

    [Fact]
    public void Parse_HelpCommand_ParsesCorrectly()
    {
        var parsed = Parser.Parse("ayuda");

        Assert.Equal("help", parsed.Verb);
        Assert.Null(parsed.DirectObject);
    }

    [Fact]
    public void Parse_UnknownVerb_PreservedAsIs()
    {
        var parsed = Parser.Parse("bailar");

        Assert.Equal("bailar", parsed.Verb);
        Assert.Null(parsed.DirectObject);
    }

    #region OriginalDirectObject Tests

    [Fact]
    public void Parse_NounAlias_PreservesOriginalValue()
    {
        // "sable" is aliased to "espada" in global aliases
        var parsed = Parser.Parse("coger sable");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);  // Normalized
        Assert.Equal("sable", parsed.OriginalDirectObject);  // Original preserved
    }

    [Fact]
    public void Parse_NoNounAlias_OriginalMatchesNormalized()
    {
        var parsed = Parser.Parse("coger libro");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("libro", parsed.DirectObject);
        Assert.Equal("libro", parsed.OriginalDirectObject);
    }

    [Fact]
    public void Parse_CompoundNoun_PreservesOriginal()
    {
        // "sable oxidado" - compound noun doesn't get partial alias replacement
        // Only exact matches in the alias dictionary are replaced
        var parsed = Parser.Parse("coger sable oxidado");

        Assert.Equal("take", parsed.Verb);
        Assert.Equal("sable oxidado", parsed.DirectObject);  // No aliasing for compound nouns
        Assert.Equal("sable oxidado", parsed.OriginalDirectObject);  // Same as normalized
    }

    [Fact]
    public void Parse_IndirectObject_PreservesOriginal()
    {
        var parsed = Parser.Parse("meter hoja en cofre");

        Assert.Equal("put", parsed.Verb);
        Assert.Equal("espada", parsed.DirectObject);  // "hoja" aliased to "espada"
        Assert.Equal("hoja", parsed.OriginalDirectObject);  // Original
        Assert.Equal("cofre", parsed.IndirectObject);
        Assert.Equal("cofre", parsed.OriginalIndirectObject);
    }

    [Fact]
    public void Parse_TalkWithNpcAlias_PreservesOriginal()
    {
        // "barbudo" aliased to "enano"
        var parsed = Parser.Parse("hablar con barbudo");

        Assert.Equal("talk", parsed.Verb);
        Assert.Equal("enano", parsed.DirectObject);  // Normalized
        Assert.Equal("barbudo", parsed.OriginalDirectObject);  // Original
    }

    #endregion

    #region Read Command Tests

    [Fact]
    public void Parse_ReadCommand_ParsesCorrectly()
    {
        var parsed = Parser.Parse("leer libro");

        Assert.Equal("read", parsed.Verb);
        Assert.Equal("libro", parsed.DirectObject);
    }

    [Theory]
    [InlineData("leer")]
    [InlineData("lee")]
    public void Parse_ReadSynonyms_AllMapToRead(string verb)
    {
        var parsed = Parser.Parse($"{verb} pergamino");

        Assert.Equal("read", parsed.Verb);
        Assert.Equal("pergamino", parsed.DirectObject);
    }

    #endregion

    #region Quest Command Tests

    [Fact]
    public void Parse_QuestsCommand_ParsesCorrectly()
    {
        var parsed = Parser.Parse("misiones");

        Assert.Equal("quests", parsed.Verb);
    }

    [Theory]
    [InlineData("misiones")]
    [InlineData("mision")]
    [InlineData("misión")]
    [InlineData("quest")]
    public void Parse_QuestSynonyms_AllMapToQuests(string verb)
    {
        var parsed = Parser.Parse(verb);

        Assert.Equal("quests", parsed.Verb);
    }

    #endregion

    #region LookIn Command Tests

    [Fact]
    public void Parse_LookIn_SingleWordVerb_ParsesWithPreposition()
    {
        // Note: The parser only takes the first word as the verb, so "ver dentro de cofre"
        // becomes verb="ver", and the rest is parsed as the object with preposition
        var parsed = Parser.Parse("ver dentro de cofre");

        // "ver" is not aliased to anything, so it stays as "ver"
        Assert.Equal("ver", parsed.Verb);
        // "dentro de cofre" - "de" is a preposition, so it splits there
        Assert.Equal("dentro", parsed.DirectObject);
        Assert.Equal("cofre", parsed.IndirectObject);
        Assert.Equal(PrepositionKind.From, parsed.Preposition);
    }

    #endregion

    #region Compound Commands and Pronouns Tests

    [Fact]
    public void ParseAll_SingleCommand_ReturnsSingleResult()
    {
        var commands = Parser.ParseAll("coger espada");

        Assert.Single(commands);
        Assert.Equal("take", commands[0].Verb);
        Assert.Equal("espada", commands[0].DirectObject);
    }

    [Fact]
    public void ParseAll_CompoundCommand_SplitsCorrectly()
    {
        var commands = Parser.ParseAll("coger espada y abrir puerta");

        Assert.Equal(2, commands.Length);
        Assert.Equal("take", commands[0].Verb);
        Assert.Equal("espada", commands[0].DirectObject);
        Assert.Equal("open", commands[1].Verb);
        Assert.Equal("puerta", commands[1].DirectObject);
    }

    [Fact]
    public void ParseAll_CompoundWithPronoun_ResolvesPronoun()
    {
        // "coger el vaso y meterlo en el baul"
        // "meterlo" should resolve to "meter" + "vaso" (from previous command)
        var commands = Parser.ParseAll("coger el vaso y meterlo en el baul");

        Assert.Equal(2, commands.Length);
        Assert.Equal("take", commands[0].Verb);
        Assert.Equal("vaso", commands[0].DirectObject);
        Assert.Equal("put", commands[1].Verb);
        Assert.Equal("vaso", commands[1].DirectObject);  // Pronoun resolved!
        Assert.Equal("baul", commands[1].IndirectObject);
    }

    [Fact]
    public void ParseAll_ObjectWithY_DoesNotSplit()
    {
        // "blanco y negro" is an object, not two commands
        var commands = Parser.ParseAll("coger caja blanco y negro");

        Assert.Single(commands);
        Assert.Equal("take", commands[0].Verb);
        Assert.Equal("caja blanco y negro", commands[0].DirectObject);
    }

    [Fact]
    public void ParseAll_VerbWithPronoun_Meterlo_ParsesCorrectly()
    {
        // First establish context with a previous command
        Parser.ParseAll("coger libro");

        // Now "meterlo" should resolve to "meter libro"
        var commands = Parser.ParseAll("meterlo en caja");

        Assert.Single(commands);
        Assert.Equal("put", commands[0].Verb);
        Assert.Equal("libro", commands[0].DirectObject);
        Assert.Equal("caja", commands[0].IndirectObject);
    }

    [Fact]
    public void ParseAll_ThreeCommands_AllParsedCorrectly()
    {
        var commands = Parser.ParseAll("coger llave y abrir puerta y ir al norte");

        Assert.Equal(3, commands.Length);
        Assert.Equal("take", commands[0].Verb);
        Assert.Equal("llave", commands[0].DirectObject);
        Assert.Equal("open", commands[1].Verb);
        Assert.Equal("puerta", commands[1].DirectObject);
        Assert.Equal("go", commands[2].Verb);
        Assert.Equal("norte", commands[2].DirectObject);
    }

    [Fact]
    public void ClearPronounContext_ResetsLastObject()
    {
        // Set up context
        Parser.ParseAll("coger espada");

        // Clear context
        Parser.ClearPronounContext();

        // Now "dejarla" has no previous object to reference
        var commands = Parser.ParseAll("dejarla");

        Assert.Single(commands);
        Assert.Equal("drop", commands[0].Verb);
        // Without context, the pronoun can't be resolved
        Assert.Null(commands[0].DirectObject);
    }

    #endregion
}
