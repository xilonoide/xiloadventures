using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Linux.Player.Screens;

/// <summary>
/// Pantalla principal del juego con layout de dos columnas
/// </summary>
public class GameScreen
{
    private readonly SplitScreenLayout _layout;
    private GameState? _lastState;
    private Room? _lastRoom;
    private string? _lastRenderedRoomId;

    public GameScreen()
    {
        _layout = new SplitScreenLayout();
    }

    /// <summary>
    /// Renderiza la pantalla completa del juego
    /// </summary>
    public void Render(GameState state, Room room, WorldModel world)
    {
        _lastState = state;
        _lastRoom = room;

        // Solo añadir descripción si es una sala diferente a la última renderizada
        var currentRoomId = room?.Id;
        if (currentRoomId != _lastRenderedRoomId)
        {
            _lastRenderedRoomId = currentRoomId;

            _layout.AddToHistory("");
            _layout.AddToHistory($"{Colors.Bold}{room?.Name ?? "Lugar desconocido"}{Colors.Reset}");

            if (room != null && !string.IsNullOrEmpty(room.Description))
            {
                _layout.AddToHistory(room.Description);
            }

            // Línea en blanco después de la descripción
            _layout.AddToHistory("");

            // Añadir objetos visibles
            var visibleObjects = GetVisibleObjects(state, room);
            if (visibleObjects.Any())
            {
                var objectNames = string.Join(", ", visibleObjects.Select(o => o.Name));
                _layout.AddToHistory($"{Colors.Object}Objetos:{Colors.Reset} {objectNames}");
            }

            // Añadir NPCs visibles
            var visibleNpcs = GetVisibleNpcs(state, room);
            if (visibleNpcs.Any())
            {
                var npcNames = string.Join(", ", visibleNpcs.Select(n => FormatNpcName(n)));
                _layout.AddToHistory($"{Colors.Npc}Personajes:{Colors.Reset} {npcNames}");
            }

            // Añadir salidas
            var exitsList = GetExitsList(room, state);
            if (exitsList.Any())
            {
                _layout.AddToHistory($"{Colors.Exit}Salidas:{Colors.Reset} {string.Join("  ", exitsList)}");
            }

            _layout.AddToHistory("");
        }

        // Renderizar todo
        _layout.Render(state, room, world);
    }

    /// <summary>
    /// Actualiza solo el panel de stats (sin redibujar todo)
    /// </summary>
    public void UpdateStats(GameState state, WorldModel world)
    {
        var hasImage = _lastRoom != null && !string.IsNullOrEmpty(_lastRoom.AsciiImage);
        _layout.UpdateRightPanel(state, world, hasImage);
    }

    /// <summary>
    /// Muestra un mensaje de acción o resultado
    /// </summary>
    public void ShowMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        var lines = message.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            _layout.AddToHistory($"  {line.Trim()}");
        }

        // Redibujar con el nuevo historial
        if (_lastState != null && _lastRoom != null)
        {
            _layout.Render(_lastState, _lastRoom, null!);
        }
    }

    /// <summary>
    /// Muestra un mensaje de error
    /// </summary>
    public void ShowError(string message)
    {
        _layout.AddToHistory($"  {Colors.Error}{message}{Colors.Reset}");

        if (_lastState != null && _lastRoom != null)
        {
            _layout.Render(_lastState, _lastRoom, null!);
        }
    }

    /// <summary>
    /// Muestra un mensaje de script
    /// </summary>
    public void ShowScriptMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        _layout.AddToHistory("");
        _layout.AddToHistory($"{Colors.Yellow}>>> {message}{Colors.Reset}");
        _layout.AddToHistory("");

        if (_lastState != null && _lastRoom != null)
        {
            _layout.Render(_lastState, _lastRoom, null!);
        }
    }

    /// <summary>
    /// Muestra opciones de diálogo
    /// </summary>
    public void ShowDialogueOptions(IEnumerable<DialogueOption> options)
    {
        _layout.AddToHistory("");
        var index = 1;
        foreach (var option in options)
        {
            _layout.AddToHistory($"  {Colors.Cyan}[{index}]{Colors.Reset} {option.Text}");
            index++;
        }

        if (_lastState != null && _lastRoom != null)
        {
            _layout.Render(_lastState, _lastRoom, null!);
        }
    }

    /// <summary>
    /// Muestra diálogo de NPC
    /// </summary>
    public void ShowDialogue(string npcName, string text)
    {
        _layout.AddToHistory("");
        _layout.AddToHistory($"{Colors.Npc}{npcName}{Colors.Reset} dice:");
        _layout.AddToHistory($"  \"{text}\"");

        if (_lastState != null && _lastRoom != null)
        {
            _layout.Render(_lastState, _lastRoom, null!);
        }
    }

    /// <summary>
    /// Muestra pantalla de Game Over
    /// </summary>
    public void ShowGameOver(string reason = "Has muerto")
    {
        ConsoleRenderer.Clear();
        var width = ConsoleRenderer.DefaultWidth;

        ConsoleRenderer.DrawTopBorder(width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawCenteredLine($"{Colors.Red}GAME OVER{Colors.Reset}", width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawCenteredLine(reason, width, Colors.Gray);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawBottomBorder(width);
    }

    /// <summary>
    /// Muestra pantalla de victoria
    /// </summary>
    public void ShowVictory(string message = "Has completado la aventura")
    {
        ConsoleRenderer.Clear();
        var width = ConsoleRenderer.DefaultWidth;

        ConsoleRenderer.DrawTopBorder(width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawCenteredLine($"{Colors.Green}VICTORIA{Colors.Reset}", width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawSeparator(width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawWrappedText(message, width, Colors.Yellow);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawBottomBorder(width);
    }

    /// <summary>
    /// Muestra la pantalla de introducción
    /// </summary>
    public void ShowIntro(string title, string introText)
    {
        ConsoleRenderer.Clear();
        var width = ConsoleRenderer.DefaultWidth;

        ConsoleRenderer.DrawTopBorder(width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawTitle(title, width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawSeparator(width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawWrappedText(introText, width);
        ConsoleRenderer.DrawEmptyLine(width);
        ConsoleRenderer.DrawBottomBorder(width);
    }

    /// <summary>
    /// Añade el comando del usuario al historial
    /// </summary>
    public void AddCommandToHistory(string command)
    {
        _layout.AddToHistory($"{Colors.Cyan}> {command}{Colors.Reset}");
    }

    /// <summary>
    /// Limpia el historial y fuerza redibujado
    /// </summary>
    public void ClearAndRedraw(GameState state, Room room, WorldModel world)
    {
        _layout.ClearHistory();
        _lastState = state;
        _lastRoom = room;
        _lastRenderedRoomId = null; // Forzar que se añada la descripción de nuevo
        Render(state, room, world);
    }

    private static IEnumerable<GameObject> GetVisibleObjects(GameState state, Room? room)
    {
        if (room == null)
            return Enumerable.Empty<GameObject>();

        var containedObjectIds = state.Objects
            .Where(o => o.IsContainer)
            .SelectMany(o => o.ContainedObjectIds)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return state.Objects
            .Where(o => o.Visible &&
                        room.ObjectIds.Contains(o.Id) &&
                        !state.InventoryObjectIds.Contains(o.Id) &&
                        !containedObjectIds.Contains(o.Id))
            .OrderBy(o => o.Name);
    }

    private static IEnumerable<Npc> GetVisibleNpcs(GameState state, Room? room)
    {
        if (room == null)
            return Enumerable.Empty<Npc>();

        return state.Npcs
            .Where(n => n.Visible && room.NpcIds.Contains(n.Id))
            .OrderBy(n => n.Name);
    }

    private static string FormatNpcName(Npc npc)
    {
        if (npc.IsCorpse)
            return $"{Colors.Gray}{npc.Name} (cadaver){Colors.Reset}";
        if (npc.IsShopkeeper)
            return $"{Colors.Yellow}{npc.Name}{Colors.Reset}";
        return npc.Name;
    }

    private static List<string> GetExitsList(Room? room, GameState state)
    {
        if (room == null || room.Exits == null || !room.Exits.Any())
            return new List<string>();

        var exitParts = new List<string>();

        foreach (var exit in room.Exits.OrderBy(e => GetDirectionOrder(e.Direction)))
        {
            var dirShort = GetDirectionShort(exit.Direction);
            var targetRoom = state.Rooms.FirstOrDefault(r =>
                r.Id.Equals(exit.TargetRoomId, StringComparison.OrdinalIgnoreCase));
            var targetName = targetRoom?.Name ?? "?";

            var door = state.Doors.FirstOrDefault(d =>
                (d.RoomIdA == room.Id && d.RoomIdB == exit.TargetRoomId) ||
                (d.RoomIdB == room.Id && d.RoomIdA == exit.TargetRoomId));

            if (door != null)
            {
                var doorState = door.IsOpen ? "abierta" : (door.IsLocked ? "llave" : "cerrada");
                exitParts.Add($"[{dirShort}]{doorState}");
            }
            else
            {
                exitParts.Add($"[{dirShort}]{targetName}");
            }
        }

        return exitParts;
    }

    private static string GetDirectionShort(string direction)
    {
        return direction?.ToLowerInvariant() switch
        {
            "north" or "norte" or "n" => "N",
            "south" or "sur" or "s" => "S",
            "east" or "este" or "e" => "E",
            "west" or "oeste" or "o" or "w" => "O",
            "northeast" or "noreste" or "ne" => "NE",
            "northwest" or "noroeste" or "no" or "nw" => "NO",
            "southeast" or "sureste" or "se" => "SE",
            "southwest" or "suroeste" or "so" or "sw" => "SO",
            "up" or "arriba" or "u" => "^",
            "down" or "abajo" or "d" => "v",
            _ => direction?.ToUpperInvariant().Substring(0, Math.Min(2, direction?.Length ?? 0)) ?? "?"
        };
    }

    private static int GetDirectionOrder(string direction)
    {
        return direction?.ToLowerInvariant() switch
        {
            "north" or "norte" or "n" => 0,
            "south" or "sur" or "s" => 1,
            "east" or "este" or "e" => 2,
            "west" or "oeste" or "o" or "w" => 3,
            "northeast" or "noreste" or "ne" => 4,
            "northwest" or "noroeste" or "no" or "nw" => 5,
            "southeast" or "sureste" or "se" => 6,
            "southwest" or "suroeste" or "so" or "sw" => 7,
            "up" or "arriba" or "u" => 8,
            "down" or "abajo" or "d" => 9,
            _ => 10
        };
    }
}
