using System.Text;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Terminal.Player;

/// <summary>
/// Layout de pantalla dividida con panel izquierdo (historial) y derecho (stats)
/// </summary>
public class SplitScreenLayout
{
    // Propiedades dinámicas basadas en el tamaño actual de la consola
    private static int TotalWidth => ConsoleRenderer.ScreenWidth;
    private static int LeftWidth => ConsoleRenderer.LeftPanelWidth;
    private static int RightWidth => ConsoleRenderer.RightPanelWidth;
    private static int Height => ConsoleRenderer.ScreenHeight;

    // Buffer del historial (panel izquierdo)
    private readonly List<string> _historyBuffer = new();
    private const int MaxHistoryLines = 100;

    // Para detectar cambios de tamaño
    private int _lastWidth;
    private int _lastHeight;

    // Altura del área de paneles
    private static int PanelSectionHeight => Height;

    // Líneas visibles en el panel izquierdo (sin header ni footer)
    // Header: 4 líneas (borde superior, título, subtítulo, separador)
    // Footer: 3 líneas (separador fino, prompt, borde inferior)
    private int GetLeftContentHeight(bool hasImage) => Height - 7;

    /// <summary>
    /// Verifica si el tamaño de la consola ha cambiado
    /// </summary>
    public bool HasSizeChanged()
    {
        var currentWidth = Console.WindowWidth;
        var currentHeight = Console.WindowHeight;

        if (currentWidth != _lastWidth || currentHeight != _lastHeight)
        {
            _lastWidth = currentWidth;
            _lastHeight = currentHeight;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Añade una línea al historial
    /// </summary>
    public void AddToHistory(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _historyBuffer.Add("");
            return;
        }

        // Hacer word wrap para que quepa en el panel izquierdo
        var maxWidth = LeftWidth - 4; // Bordes y espacios
        var lines = WrapText(text, maxWidth);
        _historyBuffer.AddRange(lines);

        // Limitar tamaño del buffer
        while (_historyBuffer.Count > MaxHistoryLines)
        {
            _historyBuffer.RemoveAt(0);
        }
    }

    /// <summary>
    /// Añade múltiples líneas al historial
    /// </summary>
    public void AddLinesToHistory(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            AddToHistory(line);
        }
    }

    /// <summary>
    /// Limpia el historial
    /// </summary>
    public void ClearHistory()
    {
        _historyBuffer.Clear();
    }

    /// <summary>
    /// Renderiza toda la pantalla
    /// </summary>
    public void Render(GameState state, Room? room, WorldModel world)
    {
        ConsoleRenderer.HideCursorFunc();
        ConsoleRenderer.Clear();

        var sb = new StringBuilder();
        var hasImage = false; // Sin imagen en terminal
        var contentHeight = GetLeftContentHeight(hasImage);

        // Sin imagen: borde superior normal con unión
        sb.Append(ConsoleRenderer.TopLeft);
        sb.Append(new string(ConsoleRenderer.Horizontal, LeftWidth - 2));
        sb.Append(ConsoleRenderer.TopT);
        sb.Append(new string(ConsoleRenderer.Horizontal, RightWidth - 1));
        sb.Append(ConsoleRenderer.TopRight);
        sb.AppendLine();

        // === SECCIÓN DE PANELES ===

        // Línea: Título sala (izq) | Stats header (der)
        var roomTitle = room?.Name ?? "Lugar desconocido";
        sb.Append(FormatLeftCell(CenterText($"{Colors.Bold}{roomTitle}{Colors.Reset}", LeftWidth - 3)));
        sb.Append(FormatRightCell($"{Colors.Yellow}Estado{Colors.Reset}"));
        sb.AppendLine();

        // Línea: Subtítulo (izq) | Salud bar (der)
        var turnInfo = $"Turno {state.TurnCounter}";
        var timeInfo = state.GameTime != default ? $" | {state.GameTime:HH:mm}" : "";
        var weatherInfo = GetWeatherText(state.Weather);
        var subtitle = $"{Colors.Gray}{turnInfo}{timeInfo} | {weatherInfo}{Colors.Reset}";
        sb.Append(FormatLeftCell(CenterText(subtitle, LeftWidth - 3)));
        var healthBar = FormatStatBar("Salud", state.Player.DynamicStats.Health, state.Player.DynamicStats.MaxHealth, Colors.Health);
        sb.Append(FormatRightCell(healthBar));
        sb.AppendLine();

        // Línea: Separador (izq) | Mana bar (der) - usa RightT para no dividir visualmente el panel derecho
        sb.Append(ConsoleRenderer.LeftT);
        sb.Append(new string(ConsoleRenderer.Horizontal, LeftWidth - 2));
        sb.Append(ConsoleRenderer.RightT);
        var manaBar = FormatStatBar("Mana", state.Player.DynamicStats.Mana, state.Player.DynamicStats.MaxMana, Colors.Mana);
        sb.Append(PadRight(manaBar, RightWidth - 1));
        sb.Append(ConsoleRenderer.Vertical);
        sb.AppendLine();

        // Líneas de contenido: historial (izq) | stats y inventario (der)
        var rightPanelLines = BuildRightPanelContent(state, world);
        var historyLinesToShow = GetVisibleHistoryLines(contentHeight);

        for (int i = 0; i < contentHeight; i++)
        {
            // Panel izquierdo: historial
            var leftContent = i < historyLinesToShow.Count ? historyLinesToShow[i] : "";
            sb.Append(FormatLeftCell(leftContent));

            // Panel derecho: stats/inventario
            var rightContent = i < rightPanelLines.Count ? rightPanelLines[i] : "";
            sb.Append(FormatRightCell(rightContent));
            sb.AppendLine();
        }

        // Línea inferior: separador fino para prompt (izq) | continuar inventario (der)
        sb.Append(ConsoleRenderer.ThinLeftT);
        sb.Append(new string(ConsoleRenderer.ThinHorizontal, LeftWidth - 2));
        sb.Append(ConsoleRenderer.Cross);
        sb.Append(new string(ConsoleRenderer.Horizontal, RightWidth - 1));
        sb.Append(ConsoleRenderer.RightT);
        sb.AppendLine();

        // Línea de prompt
        sb.Append(ConsoleRenderer.Vertical);
        sb.Append($" {Colors.Cyan}>{Colors.Reset} ");
        sb.Append(new string(' ', LeftWidth - 5));
        sb.Append(ConsoleRenderer.Vertical);
        sb.Append(new string(' ', RightWidth - 1));
        sb.Append(ConsoleRenderer.Vertical);
        sb.AppendLine();

        // Borde inferior
        sb.Append(ConsoleRenderer.BottomLeft);
        sb.Append(new string(ConsoleRenderer.Horizontal, LeftWidth - 2));
        sb.Append(ConsoleRenderer.BottomT);
        sb.Append(new string(ConsoleRenderer.Horizontal, RightWidth - 1));
        sb.Append(ConsoleRenderer.BottomRight);

        Console.Write(sb.ToString());

        // Posicionar cursor para input (después del "> ")
        ConsoleRenderer.SetCursorPosition(Height - 1, 5);
        ConsoleRenderer.ShowCursorFunc();
    }

    /// <summary>
    /// Actualiza solo el panel derecho (stats)
    /// </summary>
    public void UpdateRightPanel(GameState state, WorldModel world, bool hasImage = false)
    {
        ConsoleRenderer.HideCursorFunc();

        var rightPanelLines = BuildRightPanelContent(state, world);
        var contentHeight = GetLeftContentHeight(hasImage);

        // Calcular offset de fila si hay imagen
        int rowOffset = 0;

        // Actualizar header de stats
        ConsoleRenderer.SetCursorPosition(rowOffset + 3, LeftWidth + 1);
        var healthBar = FormatStatBar("Salud", state.Player.DynamicStats.Health, state.Player.DynamicStats.MaxHealth, Colors.Health);
        Console.Write(PadRight(healthBar, RightWidth - 1) + ConsoleRenderer.Vertical);

        ConsoleRenderer.SetCursorPosition(rowOffset + 4, LeftWidth + 1);
        var manaBar = FormatStatBar("Mana", state.Player.DynamicStats.Mana, state.Player.DynamicStats.MaxMana, Colors.Mana);
        Console.Write(PadRight(manaBar, RightWidth - 1) + ConsoleRenderer.Vertical);

        // Actualizar contenido del panel derecho
        for (int i = 0; i < contentHeight && i < rightPanelLines.Count; i++)
        {
            ConsoleRenderer.SetCursorPosition(rowOffset + 5 + i, LeftWidth + 1);
            Console.Write(PadRight(rightPanelLines[i], RightWidth - 1) + ConsoleRenderer.Vertical);
        }

        // Restaurar posición del cursor
        ConsoleRenderer.SetCursorPosition(Height - 1, 5);
        ConsoleRenderer.ShowCursorFunc();
    }

    private List<string> GetVisibleHistoryLines(int maxLines)
    {
        var count = Math.Min(_historyBuffer.Count, maxLines);
        var startIndex = Math.Max(0, _historyBuffer.Count - maxLines);
        return _historyBuffer.Skip(startIndex).Take(count).ToList();
    }

    private List<string> BuildRightPanelContent(GameState state, WorldModel world)
    {
        var lines = new List<string>();
        var maxWidth = RightWidth - 3; // Bordes y espacios

        // 1. Barras vitales adicionales (Salud/Mana ya están en el header, estas van justo debajo)
        lines.Add(FormatStatBar("Energia", state.Player.DynamicStats.Energy, 100, Colors.Energy));
        lines.Add(FormatStatBar("Cordura", state.Player.DynamicStats.Sanity, 100, Colors.Sanity));
        lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));

        // 2. Atributos
        lines.Add($"{Colors.Yellow}Atributos{Colors.Reset}");
        lines.Add($" Fue:{state.Player.Strength,2} Con:{state.Player.Constitution,2} Int:{state.Player.Intelligence,2}");
        lines.Add($" Des:{state.Player.Dexterity,2} Car:{state.Player.Charisma,2}");
        lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));

        // 3. Necesidades básicas (inverso: 0 = bien, 100 = crítico)
        lines.Add($"{Colors.Yellow}Necesidades{Colors.Reset}");
        lines.Add(FormatNeedBar("Hambre", state.Player.DynamicStats.Hunger, 100));
        lines.Add(FormatNeedBar("Sed", state.Player.DynamicStats.Thirst, 100));
        lines.Add(FormatNeedBar("Sueno", state.Player.DynamicStats.Sleep, 100));
        lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));

        // Combate (si está activo)
        if (state.ActiveCombat != null && state.ActiveCombat.IsActive)
        {
            lines.Add($"{Colors.Red}Combate{Colors.Reset}");
            var enemy = state.Npcs.FirstOrDefault(n =>
                n.Id.Equals(state.ActiveCombat.EnemyNpcId, StringComparison.OrdinalIgnoreCase));
            if (enemy != null)
            {
                lines.Add(FormatStatBar(Truncate(enemy.Name, 8), enemy.Stats.CurrentHealth, enemy.Stats.MaxHealth, Colors.Health));
            }
            lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));
        }

        // 4. Dinero
        lines.Add($"{Colors.Yellow}Dinero:{Colors.Reset} {state.Player.Money}");
        lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));

        // 5. Equipo
        lines.Add($"{Colors.Yellow}Equipo{Colors.Reset}");
        var equipment = GetEquipmentLines(state);
        foreach (var eq in equipment)
        {
            lines.Add($" {Truncate(eq, maxWidth - 2)}");
        }
        lines.Add(new string(ConsoleRenderer.ThinHorizontal, maxWidth));

        // 6. Inventario
        lines.Add($"{Colors.Yellow}Inventario{Colors.Reset}");
        var inventoryItems = GetInventoryItems(state);
        if (inventoryItems.Any())
        {
            foreach (var item in inventoryItems)
            {
                var truncated = Truncate(item, maxWidth - 2);
                lines.Add($" {truncated}");
            }
        }
        else
        {
            lines.Add($" {Colors.Gray}(vacio){Colors.Reset}");
        }

        return lines;
    }

    private string FormatNeedBar(string label, int current, int max)
    {
        // Las necesidades son inversas: 0 = verde (bien), 100 = rojo (mal)
        if (max <= 0) max = 100;
        var barWidth = 10;
        var filled = (int)((double)current / max * barWidth);
        var empty = barWidth - filled;

        // Color según nivel: verde si bajo, amarillo si medio, rojo si alto
        string color;
        if (current < 30) color = Colors.Green;
        else if (current < 70) color = Colors.Yellow;
        else color = Colors.Red;

        var bar = new string(ConsoleRenderer.BarFull, filled) + new string(ConsoleRenderer.BarEmpty, empty);
        return $"{label,-8} {color}{bar}{Colors.Reset} {current,3}";
    }

    private List<string> GetInventoryItems(GameState state)
    {
        return state.InventoryObjectIds
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var obj = state.Objects.FirstOrDefault(o =>
                    o.Id.Equals(g.Key, StringComparison.OrdinalIgnoreCase));
                var count = g.Count();
                var countStr = count > 1 ? $" x{count}" : "";
                var equipped = IsEquipped(state, g.Key) ? "*" : "";
                return obj != null ? $"{equipped}{obj.Name}{countStr}" : null;
            })
            .Where(x => x != null)
            .ToList()!;
    }

    private List<string> GetEquipmentLines(GameState state)
    {
        var lines = new List<string>();

        // Mano derecha
        var rightHandName = GetEquippedItemName(state, state.Player.EquippedRightHandId);
        lines.Add($"Mano der: {rightHandName}");

        // Mano izquierda
        var leftHandName = GetEquippedItemName(state, state.Player.EquippedLeftHandId);
        lines.Add($"Mano izq: {leftHandName}");

        // Torso
        var torsoName = GetEquippedItemName(state, state.Player.EquippedTorsoId);
        lines.Add($"Torso: {torsoName}");

        // Cabeza
        var headName = GetEquippedItemName(state, state.Player.EquippedHeadId);
        lines.Add($"Cabeza: {headName}");

        return lines;
    }

    private string GetEquippedItemName(GameState state, string? equippedId)
    {
        if (string.IsNullOrEmpty(equippedId))
            return $"{Colors.Gray}-{Colors.Reset}";

        var obj = state.Objects.FirstOrDefault(o =>
            o.Id.Equals(equippedId, StringComparison.OrdinalIgnoreCase));
        return obj?.Name ?? $"{Colors.Gray}-{Colors.Reset}";
    }

    private bool IsEquipped(GameState state, string objectId)
    {
        return state.Player.EquippedRightHandId?.Equals(objectId, StringComparison.OrdinalIgnoreCase) == true ||
               state.Player.EquippedLeftHandId?.Equals(objectId, StringComparison.OrdinalIgnoreCase) == true ||
               state.Player.EquippedTorsoId?.Equals(objectId, StringComparison.OrdinalIgnoreCase) == true ||
               state.Player.EquippedHeadId?.Equals(objectId, StringComparison.OrdinalIgnoreCase) == true;
    }

    private string FormatLeftCell(string content)
    {
        var visLen = ConsoleRenderer.GetVisibleLength(content);
        var padding = LeftWidth - 3 - visLen;
        if (padding < 0) padding = 0;
        return $"{ConsoleRenderer.Vertical} {content}{new string(' ', padding)}";
    }

    private string FormatRightCell(string content)
    {
        var visLen = ConsoleRenderer.GetVisibleLength(content);
        var padding = RightWidth - 1 - visLen;
        if (padding < 0) padding = 0;
        return $"{ConsoleRenderer.Vertical}{content}{new string(' ', padding)}{ConsoleRenderer.Vertical}";
    }

    private string PadRight(string content, int width)
    {
        var visLen = ConsoleRenderer.GetVisibleLength(content);
        var padding = width - visLen;
        if (padding < 0) padding = 0;
        return content + new string(' ', padding);
    }

    private string CenterText(string text, int width)
    {
        var visLen = ConsoleRenderer.GetVisibleLength(text);
        var totalPadding = width - visLen;
        if (totalPadding <= 0) return text;
        var leftPad = totalPadding / 2;
        var rightPad = totalPadding - leftPad;
        return new string(' ', leftPad) + text + new string(' ', rightPad);
    }

    private string FormatStatBar(string label, int current, int max, string color)
    {
        if (max <= 0) max = 1;
        var barWidth = 10;
        var filled = (int)((double)current / max * barWidth);
        var empty = barWidth - filled;
        var bar = new string(ConsoleRenderer.BarFull, filled) + new string(ConsoleRenderer.BarEmpty, empty);
        return $"{label,-8} {color}{bar}{Colors.Reset} {current,3}";
    }

    private List<string> WrapText(string text, int maxWidth)
    {
        var lines = new List<string>();
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var currentLine = new StringBuilder();

        foreach (var word in words)
        {
            var wordLen = ConsoleRenderer.GetVisibleLength(word);
            var currentLen = ConsoleRenderer.GetVisibleLength(currentLine.ToString());

            if (currentLen + wordLen + 1 > maxWidth && currentLen > 0)
            {
                lines.Add(currentLine.ToString().TrimEnd());
                currentLine.Clear();
            }

            if (currentLine.Length > 0)
                currentLine.Append(' ');
            currentLine.Append(word);
        }

        if (currentLine.Length > 0)
            lines.Add(currentLine.ToString().TrimEnd());

        return lines;
    }

    private string Truncate(string text, int maxLen)
    {
        var visLen = ConsoleRenderer.GetVisibleLength(text);
        if (visLen <= maxLen) return text;
        // Simple truncate without considering ANSI
        if (text.Length <= maxLen) return text;
        return text.Substring(0, maxLen - 2) + "..";
    }

    private string GetWeatherText(WeatherType weather)
    {
        return weather switch
        {
            WeatherType.Despejado => "Despejado",
            WeatherType.Nublado => "Nublado",
            WeatherType.Lluvioso => "Lluvioso",
            WeatherType.Tormenta => "Tormenta",
            _ => "Despejado"
        };
    }
}
