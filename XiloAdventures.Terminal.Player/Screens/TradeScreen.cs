using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;

namespace XiloAdventures.Terminal.Player.Screens;

/// <summary>
/// Pantalla de comercio
/// </summary>
public class TradeScreen
{
    private static int Width => ConsoleRenderer.ScreenWidth;

    private readonly GameState _state;
    private readonly WorldModel _world;
    private readonly Npc _merchant;
    private readonly ConsoleInput _input;
    private readonly TradeEngine _tradeEngine;

    private bool _tradeEnded;
    private string? _lastMessage;

    public TradeScreen(GameState state, WorldModel world, Npc merchant, ConsoleInput input)
    {
        _state = state;
        _world = world;
        _merchant = merchant;
        _input = input;
        _tradeEngine = new TradeEngine(state);
    }

    /// <summary>
    /// Ejecuta la sesión de comercio
    /// </summary>
    public void Run()
    {
        _tradeEngine.StartTrade(_merchant);
        _tradeEngine.TradeEnded += (s, e) => _tradeEnded = true;

        while (!_tradeEnded)
        {
            Render();
            ProcessInput();
        }

        _tradeEngine.CloseTrade();
    }

    private void Render()
    {
        ConsoleRenderer.Clear();

        var merchantItems = _tradeEngine.GetNpcItems();
        var playerItems = _tradeEngine.GetPlayerItems();
        var playerMoney = _tradeEngine.GetPlayerMoney();
        var merchantMoney = _tradeEngine.NpcHasInfiniteMoney()
            ? "Infinito"
            : _tradeEngine.GetNpcMoney().ToString("N0");

        // Encabezado
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle($"COMERCIO - {_merchant.Name}", Width);
        ConsoleRenderer.DrawLine($"Tu dinero: {Colors.Yellow}{playerMoney}{Colors.Reset} monedas", Width);
        ConsoleRenderer.DrawSeparator(Width);

        // Columnas: Comprar | Vender
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawLine($"{Colors.Bold}  COMPRAR                              VENDER{Colors.Reset}", Width);
        ConsoleRenderer.DrawLine("  ---------                            --------", Width, Colors.Gray);

        // Calcular máximo de filas
        var maxRows = Math.Max(merchantItems.Count, playerItems.Count);
        maxRows = Math.Max(maxRows, 1);

        for (int i = 0; i < maxRows; i++)
        {
            var leftCol = "";
            var rightCol = "";

            // Item del comerciante (para comprar)
            if (i < merchantItems.Count)
            {
                var item = merchantItems[i];
                var qty = item.Quantity > 1 ? $"x{item.Quantity}" : "";
                leftCol = $"[{i + 1}] {Truncate(item.Name, 18)}{qty} {item.CalculatedPrice}$";
            }

            // Item del jugador (para vender)
            if (i < playerItems.Count)
            {
                var item = playerItems[i];
                var letter = (char)('A' + i);
                var qty = item.Quantity > 1 ? $"x{item.Quantity}" : "";
                rightCol = $"[{letter}] {Truncate(item.Name, 18)}{qty} {item.CalculatedPrice}$";
            }

            // Formatear línea con dos columnas
            var line = $"  {leftCol,-34}  {rightCol}";
            ConsoleRenderer.DrawLine(line, Width);
        }

        if (merchantItems.Count == 0 && playerItems.Count == 0)
        {
            ConsoleRenderer.DrawLine("  (Sin items para comerciar)", Width, Colors.Gray);
        }

        // Mensaje de última acción
        if (!string.IsNullOrEmpty(_lastMessage))
        {
            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawSeparator(Width, thin: true);
            ConsoleRenderer.DrawLine($"  {_lastMessage}", Width, Colors.Yellow);
        }

        // Instrucciones
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawLine("Numero para comprar, letra para vender, 'salir' para cerrar", Width, Colors.Gray);
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
            _tradeEnded = true;
            return;
        }

        var merchantItems = _tradeEngine.GetNpcItems();
        var playerItems = _tradeEngine.GetPlayerItems();

        // Comprar (número)
        if (int.TryParse(input, out int buyIndex))
        {
            if (buyIndex >= 1 && buyIndex <= merchantItems.Count)
            {
                var item = merchantItems[buyIndex - 1];
                var quantity = 1;

                // Si hay más de 1, preguntar cantidad
                if (item.Quantity > 1)
                {
                    var maxQty = _tradeEngine.GetMaxBuyQuantity(item.ObjectId);
                    Console.Write($"  Cantidad (1-{maxQty}): ");
                    var qtyInput = Console.ReadLine()?.Trim();
                    if (int.TryParse(qtyInput, out int qty) && qty >= 1 && qty <= maxQty)
                    {
                        quantity = qty;
                    }
                    else if (!string.IsNullOrEmpty(qtyInput))
                    {
                        _lastMessage = "Cantidad invalida.";
                        return;
                    }
                }

                var result = _tradeEngine.BuyItem(item.ObjectId, quantity);
                _lastMessage = result.Message;
            }
            else
            {
                _lastMessage = "Opcion no valida.";
            }
            return;
        }

        // Vender (letra)
        if (input.Length == 1 && char.IsLetter(input[0]))
        {
            var sellIndex = char.ToUpper(input[0]) - 'A';
            if (sellIndex >= 0 && sellIndex < playerItems.Count)
            {
                var item = playerItems[sellIndex];
                var quantity = 1;

                // Si hay más de 1, preguntar cantidad
                if (item.Quantity > 1)
                {
                    var maxQty = _tradeEngine.GetMaxSellQuantity(item.ObjectId);
                    Console.Write($"  Cantidad (1-{maxQty}): ");
                    var qtyInput = Console.ReadLine()?.Trim();
                    if (int.TryParse(qtyInput, out int qty) && qty >= 1 && qty <= maxQty)
                    {
                        quantity = qty;
                    }
                    else if (!string.IsNullOrEmpty(qtyInput))
                    {
                        _lastMessage = "Cantidad invalida.";
                        return;
                    }
                }

                var result = _tradeEngine.SellItem(item.ObjectId, quantity);
                _lastMessage = result.Message;
            }
            else
            {
                _lastMessage = "Opcion no valida.";
            }
            return;
        }

        _lastMessage = "Comando no reconocido.";
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 2) + "..";
    }
}
