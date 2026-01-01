using System;
using System.IO;

namespace XiloAdventures.Wpf.Common.Ui;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;
    public static string WorldsFolder => Path.Combine(BaseDirectory, "worlds");

    /// <summary>
    /// Carpeta base de guardados: Documentos/xiloadventures
    /// </summary>
    public static string SavesFolder
    {
        get
        {
            var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "xiloadventures");
        }
    }

    public static string GlobalConfigPath => Path.Combine(SavesFolder, "config.xac");
    public static string WorldConfigPath(string worldId) => Path.Combine(SavesFolder, $"config_{worldId}.xac");

    /// <summary>
    /// Ruta de guardados para un mundo específico: Documentos/xiloadventures/{worldId}
    /// </summary>
    public static string WorldSavesFolder(string worldId)
    {
        return Path.Combine(SavesFolder, worldId);
    }

    /// <summary>
    /// Alias para compatibilidad con el player exportado.
    /// </summary>
    public static string PlayerSavesFolder(string worldId) => WorldSavesFolder(worldId);

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(WorldsFolder);
        Directory.CreateDirectory(SavesFolder);
    }

    public static void EnsureSavesDirectory(string worldId)
    {
        Directory.CreateDirectory(WorldSavesFolder(worldId));
    }

    /// <summary>
    /// Alias para compatibilidad con el player exportado.
    /// </summary>
    public static void EnsurePlayerSavesDirectory(string worldId) => EnsureSavesDirectory(worldId);
}
