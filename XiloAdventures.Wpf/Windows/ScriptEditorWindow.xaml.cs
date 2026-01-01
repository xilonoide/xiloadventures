using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Windows;

public partial class ScriptEditorWindow : Window
{
    private readonly WorldModel _world;
    private readonly string _ownerType;
    private readonly string _ownerId;
    private readonly string _ownerName;
    private ScriptDefinition _script;
    private ScriptNode? _selectedNode;

    // Undo/Redo support
    private readonly ScriptUndoRedoManager _undoRedo = new(50);
    private bool _isRestoringSnapshot;

    // Clipboard for copy/paste
    private List<ScriptNode>? _nodesClipboard;
    private List<NodeConnection>? _connectionsClipboard;
    private bool _clipboardIsCut;

    // Para evitar recursión en el selector de scripts
    private bool _isChangingScript;

    // Entidad actualmente seleccionada en el árbol
    private object? _selectedEntity;
    private string _selectedEntityType = "";
    private string _selectedEntityId = "";

    // Modo prueba
    private GameEngine? _testEngine;
    private WorldModel? _testWorld;
    private Action? _onTestStateChanged;

    // Providers para selectores de entidades
    public Func<IEnumerable<Room>>? GetRooms { get; set; }
    public Func<IEnumerable<GameObject>>? GetObjects { get; set; }
    public Func<IEnumerable<Npc>>? GetNpcs { get; set; }
    public Func<IEnumerable<Door>>? GetDoors { get; set; }
    public Func<IEnumerable<QuestDefinition>>? GetQuests { get; set; }
    public Func<IEnumerable<FxAsset>>? GetFxs { get; set; }

    /// <summary>
    /// Configura el modo de prueba con el engine y mundo actuales.
    /// </summary>
    public void SetTestMode(GameEngine testEngine, WorldModel testWorld, Action? onStateChanged = null)
    {
        _testEngine = testEngine;
        _testWorld = testWorld;
        _onTestStateChanged = onStateChanged;

        // Mostrar el panel de pruebas
        TestPanel.Visibility = Visibility.Visible;
        TestPanelColumn.Width = new GridLength(520);

        // Mostrar el botón de play
        PlayScriptButton.Visibility = Visibility.Visible;

        // Habilitar modo prueba en el panel de scripts
        ScriptPanel.IsTestMode = true;
        ScriptPanel.ExecuteActionRequested += ExecuteActionNode;

        UpdateTestDisplay();
    }

    public ScriptEditorWindow(WorldModel world, string ownerType, string ownerId, string ownerName)
    {
        InitializeComponent();

        _world = world;
        _ownerType = ownerType;
        _ownerId = ownerId;
        _ownerName = ownerName;

        Title = $"Editor de Scripts - {ownerName} ({GetOwnerTypeDisplayName(ownerType)})";

        // Buscar script existente o crear uno nuevo
        _script = _world.Scripts.FirstOrDefault(s =>
            s.OwnerType == ownerType && s.OwnerId == ownerId)
            ?? CreateNewScript();

        // Configurar controles
        ScriptNameTextBox.Text = _script.Name;
        ScriptNameTextBox.TextChanged += ScriptNameTextBox_TextChanged;

        NodePalette.SetOwnerType(ownerType, _world.Game);

        ScriptPanel.SetScript(_script, ownerType);
        ScriptPanel.NodeSelected += ScriptPanel_NodeSelected;
        ScriptPanel.SelectionCleared += ScriptPanel_SelectionCleared;
        ScriptPanel.ScriptEdited += ScriptPanel_ScriptEdited;
        ScriptPanel.NodeDoubleClicked += ScriptPanel_NodeDoubleClicked;

        // Estado inicial de toggles
        ToggleGridButton.IsChecked = ScriptPanel.IsGridVisible;
        ToggleSnapButton.IsChecked = ScriptPanel.IsSnapToGridEnabled;

        // Configurar entidad seleccionada inicial
        _selectedEntityType = ownerType;
        _selectedEntityId = ownerId;

        Loaded += ScriptEditorWindow_Loaded;
        Closing += ScriptEditorWindow_Closing;

        // Initial undo snapshot
        PushUndoSnapshot();
    }

    private static string GetOwnerTypeDisplayName(string ownerType)
    {
        return ownerType switch
        {
            "Game" => "Juego",
            "Room" => "Sala",
            "Door" => "Puerta",
            "Npc" => "NPC",
            "GameObject" => "Objeto",
            "Quest" => "Mision",
            "Conversation" => "Conversación",
            _ => ownerType
        };
    }

    private ScriptDefinition CreateNewScript()
    {
        var script = new ScriptDefinition
        {
            Name = $"Script de {_ownerName}",
            OwnerType = _ownerType,
            OwnerId = _ownerId
        };

        _world.Scripts.Add(script);
        return script;
    }

    #region Script Panel Events

    private void ScriptPanel_NodeSelected(ScriptNode node)
    {
        _selectedNode = node;
        UpdatePropertiesPanel();
    }

    private void ScriptPanel_SelectionCleared()
    {
        _selectedNode = null;
        UpdatePropertiesPanel();
    }

    private void ScriptPanel_ScriptEdited()
    {
        if (!_isRestoringSnapshot)
        {
            PushUndoSnapshot();
            UpdateCurrentEntityTreeColor();
            UpdateDiagnosticPanel();
        }
    }

    private void UpdateDiagnosticPanel()
    {
        DiagnosticPanel.Children.Clear();

        // Si no hay nodos, mostrar mensaje vacío
        if (_script.Nodes.Count == 0)
        {
            DiagnosticIcon.Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150));
            AddDiagnosticMessage("Script vacío", Color.FromRgb(150, 150, 150));
            return;
        }

        var validation = ScriptValidator.Validate(_script);

        if (validation.IsValid)
        {
            // Verde - script válido
            DiagnosticIcon.Foreground = new SolidColorBrush(Color.FromRgb(100, 220, 100));
            AddDiagnosticMessage("✓ Script válido", Color.FromRgb(100, 220, 100));
        }
        else
        {
            // Amarillo/Rojo - script con problemas
            DiagnosticIcon.Foreground = new SolidColorBrush(Color.FromRgb(255, 180, 80));

            if (!validation.HasEvent)
            {
                AddDiagnosticMessage("✗ Falta evento", Color.FromRgb(255, 100, 100));
            }
            else
            {
                AddDiagnosticMessage("✓ Tiene evento", Color.FromRgb(100, 220, 100));
            }

            if (!validation.HasAction)
            {
                AddDiagnosticMessage("✗ Falta acción", Color.FromRgb(255, 100, 100));
            }
            else
            {
                AddDiagnosticMessage("✓ Tiene acción", Color.FromRgb(100, 220, 100));
            }

            if (validation.HasEvent && validation.HasAction && !validation.IsConnected)
            {
                AddDiagnosticMessage("✗ Sin conexión", Color.FromRgb(255, 180, 80));
            }
            else if (validation.HasEvent && validation.HasAction)
            {
                AddDiagnosticMessage("✓ Conectado", Color.FromRgb(100, 220, 100));
            }

            // Mostrar nodos con datos incompletos
            if (validation.IncompleteNodes.Count > 0)
            {
                AddDiagnosticMessage("", Color.FromRgb(60, 60, 60)); // Separador
                AddDiagnosticMessage("Datos faltantes:", Color.FromRgb(255, 180, 80));

                foreach (var incomplete in validation.IncompleteNodes)
                {
                    var propsText = string.Join(", ", incomplete.MissingProperties);
                    AddDiagnosticMessage($"  • {incomplete.NodeDisplayName}", Color.FromRgb(255, 140, 100));
                    AddDiagnosticMessage($"     {propsText}", Color.FromRgb(200, 150, 100), 10);
                }
            }
            else
            {
                AddDiagnosticMessage("✓ Datos completos", Color.FromRgb(100, 220, 100));
            }
        }
    }

    private void AddDiagnosticMessage(string message, Color color, int fontSize = 12)
    {
        var text = new TextBlock
        {
            Text = message,
            Foreground = new SolidColorBrush(color),
            FontSize = fontSize,
            Margin = new Thickness(0, 1, 0, 1),
            TextWrapping = TextWrapping.Wrap
        };
        DiagnosticPanel.Children.Add(text);
    }

    private void UpdateCurrentEntityTreeColor()
    {
        var treeItem = FindTreeItemForEntity(_selectedEntityType, _selectedEntityId);
        if (treeItem == null) return;

        treeItem.Foreground = GetEntityScriptColor(_selectedEntityType, _selectedEntityId);
    }

    private TreeViewItem? FindTreeItemForEntity(string entityType, string entityId)
    {
        return FindTreeItemRecursive(EntityTree.Items, entityType, entityId);
    }

    private TreeViewItem? FindTreeItemRecursive(ItemCollection items, string entityType, string entityId)
    {
        foreach (var item in items)
        {
            if (item is TreeViewItem tvi)
            {
                if (tvi.Tag is (string type, string id, object _) &&
                    type == entityType && id == entityId)
                {
                    return tvi;
                }

                // Buscar en hijos
                var found = FindTreeItemRecursive(tvi.Items, entityType, entityId);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void ScriptPanel_NodeDoubleClicked(ScriptNode node)
    {
        _selectedNode = node;
        UpdatePropertiesPanel();
    }

    private void ScriptNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _script.Name = ScriptNameTextBox.Text;
        if (!_isRestoringSnapshot)
        {
            PushUndoSnapshot();
        }
    }

    #endregion

    #region Undo/Redo

    private void PushUndoSnapshot()
    {
        var snapshot = CreateSnapshot();
        _undoRedo.Push(snapshot);
        CommandManager.InvalidateRequerySuggested();
    }

    private ScriptSnapshot CreateSnapshot()
    {
        // Serialize the current script state
        var options = new JsonSerializerOptions { WriteIndented = false };
        var nodesJson = JsonSerializer.Serialize(_script.Nodes, options);
        var connectionsJson = JsonSerializer.Serialize(_script.Connections, options);

        return new ScriptSnapshot
        {
            Name = _script.Name,
            NodesJson = nodesJson,
            ConnectionsJson = connectionsJson
        };
    }

    private void RestoreSnapshot(ScriptSnapshot snapshot)
    {
        _isRestoringSnapshot = true;
        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            _script.Name = snapshot.Name;
            _script.Nodes = JsonSerializer.Deserialize<List<ScriptNode>>(snapshot.NodesJson, options) ?? new();
            _script.Connections = JsonSerializer.Deserialize<List<NodeConnection>>(snapshot.ConnectionsJson, options) ?? new();

            // Normalize properties dictionaries
            foreach (var node in _script.Nodes)
            {
                var normalizedProps = new Dictionary<string, object?>(
                    node.Properties, StringComparer.OrdinalIgnoreCase);
                node.Properties = normalizedProps;
            }

            ScriptNameTextBox.Text = _script.Name;
            ScriptPanel.SetScript(_script, _ownerType);
            _selectedNode = null;
            UpdatePropertiesPanel();
        }
        finally
        {
            _isRestoringSnapshot = false;
        }
    }

    private void Undo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _undoRedo.CanUndo;
        e.Handled = true;
    }

    private void Undo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var snapshot = _undoRedo.Undo();
        if (snapshot != null)
        {
            RestoreSnapshot(snapshot);
        }
    }

    private void Redo_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _undoRedo.CanRedo;
        e.Handled = true;
    }

    private void Redo_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var snapshot = _undoRedo.Redo();
        if (snapshot != null)
        {
            RestoreSnapshot(snapshot);
        }
    }

    #endregion

    #region Cut/Copy/Paste

    private void CutCopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        var selected = ScriptPanel.GetSelectedNodes();
        e.CanExecute = selected != null && selected.Any();
        e.Handled = true;
    }

    private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _nodesClipboard != null && _nodesClipboard.Count > 0;
        e.Handled = true;
    }

    private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selected = ScriptPanel.GetSelectedNodes().ToList();
        if (selected.Count == 0) return;

        CopyNodesToClipboard(selected);
        _clipboardIsCut = true;

        // Remove the cut nodes
        var selectedIds = selected.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        _script.Nodes.RemoveAll(n => selectedIds.Contains(n.Id));
        _script.Connections.RemoveAll(c =>
            selectedIds.Contains(c.FromNodeId) || selectedIds.Contains(c.ToNodeId));

        ScriptPanel.SetScript(_script, _ownerType);
        ScriptPanel.ClearSelection();
        PushUndoSnapshot();
    }

    private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selected = ScriptPanel.GetSelectedNodes().ToList();
        if (selected.Count == 0) return;

        CopyNodesToClipboard(selected);
        _clipboardIsCut = false;
    }

    private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_nodesClipboard == null || _nodesClipboard.Count == 0) return;

        // Create new nodes from clipboard with new IDs
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var newNodes = new List<ScriptNode>();

        foreach (var node in _nodesClipboard)
        {
            var newId = Guid.NewGuid().ToString();
            idMap[node.Id] = newId;

            var newNode = new ScriptNode
            {
                Id = newId,
                NodeType = node.NodeType,
                Category = node.Category,
                X = node.X + 40, // Offset for visibility
                Y = node.Y + 40,
                Comment = node.Comment,
                Properties = new Dictionary<string, object?>(node.Properties, StringComparer.OrdinalIgnoreCase)
            };
            newNodes.Add(newNode);
        }

        // Create new connections
        if (_connectionsClipboard != null)
        {
            foreach (var conn in _connectionsClipboard)
            {
                if (idMap.TryGetValue(conn.FromNodeId, out var newFromId) &&
                    idMap.TryGetValue(conn.ToNodeId, out var newToId))
                {
                    var newConn = new NodeConnection
                    {
                        Id = Guid.NewGuid().ToString(),
                        FromNodeId = newFromId,
                        FromPortName = conn.FromPortName,
                        ToNodeId = newToId,
                        ToPortName = conn.ToPortName
                    };
                    _script.Connections.Add(newConn);
                }
            }
        }

        // Add new nodes to script
        foreach (var node in newNodes)
        {
            _script.Nodes.Add(node);
        }

        // Update the panel and select the new nodes
        ScriptPanel.SetScript(_script, _ownerType);
        ScriptPanel.SelectNodes(newNodes.Select(n => n.Id));
        PushUndoSnapshot();

        // If it was a cut operation, clear the clipboard
        if (_clipboardIsCut)
        {
            _nodesClipboard = null;
            _connectionsClipboard = null;
        }
    }

    private void CopyNodesToClipboard(List<ScriptNode> nodes)
    {
        var options = new JsonSerializerOptions { WriteIndented = false };

        // Clone nodes
        var nodesJson = JsonSerializer.Serialize(nodes, options);
        _nodesClipboard = JsonSerializer.Deserialize<List<ScriptNode>>(nodesJson, options) ?? new();

        // Clone connections between selected nodes
        var nodeIds = nodes.Select(n => n.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var relevantConnections = _script.Connections
            .Where(c => nodeIds.Contains(c.FromNodeId) && nodeIds.Contains(c.ToNodeId))
            .ToList();

        var connJson = JsonSerializer.Serialize(relevantConnections, options);
        _connectionsClipboard = JsonSerializer.Deserialize<List<NodeConnection>>(connJson, options) ?? new();
    }

    #endregion

    #region Grid/Snap Commands

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Ignorar hotkeys sin modificador si el foco está en un control de entrada de texto
        var focused = Keyboard.FocusedElement;
        if (focused is TextBox || focused is PasswordBox || focused is System.Windows.Controls.Primitives.TextBoxBase)
            return;
        if (focused is ComboBox combo && combo.IsEditable)
            return;

        switch (e.Key)
        {
            case Key.G:
                ScriptPanel.ToggleGridVisibility();
                ToggleGridButton.IsChecked = ScriptPanel.IsGridVisible;
                e.Handled = true;
                break;
            case Key.H:
                ScriptPanel.ToggleSnapToGrid();
                ToggleSnapButton.IsChecked = ScriptPanel.IsSnapToGridEnabled;
                e.Handled = true;
                break;
        }
    }

    private void ToggleGridButton_Click(object sender, RoutedEventArgs e)
    {
        ScriptPanel.ToggleGridVisibility();
        ToggleGridButton.IsChecked = ScriptPanel.IsGridVisible;
    }

    private void ToggleSnapButton_Click(object sender, RoutedEventArgs e)
    {
        ScriptPanel.ToggleSnapToGrid();
        ToggleSnapButton.IsChecked = ScriptPanel.IsSnapToGridEnabled;
    }

    private void CenterView_Click(object sender, RoutedEventArgs e)
    {
        ScriptPanel.CenterView();
    }

    #endregion

    #region Properties Panel

    private void UpdatePropertiesPanel()
    {
        PropertiesPanel.Children.Clear();

        if (_selectedNode == null)
        {
            var noSelectionText = new TextBlock
            {
                Text = "Selecciona un nodo para ver sus propiedades",
                Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
                FontStyle = FontStyles.Italic,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            PropertiesPanel.Children.Add(noSelectionText);
            return;
        }

        var typeDef = NodeTypeRegistry.GetNodeType(_selectedNode.NodeType);
        if (typeDef == null) return;

        // Tipo de nodo
        AddPropertyHeader("Tipo");
        AddPropertyValue(typeDef.DisplayName);

        if (!string.IsNullOrEmpty(typeDef.Description))
        {
            AddPropertyDescription(typeDef.Description);
        }

        AddSeparator();

        // Propiedades editables
        if (typeDef.Properties.Length > 0)
        {
            AddPropertyHeader("Propiedades");

            foreach (var propDef in typeDef.Properties)
            {
                AddEditableProperty(propDef);
            }
        }

        // Comentario
        AddSeparator();
        AddPropertyHeader("Comentario");
        AddCommentEditor();
    }

    private void AddPropertyHeader(string text)
    {
        var header = new TextBlock
        {
            Text = text.ToUpper(),
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
            Margin = new Thickness(0, 8, 0, 4)
        };
        PropertiesPanel.Children.Add(header);
    }

    private void AddPropertyValue(string text)
    {
        var value = new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 4)
        };
        PropertiesPanel.Children.Add(value);
    }

    private void AddPropertyDescription(string text)
    {
        var desc = new TextBlock
        {
            Text = text,
            Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
            FontSize = 13,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        PropertiesPanel.Children.Add(desc);
    }

    private void AddSeparator()
    {
        var sep = new Border
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Margin = new Thickness(0, 8, 0, 4)
        };
        PropertiesPanel.Children.Add(sep);
    }

    private void AddEditableProperty(NodePropertyDefinition propDef)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

        var label = new TextBlock
        {
            Text = propDef.DisplayName,
            Foreground = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 2)
        };
        panel.Children.Add(label);

        // Obtener valor actual
        _selectedNode!.Properties.TryGetValue(propDef.Name, out var currentValue);

        UIElement editor;

        if (propDef.DataType == "select" && propDef.Options != null)
        {
            // ComboBox para selecciones con estilo oscuro
            var combo = new ComboBox
            {
                Style = (Style)FindResource("DarkComboBoxStyle"),
                ItemContainerStyle = (Style)FindResource("DarkComboBoxItemStyle")
            };

            foreach (var option in propDef.Options)
            {
                var item = new ComboBoxItem { Content = option, Tag = option, Foreground = Brushes.White };
                combo.Items.Add(item);

                // Seleccionar el valor actual
                var currentStr = currentValue?.ToString();
                if (option == currentStr || (currentStr == null && option == propDef.Options.FirstOrDefault()))
                {
                    combo.SelectedItem = item;
                }
            }

            combo.SelectionChanged += (s, e) =>
            {
                if (combo.SelectedItem is ComboBoxItem selected)
                {
                    _selectedNode.Properties[propDef.Name] = selected.Tag?.ToString();
                    PushUndoSnapshot();
                    ScriptPanel.InvalidateVisual();
                    UpdateDiagnosticPanel();
                    UpdateCurrentEntityTreeColor();
                }
            };

            editor = combo;
        }
        else if (propDef.DataType == "bool")
        {
            // CheckBox para booleanos
            var check = new CheckBox
            {
                IsChecked = currentValue is bool b ? b : (propDef.DefaultValue is bool db && db),
                Foreground = Brushes.White
            };

            check.Checked += (s, e) =>
            {
                _selectedNode.Properties[propDef.Name] = true;
                PushUndoSnapshot();
                UpdateDiagnosticPanel();
                UpdateCurrentEntityTreeColor();
            };

            check.Unchecked += (s, e) =>
            {
                _selectedNode.Properties[propDef.Name] = false;
                PushUndoSnapshot();
                UpdateDiagnosticPanel();
                UpdateCurrentEntityTreeColor();
            };

            // Hacer que la etiqueta también active/desactive el checkbox
            label.Cursor = Cursors.Hand;
            label.MouseLeftButtonDown += (_, _) => check.IsChecked = !check.IsChecked;

            editor = check;
        }
        else if (propDef.DataType == "int")
        {
            // TextBox para enteros
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.DefaultValue?.ToString() ?? "0",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4)
            };

            textBox.LostFocus += (s, e) =>
            {
                if (int.TryParse(textBox.Text, out var intValue))
                {
                    _selectedNode.Properties[propDef.Name] = intValue;
                    PushUndoSnapshot();
                    UpdateDiagnosticPanel();
                    UpdateCurrentEntityTreeColor();
                }
            };

            editor = textBox;
        }
        else if (propDef.DataType == "float")
        {
            // TextBox para flotantes
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.DefaultValue?.ToString() ?? "0",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4)
            };

            textBox.LostFocus += (s, e) =>
            {
                if (float.TryParse(textBox.Text, out var floatValue))
                {
                    _selectedNode.Properties[propDef.Name] = floatValue;
                    PushUndoSnapshot();
                    UpdateDiagnosticPanel();
                    UpdateCurrentEntityTreeColor();
                }
            };

            editor = textBox;
        }
        else if (!string.IsNullOrEmpty(propDef.EntityType))
        {
            // ComboBox para referencias a entidades
            editor = CreateEntitySelector(propDef, currentValue?.ToString());
        }
        else
        {
            // TextBox para strings y otros
            var textBox = new TextBox
            {
                Text = currentValue?.ToString() ?? propDef.DefaultValue?.ToString() ?? "",
                Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4),
                AcceptsReturn = propDef.Name == "Message",
                TextWrapping = propDef.Name == "Message" ? TextWrapping.Wrap : TextWrapping.NoWrap,
                MinHeight = propDef.Name == "Message" ? 60 : 0
            };

            textBox.LostFocus += (s, e) =>
            {
                _selectedNode.Properties[propDef.Name] = textBox.Text;
                PushUndoSnapshot();
                UpdateDiagnosticPanel();
                UpdateCurrentEntityTreeColor();
            };

            editor = textBox;
        }

        panel.Children.Add(editor);
        PropertiesPanel.Children.Add(panel);
    }

    private ComboBox CreateEntitySelector(NodePropertyDefinition propDef, string? currentValue)
    {
        var combo = new ComboBox
        {
            Style = (Style)FindResource("DarkComboBoxStyle"),
            ItemContainerStyle = (Style)FindResource("DarkComboBoxItemStyle")
        };

        combo.Items.Add(new ComboBoxItem { Content = "(Ninguno)", Tag = "", Foreground = Brushes.White });

        IEnumerable<(string Id, string Name)> entities = propDef.EntityType switch
        {
            "Room" => GetRooms?.Invoke()?.Select(r => (r.Id, r.Name)) ?? Enumerable.Empty<(string, string)>(),
            "GameObject" => GetObjects?.Invoke()?.Select(o => (o.Id, o.Name)) ?? Enumerable.Empty<(string, string)>(),
            "Npc" => GetNpcs?.Invoke()?.Select(n => (n.Id, n.Name)) ?? Enumerable.Empty<(string, string)>(),
            "Door" => GetDoors?.Invoke()?.Select(d => (d.Id, d.Name)) ?? Enumerable.Empty<(string, string)>(),
            "Quest" => GetQuests?.Invoke()?.Select(q => (q.Id, q.Name)) ?? Enumerable.Empty<(string, string)>(),
            "Fx" => GetFxs?.Invoke()?.Select(f => (f.Id, f.Id)) ?? Enumerable.Empty<(string, string)>(),
            _ => Enumerable.Empty<(string, string)>()
        };

        foreach (var (id, name) in entities)
        {
            combo.Items.Add(new ComboBoxItem { Content = name, Tag = id, Foreground = Brushes.White });
        }

        // Seleccionar valor actual
        foreach (ComboBoxItem item in combo.Items)
        {
            if (item.Tag?.ToString() == currentValue)
            {
                combo.SelectedItem = item;
                break;
            }
        }

        if (combo.SelectedItem == null)
            combo.SelectedIndex = 0;

        combo.SelectionChanged += (s, e) =>
        {
            if (combo.SelectedItem is ComboBoxItem selected)
            {
                _selectedNode!.Properties[propDef.Name] = selected.Tag?.ToString();
                PushUndoSnapshot();
                UpdateDiagnosticPanel();
                UpdateCurrentEntityTreeColor();
            }
        };

        return combo;
    }

    private void AddCommentEditor()
    {
        var textBox = new TextBox
        {
            Text = _selectedNode?.Comment ?? "",
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 4, 6, 4),
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            MinHeight = 40
        };

        textBox.LostFocus += (s, e) =>
        {
            if (_selectedNode != null)
            {
                _selectedNode.Comment = string.IsNullOrWhiteSpace(textBox.Text) ? null : textBox.Text;
                PushUndoSnapshot();
            }
        };

        PropertiesPanel.Children.Add(textBox);
    }

    #endregion

    private void ScriptEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Construir el árbol de entidades
        BuildEntityTree();

        // Poblar el selector de scripts
        PopulateScriptSelector();

        // Actualizar panel de diagnóstico
        UpdateDiagnosticPanel();

        // Centrar la vista automáticamente al abrir
        ScriptPanel.CenterView();
    }

    #region Entity Tree

    private void BuildEntityTree()
    {
        EntityTree.Items.Clear();
        TreeViewItem? nodeToSelect = null;

        // Nodo Juego
        var gameId = _world.Game?.Id ?? "game";
        var gameNode = new TreeViewItem
        {
            Header = "Juego",
            Tag = ("Game", gameId, _world.Game),
            Foreground = GetEntityScriptColor("Game", gameId)
        };
        EntityTree.Items.Add(gameNode);

        // Seleccionar si es la entidad inicial
        if (_selectedEntityType == "Game")
        {
            nodeToSelect = gameNode;
            _selectedEntity = _world.Game;
        }

        // Orden: Juego, Misiones, NPCs, Conversaciones, Salas, Objetos

        // Nodo Misiones
        var questsRoot = new TreeViewItem { Header = $"Misiones ({_world.Quests.Count})", Foreground = Brushes.White };
        foreach (var quest in _world.Quests.OrderBy(q => q.Name))
        {
            var questNode = new TreeViewItem
            {
                Header = quest.Name,
                Tag = ("Quest", quest.Id, quest),
                Foreground = GetEntityScriptColor("Quest", quest.Id)
            };
            questsRoot.Items.Add(questNode);

            if (_selectedEntityType == "Quest" && _selectedEntityId == quest.Id)
            {
                nodeToSelect = questNode;
                _selectedEntity = quest;
                questsRoot.IsExpanded = true;
            }
        }
        EntityTree.Items.Add(questsRoot);

        // Nodo NPCs
        var npcsRoot = new TreeViewItem { Header = $"NPCs ({_world.Npcs.Count})", Foreground = Brushes.White };
        foreach (var npc in _world.Npcs.OrderBy(n => n.Name))
        {
            var npcNode = new TreeViewItem
            {
                Header = npc.Name,
                Tag = ("Npc", npc.Id, npc),
                Foreground = GetEntityScriptColor("Npc", npc.Id)
            };
            npcsRoot.Items.Add(npcNode);

            if (_selectedEntityType == "Npc" && _selectedEntityId == npc.Id)
            {
                nodeToSelect = npcNode;
                _selectedEntity = npc;
                npcsRoot.IsExpanded = true;
            }
        }
        EntityTree.Items.Add(npcsRoot);

        // Nodo Salas
        var roomsRoot = new TreeViewItem { Header = $"Salas ({_world.Rooms.Count})", Foreground = Brushes.White };
        foreach (var room in _world.Rooms.OrderBy(r => r.Name))
        {
            var roomNode = new TreeViewItem
            {
                Header = room.Name,
                Tag = ("Room", room.Id, room),
                Foreground = GetEntityScriptColor("Room", room.Id)
            };

            // Añadir puertas como hijas
            if (_world.Doors != null)
            {
                foreach (var door in _world.Doors.Where(d => d.RoomIdA == room.Id || d.RoomIdB == room.Id))
                {
                    var doorNode = new TreeViewItem
                    {
                        Header = $"🚪 {door.Name}",
                        Tag = ("Door", door.Id, door),
                        Foreground = GetEntityScriptColor("Door", door.Id)
                    };
                    roomNode.Items.Add(doorNode);

                    if (_selectedEntityType == "Door" && _selectedEntityId == door.Id)
                    {
                        nodeToSelect = doorNode;
                        _selectedEntity = door;
                        roomNode.IsExpanded = true;
                        roomsRoot.IsExpanded = true;
                    }
                }
            }

            roomsRoot.Items.Add(roomNode);

            if (_selectedEntityType == "Room" && _selectedEntityId == room.Id)
            {
                nodeToSelect = roomNode;
                _selectedEntity = room;
                roomsRoot.IsExpanded = true;
            }
        }
        EntityTree.Items.Add(roomsRoot);

        // Nodo Objetos
        var objectsRoot = new TreeViewItem { Header = $"Objetos ({_world.Objects.Count})", Foreground = Brushes.White };
        foreach (var obj in _world.Objects.OrderBy(o => o.Name))
        {
            var objNode = new TreeViewItem
            {
                Header = obj.Name,
                Tag = ("GameObject", obj.Id, obj),
                Foreground = GetEntityScriptColor("GameObject", obj.Id)
            };
            objectsRoot.Items.Add(objNode);

            if (_selectedEntityType == "GameObject" && _selectedEntityId == obj.Id)
            {
                nodeToSelect = objNode;
                _selectedEntity = obj;
                objectsRoot.IsExpanded = true;
            }
        }
        EntityTree.Items.Add(objectsRoot);

        // Seleccionar y dar foco al nodo después de que el layout se actualice
        if (nodeToSelect != null)
        {
            var node = nodeToSelect;
            Dispatcher.InvokeAsync(() =>
            {
                node.IsSelected = true;
                node.Focus();
                node.BringIntoView();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private bool HasScripts(string ownerType, string ownerId)
    {
        return _world.Scripts.Any(s =>
            s.OwnerType == ownerType &&
            s.OwnerId == ownerId &&
            (s.Nodes.Count > 0 || s.Connections.Count > 0));
    }

    /// <summary>
    /// Obtiene el color del texto en el árbol según el estado de validación de los scripts
    /// Blanco = sin scripts, Amarillo = script incompleto, Verde = script válido
    /// </summary>
    private Brush GetEntityScriptColor(string ownerType, string ownerId)
    {
        var entityScripts = _world.Scripts
            .Where(s => s.OwnerType == ownerType &&
                        s.OwnerId == ownerId &&
                        (s.Nodes.Count > 0 || s.Connections.Count > 0))
            .ToList();

        if (entityScripts.Count == 0)
        {
            return Brushes.White; // Sin scripts
        }

        // Verificar si al menos un script es válido
        var hasValidScript = entityScripts.Any(s => ScriptValidator.Validate(s).IsValid);

        if (hasValidScript)
        {
            return new SolidColorBrush(Color.FromRgb(100, 220, 100)); // Verde - válido
        }
        else
        {
            return new SolidColorBrush(Color.FromRgb(255, 220, 100)); // Amarillo - incompleto
        }
    }

    private void EntityTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (EntityTree.SelectedItem is not TreeViewItem item) return;
        if (item.Tag is not (string entityType, string entityId, object entity)) return;

        // Guardar el script actual antes de cambiar
        SaveCurrentScript();

        // Actualizar la entidad seleccionada
        _selectedEntity = entity;
        _selectedEntityType = entityType;
        _selectedEntityId = entityId;

        // Actualizar la paleta de nodos según el tipo de entidad
        NodePalette.SetOwnerType(entityType, _world.Game);

        // Buscar o crear el primer script de esta entidad
        var entityScript = _world.Scripts.FirstOrDefault(s =>
            s.OwnerType == entityType && s.OwnerId == entityId);

        if (entityScript == null)
        {
            // Crear un nuevo script para esta entidad
            var entityName = GetEntityName(entity);
            entityScript = new ScriptDefinition
            {
                Name = $"Script de {entityName}",
                OwnerType = entityType,
                OwnerId = entityId
            };
            _world.Scripts.Add(entityScript);
        }

        // Cambiar al script de la entidad
        SwitchToScript(entityScript);
    }

    private void EntityTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Solo mostrar menú contextual en modo prueba
        if (_testEngine == null) return;

        // Encontrar el TreeViewItem bajo el cursor
        var hit = e.OriginalSource as DependencyObject;
        while (hit != null && hit is not TreeViewItem)
        {
            hit = VisualTreeHelper.GetParent(hit);
        }

        if (hit is not TreeViewItem item) return;

        // Verificar que sea un GameObject
        if (item.Tag is not (string entityType, string _, object entity)) return;
        if (entityType != "GameObject" || entity is not GameObject gameObj) return;

        // Seleccionar el item
        item.IsSelected = true;

        // Crear menú contextual con estilo oscuro
        var menu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Foreground = Brushes.White
        };

        var addToInventoryItem = new MenuItem
        {
            Header = "Enviar a inventario",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 220, 100)),
            FontWeight = FontWeights.SemiBold
        };
        addToInventoryItem.Click += (_, _) =>
        {
            if (!_testEngine.State.InventoryObjectIds.Contains(gameObj.Id))
            {
                _testEngine.State.InventoryObjectIds.Add(gameObj.Id);
                AppendTestOutput($"+ {gameObj.Name} añadido al inventario", Color.FromRgb(100, 220, 100));
                UpdateTestDisplay();
            }
            else
            {
                AppendTestOutput($"Ya tienes {gameObj.Name} en el inventario", Colors.Yellow);
            }
        };
        menu.Items.Add(addToInventoryItem);

        menu.IsOpen = true;
        e.Handled = true;
    }

    private static string GetEntityName(object entity)
    {
        return entity switch
        {
            GameInfo game => game.Title ?? "Juego",
            Room room => room.Name ?? room.Id,
            Door door => door.Name ?? door.Id,
            Npc npc => npc.Name ?? npc.Id,
            GameObject obj => obj.Name ?? obj.Id,
            QuestDefinition quest => quest.Name ?? quest.Id,
            _ => "Entidad"
        };
    }

    private void ExpandAllEntities_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in EntityTree.Items.OfType<TreeViewItem>())
        {
            SetEntityExpandedRecursive(item, true);
        }
    }

    private void CollapseAllEntities_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in EntityTree.Items.OfType<TreeViewItem>())
        {
            SetEntityExpandedRecursive(item, false);
        }
    }

    private void SetEntityExpandedRecursive(TreeViewItem item, bool expanded)
    {
        item.IsExpanded = expanded;
        foreach (var child in item.Items.OfType<TreeViewItem>())
        {
            SetEntityExpandedRecursive(child, expanded);
        }
    }

    private void EntitySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PerformEntitySearch(EntitySearchTextBox.Text);
    }

    private TreeViewItem? FindInEntityTree(TreeViewItem node, string text)
    {
        if (node.Header is string s && s.ToLowerInvariant().Contains(text))
            return node;

        foreach (TreeViewItem child in node.Items.OfType<TreeViewItem>())
        {
            var found = FindInEntityTree(child, text);
            if (found != null)
                return found;
        }

        return null;
    }

    private void PerformEntitySearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var normalized = text.ToLowerInvariant();

        foreach (TreeViewItem root in EntityTree.Items)
        {
            var found = FindInEntityTree(root, normalized);
            if (found != null)
            {
                found.IsSelected = true;
                found.BringIntoView();
                break;
            }
        }
    }

    #endregion

    #region New/Delete Script

    private void NewScript_Click(object sender, RoutedEventArgs e)
    {
        // Guardar el script actual
        SaveCurrentScript();

        // Usar la entidad actualmente seleccionada
        var entityName = _selectedEntity != null ? GetEntityName(_selectedEntity) : _ownerName;

        // Contar scripts existentes para esta entidad
        var existingCount = _world.Scripts
            .Count(s => s.OwnerType == _selectedEntityType && s.OwnerId == _selectedEntityId);

        // Crear nuevo script
        var newScript = new ScriptDefinition
        {
            Name = $"Script {existingCount + 1} de {entityName}",
            OwnerType = _selectedEntityType,
            OwnerId = _selectedEntityId
        };

        _world.Scripts.Add(newScript);

        // Cambiar al nuevo script
        SwitchToScript(newScript);
    }

    private void DeleteScript_Click(object sender, RoutedEventArgs e)
    {
        // Contar scripts de esta entidad
        var entityScripts = _world.Scripts
            .Where(s => s.OwnerType == _selectedEntityType && s.OwnerId == _selectedEntityId)
            .ToList();

        if (entityScripts.Count <= 1 && (_script.Nodes.Count > 0 || _script.Connections.Count > 0))
        {
            DarkConfirmDialog.Show(
                "No se puede eliminar",
                "No se puede eliminar el único script de esta entidad si contiene nodos.",
                this);
            return;
        }

        if (!DarkConfirmDialog.Show(
            "Eliminar Script",
            $"¿Seguro que deseas eliminar el script \"{_script.Name}\"?",
            this))
        {
            return;
        }

        // Encontrar otro script al que cambiar
        var otherScript = entityScripts.FirstOrDefault(s => s.Id != _script.Id);

        // Eliminar el script actual
        _world.Scripts.Remove(_script);

        if (otherScript != null)
        {
            // Cambiar al otro script de la misma entidad
            SwitchToScript(otherScript);
        }
        else
        {
            // Crear un nuevo script vacío si se eliminó el último
            var entityName = _selectedEntity != null ? GetEntityName(_selectedEntity) : _ownerName;
            var newScript = new ScriptDefinition
            {
                Name = $"Script de {entityName}",
                OwnerType = _selectedEntityType,
                OwnerId = _selectedEntityId
            };
            _world.Scripts.Add(newScript);
            SwitchToScript(newScript);
        }
    }

    #endregion

    #region Script Selector

    private void PopulateScriptSelector()
    {
        _isChangingScript = true;
        try
        {
            ScriptSelectorCombo.Items.Clear();

            // Solo scripts de la entidad seleccionada (excepto el actual)
            var entityScripts = _world.Scripts
                .Where(s => s.Id != _script.Id &&
                            s.OwnerType == _selectedEntityType &&
                            s.OwnerId == _selectedEntityId)
                .OrderBy(s => s.Name)
                .ToList();

            foreach (var script in entityScripts)
            {
                var item = new ComboBoxItem
                {
                    Content = script.Name,
                    Tag = script,
                    Foreground = Brushes.White
                };
                ScriptSelectorCombo.Items.Add(item);
            }

            // Si no hay otros scripts, mostrar mensaje
            if (ScriptSelectorCombo.Items.Count == 0)
            {
                var emptyItem = new ComboBoxItem
                {
                    Content = "(No hay otros scripts)",
                    IsEnabled = false,
                    Foreground = new SolidColorBrush(Color.FromRgb(120, 120, 120)),
                    FontStyle = FontStyles.Italic
                };
                ScriptSelectorCombo.Items.Add(emptyItem);
            }

            // Actualizar estado del botón eliminar
            UpdateDeleteButtonState();
        }
        finally
        {
            _isChangingScript = false;
        }
    }

    private void UpdateDeleteButtonState()
    {
        // Solo permitir eliminar si hay más de un script para esta entidad
        // o si el script está vacío
        var entityScriptCount = _world.Scripts
            .Count(s => s.OwnerType == _script.OwnerType && s.OwnerId == _script.OwnerId);
        DeleteScriptButton.IsEnabled = entityScriptCount > 1 ||
            (_script.Nodes.Count == 0 && _script.Connections.Count == 0);
    }

    private static int GetOwnerTypeSortOrder(string ownerType)
    {
        return ownerType switch
        {
            "Game" => 0,
            "Room" => 1,
            "Door" => 2,
            "Npc" => 3,
            "GameObject" => 4,
            "Quest" => 5,
            _ => 99
        };
    }

    private string GetOwnerName(string ownerType, string ownerId)
    {
        return ownerType switch
        {
            "Game" => _world.Game?.Title ?? "Juego",
            "Room" => _world.Rooms?.FirstOrDefault(r => r.Id == ownerId)?.Name ?? ownerId,
            "Door" => _world.Doors?.FirstOrDefault(d => d.Id == ownerId)?.Name ?? ownerId,
            "Npc" => _world.Npcs?.FirstOrDefault(n => n.Id == ownerId)?.Name ?? ownerId,
            "GameObject" => _world.Objects?.FirstOrDefault(o => o.Id == ownerId)?.Name ?? ownerId,
            "Quest" => _world.Quests?.FirstOrDefault(q => q.Id == ownerId)?.Name ?? ownerId,
            "Conversation" => _world.Conversations?.FirstOrDefault(c => c.Id == ownerId)?.Name ?? ownerId,
            _ => ownerId
        };
    }

    private void ScriptSelectorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isChangingScript) return;
        if (ScriptSelectorCombo.SelectedItem is not ComboBoxItem item) return;
        if (item.Tag is not ScriptDefinition selectedScript) return;
        if (selectedScript.Id == _script.Id) return;

        // Guardar el script actual antes de cambiar
        SaveCurrentScript();

        // Cambiar al script seleccionado
        SwitchToScript(selectedScript);
    }

    private void SaveCurrentScript()
    {
        // Sincronizar posiciones
        ScriptPanel.SyncPositionsToScript();

        // Si el nombre está vacío, usar nombre por defecto
        if (string.IsNullOrWhiteSpace(_script.Name))
        {
            _script.Name = "Script nuevo";
        }

        // Si estamos editando una conversación, sincronizar de vuelta
        if (_script.OwnerType == "Conversation")
        {
            var conversation = _world.Conversations.FirstOrDefault(c =>
                string.Equals(c.Id, _script.OwnerId, StringComparison.OrdinalIgnoreCase));

            if (conversation != null)
            {
                conversation.Name = _script.Name;
                // Los nodos y conexiones ya están vinculados por referencia
                // pero actualizamos el StartNodeId si hay un nodo de inicio
                var startNode = conversation.Nodes.FirstOrDefault(n =>
                    n.NodeType == NodeTypeId.Conversation_Start);
                conversation.StartNodeId = startNode?.Id;
            }
        }
    }

    private void SwitchToScript(ScriptDefinition newScript)
    {
        _script = newScript;

        // Actualizar UI
        _isRestoringSnapshot = true;
        try
        {
            ScriptNameTextBox.Text = _script.Name;
        }
        finally
        {
            _isRestoringSnapshot = false;
        }

        // Actualizar paleta de nodos para el nuevo tipo de propietario
        NodePalette.SetOwnerType(_script.OwnerType, _world.Game);

        // Cargar el script en el panel
        ScriptPanel.SetScript(_script, _script.OwnerType);
        _selectedNode = null;
        UpdatePropertiesPanel();

        // Reset undo/redo para el nuevo script
        _undoRedo.Clear();
        PushUndoSnapshot();

        // Actualizar título
        var ownerName = GetOwnerName(_script.OwnerType, _script.OwnerId);
        Title = $"Editor de Scripts - {ownerName} ({GetOwnerTypeDisplayName(_script.OwnerType)})";

        // Actualizar el selector
        PopulateScriptSelector();

        // Actualizar panel de diagnóstico
        UpdateDiagnosticPanel();

        // Centrar vista
        ScriptPanel.CenterView();
    }

    #endregion

    private void ScriptEditorWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        // Sincronizar posiciones antes de cerrar
        ScriptPanel.SyncPositionsToScript();

        // Si el script está vacío, eliminarlo
        if (_script.Nodes.Count == 0 && _script.Connections.Count == 0)
        {
            _world.Scripts.Remove(_script);
            return;
        }

        // Validar el script
        var validation = ScriptValidator.Validate(_script);

        if (validation.HasErrors)
        {
            var errorMessages = string.Join("\n\n", validation.Errors.Select(e => $"• {e}"));

            var shouldSave = DarkConfirmDialog.Show(
                "Script incompleto",
                $"El script tiene los siguientes problemas:\n\n{errorMessages}\n\n¿Deseas guardar de todas formas?",
                this);

            if (!shouldSave)
            {
                e.Cancel = true;
            }
        }
    }

    #region Test Mode

    private void UpdateTestDisplay()
    {
        if (_testEngine == null || _testWorld == null) return;

        var state = _testEngine.State;
        var room = _testEngine.CurrentRoom;

        // Título
        TestRoomTitle.Text = room?.Name ?? "Sala desconocida";

        // Descripción
        TestRoomDescription.Text = room?.Description ?? "";

        // Imagen de sala
        TestRoomImage.Source = null;
        if (room != null && !string.IsNullOrEmpty(room.ImageBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(room.ImageBase64);
                using var ms = new MemoryStream(bytes);
                var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                TestRoomImage.Source = bitmap;
            }
            catch { }
        }

        // Stats del jugador
        var playerStats = state.Player;
        var statsLines = new List<string>
        {
            $"Fuerza: {playerStats.Strength}",
            $"Constitución: {playerStats.Constitution}",
            $"Inteligencia: {playerStats.Intelligence}",
            $"Destreza: {playerStats.Dexterity}",
            $"Carisma: {playerStats.Charisma}"
        };
        TestStatsLabel.Text = string.Join("\n", statsLines);

        // Hora del juego
        var gameTime = state.GameTime;
        if (gameTime != default)
        {
            TestTimeLabel.Text = $"{state.TimeOfDay}, {gameTime:HH:mm}";
        }
        else
        {
            TestTimeLabel.Text = state.TimeOfDay;
        }

        // Dinero
        TestMoneyLabel.Text = state.Player.Money.ToString("N0");

        // Equipo
        var rightHand = !string.IsNullOrEmpty(state.Player.EquippedRightHandId)
            ? _testWorld.Objects.FirstOrDefault(o => o.Id.Equals(state.Player.EquippedRightHandId, StringComparison.OrdinalIgnoreCase))
            : null;
        var leftHand = !string.IsNullOrEmpty(state.Player.EquippedLeftHandId)
            ? _testWorld.Objects.FirstOrDefault(o => o.Id.Equals(state.Player.EquippedLeftHandId, StringComparison.OrdinalIgnoreCase))
            : null;
        var torso = !string.IsNullOrEmpty(state.Player.EquippedTorsoId)
            ? _testWorld.Objects.FirstOrDefault(o => o.Id.Equals(state.Player.EquippedTorsoId, StringComparison.OrdinalIgnoreCase))
            : null;

        var equipLines = new List<string>
        {
            $"Mano derecha: {(rightHand != null ? rightHand.Name : "-")}",
            $"Mano izquierda: {(leftHand != null ? leftHand.Name : "-")}",
            $"Torso: {(torso != null ? torso.Name : "-")}"
        };
        TestEquipmentLabel.Text = string.Join("\n", equipLines);

        // Inventario
        if (state.InventoryObjectIds.Count == 0)
        {
            TestInventoryLabel.Text = "(vacío)";
        }
        else
        {
            var items = state.InventoryObjectIds
                .Select(id => _testWorld.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)))
                .Where(o => o != null)
                .Select(o => o!.Name)
                .ToList();
            TestInventoryLabel.Text = items.Count > 0 ? string.Join(", ", items) : "(vacío)";
        }

        // El botón de play siempre está habilitado en modo prueba
        // La validación se hace al hacer clic
        PlayScriptButton.IsEnabled = true;
    }

    private void PlayScript_Click(object sender, RoutedEventArgs e)
    {
        if (_testEngine == null || _testWorld == null) return;

        // Verificar que hay al menos un evento
        var hasEvent = _script.Nodes.Any(n => n.Category == NodeCategory.Event);
        if (!hasEvent)
        {
            AppendTestOutput("✗ El script necesita al menos un evento para poder ejecutarse.", Colors.Red);
            return;
        }

        // Sincronizar posiciones antes de ejecutar
        ScriptPanel.SyncPositionsToScript();

        try
        {
            // Ejecutar el script usando el ScriptRunner del engine
            AppendTestOutput($"▶ Ejecutando script: {_script.Name}", Color.FromRgb(100, 200, 255));

            var result = _testEngine.ExecuteScript(_script);

            if (result.Success)
            {
                if (result.Messages.Count > 0)
                {
                    foreach (var msg in result.Messages)
                    {
                        AppendTestOutput(msg, Colors.White);
                    }
                }
                else
                {
                    AppendTestOutput("✓ Script ejecutado (sin mensajes)", Color.FromRgb(100, 220, 100));
                }
            }
            else
            {
                AppendTestOutput($"✗ Error: {result.ErrorMessage}", Colors.Red);
            }

            // Actualizar el display y notificar al editor principal
            UpdateTestDisplay();
            _onTestStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            AppendTestOutput($"✗ Error al ejecutar: {ex.Message}", Colors.Red);
        }
    }

    private void TestInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (_testEngine == null) return;

        var command = TestInputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(command)) return;

        TestInputTextBox.Clear();

        // Comando cls
        if (command.Equals("cls", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            TestOutputTextBox.Document.Blocks.Clear();
            return;
        }

        // Mostrar el comando
        AppendTestOutput($"> {command}", Color.FromRgb(150, 150, 150));

        // Procesar el comando
        var result = _testEngine.ProcessCommand(command);
        if (!string.IsNullOrEmpty(result.Message))
        {
            AppendTestOutput(result.Message, Colors.White);
        }

        UpdateTestDisplay();
        _onTestStateChanged?.Invoke();

        e.Handled = true;
    }

    private void AppendTestOutput(string text, Color color)
    {
        var paragraph = new Paragraph(new Run(text)
        {
            Foreground = new SolidColorBrush(color)
        })
        {
            Margin = new Thickness(0, 2, 0, 2)
        };

        TestOutputTextBox.Document.Blocks.Add(paragraph);
        TestOutputTextBox.ScrollToEnd();
    }

    private void ExecuteActionNode(ScriptNode node)
    {
        if (_testEngine == null || _testWorld == null) return;

        var typeDef = NodeTypeRegistry.GetNodeType(node.NodeType);
        if (typeDef == null)
        {
            AppendTestOutput($"✗ Tipo de nodo desconocido: {node.NodeType}", Colors.Red);
            return;
        }

        // Verificar propiedades obligatorias
        var missingProps = new List<string>();
        if (typeDef.Properties != null)
        {
            foreach (var propDef in typeDef.Properties)
            {
                if (!propDef.RequiresValue) continue;

                node.Properties.TryGetValue(propDef.Name, out var value);
                bool isMissing = value == null ||
                                 (value is string strVal && string.IsNullOrWhiteSpace(strVal));

                if (isMissing)
                {
                    missingProps.Add(propDef.DisplayName);
                }
            }
        }

        if (missingProps.Count > 0)
        {
            AppendTestOutput($"✗ Faltan parámetros: {string.Join(", ", missingProps)}", Colors.Red);
            return;
        }

        try
        {
            AppendTestOutput($"▶ Ejecutando: {typeDef.DisplayName}", Color.FromRgb(100, 200, 255));

            // Ejecutar la acción directamente usando el motor de scripts
            var result = _testEngine.ExecuteSingleAction(node);

            if (result.Success)
            {
                if (result.Messages.Count > 0)
                {
                    foreach (var msg in result.Messages)
                    {
                        AppendTestOutput(msg, Colors.White);
                    }
                }
                else
                {
                    AppendTestOutput("✓ Acción ejecutada", Color.FromRgb(100, 220, 100));
                }
            }
            else
            {
                AppendTestOutput($"✗ Error: {result.ErrorMessage}", Colors.Red);
            }

            UpdateTestDisplay();
            _onTestStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            AppendTestOutput($"✗ Error al ejecutar: {ex.Message}", Colors.Red);
        }
    }

    #endregion
}

/// <summary>
/// Snapshot of script state for undo/redo
/// </summary>
public class ScriptSnapshot
{
    public string Name { get; set; } = "";
    public string NodesJson { get; set; } = "";
    public string ConnectionsJson { get; set; } = "";
}

/// <summary>
/// Simple undo/redo manager for scripts
/// </summary>
public class ScriptUndoRedoManager
{
    private readonly List<ScriptSnapshot> _undoStack = new();
    private readonly List<ScriptSnapshot> _redoStack = new();
    private readonly int _maxSize;

    public ScriptUndoRedoManager(int maxSize = 50)
    {
        _maxSize = maxSize;
    }

    public bool CanUndo => _undoStack.Count > 1;
    public bool CanRedo => _redoStack.Count > 0;

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
    }

    public void Push(ScriptSnapshot snapshot)
    {
        _undoStack.Add(snapshot);
        _redoStack.Clear();

        while (_undoStack.Count > _maxSize)
        {
            _undoStack.RemoveAt(0);
        }
    }

    public ScriptSnapshot? Undo()
    {
        if (_undoStack.Count <= 1) return null;

        var current = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        _redoStack.Add(current);

        return _undoStack[^1];
    }

    public ScriptSnapshot? Redo()
    {
        if (_redoStack.Count == 0) return null;

        var snapshot = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        _undoStack.Add(snapshot);

        return snapshot;
    }
}
