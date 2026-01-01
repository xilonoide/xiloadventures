namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estadísticas de combate para NPCs.
/// Define los atributos que afectan el rendimiento del NPC en combate,
/// incluyendo estadísticas ofensivas, defensivas y de salud.
/// </summary>
/// <remarks>
/// El sistema de combate usa tiradas de D20 con bonificadores basados en estas estadísticas.
/// El bonus de cada estadística se calcula como: valor / 5 (redondeado hacia abajo).
/// Ejemplo: Fuerza 15 = bonus +3 al daño físico.
/// </remarks>
public class CombatStats
{
    /// <summary>
    /// Fuerza del NPC (afecta daño físico cuerpo a cuerpo).
    /// Bonus de daño = Strength / 5.
    /// Valor por defecto: 5 (bonus +1).
    /// </summary>
    public int Strength { get; set; } = 5;

    /// <summary>
    /// Destreza del NPC (afecta precisión y evasión).
    /// Bonus de ataque/defensa = Dexterity / 5.
    /// Valor por defecto: 5 (bonus +1).
    /// </summary>
    public int Dexterity { get; set; } = 5;

    /// <summary>
    /// Inteligencia del NPC (afecta daño mágico y habilidades especiales).
    /// Bonus de magia = Intelligence / 5.
    /// Valor por defecto: 5 (bonus +1).
    /// </summary>
    public int Intelligence { get; set; } = 5;

    /// <summary>
    /// Salud máxima del NPC.
    /// Determina cuánto daño puede recibir antes de morir.
    /// Valor por defecto: 10.
    /// </summary>
    public int MaxHealth { get; set; } = 10;

    /// <summary>
    /// Salud actual del NPC.
    /// Cuando llega a 0 o menos, el NPC muere y se convierte en cadáver (IsCorpse = true).
    /// Valor por defecto: 10.
    /// </summary>
    public int CurrentHealth { get; set; } = 10;
}
