using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Controls;

/// <summary>
/// Control visual para editar scripts de nodos tipo Blueprints.
/// </summary>
public partial class ScriptPanel : Control
{
    // Constantes de tamaño
    public const double NodeMinWidth = 180;
    public const double NodeMinHeight = 60;
    public const double NodeHeaderHeight = 24;
    public const double PortSize = 12;
    public const double PortSpacing = 22;
    public const double GridCellSize = 20;

    private ScriptDefinition? _script;
    private string _ownerType = string.Empty;

    // Estado del grid y snap-to-grid
    private bool _showGrid = true;
    private bool _snapToGrid = true;

    // Posiciones lógicas de nodos (coordenadas de mapa, no píxeles)
    private readonly Dictionary<string, Point> _nodePositions = new();
    // Rectángulos en pantalla para hit testing
    private readonly Dictionary<string, Rect> _nodeRects = new();
    // Rectángulos de puertos (nodeId, portName, isOutput)
    private readonly Dictionary<(string nodeId, string portName, bool isOutput), Rect> _portRects = new();

    // Selección
    private readonly HashSet<string> _selectedNodeIds = new();
    private readonly HashSet<string> _selectedConnectionIds = new();

    // Transformación de vista
    private double _zoom = 1.0;
    private Point _offset = new(0, 0);

    // Pan con botón central
    private bool _isPanning;
    private Point _lastMiddleDown;

    // Arrastre de nodos
    private bool _isDraggingNodes;
    private Point _dragStartMouseScreen;
    private readonly Dictionary<string, Point> _dragStartLogicalPositions = new();

    // Selección por rectángulo
    private bool _isDragSelecting;
    private Point _selectionStartScreen;
    private Point _selectionEndScreen;

    // Creación de conexiones
    private bool _isConnecting;
    private string? _connectionStartNodeId;
    private string? _connectionStartPortName;
    private bool _connectionStartIsOutput;
    private Point _connectionCurrentMouseScreen;

    // Para distinguir click de arrastre
    private Point _mouseDownScreen;
    private string? _mouseDownNodeId;
    private const double DragThreshold = 5;

    // Eventos
    public event Action<ScriptNode>? NodeSelected;
    public event Action<ScriptNode>? NodeDoubleClicked;
    public event Action? SelectionCleared;
    public event Action? ScriptEdited;
    public event Action<Point>? EmptyAreaDoubleClicked;
    public event Action<ScriptNode>? ExecuteActionRequested;

    /// <summary>
    /// Indica si estamos en modo prueba (habilita opciones adicionales como ejecutar acciones)
    /// </summary>
    public bool IsTestMode { get; set; }

    // Colores por categoría
    public static readonly Dictionary<NodeCategory, Color> CategoryColors = new()
    {
        { NodeCategory.Event, Color.FromRgb(60, 120, 60) },      // Verde
        { NodeCategory.Condition, Color.FromRgb(180, 150, 40) }, // Amarillo
        { NodeCategory.Action, Color.FromRgb(60, 100, 160) },    // Azul
        { NodeCategory.Flow, Color.FromRgb(80, 80, 80) },        // Gris
        { NodeCategory.Variable, Color.FromRgb(180, 100, 40) },  // Naranja
        { NodeCategory.Dialogue, Color.FromRgb(140, 80, 180) }   // Morado
    };

    static ScriptPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(ScriptPanel),
            new FrameworkPropertyMetadata(typeof(ScriptPanel)));

        // Congelar pens de renderizado (definidos en ScriptPanel.Rendering.cs)
        GridPen.Freeze();
        GridMajorPen.Freeze();
        NodeBorderPen.Freeze();
        NodeSelectedBorderPen.Freeze();
        ConnectionPen.Freeze();
        ConnectionSelectedPen.Freeze();
        SelectionRectPen.Freeze();
        PendingConnectionPen.Freeze();
    }

    public ScriptPanel()
    {
        Focusable = true;
        Background = Brushes.Transparent;
        AllowDrop = true;

        DragEnter += ScriptPanel_DragEnter;
        DragOver += ScriptPanel_DragOver;
        Drop += ScriptPanel_Drop;
    }

    public ScriptDefinition? Script => _script;
    public string OwnerType => _ownerType;
    public bool IsGridVisible => _showGrid;
    public bool IsSnapToGridEnabled => _snapToGrid;

    public void SetScript(ScriptDefinition? script, string ownerType)
    {
        _script = script;
        _ownerType = ownerType;
        _nodePositions.Clear();
        _selectedNodeIds.Clear();
        _selectedConnectionIds.Clear();

        if (_script != null)
        {
            foreach (var node in _script.Nodes)
            {
                _nodePositions[node.Id] = new Point(node.X, node.Y);
            }
        }

        InvalidateVisual();
    }

    public void SetGridVisibility(bool visible)
    {
        _showGrid = visible;
        InvalidateVisual();
    }

    public void SetSnapToGrid(bool enabled)
    {
        _snapToGrid = enabled;
    }

    public void ToggleGridVisibility()
    {
        _showGrid = !_showGrid;
        InvalidateVisual();
    }

    public void ToggleSnapToGrid()
    {
        _snapToGrid = !_snapToGrid;
    }

    public IEnumerable<ScriptNode> GetSelectedNodes()
    {
        if (_script == null) return Enumerable.Empty<ScriptNode>();
        return _script.Nodes.Where(n => _selectedNodeIds.Contains(n.Id));
    }

    public void SelectNode(ScriptNode node)
    {
        _selectedNodeIds.Clear();
        _selectedNodeIds.Add(node.Id);
        InvalidateVisual();
        NodeSelected?.Invoke(node);
    }

    public void SelectNodes(IEnumerable<string> nodeIds)
    {
        _selectedNodeIds.Clear();
        foreach (var id in nodeIds)
        {
            _selectedNodeIds.Add(id);
        }
        InvalidateVisual();
    }

    public void ClearSelection()
    {
        _selectedNodeIds.Clear();
        _selectedConnectionIds.Clear();
        InvalidateVisual();
        SelectionCleared?.Invoke();
    }

    public void AddNode(ScriptNode node, Point? position = null)
    {
        if (_script == null) return;

        if (position.HasValue)
        {
            node.X = position.Value.X;
            node.Y = position.Value.Y;
        }

        if (_snapToGrid)
        {
            var snapped = SnapToGrid(new Point(node.X, node.Y));
            node.X = snapped.X;
            node.Y = snapped.Y;
        }

        _script.Nodes.Add(node);
        _nodePositions[node.Id] = new Point(node.X, node.Y);
        InvalidateVisual();
        ScriptEdited?.Invoke();
    }

    public void RemoveSelectedNodes()
    {
        if (_script == null) return;

        var nodesToRemove = _script.Nodes.Where(n => _selectedNodeIds.Contains(n.Id)).ToList();
        foreach (var node in nodesToRemove)
        {
            // Eliminar conexiones asociadas
            _script.Connections.RemoveAll(c =>
                string.Equals(c.FromNodeId, node.Id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(c.ToNodeId, node.Id, StringComparison.OrdinalIgnoreCase));
            _script.Nodes.Remove(node);
            _nodePositions.Remove(node.Id);
        }

        _selectedNodeIds.Clear();
        InvalidateVisual();
        ScriptEdited?.Invoke();
        SelectionCleared?.Invoke();
    }

    public void CreateConnection(string fromNodeId, string fromPortName, string toNodeId, string toPortName)
    {
        if (_script == null) return;

        // Verificar que no exista ya
        var existing = _script.Connections.FirstOrDefault(c =>
            string.Equals(c.FromNodeId, fromNodeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.FromPortName, fromPortName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ToNodeId, toNodeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ToPortName, toPortName, StringComparison.OrdinalIgnoreCase));

        if (existing != null) return;

        // Obtener tipos de puertos para verificar compatibilidad
        var fromNode = _script.Nodes.FirstOrDefault(n =>
            string.Equals(n.Id, fromNodeId, StringComparison.OrdinalIgnoreCase));
        var toNode = _script.Nodes.FirstOrDefault(n =>
            string.Equals(n.Id, toNodeId, StringComparison.OrdinalIgnoreCase));
        if (fromNode == null || toNode == null) return;

        var fromTypeDef = NodeTypeRegistry.GetNodeType(fromNode.NodeType);
        var toTypeDef = NodeTypeRegistry.GetNodeType(toNode.NodeType);
        if (fromTypeDef == null || toTypeDef == null) return;

        var fromPort = fromTypeDef.OutputPorts.FirstOrDefault(p =>
            string.Equals(p.Name, fromPortName, StringComparison.OrdinalIgnoreCase));
        var toPort = toTypeDef.InputPorts.FirstOrDefault(p =>
            string.Equals(p.Name, toPortName, StringComparison.OrdinalIgnoreCase));
        if (fromPort == null || toPort == null) return;

        // Verificar compatibilidad de tipos
        if (fromPort.PortType != toPort.PortType) return;
        if (fromPort.PortType == PortType.Data && fromPort.DataType != toPort.DataType) return;

        // Eliminar conexiones existentes al puerto de entrada (solo una conexión por entrada)
        _script.Connections.RemoveAll(c =>
            string.Equals(c.ToNodeId, toNodeId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(c.ToPortName, toPortName, StringComparison.OrdinalIgnoreCase));

        var connection = new NodeConnection
        {
            FromNodeId = fromNodeId,
            FromPortName = fromPortName,
            ToNodeId = toNodeId,
            ToPortName = toPortName
        };

        _script.Connections.Add(connection);
        InvalidateVisual();
        ScriptEdited?.Invoke();
    }

    public void RemoveConnection(string connectionId)
    {
        if (_script == null) return;

        _script.Connections.RemoveAll(c => c.Id == connectionId);
        _selectedConnectionIds.Remove(connectionId);
        InvalidateVisual();
        ScriptEdited?.Invoke();
    }

    public void CenterView()
    {
        if (_script == null || !_script.Nodes.Any()) return;

        var minX = _script.Nodes.Min(n => n.X);
        var minY = _script.Nodes.Min(n => n.Y);
        var maxX = _script.Nodes.Max(n => n.X) + NodeMinWidth;
        var maxY = _script.Nodes.Max(n => n.Y) + NodeMinHeight;

        var centerX = (minX + maxX) / 2;
        var centerY = (minY + maxY) / 2;

        _offset = new Point(
            ActualWidth / 2 / _zoom - centerX,
            ActualHeight / 2 / _zoom - centerY);

        InvalidateVisual();
    }

    public void ZoomIn()
    {
        _zoom = Math.Min(_zoom * 1.2, 4.0);
        InvalidateVisual();
    }

    public void ZoomOut()
    {
        _zoom = Math.Max(_zoom / 1.2, 0.2);
        InvalidateVisual();
    }

    // Transformaciones de coordenadas
    public Point LogicalToScreen(Point logical)
    {
        return new Point(
            (logical.X + _offset.X) * _zoom,
            (logical.Y + _offset.Y) * _zoom);
    }

    public Point ScreenToLogical(Point screen)
    {
        return new Point(
            screen.X / _zoom - _offset.X,
            screen.Y / _zoom - _offset.Y);
    }

    private Point SnapToGrid(Point position)
    {
        var cellX = Math.Round(position.X / GridCellSize) * GridCellSize;
        var cellY = Math.Round(position.Y / GridCellSize) * GridCellSize;
        return new Point(cellX, cellY);
    }

    // Calcular altura del nodo basada en número de puertos
    public static double CalculateNodeHeight(NodeTypeDefinition typeDef)
    {
        var inputCount = typeDef.InputPorts.Length;
        var outputCount = typeDef.OutputPorts.Length;
        var maxPorts = Math.Max(inputCount, outputCount);
        var portsHeight = maxPorts * PortSpacing;
        return Math.Max(NodeMinHeight, NodeHeaderHeight + portsHeight + 10);
    }

    // Drag & Drop handlers
    private void ScriptPanel_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("NodeType"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ScriptPanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("NodeType"))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void ScriptPanel_Drop(object sender, DragEventArgs e)
    {
        if (_script == null) return;

        if (e.Data.GetDataPresent("NodeType"))
        {
            var nodeTypeStr = e.Data.GetData("NodeType") as string;
            if (string.IsNullOrEmpty(nodeTypeStr)) return;
            if (!Enum.TryParse<NodeTypeId>(nodeTypeStr, true, out var nodeType)) return;

            var typeDef = NodeTypeRegistry.GetNodeType(nodeType);
            if (typeDef == null) return;

            var screenPos = e.GetPosition(this);
            var logicalPos = ScreenToLogical(screenPos);

            var node = new ScriptNode
            {
                NodeType = nodeType,
                Category = typeDef.Category,
                X = logicalPos.X,
                Y = logicalPos.Y
            };

            // Inicializar propiedades con valores por defecto
            foreach (var prop in typeDef.Properties)
            {
                node.Properties[prop.Name] = prop.DefaultValue;
            }

            AddNode(node);

            // Seleccionar el nuevo nodo
            SelectNode(node);
        }

        e.Handled = true;
    }

    // Sincronizar posiciones de vuelta al script
    public void SyncPositionsToScript()
    {
        if (_script == null) return;

        foreach (var node in _script.Nodes)
        {
            if (_nodePositions.TryGetValue(node.Id, out var pos))
            {
                node.X = pos.X;
                node.Y = pos.Y;
            }
        }
    }
}
