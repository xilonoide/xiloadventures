using System.Collections.Generic;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Definición de una misión del juego.
/// </summary>
public class QuestDefinition
{
    /// <summary>
    /// Identificador único de la misión.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la misión que se muestra al jugador.
    /// </summary>
    public string Name { get; set; } = "Misión sin nombre";

    /// <summary>
    /// Descripción detallada de la misión.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Si es true, es una misión principal. Si es false, es secundaria.
    /// </summary>
    public bool IsMainQuest { get; set; } = true;

    /// <summary>
    /// Lista de objetivos de la misión.
    /// </summary>
    public List<string> Objectives { get; set; } = new();
}
