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
                    "- Volúmenes de datos de IA\n\n" +
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

            // 2. Eliminar contenedores, imágenes y volúmenes de Xilo
            RemoveXiloDockerResources(session);

            return ActionResult.Success;
        }
        catch
        {
            return ActionResult.Success;
        }
    }

    private static void StopDockerContainers(Session session)
    {
        var containers = new[] { "xilo-ollama", "xilo-tts", "xilo-stablediffusion", "xilo-linux-test" };

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
        // Eliminar contenedores de Xilo
        var containers = new[] { "xilo-ollama", "xilo-tts", "xilo-stablediffusion", "xilo-linux-test" };
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

        // Eliminar volúmenes de Xilo por nombre (incluido cache de Stable Diffusion)
        var volumes = new[]
        {
            "xilo-ollama",
            "xilo-tts",
            "xilo-stablediffusion",
            "xilo-stablediffusion_cache"
        };
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

        // Eliminar cualquier volumen que contenga "xilo"
        try
        {
            var output = RunProcessWithOutput("docker", "volume ls -q");
            if (!string.IsNullOrEmpty(output))
            {
                foreach (var vol in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (vol.IndexOf("xilo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try { RunProcess("docker", $"volume rm -f {vol}"); } catch { }
                    }
                }
            }
        }
        catch { }

        // Eliminar imágenes relacionadas con Xilo y las usadas por la IA
        var images = new[]
        {
            // Imágenes del modo pruebas Linux
            "xilo-linux-player",
            "xilo-linux-player:latest",
            // Imágenes de Ollama
            "ollama/ollama",
            "ollama/ollama:latest",
            // Imágenes de TTS
            "ghcr.io/coqui-ai/tts",
            "ghcr.io/coqui-ai/tts:latest",
            "ghcr.io/idiap/coqui-tts-cpu",
            "ghcr.io/idiap/coqui-tts-cpu:latest",
            "coqui/tts",
            // Imágenes de Stable Diffusion
            "stability-ai/stable-diffusion",
            "gadicc/diffusers-api",
            "gadicc/diffusers-api:latest",
            // Imágenes base usadas por Xilo
            "ubuntu:22.04",
            "nvidia/cuda:12.2.0-base-ubuntu22.04",
            // Imagen base del SDK de .NET usada para pruebas
            "mcr.microsoft.com/dotnet/sdk:8.0-jammy"
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

        // Buscar y eliminar cualquier imagen que contenga "xilo"
        try
        {
            var output = RunProcessWithOutput("docker", "images --format \"{{.Repository}}:{{.Tag}}\"");
            if (!string.IsNullOrEmpty(output))
            {
                foreach (var img in output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (img.IndexOf("xilo", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        try { RunProcess("docker", $"rmi -f {img}"); } catch { }
                    }
                }
            }
        }
        catch { }

        // Limpiar volúmenes huérfanos
        try
        {
            RunProcess("docker", "volume prune -f");
        }
        catch
        {
            // Ignorar errores
        }

        // Limpiar imágenes huérfanas (dangling)
        try
        {
            RunProcess("docker", "image prune -f");
        }
        catch
        {
            // Ignorar errores
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

    private static string RunProcessWithOutput(string fileName, string arguments)
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
                var output = process.StandardOutput.ReadToEnd();
                process.StandardError.ReadToEnd();
                process.WaitForExit(30000);
                return output;
            }
        }
        catch
        {
            // Ignorar errores
        }
        return string.Empty;
    }
}
