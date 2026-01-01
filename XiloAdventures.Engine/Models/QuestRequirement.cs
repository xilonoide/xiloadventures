using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un requisito de misión para acceder a una sala o salida.
/// </summary>
public class QuestRequirement
{
    /// <summary>
    /// ID de la misión requerida.
    /// </summary>
    public string QuestId { get; set; } = string.Empty;

    /// <summary>
    /// Estado en el que debe estar la misión.
    /// </summary>
    public QuestStatus RequiredStatus { get; set; } = QuestStatus.Completed;
}
