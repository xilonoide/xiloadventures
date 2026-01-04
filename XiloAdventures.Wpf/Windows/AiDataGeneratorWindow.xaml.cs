using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

/// <summary>
/// Modo de plataforma para generación de imágenes.
/// [RESERVED FOR FUTURE USE - DO NOT DELETE]
/// Currently only Windows mode is used. Linux ASCII mode is disabled but preserved.
/// </summary>
public enum ImagePlatformMode
{
    /// <summary>Imágenes gráficas para Windows (PNG/JPG).</summary>
    Windows,
    /// <summary>Arte ASCII para Linux (terminal). [RESERVED - Currently disabled]</summary>
    Linux
}

public partial class AiDataGeneratorWindow : Window
{
    private readonly WorldModel _world;
    private readonly string? _worldPath;
    private CancellationTokenSource? _cts;
    private bool _isProcessing;
    private bool _imagePreviewShown;

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

    public AiDataGeneratorWindow(WorldModel world, string? worldPath = null)
    {
        InitializeComponent();
        _world = world;
        _worldPath = worldPath;
    }

    private void SaveWorldIfPathAvailable()
    {
        if (string.IsNullOrEmpty(_worldPath))
        {
            System.Diagnostics.Debug.WriteLine("SaveWorldIfPathAvailable: No path available, skipping save");
            return;
        }

        try
        {
            WorldLoader.SaveWorldModel(_world, _worldPath);
            System.Diagnostics.Debug.WriteLine($"SaveWorldIfPathAvailable: Saved to {_worldPath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveWorldIfPathAvailable: Error saving - {ex.Message}");
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // CenterOwner handles initial positioning
    }

    private void RecenterWindow()
    {
        if (Owner != null)
        {
            Dispatcher.BeginInvoke(() =>
            {
                Left = Owner.Left + (Owner.Width - ActualWidth) / 2;
                Top = Owner.Top + (Owner.Height - ActualHeight) / 2;
            }, System.Windows.Threading.DispatcherPriority.Render);
        }
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
        // Validate Theme (required for descriptions and image prompts)
        if (string.IsNullOrWhiteSpace(_world.Game.Theme))
        {
            DarkErrorDialog.Show("Tema requerido",
                "El mundo necesita tener un tema/ambientación definido para generar descripciones e imágenes.\n\n" +
                "Añade un tema en las propiedades del mundo (ej: 'fantasía medieval', 'ciencia ficción', 'horror gótico').", this);
            return;
        }

        // === ARTÍCULOS ===
        var objectsWithGender = _world.Objects.Where(o => o.GenderAndPluralSetManually).ToList();
        var objectsWithoutGender = _world.Objects.Where(o => !o.GenderAndPluralSetManually).ToList();
        var doorsWithGender = _world.Doors.Where(d => d.GenderAndPluralSetManually).ToList();
        var doorsWithoutGender = _world.Doors.Where(d => !d.GenderAndPluralSetManually).ToList();

        List<GameObject> objectsToFix;
        List<Door> doorsToFix;

        int totalWithGender = objectsWithGender.Count + doorsWithGender.Count;
        if (totalWithGender > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Artículos: ",
                "elementos existentes",
                $"{totalWithGender} elemento(s) ya tienen género asignado.\n" +
                $"{objectsWithoutGender.Count + doorsWithoutGender.Count} elemento(s) sin género.\n\n" +
                "¿Qué deseas hacer con los artículos?",
                "Solo pendientes",
                "Procesar todos",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            if (choice == ChoiceResult.Option1)
            {
                objectsToFix = objectsWithoutGender;
                doorsToFix = doorsWithoutGender;
            }
            else
            {
                objectsToFix = _world.Objects.ToList();
                doorsToFix = _world.Doors.ToList();
            }
        }
        else
        {
            objectsToFix = objectsWithoutGender;
            doorsToFix = doorsWithoutGender;
        }

        // === DESCRIPCIONES ===
        var roomsWithDesc = _world.Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Description)).ToList();
        var roomsWithoutDesc = _world.Rooms
            .Where(r => string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        List<Room> roomsToDescribe;

        if (roomsWithDesc.Count > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Descripciones: ",
                "salas existentes",
                $"{roomsWithDesc.Count} sala(s) ya tienen descripción.\n" +
                $"{roomsWithoutDesc.Count} sala(s) sin descripción.\n\n" +
                "¿Qué deseas hacer con las descripciones?",
                "Solo vacías",
                "Generar todas",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            roomsToDescribe = choice == ChoiceResult.Option1
                ? roomsWithoutDesc
                : _world.Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();
        }
        else
        {
            roomsToDescribe = roomsWithoutDesc;
        }

        // === IMÁGENES ===
        var roomsWithImages = _world.Rooms
            .Where(r => !string.IsNullOrEmpty(r.ImageId) || !string.IsNullOrEmpty(r.ImageBase64))
            .ToList();
        var roomsWithoutImages = _world.Rooms
            .Where(r => string.IsNullOrEmpty(r.ImageId) && string.IsNullOrEmpty(r.ImageBase64))
            .ToList();

        List<Room> roomsToGenerate;
        ImagePlatformMode platformMode = ImagePlatformMode.Windows;

        if (roomsWithImages.Count > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Imágenes: ",
                "salas existentes",
                $"{roomsWithImages.Count} sala(s) ya tienen imagen.\n" +
                $"{roomsWithoutImages.Count} sala(s) sin imagen.\n\n" +
                "¿Qué deseas hacer con las imágenes?",
                "Solo vacías",
                "Generar todas",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            roomsToGenerate = choice == ChoiceResult.Option1
                ? roomsWithoutImages
                : _world.Rooms.ToList();
        }
        else
        {
            roomsToGenerate = roomsWithoutImages;
        }

        // [RESERVED FOR FUTURE USE - DO NOT DELETE]
        // Platform selection dialog for Windows/Linux ASCII mode.
        // Currently disabled - always uses Windows mode.
        // To re-enable: uncomment the block below and the ASCII conversion blocks.
        /*
        if (roomsToGenerate.Count > 0)
        {
            var platformChoice = DarkChoiceDialog.Show(
                "Plataforma: ",
                "tipo de imagen",
                "¿Para qué plataforma son las imágenes?\n\n" +
                "• Windows: Imágenes gráficas PNG\n" +
                "• Linux: Arte ASCII para terminal",
                "Windows (Gráficos)",
                "Linux (ASCII)",
                this);

            if (platformChoice == ChoiceResult.Cancel)
                return;

            platformMode = platformChoice == ChoiceResult.Option1
                ? ImagePlatformMode.Windows
                : ImagePlatformMode.Linux;
        }
        */

        // Nothing to do?
        int totalArticles = objectsToFix.Count + doorsToFix.Count;
        if (totalArticles == 0 && roomsToDescribe.Count == 0 && roomsToGenerate.Count == 0)
        {
            DarkErrorDialog.Show("Nada que hacer",
                "No hay elementos pendientes para procesar.", this);
            return;
        }

        // Build task summary
        var tasks = new List<string>();
        if (totalArticles > 0)
            tasks.Add($"• Corregir artículos: {objectsToFix.Count} objeto(s) y {doorsToFix.Count} puerta(s)");
        if (roomsToDescribe.Count > 0)
            tasks.Add($"• Crear descripciones: {roomsToDescribe.Count} sala(s)");
        if (roomsToGenerate.Count > 0)
            tasks.Add($"• Generar imágenes: {roomsToGenerate.Count} sala(s)");

        var confirmMessage = "Se ejecutarán las siguientes tareas:\n\n" + string.Join("\n", tasks);
        if (roomsToGenerate.Count > 0)
        {
            confirmMessage += "\n\n⚠️ La generación de imágenes es EXTREMADAMENTE LENTA.\n" +
                              "Puede tardar varios minutos por imagen.";
        }
        confirmMessage += "\n\n¿Continuar?";

        if (!DarkConfirmDialog.Show("Confirmar proceso completo", confirmMessage, this))
            return;

        // Determine if we need Stable Diffusion
        bool needsStableDiffusion = roomsToGenerate.Count > 0;

        // Ensure Docker is ready
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: needsStableDiffusion))
            return;

        // Calculate total steps
        int totalSteps = roomsToDescribe.Count + objectsToFix.Count + doorsToFix.Count + roomsToGenerate.Count;

        await RunProcessAsync(
            "Ejecutando proceso completo...",
            totalSteps,
            async (progress, ct) =>
            {
                int currentStep = 0;

                // Step 1: Create descriptions
                if (roomsToDescribe.Count > 0)
                {
                    for (int i = 0; i < roomsToDescribe.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var room = roomsToDescribe[i];
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
                            obj.GenderAndPluralSetManually = true;
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
                            door.GenderAndPluralSetManually = true;
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
                if (roomsToGenerate.Count > 0)
                {
                    for (int i = 0; i < roomsToGenerate.Count; i++)
                    {
                        ct.ThrowIfCancellationRequested();
                        var room = roomsToGenerate[i];
                        var modeLabel = platformMode == ImagePlatformMode.Linux ? "ASCII" : "imagen";
                        progress($"[3/3] Generando {modeLabel}: {room.Name}...", currentStep);

                        try
                        {
                            var imageBase64 = await GenerateRoomImageAsync(room, platformMode, ct);
                            if (!string.IsNullOrEmpty(imageBase64))
                            {
                                // [RESERVED FOR FUTURE USE - DO NOT DELETE]
                                // ASCII conversion for Linux terminal mode.
                                /*
                                if (platformMode == ImagePlatformMode.Linux)
                                {
                                    // For Linux, convert to ASCII and store in AsciiImage
                                    var asciiArt = AsciiConverter.ConvertFromBase64(imageBase64, 160);
                                    room.AsciiImage = asciiArt;
                                    room.ImageBase64 = imageBase64; // Also keep the original
                                }
                                else
                                {
                                    room.ImageBase64 = imageBase64;
                                }
                                */
                                room.ImageBase64 = imageBase64;
                                room.ImageId = null;
                                ShowImagePreview(imageBase64, room.Name);
                                SaveWorldIfPathAvailable();
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

                HideImagePreview();
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

        // Count rooms with and without images
        var roomsWithImages = _world.Rooms
            .Where(r => !string.IsNullOrEmpty(r.ImageId) || !string.IsNullOrEmpty(r.ImageBase64))
            .ToList();

        var roomsWithoutImages = _world.Rooms
            .Where(r => string.IsNullOrEmpty(r.ImageId) && string.IsNullOrEmpty(r.ImageBase64))
            .ToList();

        List<Room> roomsToGenerate;
        ImagePlatformMode platformMode = ImagePlatformMode.Windows;

        // If some rooms already have images, ask user what to do
        if (roomsWithImages.Count > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Imágenes: ",
                "salas con imágenes existentes",
                $"{roomsWithImages.Count} sala(s) ya tienen imagen.\n" +
                $"{roomsWithoutImages.Count} sala(s) no tienen imagen.\n\n" +
                "¿Qué deseas hacer?",
                "Solo vacías",
                "Generar todas",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            roomsToGenerate = choice == ChoiceResult.Option1
                ? roomsWithoutImages
                : _world.Rooms.ToList();

            if (roomsToGenerate.Count == 0)
            {
                DarkErrorDialog.Show("Sin salas pendientes",
                    "No hay salas sin imagen para generar.", this);
                return;
            }
        }
        else
        {
            // No rooms have images, generate all
            roomsToGenerate = _world.Rooms.ToList();
        }

        // [RESERVED FOR FUTURE USE - DO NOT DELETE]
        // Platform selection dialog for Windows/Linux ASCII mode.
        // Currently disabled - always uses Windows mode.
        /*
        var platformChoice = DarkChoiceDialog.Show(
            "Plataforma: ",
            "tipo de imagen",
            "¿Para qué plataforma son las imágenes?\n\n" +
            "• Windows: Imágenes gráficas PNG\n" +
            "• Linux: Arte ASCII para terminal",
            "Windows (Gráficos)",
            "Linux (ASCII)",
            this);

        if (platformChoice == ChoiceResult.Cancel)
            return;

        platformMode = platformChoice == ChoiceResult.Option1
            ? ImagePlatformMode.Windows
            : ImagePlatformMode.Linux;
        */

        // Confirm
        var confirmMessage = roomsToGenerate.Count == _world.Rooms.Count && roomsWithImages.Count > 0
            ? $"Se generarán imágenes para TODAS las {roomsToGenerate.Count} sala(s), sobrescribiendo las existentes.\n\n"
            : $"Se generarán imágenes para {roomsToGenerate.Count} sala(s).\n\n";

        if (!DarkConfirmDialog.Show("Confirmar generación",
            confirmMessage +
            "PROCESO EXTREMADAMENTE LENTO\n" +
            "Este proceso puede tardar varios minutos por imagen dependiendo de tu hardware.\n\n" +
            "¿Continuar?", this))
            return;

        // Ensure Docker + Stable Diffusion + Ollama are running (Ollama for prompt summarization)
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: true))
            return;

        var modeLabel = platformMode == ImagePlatformMode.Linux ? "ASCII" : "imágenes";
        await RunProcessAsync(
            $"Generando {modeLabel} ({roomsToGenerate.Count} salas)",
            roomsToGenerate.Count,
            async (progress, ct) =>
            {
                for (int i = 0; i < roomsToGenerate.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var room = roomsToGenerate[i];
                    var itemLabel = platformMode == ImagePlatformMode.Linux ? "ASCII" : "imagen";
                    progress($"Generando {itemLabel}: {room.Name}...", i);

                    try
                    {
                        var imageBase64 = await GenerateRoomImageAsync(room, platformMode, ct);

                        if (!string.IsNullOrEmpty(imageBase64))
                        {
                            // [RESERVED FOR FUTURE USE - DO NOT DELETE]
                            // ASCII conversion for Linux terminal mode.
                            /*
                            if (platformMode == ImagePlatformMode.Linux)
                            {
                                // For Linux, convert to ASCII and store in AsciiImage
                                var asciiArt = AsciiConverter.ConvertFromBase64(imageBase64, 160);
                                room.AsciiImage = asciiArt;
                                room.ImageBase64 = imageBase64; // Also keep the original
                            }
                            else
                            {
                                room.ImageBase64 = imageBase64;
                            }
                            */
                            room.ImageBase64 = imageBase64;
                            room.ImageId = null; // Mark as AI-generated
                            ShowImagePreview(imageBase64, room.Name);
                            SaveWorldIfPathAvailable();
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
                HideImagePreview();
                progress("Completado", roomsToGenerate.Count);
            });
    }

    private async void CorrectArticlesButton_Click(object sender, RoutedEventArgs e)
    {
        // Count objects and doors with and without gender manually set
        var objectsWithGender = _world.Objects.Where(o => o.GenderAndPluralSetManually).ToList();
        var objectsWithoutGender = _world.Objects.Where(o => !o.GenderAndPluralSetManually).ToList();
        var doorsWithGender = _world.Doors.Where(d => d.GenderAndPluralSetManually).ToList();
        var doorsWithoutGender = _world.Doors.Where(d => !d.GenderAndPluralSetManually).ToList();

        int totalWithGender = objectsWithGender.Count + doorsWithGender.Count;
        int totalWithoutGender = objectsWithoutGender.Count + doorsWithoutGender.Count;

        List<GameObject> objectsToFix;
        List<Door> doorsToFix;

        // If some elements already have gender set, ask user what to do
        if (totalWithGender > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Artículos: ",
                "elementos con género existente",
                $"{totalWithGender} elemento(s) ya tienen género asignado.\n" +
                $"{totalWithoutGender} elemento(s) no tienen género.\n\n" +
                "¿Qué deseas hacer?",
                "Solo pendientes",
                "Procesar todos",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            if (choice == ChoiceResult.Option1)
            {
                objectsToFix = objectsWithoutGender;
                doorsToFix = doorsWithoutGender;
            }
            else
            {
                objectsToFix = _world.Objects.ToList();
                doorsToFix = _world.Doors.ToList();
            }

            if (objectsToFix.Count + doorsToFix.Count == 0)
            {
                DarkErrorDialog.Show("Sin elementos pendientes",
                    "No hay elementos sin género para procesar.", this);
                return;
            }
        }
        else
        {
            objectsToFix = objectsWithoutGender;
            doorsToFix = doorsWithoutGender;

            if (objectsToFix.Count + doorsToFix.Count == 0)
            {
                DarkErrorDialog.Show("Sin elementos pendientes",
                    "Todos los objetos y puertas ya tienen género asignado.", this);
                return;
            }
        }

        int totalToFix = objectsToFix.Count + doorsToFix.Count;

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
                        obj.GenderAndPluralSetManually = true;
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
                        door.GenderAndPluralSetManually = true;
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

        // Count rooms with and without description
        var roomsWithDesc = _world.Rooms
            .Where(r => !string.IsNullOrWhiteSpace(r.Description))
            .ToList();

        var roomsWithoutDesc = _world.Rooms
            .Where(r => string.IsNullOrWhiteSpace(r.Description) && !string.IsNullOrWhiteSpace(r.Name))
            .ToList();

        List<Room> roomsToDescribe;

        // If some rooms already have descriptions, ask user what to do
        if (roomsWithDesc.Count > 0)
        {
            var choice = DarkChoiceDialog.Show(
                "Descripciones: ",
                "salas con descripción existente",
                $"{roomsWithDesc.Count} sala(s) ya tienen descripción.\n" +
                $"{roomsWithoutDesc.Count} sala(s) no tienen descripción.\n\n" +
                "¿Qué deseas hacer?",
                "Solo vacías",
                "Generar todas",
                this);

            if (choice == ChoiceResult.Cancel)
                return;

            roomsToDescribe = choice == ChoiceResult.Option1
                ? roomsWithoutDesc
                : _world.Rooms.Where(r => !string.IsNullOrWhiteSpace(r.Name)).ToList();

            if (roomsToDescribe.Count == 0)
            {
                DarkErrorDialog.Show("Sin salas pendientes",
                    "No hay salas sin descripción para generar.", this);
                return;
            }
        }
        else
        {
            roomsToDescribe = roomsWithoutDesc;

            if (roomsToDescribe.Count == 0)
            {
                DarkErrorDialog.Show("Sin salas pendientes",
                    "Todas las salas ya tienen descripción.", this);
                return;
            }
        }

        var confirmMessage = roomsToDescribe.Count == _world.Rooms.Count && roomsWithDesc.Count > 0
            ? $"Se crearán descripciones para TODAS las {roomsToDescribe.Count} sala(s), sobrescribiendo las existentes.\n\n"
            : $"Se crearán descripciones para {roomsToDescribe.Count} sala(s).\n\n";

        if (!DarkConfirmDialog.Show("Confirmar creación", confirmMessage + "¿Continuar?", this))
            return;

        // Ensure Docker + Ollama are running (no need for Stable Diffusion)
        if (!await EnsureDockerReadyAsync(includeOllama: true, includeStableDiffusion: false))
            return;

        await RunProcessAsync(
            $"Creando descripciones ({roomsToDescribe.Count} salas)",
            roomsToDescribe.Count,
            async (progress, ct) =>
            {
                for (int i = 0; i < roomsToDescribe.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    var room = roomsToDescribe[i];
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
                progress("Completado", roomsToDescribe.Count);
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
        TimeRemainingLabel.Text = "";

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await work((status, current) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ProgressLabel.Text = status;
                    ProgressBar.Value = current;
                    ProgressStatus.Text = $"{current} / {total}";

                    // Calculate time remaining
                    if (current > 0)
                    {
                        var elapsed = stopwatch.Elapsed;
                        var avgPerItem = elapsed.TotalSeconds / current;
                        var remaining = (total - current) * avgPerItem;
                        var remainingTime = TimeSpan.FromSeconds(remaining);

                        if (remainingTime.TotalHours >= 1)
                        {
                            TimeRemainingLabel.Text = $"⏱️ {(int)remainingTime.TotalHours}h {remainingTime.Minutes:D2}m restantes";
                        }
                        else if (remainingTime.TotalMinutes >= 1)
                        {
                            TimeRemainingLabel.Text = $"⏱️ {remainingTime.Minutes}m {remainingTime.Seconds:D2}s restantes";
                        }
                        else
                        {
                            TimeRemainingLabel.Text = $"⏱️ {remainingTime.Seconds}s restantes";
                        }
                    }
                });
            }, _cts.Token);

            stopwatch.Stop();
            Dispatcher.Invoke(() =>
            {
                ProgressBar.Value = total;
                ProgressStatus.Text = $"{total} / {total}";
                ProgressLabel.Text = "Proceso completado";
                var totalTime = stopwatch.Elapsed;
                if (totalTime.TotalHours >= 1)
                {
                    TimeRemainingLabel.Text = $"✅ Completado en {(int)totalTime.TotalHours}h {totalTime.Minutes:D2}m";
                }
                else if (totalTime.TotalMinutes >= 1)
                {
                    TimeRemainingLabel.Text = $"✅ Completado en {totalTime.Minutes}m {totalTime.Seconds:D2}s";
                }
                else
                {
                    TimeRemainingLabel.Text = $"✅ Completado en {totalTime.Seconds}s";
                }
            });
        }
        catch (OperationCanceledException)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressLabel.Text = "Proceso cancelado";
                TimeRemainingLabel.Text = "";
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                DarkErrorDialog.Show("Error", $"Error durante el proceso: {ex.Message}", this);
                TimeRemainingLabel.Text = "";
            });
        }
        finally
        {
            _isProcessing = false;
            _imagePreviewShown = false;
            SetButtonsEnabled(true);
            CancelButton.Visibility = Visibility.Collapsed;
            ProgressSection.Visibility = Visibility.Collapsed;
            ImagePreviewSection.Visibility = Visibility.Collapsed;
            ImagePreview.Source = null;
        }
    }

    private void SetButtonsEnabled(bool enabled)
    {
        DoAllButton.IsEnabled = enabled;
        GenerateImagesButton.IsEnabled = enabled;
        CorrectArticlesButton.IsEnabled = enabled;
        CreateDescriptionsButton.IsEnabled = enabled;
    }

    private void ShowImagePreview(string base64, string roomName)
    {
        Dispatcher.Invoke(() =>
        {
            try
            {
                var bytes = Convert.FromBase64String(base64);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = new MemoryStream(bytes);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                ImagePreview.Source = bitmap;
                ImagePreviewLabel.Text = $"🖼️ {roomName}";

                bool wasHidden = ImagePreviewSection.Visibility != Visibility.Visible;
                ImagePreviewSection.Visibility = Visibility.Visible;

                // Recenter window when preview first appears (window size changed)
                if (wasHidden && !_imagePreviewShown)
                {
                    _imagePreviewShown = true;
                    RecenterWindow();
                }
            }
            catch
            {
                // Ignore preview errors
            }
        });
    }

    private void HideImagePreview()
    {
        Dispatcher.Invoke(() =>
        {
            ImagePreviewSection.Visibility = Visibility.Collapsed;
            ImagePreview.Source = null;
        });
    }

    private async Task<string?> GenerateRoomImageAsync(Room room, ImagePlatformMode platformMode, CancellationToken ct)
    {
        string prompt;
        string negativePrompt;
        int steps;
        double guidanceScale;

        // [RESERVED FOR FUTURE USE - DO NOT DELETE]
        // Linux ASCII mode prompt generation - currently disabled.
        /*
        if (platformMode == ImagePlatformMode.Linux)
        {
            // Linux ASCII mode: Generate high-contrast images optimized for ASCII conversion
            // CLIP has 77 token limit, so keep prompt concise with key terms first
            var roomContext = await GenerateAsciiPromptAsync(room, ct);
            var baseContext = !string.IsNullOrEmpty(roomContext)
                ? roomContext
                : $"{room.Name}";

            // Optimized for 77 tokens: cinematic high-contrast for ASCII conversion
            prompt = $"panoramic {baseContext}, high contrast silhouette, monochrome green phosphor on black, CRT glow, large solid shapes, clear edges, minimal detail, cinematic lighting";

            negativePrompt = "text, letters, symbols, words, fine detail, noise, blur, colors";

            steps = 35;          // DPM++ 2M Karras, 30-40 steps
            guidanceScale = 7.0; // CFG 6-8
        }
        else
        */
        {
            // Windows mode: Standard 16-bit style image
            var generatedPrompt = await GenerateImagePromptAsync(room, ct);
            prompt = !string.IsNullOrEmpty(generatedPrompt)
                ? generatedPrompt
                : $"16-bit, {_world.Game.Theme ?? "fantasy"}, {room.Name}, atmospheric, detailed";

            negativePrompt = "text, watermark, signature, blurry, low quality, deformed";
            steps = 10;          // DPM++ with Karras works well at 10 steps
            guidanceScale = 7.0;
        }

        // Optimized for RTX 3080 Ti - using DPM++ 2M Karras scheduler for faster convergence
        var requestBody = new
        {
            modelInputs = new
            {
                prompt,
                negative_prompt = negativePrompt,
                width = 1408,
                height = 320,
                num_inference_steps = steps,
                guidance_scale = guidanceScale
            },
            callInputs = new
            {
                MODEL_ID = "runwayml/stable-diffusion-v1-5",
                PIPELINE = "StableDiffusionPipeline",
                SCHEDULER = "DPMSolverMultistepScheduler",  // Much faster than EulerAncestral
                use_karras_sigmas = true,       // Better quality at low step counts
                safety_checker = false,         // Skip NSFW check for speed
                requires_safety_checker = false,
                enable_attention_slicing = false,  // Not needed with enough VRAM (12GB)
                xformers_memory_efficient_attention = true,  // Use xformers for speed
                torch_dtype = "float16"         // FP16 is faster on RTX 3080 Ti
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

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
    /// Uses Ollama to generate a condensed image prompt from room details.
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

        var ollamaPrompt = $@"Translate the following to English and create a Stable Diffusion image prompt for 16-bit style. Output ONLY the prompt, nothing else.

{context}

STRICT RULES:
- TRANSLATE everything to English first (theme, room name, description)
- Output ONLY in English, absolutely no Spanish words
- Maximum 40 words
- Comma-separated descriptive terms
- MUST include: 16-bit videogame
- Include: detailed, panoramic view
- Focus on: colors, lighting, mood, architecture
- NO explanations, NO prefixes like 'Here is', just raw prompt text";

        try
        {
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
                if (!string.IsNullOrEmpty(result))
                {
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

                    // Clean up quotes and formatting
                    result = result.Trim('"', '\'', '\n', '\r');
                    result = result.Replace("\n", " ").Replace("\r", " ");
                    while (result.Contains("  "))
                        result = result.Replace("  ", " ");

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating prompt with Ollama: {ex.Message}");
        }

        return null;
    }

    /// <summary>
    /// Uses Ollama to generate a very short scene description for ASCII art (max 20 words).
    /// [RESERVED FOR FUTURE USE - DO NOT DELETE]
    /// This method is currently unused but preserved for future Linux ASCII mode.
    /// </summary>
    private async Task<string?> GenerateAsciiPromptAsync(Room room, CancellationToken ct)
    {
        var contextParts = new List<string>();

        if (!string.IsNullOrEmpty(_world.Game.Theme))
            contextParts.Add($"Theme: {_world.Game.Theme}");

        contextParts.Add($"Room: {room.Name}");

        if (!string.IsNullOrEmpty(room.Description))
            contextParts.Add($"Description: {room.Description}");

        var context = string.Join(". ", contextParts);

        var ollamaPrompt = $@"Create a very short scene description in English. Output ONLY the description, nothing else.

{context}

STRICT RULES:
- Maximum 15 words
- Describe ONLY the scene/location (not art style)
- Translate to English if needed
- Example: ""dark spaceship corridor with red emergency lights""
- NO quotes, NO explanations, just the scene";

        try
        {
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
                if (!string.IsNullOrEmpty(result))
                {
                    // Clean up quotes and formatting
                    result = result.Trim('"', '\'', '\n', '\r');
                    result = result.Replace("\n", " ").Replace("\r", " ");
                    while (result.Contains("  "))
                        result = result.Replace("  ", " ");

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating ASCII prompt with Ollama: {ex.Message}");
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
