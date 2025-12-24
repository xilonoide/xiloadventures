using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class AiDataGeneratorWindow : Window
{
    private readonly WorldModel _world;
    private CancellationTokenSource? _cts;
    private bool _isProcessing;

    private static readonly HttpClient _ollamaClient = new()
    {
        BaseAddress = new Uri("http://localhost:11434/"),
        Timeout = TimeSpan.FromMinutes(2)
    };

    private static readonly HttpClient _sdClient = new()
    {
        BaseAddress = new Uri("http://localhost:7860/"),
        Timeout = TimeSpan.FromMinutes(3)
    };

    public AiDataGeneratorWindow(WorldModel world)
    {
        InitializeComponent();
        _world = world;
    }

    /// <summary>
    /// Asegura que Docker esté ejecutando los servicios necesarios.
    /// </summary>
    /// <param name="includeOllama">True para LLM (géneros, descripciones).</param>
    /// <param name="includeStableDiffusion">True para generación de imágenes.</param>
    /// <returns>True si Docker está listo, false si el usuario canceló o hubo error.</returns>
    private async Task<bool> EnsureDockerReadyAsync(bool includeOllama, bool includeStableDiffusion)
    {
        var progressWindow = new DockerProgressWindow
        {
            Owner = this,
            IncludeOllama = includeOllama,
            IncludeTts = false,
            IncludeStableDiffusion = includeStableDiffusion
        };

        var result = await progressWindow.RunAsync().ConfigureAwait(true);

        if (result.Canceled || !result.Success)
        {
            if (!result.Canceled)
            {
                DarkErrorDialog.Show("Error de Docker",
                    "No se han podido iniciar los servicios de IA.\n\n" +
                    "Comprueba que Docker Desktop está instalado y en ejecución.", this);
            }
            return false;
        }

        return true;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isProcessing)
        {
            if (!DarkConfirmDialog.Show("Proceso en curso", "Hay un proceso en curso. ¿Deseas cancelarlo y cerrar?", this))
                return;

            _cts?.Cancel();
        }

        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        _cts?.Cancel();
    }

    private async void DoAllButton_Click(object sender, RoutedEventArgs e)
    {
        var errors = new List<string>();
        var tasks = new List<string>();

        // Validate Theme (required for descriptions and image prompts)
        if (string.IsNullOrWhiteSpace(_world.Game.Theme))
        {
            errors.Add("• Falta el tema/ambientación del mundo (necesario para descripciones e imágenes)");
        }

        // Check what needs to be done
        var roomsWithoutDesc = _world.Rooms
            .Where(r => string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        var objectsToFix = _world.Objects
            .Where(o => !o.GenderAndPluralSetManually)
            .ToList();

        var doorsToFix = _world.Doors
            .Where(d => !d.GenderAndPluralSetManually)
            .ToList();

        var roomsWithoutImages = _world.Rooms
            .Where(r => string.IsNullOrEmpty(r.ImageId) && string.IsNullOrEmpty(r.ImageBase64))
            .ToList();

        // Build task list
        if (roomsWithoutDesc.Count > 0)
            tasks.Add($"• Crear descripciones para {roomsWithoutDesc.Count} sala(s)");

        int itemsToFix = objectsToFix.Count + doorsToFix.Count;
        if (itemsToFix > 0)
            tasks.Add($"• Corregir artículos de {objectsToFix.Count} objeto(s) y {doorsToFix.Count} puerta(s)");

        if (roomsWithoutImages.Count > 0)
            tasks.Add($"• Generar imágenes para {roomsWithoutImages.Count} sala(s)");

        // Show errors if any
        if (errors.Count > 0)
        {
            DarkErrorDialog.Show("Validación fallida",
                "No se puede ejecutar el proceso completo:\n\n" + string.Join("\n", errors), this);
            return;
        }

        // Nothing to do?
        if (tasks.Count == 0)
        {
            DarkErrorDialog.Show("Nada que hacer",
                "Todas las salas tienen descripción e imagen, y todos los objetos tienen género asignado.", this);
            return;
        }

        // Confirm
        var confirmMessage = "Se ejecutarán las siguientes tareas:\n\n" + string.Join("\n", tasks);
        if (roomsWithoutImages.Count > 0)
        {
            confirmMessage += "\n\n⚠️ La generación de imágenes es EXTREMADAMENTE LENTA.\n" +
                              "Puede tardar varios minutos por imagen.";
        }
        confirmMessage += "\n\n¿Continuar?";

        if (!DarkConfirmDialog.Show("Confirmar proceso completo", confirmMessage, this))
            return;

        // Determine if we need Stable Diffusion
        bool needsStableDiffusion = roomsWithoutImages.Count > 0;

        // Ensure Docker is ready
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: needsStableDiffusion))
            return;

        // Calculate total steps
        int totalSteps = roomsWithoutDesc.Count + objectsToFix.Count + doorsToFix.Count + roomsWithoutImages.Count;

        await RunProcessAsync(
            "Ejecutando proceso completo...",
            totalSteps,
            async (progress, ct) =>
            {
                int currentStep = 0;

                // Step 1: Create descriptions
                if (roomsWithoutDesc.Count > 0)
                {
                    for (int i = 0; i < roomsWithoutDesc.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var room = roomsWithoutDesc[i];
                        progress($"[1/3] Describiendo: {room.Name}...", currentStep);

                        try
                        {
                            var description = await GenerateRoomDescriptionAsync(room, ct);
                            if (!string.IsNullOrEmpty(description))
                            {
                                room.Description = description;
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error generando descripción para {room.Name}: {ex.Message}");
                        }

                        currentStep++;
                    }
                }

                // Step 2: Correct articles (objects and doors)
                if (objectsToFix.Count > 0)
                {
                    for (int i = 0; i < objectsToFix.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var obj = objectsToFix[i];
                        progress($"[2/3] Analizando objeto: {obj.Name}...", currentStep);

                        try
                        {
                            var (gender, isPlural) = await GetGenderForNameAsync(obj.Name, ct);
                            obj.Gender = gender;
                            obj.IsPlural = isPlural;
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error obteniendo género para {obj.Name}: {ex.Message}");
                        }

                        currentStep++;
                    }
                }

                if (doorsToFix.Count > 0)
                {
                    for (int i = 0; i < doorsToFix.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var door = doorsToFix[i];
                        progress($"[2/3] Analizando puerta: {door.Name}...", currentStep);

                        try
                        {
                            var (gender, isPlural) = await GetGenderForNameAsync(door.Name, ct);
                            door.Gender = gender;
                            door.IsPlural = isPlural;
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error obteniendo género para {door.Name}: {ex.Message}");
                        }

                        currentStep++;
                    }
                }

                // Step 3: Generate images (now all rooms have descriptions)
                if (roomsWithoutImages.Count > 0)
                {
                    for (int i = 0; i < roomsWithoutImages.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var room = roomsWithoutImages[i];
                        progress($"[3/3] Generando imagen: {room.Name}...", currentStep);

                        try
                        {
                            var imageBase64 = await GenerateRoomImageAsync(room, ct);
                            if (!string.IsNullOrEmpty(imageBase64))
                            {
                                room.ImageBase64 = imageBase64;
                                room.ImageId = null;
                            }
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error generando imagen para {room.Name}: {ex.Message}");
                        }

                        currentStep++;
                    }
                }

                progress("¡Proceso completo!", totalSteps);
            });
    }

    private async void GenerateImagesButton_Click(object sender, RoutedEventArgs e)
    {
        // Validate: Theme is required for image prompts
        if (string.IsNullOrWhiteSpace(_world.Game.Theme))
        {
            DarkErrorDialog.Show("Tema requerido",
                "El mundo necesita tener un tema/ambientación definido para generar imágenes coherentes.\n\n" +
                "Añade un tema en las propiedades del mundo (ej: 'fantasía medieval', 'ciencia ficción', 'horror gótico').", this);
            return;
        }

        // Validate: ALL rooms must have descriptions
        var roomsWithoutDescription = _world.Rooms
            .Where(r => string.IsNullOrWhiteSpace(r.Description))
            .ToList();

        if (roomsWithoutDescription.Count > 0)
        {
            var roomNames = string.Join(", ", roomsWithoutDescription.Take(5).Select(r => $"\"{r.Name}\""));
            if (roomsWithoutDescription.Count > 5)
                roomNames += $" y {roomsWithoutDescription.Count - 5} más";

            DarkErrorDialog.Show("Salas sin descripción",
                $"Todas las salas deben tener descripción para generar imágenes.\n\n" +
                $"Salas sin descripción: {roomNames}", this);
            return;
        }

        // Find rooms without images
        var roomsWithoutImages = _world.Rooms
            .Where(r => string.IsNullOrEmpty(r.ImageId) && string.IsNullOrEmpty(r.ImageBase64))
            .ToList();

        if (roomsWithoutImages.Count == 0)
        {
            DarkErrorDialog.Show("Sin salas pendientes",
                "Todas las salas ya tienen imagen asignada.", this);
            return;
        }

        // Confirm
        if (!DarkConfirmDialog.Show("Confirmar generación",
            $"Se generarán imágenes para {roomsWithoutImages.Count} sala(s) sin imagen.\n\n" +
            "PROCESO EXTREMADAMENTE LENTO\n" +
            "Este proceso puede tardar varios minutos por imagen dependiendo de tu hardware.\n\n" +
            "¿Continuar?", this))
            return;

        // Ensure Docker + Stable Diffusion + Ollama are running (Ollama for prompt summarization)
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: true))
            return;

        await RunProcessAsync(
            $"Generando imágenes ({roomsWithoutImages.Count} salas)",
            roomsWithoutImages.Count,
            async (progress, ct) =>
            {
                for (int i = 0; i < roomsWithoutImages.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var room = roomsWithoutImages[i];
                    progress($"Generando: {room.Name}...", i);

                    try
                    {
                        var imageBase64 = await GenerateRoomImageAsync(room, ct);
                        if (!string.IsNullOrEmpty(imageBase64))
                        {
                            room.ImageBase64 = imageBase64;
                            room.ImageId = null; // Mark as AI-generated
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error generando imagen para {room.Name}: {ex.Message}");
                    }
                }
                progress("Completado", roomsWithoutImages.Count);
            });
    }

    private async void CorrectArticlesButton_Click(object sender, RoutedEventArgs e)
    {
        // Find objects and doors without gender manually set
        var objectsToFix = _world.Objects
            .Where(o => !o.GenderAndPluralSetManually)
            .ToList();

        var doorsToFix = _world.Doors
            .Where(d => !d.GenderAndPluralSetManually)
            .ToList();

        int totalToFix = objectsToFix.Count + doorsToFix.Count;

        if (totalToFix == 0)
        {
            DarkErrorDialog.Show("Sin elementos pendientes",
                "Todos los objetos y puertas ya tienen género asignado manualmente.", this);
            return;
        }

        if (!DarkConfirmDialog.Show("Confirmar corrección",
            $"Se determinará el género gramatical de {objectsToFix.Count} objeto(s) y {doorsToFix.Count} puerta(s).\n\n¿Continuar?", this))
            return;

        // Ensure Docker + Ollama are running (no need for Stable Diffusion)
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: false))
            return;

        await RunProcessAsync(
            $"Corrigiendo géneros ({totalToFix} elementos)",
            totalToFix,
            async (progress, ct) =>
            {
                int currentStep = 0;

                // Process objects
                for (int i = 0; i < objectsToFix.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var obj = objectsToFix[i];
                    progress($"Analizando objeto: {obj.Name}...", currentStep);

                    try
                    {
                        var (gender, isPlural) = await GetGenderForNameAsync(obj.Name, ct);
                        obj.Gender = gender;
                        obj.IsPlural = isPlural;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error obteniendo género para {obj.Name}: {ex.Message}");
                    }

                    currentStep++;
                }

                // Process doors
                for (int i = 0; i < doorsToFix.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var door = doorsToFix[i];
                    progress($"Analizando puerta: {door.Name}...", currentStep);

                    try
                    {
                        var (gender, isPlural) = await GetGenderForNameAsync(door.Name, ct);
                        door.Gender = gender;
                        door.IsPlural = isPlural;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error obteniendo género para {door.Name}: {ex.Message}");
                    }

                    currentStep++;
                }

                progress("Completado", totalToFix);
            });
    }

    private async void CreateDescriptionsButton_Click(object sender, RoutedEventArgs e)
    {
        // Check theme
        if (string.IsNullOrWhiteSpace(_world.Game.Theme))
        {
            DarkErrorDialog.Show("Tema requerido",
                "El mundo necesita tener un tema/ambientación definido para generar descripciones coherentes.\n\n" +
                "Añade un tema en las propiedades del mundo (ej: 'fantasía medieval', 'ciencia ficción', 'horror gótico').", this);
            return;
        }

        // Find rooms without description
        var roomsWithoutDesc = _world.Rooms
            .Where(r => string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        if (roomsWithoutDesc.Count == 0)
        {
            DarkErrorDialog.Show("Sin salas pendientes",
                "Todas las salas ya tienen descripción.", this);
            return;
        }

        if (!DarkConfirmDialog.Show("Confirmar creación",
            $"Se crearán descripciones para {roomsWithoutDesc.Count} sala(s) sin descripción.\n\n¿Continuar?", this))
            return;

        // Ensure Docker + Ollama are running (no need for Stable Diffusion)
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: false))
            return;

        await RunProcessAsync(
            $"Creando descripciones ({roomsWithoutDesc.Count} salas)",
            roomsWithoutDesc.Count,
            async (progress, ct) =>
            {
                for (int i = 0; i < roomsWithoutDesc.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var room = roomsWithoutDesc[i];
                    progress($"Describiendo: {room.Name}...", i);

                    try
                    {
                        var description = await GenerateRoomDescriptionAsync(room, ct);
                        if (!string.IsNullOrEmpty(description))
                        {
                            room.Description = description;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error generando descripción para {room.Name}: {ex.Message}");
                    }
                }
                progress("Completado", roomsWithoutDesc.Count);
            });
    }

    private async Task RunProcessAsync(string label, int total, Func<Action<string, int>, CancellationToken, Task> work)
    {
        _isProcessing = true;
        _cts = new CancellationTokenSource();

        SetButtonsEnabled(false);
        ProgressSection.Visibility = Visibility.Visible;
        CancelButton.Visibility = Visibility.Visible;
        ProgressBar.Value = 0;
        ProgressBar.Maximum = total;
        ProgressLabel.Text = label;
        ProgressStatus.Text = $"0 / {total}";

        try
        {
            await work((status, current) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressLabel.Text = status;
                    ProgressBar.Value = current;
                    ProgressStatus.Text = $"{current} / {total}";
                });
            }, _cts.Token);

            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = total;
                ProgressStatus.Text = $"{total} / {total}";
                ProgressLabel.Text = "Proceso completado";
            });
        }
        catch (OperationCanceledException)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressLabel.Text = "Proceso cancelado";
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                DarkErrorDialog.Show("Error", $"Error durante el proceso: {ex.Message}", this);
            });
        }
        finally
        {
            _isProcessing = false;
            SetButtonsEnabled(true);
            CancelButton.Visibility = Visibility.Collapsed;
            ProgressSection.Visibility = Visibility.Collapsed;
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        DoAllButton.IsEnabled = enabled;
        GenerateImagesButton.IsEnabled = enabled;
        CorrectArticlesButton.IsEnabled = enabled;
        CreateDescriptionsButton.IsEnabled = enabled;
    }

    private async Task<string?> GenerateRoomImageAsync(Room room, CancellationToken ct)
    {
        // First, use Ollama to create a condensed prompt (CLIP only handles 77 tokens)
        var prompt = await GenerateImagePromptAsync(room, ct);
        if (string.IsNullOrEmpty(prompt))
        {
            // Fallback to basic prompt if Ollama fails
            prompt = $"pixel art, 16-bit, retro game style, {_world.Game.Theme ?? "fantasy"}, {room.Name}, atmospheric, detailed";
        }

        // gadicc/diffusers-api expects this specific format
        var requestBody = new
        {
            modelInputs = new
            {
                prompt,
                negative_prompt = "text, watermark, signature, blurry, low quality, deformed, ugly, bad anatomy, realistic, photorealistic, 3d render",
                width = 896,
                height = 256,
                num_inference_steps = 25,
                guidance_scale = 7.5
            },
            callInputs = new
            {
                MODEL_ID = "runwayml/stable-diffusion-v1-5",
                PIPELINE = "StableDiffusionPipeline",
                SCHEDULER = "EulerAncestralDiscreteScheduler"
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _sdClient.PostAsync("/", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        // diffusers-api returns image_base64
        if (doc.RootElement.TryGetProperty("image_base64", out var imageElement))
        {
            return imageElement.GetString();
        }

        // Fallback for other possible response formats
        if (doc.RootElement.TryGetProperty("image", out var imgElement))
        {
            return imgElement.GetString();
        }

        if (doc.RootElement.TryGetProperty("images", out var imagesElement) &&
            imagesElement.GetArrayLength() > 0)
        {
            return imagesElement[0].GetString();
        }

        return null;
    }

    /// <summary>
    /// Uses Ollama to generate a condensed image prompt in English (max 60 words for CLIP compatibility).
    /// </summary>
    private async Task<string?> GenerateImagePromptAsync(Room room, CancellationToken ct)
    {
        var contextParts = new List<string>();

        if (!string.IsNullOrEmpty(_world.Game.Theme))
            contextParts.Add($"Theme: {_world.Game.Theme}");

        contextParts.Add($"Room name: {room.Name}");
        contextParts.Add(room.IsInterior ? "Interior scene" : "Exterior/outdoor scene");

        if (!room.IsIlluminated)
            contextParts.Add("Dark/dim lighting");

        if (!string.IsNullOrEmpty(room.Description))
            contextParts.Add($"Description: {room.Description}");

        var context = string.Join(". ", contextParts);

        var ollamaPrompt = $@"Create a Stable Diffusion image prompt for PIXEL ART style. Output ONLY the prompt, nothing else.

{context}

STRICT RULES:
- Output ONLY in English, no Spanish
- Maximum 40 words
- Comma-separated descriptive terms
- MUST include: pixel art, 16-bit, retro game style, pixelated
- Include: atmospheric, detailed, panoramic view
- Focus on: colors, lighting, mood, architecture
- NO explanations, NO prefixes like 'Here is', just raw prompt text";

        var requestBody = new
        {
            model = "llama3",
            prompt = ollamaPrompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _ollamaClient.PostAsync("api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("response", out var respElement))
        {
            var result = respElement.GetString()?.Trim();
            if (string.IsNullOrEmpty(result))
                return null;

            // Clean up common prefixes that Ollama might add
            var prefixesToRemove = new[]
            {
                "Here is a concise image prompt:",
                "Here is the prompt:",
                "Here's the prompt:",
                "Image prompt:",
                "Prompt:",
                "Here it is:"
            };

            foreach (var prefix in prefixesToRemove)
            {
                if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    result = result[prefix.Length..].TrimStart();
                    break;
                }
            }

            // Clean up any quotes or extra formatting
            result = result.Trim('"', '\'', '\n', '\r');

            // Remove any remaining newlines
            result = result.Replace("\n", " ").Replace("\r", " ");

            // Collapse multiple spaces
            while (result.Contains("  "))
                result = result.Replace("  ", " ");

            return result.Trim();
        }

        return null;
    }

    private async Task<(GrammaticalGender Gender, bool IsPlural)> GetGenderForNameAsync(string name, CancellationToken ct)
    {
        var prompt = $@"Eres un asistente que determina el género gramatical en español.

Para el objeto ""{name}"", responde EXACTAMENTE con una de estas opciones:
- masculino singular (para usar ""el"")
- femenino singular (para usar ""la"")
- masculino plural (para usar ""los"")
- femenino plural (para usar ""las"")

Responde SOLO con una de esas 4 opciones, sin explicaciones.";

        var requestBody = new
        {
            model = "llama3",
            prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _ollamaClient.PostAsync("api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("response", out var respElement))
        {
            var resp = respElement.GetString()?.Trim().ToLowerInvariant() ?? "";

            // Parse the response
            bool isPlural = resp.Contains("plural");
            bool isFeminine = resp.Contains("femenino") || resp.Contains("la ") || resp.Contains("las ");

            return (isFeminine ? GrammaticalGender.Feminine : GrammaticalGender.Masculine, isPlural);
        }

        // Default to masculine singular
        return (GrammaticalGender.Masculine, false);
    }

    private async Task<string?> GenerateRoomDescriptionAsync(Room room, CancellationToken ct)
    {
        var contextInfo = new StringBuilder();

        if (room.IsInterior)
            contextInfo.Append("Es un interior. ");
        else
            contextInfo.Append("Es un exterior. ");

        if (!room.IsIlluminated)
            contextInfo.Append("Está oscuro. ");

        var roomObjects = _world.Objects.Where(o => o.RoomId == room.Id).ToList();
        if (roomObjects.Count > 0)
        {
            var objNames = string.Join(", ", roomObjects.Select(o => o.Name));
            contextInfo.Append($"Contiene: {objNames}. ");
        }

        var prompt = $@"Eres un escritor de aventuras de texto. Escribe una descripción breve y atmosférica para una sala de un juego.

Tema del mundo: {_world.Game.Theme}
Nombre de la sala: {room.Name}
Contexto: {contextInfo}

Escribe SOLO la descripción en español, 2-3 frases, sin incluir el nombre de la sala. Debe ser evocadora y ayudar al jugador a visualizar el lugar.";

        var requestBody = new
        {
            model = "llama3",
            prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _ollamaClient.PostAsync("api/generate", content, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);

        if (doc.RootElement.TryGetProperty("response", out var respElement))
        {
            return respElement.GetString()?.Trim();
        }

        return null;
    }
}
