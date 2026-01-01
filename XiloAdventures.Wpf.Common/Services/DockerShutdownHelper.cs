using System.Diagnostics;

namespace XiloAdventures.Wpf.Common.Services;

/// <summary>
/// Helper para cerrar Docker Desktop por completo cuando se cierra la partida.
/// </summary>
public static class DockerShutdownHelper
{
    public static void TryShutdownDockerDesktop()
    {
        // En esta app solo nos importa Windows (WPF), así que usamos taskkill.
        // Si Docker Desktop no está instalado o ya está cerrado, simplemente
        // ignoramos los errores.
        TryKillProcessByImageName("Docker Desktop.exe");
        TryKillProcessByImageName("com.docker.backend.exe");
    }

    private static void TryKillProcessByImageName(string imageName)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "taskkill",
                Arguments = $"/IM \"{imageName}\" /F",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi);
            process?.WaitForExit(2000);
        }
        catch
        {
            // Ignoramos cualquier error; no queremos romper el cierre de la app
            // si por lo que sea no se puede matar el proceso.
        }
    }
}
