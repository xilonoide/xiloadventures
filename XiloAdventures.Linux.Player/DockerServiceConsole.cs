using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;

namespace XiloAdventures.Linux.Player;

/// <summary>
/// Servicio para iniciar y verificar los servicios de IA en Docker.
/// En Linux nativo: inicia los contenedores de Ollama y TTS.
/// En Docker (modo pruebas): solo verifica que los servicios están disponibles en el host.
/// </summary>
public static class DockerServiceConsole
{
    // Detectar si estamos dentro de Docker (modo pruebas desde Windows)
    private static bool IsInsideDocker => File.Exists("/.dockerenv");

    // Host para acceder a los servicios de IA
    private static string ServiceHost => IsInsideDocker ? "host.docker.internal" : "localhost";

    // Colores ANSI
    private const string Reset = "\x1b[0m";
    private const string Bold = "\x1b[1m";
    private const string Dim = "\x1b[2m";
    private const string Red = "\x1b[31m";
    private const string Green = "\x1b[32m";
    private const string Yellow = "\x1b[33m";
    private const string Cyan = "\x1b[36m";
    private const string White = "\x1b[37m";
    private const string BrightGreen = "\x1b[92m";
    private const string BrightCyan = "\x1b[96m";

    /// <summary>
    /// Inicializa los servicios de IA.
    /// En Linux nativo: inicia contenedores Docker.
    /// En Docker: solo verifica que los servicios del host están disponibles.
    /// </summary>
    public static async Task<bool> EnsureAllAsync(CancellationToken cancellationToken = default)
    {
        Console.Clear();
        DrawHeader();

        try
        {
            if (IsInsideDocker)
            {
                // Modo pruebas: los servicios ya están corriendo en Windows
                return await VerifyServicesAsync(cancellationToken);
            }
            else
            {
                // Linux nativo: iniciar contenedores
                return await StartServicesAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            DrawCancelled();
            return false;
        }
        catch (Exception ex)
        {
            DrawError(ex.Message);
            Console.WriteLine();
            Console.WriteLine($"  {Dim}Presiona Enter para continuar...{Reset}");
            Console.ReadLine();
            return false;
        }
    }

    /// <summary>
    /// Inicia los servicios de IA en contenedores Docker (Linux nativo).
    /// </summary>
    private static async Task<bool> StartServicesAsync(CancellationToken cancellationToken)
    {
        // 1. Verificar Docker
        DrawStep(1, 4, "Verificando Docker...");
        if (!await IsDockerAvailableAsync())
        {
            DrawError("Docker no está instalado o no está en ejecución.");
            Console.WriteLine();
            Console.WriteLine($"  {Dim}Instala Docker: https://docs.docker.com/engine/install/{Reset}");
            Console.WriteLine($"  {Dim}Presiona Enter para continuar sin IA...{Reset}");
            Console.ReadLine();
            return false;
        }
        DrawStepComplete(1, 4, "Docker disponible");

        // 2. Iniciar Ollama
        DrawStep(2, 4, "Iniciando Ollama...");
        if (!await EnsureOllamaContainerAsync(cancellationToken))
        {
            DrawError("No se pudo iniciar el contenedor de Ollama.");
            return false;
        }
        DrawStepComplete(2, 4, "Ollama iniciado");

        // 3. Descargar modelo llama3 si es necesario
        DrawStep(3, 4, "Verificando modelo de IA...");
        if (!await EnsureLlama3ModelAsync(cancellationToken))
        {
            DrawError("No se pudo descargar el modelo llama3.");
            return false;
        }
        DrawStepComplete(3, 4, "Modelo llama3 listo");

        // 4. Iniciar TTS (opcional)
        DrawStep(4, 4, "Iniciando TTS...");
        var ttsReady = await EnsureTtsContainerAsync(cancellationToken);
        if (ttsReady)
        {
            DrawStepComplete(4, 4, "TTS iniciado");
        }
        else
        {
            DrawStepComplete(4, 4, "TTS no disponible (voz desactivada)");
        }

        DrawSuccess();
        await Task.Delay(1000, cancellationToken);
        return true;
    }

    /// <summary>
    /// Solo verifica que los servicios están disponibles (modo pruebas en Docker).
    /// </summary>
    private static async Task<bool> VerifyServicesAsync(CancellationToken cancellationToken)
    {
        // 1. Verificar Ollama
        DrawStep(1, 2, "Verificando Ollama...");
        var ollamaReady = await WaitForServiceAsync(
            $"http://{ServiceHost}:11434/api/tags",
            "Ollama",
            30,
            cancellationToken);

        if (!ollamaReady)
        {
            DrawError("Ollama no está disponible en el host.");
            return false;
        }
        DrawStepComplete(1, 2, "Ollama disponible");

        // 2. Verificar TTS (opcional)
        DrawStep(2, 2, "Verificando TTS...");
        var ttsReady = await WaitForServiceAsync(
            $"http://{ServiceHost}:5002/api/tts?text=ok",
            "TTS",
            15,
            cancellationToken);

        if (ttsReady)
        {
            DrawStepComplete(2, 2, "TTS disponible");
        }
        else
        {
            DrawStepComplete(2, 2, "TTS no disponible (voz desactivada)");
        }

        DrawSuccess();
        await Task.Delay(1000, cancellationToken);
        return true;
    }

    private static async Task<bool> IsDockerAvailableAsync()
    {
        try
        {
            var result = await RunCommandAsync("docker", "--version");
            return result.exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> EnsureOllamaContainerAsync(CancellationToken cancellationToken)
    {
        const string containerName = "xilo-ollama";
        const string imageName = "ollama/ollama";

        // Verificar si el contenedor ya existe
        var (exitCode, output) = await RunCommandAsync("docker", $"ps -a --filter name={containerName} --format \"{{{{.Names}}}}\"");
        var containerExists = output.Trim() == containerName;

        if (containerExists)
        {
            // Verificar si está corriendo
            var (_, runningOutput) = await RunCommandAsync("docker", $"ps --filter name={containerName} --format \"{{{{.Names}}}}\"");
            if (runningOutput.Trim() != containerName)
            {
                // Iniciar contenedor existente
                ShowProgress("Iniciando contenedor existente...");
                await RunCommandAsync("docker", $"start {containerName}");
            }
        }
        else
        {
            // Primero descargar la imagen mostrando progreso
            Console.WriteLine();
            Console.WriteLine($"        {Dim}Descargando imagen de Ollama...{Reset}");
            Console.WriteLine();

            var pullSuccess = await RunCommandWithOutputAsync("docker", $"pull {imageName}", cancellationToken);
            if (!pullSuccess)
            {
                return false;
            }

            Console.WriteLine();

            // Detectar si hay GPU NVIDIA disponible
            var hasGpu = await HasNvidiaGpuAsync();
            var gpuFlag = hasGpu ? "--gpus all" : "";

            ShowProgress("Creando contenedor...");
            var createCmd = $"run -d --name {containerName} {gpuFlag} -p 11434:11434 -v xilo-ollama:/root/.ollama {imageName}";
            var (createExitCode, _) = await RunCommandAsync("docker", createCmd);

            if (createExitCode != 0)
            {
                return false;
            }
        }

        // Esperar a que Ollama esté listo
        return await WaitForServiceAsync(
            $"http://{ServiceHost}:11434/api/tags",
            "Ollama",
            60,
            cancellationToken);
    }

    private static async Task<bool> EnsureLlama3ModelAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var tagsUrl = $"http://{ServiceHost}:11434/api/tags";

        try
        {
            // Verificar si llama3 ya está descargado
            var response = await client.GetAsync(tagsUrl, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                if (doc.RootElement.TryGetProperty("models", out var models))
                {
                    foreach (var model in models.EnumerateArray())
                    {
                        if (model.TryGetProperty("name", out var name))
                        {
                            var modelName = name.GetString() ?? "";
                            if (modelName.StartsWith("llama3", StringComparison.OrdinalIgnoreCase))
                            {
                                // Ya está descargado
                                return true;
                            }
                        }
                    }
                }
            }
        }
        catch
        {
            // Continuar e intentar descargar
        }

        // Descargar llama3 mostrando progreso
        Console.WriteLine();
        Console.WriteLine($"        {Dim}Descargando modelo llama3 (~4.7 GB)...{Reset}");
        Console.WriteLine();

        var success = await RunCommandWithOutputAsync("docker", "exec xilo-ollama ollama pull llama3", cancellationToken, timeoutMinutes: 15);

        Console.WriteLine();
        return success;
    }

    private static async Task<bool> EnsureTtsContainerAsync(CancellationToken cancellationToken)
    {
        const string containerName = "xilo-tts";

        // Detectar GPU para seleccionar imagen apropiada
        var hasGpu = await HasNvidiaGpuAsync();
        var gpuFlag = hasGpu ? "--gpus all" : "";
        // Con GPU: imagen completa con soporte CUDA. Sin GPU: imagen optimizada para CPU
        var imageName = hasGpu ? "ghcr.io/coqui-ai/tts:latest" : "ghcr.io/idiap/coqui-tts-cpu:latest";

        // Verificar si el contenedor ya existe
        var (_, output) = await RunCommandAsync("docker", $"ps -a --filter name={containerName} --format \"{{{{.Names}}}}\"");
        var containerExists = output.Trim() == containerName;

        if (containerExists)
        {
            // Verificar si está corriendo
            var (_, runningOutput) = await RunCommandAsync("docker", $"ps --filter name={containerName} --format \"{{{{.Names}}}}\"");
            if (runningOutput.Trim() != containerName)
            {
                // Si hay GPU disponible, verificar si el contenedor tiene GPU configurada
                if (hasGpu)
                {
                    var (_, inspectOutput) = await RunCommandAsync("docker", $"inspect {containerName} --format \"{{{{.HostConfig.DeviceRequests}}}}\"");
                    if (!inspectOutput.Contains("nvidia", StringComparison.OrdinalIgnoreCase))
                    {
                        // Recrear contenedor con GPU
                        ShowProgress("Recreando contenedor TTS con soporte GPU...");
                        await RunCommandAsync("docker", $"rm -f {containerName}");
                        var createCmd = $"run -d --name {containerName} {gpuFlag} -p 5002:5002 -v xilo-tts:/root/.local/share/tts {imageName}";
                        await RunCommandAsync("docker", createCmd);
                    }
                    else
                    {
                        ShowProgress("Iniciando contenedor TTS existente...");
                        await RunCommandAsync("docker", $"start {containerName}");
                    }
                }
                else
                {
                    ShowProgress("Iniciando contenedor TTS existente...");
                    await RunCommandAsync("docker", $"start {containerName}");
                }
            }
        }
        else
        {
            // Primero descargar la imagen mostrando progreso
            Console.WriteLine();
            Console.WriteLine($"        {Dim}Descargando imagen de TTS (~2 GB)...{Reset}");
            if (hasGpu)
            {
                Console.WriteLine($"        {Dim}(usando aceleración GPU){Reset}");
            }
            Console.WriteLine();

            var pullSuccess = await RunCommandWithOutputAsync("docker", $"pull {imageName}", cancellationToken);
            if (!pullSuccess)
            {
                return false;
            }

            Console.WriteLine();

            ShowProgress("Creando contenedor TTS...");
            var createCmd = $"run -d --name {containerName} {gpuFlag} -p 5002:5002 -v xilo-tts:/root/.local/share/tts {imageName}";
            var (createExitCode, _) = await RunCommandAsync("docker", createCmd);

            if (createExitCode != 0)
            {
                return false;
            }
        }

        // Esperar a que TTS esté listo (puede tardar en cargar el modelo)
        return await WaitForServiceAsync(
            $"http://{ServiceHost}:5002/api/tts?text=ok",
            "TTS",
            120, // TTS puede tardar más en iniciar
            cancellationToken);
    }

    private static async Task<bool> HasNvidiaGpuAsync()
    {
        try
        {
            var (exitCode, _) = await RunCommandAsync("nvidia-smi", "--version");
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> WaitForServiceAsync(
        string url,
        string serviceName,
        int maxWaitSeconds,
        CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var elapsed = 0;
        var spinner = new[] { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };
        var spinnerIndex = 0;

        while (elapsed < maxWaitSeconds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await client.GetAsync(url, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    ClearProgressLine();
                    return true;
                }
            }
            catch { }

            Console.Write($"\r        {BrightCyan}{spinner[spinnerIndex]}{Reset} Conectando a {serviceName}... ({elapsed}s)  ");
            spinnerIndex = (spinnerIndex + 1) % spinner.Length;

            await Task.Delay(1000, cancellationToken);
            elapsed++;
        }

        ClearProgressLine();
        return false;
    }

    private static async Task<(int exitCode, string output)> RunCommandAsync(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return (-1, "");

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return (process.ExitCode, output);
        }
        catch (Exception ex)
        {
            return (-1, ex.Message);
        }
    }

    /// <summary>
    /// Ejecuta un comando mostrando su salida en tiempo real (para progreso de descargas).
    /// </summary>
    private static async Task<bool> RunCommandWithOutputAsync(
        string fileName,
        string arguments,
        CancellationToken cancellationToken,
        int timeoutMinutes = 10)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return false;

            // Leer stdout y stderr en paralelo, mostrando en consola
            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        // Formatear línea de progreso
                        Console.Write($"\r        {BrightCyan}↓{Reset} {line.PadRight(60)}");
                    }
                }
            }, cancellationToken);

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrEmpty(line))
                    {
                        // stderr a veces contiene progreso (como en docker pull)
                        Console.Write($"\r        {BrightCyan}↓{Reset} {line.PadRight(60)}");
                    }
                }
            }, cancellationToken);

            // Esperar con timeout
            var timeout = TimeSpan.FromMinutes(timeoutMinutes);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);

            try
            {
                await Task.WhenAll(outputTask, errorTask).WaitAsync(cts.Token);
                await process.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
                throw;
            }

            Console.WriteLine(); // Nueva línea después del progreso
            return process.ExitCode == 0;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }

    private static void ShowProgress(string message)
    {
        Console.Write($"\r        {BrightCyan}●{Reset} {message}".PadRight(70));
    }

    private static void DrawHeader()
    {
        var width = Math.Min(Console.WindowWidth, 80);
        var line = new string('═', width - 2);
        var title = IsInsideDocker ? "VERIFICANDO SERVICIOS DE IA" : "INICIANDO SERVICIOS DE IA";

        Console.WriteLine();
        Console.WriteLine($"{Cyan}╔{line}╗{Reset}");
        Console.WriteLine($"{Cyan}║{Reset}{Bold}{White}{title.PadLeft((width - 2 + title.Length) / 2).PadRight(width - 2)}{Reset}{Cyan}║{Reset}");
        Console.WriteLine($"{Cyan}╚{line}╝{Reset}");
        Console.WriteLine();
    }

    private static void DrawStep(int step, int total, string message)
    {
        var prefix = $"{Dim}[{step}/{total}]{Reset}";
        Console.WriteLine($"  {prefix} {Yellow}●{Reset} {message}");
    }

    private static void DrawStepComplete(int step, int total, string message)
    {
        Console.SetCursorPosition(0, Console.CursorTop - 1);
        var prefix = $"{Dim}[{step}/{total}]{Reset}";
        Console.WriteLine($"  {prefix} {Green}✓{Reset} {message.PadRight(60)}");
    }

    private static void ClearProgressLine()
    {
        Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
    }

    private static void DrawError(string message)
    {
        Console.WriteLine();
        Console.WriteLine($"  {Bold}{Red}✗ ERROR:{Reset} {message}");
    }

    private static void DrawCancelled()
    {
        Console.WriteLine();
        Console.WriteLine($"  {Yellow}⚠ Operación cancelada.{Reset}");
    }

    private static void DrawSuccess()
    {
        Console.WriteLine();
        Console.WriteLine($"  {BrightGreen}════════════════════════════════════════════{Reset}");
        Console.WriteLine($"  {BrightGreen}✓{Reset} {Bold}Servicios de IA listos{Reset}");
        Console.WriteLine($"  {BrightGreen}════════════════════════════════════════════{Reset}");
        Console.WriteLine();
    }
}
