using System.Text;

namespace XiloAdventures.Linux.Player;

/// <summary>
/// Colores ANSI para la consola
/// </summary>
public static class Colors
{
    public const string Reset = "\x1b[0m";
    public const string Bold = "\x1b[1m";
    public const string Dim = "\x1b[2m";
    public const string Italic = "\x1b[3m";
    public const string Underline = "\x1b[4m";

    // Colores de primer plano (texto)
    public const string Black = "\x1b[30m";
    public const string Red = "\x1b[91m";
    public const string Green = "\x1b[92m";
    public const string Yellow = "\x1b[93m";
    public const string Blue = "\x1b[94m";
    public const string Magenta = "\x1b[95m";
    public const string Cyan = "\x1b[96m";
    public const string White = "\x1b[97m";
    public const string Gray = "\x1b[90m";

    // Colores de fondo
    public const string BgBlack = "\x1b[40m";
    public const string BgRed = "\x1b[41m";
    public const string BgGreen = "\x1b[42m";
    public const string BgYellow = "\x1b[43m";
    public const string BgBlue = "\x1b[44m";
    public const string BgMagenta = "\x1b[45m";
    public const string BgCyan = "\x1b[46m";
    public const string BgWhite = "\x1b[47m";

    // Colores semánticos del juego
    public const string Health = Red;
    public const string Mana = Blue;
    public const string Energy = Yellow;
    public const string Sanity = Magenta;
    public const string Money = Yellow;
    public const string Success = Green;
    public const string Error = Red;
    public const string Info = Cyan;
    public const string Title = Bold + White;
    public const string Subtitle = Gray;
    public const string Exit = Cyan;
    public const string Npc = Green;
    public const string Object = Yellow;
    public const string Enemy = Red;
}

/// <summary>
/// Renderizador de consola con soporte para colores ANSI y caracteres Unicode
/// </summary>
public static class ConsoleRenderer
{
    // Caracteres de marco Unicode
    public const char TopLeft = '\u2554';      // ╔
    public const char TopRight = '\u2557';     // ╗
    public const char BottomLeft = '\u255A';   // ╚
    public const char BottomRight = '\u255D';  // ╝
    public const char Horizontal = '\u2550';   // ═
    public const char Vertical = '\u2551';     // ║
    public const char LeftT = '\u2560';        // ╠
    public const char RightT = '\u2563';       // ╣
    public const char TopT = '\u2566';         // ╦
    public const char BottomT = '\u2569';      // ╩
    public const char Cross = '\u256C';        // ╬
    public const char ThinHorizontal = '\u2500'; // ─
    public const char ThinLeftT = '\u255F';    // ╟
    public const char ThinRightT = '\u2562';   // ╢

    // Caracteres para barras de progreso
    public const char BarFull = '\u2588';      // █
    public const char BarEmpty = '\u2591';     // ░
    public const char BarHalf = '\u2592';      // ▒

    // Dimensiones mínimas
    public const int MinWidth = 80;
    public const int MinHeight = 20;

    // Proporciones de paneles (porcentaje del ancho total)
    private const double LeftPanelRatio = 0.68;
    private const double RightPanelRatio = 0.32;

    // Dimensiones dinámicas basadas en el tamaño actual de la consola
    public static int ScreenWidth => Math.Max(Console.WindowWidth, MinWidth);
    public static int ScreenHeight => Math.Max(Console.WindowHeight - 2, MinHeight); // -2 para prompt
    public static int LeftPanelWidth => (int)(ScreenWidth * LeftPanelRatio);
    public static int RightPanelWidth => ScreenWidth - LeftPanelWidth;

    // Para compatibilidad con código existente
    public static int DefaultWidth => ScreenWidth;

    // Caracteres adicionales para uniones de paneles
    public const char TopTDouble = '\u2566';      // ╦
    public const char BottomTDouble = '\u2569';   // ╩
    public const char ThinTopT = '\u2564';        // ╤
    public const char ThinBottomT = '\u2567';     // ╧

    // ANSI escape sequences para posicionamiento
    private const string CursorHome = "\x1b[H";
    private const string ClearScreen = "\x1b[2J";
    private const string HideCursor = "\x1b[?25l";
    private const string ShowCursor = "\x1b[?25h";
    private const string SaveCursor = "\x1b[s";
    private const string RestoreCursor = "\x1b[u";

    /// <summary>
    /// Mueve el cursor a una posición específica (1-indexed)
    /// </summary>
    public static void SetCursorPosition(int row, int col)
    {
        Console.Write($"\x1b[{row};{col}H");
    }

    /// <summary>
    /// Oculta el cursor
    /// </summary>
    public static void HideCursorFunc()
    {
        Console.Write(HideCursor);
    }

    /// <summary>
    /// Muestra el cursor
    /// </summary>
    public static void ShowCursorFunc()
    {
        Console.Write(ShowCursor);
    }

    /// <summary>
    /// Limpia la pantalla
    /// </summary>
    public static void Clear()
    {
        Console.Write(ClearScreen + CursorHome);
    }

    /// <summary>
    /// Escribe texto con color
    /// </summary>
    public static void Write(string text, string color = "")
    {
        if (!string.IsNullOrEmpty(color))
            Console.Write($"{color}{text}{Colors.Reset}");
        else
            Console.Write(text);
    }

    /// <summary>
    /// Escribe línea con color
    /// </summary>
    public static void WriteLine(string text = "", string color = "")
    {
        if (!string.IsNullOrEmpty(color))
            Console.WriteLine($"{color}{text}{Colors.Reset}");
        else
            Console.WriteLine(text);
    }

    /// <summary>
    /// Dibuja una línea horizontal del marco
    /// </summary>
    public static void DrawHorizontalLine(int width, char left, char fill, char right)
    {
        Console.Write(left);
        Console.Write(new string(fill, width - 2));
        Console.WriteLine(right);
    }

    /// <summary>
    /// Dibuja el borde superior del marco
    /// </summary>
    public static void DrawTopBorder(int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawHorizontalLine(width, TopLeft, Horizontal, TopRight);
    }

    /// <summary>
    /// Dibuja el borde inferior del marco
    /// </summary>
    public static void DrawBottomBorder(int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawHorizontalLine(width, BottomLeft, Horizontal, BottomRight);
    }

    /// <summary>
    /// Dibuja un separador horizontal dentro del marco
    /// </summary>
    public static void DrawSeparator(int width = 0, bool thin = false)
    {
        if (width <= 0) width = ScreenWidth;
        if (thin)
            DrawHorizontalLine(width, ThinLeftT, ThinHorizontal, ThinRightT);
        else
            DrawHorizontalLine(width, LeftT, Horizontal, RightT);
    }

    /// <summary>
    /// Dibuja una línea de contenido dentro del marco
    /// </summary>
    public static void DrawLine(string content, int width = 0, string color = "")
    {
        if (width <= 0) width = ScreenWidth;
        var visibleLength = GetVisibleLength(content);
        var padding = width - 4 - visibleLength; // 4 = 2 bordes + 2 espacios
        if (padding < 0) padding = 0;

        Console.Write($"{Vertical} ");
        if (!string.IsNullOrEmpty(color))
            Console.Write($"{color}{content}{Colors.Reset}");
        else
            Console.Write(content);
        Console.Write(new string(' ', padding));
        Console.WriteLine($" {Vertical}");
    }

    /// <summary>
    /// Dibuja una línea vacía dentro del marco
    /// </summary>
    public static void DrawEmptyLine(int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        Console.Write(Vertical);
        Console.Write(new string(' ', width - 2));
        Console.WriteLine(Vertical);
    }

    /// <summary>
    /// Dibuja una línea centrada dentro del marco
    /// </summary>
    public static void DrawCenteredLine(string content, int width = 0, string color = "")
    {
        if (width <= 0) width = ScreenWidth;
        var visibleLength = GetVisibleLength(content);
        var totalPadding = width - 4 - visibleLength;
        if (totalPadding < 0) totalPadding = 0;
        var leftPadding = totalPadding / 2;
        var rightPadding = totalPadding - leftPadding;

        Console.Write($"{Vertical} ");
        Console.Write(new string(' ', leftPadding));
        if (!string.IsNullOrEmpty(color))
            Console.Write($"{color}{content}{Colors.Reset}");
        else
            Console.Write(content);
        Console.Write(new string(' ', rightPadding));
        Console.WriteLine($" {Vertical}");
    }

    /// <summary>
    /// Dibuja un título centrado con formato
    /// </summary>
    public static void DrawTitle(string title, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawCenteredLine($"{Colors.Bold}{title}{Colors.Reset}", width);
    }

    /// <summary>
    /// Dibuja un subtítulo centrado con formato
    /// </summary>
    public static void DrawSubtitle(string subtitle, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawCenteredLine($"{Colors.Gray}{subtitle}{Colors.Reset}", width);
    }

    /// <summary>
    /// Genera una barra de progreso
    /// </summary>
    public static string ProgressBar(int current, int max, int barWidth = 16, string color = "")
    {
        if (max <= 0) max = 1;
        if (current < 0) current = 0;
        if (current > max) current = max;

        var percentage = (double)current / max;
        var filledWidth = (int)(percentage * barWidth);
        var emptyWidth = barWidth - filledWidth;

        var bar = new string(BarFull, filledWidth) + new string(BarEmpty, emptyWidth);

        if (!string.IsNullOrEmpty(color))
            return $"{color}{bar}{Colors.Reset}";

        return bar;
    }

    /// <summary>
    /// Dibuja una barra de stat con etiqueta y valores
    /// </summary>
    public static void DrawStatBar(string icon, string label, int current, int max, string color, int width = 0, int barWidth = 16)
    {
        if (width <= 0) width = ScreenWidth;
        var bar = ProgressBar(current, max, barWidth, color);
        var valueStr = $"{current}/{max}";
        var content = $"{icon} {label,-8} {bar}  {color}{valueStr}{Colors.Reset}";
        DrawLine(content, width);
    }

    /// <summary>
    /// Dibuja texto que puede necesitar múltiples líneas (word wrap)
    /// </summary>
    public static void DrawWrappedText(string text, int width = 0, string color = "", string prefix = "")
    {
        if (width <= 0) width = ScreenWidth;
        var maxLineWidth = width - 4 - prefix.Length; // 4 = bordes + espacios
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            if (currentLine.Length + word.Length + 1 > maxLineWidth)
            {
                if (currentLine.Length > 0)
                {
                    DrawLine(prefix + currentLine.ToString().TrimEnd(), width, color);
                    currentLine.Clear();
                }
            }

            if (currentLine.Length > 0)
                currentLine.Append(' ');
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
            DrawLine(prefix + currentLine.ToString().TrimEnd(), width, color);
    }

    /// <summary>
    /// Dibuja un mensaje de error
    /// </summary>
    public static void DrawError(string message, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawLine($"{Colors.Error}Error: {message}{Colors.Reset}", width);
    }

    /// <summary>
    /// Dibuja un mensaje de éxito
    /// </summary>
    public static void DrawSuccess(string message, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawLine($"{Colors.Success}{message}{Colors.Reset}", width);
    }

    /// <summary>
    /// Dibuja opciones de menú numeradas
    /// </summary>
    public static void DrawMenuOptions(IEnumerable<string> options, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        var index = 1;
        foreach (var option in options)
        {
            DrawLine($"[{index}] {option}", width, Colors.Cyan);
            index++;
        }
    }

    /// <summary>
    /// Dibuja una lista con viñetas
    /// </summary>
    public static void DrawBulletList(IEnumerable<string> items, int width = 0, string color = "", string bullet = "  * ")
    {
        if (width <= 0) width = ScreenWidth;
        foreach (var item in items)
        {
            DrawLine($"{bullet}{item}", width, color);
        }
    }

    /// <summary>
    /// Muestra el prompt de entrada
    /// </summary>
    public static void ShowPrompt(string prompt = "> ")
    {
        Console.WriteLine();
        Console.Write($"{Colors.Cyan}{prompt}{Colors.Reset}");
    }

    /// <summary>
    /// Muestra mensaje y espera Enter
    /// </summary>
    public static void WaitForEnter(string message = "Presiona Enter para continuar...")
    {
        Console.WriteLine();
        Write(message, Colors.Gray);
        Console.ReadLine();
    }

    /// <summary>
    /// Calcula la longitud visible de un string (sin códigos ANSI)
    /// </summary>
    public static int GetVisibleLength(string text)
    {
        if (string.IsNullOrEmpty(text))
            return 0;

        // Remover secuencias ANSI para calcular longitud real
        var result = System.Text.RegularExpressions.Regex.Replace(text, @"\x1b\[[0-9;]*m", "");
        return result.Length;
    }

    /// <summary>
    /// Trunca un string a la longitud máxima visible
    /// </summary>
    public static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || GetVisibleLength(text) <= maxLength)
            return text;

        // Simplificado: truncar sin considerar ANSI
        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength - 3) + "...";
    }

    /// <summary>
    /// Dibuja un cuadro de diálogo simple
    /// </summary>
    public static void DrawDialog(string title, string message, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawTopBorder(width);
        DrawTitle(title, width);
        DrawSeparator(width);
        DrawEmptyLine(width);
        DrawWrappedText(message, width);
        DrawEmptyLine(width);
        DrawBottomBorder(width);
    }

    /// <summary>
    /// Dibuja un cuadro de confirmación con opciones Sí/No
    /// </summary>
    public static void DrawConfirmDialog(string title, string message, int width = 0)
    {
        if (width <= 0) width = ScreenWidth;
        DrawTopBorder(width);
        DrawTitle(title, width);
        DrawSeparator(width);
        DrawEmptyLine(width);
        DrawWrappedText(message, width);
        DrawEmptyLine(width);
        DrawSeparator(width, thin: true);
        DrawLine("[S] Si    [N] No", width, Colors.Cyan);
        DrawBottomBorder(width);
    }

    /// <summary>
    /// Formatea el tiempo de juego
    /// </summary>
    public static string FormatTime(DateTime gameTime)
    {
        return gameTime.ToString("HH:mm");
    }

    /// <summary>
    /// Obtiene el emoji del clima
    /// </summary>
    public static string GetWeatherEmoji(string weather)
    {
        return weather?.ToLowerInvariant() switch
        {
            "despejado" or "soleado" or "clear" or "sunny" => "sun",
            "nublado" or "cloudy" => "cloud",
            "lluvia" or "lluvioso" or "rain" or "rainy" => "cloud_with_rain",
            "tormenta" or "storm" or "stormy" => "cloud_with_lightning",
            "nieve" or "nevando" or "snow" or "snowy" => "snowflake",
            "niebla" or "fog" or "foggy" => "fog",
            "viento" or "ventoso" or "wind" or "windy" => "wind_face",
            _ => "sun"
        };
    }

    /// <summary>
    /// Capitaliza la primera letra
    /// </summary>
    public static string Capitalize(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        return char.ToUpper(text[0]) + text.Substring(1);
    }
}
