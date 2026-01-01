namespace XiloAdventures.Engine.Models;

/// <summary>
/// Resultado de procesar un comando del jugador.
/// Usa el patrón Result para distinguir entre éxito y error sin comparar strings.
/// </summary>
public class CommandResult
{
    /// <summary>Mensaje para mostrar al jugador.</summary>
    public string Message { get; }

    /// <summary>Indica si el comando se procesó correctamente.</summary>
    public bool IsSuccess { get; }

    /// <summary>Indica si hubo un error al procesar el comando.</summary>
    public bool HasError => !IsSuccess;

    /// <summary>Indica si se debe limpiar la pantalla antes de mostrar el mensaje.</summary>
    public bool ClearScreenBefore { get; private set; }

    private CommandResult(string message, bool isSuccess)
    {
        Message = message;
        IsSuccess = isSuccess;
    }

    /// <summary>Crea un resultado exitoso.</summary>
    public static CommandResult Success(string message) => new(message, true);

    /// <summary>Crea un resultado exitoso que limpia la pantalla antes de mostrar el mensaje.</summary>
    public static CommandResult SuccessWithClear(string message) => new(message, true) { ClearScreenBefore = true };

    /// <summary>Crea un resultado de error (parser no entendió, objeto no encontrado, etc.).</summary>
    public static CommandResult Error(string message) => new(message, false);

    /// <summary>Resultado vacío exitoso.</summary>
    public static CommandResult Empty => new(string.Empty, true);

    /// <summary>Combina múltiples resultados en uno solo.</summary>
    public static CommandResult Combine(params CommandResult[] results)
    {
        var messages = new List<string>();
        var anyError = false;

        foreach (var r in results)
        {
            if (!string.IsNullOrWhiteSpace(r.Message))
                messages.Add(r.Message.TrimEnd());
            if (r.HasError)
                anyError = true;
        }

        return new CommandResult(string.Join("\n", messages), !anyError);
    }

    /// <summary>Añade un mensaje adicional al resultado.</summary>
    public CommandResult AppendMessage(string additionalMessage)
    {
        if (string.IsNullOrWhiteSpace(additionalMessage))
            return this;
        var newMessage = string.IsNullOrWhiteSpace(Message)
            ? additionalMessage
            : $"{Message.TrimEnd()}\n{additionalMessage}";
        return new CommandResult(newMessage, IsSuccess) { ClearScreenBefore = ClearScreenBefore };
    }
}
