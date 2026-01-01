namespace XiloAdventures.Engine.Models.Enums;

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
