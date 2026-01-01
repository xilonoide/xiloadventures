using System;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Información general de configuración del juego.
/// </summary>
public class GameInfo
{
    /// <summary>
    /// Identificador único del mundo (GUID generado automáticamente).
    /// </summary>
    [Browsable(false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Tema o ambientación del mundo (ej: "fantasía medieval", "ciencia ficción", "horror gótico").
    /// Se usa como contexto para la generación de contenido con IA.
    /// </summary>
    public string? Theme { get; set; }

    /// <summary>
    /// Título del juego que se muestra al jugador.
    /// </summary>
    public string Title { get; set; } = "Aventura sin título";

    /// <summary>
    /// ID de la sala donde comienza el jugador.
    /// </summary>
    public string StartRoomId { get; set; } = string.Empty;

    /// <summary>
    /// ID de la música de fondo global del mundo.
    /// </summary>
    public string? WorldMusicId { get; set; }

    /// <summary>
    /// Clave de cifrado para las partidas guardadas de los jugadores.
    /// Debe tener 8 caracteres.
    /// </summary>
    public string? EncryptionKey { get; set; }

    /// <summary>
    /// Fuente por defecto del juego.
    /// </summary>
    public string DefaultFontFamily { get; set; } = "Segoe UI";

    /// <summary>
    /// Hora inicial de la partida (0-23).
    /// </summary>
    public int StartHour { get; set; } = 9;

    /// <summary>
    /// Clima inicial del mundo.
    /// </summary>
    public WeatherType StartWeather { get; set; } = WeatherType.Despejado;

    /// <summary>
    /// Minutos reales que equivalen a 1 hora de juego (1-10).
    /// </summary>
    public int MinutesPerGameHour { get; set; } = 6;

    /// <summary>
    /// Diccionario de sinónimos por mundo para el parser (JSON).
    /// </summary>
    public string? ParserDictionaryJson { get; set; }

    /// <summary>
    /// Activar sonido en modo pruebas del editor.
    /// </summary>
    [Browsable(false)]
    public bool TestModeSoundEnabled { get; set; } = false;

    /// <summary>
    /// Volumen de música en modo pruebas (0-10).
    /// </summary>
    [Browsable(false)]
    public double TestModeMusicVolume { get; set; } = 5;

    /// <summary>
    /// Volumen de efectos en modo pruebas (0-10).
    /// </summary>
    [Browsable(false)]
    public double TestModeEffectsVolume { get; set; } = 5;

    /// <summary>
    /// Volumen de voz en modo pruebas (0-10).
    /// </summary>
    [Browsable(false)]
    public double TestModeVoiceVolume { get; set; } = 5;

    /// <summary>
    /// Volumen master en modo pruebas (1-10).
    /// </summary>
    [Browsable(false)]
    public double TestModeMasterVolume { get; set; } = 5;

    /// <summary>
    /// Activar IA en modo pruebas del editor.
    /// </summary>
    [Browsable(false)]
    public bool TestModeAiEnabled { get; set; } = false;

    /// <summary>
    /// Texto de introducción que se muestra al empezar una nueva partida (vacío = no mostrar).
    /// </summary>
    public string IntroText { get; set; } = "";

    /// <summary>
    /// Texto que se muestra al finalizar la aventura (vacío = texto por defecto).
    /// </summary>
    public string EndingText { get; set; } = "";

    /// <summary>
    /// ID de la música que suena al finalizar la aventura.
    /// </summary>
    public string? EndingMusicId { get; set; }

    #region Combat Configuration

    /// <summary>
    /// Activa el sistema de combate (salud, maná, energía, cordura).
    /// </summary>
    public bool CombatEnabled { get; set; } = false;

    /// <summary>
    /// Activa el sistema de magia (maná y habilidades mágicas). Requiere CombatEnabled.
    /// </summary>
    public bool MagicEnabled { get; set; } = false;

    #endregion

    #region Basic Needs Configuration

    /// <summary>
    /// Activa el sistema de necesidades básicas (hambre, sed, sueño).
    /// </summary>
    public bool BasicNeedsEnabled { get; set; } = false;

    /// <summary>
    /// Velocidad de incremento del hambre por turno.
    /// </summary>
    public NeedRate HungerRate { get; set; } = NeedRate.Normal;

    /// <summary>
    /// Velocidad de incremento de la sed por turno.
    /// </summary>
    public NeedRate ThirstRate { get; set; } = NeedRate.Normal;

    /// <summary>
    /// Velocidad de incremento del sueño por turno.
    /// </summary>
    public NeedRate SleepRate { get; set; } = NeedRate.Normal;

    /// <summary>
    /// Texto de muerte por hambre.
    /// </summary>
    public string HungerDeathText { get; set; } = "Has muerto de hambre. Tu cuerpo no pudo resistir más sin alimento.";

    /// <summary>
    /// Texto de muerte por sed.
    /// </summary>
    public string ThirstDeathText { get; set; } = "Has muerto de sed. La deshidratación ha acabado contigo.";

    /// <summary>
    /// Texto de muerte por agotamiento.
    /// </summary>
    public string SleepDeathText { get; set; } = "Has muerto de agotamiento. Tu cuerpo colapsó por falta de sueño.";

    /// <summary>
    /// Texto de muerte por perder toda la salud.
    /// </summary>
    public string HealthDeathText { get; set; } = "Has muerto. Tus heridas fueron demasiado graves.";

    /// <summary>
    /// Texto de muerte por perder toda la cordura.
    /// </summary>
    public string SanityDeathText { get; set; } = "Tu mente se ha quebrado. La locura te consume por completo.";

    #endregion

    #region Crafting Configuration

    /// <summary>
    /// Activa el sistema de fabricación (crear objetos combinando ingredientes).
    /// </summary>
    public bool CraftingEnabled { get; set; } = false;

    #endregion
}
