using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Controls;

public partial class ScriptPanel
{
    // Brushes y Pens reutilizables
    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(30, 30, 30));
    private static readonly Brush GridBrush = new SolidColorBrush(Color.FromRgb(45, 45, 45));
    private static readonly Brush GridMajorBrush = new SolidColorBrush(Color.FromRgb(55, 55, 55));
    private static readonly Brush NodeBorderBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
    private static readonly Brush NodeSelectedBorderBrush = new SolidColorBrush(Color.FromRgb(255, 200, 50));
    private static readonly Brush TextBrush = Brushes.White;
    private static readonly Brush TextDimBrush = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private static readonly Brush PortExecBrush = Brushes.White;
    private static readonly Brush PortDataBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255));
    private static readonly Brush ConnectionBrush = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private static readonly Brush ConnectionSelectedBrush = new SolidColorBrush(Color.FromRgb(255, 220, 80));
    private static readonly Brush SelectionRectBrush = new SolidColorBrush(Color.FromArgb(40, 100, 150, 255));
    private static readonly Brush SelectionRectBorderBrush = new SolidColorBrush(Color.FromRgb(100, 150, 255));

    private static readonly Pen GridPen = new(GridBrush, 1);
    private static readonly Pen GridMajorPen = new(GridMajorBrush, 1);
    private static readonly Pen NodeBorderPen = new(NodeBorderBrush, 2);
    private static readonly Pen NodeSelectedBorderPen = new(NodeSelectedBorderBrush, 3);
    private static readonly Pen ConnectionPen = new(ConnectionBrush, 2);
    private static readonly Pen ConnectionSelectedPen = new(ConnectionSelectedBrush, 3);
    private static readonly Pen SelectionRectPen = new(SelectionRectBorderBrush, 1) { DashStyle = DashStyles.Dash };
    private static readonly Pen PendingConnectionPen = new(new SolidColorBrush(Color.FromRgb(255, 255, 255)), 2) { DashStyle = DashStyles.Dash };

    private static readonly Typeface NodeTitleTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal);
    private static readonly Typeface NodeTextTypeface = new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    // Los Pens se congelan en el constructor estático de ScriptPanel.cs

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        // Fondo
        dc.DrawRectangle(BackgroundBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));

        if (_script == null) return;

        // Limpiar rectángulos de hit testing
        _nodeRects.Clear();
        _portRects.Clear();

        // Dibujar grid
        if (_showGrid)
        {
            DrawGrid(dc);
        }

        // Dibujar conexiones
        DrawConnections(dc);

        // Dibujar nodos
        DrawNodes(dc);

        // Dibujar rectángulo de selección
        if (_isDragSelecting)
        {
            DrawSelectionRectangle(dc);
        }

        // Dibujar conexión pendiente
        if (_isConnecting)
        {
            DrawPendingConnection(dc);
        }
    }

    private void DrawGrid(DrawingContext dc)
    {
        var cellSize = GridCellSize * _zoom;
        var majorInterval = 5;

        // Calcular rango visible
        var startX = _offset.X * _zoom % cellSize;
        var startY = _offset.Y * _zoom % cellSize;

        // Ajustar para que las líneas mayores queden alineadas
        var offsetCellsX = (int)Math.Floor(_offset.X / GridCellSize);
        var offsetCellsY = (int)Math.Floor(_offset.Y / GridCellSize);

        // Dibujar líneas verticales
        for (double x = startX; x < ActualWidth; x += cellSize)
        {
            var cellIndex = (int)Math.Round((x - startX) / cellSize) - offsetCellsX;
            var pen = cellIndex % majorInterval == 0 ? GridMajorPen : GridPen;
            dc.DrawLine(pen, new Point(x, 0), new Point(x, ActualHeight));
        }

        // Dibujar líneas horizontales
        for (double y = startY; y < ActualHeight; y += cellSize)
        {
            var cellIndex = (int)Math.Round((y - startY) / cellSize) - offsetCellsY;
            var pen = cellIndex % majorInterval == 0 ? GridMajorPen : GridPen;
            dc.DrawLine(pen, new Point(0, y), new Point(ActualWidth, y));
        }
    }

    private void DrawConnections(DrawingContext dc)
    {
        if (_script == null) return;

        foreach (var connection in _script.Connections)
        {
            var fromNode = _script.Nodes.FirstOrDefault(n =>
                string.Equals(n.Id, connection.FromNodeId, StringComparison.OrdinalIgnoreCase));
            var toNode = _script.Nodes.FirstOrDefault(n =>
                string.Equals(n.Id, connection.ToNodeId, StringComparison.OrdinalIgnoreCase));

            if (fromNode == null || toNode == null) continue;

            var fromTypeDef = NodeTypeRegistry.GetNodeType(fromNode.NodeType);
            var toTypeDef = NodeTypeRegistry.GetNodeType(toNode.NodeType);

            if (fromTypeDef == null || toTypeDef == null) continue;

            var fromPortIndex = Array.FindIndex(fromTypeDef.OutputPorts, p =>
                string.Equals(p.Name, connection.FromPortName, StringComparison.OrdinalIgnoreCase));
            var toPortIndex = Array.FindIndex(toTypeDef.InputPorts, p =>
                string.Equals(p.Name, connection.ToPortName, StringComparison.OrdinalIgnoreCase));

            if (fromPortIndex < 0 || toPortIndex < 0) continue;

            var fromPos = GetOutputPortScreenPosition(fromNode, fromTypeDef, fromPortIndex);
            var toPos = GetInputPortScreenPosition(toNode, toTypeDef, toPortIndex);

            var isSelected = _selectedConnectionIds.Contains(connection.Id);
            var pen = isSelected ? ConnectionSelectedPen : ConnectionPen;

            // Determinar color por tipo de puerto
            var fromPort = fromTypeDef.OutputPorts[fromPortIndex];
            if (fromPort.PortType == PortType.Data)
            {
                var dataPen = new Pen(PortDataBrush, isSelected ? 3 : 2);
                dataPen.Freeze();
                pen = dataPen;
            }

            DrawBezierConnection(dc, fromPos, toPos, pen);
        }
    }

    private void DrawBezierConnection(DrawingContext dc, Point from, Point to, Pen pen)
    {
        var distance = Math.Abs(to.X - from.X);
        var controlOffset = Math.Max(50, distance / 2);

        var control1 = new Point(from.X + controlOffset, from.Y);
        var control2 = new Point(to.X - controlOffset, to.Y);

        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(from, false, false);
            ctx.BezierTo(control1, control2, to, true, false);
        }
        geometry.Freeze();

        dc.DrawGeometry(null, pen, geometry);
    }

    private void DrawNodes(DrawingContext dc)
    {
        if (_script == null) return;

        foreach (var node in _script.Nodes)
        {
            DrawNode(dc, node);
        }
    }

    private void DrawNode(DrawingContext dc, ScriptNode node)
    {
        var typeDef = NodeTypeRegistry.GetNodeType(node.NodeType);
        if (typeDef == null) return;

        // Obtener posición
        if (!_nodePositions.TryGetValue(node.Id, out var logicalPos))
        {
            logicalPos = new Point(node.X, node.Y);
            _nodePositions[node.Id] = logicalPos;
        }

        var screenPos = LogicalToScreen(logicalPos);
        var nodeHeight = CalculateNodeHeight(typeDef);
        var nodeWidth = NodeMinWidth * _zoom;
        var nodeHeightScaled = nodeHeight * _zoom;

        var nodeRect = new Rect(screenPos.X, screenPos.Y, nodeWidth, nodeHeightScaled);
        _nodeRects[node.Id] = nodeRect;

        // Determinar si está seleccionado
        var isSelected = _selectedNodeIds.Contains(node.Id);

        // Color de fondo según categoría
        var categoryColor = CategoryColors.TryGetValue(node.Category, out var cc) ? cc : Color.FromRgb(60, 60, 60);
        var fillBrush = new SolidColorBrush(categoryColor);
        fillBrush.Freeze();

        var headerColor = Color.FromRgb(
            (byte)Math.Min(255, categoryColor.R + 20),
            (byte)Math.Min(255, categoryColor.G + 20),
            (byte)Math.Min(255, categoryColor.B + 20));
        var headerBrush = new SolidColorBrush(headerColor);
        headerBrush.Freeze();

        // Dibujar sombra
        var shadowRect = new Rect(nodeRect.X + 3, nodeRect.Y + 3, nodeRect.Width, nodeRect.Height);
        var shadowBrush = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0));
        shadowBrush.Freeze();
        dc.DrawRoundedRectangle(shadowBrush, null, shadowRect, 6, 6);

        // Dibujar cuerpo del nodo
        var borderPen = isSelected ? NodeSelectedBorderPen : NodeBorderPen;
        dc.DrawRoundedRectangle(fillBrush, borderPen, nodeRect, 6, 6);

        // Dibujar cabecera
        var headerRect = new Rect(nodeRect.X, nodeRect.Y, nodeRect.Width, NodeHeaderHeight * _zoom);
        var headerGeometry = new StreamGeometry();
        using (var ctx = headerGeometry.Open())
        {
            var radius = 6.0;
            ctx.BeginFigure(new Point(headerRect.Left + radius, headerRect.Top), true, true);
            ctx.ArcTo(new Point(headerRect.Right - radius, headerRect.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            ctx.ArcTo(new Point(headerRect.Right, headerRect.Top + radius), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
            ctx.LineTo(new Point(headerRect.Right, headerRect.Bottom), true, false);
            ctx.LineTo(new Point(headerRect.Left, headerRect.Bottom), true, false);
            ctx.LineTo(new Point(headerRect.Left, headerRect.Top + radius), true, false);
            ctx.ArcTo(new Point(headerRect.Left + radius, headerRect.Top), new Size(radius, radius), 0, false, SweepDirection.Clockwise, true, false);
        }
        headerGeometry.Freeze();
        dc.DrawGeometry(headerBrush, null, headerGeometry);

        // Dibujar título
        var titleText = new FormattedText(
            typeDef.DisplayName,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            NodeTitleTypeface,
            12 * _zoom,
            TextBrush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var titleX = nodeRect.X + 8 * _zoom;
        var titleY = nodeRect.Y + (NodeHeaderHeight * _zoom - titleText.Height) / 2;
        dc.DrawText(titleText, new Point(titleX, titleY));

        // Dibujar puertos de entrada
        var inputStartY = nodeRect.Y + NodeHeaderHeight * _zoom + 5 * _zoom;
        for (int i = 0; i < typeDef.InputPorts.Length; i++)
        {
            var port = typeDef.InputPorts[i];
            var portY = inputStartY + i * PortSpacing * _zoom;
            DrawPort(dc, node.Id, port, nodeRect.X, portY, false, i);
        }

        // Dibujar puertos de salida
        var outputStartY = nodeRect.Y + NodeHeaderHeight * _zoom + 5 * _zoom;
        for (int i = 0; i < typeDef.OutputPorts.Length; i++)
        {
            var port = typeDef.OutputPorts[i];
            var portY = outputStartY + i * PortSpacing * _zoom;
            DrawPort(dc, node.Id, port, nodeRect.Right, portY, true, i);
        }
    }

    private void DrawPort(DrawingContext dc, string nodeId, NodePort port, double nodeEdgeX, double portY, bool isOutput, int index)
    {
        var portSize = PortSize * _zoom;
        var portX = isOutput ? nodeEdgeX - portSize / 2 : nodeEdgeX - portSize / 2;

        var portRect = new Rect(portX, portY, portSize, portSize);
        _portRects[(nodeId, port.Name, isOutput)] = portRect;

        var brush = port.PortType == PortType.Execution ? PortExecBrush : PortDataBrush;

        if (port.PortType == PortType.Execution)
        {
            // Triángulo para puertos de ejecución
            var triangleGeometry = new StreamGeometry();
            using (var ctx = triangleGeometry.Open())
            {
                if (isOutput)
                {
                    ctx.BeginFigure(new Point(portRect.Left, portRect.Top), true, true);
                    ctx.LineTo(new Point(portRect.Right, portRect.Top + portRect.Height / 2), true, false);
                    ctx.LineTo(new Point(portRect.Left, portRect.Bottom), true, false);
                }
                else
                {
                    ctx.BeginFigure(new Point(portRect.Right, portRect.Top), true, true);
                    ctx.LineTo(new Point(portRect.Left, portRect.Top + portRect.Height / 2), true, false);
                    ctx.LineTo(new Point(portRect.Right, portRect.Bottom), true, false);
                }
            }
            triangleGeometry.Freeze();
            dc.DrawGeometry(brush, null, triangleGeometry);
        }
        else
        {
            // Círculo para puertos de datos
            var center = new Point(portRect.X + portRect.Width / 2, portRect.Y + portRect.Height / 2);
            dc.DrawEllipse(brush, null, center, portSize / 2, portSize / 2);
        }

        // Dibujar etiqueta del puerto
        if (!string.IsNullOrEmpty(port.Label))
        {
            var labelText = new FormattedText(
                port.Label,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                NodeTextTypeface,
                10 * _zoom,
                TextDimBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            var labelX = isOutput
                ? portRect.Left - labelText.Width - 4 * _zoom
                : portRect.Right + 4 * _zoom;
            var labelY = portRect.Top + (portRect.Height - labelText.Height) / 2;

            dc.DrawText(labelText, new Point(labelX, labelY));
        }
    }

    private void DrawSelectionRectangle(DrawingContext dc)
    {
        var rect = new Rect(
            Math.Min(_selectionStartScreen.X, _selectionEndScreen.X),
            Math.Min(_selectionStartScreen.Y, _selectionEndScreen.Y),
            Math.Abs(_selectionEndScreen.X - _selectionStartScreen.X),
            Math.Abs(_selectionEndScreen.Y - _selectionStartScreen.Y));

        dc.DrawRectangle(SelectionRectBrush, SelectionRectPen, rect);
    }

    private void DrawPendingConnection(DrawingContext dc)
    {
        if (string.IsNullOrEmpty(_connectionStartNodeId)) return;

        var startKey = (_connectionStartNodeId, _connectionStartPortName!, _connectionStartIsOutput);
        if (!_portRects.TryGetValue(startKey, out var startRect)) return;

        var startPos = new Point(
            startRect.X + startRect.Width / 2,
            startRect.Y + startRect.Height / 2);

        DrawBezierConnection(dc, startPos, _connectionCurrentMouseScreen, PendingConnectionPen);
    }

    // Helpers para obtener posiciones de puertos
    private Point GetOutputPortScreenPosition(ScriptNode node, NodeTypeDefinition typeDef, int portIndex)
    {
        if (!_nodePositions.TryGetValue(node.Id, out var logicalPos))
            logicalPos = new Point(node.X, node.Y);

        var screenPos = LogicalToScreen(logicalPos);
        var nodeWidth = NodeMinWidth * _zoom;
        var portY = screenPos.Y + NodeHeaderHeight * _zoom + 5 * _zoom + portIndex * PortSpacing * _zoom + PortSize * _zoom / 2;

        return new Point(screenPos.X + nodeWidth, portY);
    }

    private Point GetInputPortScreenPosition(ScriptNode node, NodeTypeDefinition typeDef, int portIndex)
    {
        if (!_nodePositions.TryGetValue(node.Id, out var logicalPos))
            logicalPos = new Point(node.X, node.Y);

        var screenPos = LogicalToScreen(logicalPos);
        var portY = screenPos.Y + NodeHeaderHeight * _zoom + 5 * _zoom + portIndex * PortSpacing * _zoom + PortSize * _zoom / 2;

        return new Point(screenPos.X, portY);
    }
}
