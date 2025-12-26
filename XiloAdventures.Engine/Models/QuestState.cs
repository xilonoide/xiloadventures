using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Estado actual de una misión en la partida del jugador.
/// Rastrea el progreso y estado de una misión específica.
/// </summary>
/// <remarks>
/// Cada misión definida (QuestDefinition) puede tener un QuestState correspondiente
/// en GameState.Quests una vez que el jugador la descubre o inicia.
/// Las misiones no descubiertas no tienen QuestState asociado.
/// </remarks>
public class QuestState
{
    /// <summary>
    /// ID de la misión.
    /// Debe coincidir con el Id de una QuestDefinition en el mundo.
    /// </summary>
    public string QuestId { get; set; } = string.Empty;

    /// <summary>
    /// Estado actual de la misión.
    /// NotStarted: el jugador conoce la misión pero no la ha aceptado.
    /// InProgress: el jugador está trabajando activamente en ella.
    /// Completed: todos los objetivos se han cumplido.
    /// Failed: la misión ya no se puede completar.
    /// </summary>
    public QuestStatus Status { get; set; } = QuestStatus.NotStarted;

    /// <summary>
    /// Índice del objetivo actual (para misiones con múltiples objetivos secuenciales).
    /// Comienza en 0 y avanza al completar cada objetivo.
    /// </summary>
    public int CurrentObjectiveIndex { get; set; }
}
