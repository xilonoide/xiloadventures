using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XiloAdventures.Wpf.Common.Utilities;

public static class DockerDesktopCleaner
{
    public sealed record StepResult(string Step, bool Success, string Message);
    public sealed record CleanupResult(bool Success, IReadOnlyList<StepResult> Steps);

    /// <summary>
    /// Limpieza agresiva de Docker Desktop en Windows.
    /// - Desregistra las distros WSL docker-desktop y docker-desktop-data si existen.
    /// - Borra carpetas típicas de Docker Desktop.
    /// 
    /// Recomendación: ejecutar con privilegios de admin para evitar fallos en ProgramData.
    /// 
    /// IMPORTANTE: Esto eliminará imágenes/volúmenes/containers de Docker Desktop.
    /// </summary>
    public static async Task<CleanupResult> CleanDockerDesktopHardAsync(
        bool confirmDestructive,
        CancellationToken ct = default)
    {
        var steps = new List<StepResult>();

        if (!confirmDestructive)
        {
            steps.Add(new StepResult(
                "Guard",
                false,
                "confirmDestructive=false. Limpieza cancelada para evitar borrado accidental."));
            return new CleanupResult(false, steps);
        }

        // 0) Desinstalar Docker Desktop si el instalador existe
        var installerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Docker", "Docker", "Docker Desktop Installer.exe");
        if (File.Exists(installerPath))
        {
            try
            {
                // Ejecutamos el desinstalador. Ojo: esto puede mostrar UI y requerir interacción.
                var (code, stdout, stderr) = await RunProcessAsync(
                    installerPath,
                    "uninstall",
                    ct);

                if (code == 0)
                {
                    steps.Add(new StepResult("Uninstall Docker Desktop", true, "Desinstalador ejecutado correctamente."));
                }
                else
                {
                    steps.Add(new StepResult("Uninstall Docker Desktop", false,
                       $"El desinstalador retornó código {code}. Output: {TrimForLog(stdout)} {TrimForLog(stderr)}"));
                }
            }
            catch (Exception ex)
            {
                steps.Add(new StepResult("Uninstall Docker Desktop", false, ex.Message));
            }
        }
        else
        {
            steps.Add(new StepResult("Uninstall Docker Desktop", true, $"No se encontró el desinstalador (quizá ya no está instalado). Ruta: {installerPath}"));
        }

        // 1) Listar distros WSL
        string wslListOutput;
        try
        {
            var (code, stdout, stderr) = await RunProcessAsync(
                fileName: "wsl.exe",
                arguments: "-l -v",
                ct: ct);

            wslListOutput = stdout + "\n" + stderr;

            if (code != 0)
            {
                steps.Add(new StepResult("WSL list", false,
                    $"No se pudo listar WSL (code={code}). Output: {TrimForLog(wslListOutput)}"));
            }
            else
            {
                steps.Add(new StepResult("WSL list", true, "Listado de WSL obtenido."));
            }
        }
        catch (Exception ex)
        {
            steps.Add(new StepResult("WSL list", false, ex.Message));
            // Si no podemos listar, aún podemos intentar desregistrar a ciegas
            wslListOutput = string.Empty;
        }

        bool HasDistro(string name) =>
            wslListOutput.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;

        // 2) Unregister distros si existen (o intentar igualmente)
        foreach (var distro in new[] { "docker-desktop", "docker-desktop-data" })
        {
            try
            {
                // Si no pudimos listar, intentamos igualmente.
                if (string.IsNullOrWhiteSpace(wslListOutput) || HasDistro(distro))
                {
                    var (code, stdout, stderr) = await RunProcessAsync(
                        "wsl.exe",
                        $"--unregister {distro}",
                        ct);

                    if (code == 0)
                    {
                        steps.Add(new StepResult($"WSL unregister {distro}", true, "OK"));
                    }
                    else
                    {
                        // Si no existe, WSL suele devolver error; lo anotamos sin tumbar toda la limpieza.
                        var msg = $"{TrimForLog(stdout)} {TrimForLog(stderr)}".Trim();
                        steps.Add(new StepResult($"WSL unregister {distro}", false,
                            $"code={code}. {msg}"));
                    }
                }
                else
                {
                    steps.Add(new StepResult($"WSL unregister {distro}", true, "No estaba presente."));
                }
            }
            catch (Exception ex)
            {
                steps.Add(new StepResult($"WSL unregister {distro}", false, ex.Message));
            }
        }

        // 3) Borrar carpetas típicas
        var paths = new[]
        {
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Docker"),
            Environment.ExpandEnvironmentVariables(@"%APPDATA%\Docker"),
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Docker Desktop"),
            Environment.ExpandEnvironmentVariables(@"%APPDATA%\Docker Desktop"),
            Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\DockerDesktop"),
            Environment.ExpandEnvironmentVariables(@"%PROGRAMDATA%\Docker"),
        };

        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // Borrado recursivo
                    Directory.Delete(path, recursive: true);
                    steps.Add(new StepResult($"Delete dir {path}", true, "Borrado."));
                }
                else
                {
                    steps.Add(new StepResult($"Delete dir {path}", true, "No existe."));
                }
            }
            catch (Exception ex)
            {
                steps.Add(new StepResult($"Delete dir {path}", false, ex.Message));
            }
        }

        // 4) Resultado global
        var success = steps.All(s => s.Success || s.Step.StartsWith("WSL unregister", StringComparison.OrdinalIgnoreCase));
        // Nota: unregister puede fallar si la distro no existe; lo consideramos "no crítico"
        // si el resto fue bien. Ajusta esta lógica a tu gusto.

        return new CleanupResult(success, steps);
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunProcessAsync(
        string fileName,
        string arguments,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi, EnableRaisingEvents = true };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);

        p.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        p.Exited += (_, __) => tcs.TrySetResult(p.ExitCode);

        if (!p.Start())
            throw new InvalidOperationException($"No se pudo iniciar proceso: {fileName}");

        p.BeginOutputReadLine();
        p.BeginErrorReadLine();

        using (ct.Register(() =>
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); }
            catch { /* ignore */ }
            tcs.TrySetCanceled(ct);
        }))
        {
            var exitCode = await tcs.Task.ConfigureAwait(false);
            return (exitCode, stdout.ToString(), stderr.ToString());
        }
    }

    private static string TrimForLog(string s, int max = 300)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim();
        return s.Length <= max ? s : s.Substring(0, max) + " ...";
    }
}
