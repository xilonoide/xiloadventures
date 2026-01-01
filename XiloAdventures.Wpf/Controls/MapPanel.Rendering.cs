using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Windows;

namespace XiloAdventures.Wpf.Controls;

public partial class MapPanel : Control
{
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (_world == null)
            return;

        EnsureLayout();
        UpdateRoomRects();

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        dc.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            null,
            new Rect(0, 0, width, height));

        if (_showGrid)
        {
            DrawGrid(dc, width, height);
        }

        DrawConnections(dc);
        DrawRooms(dc);
        DrawPatrolRoutes(dc);  // Después de las salas para que los números sean visibles
        DrawSelectionRectangle(dc);
        DrawPendingConnection(dc);
    }

    private void EnsureLayout()
    {
        if (_world == null)
            return;

        // Distribución sencilla en rejilla para salas que no tengan posición
        const double padding = 40;
        int index = 0;

        int roomsPerRow;
        if (double.IsNaN(ActualWidth) || ActualWidth <= 0)
        {
            roomsPerRow = 6;
        }
        else
        {
            roomsPerRow = Math.Max(1, (int)((ActualWidth - padding) / (RoomBoxWidth + padding)));
        }

        foreach (var room in _world.Rooms)
        {
            if (!_roomPositions.ContainsKey(room.Id))
            {
                int row = index / roomsPerRow;
                int col = index % roomsPerRow;

                double x = padding + RoomBoxWidth / 2 + col * (RoomBoxWidth + padding);
                double y = padding + RoomBoxHeight / 2 + row * (RoomBoxHeight + padding);

                _roomPositions[room.Id] = new Point(x, y);
            }

            index++;
        }
    }

    private void UpdateRoomRects()
    {
        _roomRects.Clear();
        _portRects.Clear();
        _roomObjectIconRects.Clear();
        _roomNpcIconRects.Clear();
        _roomStartIconRects.Clear();
        _doorIconRects.Clear();
        _keyIconRects.Clear();

        if (_world == null)
            return;

        foreach (var room in _world.Rooms)
        {
            if (!_roomPositions.TryGetValue(room.Id, out var logicalCenter))
                continue;

            Point topLeftLogical = new(
                logicalCenter.X - RoomBoxWidth / 2.0,
                logicalCenter.Y - RoomBoxHeight / 2.0);
            Point bottomRightLogical = new(
                logicalCenter.X + RoomBoxWidth / 2.0,
                logicalCenter.Y + RoomBoxHeight / 2.0);

            Point topLeftScreen = LogicalToScreen(topLeftLogical);
            Point bottomRightScreen = LogicalToScreen(bottomRightLogical);

            Rect rect = new Rect(topLeftScreen, bottomRightScreen);
            _roomRects[room.Id] = rect;
        }
    }

    private void DrawRooms(DrawingContext dc)
    {
        if (_world == null)
            return;

        Typeface typeface = new("Segoe UI");

        foreach (var room in _world.Rooms)
        {
            if (!_roomPositions.TryGetValue(room.Id, out _))
                continue;

            if (!_roomRects.TryGetValue(room.Id, out var rect))
                continue;

            bool isSelected = _selectedRoomIds.Contains(room.Id);
            bool isLit = room.IsIlluminated;
            bool isTestPlayerRoom = _testPlayerRoomId != null &&
                string.Equals(room.Id, _testPlayerRoomId, StringComparison.OrdinalIgnoreCase);
            bool isBreathingRoom = _breathingRoomId != null &&
                string.Equals(room.Id, _breathingRoomId, StringComparison.OrdinalIgnoreCase);

            // Filtro por zona: reducir opacidad de salas que no pertenecen a la zona filtrada
            bool isInFilteredZone = string.IsNullOrEmpty(_zoneFilter) ||
                string.Equals(room.Zone, _zoneFilter, StringComparison.OrdinalIgnoreCase);
            double zoneOpacity = isInFilteredZone ? 1.0 : 0.25;

            // Aplicar opacidad para salas fuera de la zona filtrada
            if (zoneOpacity < 1.0)
            {
                dc.PushOpacity(zoneOpacity);
            }

            // Dibujar resplandor con efecto de respiración (5 pulsos)
            if (isBreathingRoom)
            {
                double intensity = GetBreathingIntensity();
                var glowColor = Color.FromRgb(0, 180, 255); // Mismo color que el test player room

                for (int i = 4; i >= 1; i--)
                {
                    double expand = i * 4 * intensity; // El tamaño también respira
                    byte baseAlpha = (byte)(60 - i * 12);
                    byte alpha = (byte)(baseAlpha * intensity);
                    var glowRect = new Rect(
                        rect.X - expand,
                        rect.Y - expand,
                        rect.Width + expand * 2,
                        rect.Height + expand * 2);
                    var glowBrush = new SolidColorBrush(Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B));
                    dc.DrawRoundedRectangle(glowBrush, null, glowRect, 6 + expand / 2, 6 + expand / 2);
                }
            }
            // Dibujar resplandor si es la sala del jugador en prueba (solo si no está respirando)
            else if (isTestPlayerRoom)
            {
                var glowColor = Color.FromRgb(0, 180, 255);
                for (int i = 4; i >= 1; i--)
                {
                    double expand = i * 4;
                    byte alpha = (byte)(60 - i * 12);
                    var glowRect = new Rect(
                        rect.X - expand,
                        rect.Y - expand,
                        rect.Width + expand * 2,
                        rect.Height + expand * 2);
                    var glowBrush = new SolidColorBrush(Color.FromArgb(alpha, glowColor.R, glowColor.G, glowColor.B));
                    dc.DrawRoundedRectangle(glowBrush, null, glowRect, 6 + expand / 2, 6 + expand / 2);
                }
            }

            // Colores:
            //  - Salas seleccionadas: verde oscuro.
            //  - Salas iluminadas (IsLit = true) no seleccionadas: azul (como el antiguo color de selección).
            //  - Salas no iluminadas y no seleccionadas: gris oscuro original.
            SolidColorBrush selectedBrush = new(Color.FromRgb(40, 100, 40));
            SolidColorBrush litBrush = new(Color.FromRgb(70, 110, 170));
            SolidColorBrush normalBrush = new(Color.FromRgb(45, 45, 45));

            Brush fill = isSelected
                ? selectedBrush
                : (isLit ? litBrush : normalBrush);

            double borderThickness = room.IsInterior ? 2.0 : 1.0;

            // Borde más brillante si es la sala del jugador o está respirando
            Pen borderPen;
            if (isBreathingRoom)
            {
                double intensity = GetBreathingIntensity();
                byte brightness = (byte)(150 + 105 * intensity); // 150-255
                borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0, brightness, 255)), 2.5);
            }
            else if (isTestPlayerRoom)
            {
                borderPen = new Pen(new SolidColorBrush(Color.FromRgb(0, 200, 255)), 2.5);
            }
            else if (isSelected)
            {
                borderPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 220, 255)), borderThickness);
            }
            else
            {
                borderPen = new Pen(new SolidColorBrush(Color.FromRgb(120, 120, 120)), borderThickness);
            }

            dc.DrawRoundedRectangle(fill, borderPen, rect, 6, 6);

            string text = string.IsNullOrWhiteSpace(room.Name)
                ? room.Id
                : room.Name;

            var formatted = new FormattedText(
                text,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                12,
                Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            // Si el nombre no cabe en una sola línea, permitimos que se divida en varias líneas
            // ajustándolo al ancho de la caja de la sala.
            const double textPadding = 10.0;
            formatted.MaxTextWidth = rect.Width - textPadding * 2.0;

            Point textPos = new(
                rect.X + (rect.Width - formatted.Width) / 2.0,
                rect.Y + (rect.Height - formatted.Height) / 2.0);

            // Dibujamos iconos de objetos y NPCs si la sala contiene alguno.
            bool hasObjects = room.ObjectIds != null && room.ObjectIds.Count > 0;

            // Además, comprobamos si hay objetos cuyo RoomId apunta a esta sala,
            // por si el JSON sólo ha rellenado RoomId y no la lista Room.ObjectIds.
            if (!hasObjects && _world?.Objects != null)
            {
                hasObjects = _world.Objects.Any(o =>
                    !string.IsNullOrWhiteSpace(o.RoomId) &&
                    string.Equals(o.RoomId, room.Id, StringComparison.OrdinalIgnoreCase));
            }

            bool hasNpcs = room.NpcIds != null && room.NpcIds.Count > 0;

            // Además, comprobamos si hay NPCs cuyo RoomId apunta a esta sala,
            // por si el JSON sólo ha rellenado RoomId y no la lista Room.NpcIds.
            if (!hasNpcs && _world?.Npcs != null)
            {
                hasNpcs = _world.Npcs.Any(n =>
                    !string.IsNullOrWhiteSpace(n.RoomId) &&
                    string.Equals(n.RoomId, room.Id, StringComparison.OrdinalIgnoreCase));
            }

            bool isStartRoom = _world?.Game != null &&
                               string.Equals(_world.Game.StartRoomId, room.Id, StringComparison.OrdinalIgnoreCase);

            const double iconSize = 14.0;
            const double iconMargin = 4.0;

            if (hasObjects)
            {
                Rect objRect = new Rect(
                    rect.X + iconMargin,
                    rect.Y + iconMargin,
                    iconSize,
                    iconSize);

                _roomObjectIconRects[room.Id] = objRect;

                SolidColorBrush objBg = new SolidColorBrush(Color.FromRgb(90, 140, 90));
                Pen objPen = new Pen(new SolidColorBrush(Color.FromRgb(230, 230, 230)), 0.8);
                dc.DrawRoundedRectangle(objBg, objPen, objRect, 3, 3);

                var objText = new FormattedText(
                    "O",
                    System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                Point objTextPos = new(
                    objRect.X + (objRect.Width - objText.Width) / 2.0,
                    objRect.Y + (objRect.Height - objText.Height) / 2.0);
                dc.DrawText(objText, objTextPos);
            }

            if (hasNpcs)
            {
                Rect npcRect = new Rect(
                    rect.X + rect.Width - iconMargin - iconSize,
                    rect.Y + iconMargin,
                    iconSize,
                    iconSize);

                _roomNpcIconRects[room.Id] = npcRect;

                SolidColorBrush npcBg = new SolidColorBrush(Color.FromRgb(140, 90, 90));
                Pen npcPen = new Pen(new SolidColorBrush(Color.FromRgb(230, 230, 230)), 0.8);
                dc.DrawRoundedRectangle(npcBg, npcPen, npcRect, 3, 3);

                var npcText = new FormattedText(
                    "N",
                    System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                Point npcTextPos = new(
                    npcRect.X + (npcRect.Width - npcText.Width) / 2.0,
                    npcRect.Y + (npcRect.Height - npcText.Height) / 2.0);
                dc.DrawText(npcText, npcTextPos);
            }

            if (isStartRoom)
            {
                Rect startRect = new Rect(
                    rect.X + iconMargin,
                    rect.Bottom - iconMargin - iconSize,
                    iconSize,
                    iconSize);

                _roomStartIconRects[room.Id] = startRect;

                SolidColorBrush startBg = new SolidColorBrush(Color.FromRgb(90, 90, 140));
                Pen startPen = new Pen(new SolidColorBrush(Color.FromRgb(200, 200, 255)), 1.0);
                dc.DrawRoundedRectangle(startBg, startPen, startRect, 3, 3);

                var startText = new FormattedText(
                    "S",
                    System.Globalization.CultureInfo.CurrentUICulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    10,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                Point startTextPos = new(
                    startRect.X + (startRect.Width - startText.Width) / 2.0,
                    startRect.Y + (startRect.Height - startText.Height) / 2.0);
                dc.DrawText(startText, startTextPos);
            }


            // Dibujamos los puntos de salida (puertos) alrededor de la sala.
            const double portSize = 12.0;
            double halfWidth = rect.Width / 2.0;
            double halfHeight = rect.Height / 2.0;

            Point centerScreen = new Point(rect.X + halfWidth, rect.Y + halfHeight);

            Point topCenter = new Point(centerScreen.X, rect.Y);
            Point bottomCenter = new Point(centerScreen.X, rect.Y + rect.Height);
            Point leftCenter = new Point(rect.X, centerScreen.Y);
            Point rightCenter = new Point(rect.X + rect.Width, centerScreen.Y);

            Point topLeft = new Point(rect.X, rect.Y);
            Point topRight = new Point(rect.X + rect.Width, rect.Y);
            Point bottomLeft = new Point(rect.X, rect.Y + rect.Height);
            Point bottomRight = new Point(rect.X + rect.Width, rect.Y + rect.Height);

            // Puntos "arriba" y "abajo" algo separados de los centros para no
            // solaparse tanto con el texto, pero situados sobre el borde superior/inferior.
            Point upPoint = new Point((centerScreen.X + topRight.X) / 2.0, rect.Y);
            Point downPoint = new Point((centerScreen.X + bottomRight.X) / 2.0, rect.Y + rect.Height);

            var ports = new (string direction, Point point)[]
            {
                ("norte", topCenter),
                ("sur", bottomCenter),
                ("oeste", leftCenter),
                ("este", rightCenter),
                ("noroeste", topLeft),
                ("noreste", topRight),
                ("suroeste", bottomLeft),
                ("sureste", bottomRight),
                ("arriba", upPoint),
                ("abajo", downPoint)
            };

            foreach (var port in ports)
            {
                Rect portRect = new Rect(
                    port.point.X - portSize / 2.0,
                    port.point.Y - portSize / 2.0,
                    portSize,
                    portSize);

                _portRects[(room.Id, port.direction)] = portRect;

                // Estilo circular como en el editor de scripts
                SolidColorBrush portFill = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                Pen portPen = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1.5);

                var center = new Point(port.point.X, port.point.Y);
                dc.DrawEllipse(portFill, portPen, center, portSize / 2.0, portSize / 2.0);
            }

            dc.DrawText(formatted, textPos);

            // Restaurar opacidad si se aplicó filtro de zona
            if (zoneOpacity < 1.0)
            {
                dc.Pop();
            }
        }
    }



    private void DrawConnections(DrawingContext dc)
    {
        if (_world == null)
            return;

        // Los rectángulos de hit test de salidas se recalculan en cada render
        _exitHitRects.Clear();

        // Para evitar solapamiento de textos cuando hay conexiones bidireccionales
        // entre las mismas dos salas, sólo dibujamos una etiqueta por par de salas.
        var labeledConnections = new HashSet<(string a, string b)>();

        // Mapa rápido de puertas por Id para consultar su estado (abierta/cerrada).
        // En modo pruebas usar _testDoors (estado del GameState), sino usar _world.Doors
        var doorsSource = _testDoors ?? _world.Doors;
        Dictionary<string, Door>? doorsById = null;
        if (doorsSource != null && doorsSource.Count > 0)
        {
            doorsById = doorsSource
                .Where(d => !string.IsNullOrWhiteSpace(d.Id))
                .ToDictionary(d => d.Id, d => d, StringComparer.OrdinalIgnoreCase);
        }

        // Pens para conexiones según estado de puerta
        Pen normalPen = new(new SolidColorBrush(Color.FromRgb(200, 200, 200)), 2.0);
        Pen selectedPen = new(new SolidColorBrush(Color.FromRgb(255, 220, 80)), 3.0);
        Pen doorOpenPen = new(new SolidColorBrush(Color.FromRgb(80, 200, 80)), 2.0);
        Pen doorClosedPen = new(new SolidColorBrush(Color.FromRgb(200, 80, 80)), 2.0);
        Pen doorOpenSelectedPen = new(new SolidColorBrush(Color.FromRgb(120, 255, 120)), 3.0);
        Pen doorClosedSelectedPen = new(new SolidColorBrush(Color.FromRgb(255, 120, 120)), 3.0);

        Typeface typeface = new("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Si hay exactamente una sala seleccionada, los textos de las salidas
        // sólo se mostrarán para las conexiones que involucren a esa sala.
        string? singleSelectedRoomId = null;
        bool hasSingleSelectedRoom = _selectedRoomIds.Count == 1;
        if (hasSingleSelectedRoom)
        {
            singleSelectedRoomId = _selectedRoomIds.First();
        }

        foreach (var room in _world.Rooms)
        {
            if (room.Exits == null)
                continue;

            if (!_roomRects.TryGetValue(room.Id, out var fromRect))
                continue;

            for (int i = 0; i < room.Exits.Count; i++)
            {
                var exit = room.Exits[i];
                if (exit == null || string.IsNullOrEmpty(exit.TargetRoomId))
                    continue;

                var target = _world.Rooms.FirstOrDefault(r => r.Id == exit.TargetRoomId);
                if (target == null)
                    continue;

                if (!_roomRects.TryGetValue(target.Id, out var toRect))
                    continue;

                string rawDirection = exit.Direction ?? string.Empty;
                string normDir = NormalizeDirectionLabel(rawDirection);

                Point fromPoint = GetPortPointForDirection(fromRect, normDir);
                string oppositeDir = GetOppositeDirection(normDir);
                Point toPoint = GetPortPointForDirection(toRect, oppositeDir);

                var exitKey = (room.Id, i);
                bool isSelected = _selectedExits.Contains(exitKey);

                // Detectar si hay puerta en esta conexión
                Door? door = null;
                if (doorsById != null && !string.IsNullOrEmpty(exit.DoorId))
                {
                    doorsById.TryGetValue(exit.DoorId, out door);
                }

                if (door == null && doorsSource != null && doorsSource.Count > 0)
                {
                    door = doorsSource.FirstOrDefault(d =>
                        !string.IsNullOrEmpty(d.RoomIdA) &&
                        !string.IsNullOrEmpty(d.RoomIdB) &&
                        ((string.Equals(d.RoomIdA, room.Id, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(d.RoomIdB, target.Id, StringComparison.OrdinalIgnoreCase)) ||
                         (string.Equals(d.RoomIdB, room.Id, StringComparison.OrdinalIgnoreCase) &&
                          string.Equals(d.RoomIdA, target.Id, StringComparison.OrdinalIgnoreCase))));
                }

                // Determinar el pen según estado de puerta y selección
                Pen linePen;
                if (door != null)
                {
                    if (door.IsOpen)
                        linePen = isSelected ? doorOpenSelectedPen : doorOpenPen;
                    else
                        linePen = isSelected ? doorClosedSelectedPen : doorClosedPen;
                }
                else
                {
                    linePen = isSelected ? selectedPen : normalPen;
                }

                // No dibujar la línea de la salida que se está editando actualmente
                // (se dibuja por separado en DrawPendingConnection con línea punteada)
                bool isBeingEdited = _isEditingExit &&
                                     _editingExitRoom != null &&
                                     ReferenceEquals(_editingExitRoom, room) &&
                                     _editingExitIndex == i;

                if (!isBeingEdited)
                {
                    DrawBezierConnection(dc, fromPoint, toPoint, linePen);
                }

                // Rectángulo de hit test que cubre toda la línea de la salida,
                // con un pequeño margen para facilitar el click.
                Point mid = Midpoint(fromPoint, toPoint);

                // Rectángulo de hit-test estrecho alrededor de la línea de la salida,
                // para evitar que se seleccione haciendo click demasiado lejos.
                Rect baseRect = new Rect(fromPoint, toPoint);
                double dx = Math.Abs(fromPoint.X - toPoint.X);
                double dy = Math.Abs(fromPoint.Y - toPoint.Y);
                const double halfThickness = 4.0;

                Rect lineHitRect;
                if (dx >= dy)
                {
                    // Conexión principalmente horizontal: corredor fino en vertical.
                    double centerY = (fromPoint.Y + toPoint.Y) / 2.0;
                    lineHitRect = new Rect(
                        baseRect.X,
                        centerY - halfThickness,
                        baseRect.Width,
                        halfThickness * 2.0);
                }
                else
                {
                    // Conexión principalmente vertical: corredor fino en horizontal.
                    double centerX = (fromPoint.X + toPoint.X) / 2.0;
                    lineHitRect = new Rect(
                        centerX - halfThickness,
                        baseRect.Y,
                        halfThickness * 2.0,
                        baseRect.Height);
                }

                // Inicialmente, el hit test de la salida es el de la línea.
                Rect hitRect = lineHitRect;

                // Etiqueta con la dirección en el punto medio.
                // Si hay exactamente una sala seleccionada, el texto de la salida
                // se muestra desde el punto de vista de esa sala.
                string labelDirectionRaw = exit.Direction ?? string.Empty;

                if (hasSingleSelectedRoom && singleSelectedRoomId != null)
                {
                    if (singleSelectedRoomId == room.Id)
                    {
                        // Vista desde la sala origen: usamos la dirección tal cual está definida.
                        labelDirectionRaw = rawDirection;
                    }
                    else if (singleSelectedRoomId == target.Id)
                    {
                        // Vista desde la sala destino: usamos la dirección opuesta.
                        string labelNorm = NormalizeDirectionLabel(rawDirection);
                        string oppositeForLabel = GetOppositeDirection(labelNorm);
                        labelDirectionRaw = oppositeForLabel;
                    }
                }

                string label = GetSingleDirectionLabel(labelDirectionRaw);

                // Clave de conexión independiente del sentido (A-B == B-A)
                var key = string.CompareOrdinal(room.Id, target.Id) <= 0
                    ? (room.Id, target.Id)
                    : (target.Id, room.Id);

                bool canDrawLabel =
                    !string.IsNullOrWhiteSpace(label) &&
                    (!hasSingleSelectedRoom ||
                     (singleSelectedRoomId == room.Id || singleSelectedRoomId == target.Id)) &&
                    !labeledConnections.Contains(key);

                if (canDrawLabel && !isBeingEdited)
                {
                    labeledConnections.Add(key);

                    var formatted = new FormattedText(
                        label,
                        System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        11,
                        isSelected ? Brushes.LightYellow : Brushes.White,
                        dpi);

                    Point textPos = new(
                        mid.X - formatted.Width / 2.0,
                        mid.Y - formatted.Height - 4);

                    // Rectángulo específico para el texto de la etiqueta
                    Rect textRect = new Rect(
                        textPos.X - 4,
                        textPos.Y - 2,
                        formatted.Width + 8,
                        formatted.Height + 4);

                    // Combinamos la zona de la línea con la del texto.
                    hitRect = Rect.Union(hitRect, textRect);

                    dc.DrawText(formatted, textPos);
                }

                // No agregar hit test para la salida que se está editando
                if (!isBeingEdited)
                {
                    _exitHitRects[exitKey] = hitRect;
                }

                // Dibujar icono de llave si la puerta tiene cerradura con llave
                if (door != null && !string.IsNullOrWhiteSpace(door.KeyObjectId) && !isBeingEdited)
                {
                    const double keyIconSize = 16.0;
                    const double keyIconOffset = 12.0;

                    // Posicionar el icono debajo del punto medio de la línea
                    Rect keyRect = new Rect(
                        mid.X - keyIconSize / 2.0,
                        mid.Y + keyIconOffset,
                        keyIconSize,
                        keyIconSize);

                    _keyIconRects[door.KeyObjectId] = keyRect;

                    // Color según estado: verde si está abierta, rojo si está cerrada/bloqueada
                    Color keyColor = door.IsOpen ? Color.FromRgb(80, 200, 80) : Color.FromRgb(200, 80, 80);
                    SolidColorBrush keyBg = new SolidColorBrush(keyColor);
                    Pen keyPen = new Pen(Brushes.White, 1.0);

                    dc.DrawRoundedRectangle(keyBg, keyPen, keyRect, 3, 3);

                    // Dibujar símbolo de llave (usando Segoe MDL2 Assets)
                    var keyText = new FormattedText(
                        "\uE72E", // Icono de llave
                        System.Globalization.CultureInfo.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        new Typeface(new FontFamily("Segoe MDL2 Assets"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal),
                        10,
                        Brushes.White,
                        dpi);

                    Point keyTextPos = new(
                        keyRect.X + (keyRect.Width - keyText.Width) / 2.0,
                        keyRect.Y + (keyRect.Height - keyText.Height) / 2.0);
                    dc.DrawText(keyText, keyTextPos);
                }
            }
        }
    }

    private void DrawSelectionRectangle(DrawingContext dc)
    {
        if (!_isDragSelecting)
            return;

        Rect rect = new(_selectionStartScreen, _selectionEndScreen);

        if (rect.Width < 0)
        {
            rect = new Rect(
                rect.X + rect.Width,
                rect.Y,
                -rect.Width,
                rect.Height);
        }
        if (rect.Height < 0)
        {
            rect = new Rect(
                rect.X,
                rect.Y + rect.Height,
                rect.Width,
                -rect.Height);
        }

        dc.DrawRectangle(
            new SolidColorBrush(Color.FromArgb(40, 80, 160, 240)),
            new Pen(new SolidColorBrush(Color.FromRgb(80, 160, 240)), 1.0),
            rect);
    }

    private void DrawPendingConnection(DrawingContext dc)
    {
        if (_world == null)
            return;

        Point fromScreen;
        Point toScreen;
        bool hasConnection = false;

        // Dibujar línea cuando se está editando una salida existente
        if (_isEditingExit && _editingExitRoom != null &&
            _editingExitRoom.Exits != null &&
            _editingExitIndex >= 0 && _editingExitIndex < _editingExitRoom.Exits.Count)
        {
            var exit = _editingExitRoom.Exits[_editingExitIndex];

            if (_editingExitIsOrigin)
            {
                // Editando el ORIGEN: la línea va desde el mouse hasta el puerto destino
                fromScreen = _connectionCurrentMouseScreen;

                // Encontrar el destino actual
                var targetRoom = _world.Rooms.FirstOrDefault(r => r.Id == exit.TargetRoomId);
                if (targetRoom != null && _roomRects.TryGetValue(targetRoom.Id, out var targetRect))
                {
                    string normDir = NormalizeDirectionLabel(exit.Direction);
                    string oppositeDir = GetOppositeDirection(normDir);
                    toScreen = GetPortPointForDirection(targetRect, oppositeDir);
                    hasConnection = true;
                }
            }
            else
            {
                // Editando el DESTINO: la línea va desde el puerto origen hasta el mouse
                if (_roomRects.TryGetValue(_editingExitRoom.Id, out var editRect))
                {
                    string normDir = NormalizeDirectionLabel(exit.Direction);
                    fromScreen = GetPortPointForDirection(editRect, normDir);
                    toScreen = _connectionCurrentMouseScreen;
                    hasConnection = true;
                }
            }
        }
        // Dibujar línea cuando se está creando una nueva conexión
        else if (_connectionStart != null)
        {
            if (_roomRects.TryGetValue(_connectionStart.Id, out var fromRect))
            {
                if (!string.IsNullOrEmpty(_pendingPortDirection))
                {
                    string normDir = NormalizeDirectionLabel(_pendingPortDirection);
                    fromScreen = GetPortPointForDirection(fromRect, normDir);
                }
                else
                {
                    fromScreen = new Point(
                        fromRect.X + fromRect.Width / 2.0,
                        fromRect.Y + fromRect.Height / 2.0);
                }
                toScreen = _connectionCurrentMouseScreen;
                hasConnection = true;
            }
            else if (_roomPositions.TryGetValue(_connectionStart.Id, out var fromCenterLogical))
            {
                fromScreen = LogicalToScreen(fromCenterLogical);
                toScreen = _connectionCurrentMouseScreen;
                hasConnection = true;
            }
        }

        if (!hasConnection)
            return;

        Pen pen = new(new SolidColorBrush(Color.FromRgb(0, 200, 255)), 2.0)
        {
            DashStyle = DashStyles.Dash
        };

        DrawBezierConnection(dc, fromScreen, toScreen, pen);
    }

    private void DrawBezierConnection(DrawingContext dc, Point from, Point to, Pen pen)
    {
        // Calcular la distancia y dirección para los puntos de control
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Offset de control proporcional a la distancia
        var controlOffset = Math.Max(30, distance / 3);

        Point control1, control2;

        // Determinar la dirección predominante para curvar apropiadamente
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            // Conexión más horizontal
            control1 = new Point(from.X + controlOffset * Math.Sign(dx), from.Y);
            control2 = new Point(to.X - controlOffset * Math.Sign(dx), to.Y);
        }
        else
        {
            // Conexión más vertical
            control1 = new Point(from.X, from.Y + controlOffset * Math.Sign(dy));
            control2 = new Point(to.X, to.Y - controlOffset * Math.Sign(dy));
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(from, false, false);
            ctx.BezierTo(control1, control2, to, true, false);
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    private void DrawPatrolRoutes(DrawingContext dc)
    {
        if (_world == null) return;

        // Si estamos editando una ruta, dibujar solo esa
        if (_isEditingPatrolRoute && _patrolRouteNpc != null)
        {
            DrawSinglePatrolRoute(dc, _patrolRouteNpc, isEditing: true);
            return;
        }

        // Si se deben mostrar todas las rutas (nodo padre NPCs seleccionado)
        if (_showAllPatrolRoutes)
        {
            // Obtener NPCs con rutas válidas, ordenados por longitud de ruta descendente
            var npcsWithRoutes = _world.Npcs
                .Where(n => n.PatrolRoute.Count >= 2)
                .OrderByDescending(n => n.PatrolRoute.Count)
                .ToList();

            // Registrar qué conexiones ya han sido dibujadas para evitar solapamiento
            var drawnConnections = new HashSet<(string, string)>(new ConnectionComparer());

            foreach (var npc in npcsWithRoutes)
            {
                DrawSinglePatrolRoute(dc, npc, isEditing: false, drawnConnections);
            }
            return;
        }

        // Si hay NPCs específicos seleccionados, mostrar solo sus rutas
        if (_visiblePatrolRouteNpcIds.Count > 0)
        {
            foreach (var npc in _world.Npcs)
            {
                if (npc.PatrolRoute.Count >= 2 &&
                    _visiblePatrolRouteNpcIds.Contains(npc.Id))
                {
                    DrawSinglePatrolRoute(dc, npc, isEditing: false);
                }
            }
        }
        // Si no hay nada seleccionado, no dibujar ninguna ruta
    }

    /// <summary>
    /// Comparador de conexiones que trata (A,B) igual que (B,A)
    /// </summary>
    private class ConnectionComparer : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y)
        {
            return (StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item1) &&
                    StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item2)) ||
                   (StringComparer.OrdinalIgnoreCase.Equals(x.Item1, y.Item2) &&
                    StringComparer.OrdinalIgnoreCase.Equals(x.Item2, y.Item1));
        }

        public int GetHashCode((string, string) obj)
        {
            var h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item1);
            var h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Item2);
            return h1 ^ h2; // XOR para que (A,B) y (B,A) tengan el mismo hash
        }
    }

    private void DrawSinglePatrolRoute(DrawingContext dc, Npc npc, bool isEditing, HashSet<(string, string)>? drawnConnections = null)
    {
        if (npc.PatrolRoute.Count < 1) return;

        // Color naranja para rutas de patrulla
        var routeColor = isEditing
            ? Color.FromRgb(255, 180, 80)  // Naranja más brillante cuando se edita
            : Color.FromRgb(200, 140, 60); // Naranja normal

        Pen routePen = new(new SolidColorBrush(routeColor), 2.5)
        {
            DashStyle = new DashStyle(new double[] { 6, 3 }, 0)
        };

        Typeface typeface = new("Segoe UI");
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        // Offset perpendicular para separar las líneas de patrulla de las conexiones
        const double patrolOffset = 12.0;

        // Dibujar las líneas entre waypoints (modo ping-pong)
        for (int i = 0; i < npc.PatrolRoute.Count; i++)
        {
            var roomId = npc.PatrolRoute[i];
            var nextRoomId = i + 1 < npc.PatrolRoute.Count ? npc.PatrolRoute[i + 1] : null;

            if (!_roomRects.TryGetValue(roomId, out var fromRect)) continue;

            // Dibujar número de waypoint
            var waypointNum = (i + 1).ToString();
            var formatted = new FormattedText(
                waypointNum,
                System.Globalization.CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                typeface,
                11,
                new SolidColorBrush(routeColor),
                dpi);

            // Posicionar el número en la esquina inferior derecha de la sala
            Point numPos = new(
                fromRect.Right - 18,
                fromRect.Bottom - 18);

            // Fondo del número
            Rect numBg = new Rect(numPos.X - 2, numPos.Y - 2, formatted.Width + 4, formatted.Height + 4);
            dc.DrawRoundedRectangle(
                new SolidColorBrush(Color.FromArgb(200, 40, 40, 40)),
                new Pen(new SolidColorBrush(routeColor), 1),
                numBg, 3, 3);
            dc.DrawText(formatted, numPos);

            // Dibujar línea curva al siguiente waypoint (modo ping-pong: ida y vuelta)
            if (nextRoomId != null && _roomRects.TryGetValue(nextRoomId, out var toRect))
            {
                // Si estamos controlando solapamientos, verificar si esta conexión ya fue dibujada
                if (drawnConnections != null)
                {
                    var connectionKey = (roomId, nextRoomId);
                    if (drawnConnections.Contains(connectionKey))
                    {
                        continue; // Esta conexión ya fue dibujada por un NPC con ruta más larga
                    }
                    drawnConnections.Add(connectionKey);
                }

                // Buscar la salida real que conecta estas dos salas
                var fromRoom = _world?.Rooms?.FirstOrDefault(r =>
                    string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));

                if (fromRoom?.Exits == null) continue;

                var exit = fromRoom.Exits.FirstOrDefault(e =>
                    e != null && string.Equals(e.TargetRoomId, nextRoomId, StringComparison.OrdinalIgnoreCase));

                if (exit == null) continue;

                // Usar los mismos puntos de puerto que las conexiones normales
                string normDir = NormalizeDirectionLabel(exit.Direction ?? string.Empty);
                string oppositeDir = GetOppositeDirection(normDir);

                Point fromPort = GetPortPointForDirection(fromRect, normDir);
                Point toPort = GetPortPointForDirection(toRect, oppositeDir);

                // Calcular la dirección y los vectores perpendiculares basados en la línea de conexión
                double dx = toPort.X - fromPort.X;
                double dy = toPort.Y - fromPort.Y;
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length < 1) continue;

                // Vector perpendicular derecho (rotación 90° horario): (dy, -dx) normalizado
                // Ida siempre por la derecha del puerto
                double rightPerpX = dy / length;
                double rightPerpY = -dx / length;

                // Vector perpendicular izquierdo (rotación 90° antihorario): (-dy, dx) normalizado
                // Vuelta siempre por la izquierda del puerto
                double leftPerpX = -dy / length;
                double leftPerpY = dx / length;

                // Puntos de inicio/fin de las líneas de patrulla junto a los puertos
                Point fromIda = new(fromPort.X + rightPerpX * patrolOffset, fromPort.Y + rightPerpY * patrolOffset);
                Point toIda = new(toPort.X + rightPerpX * patrolOffset, toPort.Y + rightPerpY * patrolOffset);

                Point fromVuelta = new(toPort.X + leftPerpX * patrolOffset, toPort.Y + leftPerpY * patrolOffset);
                Point toVuelta = new(fromPort.X + leftPerpX * patrolOffset, fromPort.Y + leftPerpY * patrolOffset);

                // Línea de ida (por la derecha)
                DrawPatrolBezierConnection(dc, fromIda, toIda, routePen, rightPerpX, rightPerpY, patrolOffset);
                DrawPatrolArrow(dc, fromIda, toIda, routeColor, rightPerpX, rightPerpY, patrolOffset);

                // Línea de vuelta (por la izquierda)
                DrawPatrolBezierConnection(dc, fromVuelta, toVuelta, routePen, leftPerpX, leftPerpY, patrolOffset);
                DrawPatrolArrow(dc, fromVuelta, toVuelta, routeColor, leftPerpX, leftPerpY, patrolOffset);
            }
        }

        // Si estamos editando, resaltar las salas en la ruta
        if (isEditing)
        {
            var highlightBrush = new SolidColorBrush(Color.FromArgb(60, 255, 165, 0));
            var highlightPen = new Pen(new SolidColorBrush(routeColor), 2);

            foreach (var roomId in npc.PatrolRoute)
            {
                if (_roomRects.TryGetValue(roomId, out var rect))
                {
                    dc.DrawRoundedRectangle(highlightBrush, highlightPen, rect, 8, 8);
                }
            }
        }
    }

    private Point GetEdgePointWithOffset(Rect rect, Point target, double perpX, double perpY, double offset)
    {
        Point center = new(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        double dx = target.X - center.X;
        double dy = target.Y - center.Y;

        if (Math.Abs(dx) < 0.001 && Math.Abs(dy) < 0.001)
            return new Point(center.X + perpX * offset, center.Y + perpY * offset);

        // Calcular la intersección con el borde del rectángulo
        double scaleX = Math.Abs(dx) > 0.001 ? (rect.Width / 2) / Math.Abs(dx) : double.MaxValue;
        double scaleY = Math.Abs(dy) > 0.001 ? (rect.Height / 2) / Math.Abs(dy) : double.MaxValue;
        double scale = Math.Min(scaleX, scaleY);

        Point edgePoint = new(center.X + dx * scale, center.Y + dy * scale);

        // Aplicar offset perpendicular
        return new Point(edgePoint.X + perpX * offset, edgePoint.Y + perpY * offset);
    }

    private void DrawPatrolBezierConnection(DrawingContext dc, Point from, Point to, Pen pen,
        double perpX, double perpY, double offset)
    {
        // Calcular la distancia y dirección para los puntos de control
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var distance = Math.Sqrt(dx * dx + dy * dy);

        // Offset de control proporcional a la distancia
        var controlOffset = Math.Max(30, distance / 3);

        Point control1, control2;

        // Determinar la dirección predominante para curvar apropiadamente
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            // Conexión más horizontal
            control1 = new Point(from.X + controlOffset * Math.Sign(dx), from.Y);
            control2 = new Point(to.X - controlOffset * Math.Sign(dx), to.Y);
        }
        else
        {
            // Conexión más vertical
            control1 = new Point(from.X, from.Y + controlOffset * Math.Sign(dy));
            control2 = new Point(to.X, to.Y - controlOffset * Math.Sign(dy));
        }

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(from, false, false);
            ctx.BezierTo(control1, control2, to, true, false);
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    private void DrawPatrolArrow(DrawingContext dc, Point from, Point to, Color color,
        double perpX, double perpY, double offset)
    {
        // Calcular el punto medio de la curva (aproximación)
        Point mid = new((from.X + to.X) / 2, (from.Y + to.Y) / 2);

        double dx = to.X - from.X;
        double dy = to.Y - from.Y;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1) return;

        // Normalizar
        dx /= length;
        dy /= length;

        // Tamaño de la flecha
        const double arrowSize = 8;

        // Puntos de la flecha
        Point tip = mid;
        Point left = new(mid.X - arrowSize * dx - arrowSize * 0.5 * (-dy),
                         mid.Y - arrowSize * dy - arrowSize * 0.5 * dx);
        Point right = new(mid.X - arrowSize * dx + arrowSize * 0.5 * (-dy),
                          mid.Y - arrowSize * dy + arrowSize * 0.5 * dx);

        var arrowGeometry = new StreamGeometry();
        using (var ctx = arrowGeometry.Open())
        {
            ctx.BeginFigure(tip, true, true);
            ctx.LineTo(left, true, false);
            ctx.LineTo(right, true, false);
        }
        arrowGeometry.Freeze();

        dc.DrawGeometry(new SolidColorBrush(color), null, arrowGeometry);
    }

    private void DrawGrid(DrawingContext dc, double width, double height)
    {
        Pen gridPen = new(new SolidColorBrush(Color.FromRgb(35, 35, 35)), 1.0);

        // Calculamos el área visible en coordenadas lógicas
        Point topLeftLogical = ScreenToLogical(new Point(0, 0));
        Point bottomRightLogical = ScreenToLogical(new Point(width, height));

        // Encontramos el inicio y fin de las líneas del grid alineadas con las celdas
        double startX = Math.Floor(topLeftLogical.X / RoomBoxWidth) * RoomBoxWidth;
        double endX = Math.Ceiling(bottomRightLogical.X / RoomBoxWidth) * RoomBoxWidth;
        double startY = Math.Floor(topLeftLogical.Y / RoomBoxHeight) * RoomBoxHeight;
        double endY = Math.Ceiling(bottomRightLogical.Y / RoomBoxHeight) * RoomBoxHeight;

        // Líneas verticales
        for (double x = startX; x <= endX; x += RoomBoxWidth)
        {
            Point topScreen = LogicalToScreen(new Point(x, topLeftLogical.Y));
            Point bottomScreen = LogicalToScreen(new Point(x, bottomRightLogical.Y));
            dc.DrawLine(gridPen, topScreen, bottomScreen);
        }

        // Líneas horizontales
        for (double y = startY; y <= endY; y += RoomBoxHeight)
        {
            Point leftScreen = LogicalToScreen(new Point(topLeftLogical.X, y));
            Point rightScreen = LogicalToScreen(new Point(bottomRightLogical.X, y));
            dc.DrawLine(gridPen, leftScreen, rightScreen);
        }
    }

}
