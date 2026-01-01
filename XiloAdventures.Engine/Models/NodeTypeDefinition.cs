using System;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Definición de un tipo de nodo disponible en el editor visual.
/// Contiene toda la información necesaria para crear instancias del nodo,
/// incluyendo sus puertos, propiedades y restricciones de uso.
/// </summary>
/// <remarks>
/// Los tipos de nodo se registran en NodeTypeRegistry y se usan para:
/// - Mostrar los nodos disponibles en la paleta del editor
/// - Crear nuevos nodos con la configuración correcta
/// - Validar conexiones entre puertos
/// - Ejecutar la lógica del nodo en tiempo de ejecución
/// </remarks>
public class NodeTypeDefinition
{
    /// <summary>
    /// Identificador único del tipo de nodo.
    /// Corresponde a un valor del enum NodeTypeId.
    /// </summary>
    public NodeTypeId TypeId { get; set; }

    /// <summary>
    /// Nombre legible para mostrar en el editor.
    /// Ejemplo: "Mostrar Mensaje", "Dar Objeto", "Si Tiene Objeto".
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Descripción detallada del nodo para mostrar en tooltips.
    /// Explica qué hace el nodo y cómo usarlo.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Categoría del nodo: Event, Condition, Action, Flow, Variable o Dialogue.
    /// Determina el color y la sección en la paleta del editor.
    /// </summary>
    public NodeCategory Category { get; set; }

    /// <summary>
    /// Subgrupo del nodo dentro de su categoría.
    /// Permite organizar nodos relacionados en subsecciones del editor.
    /// </summary>
    public NodeSubgroup Subgroup { get; set; } = NodeSubgroup.None;

    /// <summary>
    /// Tipos de entidades que pueden usar este nodo (flags).
    /// Un nodo con Room | Npc solo aparecerá en scripts de habitaciones o NPCs.
    /// </summary>
    public NodeOwnerType OwnerTypes { get; set; } = NodeOwnerType.None;

    /// <summary>
    /// Puertos de entrada del nodo.
    /// Incluyen el puerto de ejecución "Exec" y puertos de datos.
    /// </summary>
    public NodePort[] InputPorts { get; set; } = Array.Empty<NodePort>();

    /// <summary>
    /// Puertos de salida del nodo.
    /// Incluyen puertos de ejecución (como "True"/"False") y datos.
    /// </summary>
    public NodePort[] OutputPorts { get; set; } = Array.Empty<NodePort>();

    /// <summary>
    /// Propiedades editables del nodo.
    /// Se muestran en el panel de propiedades cuando el nodo está seleccionado.
    /// </summary>
    public NodePropertyDefinition[] Properties { get; set; } = Array.Empty<NodePropertyDefinition>();

    /// <summary>
    /// Característica del juego requerida para que este nodo esté disponible.
    /// Permite ocultar nodos de sistemas opcionales como comercio o combate.
    /// </summary>
    public RequiredFeature RequiredFeature { get; set; } = RequiredFeature.None;
}
