using System.Collections.Generic;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa una carpeta para organizar elementos en el editor.
/// Las carpetas son solo para organización visual, no afectan al juego.
/// </summary>
public class EditorFolder
{
    /// <summary>
    /// Identificador único de la carpeta.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre de la carpeta mostrado en el árbol.
    /// </summary>
    public string Name { get; set; } = "Nueva carpeta";

    /// <summary>
    /// Tipo de contenido que puede contener esta carpeta.
    /// </summary>
    public EditorFolderType FolderType { get; set; }

    /// <summary>
    /// ID de la carpeta padre (null si está en la raíz).
    /// </summary>
    public string? ParentFolderId { get; set; }

    /// <summary>
    /// IDs de los elementos contenidos en esta carpeta.
    /// </summary>
    public List<string> ItemIds { get; set; } = new();
}

/// <summary>
/// Tipos de carpeta según el contenido que pueden albergar.
/// </summary>
public enum EditorFolderType
{
    /// <summary>Carpeta para objetos.</summary>
    Objects,
    /// <summary>Carpeta para NPCs.</summary>
    Npcs,
    /// <summary>Carpeta para salas.</summary>
    Rooms,
    /// <summary>Carpeta para misiones.</summary>
    Quests
}
