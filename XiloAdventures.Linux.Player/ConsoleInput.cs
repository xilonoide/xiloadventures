namespace XiloAdventures.Linux.Player;

/// <summary>
/// Maneja la entrada de consola con historial de comandos
/// </summary>
public class ConsoleInput
{
    private readonly List<string> _history = new();
    private int _historyIndex = -1;
    private const int MaxHistorySize = 100;

    /// <summary>
    /// Acción a ejecutar cuando se detecta un cambio de tamaño de consola
    /// </summary>
    public Action? OnResizeDetected { get; set; }

    // Para detectar cambios de tamaño durante la entrada
    private int _lastInputWidth;
    private int _lastInputHeight;

    /// <summary>
    /// Lee una línea de entrada sin mostrar prompt (cursor ya posicionado)
    /// </summary>
    public string ReadLineInPlace()
    {
        var input = new List<char>();
        var cursorPosition = 0;
        var startCol = Console.CursorLeft;
        var startRow = Console.CursorTop;
        _historyIndex = _history.Count;

        // Inicializar tamaño actual para detección de resize
        try
        {
            _lastInputWidth = Console.WindowWidth;
            _lastInputHeight = Console.WindowHeight;
        }
        catch { }

        while (true)
        {
            // Polling: esperar a que haya una tecla disponible
            while (!Console.KeyAvailable)
            {
                // Verificar si cambió el tamaño de la consola
                if (CheckAndHandleResize())
                {
                    // Se detectó resize, actualizar posición inicial
                    // (la pantalla ya se redibujó, pero el prompt podría haber cambiado de posición)
                    startCol = Console.CursorLeft;
                    startRow = Console.CursorTop;
                }

                // Pequeña pausa para no consumir CPU
                Thread.Sleep(50);
            }

            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    var result = new string(input.ToArray());
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        AddToHistory(result);
                    }
                    return result;

                case ConsoleKey.Backspace:
                    if (cursorPosition > 0)
                    {
                        input.RemoveAt(cursorPosition - 1);
                        cursorPosition--;
                        RefreshLineInPlace(startCol, startRow, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPosition < input.Count)
                    {
                        input.RemoveAt(cursorPosition);
                        RefreshLineInPlace(startCol, startRow, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        Console.SetCursorPosition(startCol + cursorPosition, startRow);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPosition < input.Count)
                    {
                        cursorPosition++;
                        Console.SetCursorPosition(startCol + cursorPosition, startRow);
                    }
                    break;

                case ConsoleKey.Home:
                    cursorPosition = 0;
                    Console.SetCursorPosition(startCol, startRow);
                    break;

                case ConsoleKey.End:
                    cursorPosition = input.Count;
                    Console.SetCursorPosition(startCol + cursorPosition, startRow);
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0 && _historyIndex > 0)
                    {
                        _historyIndex--;
                        SetInputFromHistoryInPlace(startCol, startRow, input, ref cursorPosition);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        SetInputFromHistoryInPlace(startCol, startRow, input, ref cursorPosition);
                    }
                    else if (_historyIndex == _history.Count - 1)
                    {
                        _historyIndex = _history.Count;
                        input.Clear();
                        cursorPosition = 0;
                        RefreshLineInPlace(startCol, startRow, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.Escape:
                    input.Clear();
                    cursorPosition = 0;
                    RefreshLineInPlace(startCol, startRow, input, cursorPosition);
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorPosition, key.KeyChar);
                        cursorPosition++;
                        RefreshLineInPlace(startCol, startRow, input, cursorPosition);
                    }
                    break;
            }
        }
    }

    private void RefreshLineInPlace(int startCol, int startRow, List<char> input, int cursorPosition)
    {
        Console.SetCursorPosition(startCol, startRow);
        var text = new string(input.ToArray());
        Console.Write(text);
        // Limpiar resto de la línea (dentro del panel izquierdo)
        var clearLen = ConsoleRenderer.LeftPanelWidth - startCol - input.Count - 3;
        if (clearLen > 0)
            Console.Write(new string(' ', clearLen));
        Console.SetCursorPosition(startCol + cursorPosition, startRow);
    }

    private void SetInputFromHistoryInPlace(int startCol, int startRow, List<char> input, ref int cursorPosition)
    {
        input.Clear();
        var historyItem = _history[_historyIndex];
        // Truncar si es muy largo para el panel
        var maxLen = ConsoleRenderer.LeftPanelWidth - startCol - 5;
        if (historyItem.Length > maxLen)
            historyItem = historyItem.Substring(0, maxLen);
        input.AddRange(historyItem);
        cursorPosition = input.Count;
        RefreshLineInPlace(startCol, startRow, input, cursorPosition);
    }

    /// <summary>
    /// Lee una línea de entrada con soporte para historial (flechas arriba/abajo)
    /// </summary>
    public string ReadLine(string prompt = "> ")
    {
        Console.Write($"{Colors.Cyan}{prompt}{Colors.Reset}");

        var input = new List<char>();
        var cursorPosition = 0;
        _historyIndex = _history.Count;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    var result = new string(input.ToArray());
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        AddToHistory(result);
                    }
                    return result;

                case ConsoleKey.Backspace:
                    if (cursorPosition > 0)
                    {
                        input.RemoveAt(cursorPosition - 1);
                        cursorPosition--;
                        RefreshLine(prompt, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (cursorPosition < input.Count)
                    {
                        input.RemoveAt(cursorPosition);
                        RefreshLine(prompt, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (cursorPosition > 0)
                    {
                        cursorPosition--;
                        Console.SetCursorPosition(prompt.Length + cursorPosition, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (cursorPosition < input.Count)
                    {
                        cursorPosition++;
                        Console.SetCursorPosition(prompt.Length + cursorPosition, Console.CursorTop);
                    }
                    break;

                case ConsoleKey.Home:
                    cursorPosition = 0;
                    Console.SetCursorPosition(prompt.Length, Console.CursorTop);
                    break;

                case ConsoleKey.End:
                    cursorPosition = input.Count;
                    Console.SetCursorPosition(prompt.Length + cursorPosition, Console.CursorTop);
                    break;

                case ConsoleKey.UpArrow:
                    if (_history.Count > 0 && _historyIndex > 0)
                    {
                        _historyIndex--;
                        SetInputFromHistory(prompt, input, ref cursorPosition);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (_historyIndex < _history.Count - 1)
                    {
                        _historyIndex++;
                        SetInputFromHistory(prompt, input, ref cursorPosition);
                    }
                    else if (_historyIndex == _history.Count - 1)
                    {
                        _historyIndex = _history.Count;
                        input.Clear();
                        cursorPosition = 0;
                        RefreshLine(prompt, input, cursorPosition);
                    }
                    break;

                case ConsoleKey.Escape:
                    input.Clear();
                    cursorPosition = 0;
                    RefreshLine(prompt, input, cursorPosition);
                    break;

                case ConsoleKey.Tab:
                    // Podría implementarse autocompletado aquí
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        input.Insert(cursorPosition, key.KeyChar);
                        cursorPosition++;
                        RefreshLine(prompt, input, cursorPosition);
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Lee un número dentro de un rango
    /// </summary>
    public int? ReadNumber(string prompt, int min, int max)
    {
        Console.Write($"{Colors.Cyan}{prompt}{Colors.Reset}");
        var input = Console.ReadLine()?.Trim();

        if (string.IsNullOrEmpty(input))
            return null;

        if (int.TryParse(input, out int number) && number >= min && number <= max)
            return number;

        return null;
    }

    /// <summary>
    /// Lee una opción de menú (número o texto)
    /// </summary>
    public string ReadMenuOption(string prompt = "> ")
    {
        return ReadLine(prompt).Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Lee confirmación Sí/No
    /// </summary>
    public bool ReadConfirmation(string prompt = "¿Continuar? (s/n): ")
    {
        Console.Write($"{Colors.Cyan}{prompt}{Colors.Reset}");
        var input = Console.ReadLine()?.Trim().ToLowerInvariant();
        return input == "s" || input == "si" || input == "sí" || input == "y" || input == "yes";
    }

    /// <summary>
    /// Espera a que el usuario presione una tecla
    /// </summary>
    public ConsoleKey WaitForKey(string message = "")
    {
        if (!string.IsNullOrEmpty(message))
            Console.Write($"{Colors.Gray}{message}{Colors.Reset}");

        return Console.ReadKey(intercept: true).Key;
    }

    /// <summary>
    /// Espera a que el usuario presione Enter
    /// </summary>
    public void WaitForEnter(string message = "Presiona Enter para continuar...")
    {
        Console.Write($"{Colors.Gray}{message}{Colors.Reset}");
        Console.ReadLine();
    }

    private void AddToHistory(string command)
    {
        // No añadir duplicados consecutivos
        if (_history.Count > 0 && _history[^1] == command)
            return;

        _history.Add(command);

        // Limitar tamaño del historial
        if (_history.Count > MaxHistorySize)
        {
            _history.RemoveAt(0);
        }
    }

    private void SetInputFromHistory(string prompt, List<char> input, ref int cursorPosition)
    {
        input.Clear();
        var historyItem = _history[_historyIndex];
        input.AddRange(historyItem);
        cursorPosition = input.Count;
        RefreshLine(prompt, input, cursorPosition);
    }

    private void RefreshLine(string prompt, List<char> input, int cursorPosition)
    {
        // Mover cursor al inicio de la línea
        Console.Write('\r');

        // Escribir prompt y contenido
        var line = $"{Colors.Cyan}{prompt}{Colors.Reset}{new string(input.ToArray())}";
        Console.Write(line);

        // Limpiar el resto de la línea
        try
        {
            var windowWidth = Console.WindowWidth;
            var clearLength = windowWidth - prompt.Length - input.Count - 1;
            if (clearLength > 0)
                Console.Write(new string(' ', clearLength));
        }
        catch
        {
            // En algunas terminales Linux Console.WindowWidth puede fallar
            // Usar un valor por defecto basado en el ancho de la pantalla
            var clearLength = ConsoleRenderer.DefaultWidth - prompt.Length - input.Count - 1;
            if (clearLength > 0)
                Console.Write(new string(' ', clearLength));
        }

        // Posicionar cursor
        Console.SetCursorPosition(prompt.Length + cursorPosition, Console.CursorTop);
    }

    /// <summary>
    /// Limpia el historial
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _historyIndex = -1;
    }

    /// <summary>
    /// Obtiene el historial de comandos
    /// </summary>
    public IReadOnlyList<string> History => _history.AsReadOnly();

    /// <summary>
    /// Verifica si el tamaño de la consola cambió y ejecuta el callback de resize si existe
    /// </summary>
    private bool CheckAndHandleResize()
    {
        try
        {
            var currentWidth = Console.WindowWidth;
            var currentHeight = Console.WindowHeight;

            if (currentWidth != _lastInputWidth || currentHeight != _lastInputHeight)
            {
                _lastInputWidth = currentWidth;
                _lastInputHeight = currentHeight;

                // Ejecutar callback de resize si está configurado
                OnResizeDetected?.Invoke();
                return true;
            }
        }
        catch
        {
            // Ignorar errores al leer el tamaño
        }
        return false;
    }
}
