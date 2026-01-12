using System.Text;
using XiloAdventures.Terminal.Player;

// Parsear argumentos de línea de comandos
var options = GameOptions.Parse(args);

// Configurar consola para UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Habilitar secuencias ANSI en Windows Terminal / CMD
EnableVirtualTerminalProcessing();

// Verificar tamaño mínimo de consola
EnsureMinimumConsoleSize(ConsoleRenderer.MinWidth, ConsoleRenderer.MinHeight);

// Iniciar el juego con las opciones
var game = new ConsoleGame(options);
await game.RunAsync();

static void EnsureMinimumConsoleSize(int minWidth, int minHeight)
{
    try
    {
        var currentWidth = Console.WindowWidth;
        var currentHeight = Console.WindowHeight;

        // Solo mostrar advertencia si es muy pequeña
        if (currentWidth < minWidth || currentHeight < minHeight)
        {
            Console.Clear();
            Console.WriteLine();
            Console.WriteLine("  Nota: La ventana de consola es pequena.");
            Console.WriteLine($"  Recomendado: al menos {minWidth}x{minHeight} caracteres.");
            Console.WriteLine("  El juego se adaptara al tamano disponible.");
            Console.WriteLine();
            Console.Write("  Presiona Enter para continuar...");
            Console.ReadLine();
        }
    }
    catch
    {
        // Ignorar errores al verificar tamaño
    }
}

static void EnableVirtualTerminalProcessing()
{
    // Habilitar secuencias de escape ANSI en Windows
    // Esto es necesario para que los colores funcionen en CMD
    try
    {
        if (OperatingSystem.IsWindows())
        {
            // En Windows 10+, las secuencias ANSI están habilitadas por defecto en Windows Terminal
            // pero CMD necesita habilitarlas explícitamente
            var handle = GetStdHandle(-11); // STD_OUTPUT_HANDLE
            if (handle != IntPtr.Zero && handle != new IntPtr(-1))
            {
                GetConsoleMode(handle, out uint mode);
                SetConsoleMode(handle, mode | 0x0004); // ENABLE_VIRTUAL_TERMINAL_PROCESSING
            }
        }
    }
    catch
    {
        // Si falla, los colores no funcionarán pero el juego sí
    }
}

// P/Invoke para habilitar ANSI en CMD
[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
static extern IntPtr GetStdHandle(int nStdHandle);

[System.Runtime.InteropServices.DllImport("kernel32.dll")]
static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

[System.Runtime.InteropServices.DllImport("kernel32.dll")]
static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
