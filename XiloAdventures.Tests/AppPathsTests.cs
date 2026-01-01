using System;
using System.IO;
using XiloAdventures.Wpf.Common.Ui;
using Xunit;

public class AppPathsTests
{
    [Fact]
    public void EnsureDirectories_CreatesWorldsAndSaves()
    {
        // WorldsFolder está en BaseDirectory/worlds
        // SavesFolder está en Documents/xiloadventures
        var worlds = AppPaths.WorldsFolder;
        var saves = AppPaths.SavesFolder;

        // Nota: No borramos los directorios porque pueden contener datos del usuario
        // Solo verificamos que EnsureDirectories los crea si no existen

        AppPaths.EnsureDirectories();

        Assert.True(Directory.Exists(worlds), $"WorldsFolder should exist: {worlds}");
        Assert.True(Directory.Exists(saves), $"SavesFolder should exist: {saves}");
    }

    [Fact]
    public void WorldConfigPath_IncludesWorldId()
    {
        var id = Guid.NewGuid().ToString("N");
        var path = AppPaths.WorldConfigPath(id);

        Assert.Contains(id, path, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".xac", path, StringComparison.OrdinalIgnoreCase);
    }
}
