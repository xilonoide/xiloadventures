namespace XiloAdventures.Engine.Engine;

/// <summary>
/// Clase base con utilidades para la selección aleatoria de mensajes.
/// </summary>
internal static class RandomMessageHelper
{
    private static readonly Random Random = new();

    /// <summary>
    /// Selecciona un mensaje aleatorio de la lista proporcionada.
    /// </summary>
    internal static string Pick(params string[] messages) => messages[Random.Next(messages.Length)];
}
