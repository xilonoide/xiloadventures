namespace XiloAdventures.Engine.Models;

/// <summary>
/// Tipos de clima disponibles en el mundo del juego.
/// </summary>
public enum WeatherType
{
    /// <summary>Cielo despejado, buen tiempo.</summary>
    Despejado,
    /// <summary>Lluvia ligera o moderada.</summary>
    Lluvioso,
    /// <summary>Cielo cubierto de nubes.</summary>
    Nublado,
    /// <summary>Tormenta con lluvia intensa y rayos.</summary>
    Tormenta
}

/// <summary>
/// Tipos de objetos del juego que determinan su comportamiento y uso.
/// </summary>
public enum ObjectType
{
    /// <summary>Objeto sin tipo específico.</summary>
    Ninguno,
    /// <summary>Arma equipable para combate.</summary>
    Arma,
    /// <summary>Armadura equipable para defensa.</summary>
    Armadura,
    /// <summary>Alimento consumible que reduce el hambre.</summary>
    Comida,
    /// <summary>Bebida consumible que reduce la sed.</summary>
    Bebida,
    /// <summary>Prenda de vestir.</summary>
    Ropa,
    /// <summary>Llave para abrir cerraduras.</summary>
    Llave,
    /// <summary>Documento legible (libro, carta, pergamino, etc.).</summary>
    Texto
}

/// <summary>
/// Tipos de daño para armas y habilidades de combate.
/// </summary>
public enum DamageType
{
    /// <summary>Daño físico (usa Fuerza).</summary>
    Physical,
    /// <summary>Daño mágico (usa Inteligencia).</summary>
    Magical,
    /// <summary>Daño perforante (ignora parte de la defensa).</summary>
    Piercing
}

/// <summary>
/// Género gramatical para artículos en español (el/la/los/las).
/// </summary>
public enum GrammaticalGender
{
    /// <summary>Género masculino (el, los, un, unos).</summary>
    Masculine,
    /// <summary>Género femenino (la, las, una, unas).</summary>
    Feminine
}

/// <summary>
/// Modo de movimiento para NPCs (patrulla y seguimiento).
/// </summary>
public enum MovementMode
{
    /// <summary>Movimiento basado en turnos del jugador.</summary>
    Turns,
    /// <summary>Movimiento basado en tiempo real (segundos).</summary>
    Time
}

/// <summary>
/// Velocidad de incremento de necesidades básicas.
/// </summary>
public enum NeedRate
{
    /// <summary>Incremento lento (modificador 0.5).</summary>
    Low,
    /// <summary>Incremento normal (modificador 1.0).</summary>
    Normal,
    /// <summary>Incremento rápido (modificador 1.5).</summary>
    High
}

/// <summary>
/// Estados posibles de una misión.
/// </summary>
public enum QuestStatus
{
    /// <summary>Misión no iniciada.</summary>
    NotStarted,
    /// <summary>Misión en progreso.</summary>
    InProgress,
    /// <summary>Misión completada exitosamente.</summary>
    Completed,
    /// <summary>Misión fallida.</summary>
    Failed
}

/// <summary>
/// Modo de duración para los modificadores temporales.
/// </summary>
public enum ModifierDurationType
{
    /// <summary>El modificador dura un número de turnos.</summary>
    Turns,
    /// <summary>El modificador dura un número de segundos (tiempo real).</summary>
    Seconds,
    /// <summary>El modificador no caduca (permanente hasta que se elimine).</summary>
    Permanent
}

/// <summary>
/// Tipo de estado del jugador que se puede modificar.
/// </summary>
public enum PlayerStateType
{
    /// <summary>Salud actual.</summary>
    Health,
    /// <summary>Salud máxima.</summary>
    MaxHealth,
    /// <summary>Nivel de hambre.</summary>
    Hunger,
    /// <summary>Nivel de sed.</summary>
    Thirst,
    /// <summary>Nivel de energía.</summary>
    Energy,
    /// <summary>Nivel de sueño/cansancio.</summary>
    Sleep,
    /// <summary>Nivel de cordura.</summary>
    Sanity,
    /// <summary>Mana actual.</summary>
    Mana,
    /// <summary>Mana máximo.</summary>
    MaxMana,
    /// <summary>Fuerza.</summary>
    Strength,
    /// <summary>Constitución.</summary>
    Constitution,
    /// <summary>Inteligencia.</summary>
    Intelligence,
    /// <summary>Destreza.</summary>
    Dexterity,
    /// <summary>Carisma.</summary>
    Charisma,
    /// <summary>Dinero.</summary>
    Gold
}

#region Combat Enums

/// <summary>
/// Fase actual del combate por turnos.
/// </summary>
public enum CombatPhase
{
    /// <summary>Determinando orden de turnos (tirada de iniciativa).</summary>
    Initiative,
    /// <summary>Esperando acción del jugador.</summary>
    PlayerAction,
    /// <summary>Jugador tirando dados de ataque/defensa.</summary>
    PlayerRoll,
    /// <summary>NPC eligiendo acción.</summary>
    NpcAction,
    /// <summary>NPC tirando dados.</summary>
    NpcRoll,
    /// <summary>Resolviendo daño del turno.</summary>
    Resolution,
    /// <summary>Fin de ronda (verificar victoria/derrota).</summary>
    RoundEnd,
    /// <summary>Jugador ganó el combate.</summary>
    Victory,
    /// <summary>Jugador perdió el combate.</summary>
    Defeat
}

/// <summary>
/// Acción que un combatiente puede realizar en su turno.
/// </summary>
public enum CombatAction
{
    /// <summary>Sin acción seleccionada.</summary>
    None,
    /// <summary>Atacar al enemigo.</summary>
    Attack,
    /// <summary>Postura defensiva (+5 defensa este turno).</summary>
    Defend,
    /// <summary>Intentar huir del combate.</summary>
    Flee,
    /// <summary>Usar un objeto del inventario.</summary>
    UseItem,
    /// <summary>Usar una habilidad especial (consume maná).</summary>
    UseAbility
}

/// <summary>
/// Tipo de entrada en el log de combate para formateo.
/// </summary>
public enum CombatLogType
{
    /// <summary>Mensaje normal.</summary>
    Normal,
    /// <summary>Ataque exitoso.</summary>
    Hit,
    /// <summary>Ataque fallido.</summary>
    Miss,
    /// <summary>Golpe crítico.</summary>
    Critical,
    /// <summary>Fallo épico.</summary>
    Fumble,
    /// <summary>Victoria.</summary>
    Victory,
    /// <summary>Derrota.</summary>
    Defeat,
    /// <summary>Huida exitosa.</summary>
    Fled,
    /// <summary>Información del sistema.</summary>
    System
}

/// <summary>
/// Tipo de habilidad de combate.
/// </summary>
public enum AbilityType
{
    /// <summary>Habilidad de ataque mágico.</summary>
    Attack,
    /// <summary>Habilidad de defensa mágica.</summary>
    Defense
}

#endregion
