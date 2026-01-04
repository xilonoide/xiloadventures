namespace XiloAdventures.Linux.Player;

/// <summary>
/// Opciones de configuración del juego parseadas desde línea de comandos
/// </summary>
public class GameOptions
{
    /// <summary>
    /// Habilitar sonido (por defecto: true)
    /// Usar --sound-off para desactivar
    /// </summary>
    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Habilitar IA para comandos desconocidos (por defecto: false)
    /// Usar --ia-on para activar
    /// </summary>
    public bool IaEnabled { get; set; } = false;

    /// <summary>
    /// Parsea los argumentos de línea de comandos
    /// </summary>
    public static GameOptions Parse(string[] args)
    {
        var options = new GameOptions();

        foreach (var arg in args)
        {
            var lowerArg = arg.ToLowerInvariant();

            switch (lowerArg)
            {
                case "--sound-off":
                case "--no-sound":
                case "-s":
                    options.SoundEnabled = false;
                    break;

                case "--ia-on":
                case "--ai-on":
                case "-i":
                    options.IaEnabled = true;
                    break;

                case "--help":
                case "-h":
                case "-?":
                    ShowHelp();
                    Environment.Exit(0);
                    break;
            }
        }

        return options;
    }

    /// <summary>
    /// Muestra la ayuda de línea de comandos
    /// </summary>
    private static void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("XiloAdventures Linux Player");
        Console.WriteLine("===========================");
        Console.WriteLine();
        Console.WriteLine("Uso: XiloAdventures.Linux.Player [opciones]");
        Console.WriteLine();
        Console.WriteLine("Opciones:");
        Console.WriteLine("  --sound-off, -s    Desactivar sonido");
        Console.WriteLine("  --ia-on, -i        Activar IA para comandos desconocidos");
        Console.WriteLine("  --help, -h         Mostrar esta ayuda");
        Console.WriteLine();
        Console.WriteLine("Por defecto: sonido activado, IA desactivada");
        Console.WriteLine();
    }
}
