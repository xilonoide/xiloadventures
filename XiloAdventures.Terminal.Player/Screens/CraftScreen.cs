using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Terminal.Player.Screens;

/// <summary>
/// Pantalla de fabricación
/// </summary>
public class CraftScreen
{
    private static int Width => ConsoleRenderer.ScreenWidth;

    private readonly GameState _state;
    private readonly WorldModel _world;
    private readonly string _currentRoomId;
    private readonly ConsoleInput _input;
    private readonly CraftEngine _craftEngine;

    private bool _craftEnded;
    private string? _lastMessage;

    public CraftScreen(GameState state, WorldModel world, string currentRoomId, ConsoleInput input)
    {
        _state = state;
        _world = world;
        _currentRoomId = currentRoomId;
        _input = input;
        _craftEngine = new CraftEngine(state);
    }

    /// <summary>
    /// Ejecuta la sesión de fabricación
    /// </summary>
    public void Run()
    {
        _craftEngine.StartCraft(_currentRoomId);
        _craftEngine.CraftEnded += (s, e) => _craftEnded = true;
        _craftEngine.ItemCrafted += OnItemCrafted;

        while (!_craftEnded)
        {
            Render();
            ProcessInput();
        }

        _craftEngine.CloseCraft();
    }

    private void Render()
    {
        ConsoleRenderer.Clear();

        var availableItems = _craftEngine.GetAvailableItems();
        var selectedItems = _craftEngine.GetSelectedIngredients();
        var matchingRecipe = _craftEngine.GetMatchingRecipe();

        // Encabezado
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("FABRICACION", Width);
        ConsoleRenderer.DrawSeparator(Width);

        // Materiales disponibles
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawLine($"{Colors.Bold}MATERIALES DISPONIBLES:{Colors.Reset}", Width);

        if (availableItems.Any())
        {
            var index = 1;
            foreach (var item in availableItems)
            {
                var qty = item.Quantity > 1 ? $" x{item.Quantity}" : "";
                var location = item.Location == CraftItemLocation.Inventory ? "(inv)" : "(sala)";
                var selected = item.SelectedQuantity > 0 ? $" [{item.SelectedQuantity} selec.]" : "";

                ConsoleRenderer.DrawLine(
                    $"  [{index}] {item.Name}{qty} {Colors.Gray}{location}{Colors.Reset}{Colors.Cyan}{selected}{Colors.Reset}",
                    Width);
                index++;
            }
        }
        else
        {
            ConsoleRenderer.DrawLine("  (No hay materiales disponibles)", Width, Colors.Gray);
        }

        // Ingredientes seleccionados
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawLine($"{Colors.Bold}SELECCIONADOS:{Colors.Reset}", Width);

        if (selectedItems.Any())
        {
            var selectedNames = selectedItems
                .Select(i => i.SelectedQuantity > 1 ? $"{i.Name} x{i.SelectedQuantity}" : i.Name);
            ConsoleRenderer.DrawLine($"  {string.Join(", ", selectedNames)}", Width, Colors.Yellow);
        }
        else
        {
            ConsoleRenderer.DrawLine("  (Ninguno)", Width, Colors.Gray);
        }

        // Receta coincidente
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        if (matchingRecipe != null)
        {
            ConsoleRenderer.DrawLine($"{Colors.Bold}RECETA ENCONTRADA:{Colors.Reset}", Width);
            ConsoleRenderer.DrawLine($"  {Colors.Green}{matchingRecipe.Name}{Colors.Reset}", Width);

            if (!string.IsNullOrEmpty(matchingRecipe.Description))
            {
                ConsoleRenderer.DrawWrappedText($"  {matchingRecipe.Description}", Width, Colors.Gray);
            }

            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawLine($"  {Colors.Cyan}[F] Fabricar{Colors.Reset}", Width);
        }
        else if (selectedItems.Any())
        {
            ConsoleRenderer.DrawLine($"  {Colors.Gray}No hay receta para esta combinacion{Colors.Reset}", Width);
        }

        // Mensaje de última acción
        if (!string.IsNullOrEmpty(_lastMessage))
        {
            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawLine($"  {_lastMessage}", Width, Colors.Yellow);
        }

        // Instrucciones
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawLine("Numero para seleccionar/deseleccionar, [F] fabricar, [C] limpiar, 'salir'", Width, Colors.Gray);
        ConsoleRenderer.DrawBottomBorder(Width);
    }

    private void ProcessInput()
    {
        var input = _input.ReadLine().Trim();
        _lastMessage = null;

        if (string.IsNullOrEmpty(input))
            return;

        // Salir
        if (input.Equals("salir", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("cerrar", StringComparison.OrdinalIgnoreCase))
        {
            _craftEnded = true;
            return;
        }

        // Fabricar
        if (input.Equals("f", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("fabricar", StringComparison.OrdinalIgnoreCase))
        {
            var recipe = _craftEngine.GetMatchingRecipe();
            if (recipe != null)
            {
                var result = _craftEngine.Craft();
                _lastMessage = result.Message;
            }
            else
            {
                _lastMessage = "No hay receta valida para fabricar.";
            }
            return;
        }

        // Limpiar selección
        if (input.Equals("c", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("limpiar", StringComparison.OrdinalIgnoreCase))
        {
            _craftEngine.ClearIngredients();
            _lastMessage = "Seleccion limpiada.";
            return;
        }

        // Seleccionar/deseleccionar material (número)
        if (int.TryParse(input, out int itemIndex))
        {
            var availableItems = _craftEngine.GetAvailableItems();

            if (itemIndex >= 1 && itemIndex <= availableItems.Count)
            {
                var item = availableItems[itemIndex - 1];

                // Si ya está seleccionado, deseleccionar
                if (item.SelectedQuantity > 0)
                {
                    _craftEngine.RemoveIngredient(item.ObjectId, 1);
                    _lastMessage = $"Quitado: {item.Name}";
                }
                else
                {
                    // Seleccionar
                    var availableQty = item.Quantity - item.SelectedQuantity;
                    var quantity = 1;

                    if (availableQty > 1)
                    {
                        Console.Write($"  Cantidad (1-{availableQty}): ");
                        var qtyInput = Console.ReadLine()?.Trim();
                        if (int.TryParse(qtyInput, out int qty) && qty >= 1 && qty <= availableQty)
                        {
                            quantity = qty;
                        }
                        else if (!string.IsNullOrEmpty(qtyInput))
                        {
                            _lastMessage = "Cantidad invalida.";
                            return;
                        }
                    }

                    _craftEngine.AddIngredient(item.ObjectId, quantity);
                    _lastMessage = $"Anadido: {item.Name}" + (quantity > 1 ? $" x{quantity}" : "");
                }
            }
            else
            {
                _lastMessage = "Numero no valido.";
            }
            return;
        }

        _lastMessage = "Comando no reconocido.";
    }

    private void OnItemCrafted(object? sender, CraftResult result)
    {
        if (result.Success)
        {
            var createdObject = _state.Objects.FirstOrDefault(o =>
                o.Id.Equals(result.CreatedObjectId, StringComparison.OrdinalIgnoreCase));
            _lastMessage = $"{Colors.Green}Fabricado: {createdObject?.Name ?? "item"}{Colors.Reset}";
        }
    }
}
