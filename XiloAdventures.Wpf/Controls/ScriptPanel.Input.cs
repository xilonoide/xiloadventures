using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Controls;

public partial class ScriptPanel
{
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var pos = e.GetPosition(this);
        _mouseDownScreen = pos;

        // Verificar si hicimos click en un puerto
        var hitPort = HitTestPort(pos);
        if (hitPort.HasValue)
        {
            var (nodeId, portName, isOutput) = hitPort.Value;

            // Solo iniciar conexión desde puertos de salida
            if (isOutput)
            {
                _isConnecting = true;
                _connectionStartNodeId = nodeId;
                _connectionStartPortName = portName;
                _connectionStartIsOutput = true;
                _connectionCurrentMouseScreen = pos;
                CaptureMouse();
                InvalidateVisual();
                return;
            }
        }

        // Verificar si hicimos click en un nodo
        var hitNodeId = HitTestNode(pos);
        if (!string.IsNullOrEmpty(hitNodeId))
        {
            _mouseDownNodeId = hitNodeId;

            // Modificadores de selección
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                // Toggle selección
                if (_selectedNodeIds.Contains(hitNodeId))
                    _selectedNodeIds.Remove(hitNodeId);
                else
                    _selectedNodeIds.Add(hitNodeId);
            }
            else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                // Añadir a selección
                _selectedNodeIds.Add(hitNodeId);
            }
            else if (!_selectedNodeIds.Contains(hitNodeId))
            {
                // Nueva selección única
                _selectedNodeIds.Clear();
                _selectedNodeIds.Add(hitNodeId);
            }

            // Notificar selección
            var selectedNode = _script?.Nodes.FirstOrDefault(n =>
                string.Equals(n.Id, hitNodeId, StringComparison.OrdinalIgnoreCase));
            if (selectedNode != null && _selectedNodeIds.Count == 1)
            {
                NodeSelected?.Invoke(selectedNode);
            }

            // Preparar para arrastre
            _dragStartMouseScreen = pos;
            foreach (var id in _selectedNodeIds)
            {
                if (_nodePositions.TryGetValue(id, out var nodePos))
                    _dragStartLogicalPositions[id] = nodePos;
            }

            CaptureMouse();
            InvalidateVisual();
            return;
        }

        // Click en área vacía - iniciar selección por rectángulo
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control) &&
            !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            _selectedNodeIds.Clear();
            _selectedConnectionIds.Clear();
            SelectionCleared?.Invoke();
        }

        _isDragSelecting = true;
        _selectionStartScreen = pos;
        _selectionEndScreen = pos;
        CaptureMouse();
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        var pos = e.GetPosition(this);

        if (_isConnecting)
        {
            // Verificar si soltamos sobre un puerto de entrada
            var hitPort = HitTestPort(pos);
            if (hitPort.HasValue)
            {
                var (nodeId, portName, isOutput) = hitPort.Value;
                if (!isOutput && nodeId != _connectionStartNodeId)
                {
                    // Crear conexión
                    CreateConnection(_connectionStartNodeId!, _connectionStartPortName!, nodeId, portName);
                }
            }

            _isConnecting = false;
            _connectionStartNodeId = null;
            _connectionStartPortName = null;
            ReleaseMouseCapture();
            InvalidateVisual();
            return;
        }

        if (_isDraggingNodes)
        {
            // Sincronizar posiciones al script
            SyncPositionsToScript();
            ScriptEdited?.Invoke();

            _isDraggingNodes = false;
            _dragStartLogicalPositions.Clear();
            ReleaseMouseCapture();
            InvalidateVisual();
            return;
        }

        if (_isDragSelecting)
        {
            // Seleccionar nodos dentro del rectángulo
            var selectionRect = new Rect(
                Math.Min(_selectionStartScreen.X, _selectionEndScreen.X),
                Math.Min(_selectionStartScreen.Y, _selectionEndScreen.Y),
                Math.Abs(_selectionEndScreen.X - _selectionStartScreen.X),
                Math.Abs(_selectionEndScreen.Y - _selectionStartScreen.Y));

            foreach (var kvp in _nodeRects)
            {
                if (selectionRect.IntersectsWith(kvp.Value))
                {
                    _selectedNodeIds.Add(kvp.Key);
                }
            }

            _isDragSelecting = false;
            ReleaseMouseCapture();
            InvalidateVisual();
            return;
        }

        // Click simple - verificar si fue en un nodo
        if (_mouseDownNodeId != null)
        {
            var distance = (pos - _mouseDownScreen).Length;
            if (distance < DragThreshold)
            {
                // Fue un click, no un arrastre
                // La selección ya se manejó en MouseDown
            }
        }

        _mouseDownNodeId = null;
        ReleaseMouseCapture();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        Focus();

        var pos = e.GetPosition(this);

        // Click derecho en nodo - mostrar menú contextual
        var hitNodeId = HitTestNode(pos);
        if (!string.IsNullOrEmpty(hitNodeId) && _script != null)
        {
            var node = _script.Nodes.FirstOrDefault(n =>
                string.Equals(n.Id, hitNodeId, StringComparison.OrdinalIgnoreCase));

            if (node != null)
            {
                ShowNodeContextMenu(node, pos);
                e.Handled = true;
                return;
            }
        }

        // Click derecho en conexión para eliminarla
        var hitConnection = HitTestConnection(pos);
        if (!string.IsNullOrEmpty(hitConnection))
        {
            RemoveConnection(hitConnection);
            e.Handled = true;
        }
    }

    private void ShowNodeContextMenu(ScriptNode node, Point position)
    {
        var typeDef = NodeTypeRegistry.GetNodeType(node.NodeType);
        if (typeDef == null) return;

        var menu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Foreground = Brushes.White
        };

        // Opción de eliminar nodo
        var deleteItem = new MenuItem
        {
            Header = "Eliminar nodo",
            Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100))
        };
        deleteItem.Click += (_, _) =>
        {
            _selectedNodeIds.Clear();
            _selectedNodeIds.Add(node.Id);
            RemoveSelectedNodes();
        };
        menu.Items.Add(deleteItem);

        // Si estamos en modo prueba y es una acción, añadir opción de ejecutar
        if (IsTestMode && typeDef.Category == NodeCategory.Action)
        {
            menu.Items.Add(new Separator());

            var executeItem = new MenuItem
            {
                Header = "Ejecutar",
                Foreground = new SolidColorBrush(Color.FromRgb(100, 220, 100)),
                FontWeight = FontWeights.SemiBold
            };
            executeItem.Click += (_, _) => ExecuteActionRequested?.Invoke(node);
            menu.Items.Add(executeItem);
        }

        menu.IsOpen = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var pos = e.GetPosition(this);

        if (_isConnecting)
        {
            _connectionCurrentMouseScreen = pos;
            InvalidateVisual();
            return;
        }

        if (_isPanning && e.MiddleButton == MouseButtonState.Pressed)
        {
            var delta = pos - _lastMiddleDown;
            _offset = new Point(
                _offset.X + delta.X / _zoom,
                _offset.Y + delta.Y / _zoom);
            _lastMiddleDown = pos;
            InvalidateVisual();
            return;
        }

        if (_isDragSelecting && e.LeftButton == MouseButtonState.Pressed)
        {
            _selectionEndScreen = pos;
            InvalidateVisual();
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed && _mouseDownNodeId != null)
        {
            var distance = (pos - _mouseDownScreen).Length;

            if (!_isDraggingNodes && distance > DragThreshold)
            {
                _isDraggingNodes = true;
            }

            if (_isDraggingNodes)
            {
                var deltaScreen = pos - _dragStartMouseScreen;
                var deltaLogical = new Vector(deltaScreen.X / _zoom, deltaScreen.Y / _zoom);

                foreach (var kvp in _dragStartLogicalPositions)
                {
                    var newPos = new Point(
                        kvp.Value.X + deltaLogical.X,
                        kvp.Value.Y + deltaLogical.Y);

                    if (_snapToGrid)
                    {
                        newPos = SnapToGrid(newPos);
                    }

                    _nodePositions[kvp.Key] = newPos;
                }

                InvalidateVisual();
            }
        }
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.ChangedButton == MouseButton.Middle)
        {
            Focus();
            _isPanning = true;
            _lastMiddleDown = e.GetPosition(this);
            CaptureMouse();
            e.Handled = true;
        }
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);

        if (e.ChangedButton == MouseButton.Middle)
        {
            _isPanning = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        var pos = e.GetPosition(this);
        var logicalBefore = ScreenToLogical(pos);

        // Ajustar zoom
        var factor = e.Delta > 0 ? 1.15 : 1.0 / 1.15;
        _zoom *= factor;
        _zoom = Math.Clamp(_zoom, 0.2, 4.0);

        // Mantener el punto bajo el ratón
        var logicalAfter = ScreenToLogical(pos);
        _offset = new Point(
            _offset.X + (logicalAfter.X - logicalBefore.X),
            _offset.Y + (logicalAfter.Y - logicalBefore.Y));

        InvalidateVisual();
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
        base.OnMouseDoubleClick(e);

        if (e.ChangedButton != MouseButton.Left) return;

        var pos = e.GetPosition(this);
        var hitNodeId = HitTestNode(pos);

        if (!string.IsNullOrEmpty(hitNodeId))
        {
            var node = _script?.Nodes.FirstOrDefault(n =>
                string.Equals(n.Id, hitNodeId, StringComparison.OrdinalIgnoreCase));
            if (node != null)
            {
                NodeDoubleClicked?.Invoke(node);
            }
        }
        else
        {
            var logicalPos = ScreenToLogical(pos);
            EmptyAreaDoubleClicked?.Invoke(logicalPos);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // Ignorar hotkeys si el foco está en un TextBox
        if (Keyboard.FocusedElement is TextBox)
            return;

        switch (e.Key)
        {
            case Key.Delete:
                RemoveSelectedNodes();
                e.Handled = true;
                break;

            case Key.A when Keyboard.Modifiers.HasFlag(ModifierKeys.Control):
                // Seleccionar todos los nodos
                if (_script != null)
                {
                    _selectedNodeIds.Clear();
                    foreach (var node in _script.Nodes)
                    {
                        _selectedNodeIds.Add(node.Id);
                    }
                    InvalidateVisual();
                }
                e.Handled = true;
                break;

            case Key.Escape:
                if (_isConnecting)
                {
                    _isConnecting = false;
                    _connectionStartNodeId = null;
                    _connectionStartPortName = null;
                    ReleaseMouseCapture();
                    InvalidateVisual();
                }
                else
                {
                    ClearSelection();
                }
                e.Handled = true;
                break;

            case Key.F:
                CenterView();
                e.Handled = true;
                break;
        }
    }

    // Hit testing
    private string? HitTestNode(Point screenPos)
    {
        foreach (var kvp in _nodeRects)
        {
            if (IsPointInRoundedRect(screenPos, kvp.Value, 6))
                return kvp.Key;
        }
        return null;
    }

    /// <summary>
    /// Comprueba si un punto está dentro de un rectángulo con esquinas redondeadas.
    /// Incluye también puntos ligeramente fuera del borde para mejor usabilidad.
    /// El arco del header puede ser muy alto (aproximadamente Width/2) debido al escalado de WPF.
    /// </summary>
    private static bool IsPointInRoundedRect(Point point, Rect rect, double cornerRadius)
    {
        // El arco del header en WPF se escala automáticamente, creando un semicírculo
        // cuya altura es aproximadamente la mitad del ancho del nodo
        var arcHeight = rect.Width / 2;

        // Expandir el rect para incluir el arco completo del header
        var expandedRect = new Rect(
            rect.X - 8,
            rect.Y - arcHeight,  // El arco puede ser tan alto como la mitad del ancho
            rect.Width + 16,
            rect.Height + arcHeight + 8);

        // Si no está dentro del rectángulo expandido, no está dentro
        if (!expandedRect.Contains(point))
            return false;

        // Si está claramente dentro del rectángulo principal, está dentro
        if (rect.Contains(point))
            return true;

        // Para puntos encima del nodo (media luna/arco del header), verificar si está
        // dentro del área del semicírculo
        if (point.Y < rect.Top)
        {
            // Centro del arco está en el centro horizontal del nodo, en el borde superior
            var centerX = rect.Left + rect.Width / 2;
            var centerY = rect.Top;

            // Radio del semicírculo (aproximadamente la mitad del ancho)
            var radius = rect.Width / 2;

            // Verificar si el punto está dentro del semicírculo
            var dx = point.X - centerX;
            var dy = point.Y - centerY;

            // El punto está dentro si está en el semicírculo superior
            if (dx * dx + dy * dy <= radius * radius && dy <= 0)
                return true;
        }

        // Para puntos en el área expandida lateral o inferior, aceptar
        return true;
    }

    private (string nodeId, string portName, bool isOutput)? HitTestPort(Point screenPos)
    {
        foreach (var kvp in _portRects)
        {
            // Expandir área de hit testing para puertos
            var expandedRect = new Rect(
                kvp.Value.X - 4,
                kvp.Value.Y - 4,
                kvp.Value.Width + 8,
                kvp.Value.Height + 8);

            if (expandedRect.Contains(screenPos))
                return kvp.Key;
        }
        return null;
    }

    private string? HitTestConnection(Point screenPos)
    {
        if (_script == null) return null;

        const double hitDistance = 8;

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

            // Simplificado: verificar distancia al segmento recto (aproximación)
            var distance = DistanceToLineSegment(screenPos, fromPos, toPos);
            if (distance < hitDistance)
            {
                return connection.Id;
            }
        }

        return null;
    }

    private static double DistanceToLineSegment(Point p, Point a, Point b)
    {
        var ab = b - a;
        var ap = p - a;

        var t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / (ab.X * ab.X + ab.Y * ab.Y), 0, 1);
        var closest = new Point(a.X + t * ab.X, a.Y + t * ab.Y);

        return (p - closest).Length;
    }
}
