using System.Text;
using XiloAdventures.Linux.Player;

// Parsear argumentos de línea de comandos
var options = GameOptions.Parse(args);

// Configurar consola para UTF-8
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

// Si la IA está habilitada, inicializar servicios de Docker
if (options.IaEnabled)
{
    var aiReady = await DockerServiceConsole.EnsureAllAsync();
    if (!aiReady)
    {
        // Continuar sin IA si falló la inicialización
        options.IaEnabled = false;
        Console.WriteLine("  Continuando sin IA...");
        Console.WriteLine();
        await Task.Delay(1500);
    }
}

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
