using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace XiloAdventures.Wpf.Common.Services;

/// <summary>
/// Información de progreso de descarga de un modelo de Ollama.
/// </summary>
public sealed class ModelDownloadProgress
{
    public int Percentage { get; init; }
    public string Downloaded { get; init; } = "";
    public string Total { get; init; } = "";
    public string Speed { get; init; } = "";
    public string Eta { get; init; } = "";
    public bool IsCompleted { get; init; }
    public string Status { get; init; } = "";
}

public static class DockerService
{
    private static readonly SemaphoreSlim _lock = new(1, 1);

    private const string OllamaContainerName = "xilo-ollama";
    private const string TtsContainerName = "xilo-tts";
    private const string StableDiffusionContainerName = "xilo-stablediffusion";

    public static async Task EnsureAllAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default, bool includeTts = true, bool includeStableDiffusion = false, bool includeOllama = true, string ollamaModel = "llama3", IProgress<ModelDownloadProgress>? modelDownloadProgress = null)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            progress?.Report("Comprobando Docker Desktop...");
            await EnsureDockerAvailableAsync(progress, cancellationToken).ConfigureAwait(false);

            if (includeOllama)
            {
                progress?.Report("Preparando contenedor de IA (Ollama)...");
                await EnsureOllamaAsync(progress, cancellationToken).ConfigureAwait(false);

                progress?.Report($"Descargando modelo {ollamaModel} (si es necesario)...");
                await EnsureLlamaModelAsync(ollamaModel, progress, modelDownloadProgress, cancellationToken).ConfigureAwait(false);
            }

            if (includeTts)
            {
                progress?.Report("Preparando servidor de voz (Coqui TTS)...");
                await EnsureTtsAsync(progress, cancellationToken).ConfigureAwait(false);
            }

            if (includeStableDiffusion)
            {
                progress?.Report("Preparando servidor de imágenes (Stable Diffusion)...");
                await EnsureStableDiffusionAsync(progress, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public static async Task StopAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            await StopContainerIfExistsAsync(OllamaContainerName, cancellationToken).ConfigureAwait(false);
            await StopContainerIfExistsAsync(TtsContainerName, cancellationToken).ConfigureAwait(false);
            await StopContainerIfExistsAsync(StableDiffusionContainerName, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private static async Task StopContainerIfExistsAsync(string name, CancellationToken cancellationToken)
    {
        if (await ContainerExistsAsync(name, cancellationToken).ConfigureAwait(false))
        {
            try
            {
                await RunDockerCheckedAsync($"stop {name}", cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                // Si no podemos parar un contenedor, no bloqueamos la cancelación.
            }
        }
    }

    private static async Task EnsureDockerAvailableAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Primero intentamos hablar con Docker normalmente.
        try
        {
            await RunDockerCheckedAsync("info", cancellationToken).ConfigureAwait(false);
            return;
        }
        catch
        {
            // Si falla, intentamos arrancar Docker Desktop nosotros mismos (si existe).
        }

        if (await TryStartDockerDesktopAsync(progress, TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false))
        {
            // Si hemos conseguido arrancar Docker Desktop y 'docker info' responde, todo OK.
            return;
        }

        // Si llegamos aquí, o Docker no está instalado o no hemos sido capaces de arrancarlo.
        throw new InvalidOperationException(
            "No se ha podido contactar con Docker. Asegúrate de que Docker Desktop está instalado y en ejecución.");
    }

    private static bool TryGetDockerDesktopPath(out string path)
    {
        // Rutas típicas de instalación de Docker Desktop en Windows.
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        string[] candidates =
        {
            Path.Combine(programFiles, "Docker", "Docker", "Docker Desktop.exe"),
            Path.Combine(programFilesX86, "Docker", "Docker", "Docker Desktop.exe")
        };

        foreach (var candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate) && File.Exists(candidate))
            {
                path = candidate;
                return true;
            }
        }

        path = string.Empty;
        return false;
    }

    private static async Task<bool> TryStartDockerDesktopAsync(IProgress<string>? progress, TimeSpan timeout, CancellationToken cancellationToken)
    {
        if (!TryGetDockerDesktopPath(out var exePath))
        {
            // No hemos encontrado Docker Desktop instalado.
            return false;
        }

        try
        {
            progress?.Report("Arrancando Docker Desktop...");
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                }
            };
            process.Start();
        }
        catch
        {
            // Si no podemos arrancarlo, devolvemos false y dejaremos que arriba se muestre el mensaje clásico.
            return false;
        }

        // Esperamos a que el daemon de Docker responda a 'docker info'.
        var start = DateTime.UtcNow;

        while (DateTime.UtcNow - start < timeout)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);

            try
            {
                await RunDockerCheckedAsync("info", cancellationToken).ConfigureAwait(false);
                // Si llegamos aquí sin excepción, Docker ya está operativo.
                return true;
            }
            catch
            {
                // Todavía no ha arrancado, seguimos esperando hasta agotar el timeout.
            }

            progress?.Report("Esperando a que Docker termine de arrancar...");
        }

        return false;
    }

    private static async Task EnsureOllamaAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (await IsContainerRunningAsync(OllamaContainerName, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        if (await ContainerExistsAsync(OllamaContainerName, cancellationToken).ConfigureAwait(false))
        {
            progress?.Report("Arrancando contenedor existente de Ollama...");
            await RunDockerCheckedAsync($"start {OllamaContainerName}", cancellationToken).ConfigureAwait(false);
            return;
        }

        progress?.Report("Descargando imagen de Ollama (la primera vez tarda hasta 15')...");
        await RunDockerCheckedAsync("pull ollama/ollama:latest", cancellationToken).ConfigureAwait(false);

        progress?.Report("Creando contenedor de Ollama con soporte GPU...");
        // Creamos el contenedor con volumen persistente para los modelos y soporte GPU
        await RunDockerCheckedAsync(
            $"run -d --gpus all --name {OllamaContainerName} -p 11434:11434 -v {OllamaContainerName}:/root/.ollama ollama/ollama:latest",
            cancellationToken).ConfigureAwait(false);
    }

    private static async Task EnsureLlamaModelAsync(string modelName, IProgress<string>? progress, IProgress<ModelDownloadProgress>? downloadProgress, CancellationToken cancellationToken)
    {
        // Siempre intentamos hacer pull; si ya está descargado será rápido.
        // Mensaje especial para modelos grandes
        var downloadMessage = modelName switch
        {
            "llama3.1:70b" => $"Descargando modelo {modelName} (40GB) (esto puede tardar hasta 1h la primera vez)...",
            "llama3.1:8b" => $"Descargando modelo {modelName} (esto puede tardar un poco)...",
            _ => $"Descargando modelo {modelName} (esto puede tardar varios minutos, pero solo lo haré esta vez)..."
        };
        progress?.Report(downloadMessage);

        // Si hay un progress reporter, usamos streaming para mostrar el progreso real
        if (downloadProgress != null)
        {
            await RunOllamaPullWithProgressAsync(modelName, downloadProgress, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await RunDockerCheckedAsync($"exec {OllamaContainerName} ollama pull {modelName}", cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Ejecuta ollama pull con streaming de salida para reportar progreso de descarga.
    /// </summary>
    private static async Task RunOllamaPullWithProgressAsync(string modelName, IProgress<ModelDownloadProgress> downloadProgress, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec {OllamaContainerName} ollama pull {modelName}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
        {
            throw new InvalidOperationException("No se ha podido iniciar el comando 'docker'.");
        }

        using var reg = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // Ignoramos errores al matar el proceso en cancelación.
            }
        });

        // Regex para parsear líneas de progreso de ollama pull
        // Ejemplo: "pulling de20d2cf2dc4... 45% ▕████████████                    ▏ 17 GB/39 GB  25 MB/s  14m52s"
        var progressRegex = new Regex(@"(\d+)%.*?(\d+(?:\.\d+)?\s*[KMGT]?B)/(\d+(?:\.\d+)?\s*[KMGT]?B)\s+(\d+(?:\.\d+)?\s*[KMGT]?B/s)\s+(.+)$", RegexOptions.Compiled);
        var pullRegex = new Regex(@"pulling\s+(\w+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        var completedRegex = new Regex(@"(success|verifying|writing)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        // Regex para limpiar códigos ANSI de escape (control de terminal)
        var ansiEscapeRegex = new Regex(@"\x1B\[[0-9;]*[a-zA-Z]|\x1B\[\?[0-9;]*[a-zA-Z]", RegexOptions.Compiled);

        // Leer stderr en streaming (ollama escribe el progreso en stderr)
        var stderrReader = process.StandardError;
        var buffer = new char[4096];
        var lineBuffer = "";

        while (!process.HasExited || stderrReader.Peek() >= 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int charsRead = await stderrReader.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
            if (charsRead == 0)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                continue;
            }

            lineBuffer += new string(buffer, 0, charsRead);

            // Procesar líneas completas o actualizaciones de progreso (que usan \r)
            var lines = lineBuffer.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var progressMatch = progressRegex.Match(line);
                if (progressMatch.Success)
                {
                    // Limpiar códigos ANSI y texto extra del ETA (ej: "0spulling manifest")
                    var eta = ansiEscapeRegex.Replace(progressMatch.Groups[5].Value.Trim(), "");
                    // Solo mantener el tiempo (formato: 14m52s, 3m2s, 45s, etc.)
                    var etaTimeMatch = Regex.Match(eta, @"^(\d+[hms])+");
                    eta = etaTimeMatch.Success ? etaTimeMatch.Value : "";
                    downloadProgress.Report(new ModelDownloadProgress
                    {
                        Percentage = int.Parse(progressMatch.Groups[1].Value),
                        Downloaded = progressMatch.Groups[2].Value.Trim(),
                        Total = progressMatch.Groups[3].Value.Trim(),
                        Speed = progressMatch.Groups[4].Value.Trim(),
                        Eta = eta,
                        IsCompleted = false,
                        Status = "Descargando..."
                    });
                }
                else if (pullRegex.IsMatch(line))
                {
                    downloadProgress.Report(new ModelDownloadProgress
                    {
                        Percentage = 0,
                        Status = "Iniciando descarga...",
                        IsCompleted = false
                    });
                }
                else if (completedRegex.IsMatch(line))
                {
                    downloadProgress.Report(new ModelDownloadProgress
                    {
                        Percentage = 100,
                        Status = "Verificando...",
                        IsCompleted = true
                    });
                }
            }

            // Mantener solo la última parte incompleta
            var lastNewline = lineBuffer.LastIndexOfAny(new[] { '\r', '\n' });
            lineBuffer = lastNewline >= 0 ? lineBuffer[(lastNewline + 1)..] : "";
        }

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"Error al descargar modelo: {stderr}");
        }

        downloadProgress.Report(new ModelDownloadProgress
        {
            Percentage = 100,
            Status = "Completado",
            IsCompleted = true
        });
    }

    private static async Task EnsureTtsAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (await IsContainerRunningAsync(TtsContainerName, cancellationToken).ConfigureAwait(false))
        {
            progress?.Report("Coqui TTS ya está en ejecución.");
            return;
        }

        if (await ContainerExistsAsync(TtsContainerName, cancellationToken).ConfigureAwait(false))
        {
            progress?.Report("Arrancando contenedor existente de Coqui TTS...");
            await RunDockerCheckedAsync($"start {TtsContainerName}", cancellationToken).ConfigureAwait(false);

            progress?.Report("Esperando a que Coqui TTS esté listo...");
            await WaitForTtsReadyAsync(progress, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Detectar si hay GPU NVIDIA disponible
        progress?.Report("Detectando GPU NVIDIA...");
        bool hasGpu = await HasNvidiaGpuAsync(cancellationToken).ConfigureAwait(false);

        string ttsImage = hasGpu ? "ghcr.io/coqui-ai/tts:latest" : "ghcr.io/idiap/coqui-tts-cpu:latest";
        progress?.Report($"Descargando imagen de Coqui TTS {(hasGpu ? "(GPU)" : "(CPU)")} (la primera vez tarda hasta 15')...");
        await RunDockerCheckedAsync($"pull {ttsImage}", cancellationToken).ConfigureAwait(false);

        progress?.Report("Creando contenedor de Coqui TTS...");
        // Arrancamos el servidor HTTP de TTS en el puerto 5002 con un modelo de un solo hablante
        string runCommand = hasGpu
            ? $"run -d --gpus all --name {TtsContainerName} -p 5002:5002 --entrypoint python3 {ttsImage} TTS/server/server.py --model_name tts_models/es/css10/vits"
            : $"run -d --name {TtsContainerName} -p 5002:5002 --entrypoint python3 {ttsImage} TTS/server/server.py --model_name tts_models/es/css10/vits";
        await RunDockerCheckedAsync(runCommand, cancellationToken).ConfigureAwait(false);

        progress?.Report("Esperando a que Coqui TTS esté listo...");
        await WaitForTtsReadyAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForTtsReadyAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        const int maxAttempts = 100;
        var maxDuration = TimeSpan.FromMinutes(2);
        var stopwatch = Stopwatch.StartNew();
        int attempt = 0;

        while (attempt < maxAttempts && stopwatch.Elapsed < maxDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            try
            {
                progress?.Report($"Esperando disponibilidad de Coqui TTS...");

                using var client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:5002"),
                    Timeout = TimeSpan.FromSeconds(3)
                };

                var response = await client.GetAsync("api/tts?text=ok", cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
                    if (bytes.Length > 0)
                    {
                        return;
                    }
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout interno de la petición HTTP, reintentamos.
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                // El servidor aún no está listo, reintentamos.
            }

            await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
        }

        throw new InvalidOperationException("Coqui TTS no ha estado disponible tras 100 reintentos o 2 minutos de espera.");
    }

    /// <summary>
    /// Detecta si hay una GPU NVIDIA disponible con soporte Docker.
    /// </summary>
    private static async Task<bool> HasNvidiaGpuAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Intentamos ejecutar nvidia-smi dentro de un contenedor con GPU
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "run --rm --gpus all nvidia/cuda:12.2.0-base-ubuntu22.04 nvidia-smi",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi };
            if (!process.Start())
                return false;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30)); // Timeout de 30 segundos

            try
            {
                await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
                return process.ExitCode == 0;
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(true); } catch { }
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    private static async Task EnsureStableDiffusionAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        if (await IsContainerRunningAsync(StableDiffusionContainerName, cancellationToken).ConfigureAwait(false))
        {
            progress?.Report("Stable Diffusion ya está en ejecución.");
            return;
        }

        if (await ContainerExistsAsync(StableDiffusionContainerName, cancellationToken).ConfigureAwait(false))
        {
            progress?.Report("Arrancando contenedor existente de Stable Diffusion...");
            await RunDockerCheckedAsync($"start {StableDiffusionContainerName}", cancellationToken).ConfigureAwait(false);

            progress?.Report("Esperando a que Stable Diffusion esté listo...");
            await WaitForStableDiffusionReadyAsync(progress, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Detectar si hay GPU NVIDIA disponible
        progress?.Report("Detectando GPU NVIDIA...");
        bool hasGpu = await HasNvidiaGpuAsync(cancellationToken).ConfigureAwait(false);

        if (hasGpu)
        {
            progress?.Report("GPU NVIDIA detectada. Se usará aceleración por hardware.");
        }
        else
        {
            progress?.Report("No se detectó GPU NVIDIA. Se usará CPU (más lento).");
        }

        progress?.Report("Descargando imagen de Stable Diffusion (la primera vez tarda 10-20')...");
        await RunDockerCheckedAsync("pull gadicc/diffusers-api:latest", cancellationToken).ConfigureAwait(false);

        progress?.Report("Creando contenedor de Stable Diffusion...");

        // Comando diferente según si hay GPU o no
        // Optimizations for RTX 3080 Ti: xformers, fp16, better memory allocation
        // DNS settings to ensure HuggingFace connectivity
        string runCommand = hasGpu
            ? $"run -d --gpus all --name {StableDiffusionContainerName} -p 7860:8000 " +
              $"--dns 8.8.8.8 --dns 8.8.4.4 " +
              $"-e PYTORCH_CUDA_ALLOC_CONF=max_split_size_mb:512 " +
              $"-v {StableDiffusionContainerName}_cache:/root/.cache gadicc/diffusers-api:latest"
            : $"run -d --name {StableDiffusionContainerName} -p 7860:8000 --dns 8.8.8.8 --dns 8.8.4.4 -v {StableDiffusionContainerName}_cache:/root/.cache gadicc/diffusers-api:latest";

        await RunDockerCheckedAsync(runCommand, cancellationToken).ConfigureAwait(false);

        progress?.Report("Esperando a que Stable Diffusion esté listo (cargando modelo)...");
        await WaitForStableDiffusionReadyAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WaitForStableDiffusionReadyAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        const int maxAttempts = 120; // SD tarda más en arrancar por la carga del modelo
        var maxDuration = TimeSpan.FromMinutes(5);
        var stopwatch = Stopwatch.StartNew();
        int attempt = 0;

        // Fase 1: Esperar a que el servidor HTTP esté activo
        while (attempt < maxAttempts && stopwatch.Elapsed < maxDuration)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;
            try
            {
                progress?.Report($"Esperando disponibilidad de Stable Diffusion...");

                using var client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:7860"),
                    Timeout = TimeSpan.FromSeconds(5)
                };

                // Intentar acceder al endpoint raíz para verificar que el servidor está listo
                // Cualquier respuesta HTTP (incluso 405 Method Not Allowed) indica que el servidor está activo
                var response = await client.GetAsync("/", cancellationToken).ConfigureAwait(false);
                // Si obtenemos cualquier respuesta HTTP, el servidor está listo
                break;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout interno de la petición HTTP, reintentamos.
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException)
            {
                // El servidor aún no está listo, reintentamos.
            }

            await Task.Delay(2500, cancellationToken).ConfigureAwait(false);
        }

        if (attempt >= maxAttempts || stopwatch.Elapsed >= maxDuration)
        {
            throw new InvalidOperationException("Stable Diffusion no ha estado disponible tras 120 reintentos o 5 minutos de espera.");
        }

        // Fase 2: Precarga del modelo (warmup) - esto descarga el modelo si no está en caché
        progress?.Report("Precargando modelo de Stable Diffusion (primera vez puede tardar varios minutos)...");
        await WarmupStableDiffusionModelAsync(progress, cancellationToken).ConfigureAwait(false);
    }

    private static async Task WarmupStableDiffusionModelAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:7860"),
            Timeout = TimeSpan.FromMinutes(30) // La descarga del modelo puede tardar mucho
        };

        // Hacer una petición mínima para forzar la descarga/carga del modelo
        var warmupRequest = new
        {
            modelInputs = new
            {
                prompt = "test",
                width = 64,
                height = 64,
                num_inference_steps = 1
            },
            callInputs = new
            {
                MODEL_ID = "runwayml/stable-diffusion-v1-5",
                PIPELINE = "StableDiffusionPipeline",
                safety_checker = false,
                requires_safety_checker = false
            }
        };

        var json = System.Text.Json.JsonSerializer.Serialize(warmupRequest);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync("/", content, cancellationToken).ConfigureAwait(false);
            // No nos importa si la imagen sale bien o mal, solo que el modelo se haya cargado
            progress?.Report("Modelo de Stable Diffusion listo.");
        }
        catch (Exception ex)
        {
            // Si falla el warmup, continuamos - el modelo se cargará en la primera petición real
            progress?.Report($"Advertencia: warmup falló ({ex.Message}), el modelo se cargará al generar.");
        }
    }

    private static async Task<bool> IsContainerRunningAsync(string name, CancellationToken cancellationToken)
    {
        var output = await RunDockerGetOutputAsync(
            $"ps --filter name={name} --format {{{{.ID}}}}", cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(output);
    }

    private static async Task<bool> ContainerExistsAsync(string name, CancellationToken cancellationToken)
    {
        var output = await RunDockerGetOutputAsync(
            $"ps -a --filter name={name} --format {{{{.ID}}}}", cancellationToken).ConfigureAwait(false);
        return !string.IsNullOrWhiteSpace(output);
    }

    /// <summary>
    /// Ejecuta un comando docker y lanza excepción si falla.
    /// </summary>
    private static async Task RunDockerCheckedAsync(string args, CancellationToken cancellationToken)
    {
        await RunDockerCoreAsync(args, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Ejecuta un comando docker y devuelve la salida estándar.
    /// </summary>
    private static async Task<string> RunDockerGetOutputAsync(string args, CancellationToken cancellationToken)
    {
        return await RunDockerCoreAsync(args, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Método interno que ejecuta un comando docker, valida el resultado y devuelve stdout.
    /// </summary>
    private static async Task<string> RunDockerCoreAsync(string args, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = args,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        if (!process.Start())
        {
            throw new InvalidOperationException("No se ha podido iniciar el comando 'docker'.");
        }

        using var reg = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // Ignoramos errores al matar el proceso en cancelación.
            }
        });

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();

        await Task.WhenAll(process.WaitForExitAsync(cancellationToken), stdoutTask, stderrTask).ConfigureAwait(false);

        string stdout = stdoutTask.Result;
        string stderr = stderrTask.Result;

        if (process.ExitCode != 0)
        {
            var message = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
            throw new InvalidOperationException($"Error al ejecutar 'docker {args}': {message}");
        }

        return stdout.Trim();
    }
}
