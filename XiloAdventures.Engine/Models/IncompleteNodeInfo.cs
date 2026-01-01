using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Información sobre un nodo de script con datos incompletos.
/// Usado por el sistema de validación para identificar nodos que necesitan
/// que el diseñador complete sus propiedades obligatorias.
/// </summary>
public class IncompleteNodeInfo
{
    /// <summary>
    /// ID único del nodo incompleto.
    /// Permite localizar el nodo en el editor de scripts.
    /// </summary>
    public string NodeId { get; set; } = "";

    /// <summary>
    /// Nombre de visualización del tipo de nodo (ej: "Mostrar Mensaje", "Dar Objeto").
    /// Se muestra en los mensajes de error para ayudar al usuario.
    /// </summary>
    public string NodeDisplayName { get; set; } = "";

    /// <summary>
    /// Lista de nombres de propiedades que faltan por completar.
    /// Cada entrada es el DisplayName de la propiedad, no su nombre interno.
    /// </summary>
    public List<string> MissingProperties { get; set; } = new();
}
