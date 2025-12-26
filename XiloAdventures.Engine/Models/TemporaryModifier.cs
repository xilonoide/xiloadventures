using System;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Modificador temporal aplicado a un estado del jugador.
/// Permite efectos positivos (buffs) o negativos (debuffs) con duración limitada.
/// </summary>
/// <remarks>
/// Los modificadores pueden ser:
/// - Recurrentes: aplican su efecto cada turno/segundo (ej: veneno, regeneración)
/// - Estáticos: dan un bonus/penalty mientras están activos (ej: bendición de fuerza)
///
/// La duración puede medirse en turnos (basado en acciones del jugador)
/// o en segundos reales, o ser permanente hasta que se elimine explícitamente.
/// </remarks>
public class TemporaryModifier
{
    /// <summary>
    /// Identificador único del modificador.
    /// Generado automáticamente al crear el modificador.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Nombre descriptivo del modificador (ej: "Veneno", "Bendición de Fuerza").
    /// Se muestra al jugador en la interfaz de estados activos.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Estado del jugador que modifica.
    /// Determina qué estadística se ve afectada (Salud, Mana, Hambre, etc.).
    /// </summary>
    public PlayerStateType StateType { get; set; }

    /// <summary>
    /// Cantidad a modificar por aplicación.
    /// Positivo: beneficio (curación, buff).
    /// Negativo: perjuicio (daño, debuff).
    /// </summary>
    public int Amount { get; set; }

    /// <summary>
    /// Tipo de duración del modificador.
    /// Turns: basado en turnos del jugador.
    /// Seconds: basado en tiempo real.
    /// Permanent: hasta eliminación explícita.
    /// </summary>
    public ModifierDurationType DurationType { get; set; }

    /// <summary>
    /// Duración restante (turnos o segundos según DurationType).
    /// Para modificadores permanentes, este valor se ignora.
    /// </summary>
    public int RemainingDuration { get; set; }

    /// <summary>
    /// Momento en que se aplicó el modificador.
    /// Usado para calcular expiración en modo Seconds.
    /// </summary>
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Si es true, el modificador se aplica cada turno/segundo.
    /// Si es false, es un bonus/penalty estático que solo afecta mientras está activo.
    /// </summary>
    /// <example>
    /// Recurrente true: Veneno (-5 salud cada turno).
    /// Recurrente false: Bendición (+3 fuerza mientras dure).
    /// </example>
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Indica si el modificador ya expiró y debe ser removido.
    /// Los modificadores permanentes nunca expiran automáticamente.
    /// </summary>
    [Browsable(false)]
    public bool IsExpired
    {
        get
        {
            if (DurationType == ModifierDurationType.Permanent)
                return false;
            if (DurationType == ModifierDurationType.Turns)
                return RemainingDuration <= 0;
            // Para Seconds, calculamos basado en tiempo transcurrido
            var elapsed = (DateTime.UtcNow - AppliedAt).TotalSeconds;
            return elapsed >= RemainingDuration;
        }
    }
}
