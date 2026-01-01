using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using WixToolset.Dtf.WindowsInstaller;

namespace XiloAdventures.Wpf.Installer.CustomActions;

public class CustomActions
{
    // Win32 MessageBox API directa para garantizar visibilidad
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    // Constantes para MessageBox
    private const uint MB_YESNO = 0x00000004;
    private const uint MB_ICONQUESTION = 0x00000020;
    private const uint MB_ICONWARNING = 0x00000030;
    private const uint MB_DEFBUTTON2 = 0x00000100;
    private const uint MB_TOPMOST = 0x00040000;
    private const uint MB_SETFOREGROUND = 0x00010000;
    private const uint MB_SYSTEMMODAL = 0x00001000;
    private const int IDYES = 6;

    /// <summary>
    /// Muestra un MessageBox Win32 que siempre aparece en primer plano.
    /// </summary>
    private static bool ShowTopMostYesNoDialog(string message, string title, bool isWarning = false)
    {
        uint flags = MB_YESNO | MB_DEFBUTTON2 | MB_TOPMOST | MB_SETFOREGROUND | MB_SYSTEMMODAL;
        flags |= isWarning ? MB_ICONWARNING : MB_ICONQUESTION;

        int result = MessageBoxW(IntPtr.Zero, message, title, flags);
        return result == IDYES;
    }

    /// <summary>
    /// Muestra un diálogo preguntando al usuario qué datos adicionales eliminar durante la desinstalación.
    /// </summary>
    [CustomAction]
    public static ActionResult ShowUninstallOptions(Session session)
    {
        try
        {
            var uiLevel = session["UILevel"];

            // Si no hay UI disponible (silent install), usar valores por defecto
            // UILevel: 2=none, 3=basic, 4=reduced, 5=full
            if (string.IsNullOrEmpty(uiLevel) || int.Parse(uiLevel) <= 2)
            {
                session["DELETESAVEDGAMES"] = "0";
                session["DELETEDOCKERDATA"] = "0";
                return ActionResult.Success;
            }

            try
            {
                bool deleteSaves = ShowTopMostYesNoDialog(
                    "¿Deseas eliminar también las partidas guardadas?\n\n" +
                    "(Se encuentran en Documentos\\xiloadventures)",
                    "Xilo Adventures - Desinstalación",
                    isWarning: false);

                session["DELETESAVEDGAMES"] = deleteSaves ? "1" : "0";

                bool deleteDocker = ShowTopMostYesNoDialog(
                    "¿Deseas eliminar los datos de Docker (IA)?\n\n" +
                    "Esto incluye:\n" +
                    "- Contenedores de Xilo Adventures\n" +
                    "- Imágenes de Docker descargadas\n" +
                    "- Datos WSL de Docker Desktop\n" +
                    "- Docker Desktop (si está instalado)\n\n" +
                    "ADVERTENCIA: Si usas Docker para otros proyectos,\n" +
                    "esto podría afectar a tus otros contenedores.",
                    "Xilo Adventures - Eliminar datos de Docker",
                    isWarning: true);

                session["DELETEDOCKERDATA"] = deleteDocker ? "1" : "0";
            }
            catch
            {
                session["DELETESAVEDGAMES"] = "0";
                session["DELETEDOCKERDATA"] = "0";
            }

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    /// <summary>
    /// Elimina el acceso directo del escritorio (método de respaldo).
    /// </summary>
    [CustomAction]
    public static ActionResult DeleteDesktopShortcut(Session session)
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var commonDesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory);

            var shortcutNames = new[] { "Xilo Adventures.lnk", "Xilo Adventures", "XiloAdventures.lnk" };
            var paths = new[] { desktopPath, commonDesktopPath };

            foreach (var basePath in paths)
            {
                foreach (var name in shortcutNames)
                {
                    var fullPath = Path.Combine(basePath, name);
                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            File.Delete(fullPath);
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    }
                }

                // También buscar con wildcard
                try
                {
                    var xiloShortcuts = Directory.GetFiles(basePath, "Xilo*.lnk");
                    foreach (var shortcut in xiloShortcuts)
                    {
                        try
                        {
                            File.Delete(shortcut);
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    }
                }
                catch
                {
                    // Ignorar errores
                }
            }

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    /// <summary>
    /// Elimina las partidas guardadas del usuario.
    /// </summary>
    [CustomAction]
    public static ActionResult DeleteSavedGames(Session session)
    {
        try
        {
            var customActionData = session.CustomActionData;

            // Si no se indicó que hay que eliminar, salir
            if (customActionData.Count == 0 || !customActionData.ContainsKey("DELETESAVEDGAMES") || customActionData["DELETESAVEDGAMES"] != "1")
            {
                return ActionResult.Success;
            }

            var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var savesPath = Path.Combine(documentsPath, "xiloadventures");

            if (Directory.Exists(savesPath))
            {
                Directory.Delete(savesPath, recursive: true);
            }

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    /// <summary>
    /// Elimina los datos de Docker: contenedores, imágenes, WSL distros y opcionalmente Docker Desktop.
    /// </summary>
    [CustomAction]
    public static ActionResult DeleteDockerData(Session session)
    {
        try
        {
            var customActionData = session.CustomActionData;

            // Si no se indicó que hay que eliminar, salir
            if (customActionData.Count == 0 || !customActionData.ContainsKey("DELETEDOCKERDATA") || customActionData["DELETEDOCKERDATA"] != "1")
            {
                return ActionResult.Success;
            }

            // 1. Detener contenedores de Xilo Adventures
            StopDockerContainers(session);

            // 2. Eliminar contenedores e imágenes de Xilo
            RemoveXiloDockerResources(session);

            // 3. Desinstalar Docker Desktop si existe
            UninstallDockerDesktop(session);

            // 4. Eliminar distros WSL de Docker
            UnregisterDockerWslDistros(session);

            // 5. Eliminar carpetas de Docker
            DeleteDockerFolders(session);

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    private static void StopDockerContainers(Session session)
    {
        var containers = new[] { "xilo-ollama", "xilo-tts", "xilo-stablediffusion" };

        foreach (var container in containers)
        {
            try
            {
                RunProcess("docker", $"stop {container}");
            }
            catch
            {
                // Ignorar errores
            }
        }
    }

    private static void RemoveXiloDockerResources(Session session)
    {
        var containers = new[] { "xilo-ollama", "xilo-tts", "xilo-stablediffusion" };

        foreach (var container in containers)
        {
            try
            {
                RunProcess("docker", $"rm -f {container}");
            }
            catch
            {
                // Ignorar errores
            }
        }

        // Eliminar volúmenes de Xilo por nombre
        var volumes = new[] { "xilo-ollama", "xilo-tts", "xilo-stablediffusion" };
        foreach (var volume in volumes)
        {
            try
            {
                RunProcess("docker", $"volume rm -f {volume}");
            }
            catch
            {
                // Ignorar errores
            }
        }

        // Eliminar imágenes relacionadas con Xilo
        var images = new[]
        {
            "ollama/ollama",
            "ollama/ollama:latest",
            "ghcr.io/coqui-ai/tts:latest",
            "ghcr.io/idiap/coqui-tts-cpu:latest",
            "coqui/tts",
            "stability-ai/stable-diffusion"
        };
        foreach (var image in images)
        {
            try
            {
                RunProcess("docker", $"rmi -f {image}");
            }
            catch
            {
                // Ignorar errores
            }
        }

        // Limpiar volúmenes huérfanos
        try
        {
            RunProcess("docker", "volume prune -f");
        }
        catch
        {
            // Ignorar errores
        }

        // Limpiar imágenes huérfanas
        try
        {
            RunProcess("docker", "image prune -f");
        }
        catch
        {
            // Ignorar errores
        }
    }

    private static void UninstallDockerDesktop(Session session)
    {
        var installerPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "Docker", "Docker", "Docker Desktop Installer.exe");

        if (File.Exists(installerPath))
        {
            try
            {
                // Primero intentamos cerrar Docker Desktop
                RunProcess("taskkill", "/IM \"Docker Desktop.exe\" /F");
                RunProcess("taskkill", "/IM \"com.docker.backend.exe\" /F");

                // Ejecutar el desinstalador
                RunProcess(installerPath, "uninstall --quiet");
            }
            catch
            {
                // Ignorar errores
            }
        }
    }

    private static void UnregisterDockerWslDistros(Session session)
    {
        var distros = new[] { "docker-desktop", "docker-desktop-data" };

        foreach (var distro in distros)
        {
            try
            {
                RunProcess("wsl.exe", $"--unregister {distro}");
            }
            catch
            {
                // Ignorar errores
            }
        }
    }

    private static void DeleteDockerFolders(Session session)
    {
        var folders = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Docker"),
            Environment.ExpandEnvironmentVariables(@"%APPDATA%\Docker"),
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Docker Desktop"),
            Environment.ExpandEnvironmentVariables(@"%APPDATA%\Docker Desktop"),
            Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\DockerDesktop"),
            Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\Docker"),
        };

        foreach (var folder in folders)
        {
            try
            {
                if (Directory.Exists(folder))
                {
                    Directory.Delete(folder, recursive: true);
                }
            }
            catch
            {
                // Ignorar errores
            }
        }
    }

    private static void RunProcess(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit(30000); // 30 segundos de timeout
            }
        }
        catch
        {
            // Ignorar errores
        }
    }
}
