namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estados dinámicos del jugador que varían durante el juego.
/// Todos los valores de necesidades van de 0 a 100 (porcentaje).
/// </summary>
/// <remarks>
/// Los acumuladores internos permiten cambios fraccionarios en las necesidades,
/// que se redondean al actualizar los valores visibles.
/// El sistema de necesidades básicas puede desactivarse en GameInfo.BasicNeedsEnabled.
/// </remarks>
public class PlayerDynamicStats
{
    /// <summary>
    /// Salud actual del jugador.
    /// 0 = muerto, valor máximo = MaxHealth.
    /// </summary>
    public int Health { get; set; } = 100;

    /// <summary>
    /// Salud máxima del jugador.
    /// Determina cuánto daño puede recibir antes de morir.
    /// </summary>
    public int MaxHealth { get; set; } = 100;

    /// <summary>
    /// Nivel de hambre (0 = lleno, 100 = muriendo de hambre).
    /// Aumenta con el tiempo según HungerRate.
    /// Valores altos causan pérdida de salud y energía.
    /// </summary>
    public int Hunger { get; set; } = 0;

    /// <summary>
    /// Nivel de sed (0 = hidratado, 100 = deshidratado).
    /// Aumenta más rápido que el hambre según ThirstRate.
    /// Valores altos causan pérdida de salud y cordura.
    /// </summary>
    public int Thirst { get; set; } = 0;

    /// <summary>
    /// Nivel de energía (0 = exhausto, 100 = lleno de energía).
    /// Disminuye con acciones físicas y aumenta al descansar.
    /// Valores bajos reducen la efectividad en combate.
    /// </summary>
    public int Energy { get; set; } = 100;

    /// <summary>
    /// Nivel de sueño/cansancio (0 = descansado, 100 = agotamiento extremo).
    /// Aumenta con el tiempo y disminuye al dormir.
    /// Valores altos pueden causar desmayos.
    /// </summary>
    public int Sleep { get; set; } = 0;

    /// <summary>
    /// Nivel de cordura/salud mental (0 = locura, 100 = mente sana).
    /// Disminuye con eventos traumáticos y aumenta al descansar o con ciertos items.
    /// Valores bajos pueden causar alucinaciones o acciones involuntarias.
    /// </summary>
    public int Sanity { get; set; } = 100;

    /// <summary>
    /// Nivel de mana/poder mágico (0 = sin magia, MaxMana = máximo poder).
    /// Se consume al usar habilidades mágicas y se regenera con el tiempo o items.
    /// </summary>
    public int Mana { get; set; } = 100;

    /// <summary>
    /// Mana máximo del jugador.
    /// Determina cuántas habilidades mágicas puede usar antes de necesitar recargar.
    /// </summary>
    public int MaxMana { get; set; } = 100;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de hambre.
    /// Permite cambios más precisos que se acumulan hasta el siguiente entero.
    /// </summary>
    internal double HungerAccumulator { get; set; } = 0;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de sed.
    /// Permite cambios más precisos que se acumulan hasta el siguiente entero.
    /// </summary>
    internal double ThirstAccumulator { get; set; } = 0;

    /// <summary>
    /// Acumulador interno para incrementos fraccionarios de sueño.
    /// Permite cambios más precisos que se acumulan hasta el siguiente entero.
    /// </summary>
    internal double SleepAccumulator { get; set; } = 0;
}
