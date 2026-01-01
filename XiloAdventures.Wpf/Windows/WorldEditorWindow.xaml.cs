using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Common.Services;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Windows;
using XiloAdventures.Wpf.Controls;

namespace XiloAdventures.Wpf.Windows;

public partial class WorldEditorWindow : Window
{
    public static RoutedCommand ToggleGridCommand = new RoutedCommand();
    public static RoutedCommand ToggleSnapCommand = new RoutedCommand();
    public static RoutedCommand PlayCommand = new RoutedCommand();
    public static RoutedCommand ScriptEditorCommand = new RoutedCommand();

    private WorldModel _world = new();
    private string? _currentPath;
    private List<Room>? _roomsClipboard;
    private bool _roomsClipboardIsCut;
    private IReadOnlyDictionary<string, Point>? _roomsClipboardPositions;
    private Dictionary<string, string>? _lastClipboardIdMap;

    private readonly UndoRedoManager _undoRedo = new();

    // Búsqueda de propiedades
    private TextBlock? _highlightedPropertyLabel;
    private Brush? _highlightedPropertyOriginalForeground;
    private System.Windows.Threading.DispatcherTimer? _highlightTimer;

    private bool _isPlayRunning;
    private System.Windows.Threading.DispatcherTimer? _autoSyncTimer;
    private System.Windows.Threading.DispatcherTimer? _testNpcMovementTimer;

    // Panel de prueba integrado
    private GameEngine? _testEngine;
    private WorldModel? _testWorld;
    private SoundManager? _testSoundManager;
    private bool _testAiEnabled;
    private readonly List<string> _testCommandHistory = new();
    private int _testHistoryIndex = -1;
    private bool _isDirty;
    private bool _skipNextAutoSync;
    private readonly string _baseTitle;
    private TreeViewItem? _gameTreeNode;
    private string? _initialWorldPath;
    private bool _useLlmForGenders;
    private string? _selectedZoneOriginalName; // Para rastrear cambios de nombre de zona
    public bool IsCanceled { get; private set; }

    private static readonly HttpClient _httpClient = new()
    {
        BaseAddress = new Uri("http://localhost:11434/"),
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly HttpClient _sdHttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:7860/"),
        Timeout = TimeSpan.FromMinutes(10) // Generación de imágenes puede tardar mucho
    };

    public WorldEditorWindow()
    {
        InitializeComponent();
        _baseTitle = Title ?? "Editor de mundos";
        PropertyEditor.PropertyEdited += PropertyEditor_PropertyEdited;
        PropertyEditor.RequestDeleteObject += PropertyEditor_RequestDeleteObject;
        PropertyEditor.RequestAiImageGeneration += PropertyEditor_RequestAiImageGeneration;
        PropertyEditor.RequestAiDescriptionGeneration += PropertyEditor_RequestAiDescriptionGeneration;
        PropertyEditor.RequestEditPatrolRoute += PropertyEditor_RequestEditPatrolRoute;
        PropertyEditor.RequestManageAbilities += PropertyEditor_RequestManageAbilities;
        PropertyEditor.GetRooms = () => _world.Rooms;
        PropertyEditor.GetZones = () => WorldLoader.GetZones(_world);
        PropertyEditor.GetMusics = () => _world.Musics;
        PropertyEditor.GetObjects = () => _world.Objects;
        PropertyEditor.GetAbilities = () => _world.Abilities;
        PropertyEditor.GetQuests = () => _world.Quests;
        PropertyEditor.GetNpcs = () => _world.Npcs;
        PropertyEditor.GetPlayerDefinition = () => _world.Player;
        PropertyEditor.GetGameInfo = () => _world.Game;
        PropertyEditor.GetParserDictionary = () => _world.Game.ParserDictionaryJson;
        PropertyEditor.SetParserDictionary = json =>
        {
            _world.Game.ParserDictionaryJson = json;
            SetDirty(true);
        };
        MapPanel.RoomClicked += MapPanel_RoomClicked;
        MapPanel.MapEdited += MapPanel_MapEdited;
        MapPanel.DoorCreated += MapPanel_DoorCreated;
        MapPanel.DoorDoubleClicked += MapPanel_DoorDoubleClicked;
        MapPanel.DoorClicked += MapPanel_DoorClicked;
        MapPanel.KeyIconClicked += MapPanel_KeyIconClicked;
        MapPanel.SelectionCleared += MapPanel_SelectionCleared;

        MapPanel.AddObjectToRoomRequested += MapPanel_AddObjectToRoomRequested;
        MapPanel.AddNpcToRoomRequested += MapPanel_AddNpcToRoomRequested;
        MapPanel.EmptyMapDoubleClicked += MapPanel_EmptyMapDoubleClicked;
        MapPanel.RoomsDeleteRequested += MapPanel_RoomsDeleteRequested;
        MapPanel.PatrolRouteEdited += MapPanel_PatrolRouteEdited;
        MapPanel.TeleportToRoomRequested += MapPanel_TeleportToRoomRequested;

        Loaded += Window_Loaded;
    }

    public WorldEditorWindow(string? worldPath) : this()
    {
        _initialWorldPath = worldPath;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        LoadingOverlay.Visibility = Visibility.Visible;
        await Dispatcher.InvokeAsync(() => { }, DispatcherPriority.Render);

        bool isNewWorld = false;
        Room? startRoom = null;

        try
        {
            if (!string.IsNullOrWhiteSpace(_initialWorldPath) && System.IO.File.Exists(_initialWorldPath))
            {
                TryLoadWorldWithPrompt(_initialWorldPath);
                if (IsCanceled)
                {
                    Close();
                    return;
                }
            }
            else
            {
                _world = new WorldModel();
                _world.Game.Id = Guid.NewGuid().ToString();
                _world.Game.Title = "Nuevo mundo";
                _world.Game.StartRoomId = "sala_inicio";
                _world.ShowGrid = true; // Grid activado por defecto en mundos nuevos

                _world.Rooms.Clear();
                startRoom = new Room
                {
                    Id = "sala_inicio",
                    Name = "Sala inicial",
                    Description = "Esta es la sala inicial de tu mundo.",
                    Zone = "Inicial"
                };
                _world.Rooms.Add(startRoom);

                // Posición ajustada al grid (centro de la primera celda: 80, 45)
                _world.RoomPositions["sala_inicio"] = new MapPosition { X = 80, Y = 45 };

                _currentPath = null;
                isNewWorld = true;
            }

            MapPanel.SetWorld(_world);
            BuildTree();
            UpdateZoneFilter();
            ResetUndoRedo();
            SetDirty(false);

            // Restaurar estado del grid y snap-to-grid desde el mundo
            MapPanel.SetGridVisibility(_world.ShowGrid);
            MapPanel.SetSnapToGrid(_world.SnapToGrid);

            // Sincronizar estado visual de los ToggleButtons
            ToggleGridButton.IsChecked = _world.ShowGrid;
            ToggleSnapButton.IsChecked = _world.SnapToGrid;

            // Inicializar estado de IA
            _useLlmForGenders = _world.UseLlmForGenders;
            PropertyEditor.IsAiEnabled = _useLlmForGenders;

            // Si la IA estaba activada en el mundo, iniciar Docker silenciosamente
            if (_useLlmForGenders)
            {
                await EnsureDockerStartedForAiAsync();
            }

            // Centrar en la sala inicial si es un mundo nuevo
            if (isNewWorld && startRoom != null)
            {
                MapPanel.CenterOnRoom(startRoom);
            }

            // Centrar en la sala inicial si se cargó un mundo existente
            if (!string.IsNullOrWhiteSpace(_initialWorldPath) && System.IO.File.Exists(_initialWorldPath))
            {
                startRoom = _world.Rooms.FirstOrDefault(r => r.Id == _world.Game.StartRoomId);
                if (startRoom != null)
                {
                    MapPanel.CenterOnRoom(startRoom);
                }
            }
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
        }
    }

    private void TryLoadWorldWithPrompt(string worldPath)
    {
        try
        {
            _world = WorldLoader.LoadWorldModel(worldPath);
            _currentPath = worldPath;
        }
        catch (Exception ex)
        {
            DarkErrorDialog.Show("Error al cargar el mundo", ex.Message, this);
            IsCanceled = true;
        }
    }

    private void PropertyEditor_PropertyEdited(object? obj, string propertyName)
    {
        if (obj is null) return;

        // Si se ha editado el nombre o IsContainer, actualizamos el nodo correspondiente en el árbol
        if (propertyName == "Name" || propertyName == "IsContainer")
        {
            foreach (TreeViewItem root in WorldTree.Items)
            {
                UpdateTreeHeaderRecursive(root, obj);
            }
        }

        // Si se cambian las salas de una puerta, reconstruir el árbol
        if (obj is Door && (propertyName == "RoomIdA" || propertyName == "RoomIdB"))
        {
            BuildTree();
        }

        // Si se cambia IsOpen o IsLocked de una puerta, redibujar el mapa
        if (obj is Door && (propertyName == "IsOpen" || propertyName == "IsLocked"))
        {
            MapPanel.InvalidateVisual();
        }

        // Si se cambia el nombre de una zona, actualizar todas las salas de esa zona
        if (obj is ZoneNodeTag zoneTag && propertyName == "ZoneName")
        {
            var oldName = _selectedZoneOriginalName;
            var newName = zoneTag.ZoneName;

            if (!string.IsNullOrEmpty(oldName) && oldName != newName)
            {
                // Actualizar todas las salas que pertenecían a la zona antigua
                foreach (var room in _world.Rooms.Where(r => r.Zone == oldName))
                {
                    room.Zone = newName;
                }

                // Actualizar el nombre original guardado
                _selectedZoneOriginalName = newName;

                // Actualizar solo el header del nodo seleccionado (sin reconstruir el árbol)
                if (WorldTree.SelectedItem is TreeViewItem selectedItem && selectedItem.Tag == zoneTag)
                {
                    selectedItem.Header = $"🗺️ {newName}";
                }

                // Actualizar el filtro de zonas sin cambiar la selección actual
                UpdateZoneFilterItems();
            }
        }

        // Si se cambia la fuente por defecto, aplicarla directamente al modo prueba sin recargar
        if (obj is GameInfo gameInfo && propertyName == "DefaultFontFamily")
        {
            if (TestPanel.Visibility == Visibility.Visible && _testEngine != null)
            {
                var fontFamily = new FontFamily(gameInfo.DefaultFontFamily);
                TestOutputTextBox.FontFamily = fontFamily;
                TestInputTextBox.FontFamily = fontFamily;
                TestRoomTitle.FontFamily = fontFamily;
                TestRoomDescription.FontFamily = fontFamily;
                _skipNextAutoSync = true;
            }
        }

        PushUndoSnapshot();
        SetDirty(true);
    }

    private void UpdateTreeHeaderRecursive(TreeViewItem item, object target)
    {
        if (item.Tag == target)
        {
            string headerText = item.Header?.ToString() ?? string.Empty;

            switch (target)
            {
                case Room r:
                    headerText = r.Name;
                    break;
                case GameObject o:
                    // Incluir icono 📦 si es contenedor
                    headerText = o.IsContainer ? $"📦 {o.Name}" : o.Name;
                    break;
                case Npc n:
                    headerText = n.Name;
                    break;
                case QuestDefinition q:
                    headerText = q.Name;
                    break;
                case Door d:
                    headerText = d.Name;
                    break;
            }

            item.Header = headerText;
        }

        foreach (var child in item.Items.OfType<TreeViewItem>())
        {
            UpdateTreeHeaderRecursive(child, target);
        }
    }

    private void MapPanel_RoomClicked(Room room)
    {
        // seleccionar en el árbol la sala correspondiente
        SelectRoomInTree(room);
        MapPanel.SetSelectedRoom(room);
    }

    private void MapPanel_SelectionCleared()
    {
        ClearTreeSelection();
        PropertyEditor.SetObject(null);
    }

    private void MapPanel_MapEdited()
    {
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void MapPanel_DoorCreated(Door door, GameObject? createdObject)
    {
        AddDoorToTreeNode(door);

        if (createdObject != null)
            AddObjectToTreeNode(createdObject);

        SelectDoorInTree(door);
        PropertyEditor.SetObject(door);
        SetDirty(true);
    }

    private void MapPanel_DoorDoubleClicked(Door door)
    {
        SelectDoorInTree(door);
        PropertyEditor.SetObject(door);
    }

    private void MapPanel_DoorClicked(Door door)
    {
        SelectDoorInTree(door);
        PropertyEditor.SetObject(door);
    }

    private void MapPanel_KeyIconClicked(GameObject keyObj)
    {
        SelectObjectInTree(keyObj);
        PropertyEditor.SetObject(keyObj);
    }

    private void BuildTree()
    {
        // Guardar el estado expandido de los nodos antes de reconstruir
        var expandedNodes = new HashSet<string>();
        var expandedFolderIds = new HashSet<string>();
        CollectExpandedNodes(WorldTree.Items, expandedNodes, expandedFolderIds);

        WorldTree.Items.Clear();
        _gameTreeNode = null;

        _world.Doors ??= new List<Door>();
        _world.Folders ??= new List<EditorFolder>();

        var gameNode = new TreeViewItem { Header = "Juego", Tag = _world.Game, Foreground = Brushes.White };
        _gameTreeNode = gameNode;

        // Nodo Jugador como hijo de Juego
        _world.Player ??= new PlayerDefinition();
        var playerNode = new TreeViewItem { Header = "Jugador", Tag = _world.Player, Foreground = Brushes.White };

        // Añadir subnodo Objetos para el jugador si tiene objetos asignados
        AddAssignedObjectsNode(playerNode, GetPlayerAssignedObjectIds());

        gameNode.Items.Add(playerNode);
        WorldTree.Items.Add(gameNode);

        // Orden: Juego, Misiones, Conversaciones, NPCs, Salas, Objetos
        var questsRoot = new TreeViewItem { Header = $"Misiones ({_world.Quests.Count})", Foreground = Brushes.White };
        BuildFolderTree(questsRoot, EditorFolderType.Quests, null, expandedFolderIds);
        var questIdsInFolders = GetItemIdsInFolders(EditorFolderType.Quests);
        foreach (var q in _world.Quests.Where(q => !questIdsInFolders.Contains(q.Id)))
        {
            questsRoot.Items.Add(new TreeViewItem { Header = q.Name, Tag = q, Foreground = Brushes.White });
        }
        WorldTree.Items.Add(questsRoot);

        var npcsRoot = new TreeViewItem { Header = $"NPCs ({_world.Npcs.Count})", Foreground = Brushes.White };
        BuildFolderTree(npcsRoot, EditorFolderType.Npcs, null, expandedFolderIds);
        var npcIdsInFolders = GetItemIdsInFolders(EditorFolderType.Npcs);
        foreach (var npc in _world.Npcs.Where(n => !npcIdsInFolders.Contains(n.Id)))
        {
            var npcNode = new TreeViewItem { Header = npc.Name, Tag = npc, Foreground = Brushes.White };
            AddAssignedObjectsNode(npcNode, GetNpcAssignedObjectIds(npc));
            npcsRoot.Items.Add(npcNode);
        }
        WorldTree.Items.Add(npcsRoot);

        var roomsRoot = new TreeViewItem { Header = $"Salas ({_world.Rooms.Count})", Foreground = Brushes.White };
        BuildZoneTree(roomsRoot, expandedNodes);
        WorldTree.Items.Add(roomsRoot);

        var objsRoot = new TreeViewItem { Header = $"Objetos ({_world.Objects.Count})", Foreground = Brushes.White };
        BuildFolderTree(objsRoot, EditorFolderType.Objects, null, expandedFolderIds);
        var objIdsInFolders = GetItemIdsInFolders(EditorFolderType.Objects);
        foreach (var obj in _world.Objects.Where(o => !objIdsInFolders.Contains(o.Id)))
        {
            if (!IsObjectContainedInAnother(obj))
            {
                BuildObjectTreeRecursive(objsRoot, obj);
            }
        }
        WorldTree.Items.Add(objsRoot);

        // Restaurar el estado expandido de los nodos
        RestoreExpandedNodes(WorldTree.Items, expandedNodes, expandedFolderIds);
    }

    private void CollectExpandedNodes(ItemCollection items, HashSet<string> expandedNodes, HashSet<string> expandedFolderIds)
    {
        foreach (TreeViewItem item in items.OfType<TreeViewItem>())
        {
            if (item.IsExpanded)
            {
                if (item.Tag is EditorFolder folder)
                    expandedFolderIds.Add(folder.Id);
                else if (item.Tag is ZoneNodeTag zone)
                    expandedNodes.Add($"Zone:{zone.ZoneName}");
                else if (item.Header is string header)
                {
                    var baseName = header.Contains(" (") ? header[..header.IndexOf(" (")] : header;
                    expandedNodes.Add(baseName);
                }
            }
            CollectExpandedNodes(item.Items, expandedNodes, expandedFolderIds);
        }
    }

    private void RestoreExpandedNodes(ItemCollection items, HashSet<string> expandedNodes, HashSet<string> expandedFolderIds)
    {
        foreach (TreeViewItem item in items.OfType<TreeViewItem>())
        {
            if (item.Tag is EditorFolder folder && expandedFolderIds.Contains(folder.Id))
                item.IsExpanded = true;
            else if (item.Tag is ZoneNodeTag zone && expandedNodes.Contains($"Zone:{zone.ZoneName}"))
                item.IsExpanded = true;
            else if (item.Header is string header)
            {
                var baseName = header.Contains(" (") ? header[..header.IndexOf(" (")] : header;
                if (expandedNodes.Contains(baseName))
                    item.IsExpanded = true;
            }
            RestoreExpandedNodes(item.Items, expandedNodes, expandedFolderIds);
        }
    }

    /// <summary>
    /// Clase auxiliar para identificar nodos de zona en el árbol.
    /// </summary>
    private class ZoneNodeTag
    {
        public string ZoneName { get; set; } = string.Empty;
    }

    /// <summary>
    /// StyleSelector que aplica estilos solo a MenuItems, no a Separators.
    /// </summary>
    private class MenuItemStyleSelector : StyleSelector
    {
        public Style? MenuItemStyle { get; set; }

        public override Style? SelectStyle(object item, DependencyObject container)
        {
            if (container is MenuItem)
                return MenuItemStyle;
            return null;
        }
    }

    private void BuildZoneTree(TreeViewItem parent, HashSet<string> expandedNodes)
    {
        // Obtener todas las zonas únicas
        var zones = _world.Rooms
            .Select(r => r.Zone ?? "")
            .Where(z => !string.IsNullOrEmpty(z))
            .Distinct()
            .OrderBy(z => z == "Inicial" ? 0 : 1) // "Inicial" primero
            .ThenBy(z => z)
            .ToList();

        // Si no hay zonas, crear una zona por defecto
        if (zones.Count == 0)
        {
            zones.Add("Inicial");
        }

        foreach (var zoneName in zones)
        {
            var roomsInZone = _world.Rooms.Where(r => r.Zone == zoneName).ToList();
            var zoneNode = new TreeViewItem
            {
                Header = $"🗺️ {zoneName} ({roomsInZone.Count})",
                Tag = new ZoneNodeTag { ZoneName = zoneName },
                Foreground = Brushes.White,
                IsExpanded = expandedNodes.Contains($"Zone:{zoneName}")
            };

            foreach (var room in roomsInZone.OrderBy(r => r.Name))
            {
                var roomNode = BuildRoomNode(room);
                zoneNode.Items.Add(roomNode);
            }

            parent.Items.Add(zoneNode);
        }

        // Salas sin zona (no debería haber, pero por si acaso)
        var roomsWithoutZone = _world.Rooms.Where(r => string.IsNullOrEmpty(r.Zone)).ToList();
        if (roomsWithoutZone.Any())
        {
            var noZoneNode = new TreeViewItem
            {
                Header = $"⚠️ Sin zona ({roomsWithoutZone.Count})",
                Tag = new ZoneNodeTag { ZoneName = "" },
                Foreground = Brushes.Orange,
                IsExpanded = true
            };

            foreach (var room in roomsWithoutZone.OrderBy(r => r.Name))
            {
                var roomNode = BuildRoomNode(room);
                noZoneNode.Items.Add(roomNode);
            }

            parent.Items.Add(noZoneNode);
        }
    }

    private void BuildFolderTree(TreeViewItem parent, EditorFolderType folderType, string? parentFolderId, HashSet<string> expandedFolderIds)
    {
        var folders = _world.Folders
            .Where(f => f.FolderType == folderType && f.ParentFolderId == parentFolderId)
            .OrderBy(f => f.Name)
            .ToList();

        foreach (var folder in folders)
        {
            var folderNode = new TreeViewItem
            {
                Header = $"📁 {folder.Name}",
                Tag = folder,
                Foreground = Brushes.White
            };

            // Añadir subcarpetas recursivamente
            BuildFolderTree(folderNode, folderType, folder.Id, expandedFolderIds);

            // Añadir items de la carpeta
            foreach (var itemId in folder.ItemIds)
            {
                var itemNode = CreateItemNodeById(folderType, itemId);
                if (itemNode != null)
                    folderNode.Items.Add(itemNode);
            }

            if (expandedFolderIds.Contains(folder.Id))
                folderNode.IsExpanded = true;

            parent.Items.Insert(0, folderNode); // Carpetas al inicio
        }
    }

    private TreeViewItem? CreateItemNodeById(EditorFolderType folderType, string itemId)
    {
        return folderType switch
        {
            EditorFolderType.Objects => _world.Objects.FirstOrDefault(o => o.Id == itemId) is GameObject obj
                ? CreateObjectNode(obj) : null,
            EditorFolderType.Npcs => _world.Npcs.FirstOrDefault(n => n.Id == itemId) is Npc npc
                ? CreateNpcNode(npc) : null,
            EditorFolderType.Rooms => _world.Rooms.FirstOrDefault(r => r.Id == itemId) is Room room
                ? BuildRoomNode(room) : null,
            EditorFolderType.Quests => _world.Quests.FirstOrDefault(q => q.Id == itemId) is QuestDefinition quest
                ? new TreeViewItem { Header = quest.Name, Tag = quest, Foreground = Brushes.White } : null,
            _ => null
        };
    }

    private TreeViewItem CreateObjectNode(GameObject obj)
    {
        var node = new TreeViewItem { Header = obj.Name, Tag = obj, Foreground = Brushes.White };
        // Añadir objetos contenidos recursivamente
        if (obj.IsContainer)
        {
            foreach (var containedId in obj.ContainedObjectIds)
            {
                var contained = _world.Objects.FirstOrDefault(o => o.Id == containedId);
                if (contained != null)
                {
                    node.Items.Add(CreateObjectNode(contained));
                }
            }
        }
        return node;
    }

    private TreeViewItem CreateNpcNode(Npc npc)
    {
        var npcNode = new TreeViewItem { Header = npc.Name, Tag = npc, Foreground = Brushes.White };
        AddAssignedObjectsNode(npcNode, GetNpcAssignedObjectIds(npc));
        return npcNode;
    }

    private TreeViewItem BuildRoomNode(Room room)
    {
        var roomNode = new TreeViewItem { Header = room.Name, Tag = room, Foreground = Brushes.White };

        // Añadir puertas que conectan con esta sala como hijas
        foreach (var door in _world.Doors.Where(d => d.RoomIdA == room.Id || d.RoomIdB == room.Id))
        {
            var exit = room.Exits?.FirstOrDefault(e => e.DoorId == door.Id);
            var dirAbbrev = exit != null ? GetDirectionAbbreviation(exit.Direction) : null;
            var header = !string.IsNullOrEmpty(dirAbbrev) ? $"({dirAbbrev}) {door.Name}" : door.Name;
            roomNode.Items.Add(new TreeViewItem { Header = header, Tag = door, Foreground = Brushes.White });
        }

        // Añadir subnodo Objetos para la sala
        AddAssignedObjectsNode(roomNode, GetRoomAssignedObjectIds(room));

        return roomNode;
    }

    private HashSet<string> GetItemIdsInFolders(EditorFolderType folderType)
    {
        var ids = new HashSet<string>();
        foreach (var folder in _world.Folders.Where(f => f.FolderType == folderType))
        {
            foreach (var id in folder.ItemIds)
                ids.Add(id);
        }
        return ids;
    }

    private void AddAssignedObjectsNode(TreeViewItem parentNode, List<string> objectIds)
    {
        if (objectIds.Count == 0) return;

        var objectsNode = new TreeViewItem
        {
            Header = $"📦 Objetos ({objectIds.Count})",
            Foreground = Brushes.LightGray,
            FontStyle = FontStyles.Italic
        };

        foreach (var objId in objectIds)
        {
            var obj = _world.Objects.FirstOrDefault(o => o.Id == objId);
            if (obj != null)
            {
                objectsNode.Items.Add(new TreeViewItem
                {
                    Header = obj.Name,
                    Tag = obj,
                    Foreground = Brushes.LightGray
                });
            }
        }

        parentNode.Items.Add(objectsNode);
    }

    private List<string> GetPlayerAssignedObjectIds()
    {
        var ids = new List<string>();
        if (_world.Player.InitialInventory != null)
            ids.AddRange(_world.Player.InitialInventory.Select(i => i.ObjectId).Where(id => !string.IsNullOrEmpty(id)));
        if (!string.IsNullOrEmpty(_world.Player.InitialRightHandId))
            ids.Add(_world.Player.InitialRightHandId);
        if (!string.IsNullOrEmpty(_world.Player.InitialLeftHandId) && _world.Player.InitialLeftHandId != _world.Player.InitialRightHandId)
            ids.Add(_world.Player.InitialLeftHandId);
        if (!string.IsNullOrEmpty(_world.Player.InitialTorsoId))
            ids.Add(_world.Player.InitialTorsoId);
        return ids.Distinct().ToList();
    }

    private List<string> GetNpcAssignedObjectIds(Npc npc)
    {
        var ids = new List<string>();
        if (npc.Inventory != null)
            ids.AddRange(npc.Inventory.Select(i => i.ObjectId).Where(id => !string.IsNullOrEmpty(id)));
        if (!string.IsNullOrEmpty(npc.EquippedRightHandId))
            ids.Add(npc.EquippedRightHandId);
        if (!string.IsNullOrEmpty(npc.EquippedLeftHandId) && npc.EquippedLeftHandId != npc.EquippedRightHandId)
            ids.Add(npc.EquippedLeftHandId);
        if (!string.IsNullOrEmpty(npc.EquippedTorsoId))
            ids.Add(npc.EquippedTorsoId);
        return ids.Distinct().ToList();
    }

    private List<string> GetRoomAssignedObjectIds(Room room)
    {
        return room.ObjectIds?.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList() ?? new List<string>();
    }

    private void SelectGameTreeNode()
    {
        if (_gameTreeNode == null)
            return;

        Dispatcher.InvokeAsync(() =>
        {
            WorldTree.UpdateLayout();
            _gameTreeNode.IsSelected = true;
            _gameTreeNode.Focus();
            _gameTreeNode.BringIntoView();
        }, DispatcherPriority.Loaded);
    }

    private void ClearTreeSelection()
    {
        if (WorldTree.SelectedItem is TreeViewItem selectedItem)
        {
            selectedItem.IsSelected = false;
        }
    }

    private void WorldTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (WorldTree.SelectedItem is not TreeViewItem item)
        {
            PropertyEditor.SetObject(null);
            MapPanel.SetSelectedRoom(null);
            MapPanel.SetVisiblePatrolRoutes(null);
            PropertyScrollViewer.Visibility = Visibility.Collapsed;
            return;
        }

        // Solo mostrar propiedades si el nodo tiene un objeto asociado
        if (item.Tag != null)
        {
            PropertyScrollViewer.Visibility = Visibility.Visible;
            PropertyEditor.SetObject(item.Tag);

            // Guardar nombre original de zona para detectar cambios
            if (item.Tag is ZoneNodeTag zoneTag)
            {
                _selectedZoneOriginalName = zoneTag.ZoneName;
            }
            else
            {
                _selectedZoneOriginalName = null;
            }
        }
        else
        {
            PropertyScrollViewer.Visibility = Visibility.Collapsed;
            PropertyEditor.SetObject(null);
            _selectedZoneOriginalName = null;
        }

        if (item.Tag is Room room)
        {
            MapPanel.SetSelectedRoom(room);
            MapPanel.SetVisiblePatrolRoutes(null);
        }
        else if (item.Tag is Npc npc)
        {
            MapPanel.SetSelectedRoom(null);
            MapPanel.SetVisiblePatrolRoutes(npc.Id);
        }
        else if (item.Header is string header && header.StartsWith("NPCs"))
        {
            // Nodo padre NPCs seleccionado - mostrar todas las rutas
            MapPanel.SetSelectedRoom(null);
            MapPanel.SetVisiblePatrolRoutes(null, showAll: true);
        }
        else
        {
            MapPanel.SetSelectedRoom(null);
            MapPanel.SetVisiblePatrolRoutes(null);
        }
    }

    private void WorldTree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (WorldTree.SelectedItem is not TreeViewItem item)
            return;

        switch (item.Tag)
        {
            case Room room:
                MapPanel.CenterOnRoom(room);
                break;
            case Door door:
                MapPanel.CenterOnDoor(door);
                break;
            case GameObject obj:
                CenterOnObject(obj);
                break;
            case Npc npc:
                CenterOnNpc(npc);
                break;
        }
    }

    private Point? _dragStartPoint;
    private TreeViewItem? _dragItem;

    private void WorldTree_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);

        // Encontrar el TreeViewItem bajo el cursor para el drag
        var hit = e.OriginalSource as DependencyObject;
        while (hit != null && hit is not TreeViewItem)
        {
            hit = VisualTreeHelper.GetParent(hit);
        }
        _dragItem = hit as TreeViewItem;
    }

    private void WorldTree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Encontrar el TreeViewItem bajo el cursor
        var hit = e.OriginalSource as DependencyObject;
        while (hit != null && hit is not TreeViewItem)
        {
            hit = VisualTreeHelper.GetParent(hit);
        }

        if (hit is not TreeViewItem item) return;

        // Seleccionar el item
        item.IsSelected = true;

        // Menú contextual para modo prueba (GameObjects)
        if (_testEngine != null && item.Tag is GameObject gameObj)
        {
            ShowTestModeContextMenu(gameObj);
            e.Handled = true;
            return;
        }

        // Menú contextual para modo edición
        if (_testEngine == null)
        {
            var header = item.Header?.ToString() ?? "";

            // Menú contextual para nodos de zona
            if (item.Tag is ZoneNodeTag zoneTag)
            {
                ShowZoneContextMenu(zoneTag, item);
                e.Handled = true;
                return;
            }

            // Detectar nodos raíz que soportan carpetas/zonas
            var headerUpper = header.ToUpper();

            // Nodo raíz "Salas" - mostrar opción de crear zona
            if (headerUpper.Contains("SALAS"))
            {
                ShowSalasRootContextMenu();
                e.Handled = true;
                return;
            }

            EditorFolderType? folderType = null;
            if (headerUpper.Contains("OBJETOS")) folderType = EditorFolderType.Objects;
            else if (headerUpper.Contains("NPCS")) folderType = EditorFolderType.Npcs;
            else if (headerUpper.Contains("MISIONES")) folderType = EditorFolderType.Quests;

            // También detectar carpetas existentes
            if (item.Tag is EditorFolder folder)
            {
                ShowFolderContextMenu(folder, item);
                e.Handled = true;
                return;
            }

            if (folderType.HasValue)
            {
                ShowAddFolderContextMenu(folderType.Value, null);
                e.Handled = true;
                return;
            }
        }
    }

    private void ShowTestModeContextMenu(GameObject gameObj)
    {
        var menu = CreateDarkContextMenu();

        var addToInventoryItem = new MenuItem
        {
            Header = "Enviar a inventario",
            Foreground = new SolidColorBrush(Color.FromRgb(100, 220, 100)),
            FontWeight = FontWeights.SemiBold
        };
        addToInventoryItem.Click += (_, _) =>
        {
            if (_testEngine != null && !_testEngine.State.InventoryObjectIds.Contains(gameObj.Id))
            {
                _testEngine.State.InventoryObjectIds.Add(gameObj.Id);
                AppendTestOutput($"+ {gameObj.Name} añadido al inventario");
                UpdateTestDisplay();
            }
            else
            {
                AppendTestOutput($"Ya tienes {gameObj.Name} en el inventario");
            }
        };
        menu.Items.Add(addToInventoryItem);
        menu.IsOpen = true;
    }

    private void ShowAddFolderContextMenu(EditorFolderType folderType, string? parentFolderId)
    {
        var menu = CreateDarkContextMenu();

        var addFolderItem = new MenuItem
        {
            Header = "📁 Añadir carpeta",
            Foreground = Brushes.White
        };
        addFolderItem.Click += (_, _) =>
        {
            var folder = new EditorFolder
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Nueva carpeta",
                FolderType = folderType,
                ParentFolderId = parentFolderId
            };
            _world.Folders.Add(folder);
            BuildTree();

            // Expandir el nodo padre
            if (parentFolderId != null)
            {
                // Padre es otra carpeta
                var parentFolder = _world.Folders.FirstOrDefault(f => f.Id == parentFolderId);
                if (parentFolder != null)
                {
                    var parentItem = FindTreeItemByTag(WorldTree.Items, parentFolder);
                    if (parentItem != null)
                        parentItem.IsExpanded = true;
                }
            }
            else
            {
                // Padre es el nodo raíz (Objetos, NPCs, Salas, Misiones)
                ExpandRootNodeByFolderType(folderType);
            }

            // Seleccionar la carpeta recién creada y entrar en modo edición
            var folderItem = FindTreeItemByTag(WorldTree.Items, folder);
            if (folderItem != null)
            {
                folderItem.IsSelected = true;
                // Usar Dispatcher para asegurar que el árbol está listo antes de editar
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StartFolderNameEdit(folderItem, folder);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        };
        menu.Items.Add(addFolderItem);
        menu.IsOpen = true;
    }

    private void ShowFolderContextMenu(EditorFolder folder, TreeViewItem folderItem)
    {
        var menu = CreateDarkContextMenu();

        var addSubfolderItem = new MenuItem
        {
            Header = "📁 Añadir subcarpeta",
            Foreground = Brushes.White
        };
        addSubfolderItem.Click += (_, _) =>
        {
            var subfolder = new EditorFolder
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Nueva carpeta",
                FolderType = folder.FolderType,
                ParentFolderId = folder.Id
            };
            _world.Folders.Add(subfolder);
            BuildTree();

            // Expandir la carpeta padre y seleccionar la subcarpeta
            var parentItem = FindTreeItemByTag(WorldTree.Items, folder);
            if (parentItem != null)
            {
                parentItem.IsExpanded = true;
            }

            var subfolderItem = FindTreeItemByTag(WorldTree.Items, subfolder);
            if (subfolderItem != null)
            {
                subfolderItem.IsSelected = true;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    StartFolderNameEdit(subfolderItem, subfolder);
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        };
        menu.Items.Add(addSubfolderItem);

        var renameItem = new MenuItem
        {
            Header = "✏️ Renombrar",
            Foreground = Brushes.White
        };
        renameItem.Click += (_, _) =>
        {
            StartFolderNameEdit(folderItem, folder);
        };
        menu.Items.Add(renameItem);

        var deleteItem = new MenuItem
        {
            Header = "🗑️ Eliminar carpeta",
            Foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100))
        };
        deleteItem.Click += (_, _) =>
        {
            DeleteFolder(folder);
        };
        menu.Items.Add(deleteItem);

        menu.IsOpen = true;
    }

    private void DeleteFolderRecursive(string folderId)
    {
        var subfolders = _world.Folders.Where(f => f.ParentFolderId == folderId).ToList();
        foreach (var subfolder in subfolders)
        {
            DeleteFolderRecursive(subfolder.Id);
            _world.Folders.Remove(subfolder);
        }
    }

    private ContextMenu CreateDarkContextMenu()
    {
        var menu = new ContextMenu
        {
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 45)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            Foreground = Brushes.White
        };

        // Template personalizado sin columna de icon/checkbox
        var template = new ControlTemplate(typeof(MenuItem));

        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "Border";
        border.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(45, 45, 45)));
        border.SetValue(Border.PaddingProperty, new Thickness(8, 4, 8, 4));

        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.ContentSourceProperty, "Header");
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

        border.AppendChild(contentPresenter);
        template.VisualTree = border;

        // Trigger para hover
        var hoverTrigger = new Trigger { Property = MenuItem.IsHighlightedProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(62, 62, 64)), "Border"));
        template.Triggers.Add(hoverTrigger);

        var itemStyle = new Style(typeof(MenuItem));
        itemStyle.Setters.Add(new Setter(MenuItem.ForegroundProperty, Brushes.White));
        itemStyle.Setters.Add(new Setter(MenuItem.TemplateProperty, template));

        // Usar StyleSelector para evitar aplicar el estilo MenuItem a Separators
        menu.ItemContainerStyleSelector = new MenuItemStyleSelector { MenuItemStyle = itemStyle };

        return menu;
    }

    private void ShowSalasRootContextMenu()
    {
        var menu = CreateDarkContextMenu();

        var createZoneItem = new MenuItem
        {
            Header = "🗺️ Crear zona",
            Foreground = Brushes.White
        };
        createZoneItem.Click += (s, ev) => AddZone_Click(s, ev);
        menu.Items.Add(createZoneItem);

        menu.IsOpen = true;
    }

    private void ShowZoneContextMenu(ZoneNodeTag zoneTag, TreeViewItem zoneNode)
    {
        var menu = CreateDarkContextMenu();
        var zoneName = zoneTag.ZoneName;

        // Añadir sala a esta zona
        var addRoomItem = new MenuItem
        {
            Header = "➕ Añadir sala",
            Foreground = Brushes.White
        };
        addRoomItem.Click += (s, ev) =>
        {
            var index = _world.Rooms.Count + 1;
            var room = new Room
            {
                Id = $"sala_{index}",
                Name = $"Sala {index}",
                Description = "Nueva sala.",
                Zone = zoneName
            };
            _world.Rooms.Add(room);
            MapPanel.SetWorld(_world);
            BuildTree();
            UpdateZoneFilter();
            SelectRoomInTree(room);
            PushUndoSnapshot();
            SetDirty(true);
        };
        menu.Items.Add(addRoomItem);

        menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)) });

        // Renombrar zona
        var renameItem = new MenuItem
        {
            Header = "✏️ Renombrar zona",
            Foreground = Brushes.White
        };
        renameItem.Click += (s, ev) =>
        {
            StartZoneRename(zoneNode, zoneName);
        };
        menu.Items.Add(renameItem);

        menu.Items.Add(new Separator { Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)) });

        // Eliminar zona (solo si no es la última)
        var totalZones = _world.Rooms
            .Select(r => r.Zone)
            .Where(z => !string.IsNullOrEmpty(z))
            .Distinct()
            .Count();

        var deleteItem = new MenuItem
        {
            Header = "🗑️ Eliminar zona",
            Foreground = totalZones > 1 ? Brushes.White : Brushes.Gray,
            IsEnabled = totalZones > 1
        };
        deleteItem.Click += (s, ev) => DeleteZone(zoneName);
        menu.Items.Add(deleteItem);

        menu.IsOpen = true;
    }

    private void MoveItemToFolder(string itemId, EditorFolder targetFolder, EditorFolderType folderType)
    {
        // Quitar de cualquier carpeta anterior
        foreach (var folder in _world.Folders.Where(f => f.FolderType == folderType))
        {
            folder.ItemIds.Remove(itemId);
        }

        // Añadir a la nueva carpeta
        if (!targetFolder.ItemIds.Contains(itemId))
        {
            targetFolder.ItemIds.Add(itemId);
        }
    }

    private void RemoveItemFromFolders(string itemId, EditorFolderType folderType)
    {
        foreach (var folder in _world.Folders.Where(f => f.FolderType == folderType))
        {
            folder.ItemIds.Remove(itemId);
        }
    }

    private void ExpandRootNodeByFolderType(EditorFolderType folderType)
    {
        var headerKeyword = folderType switch
        {
            EditorFolderType.Objects => "OBJETOS",
            EditorFolderType.Npcs => "NPCS",
            EditorFolderType.Rooms => "SALAS",
            EditorFolderType.Quests => "MISIONES",
            _ => ""
        };

        foreach (TreeViewItem item in WorldTree.Items)
        {
            if (item.Header is string header && header.ToUpper().Contains(headerKeyword))
            {
                item.IsExpanded = true;
                break;
            }
        }
    }

    private void SelectTreeItemByTag(object tag)
    {
        Dispatcher.InvokeAsync(() =>
        {
            var item = FindTreeItemByTag(WorldTree.Items, tag);
            if (item != null)
            {
                item.IsSelected = true;
                item.BringIntoView();
            }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private TreeViewItem? FindTreeItemByTag(ItemCollection items, object tag)
    {
        foreach (var i in items.OfType<TreeViewItem>())
        {
            if (i.Tag == tag) return i;
            var found = FindTreeItemByTag(i.Items, tag);
            if (found != null) return found;
        }
        return null;
    }

    private void WorldTree_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _dragStartPoint.HasValue && _dragItem != null)
        {
            Point currentPosition = e.GetPosition(null);
            Vector diff = _dragStartPoint.Value - currentPosition;

            // Solo iniciar drag si el movimiento es significativo (más de 5 píxeles)
            if (Math.Abs(diff.X) > 5 || Math.Abs(diff.Y) > 5)
            {
                // Usar el item capturado en MouseDown, no el seleccionado
                var item = _dragItem;

                // Permitir drag de objetos, NPCs, salas y misiones
                if (item.Tag is GameObject gameObj)
                {
                    var data = new DataObject();
                    data.SetData("GameObject", gameObj);
                    DragDrop.DoDragDrop(WorldTree, data, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragItem = null;
                }
                else if (item.Tag is Npc npc)
                {
                    var data = new DataObject();
                    data.SetData("Npc", npc);
                    DragDrop.DoDragDrop(WorldTree, data, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragItem = null;
                }
                else if (item.Tag is Room room)
                {
                    var data = new DataObject();
                    data.SetData("Room", room);
                    DragDrop.DoDragDrop(WorldTree, data, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragItem = null;
                }
                else if (item.Tag is QuestDefinition quest)
                {
                    var data = new DataObject();
                    data.SetData("Quest", quest);
                    DragDrop.DoDragDrop(WorldTree, data, DragDropEffects.Move);
                    _dragStartPoint = null;
                    _dragItem = null;
                }
            }
        }
    }

    private void WorldTree_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("GameObject") || e.Data.GetDataPresent("Npc") ||
            e.Data.GetDataPresent("Room") || e.Data.GetDataPresent("Quest"))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void WorldTree_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("GameObject") || e.Data.GetDataPresent("Npc") ||
            e.Data.GetDataPresent("Room") || e.Data.GetDataPresent("Quest"))
        {
            // Obtener el TreeViewItem sobre el que se está arrastrando
            var targetItem = GetTreeViewItemAtPoint(WorldTree, e.GetPosition(WorldTree));

            if (targetItem != null)
            {
                // Validar si es una operación válida
                bool isValid = false;

                if (e.Data.GetDataPresent("GameObject"))
                {
                    var draggedObj = e.Data.GetData("GameObject") as GameObject;
                    if (draggedObj != null)
                    {
                        // Objeto → Contenedor (debe ser diferente y no crear referencia circular)
                        if (targetItem.Tag is GameObject targetObj && targetObj.IsContainer && targetObj.Id != draggedObj.Id)
                        {
                            // Verificar que no se cree una referencia circular
                            if (!WouldCreateCircularReference(draggedObj, targetObj))
                            {
                                // Verificar capacidad del contenedor (si tiene límite)
                                if (targetObj.MaxCapacity > 0)
                                {
                                    double currentVolume = CalculateContainerUsedVolume(targetObj);
                                    double newVolume = currentVolume + draggedObj.Volume;

                                    // Solo permitir si no se excede la capacidad
                                    isValid = newVolume <= targetObj.MaxCapacity;
                                }
                                else
                                {
                                    // Sin límite de capacidad
                                    isValid = true;
                                }
                            }
                        }
                        // Objeto → Sala
                        else if (targetItem.Tag is Room)
                        {
                            isValid = true;
                        }
                        // Objeto → Carpeta de objetos
                        else if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Objects)
                        {
                            isValid = true;
                        }
                    }
                }
                else if (e.Data.GetDataPresent("Npc"))
                {
                    // NPC → Sala
                    if (targetItem.Tag is Room)
                    {
                        isValid = true;
                    }
                    // NPC → Carpeta de NPCs
                    else if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Npcs)
                    {
                        isValid = true;
                    }
                }
                else if (e.Data.GetDataPresent("Room"))
                {
                    var draggedRoom = e.Data.GetData("Room") as Room;
                    // Sala → Zona
                    if (targetItem.Tag is ZoneNodeTag)
                    {
                        isValid = true;
                    }
                    // Sala → Sala (asignar a la zona de la sala destino)
                    else if (targetItem.Tag is Room targetRoom && draggedRoom != null &&
                             targetRoom.Id != draggedRoom.Id && !string.IsNullOrEmpty(targetRoom.Zone))
                    {
                        isValid = true;
                    }
                    // Sala → Carpeta de salas
                    else if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Rooms)
                    {
                        isValid = true;
                    }
                }
                else if (e.Data.GetDataPresent("Quest"))
                {
                    // Misión → Carpeta de misiones
                    if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Quests)
                    {
                        isValid = true;
                    }
                }

                e.Effects = isValid ? DragDropEffects.Move : DragDropEffects.None;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void WorldTree_Drop(object sender, DragEventArgs e)
    {
        try
        {
            var targetItem = GetTreeViewItemAtPoint(WorldTree, e.GetPosition(WorldTree));
            if (targetItem == null) return;

            bool changed = false;

            if (e.Data.GetDataPresent("GameObject"))
            {
                var draggedObj = e.Data.GetData("GameObject") as GameObject;
                if (draggedObj != null)
                {
                    // Objeto → Contenedor
                    if (targetItem.Tag is GameObject targetObj && targetObj.IsContainer && targetObj.Id != draggedObj.Id)
                    {
                        // Verificar capacidad del contenedor (si tiene límite)
                        if (targetObj.MaxCapacity > 0)
                        {
                            double currentVolume = CalculateContainerUsedVolume(targetObj);
                            double newVolume = currentVolume + draggedObj.Volume;

                            if (newVolume > targetObj.MaxCapacity)
                            {
                                var dlg = new AlertWindow(
                                    $"No hay espacio suficiente en '{targetObj.Name}'.\n\n" +
                                    $"Capacidad máxima: {targetObj.MaxCapacity:F0} cm³\n" +
                                    $"Volumen usado: {currentVolume:F0} cm³\n" +
                                    $"Espacio disponible: {(targetObj.MaxCapacity - currentVolume):F0} cm³\n" +
                                    $"Volumen del objeto: {draggedObj.Volume:F0} cm³",
                                    "Capacidad excedida")
                                {
                                    Owner = this
                                };
                                dlg.ShowDialog();
                                return;
                            }
                        }

                        // Quitar de contenedor anterior si estaba en uno
                        foreach (var container in _world.Objects.Where(o => o.IsContainer))
                        {
                            if (container.ContainedObjectIds.Contains(draggedObj.Id, StringComparer.OrdinalIgnoreCase))
                            {
                                container.ContainedObjectIds.Remove(draggedObj.Id);
                            }
                        }

                        // Añadir al nuevo contenedor
                        if (!targetObj.ContainedObjectIds.Contains(draggedObj.Id, StringComparer.OrdinalIgnoreCase))
                        {
                            targetObj.ContainedObjectIds.Add(draggedObj.Id);
                        }

                        // Sincronizar la sala del objeto con la del contenedor
                        draggedObj.RoomId = targetObj.RoomId;

                        changed = true;
                    }
                    // Objeto → Sala
                    else if (targetItem.Tag is Room targetRoom)
                    {
                        // Quitar del contenedor si estaba en uno
                        foreach (var container in _world.Objects.Where(o => o.IsContainer))
                        {
                            if (container.ContainedObjectIds.Contains(draggedObj.Id, StringComparer.OrdinalIgnoreCase))
                            {
                                container.ContainedObjectIds.Remove(draggedObj.Id);
                            }
                        }

                        // Cambiar la sala del objeto
                        draggedObj.RoomId = targetRoom.Id;
                        changed = true;
                    }
                }
            }
            else if (e.Data.GetDataPresent("Npc"))
            {
                var draggedNpc = e.Data.GetData("Npc") as Npc;
                if (draggedNpc != null)
                {
                    if (targetItem.Tag is Room targetRoom)
                    {
                        // NPC → Sala
                        draggedNpc.RoomId = targetRoom.Id;
                        changed = true;
                    }
                    else if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Npcs)
                    {
                        // NPC → Carpeta
                        MoveItemToFolder(draggedNpc.Id, folder, EditorFolderType.Npcs);
                        changed = true;
                    }
                }
            }
            else if (e.Data.GetDataPresent("Room"))
            {
                var draggedRoom = e.Data.GetData("Room") as Room;
                if (draggedRoom != null)
                {
                    // Room → Zona
                    if (targetItem.Tag is ZoneNodeTag zoneTag)
                    {
                        if (draggedRoom.Zone != zoneTag.ZoneName)
                        {
                            draggedRoom.Zone = zoneTag.ZoneName;
                            changed = true;
                        }
                    }
                    // Room → Room (asignar a la zona de la sala destino)
                    else if (targetItem.Tag is Room targetRoom && targetRoom.Id != draggedRoom.Id)
                    {
                        if (!string.IsNullOrEmpty(targetRoom.Zone) && draggedRoom.Zone != targetRoom.Zone)
                        {
                            draggedRoom.Zone = targetRoom.Zone;
                            changed = true;
                        }
                    }
                    // Room → Carpeta (compatibilidad hacia atrás)
                    else if (targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Rooms)
                    {
                        MoveItemToFolder(draggedRoom.Id, folder, EditorFolderType.Rooms);
                        changed = true;
                    }
                }
            }
            else if (e.Data.GetDataPresent("Quest"))
            {
                var draggedQuest = e.Data.GetData("Quest") as QuestDefinition;
                if (draggedQuest != null && targetItem.Tag is EditorFolder folder && folder.FolderType == EditorFolderType.Quests)
                {
                    MoveItemToFolder(draggedQuest.Id, folder, EditorFolderType.Quests);
                    changed = true;
                }
            }

            // Manejar drop de GameObject en carpeta
            if (e.Data.GetDataPresent("GameObject") && targetItem.Tag is EditorFolder objFolder && objFolder.FolderType == EditorFolderType.Objects)
            {
                var draggedObj = e.Data.GetData("GameObject") as GameObject;
                if (draggedObj != null)
                {
                    MoveItemToFolder(draggedObj.Id, objFolder, EditorFolderType.Objects);
                    changed = true;
                }
            }

            if (changed)
            {
                BuildTree();
                UpdateZoneFilter();
                MapPanel.InvalidateVisual();
                PropertyEditor.SetObject(WorldTree.SelectedItem is TreeViewItem item ? item.Tag : null);
                PushUndoSnapshot();
                SetDirty(true);
            }

            e.Handled = true;
        }
        catch
        {
            // Ignorar errores
        }
    }

    private TreeViewItem? GetTreeViewItemAtPoint(TreeView treeView, Point point)
    {
        var hitTestResult = VisualTreeHelper.HitTest(treeView, point);
        if (hitTestResult == null) return null;

        var element = hitTestResult.VisualHit;
        while (element != null && element != treeView)
        {
            if (element is TreeViewItem item)
            {
                return item;
            }
            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    private void MapPanel_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("GameObject") || e.Data.GetDataPresent("Npc"))
        {
            e.Effects = DragDropEffects.Move;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void MapPanel_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("GameObject") || e.Data.GetDataPresent("Npc"))
        {
            var point = e.GetPosition(MapPanel);
            var room = MapPanel.GetRoomAtPoint(point);
            e.Effects = room != null ? DragDropEffects.Move : DragDropEffects.None;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void MapPanel_Drop(object sender, DragEventArgs e)
    {
        try
        {
            if (_world == null) return;

            var point = e.GetPosition(MapPanel);
            var targetRoom = MapPanel.GetRoomAtPoint(point);
            if (targetRoom == null)
            {
                e.Handled = true;
                return;
            }

            object? droppedItem = null;
            bool changed = false;

            if (e.Data.GetDataPresent("GameObject"))
            {
                var gameObj = e.Data.GetData("GameObject") as GameObject;
                if (gameObj != null)
                {
                    // Quitar de cualquier contenedor
                    foreach (var container in _world.Objects.Where(o => o.IsContainer))
                    {
                        container.ContainedObjectIds.Remove(gameObj.Id);
                    }
                    gameObj.RoomId = targetRoom.Id;
                    droppedItem = gameObj;
                    changed = true;
                }
            }
            else if (e.Data.GetDataPresent("Npc"))
            {
                var npc = e.Data.GetData("Npc") as Npc;
                if (npc != null)
                {
                    // Si el NPC tiene una ruta de patrulla, preguntar antes de moverlo
                    if (npc.PatrolRoute.Count > 0)
                    {
                        var confirmWindow = new AlertWindow(
                            $"El NPC '{npc.Name}' tiene una ruta de patrulla configurada.\n\n" +
                            $"Si lo mueves a otra sala, la ruta de patrulla se eliminará " +
                            $"y el NPC dejará de patrullar.\n\n" +
                            $"¿Deseas continuar?",
                            "Mover NPC con ruta de patrulla");
                        confirmWindow.ShowCancelButton(true);
                        confirmWindow.SetOkButtonText("Mover y eliminar ruta");
                        confirmWindow.Owner = this;

                        if (confirmWindow.ShowDialog() != true)
                        {
                            e.Handled = true;
                            return; // El usuario canceló
                        }

                        // Limpiar la ruta de patrulla y desactivar el patrullaje
                        npc.PatrolRoute.Clear();
                        npc.IsPatrolling = false;
                    }

                    npc.RoomId = targetRoom.Id;
                    droppedItem = npc;
                    changed = true;
                }
            }

            if (changed && droppedItem != null)
            {
                BuildTree();
                SelectTreeItemByTag(droppedItem);
                PushUndoSnapshot();
                SetDirty(true);
                MapPanel.InvalidateVisual();

                // Forzar refresh del PropertyEditor después de que el UI se actualice
                var itemToShow = droppedItem;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PropertyEditor.SetObject(null);
                    PropertyEditor.SetObject(itemToShow);
                }), System.Windows.Threading.DispatcherPriority.Render);
            }

            e.Handled = true;
        }
        catch
        {
            // Ignorar errores
        }
    }

    /// <summary>
    /// Verifica si poner draggedObj dentro de targetContainer crearía una referencia circular.
    /// Por ejemplo, si draggedObj contiene targetContainer (directa o indirectamente).
    /// </summary>
    private bool WouldCreateCircularReference(GameObject draggedObj, GameObject targetContainer)
    {
        if (!draggedObj.IsContainer)
        {
            // Si draggedObj no es contenedor, no puede crear referencias circulares
            return false;
        }

        // Verificar si targetContainer está contenido (directa o indirectamente) en draggedObj
        return IsObjectContainedIn(targetContainer, draggedObj);
    }

    /// <summary>
    /// Verifica si obj está contenido (directa o indirectamente) en container.
    /// </summary>
    private bool IsObjectContainedIn(GameObject obj, GameObject container)
    {
        if (!container.IsContainer)
            return false;

        // Verificación directa
        if (container.ContainedObjectIds.Contains(obj.Id, StringComparer.OrdinalIgnoreCase))
            return true;

        // Verificación indirecta (recursiva)
        foreach (var containedId in container.ContainedObjectIds)
        {
            var containedObj = _world.Objects.FirstOrDefault(o =>
                string.Equals(o.Id, containedId, StringComparison.OrdinalIgnoreCase));

            if (containedObj != null && containedObj.IsContainer)
            {
                if (IsObjectContainedIn(obj, containedObj))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifica si un objeto está contenido dentro de algún otro objeto.
    /// </summary>
    private bool IsObjectContainedInAnother(GameObject obj)
    {
        return _world.Objects.Any(container =>
            container.IsContainer &&
            container.ContainedObjectIds.Contains(obj.Id, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Construye recursivamente el árbol de objetos, incluyendo objetos contenidos como hijos.
    /// </summary>
    private void BuildObjectTreeRecursive(TreeViewItem parentNode, GameObject obj)
    {
        var header = obj.IsContainer ? $"📦 {obj.Name}" : obj.Name;
        var objNode = new TreeViewItem { Header = header, Tag = obj, Foreground = Brushes.White };

        // Si es un contenedor, añadir sus objetos contenidos como hijos
        if (obj.IsContainer && obj.ContainedObjectIds.Count > 0)
        {
            foreach (var containedId in obj.ContainedObjectIds)
            {
                var containedObj = _world.Objects.FirstOrDefault(o =>
                    string.Equals(o.Id, containedId, StringComparison.OrdinalIgnoreCase));

                if (containedObj != null)
                {
                    BuildObjectTreeRecursive(objNode, containedObj);
                }
            }
        }

        parentNode.Items.Add(objNode);
    }

    /// <summary>
    /// Calcula el volumen total de los objetos contenidos en un contenedor.
    /// </summary>
    private double CalculateContainerUsedVolume(GameObject container)
    {
        if (!container.IsContainer)
            return 0;

        double totalVolume = 0;

        foreach (var containedId in container.ContainedObjectIds)
        {
            var containedObj = _world.Objects.FirstOrDefault(o =>
                string.Equals(o.Id, containedId, StringComparison.OrdinalIgnoreCase));

            if (containedObj != null)
            {
                totalVolume += containedObj.Volume;
            }
        }

        return totalVolume;
    }

    private void CenterOnObject(GameObject obj)
    {
        if (_world == null || string.IsNullOrWhiteSpace(obj.RoomId))
            return;

        var room = _world.Rooms.FirstOrDefault(r => r.Id == obj.RoomId);
        if (room == null)
            return;

        MapPanel.CenterOnRoom(room);
        MapPanel.HighlightRoomWithBreathing(room.Id);
    }

    private void CenterOnNpc(Npc npc)
    {
        if (_world == null || string.IsNullOrWhiteSpace(npc.RoomId))
            return;

        var room = _world.Rooms.FirstOrDefault(r => r.Id == npc.RoomId);
        if (room == null)
            return;

        MapPanel.CenterOnRoom(room);
        MapPanel.HighlightRoomWithBreathing(room.Id);
    }

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in WorldTree.Items.OfType<TreeViewItem>())
        {
            SetExpandedRecursive(item, true);
        }
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var item in WorldTree.Items.OfType<TreeViewItem>())
        {
            SetExpandedRecursive(item, false);
        }
    }

    private void SetExpandedRecursive(TreeViewItem item, bool expanded)
    {
        item.IsExpanded = expanded;
        foreach (var child in item.Items.OfType<TreeViewItem>())
        {
            SetExpandedRecursive(child, expanded);
        }
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PerformSearch(SearchTextBox.Text);
    }

    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        PerformSearch(SearchTextBox.Text);
    }

    private TreeViewItem? FindInTree(TreeViewItem node, string text)
    {
        if (node.Header is string s && s.ToLowerInvariant().Contains(text))
            return node;

        foreach (TreeViewItem child in node.Items.OfType<TreeViewItem>())
        {
            var found = FindInTree(child, text);
            if (found != null)
                return found;
        }

        return null;
    }

    private void PerformSearch(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var normalized = text.ToLowerInvariant();

        foreach (TreeViewItem root in WorldTree.Items)
        {
            var found = FindInTree(root, normalized);
            if (found != null)
            {
                found.IsSelected = true;
                found.BringIntoView();
                break;
            }
        }
    }

    #region Property Panel Search and Expand/Collapse

    private void PropertyExpandAll_Click(object sender, RoutedEventArgs e)
    {
        PropertyEditor.ExpandAll();
    }

    private void PropertyCollapseAll_Click(object sender, RoutedEventArgs e)
    {
        PropertyEditor.CollapseAll();
    }

    private void PropertySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        PerformPropertySearch(PropertySearchTextBox.Text);
    }

    private void PerformPropertySearch(string text)
    {
        // Limpiar highlight anterior
        ClearPropertyHighlight();

        if (string.IsNullOrWhiteSpace(text))
            return;

        var (element, parentExpander) = PropertyEditor.SearchProperty(text);

        if (element != null)
        {
            // Si está en un Expander cerrado, abrirlo
            if (parentExpander != null && !parentExpander.IsExpanded)
            {
                parentExpander.IsExpanded = true;
            }

            // Esperar a que el layout se actualice después de expandir
            Dispatcher.InvokeAsync(() =>
            {
                // Hacer scroll para centrar el elemento verticalmente
                ScrollToCenter(element);
            }, System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }

    private void ScrollToCenter(UIElement element)
    {
        var scrollViewer = PropertyEditor.InternalScrollViewer;

        // Obtener la posición del elemento relativa al ScrollViewer
        var transform = element.TransformToAncestor(scrollViewer);
        var elementPosition = transform.Transform(new Point(0, 0));

        // Calcular el offset para centrar verticalmente
        var elementHeight = element is FrameworkElement fe ? fe.ActualHeight : 20;
        var viewportHeight = scrollViewer.ViewportHeight;
        var currentOffset = scrollViewer.VerticalOffset;

        // Posición deseada: centrar el elemento en el viewport
        var targetOffset = currentOffset + elementPosition.Y - (viewportHeight / 2) + (elementHeight / 2);

        // Asegurar que el offset esté dentro de los límites
        targetOffset = Math.Max(0, Math.Min(targetOffset, scrollViewer.ScrollableHeight));

        scrollViewer.ScrollToVerticalOffset(targetOffset);

        // Resaltar brevemente el elemento encontrado
        HighlightElement(element);
    }

    private void HighlightElement(UIElement element)
    {
        if (element is not TextBlock tb) return;

        // Guardar referencia para poder limpiar después
        _highlightedPropertyLabel = tb;
        _highlightedPropertyOriginalForeground = tb.Foreground;
        tb.Foreground = new SolidColorBrush(Colors.Yellow);

        // Detener timer anterior si existe
        _highlightTimer?.Stop();

        // Restaurar después de un tiempo si no se limpia manualmente
        _highlightTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _highlightTimer.Tick += (s, e) =>
        {
            ClearPropertyHighlight();
        };
        _highlightTimer.Start();
    }

    private void ClearPropertyHighlight()
    {
        // Detener timer
        _highlightTimer?.Stop();
        _highlightTimer = null;

        // Restaurar color original
        if (_highlightedPropertyLabel != null && _highlightedPropertyOriginalForeground != null)
        {
            _highlightedPropertyLabel.Foreground = _highlightedPropertyOriginalForeground;
        }

        _highlightedPropertyLabel = null;
        _highlightedPropertyOriginalForeground = null;
    }

    #endregion

    private void RoomImageButton_Click(object sender, RoutedEventArgs e)
    {
        if (WorldTree.SelectedItem is not TreeViewItem item || item.Tag is not Room room)
            return;

        var dlg = new OpenFileDialog
        {
            Title = "Seleccionar imagen de sala",
            Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            room.ImageId = fileName;
            PropertyEditor.SetObject(room);
            MapPanel.InvalidateVisual();
            PushUndoSnapshot();
        }
    }

    private void RoomMusicButton_Click(object sender, RoutedEventArgs e)
    {
        if (WorldTree.SelectedItem is not TreeViewItem item || item.Tag is not Room room)
            return;

        var dlg = new OpenFileDialog
        {
            Title = "Seleccionar música de sala",
            Filter = "Audio (*.mp3;*.wav)|*.mp3;*.wav|Todos los archivos (*.*)|*.*"
        };

        if (dlg.ShowDialog(this) == true)
        {
            var fileName = System.IO.Path.GetFileName(dlg.FileName);
            room.MusicId = fileName;
            PropertyEditor.SetObject(room);
            PushUndoSnapshot();
        }
    }

    private void NewMenu_Click(object sender, RoutedEventArgs e)
    {
        _world = new WorldModel();
        _world.Game.Id = Guid.NewGuid().ToString();
        _world.Game.Title = "Nuevo mundo";
        _world.Game.StartRoomId = "sala_inicio";
        _world.ShowGrid = true; // Grid activado por defecto en mundos nuevos

        var startRoom = new Room
        {
            Id = "sala_inicio",
            Name = "Sala inicial",
            Description = "La sala inicial de tu mundo.",
            Zone = "Inicial"
        };
        _world.Rooms.Add(startRoom);

        // Posición ajustada al grid (centro de la primera celda: 80, 45)
        _world.RoomPositions["sala_inicio"] = new MapPosition { X = 80, Y = 45 };

        _currentPath = null;
        MapPanel.SetWorld(_world);
        BuildTree();
        UpdateZoneFilter();
        ResetUndoRedo();

        // Sincronizar estado visual del grid
        MapPanel.SetGridVisibility(_world.ShowGrid);
        MapPanel.SetSnapToGrid(_world.SnapToGrid);
        ToggleGridButton.IsChecked = _world.ShowGrid;
        ToggleSnapButton.IsChecked = _world.SnapToGrid;

        // Centrar el mapa en la sala inicial
        MapPanel.CenterOnRoom(startRoom);
    }

    private void OpenMenu_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Abrir mundo",
            Filter = "Mundos (*.xaw)|*.xaw|Todos los archivos (*.*)|*.*",
            InitialDirectory = AppPaths.WorldsFolder
        };

        if (dlg.ShowDialog(this) == true)
        {
            TryLoadWorldWithPrompt(dlg.FileName);
            if (IsCanceled)
                return;

            MapPanel.SetWorld(_world);
            BuildTree();
            UpdateZoneFilter();
            ResetUndoRedo();
        }
    }




    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        if (_world == null)
            return;

        // Si el panel ya está visible, cerrarlo (toggle)
        if (TestPanel.Visibility == Visibility.Visible)
        {
            HideTestPanel();
            return;
        }

        if (_isPlayRunning)
            return;

        ShowPlayLoading("Guardando mundo...");
        await Dispatcher.Yield();

        // Guardar antes de lanzar la partida de prueba
        if (!await PerformSaveAsync(hideLoadingOnComplete: false))
        {
            HidePlayLoading();
            return;
        }

        ShowPlayLoading("Preparando todo...");
        await Dispatcher.Yield();

        _isPlayRunning = true;

        try
        {
            // Sincronizar posiciones del mapa
            SyncMapPositionsToWorld();

            Directory.CreateDirectory(AppPaths.WorldsFolder);

            if (string.IsNullOrEmpty(_currentPath))
            {
                var baseName = string.IsNullOrWhiteSpace(_world!.Game.Id)
                    ? "mundo_desde_editor"
                    : _world.Game.Id;
                _currentPath = Path.Combine(AppPaths.WorldsFolder, baseName + ".xaw");
            }

            WorldLoader.SaveWorldModel(_world!, _currentPath);

            // Cargar mundo para la prueba
            _testWorld = WorldLoader.LoadWorldModel(_currentPath);
            Parser.SetWorldDictionary(_testWorld.Game.ParserDictionaryJson);
            var state = WorldLoader.CreateInitialState(_testWorld);

            // Obtener configuración del modo pruebas
            var soundEnabled = _world.Game.TestModeSoundEnabled;
            var aiEnabled = _world.Game.TestModeAiEnabled;

            // Si la IA está activada, iniciar Docker
            if (aiEnabled)
            {
                ShowPlayLoading("Iniciando servicios de IA...");
                await Dispatcher.Yield();

                var dockerWindow = new DockerProgressWindow { Owner = this, OllamaModel = "llama3.2:3b" };
                var dockerResult = await dockerWindow.RunAsync();

                if (dockerResult.Canceled)
                {
                    HidePlayLoading();
                    _isPlayRunning = false;
                    return;
                }

                if (!dockerResult.Success)
                {
                    aiEnabled = false;
                    new AlertWindow(
                        "No se han podido iniciar los servicios de IA y voz.\n\n" +
                        "Comprueba que Docker Desktop está instalado y en ejecución.\n\n" +
                        "Continuando sin IA...",
                        "Aviso")
                    {
                        Owner = this
                    }.ShowDialog();
                }
            }

            // Crear SoundManager con la configuración del modo pruebas
            _testSoundManager = new SoundManager
            {
                SoundEnabled = soundEnabled,
                MusicVolume = (float)(_world.Game.TestModeMusicVolume / 10.0),
                EffectsVolume = (float)(_world.Game.TestModeEffectsVolume / 10.0),
                MasterVolume = (float)(_world.Game.TestModeMasterVolume / 10.0),
                VoiceVolume = (float)(_world.Game.TestModeVoiceVolume / 10.0)
            };
            _testSoundManager.RefreshVolumes();

            // Guardar si la IA está activa para el modo pruebas
            _testAiEnabled = aiEnabled;

            // Crear el engine (con modo debug activo para el editor)
            _testEngine = new GameEngine(_testWorld, state, _testSoundManager, isDebugMode: true);
            _testEngine.ScriptMessage += msg => Dispatcher.Invoke(() => HandleTestScriptMessage(msg));
            _testEngine.AdventureCompleted += () => Dispatcher.Invoke(ShowEndingWindow);
            _testEngine.CombatStarted += npcId => Dispatcher.Invoke(() => HandleTestCombatStarted(npcId));
            _testEngine.TradeOpened += npc => Dispatcher.Invoke(() => HandleTestTradeOpened(npc));
            _testEngine.ConversationDialogue += msg => Dispatcher.Invoke(() => HandleTestConversationDialogue(msg));
            _testEngine.ConversationOptions += options => Dispatcher.Invoke(() => HandleTestConversationOptions(options));
            _testEngine.ConversationEnded += () => Dispatcher.Invoke(HandleTestConversationEnded);
            _testEngine.HelpRequested += () => Dispatcher.Invoke(HandleTestHelpRequested);

            // Si hay sonido y voz, precargar la descripción de la sala inicial
            if (soundEnabled && _world.Game.TestModeVoiceVolume > 0 && aiEnabled)
            {
                try
                {
                    var startRoom = state.Rooms
                        .FirstOrDefault(r => r.Id.Equals(state.CurrentRoomId, StringComparison.OrdinalIgnoreCase));
                    if (startRoom != null && !string.IsNullOrWhiteSpace(startRoom.Description))
                    {
                        await _testSoundManager.PreloadRoomVoiceAsync(startRoom.Id, startRoom.Description);
                    }
                }
                catch
                {
                    // Si falla la precarga de voz, continuar sin interrumpir
                }
            }

            // Mostrar el panel (descripción ya visible en el panel, no repetir en consola)
            ShowTestPanel();
            UpdateTestDisplay(showRoomDescription: false);

            HidePlayLoading();
            TestInputTextBox.Focus();
        }
        catch (Exception ex)
        {
            HidePlayLoading();
            _isPlayRunning = false;
            new AlertWindow($"Error al preparar la partida de prueba:\n{ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void ShowTestPanel()
    {
        TestPanelColumn.Width = new GridLength(520);
        TestPanel.Visibility = Visibility.Visible;
        TestOutputTextBox.Document.Blocks.Clear();

        // Aplicar fuente por defecto del juego (excepto stats)
        var fontFamily = new FontFamily(_world.Game.DefaultFontFamily);
        TestOutputTextBox.FontFamily = fontFamily;
        TestInputTextBox.FontFamily = fontFamily;
        TestRoomTitle.FontFamily = fontFamily;
        TestRoomDescription.FontFamily = fontFamily;

        // Iniciar timer para movimiento de NPCs basado en tiempo
        _testNpcMovementTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _testNpcMovementTimer.Tick += (_, _) =>
        {
            if (_testEngine != null)
            {
                _testEngine.UpdateNpcTimedMovement();
                UpdateTestDisplay();
            }
        };
        _testNpcMovementTimer.Start();
    }

    private void HideTestPanel()
    {
        // Detener timer de movimiento de NPCs
        _testNpcMovementTimer?.Stop();
        _testNpcMovementTimer = null;

        TestPanel.Visibility = Visibility.Collapsed;
        TestPanelColumn.Width = new GridLength(0);
        _testSoundManager?.StopMusic();
        _testSoundManager?.Dispose();
        _testSoundManager = null;
        _testEngine = null;
        _testWorld = null;
        _testAiEnabled = false;
        _isPlayRunning = false;
        MapPanel.SetTestPlayerRoom(null); // Quitar resplandor
        MapPanel.SetTestDoors(null); // Restaurar estado de puertas del editor
    }

    private void UpdateTestDisplay(bool showRoomDescription = false)
    {
        if (_testEngine == null || _testWorld == null) return;

        var state = _testEngine.State;
        var room = _testEngine.CurrentRoom;

        // Título y descripción completa (con objetos y NPCs)
        TestRoomTitle.Text = room?.Name ?? "Sala desconocida";
        TestRoomDescription.Text = _testEngine.DescribeCurrentRoom();

        // Actualizar resplandor en el mapa y estado de puertas
        MapPanel.SetTestPlayerRoom(state.CurrentRoomId);
        MapPanel.SetTestDoors(state.Doors);

        // Al entrar a una sala nueva, mostrar la descripción en la consola
        if (showRoomDescription && room != null)
        {
            AppendTestOutput(_testEngine.DescribeCurrentRoom());
        }

        // Imagen de sala (desde Base64)
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

        // Dinero
        TestMoneyLabel.Text = playerStats.Money.ToString("N0");

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
                .Select(id => _testWorld.Objects.FirstOrDefault(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                .Where(o => o != null)
                .Select(o => $"• {o!.Name}");
            TestInventoryLabel.Text = string.Join("\n", items);
        }

        // Estados de combate (visibles si están activos)
        if (_testWorld.Game.CombatEnabled)
        {
            TestCombatPanel.Visibility = Visibility.Visible;
            var dynamicStats = state.Player.DynamicStats;
            TestHealthBar.Value = dynamicStats.Health;
            TestHealthBar.Maximum = dynamicStats.MaxHealth;
            TestEnergyBar.Value = dynamicStats.Energy;
            TestSanityBar.Value = dynamicStats.Sanity;

            // Mana solo visible si MagicEnabled
            if (_testWorld.Game.MagicEnabled)
            {
                TestManaPanel.Visibility = Visibility.Visible;
                TestManaBar.Value = dynamicStats.Mana;
                TestManaBar.Maximum = dynamicStats.MaxMana;
            }
            else
            {
                TestManaPanel.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            TestCombatPanel.Visibility = Visibility.Collapsed;
        }

        // Necesidades básicas (visibles si están activas)
        if (_testWorld.Game.BasicNeedsEnabled)
        {
            TestBasicNeedsPanel.Visibility = Visibility.Visible;
            var dynamicStats = state.Player.DynamicStats;
            TestHungerBar.Value = dynamicStats.Hunger;
            TestThirstBar.Value = dynamicStats.Thirst;
            TestSleepBar.Value = dynamicStats.Sleep;
        }
        else
        {
            TestBasicNeedsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void AppendTestOutput(string text, bool isCommand = false)
    {
        Brush foreground;
        if (isCommand)
            foreground = new SolidColorBrush(Color.FromRgb(150, 200, 255));
        else if (text.StartsWith("[Error]", StringComparison.OrdinalIgnoreCase))
            foreground = new SolidColorBrush(Color.FromRgb(255, 100, 100)); // Rojo
        else if (text.StartsWith("[Debug]", StringComparison.OrdinalIgnoreCase))
            foreground = new SolidColorBrush(Color.FromRgb(255, 220, 100)); // Amarillo
        else
            foreground = Brushes.White;

        var paragraph = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, 0, 0, 4),
            Foreground = foreground
        };
        TestOutputTextBox.Document.Blocks.Add(paragraph);
        TestOutputTextBox.ScrollToEnd();
    }

    private void HandleTestScriptMessage(string msg)
    {
        // Los mensajes [Debug] solo se muestran si el toggle está activado
        // Los mensajes [Error] siempre se muestran
        if (msg.StartsWith("[Debug]", StringComparison.OrdinalIgnoreCase))
        {
            if (TestDebugToggle.IsChecked == true)
                AppendTestOutput(msg);
        }
        else
        {
            // Mensajes normales y de error siempre se muestran
            AppendTestOutput(msg);
        }
    }

    private void ShowEndingWindow()
    {
        if (_testWorld?.Game == null) return;

        // Buscar la música de finalización en la biblioteca
        string? endingMusicBase64 = null;
        if (!string.IsNullOrEmpty(_testWorld.Game.EndingMusicId))
        {
            var musicAsset = _testWorld.Musics.FirstOrDefault(m =>
                m.Id.Equals(_testWorld.Game.EndingMusicId, StringComparison.OrdinalIgnoreCase));
            endingMusicBase64 = musicAsset?.Base64;
        }

        var endingWindow = new EndingWindow
        {
            EndingText = _testWorld.Game.EndingText,
            LogoBase64 = null, // TODO: obtener logo del juego si existe
            MusicBase64 = endingMusicBase64,
            CloseApplicationOnExit = false // En modo pruebas no cerrar el editor
        };

        // Detener la música del modo pruebas
        _testSoundManager?.StopMusic();

        endingWindow.ShowDialog();

        // Mostrar mensaje de que la aventura ha terminado
        AppendTestOutput("\n[¡La aventura ha terminado!]\n");
    }

    private void HandleTestCombatStarted(string npcId)
    {
        if (_testEngine == null || _testWorld == null) return;

        // Buscar el NPC enemigo
        var enemy = _testEngine.State.Npcs.FirstOrDefault(n =>
            n.Id.Equals(npcId, StringComparison.OrdinalIgnoreCase));

        if (enemy == null)
        {
            AppendTestOutput("Error: No se encontro el enemigo para el combate.");
            return;
        }

        // Obtener el inventario del jugador para el combate
        var playerInventory = _testEngine.State.InventoryObjectIds
            .Select(id => _testEngine.State.Objects.FirstOrDefault(o => o.Id == id))
            .Where(o => o != null)
            .Cast<GameObject>()
            .ToList();

        // Crear el motor de combate
        var combatEngine = new CombatEngine(_testEngine.State);

        // Crear y mostrar la ventana de combate
        var combatWindow = new CombatWindow(
            combatEngine, _testEngine.State, enemy, playerInventory, _testWorld.Game.MagicEnabled)
        {
            Owner = this
        };

        combatWindow.CombatEnded += reason =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                UpdateTestDisplay();
                var resultMsg = reason switch
                {
                    CombatEndReason.Victory => $"Has derrotado a {enemy.Name}.",
                    CombatEndReason.Defeat => "Has sido derrotado.",
                    CombatEndReason.Fled => "Has huido del combate.",
                    CombatEndReason.EnemyFled => $"{enemy.Name} ha huido.",
                    _ => "El combate ha terminado."
                };
                AppendTestOutput(resultMsg);
            });
        };

        combatWindow.ShowDialog();
    }

    private void HandleTestTradeOpened(Npc merchant)
    {
        if (_testEngine == null || _testWorld == null) return;

        // Crear el motor de comercio
        var tradeEngine = new TradeEngine(_testEngine.State);

        // Crear y mostrar la ventana de comercio
        var tradeWindow = new TradeWindow(tradeEngine, _testEngine.State, merchant)
        {
            Owner = this
        };

        tradeWindow.TradeClosed += () =>
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Notificar al ConversationEngine que la tienda cerro
                _testEngine.CloseShop();

                // Actualizar UI del modo prueba
                UpdateTestDisplay();
            });
        };

        tradeWindow.ShowDialog();
    }

    private void HandleTestConversationDialogue(ConversationMessage message)
    {
        var emotionStr = message.Emotion != "Neutral" ? $" ({message.Emotion})" : "";
        var speaker = message.IsNpc ? message.SpeakerName : "Tú";
        var formattedText = $"[{speaker}{emotionStr}]: \"{message.Text}\"";
        AppendTestOutput(formattedText);
    }

    private void HandleTestConversationOptions(List<DialogueOption> options)
    {
        if (options == null || options.Count == 0) return;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("\n¿Qué dices?");
        foreach (var option in options)
        {
            var prefix = option.IsEnabled ? $"  [{option.Index + 1}]" : $"  (×)";
            var suffix = !option.IsEnabled && !string.IsNullOrEmpty(option.DisabledReason)
                ? $" - {option.DisabledReason}"
                : "";
            sb.AppendLine($"{prefix} {option.Text}{suffix}");
        }
        sb.AppendLine("\n(Escribe el número de tu elección o 'salir' para terminar)");
        AppendTestOutput(sb.ToString());
    }

    private void HandleTestConversationEnded()
    {
        AppendTestOutput("\n[Fin de la conversación]\n");
    }

    private void HandleTestHelpRequested()
    {
        var helpWindow = new HelpWindow
        {
            Owner = this
        };
        helpWindow.ShowDialog();
    }

    private void TestDebugToggle_Changed(object sender, RoutedEventArgs e)
    {
        if (TestDebugIcon == null) return;
        TestDebugIcon.Foreground = TestDebugToggle.IsChecked == true
            ? new SolidColorBrush(Color.FromRgb(255, 220, 100)) // Amarillo
            : new SolidColorBrush(Color.FromRgb(136, 136, 136)); // Gris
    }

    private System.Windows.Threading.DispatcherTimer? _testMessageTimer;
    private Paragraph? _currentTestSystemMessage;

    private void AppendTestSystemMessage(string text)
    {
        // Eliminar mensaje anterior si existe
        if (_currentTestSystemMessage != null && TestOutputTextBox.Document.Blocks.Contains(_currentTestSystemMessage))
        {
            TestOutputTextBox.Document.Blocks.Remove(_currentTestSystemMessage);
        }
        _testMessageTimer?.Stop();

        var paragraph = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0, 0, 0, 4),
            Foreground = new SolidColorBrush(Color.FromRgb(100, 180, 255))
        };
        TestOutputTextBox.Document.Blocks.Add(paragraph);
        TestOutputTextBox.ScrollToEnd();
        _currentTestSystemMessage = paragraph;

        // Timer para eliminar después de 2 segundos
        _testMessageTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _testMessageTimer.Tick += (_, _) =>
        {
            _testMessageTimer.Stop();
            if (_currentTestSystemMessage != null && TestOutputTextBox.Document.Blocks.Contains(_currentTestSystemMessage))
            {
                TestOutputTextBox.Document.Blocks.Remove(_currentTestSystemMessage);
                _currentTestSystemMessage = null;
            }
        };
        _testMessageTimer.Start();
    }

    private void TestPanel_Close_Click(object sender, RoutedEventArgs e)
    {
        HideTestPanel();
    }

    private void TestPanel_Restart_Click(object sender, RoutedEventArgs e)
    {
        if (_testWorld == null || _currentPath == null || _testSoundManager == null) return;

        try
        {
            var state = WorldLoader.CreateInitialState(_testWorld);
            _testEngine = new GameEngine(_testWorld, state, _testSoundManager, isDebugMode: true);
            _testEngine.ScriptMessage += msg => Dispatcher.Invoke(() => HandleTestScriptMessage(msg));
            _testEngine.AdventureCompleted += () => Dispatcher.Invoke(ShowEndingWindow);
            _testEngine.CombatStarted += npcId => Dispatcher.Invoke(() => HandleTestCombatStarted(npcId));
            _testEngine.TradeOpened += npc => Dispatcher.Invoke(() => HandleTestTradeOpened(npc));
            _testEngine.ConversationDialogue += msg => Dispatcher.Invoke(() => HandleTestConversationDialogue(msg));
            _testEngine.ConversationOptions += options => Dispatcher.Invoke(() => HandleTestConversationOptions(options));
            _testEngine.ConversationEnded += () => Dispatcher.Invoke(HandleTestConversationEnded);
            _testEngine.HelpRequested += () => Dispatcher.Invoke(HandleTestHelpRequested);
            TestOutputTextBox.Document.Blocks.Clear();
            _testCommandHistory.Clear();
            _testHistoryIndex = -1;
            UpdateTestDisplay(showRoomDescription: true);
            TestInputTextBox.Focus();
        }
        catch
        {
            // Ignorar errores
        }
    }

    private void TestInputTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var command = TestInputTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(command))
            {
                ProcessTestCommand(command);
                _testCommandHistory.Add(command);
                _testHistoryIndex = _testCommandHistory.Count;
            }
            TestInputTextBox.Clear();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            // Historial hacia atrás
            if (_testHistoryIndex > 0)
            {
                _testHistoryIndex--;
                TestInputTextBox.Text = _testCommandHistory[_testHistoryIndex];
                TestInputTextBox.CaretIndex = TestInputTextBox.Text.Length;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            // Historial hacia adelante
            if (_testHistoryIndex < _testCommandHistory.Count - 1)
            {
                _testHistoryIndex++;
                TestInputTextBox.Text = _testCommandHistory[_testHistoryIndex];
                TestInputTextBox.CaretIndex = TestInputTextBox.Text.Length;
            }
            else if (_testHistoryIndex == _testCommandHistory.Count - 1)
            {
                _testHistoryIndex = _testCommandHistory.Count;
                TestInputTextBox.Clear();
            }
            e.Handled = true;
        }
    }

    private void ProcessTestCommand(string command)
    {
        if (_testEngine == null) return;

        // Comando especial: limpiar consola
        if (command.Equals("cls", StringComparison.OrdinalIgnoreCase) ||
            command.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            TestOutputTextBox.Document.Blocks.Clear();
            return;
        }

        AppendTestOutput($"> {command}", isCommand: true);

        var result = _testEngine.ProcessCommand(command);

        if (!string.IsNullOrEmpty(result.Message))
        {
            AppendTestOutput(result.Message);
        }

        // Añadir separador visual
        var separator = new Paragraph(new Run("─────────────────────────"))
        {
            Margin = new Thickness(0, 2, 0, 6),
            Foreground = new SolidColorBrush(Color.FromRgb(60, 60, 60))
        };
        TestOutputTextBox.Document.Blocks.Add(separator);
        TestOutputTextBox.ScrollToEnd();

        UpdateTestDisplay();
    }

    private void ShowPlayLoading(string message)
    {
        PlayLoadingText.Text = message;
        PlayLoadingOverlay.Visibility = Visibility.Visible;
    }

    private void HidePlayLoading()
    {
        PlayLoadingOverlay.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Valida que la clave de encriptación tenga el formato correcto.
    /// Retorna true si es válida, false si no lo es.
    /// </summary>
    private bool ValidateEncryptionKey()
    {
        if (_world == null)
            return true;

        // Forzar la actualización del valor de la clave de encriptación antes de validar
        PropertyEditor.UpdateEncryptionKey(_world.Game);

        var key = _world.Game.EncryptionKey;
        if (!string.IsNullOrWhiteSpace(key) && key.Trim().Length != 8)
        {
            new AlertWindow("La 'Clave de cifrado' debe tener exactamente 8 caracteres o dejarse vacía para usar la clave por defecto.",
                            "Clave incorrecta")
            {
                Owner = this
            }.ShowDialog();

            // Intentar seleccionar el nodo juego para facilitar al usuario corregirlo
            SelectGameTreeNode();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que las características del jugador sumen exactamente 100 puntos.
    /// Retorna true si es válido, false si no lo es.
    /// </summary>
    private bool ValidatePlayerAttributes()
    {
        if (_world?.Player == null)
            return true;

        var total = _world.Player.TotalAttributePoints;
        if (total != 100)
        {
            new AlertWindow(
                $"Las características del jugador deben sumar exactamente 100 puntos.\n\n" +
                $"Suma actual: {total} puntos\n" +
                $"Diferencia: {(total > 100 ? "+" : "")}{total - 100} puntos",
                "Características incorrectas")
            {
                Owner = this
            }.ShowDialog();

            // Seleccionar el nodo Jugador para facilitar la corrección
            SelectPlayerInTree();
            return false;
        }

        // Validar y corregir dinero inicial si es negativo
        if (_world.Player.InitialMoney < 0)
        {
            _world.Player.InitialMoney = 0;
        }

        return true;
    }

    /// <summary>
    /// Valida que el mundo tenga al menos una sala y que el jugador esté asignado a una sala existente.
    /// </summary>
    private bool ValidateWorldMinimumRequirements()
    {
        if (_world == null)
            return false;

        // Debe haber al menos una sala
        if (_world.Rooms.Count == 0)
        {
            new AlertWindow(
                "El mundo debe tener al menos una sala.\n\n" +
                "Añade una sala antes de guardar.",
                "Mundo incompleto")
            {
                Owner = this
            }.ShowDialog();
            return false;
        }

        // El jugador debe tener una sala inicial válida
        var startRoomId = _world.Game.StartRoomId;
        if (string.IsNullOrWhiteSpace(startRoomId))
        {
            new AlertWindow(
                "Debes asignar una sala inicial al jugador.\n\n" +
                "Selecciona el nodo 'Juego' y configura la propiedad 'Sala inicial'.",
                "Mundo incompleto")
            {
                Owner = this
            }.ShowDialog();
            SelectGameTreeNode();
            return false;
        }

        // La sala inicial debe existir
        if (!_world.Rooms.Any(r => r.Id == startRoomId))
        {
            new AlertWindow(
                $"La sala inicial '{startRoomId}' no existe en el mundo.\n\n" +
                "Selecciona una sala inicial válida en las propiedades del juego.",
                "Mundo incompleto")
            {
                Owner = this
            }.ShowDialog();
            SelectGameTreeNode();
            return false;
        }

        return true;
    }

    /// <summary>
    /// Selecciona el nodo Jugador en el árbol.
    /// </summary>
    private void SelectPlayerInTree()
    {
        if (_world?.Player == null) return;

        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString() == "Juego")
            {
                root.IsExpanded = true;
                foreach (TreeViewItem child in root.Items.OfType<TreeViewItem>())
                {
                    if (child.Tag == _world.Player)
                    {
                        WorldTree.Focus();
                        child.IsSelected = true;
                        child.BringIntoView();
                        child.Focus();
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Valida que todas las puertas tengan coherencia entre cerradura y llave.
    /// - Puerta con cerradura debe tener llave asignada.
    /// - Puerta con llave asignada debe tener cerradura activa.
    /// Retorna true si es válido, false si no lo es.
    /// </summary>
    private bool ValidateDoorKeys()
    {
        if (_world?.Doors == null)
            return true;

        // Puertas con cerradura pero sin llave
        var doorsWithoutKey = _world.Doors
            .Where(d => d.IsLocked && string.IsNullOrWhiteSpace(d.KeyObjectId))
            .ToList();

        if (doorsWithoutKey.Count > 0)
        {
            var doorNames = string.Join("\n• ", doorsWithoutKey.Select(d =>
                string.IsNullOrWhiteSpace(d.Name) ? d.Id : d.Name));

            new AlertWindow(
                $"Las siguientes puertas tienen cerradura pero no tienen llave asignada:\n\n• {doorNames}\n\nAsigna una llave o desactiva la cerradura.",
                "Puertas sin llave")
            {
                Owner = this
            }.ShowDialog();

            SelectDoorInTree(doorsWithoutKey[0]);
            PropertyEditor.SetObject(doorsWithoutKey[0]);
            return false;
        }

        // Puertas con llave asignada pero sin cerradura activa
        var doorsWithKeyNoLock = _world.Doors
            .Where(d => !d.IsLocked && !string.IsNullOrWhiteSpace(d.KeyObjectId))
            .ToList();

        if (doorsWithKeyNoLock.Count > 0)
        {
            var doorNames = string.Join("\n• ", doorsWithKeyNoLock.Select(d =>
                string.IsNullOrWhiteSpace(d.Name) ? d.Id : d.Name));

            new AlertWindow(
                $"Las siguientes puertas tienen llave asignada pero no tienen cerradura activa:\n\n• {doorNames}\n\nActiva la cerradura o quita la llave asignada.",
                "Puertas con llave sin cerradura")
            {
                Owner = this
            }.ShowDialog();

            SelectDoorInTree(doorsWithKeyNoLock[0]);
            PropertyEditor.SetObject(doorsWithKeyNoLock[0]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que los NPCs con patrulla activa tengan una ruta definida (al menos 2 salas).
    /// </summary>
    private bool ValidateNpcPatrols()
    {
        if (_world?.Npcs == null)
            return true;

        // NPCs con patrulla activa pero sin ruta definida (o con menos de 2 puntos)
        var npcsWithoutRoute = _world.Npcs
            .Where(n => n.IsPatrolling && n.PatrolRoute.Count < 2)
            .ToList();

        if (npcsWithoutRoute.Count > 0)
        {
            var npcNames = string.Join("\n• ", npcsWithoutRoute.Select(n =>
                string.IsNullOrWhiteSpace(n.Name) ? n.Id : n.Name));

            new AlertWindow(
                $"Los siguientes NPCs tienen patrulla activa pero no tienen ruta definida (mínimo 2 salas):\n\n• {npcNames}\n\n" +
                "Define una ruta de patrulla o desactiva la opción 'Está patrullando'.",
                "NPCs sin ruta de patrulla")
            {
                Owner = this
            }.ShowDialog();

            SelectNpcInTree(npcsWithoutRoute[0]);
            PropertyEditor.SetObject(npcsWithoutRoute[0]);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Valida que todas las salas tengan una zona asignada.
    /// </summary>
    private bool ValidateRoomZones()
    {
        if (_world?.Rooms == null)
            return true;

        var roomsWithoutZone = _world.Rooms
            .Where(r => string.IsNullOrEmpty(r.Zone))
            .ToList();

        if (roomsWithoutZone.Count > 0)
        {
            var roomNames = string.Join("\n• ", roomsWithoutZone.Select(r =>
                string.IsNullOrWhiteSpace(r.Name) ? r.Id : r.Name));

            new AlertWindow(
                $"Las siguientes salas no tienen zona asignada:\n\n• {roomNames}\n\nTodas las salas deben pertenecer a una zona.",
                "Salas sin zona")
            {
                Owner = this
            }.ShowDialog();

            SelectRoomInTree(roomsWithoutZone[0]);
            PropertyEditor.SetObject(roomsWithoutZone[0]);
            return false;
        }

        return true;
    }

    private async void SaveMenu_Click(object sender, RoutedEventArgs e)
    {
        await PerformSaveAsync();
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformSaveAsync();
    }

    private async Task<bool> PerformSaveAsync(bool hideLoadingOnComplete = true)
    {
        // Simulamos sender/e para reaprovechar lógica existente si fuera necesario,
        // aunque idealmente SaveAsMenu_Click debería refactorizarse también.
        // Por ahora mantenemos la compatibilidad con el resto del código.
        var sender = this;
        var e = new RoutedEventArgs();

        if (string.IsNullOrEmpty(_currentPath))
        {
            SaveAsMenu_Click(sender, e);
            return false; // No sabemos si el usuario guardó o canceló
        }

        // Aplicar validaciones pendientes del PropertyEditor antes de guardar
        PropertyEditor.ApplyPendingValidations();

        // Validar requisitos mínimos del mundo
        if (!ValidateWorldMinimumRequirements())
            return false;

        // Validar clave de encriptación
        if (!ValidateEncryptionKey())
            return false;

        // Validar características del jugador
        if (!ValidatePlayerAttributes())
            return false;

        // Validar puertas con cerradura tengan llave asignada
        if (!ValidateDoorKeys())
            return false;

        // Validar NPCs con patrulla tengan ruta definida
        if (!ValidateNpcPatrols())
            return false;

        // Validar que todas las salas tengan zona asignada
        if (!ValidateRoomZones())
            return false;

        // Si la IA está activada, determinar géneros gramaticales antes de guardar
        if (_useLlmForGenders)
        {
            await ApplyLlmGendersAsync();
            // Refrescar el PropertyEditor para mostrar los cambios del LLM
            RefreshPropertyEditor();
        }

        ShowPlayLoading("Guardando mundo...");

        try
        {
            await Task.Run(() =>
            {
                // Antes de guardar, sincronizamos las posiciones actuales del mapa con el modelo.
                if (_world != null)
                {
                    var roomIds = _world.Rooms.Select(r => r.Id);
                    var positions = MapPanel.GetRoomPositions(roomIds);

                    _world.RoomPositions ??= new Dictionary<string, MapPosition>();
                    _world.RoomPositions.Clear();

                    foreach (var kv in positions)
                    {
                        _world.RoomPositions[kv.Key] = new MapPosition
                        {
                            X = kv.Value.X,
                            Y = kv.Value.Y
                        };
                    }

                    // Guardar estado del grid y snap-to-grid
                    _world.ShowGrid = MapPanel.IsGridVisible;
                    _world.SnapToGrid = MapPanel.IsSnapToGridEnabled;

                    // Guardar configuración de IA
                    _world.UseLlmForGenders = _useLlmForGenders;
                }

                Directory.CreateDirectory(AppPaths.WorldsFolder);
                WorldLoader.SaveWorldModel(_world!, _currentPath);
            });

            SetDirty(false);
            if (hideLoadingOnComplete)
                HidePlayLoading();
            return true;
        }
        catch (Exception ex)
        {
            HidePlayLoading();
            new AlertWindow($"Error al guardar mundo:\n{ex.Message}", "Error") { Owner = this }.ShowDialog();
            return false;
        }
    }

    /// <summary>
    /// Guarda el mundo de forma síncrona (para usar en OnClosing).
    /// No muestra overlay de carga para evitar deadlock.
    /// </summary>
    private bool PerformSave()
    {
        if (string.IsNullOrEmpty(_currentPath))
        {
            SaveAsMenu_Click(this, new RoutedEventArgs());
            return !_isDirty; // Si ya no está dirty, se guardó correctamente
        }

        // Aplicar validaciones pendientes del PropertyEditor antes de guardar
        PropertyEditor.ApplyPendingValidations();

        // Validar requisitos mínimos del mundo
        if (!ValidateWorldMinimumRequirements())
            return false;

        // Validar clave de encriptación (ya se valida en OnClosing, pero por si acaso)
        if (!ValidateEncryptionKey())
            return false;

        // Validar características del jugador
        if (!ValidatePlayerAttributes())
            return false;

        // Validar puertas con cerradura tengan llave asignada
        if (!ValidateDoorKeys())
            return false;

        // Validar NPCs con patrulla tengan ruta definida
        if (!ValidateNpcPatrols())
            return false;

        // Validar que todas las salas tengan zona asignada
        if (!ValidateRoomZones())
            return false;

        try
        {
            // Sincronizar posiciones del mapa con el modelo
            if (_world != null)
            {
                var roomIds = _world.Rooms.Select(r => r.Id);
                var positions = MapPanel.GetRoomPositions(roomIds);

                _world.RoomPositions ??= new Dictionary<string, MapPosition>();
                _world.RoomPositions.Clear();

                foreach (var kv in positions)
                {
                    _world.RoomPositions[kv.Key] = new MapPosition
                    {
                        X = kv.Value.X,
                        Y = kv.Value.Y
                    };
                }

                // Guardar estado del grid y snap-to-grid
                _world.ShowGrid = MapPanel.IsGridVisible;
                _world.SnapToGrid = MapPanel.IsSnapToGridEnabled;

                // Guardar configuración de IA
                _world.UseLlmForGenders = _useLlmForGenders;
            }

            Directory.CreateDirectory(AppPaths.WorldsFolder);
            WorldLoader.SaveWorldModel(_world!, _currentPath);

            SetDirty(false);
            return true;
        }
        catch (Exception ex)
        {
            new AlertWindow($"Error al guardar mundo:\n{ex.Message}", "Error") { Owner = this }.ShowDialog();
            return false;
        }
    }

    private void SaveAsMenu_Click(object sender, RoutedEventArgs e)
    {
        // Aplicar validaciones pendientes del PropertyEditor antes de guardar
        PropertyEditor.ApplyPendingValidations();

        // Validar requisitos mínimos del mundo
        if (!ValidateWorldMinimumRequirements())
            return;

        if (!ValidateEncryptionKey())
            return;

        if (!ValidatePlayerAttributes())
            return;

        if (!ValidateDoorKeys())
            return;

        if (!ValidateRoomZones())
            return;

        var dlg = new SaveFileDialog
        {
            Title = "Guardar mundo",
            Filter = "Mundos (*.xaw)|*.xaw|Todos los archivos (*.*)|*.*",
            InitialDirectory = AppPaths.WorldsFolder,
            FileName = string.IsNullOrEmpty(_world.Game.Id) ? "nuevo_mundo.json" : _world.Game.Id + ".xaw"
        };

        if (dlg.ShowDialog(this) == true)
        {
            _currentPath = dlg.FileName;
            SaveMenu_Click(sender, e);
        }
    }

    private void CreateFromZonesMenu_Click(object sender, RoutedEventArgs e)
    {
        // Seleccionar múltiples archivos .json de zona
        var openDlg = new OpenFileDialog
        {
            Title = "Selecciona los archivos de zona a fusionar",
            Filter = "Archivos de zona (*.json)|*.json|Todos los archivos (*.*)|*.*",
            Multiselect = true
        };

        if (openDlg.ShowDialog(this) != true || openDlg.FileNames.Length == 0)
            return;

        if (openDlg.FileNames.Length < 2)
        {
            new AlertWindow("Debes seleccionar al menos 2 archivos de zona para fusionar.", "Error")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        // Ordenar archivos por nombre para mantener orden consistente
        var sortedFiles = openDlg.FileNames.OrderBy(f => System.IO.Path.GetFileName(f)).ToArray();

        // Seleccionar donde guardar el .xaw fusionado
        var saveDlg = new SaveFileDialog
        {
            Title = "Guardar mundo fusionado",
            Filter = "Mundos (*.xaw)|*.xaw",
            InitialDirectory = AppPaths.WorldsFolder,
            FileName = "mundo_fusionado.xaw"
        };

        if (saveDlg.ShowDialog(this) != true)
            return;

        try
        {
            WorldLoader.MergeAndSaveZoneFiles(sortedFiles, saveDlg.FileName);
            new AlertWindow($"Mundo fusionado correctamente:\n{System.IO.Path.GetFileName(saveDlg.FileName)}\n\n{sortedFiles.Length} zonas combinadas.", "Éxito")
            {
                Owner = this
            }.ShowDialog();

            // Preguntar si quiere abrir el mundo recién creado
            var openWorldDlg = new AlertWindow("¿Deseas abrir el mundo recién creado?", "Abrir mundo");
            openWorldDlg.ShowCancelButton(true);
            openWorldDlg.SetOkButtonText("Abrir");
            openWorldDlg.Owner = this;

            if (openWorldDlg.ShowDialog() == true)
            {
                TryLoadWorldWithPrompt(saveDlg.FileName);
                if (!IsCanceled)
                {
                    MapPanel.SetWorld(_world);
                    BuildTree();
                    UpdateZoneFilter();
                    ResetUndoRedo();
                }
            }
        }
        catch (Exception ex)
        {
            new AlertWindow($"Error al fusionar los mundos:\n{ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SaveMenu_Click(sender, e);
    }

    private void PlayCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        PlayButton_Click(sender, e);
    }

    private void ScriptEditorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        OpenScriptEditor_Click(sender, e);
    }

    private void CloseMenu_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void MusicMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_world == null)
        {
            new AlertWindow("No hay ningún mundo abierto.", "Gestión de Música")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        // Guardar el objeto actualmente seleccionado
        var currentObject = PropertyEditor.GetCurrentObject();

        var musicWindow = new MusicManagerWindow(_world)
        {
            Owner = this
        };
        musicWindow.ShowDialog();

        // Marcar como modificado si se han hecho cambios
        SetDirty(true);

        // Recargar el PropertyEditor para actualizar los combos de música
        if (currentObject != null)
        {
            PropertyEditor.SetObject(currentObject);
        }
    }

    private void FxMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_world == null)
        {
            new AlertWindow("No hay ningún mundo abierto.", "Gestión de FX")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        var fxWindow = new FxManagerWindow(_world)
        {
            Owner = this
        };
        fxWindow.ShowDialog();

        SetDirty(true);
    }

    private void AbilitiesMenu_Click(object sender, RoutedEventArgs e)
    {
        var abilityWindow = new AbilityManagerWindow(_world)
        {
            Owner = this
        };
        abilityWindow.ShowDialog();

        SetDirty(true);
    }

    private void PropertyEditor_RequestManageAbilities()
    {
        var abilityWindow = new AbilityManagerWindow(_world)
        {
            Owner = this
        };
        abilityWindow.ShowDialog();

        SetDirty(true);
    }

    private void GenerateDataMenu_Click(object sender, RoutedEventArgs e)
    {
        var generatorWindow = new AiDataGeneratorWindow(_world, _currentPath)
        {
            Owner = this
        };
        generatorWindow.ShowDialog();

        // Refresh UI after potential changes
        MapPanel.SetWorld(_world);
        BuildTree();
        var currentObject = PropertyEditor.GetCurrentObject();
        if (currentObject != null)
        {
            PropertyEditor.SetObject(currentObject);
        }
        SetDirty(true);
    }

    private void PromptGeneratorMenu_Click(object sender, RoutedEventArgs e)
    {
        var promptWindow = new PromptGeneratorWindow
        {
            Owner = this
        };
        promptWindow.ShowDialog();
    }

    private void TestModeOptionsMenu_Click(object sender, RoutedEventArgs e)
    {
        if (_world?.Game == null) return;

        var optionsWindow = new TestModeOptionsWindow
        {
            Owner = this,
            SoundEnabled = _world.Game.TestModeSoundEnabled,
            MusicVolume = _world.Game.TestModeMusicVolume,
            EffectsVolume = _world.Game.TestModeEffectsVolume,
            VoiceVolume = _world.Game.TestModeVoiceVolume,
            MasterVolume = _world.Game.TestModeMasterVolume,
            AiEnabled = _world.Game.TestModeAiEnabled
        };

        optionsWindow.ShowDialog();

        // Siempre guardar las opciones al cerrar la ventana
        _world.Game.TestModeSoundEnabled = optionsWindow.SoundEnabled;
        _world.Game.TestModeMusicVolume = optionsWindow.MusicVolume;
        _world.Game.TestModeEffectsVolume = optionsWindow.EffectsVolume;
        _world.Game.TestModeVoiceVolume = optionsWindow.VoiceVolume;
        _world.Game.TestModeMasterVolume = optionsWindow.MasterVolume;
        _world.Game.TestModeAiEnabled = optionsWindow.AiEnabled;
        SetDirty(true);
    }

    private void AboutMenu_Click(object sender, RoutedEventArgs e)
    {
        var aboutWindow = new AboutWindow
        {
            Owner = this
        };
        aboutWindow.ShowDialog();
    }

    private void AddZone_Click(object sender, RoutedEventArgs e)
    {
        // Generar nombre único para la zona
        var existingZones = _world.Rooms
            .Select(r => r.Zone ?? "")
            .Where(z => !string.IsNullOrEmpty(z))
            .Distinct()
            .ToList();

        var index = 1;
        var zoneName = "Nueva zona";
        while (existingZones.Contains(zoneName))
        {
            index++;
            zoneName = $"Nueva zona {index}";
        }

        // Crear sala placeholder para la zona (toda zona debe tener al menos una sala)
        var roomIndex = _world.Rooms.Count + 1;
        var room = new Room
        {
            Id = $"sala_{roomIndex}",
            Name = $"Sala de {zoneName}",
            Description = $"Sala inicial de la zona {zoneName}.",
            Zone = zoneName
        };
        _world.Rooms.Add(room);

        MapPanel.SetWorld(_world);
        BuildTree();
        UpdateZoneFilter();
        SelectZoneInTreeForEditing(zoneName);

        PushUndoSnapshot();
        SetDirty(true);
    }

    private void AddRoom_Click(object sender, RoutedEventArgs e)
    {
        // Determinar la zona para la nueva sala basándose en el filtro seleccionado
        string? targetZone = null;
        var selectedFilter = ZoneFilterComboBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(selectedFilter) && selectedFilter != "(Todas las zonas)")
        {
            targetZone = selectedFilter;
        }
        else
        {
            // Si no hay zona específica en el filtro, usar la del árbol o la primera disponible
            if (WorldTree.SelectedItem is TreeViewItem item)
            {
                if (item.Tag is ZoneNodeTag zoneTag)
                {
                    targetZone = zoneTag.ZoneName;
                }
                else if (item.Tag is Room selectedRoom)
                {
                    targetZone = selectedRoom.Zone;
                }
            }

            // Si aún no hay zona, usar la primera disponible o "Inicial"
            if (string.IsNullOrEmpty(targetZone))
            {
                targetZone = _world.Rooms
                    .Select(r => r.Zone)
                    .FirstOrDefault(z => !string.IsNullOrEmpty(z)) ?? "Inicial";
            }
        }

        var index = _world.Rooms.Count + 1;
        var room = new Room
        {
            Id = $"sala_{index}",
            Name = $"Sala {index}",
            Description = "Nueva sala.",
            Zone = targetZone
        };
        _world.Rooms.Add(room);
        MapPanel.SetWorld(_world);
        BuildTree();
        UpdateZoneFilter();
        SelectRoomInTree(room);

        PushUndoSnapshot();
        SetDirty(true);
    }

    private void AddDoor_Click(object sender, RoutedEventArgs e)
    {
        _world.Doors ??= new List<Door>();

        var index = _world.Doors.Count + 1;
        var door = new Door
        {
            Id = $"door_{index}",
            Name = $"Puerta {index}",
            Description = "Nueva puerta.",
            IsOpen = false,
            IsLocked = false,
            KeyObjectId = null,
            OpenFromSide = DoorOpenSide.Both
        };

        // Si hay una sala seleccionada, la usamos como RoomIdA por defecto
        if (WorldTree.SelectedItem is TreeViewItem item && item.Tag is Room room)
        {
            door.RoomIdA = room.Id;
        }

        _world.Doors.Add(door);
        BuildTree();
        SelectDoorInTree(door);
        PropertyEditor.SetObject(door);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void AddObject_Click(object sender, RoutedEventArgs e)
    {
        var index = _world.Objects.Count + 1;
        var obj = new GameObject
        {
            Id = $"obj_{index}",
            Name = $"Objeto {index}",
            Description = "Nuevo objeto.",
            CanTake = true,
            Visible = true
        };

        if (WorldTree.SelectedItem is TreeViewItem item && item.Tag is Room room)
        {
            obj.RoomId = room.Id;
        }

        _world.Objects.Add(obj);
        BuildTree();
        SelectObjectInTree(obj);
        PropertyEditor.SetObject(obj);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void AddNpc_Click(object sender, RoutedEventArgs e)
    {
        var index = _world.Npcs.Count + 1;
        var npc = new Npc
        {
            Id = $"npc_{index}",
            Name = $"PNJ {index}",
            Description = "Nuevo personaje.",
            Visible = true
        };

        if (WorldTree.SelectedItem is TreeViewItem item && item.Tag is Room room)
        {
            npc.RoomId = room.Id;
        }

        _world.Npcs.Add(npc);
        BuildTree();
        SelectNpcInTree(npc);
        PropertyEditor.SetObject(npc);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void AddQuest_Click(object sender, RoutedEventArgs e)
    {
        var index = _world.Quests.Count + 1;
        var q = new QuestDefinition
        {
            Id = $"quest_{index}",
            Name = $"Misión {index}",
            Description = "Nueva misión."
        };
        _world.Quests.Add(q);
        BuildTree();
        SelectQuestInTree(q);
        PropertyEditor.SetObject(q);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private static string NormalizeDirectionForRoom(string direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return string.Empty;

        var key = direction.Trim().ToLowerInvariant();
        return key switch
        {
            "n" or "norte" => "norte",
            "s" or "sur" => "sur",
            "e" or "este" => "este",
            "o" or "oeste" => "oeste",
            "ne" or "noreste" => "noreste",
            "no" or "noroeste" => "noroeste",
            "se" or "sureste" => "sureste",
            "so" or "suroeste" => "suroeste",
            "arriba" or "subir" => "arriba",
            "abajo" or "bajar" => "abajo",
            _ => direction.Trim()
        };
    }

    private static string? GetDirectionAbbreviation(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return null;

        var key = direction.Trim().ToLowerInvariant();
        return key switch
        {
            "n" or "norte" => "N",
            "s" or "sur" => "S",
            "e" or "este" => "E",
            "o" or "oeste" => "O",
            "ne" or "noreste" => "NE",
            "no" or "noroeste" => "NO",
            "se" or "sureste" => "SE",
            "so" or "suroeste" => "SO",
            "arriba" or "subir" => "AR",
            "abajo" or "bajar" => "AB",
            _ => null
        };
    }

    private static bool RoomHasExitInDirection(Room room, string direction)
    {
        var norm = NormalizeDirectionForRoom(direction);
        return room.Exits.Any(ex =>
            string.Equals(NormalizeDirectionForRoom(ex.Direction), norm, StringComparison.OrdinalIgnoreCase));
    }

    private void WorldTree_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            if (WorldTree.SelectedItem is TreeViewItem item)
            {
                HandleDeleteTreeItem(item);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F2)
        {
            if (WorldTree.SelectedItem is TreeViewItem item)
            {
                if (item.Tag is EditorFolder folder)
                {
                    StartFolderNameEdit(item, folder);
                    e.Handled = true;
                }
                else if (item.Tag is ZoneNodeTag zoneTag)
                {
                    StartZoneRename(item, zoneTag.ZoneName);
                    e.Handled = true;
                }
            }
        }
    }

    private TreeViewItem? _editingFolderItem;
    private EditorFolder? _editingFolder;
    private TextBox? _folderNameTextBox;

    private void StartFolderNameEdit(TreeViewItem item, EditorFolder folder)
    {
        _editingFolderItem = item;
        _editingFolder = folder;

        // Crear TextBox para edición inline
        _folderNameTextBox = new TextBox
        {
            Text = folder.Name,
            MinWidth = 100,
            Background = new SolidColorBrush(Color.FromRgb(45, 45, 48)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            CaretBrush = Brushes.White
        };

        _folderNameTextBox.KeyDown += FolderNameTextBox_KeyDown;
        _folderNameTextBox.LostFocus += FolderNameTextBox_LostFocus;

        // Reemplazar el header con el TextBox
        item.Header = _folderNameTextBox;

        // Seleccionar todo el texto y dar foco con prioridad alta
        Dispatcher.BeginInvoke(new Action(() =>
        {
            _folderNameTextBox?.SelectAll();
            _folderNameTextBox?.Focus();
            Keyboard.Focus(_folderNameTextBox);
        }), System.Windows.Threading.DispatcherPriority.Input);
    }

    private void FolderNameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitFolderNameEdit();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            CancelFolderNameEdit();
            e.Handled = true;
        }
    }

    private void FolderNameTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        // Al perder el foco, confirmar la edición
        CommitFolderNameEdit();
    }

    private void CommitFolderNameEdit()
    {
        if (_editingFolder == null || _editingFolderItem == null || _folderNameTextBox == null)
            return;

        var newName = _folderNameTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(newName))
        {
            _editingFolder.Name = newName;
            SetDirty(true);
        }

        // Restaurar el header normal
        _editingFolderItem.Header = $"📁 {_editingFolder.Name}";

        CleanupFolderEdit();
    }

    private void CancelFolderNameEdit()
    {
        if (_editingFolder == null || _editingFolderItem == null)
            return;

        // Restaurar el header sin cambiar el nombre
        _editingFolderItem.Header = $"📁 {_editingFolder.Name}";

        CleanupFolderEdit();
    }

    private void CleanupFolderEdit()
    {
        if (_folderNameTextBox != null)
        {
            _folderNameTextBox.KeyDown -= FolderNameTextBox_KeyDown;
            _folderNameTextBox.LostFocus -= FolderNameTextBox_LostFocus;
        }

        _editingFolderItem = null;
        _editingFolder = null;
        _folderNameTextBox = null;
    }

    private void HandleDeleteTreeItem(TreeViewItem item)
    {
        if (item?.Tag is null)
            return;

        switch (item.Tag)
        {
            case Room room:
                DeleteRoom(room);
                break;
            case GameObject obj:
                DeleteObject(obj);
                break;
            case Npc npc:
                DeleteNpc(npc);
                break;
            case QuestDefinition quest:
                DeleteQuest(quest);
                break;
            case Door door:
                DeleteDoor(door);
                break;
            case EditorFolder folder:
                DeleteFolder(folder);
                break;
            case ZoneNodeTag zoneTag:
                DeleteZone(zoneTag.ZoneName);
                break;
        }
    }

    private void DeleteFolder(EditorFolder folder)
    {
        if (folder is null) return;

        // Mover los items de la carpeta a la raíz (liberar asociaciones)
        folder.ItemIds.Clear();
        // Eliminar subcarpetas recursivamente
        DeleteFolderRecursive(folder.Id);
        _world.Folders.Remove(folder);
        BuildTree();
        SetDirty(true);
    }

    private void DeleteZone(string zoneName)
    {
        // Verificar que no sea la última zona
        var totalZones = _world.Rooms
            .Select(r => r.Zone)
            .Where(z => !string.IsNullOrEmpty(z))
            .Distinct()
            .Count();

        if (totalZones <= 1)
        {
            new AlertWindow("No se puede eliminar la última zona del mundo.", "Eliminar zona")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        var roomsInZone = _world.Rooms.Count(r => r.Zone == zoneName);

        if (roomsInZone > 0)
        {
            // Mostrar diálogo oscuro con opciones
            var result = ShowZoneDeleteDialog(zoneName, roomsInZone);

            if (result == ZoneDeleteOption.MoveRooms)
            {
                // Mover salas a la primera zona disponible
                var otherZone = _world.Rooms
                    .Select(r => r.Zone)
                    .FirstOrDefault(z => !string.IsNullOrEmpty(z) && z != zoneName) ?? "Inicial";

                foreach (var room in _world.Rooms.Where(r => r.Zone == zoneName))
                {
                    room.Zone = otherZone;
                }
            }
            else if (result == ZoneDeleteOption.DeleteRooms)
            {
                // Eliminar las salas de la zona
                var roomsToRemove = _world.Rooms.Where(r => r.Zone == zoneName).ToList();
                foreach (var room in roomsToRemove)
                {
                    _world.Rooms.Remove(room);
                    _world.RoomPositions.Remove(room.Id);
                }
            }
            else
            {
                return; // Cancelar
            }
        }

        MapPanel.SetWorld(_world);
        BuildTree();
        UpdateZoneFilter();
        PushUndoSnapshot();
        SetDirty(true);
    }

    private enum ZoneDeleteOption { Cancel, MoveRooms, DeleteRooms }

    private ZoneDeleteOption ShowZoneDeleteDialog(string zoneName, int roomCount)
    {
        var result = ZoneDeleteOption.Cancel;

        var dialog = new Window
        {
            Title = "Eliminar zona",
            Width = 420,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.None,
            Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
            AllowsTransparency = true
        };

        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };

        var mainGrid = new Grid();
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var messagePanel = new StackPanel
        {
            Margin = new Thickness(20),
            VerticalAlignment = VerticalAlignment.Center
        };

        messagePanel.Children.Add(new TextBlock
        {
            Text = $"La zona '{zoneName}' contiene {roomCount} sala(s).",
            Foreground = Brushes.White,
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });

        messagePanel.Children.Add(new TextBlock
        {
            Text = "¿Qué deseas hacer con las salas?",
            Foreground = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
            FontSize = 13
        });

        Grid.SetRow(messagePanel, 0);
        mainGrid.Children.Add(messagePanel);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(20, 10, 20, 20)
        };

        var moveButton = new Button
        {
            Content = "Mover a otra zona",
            Padding = new Thickness(15, 8, 15, 8),
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(0, 122, 204)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        moveButton.Click += (s, e) => { result = ZoneDeleteOption.MoveRooms; dialog.Close(); };

        var deleteButton = new Button
        {
            Content = "Eliminar salas",
            Padding = new Thickness(15, 8, 15, 8),
            Margin = new Thickness(0, 0, 10, 0),
            Background = new SolidColorBrush(Color.FromRgb(180, 60, 60)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        deleteButton.Click += (s, e) => { result = ZoneDeleteOption.DeleteRooms; dialog.Close(); };

        var cancelButton = new Button
        {
            Content = "Cancelar",
            Padding = new Thickness(15, 8, 15, 8),
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsCancel = true
        };
        cancelButton.Click += (s, e) => { result = ZoneDeleteOption.Cancel; dialog.Close(); };

        buttonsPanel.Children.Add(moveButton);
        buttonsPanel.Children.Add(deleteButton);
        buttonsPanel.Children.Add(cancelButton);

        Grid.SetRow(buttonsPanel, 1);
        mainGrid.Children.Add(buttonsPanel);

        border.Child = mainGrid;
        dialog.Content = border;

        // Permitir arrastrar la ventana
        dialog.MouseLeftButtonDown += (s, e) => { if (e.LeftButton == MouseButtonState.Pressed) dialog.DragMove(); };

        dialog.ShowDialog();
        return result;
    }

    private void DeleteRoom(Room room)
    {
        if (room is null) return;

        var dlg = new ConfirmWindow($"¿Eliminar la sala '{room.Name}'?", "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        // Quitar salidas que apunten a esta sala
        foreach (var r in _world.Rooms)
        {
            r.Exits.RemoveAll(ex => string.Equals(ex.TargetRoomId, room.Id, StringComparison.OrdinalIgnoreCase));
        }

        // Quitar puertas que conecten con esta sala
        if (_world.Doors != null)
        {
            _world.Doors.RemoveAll(d =>
                string.Equals(d.RoomIdA, room.Id, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(d.RoomIdB, room.Id, StringComparison.OrdinalIgnoreCase));
        }

        // Desasociar objetos y NPCs de esta sala
        foreach (var obj in _world.Objects.Where(o => string.Equals(o.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
        {
            obj.RoomId = null;
        }

        foreach (var npc in _world.Npcs.Where(n => string.Equals(n.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
        {
            npc.RoomId = null;
        }

        // Limpiar sala de inicio si era esta
        if (string.Equals(_world.Game.StartRoomId, room.Id, StringComparison.OrdinalIgnoreCase))
        {
            _world.Game.StartRoomId = string.Empty;
        }

        _world.Rooms.Remove(room);

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

    }

    private void DeleteObject(GameObject obj)
    {
        if (obj is null) return;

        var dlg = new ConfirmWindow($"¿Eliminar el objeto '{obj.Name}'?", "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        WorldEditorHelpers.DeleteObject(_world, obj);

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

    }

    /// <summary>
    /// Elimina un objeto por su ID sin pedir confirmación (usada desde PropertyEditor).
    /// </summary>
    private void PropertyEditor_RequestDeleteObject(string objectId)
    {
        if (string.IsNullOrEmpty(objectId)) return;

        var obj = _world.Objects.FirstOrDefault(o => o.Id == objectId);
        if (obj == null) return;

        WorldEditorHelpers.DeleteObject(_world, obj);

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
    }

    /// <summary>
    /// Abre el editor de scripts para la entidad seleccionada actualmente en el árbol.
    /// </summary>
    private void OpenScriptEditor_Click(object sender, RoutedEventArgs e)
    {
        // Obtener la entidad seleccionada del árbol
        var selectedItem = WorldTree.SelectedItem as TreeViewItem;
        var entity = selectedItem?.Tag;

        if (entity == null)
        {
            new AlertWindow("Selecciona primero una entidad en el árbol para editar sus scripts.", "Sin selección") { Owner = this }.ShowDialog();
            return;
        }

        // Determinar el tipo, ID y nombre de la entidad
        string? ownerType = entity switch
        {
            GameInfo => "Game",
            Room => "Room",
            Door => "Door",
            Npc => "Npc",
            GameObject => "GameObject",
            QuestDefinition => "Quest",
            PlayerDefinition => null, // El jugador no tiene scripts
            _ => null
        };

        if (ownerType == null)
        {
            new AlertWindow("Esta entidad no soporta scripts.", "No soportado") { Owner = this }.ShowDialog();
            return;
        }

        string ownerId = entity switch
        {
            GameInfo game => game.Id,
            Room room => room.Id,
            Door door => door.Id,
            Npc npc => npc.Id,
            GameObject go => go.Id,
            QuestDefinition quest => quest.Id,
            _ => string.Empty
        };

        string ownerName = entity switch
        {
            GameInfo game => game.Title ?? "Juego",
            Room room => room.Name ?? room.Id,
            Door door => door.Name ?? door.Id,
            Npc npc => npc.Name ?? npc.Id,
            GameObject go => go.Name ?? go.Id,
            QuestDefinition quest => quest.Name ?? quest.Id,
            _ => string.Empty
        };

        var scriptEditor = new ScriptEditorWindow(_world, ownerType, ownerId, ownerName)
        {
            Owner = this,
            GetRooms = () => _world.Rooms,
            GetObjects = () => _world.Objects,
            GetNpcs = () => _world.Npcs,
            GetDoors = () => _world.Doors,
            GetQuests = () => _world.Quests,
            GetFxs = () => _world.Fxs
        };

        // Si estamos en modo prueba, pasar el engine para poder probar scripts
        if (_testEngine != null && _testWorld != null)
        {
            scriptEditor.SetTestMode(_testEngine, _testWorld, () =>
            {
                // Callback cuando el estado cambia en el editor de scripts
                UpdateTestDisplay();
            });
        }

        scriptEditor.ShowDialog();
        SetDirty(true);
    }

    /// <summary>
    /// Inicia el modo de edición de ruta de patrulla en el mapa.
    /// </summary>
    private void PropertyEditor_RequestEditPatrolRoute(Npc npc)
    {
        if (npc == null) return;
        MapPanel.StartEditingPatrolRoute(npc);
    }

    /// <summary>
    /// Manejador cuando la ruta de patrulla ha sido editada.
    /// </summary>
    private void MapPanel_PatrolRouteEdited(Npc npc)
    {
        SetDirty(true);
        // Actualizar el PropertyEditor si el NPC está seleccionado
        if (PropertyEditor.GetCurrentObject() == npc)
        {
            PropertyEditor.SetObject(npc);
        }
    }

    /// <summary>
    /// Genera una imagen con IA para una sala usando Stable Diffusion.
    /// </summary>
    private async void PropertyEditor_RequestAiImageGeneration(Room room)
    {
        if (room == null) return;

        try
        {
            // Verificar que Stable Diffusion esté disponible
            ShowPlayLoading("Verificando servidor...");

            try
            {
                // Verificar si SD está corriendo (la API solo acepta POST, así que 405 en GET significa que está activo)
                var healthCheck = await _sdHttpClient.GetAsync("/");
                // 405 Method Not Allowed significa que el servidor está activo pero no acepta GET
                if (!healthCheck.IsSuccessStatusCode && (int)healthCheck.StatusCode != 405)
                {
                    throw new HttpRequestException("Stable Diffusion no está disponible");
                }
            }
            catch (HttpRequestException)
            {
                // SD no está corriendo, intentar iniciarlo
                HidePlayLoading();

                var link = new TextBlock
                {
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var hyperlink = new Hyperlink
                {
                    NavigateUri = new Uri("https://docs.docker.com/desktop/setup/install/windows-install/")
                };
                hyperlink.Inlines.Add("Instala Docker Desktop");
                hyperlink.RequestNavigate += (_, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.AbsoluteUri,
                            UseShellExecute = true
                        });
                        e.Handled = true;
                    }
                    catch { }
                };
                link.Inlines.Add(hyperlink);

                var confirmDialog = new DarkConfirmDialog(
                    "Iniciar Stable Diffusion",
                    "El servidor de imágenes (Stable Diffusion) no está iniciado.\n\n" +
                    "¿Deseas iniciarlo ahora? (La primera vez puede tardar 10-20 minutos en descargar el modelo)\n\n" +
                    "Instala Docker Desktop si no lo has hecho ya.",
                    this);
                confirmDialog.SetCustomContent(link);

                if (confirmDialog.ShowDialog() != true)
                    return;

                // Iniciar Docker con SD
                var progressWindow = new DockerProgressWindow
                {
                    Owner = this,
                    IncludeTts = false,
                    IncludeStableDiffusion = true
                };

                var dockerResult = await progressWindow.RunAsync();
                if (!dockerResult.Success)
                {
                    DarkErrorDialog.Show("No se pudo iniciar el servidor de imágenes.", "Error", this);
                    return;
                }
            }

            ShowPlayLoading("Generando prompt con IA...");

            // Generar el prompt con Ollama
            var prompt = await GenerateImagePromptAsync(room);

            ShowPlayLoading("Generando imagen con IA...");

            // Llamar a la API de Stable Diffusion (gadicc/diffusers-api)
            // Optimized for RTX 3080 Ti with DPM++ scheduler and xformers
            var requestBody = new
            {
                modelInputs = new
                {
                    prompt = prompt,
                    negative_prompt = "text, watermark, signature, blurry, low quality, deformed",
                    width = 1408,
                    height = 320,
                    num_inference_steps = 10,   // DPM++ with Karras works well at 10 steps
                    guidance_scale = 7.0
                },
                callInputs = new
                {
                    MODEL_ID = "runwayml/stable-diffusion-v1-5",
                    PIPELINE = "StableDiffusionPipeline",
                    SCHEDULER = "DPMSolverMultistepScheduler",
                    use_karras_sigmas = true,
                    safety_checker = false,
                    requires_safety_checker = false,
                    enable_attention_slicing = false,
                    xformers_memory_efficient_attention = true,
                    torch_dtype = "float16"
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _sdHttpClient.PostAsync("/", content);

            if (!response.IsSuccessStatusCode)
            {
                var errorText = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Error de Stable Diffusion: {errorText}");
            }

            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);

            string? imageBase64 = null;

            // diffusers-api retorna image_base64
            if (doc.RootElement.TryGetProperty("image_base64", out var imageElement))
            {
                imageBase64 = imageElement.GetString();
            }
            else if (doc.RootElement.TryGetProperty("image", out var imgElement))
            {
                imageBase64 = imgElement.GetString();
            }
            else if (doc.RootElement.TryGetProperty("images", out var imagesElement) &&
                     imagesElement.ValueKind == JsonValueKind.Array &&
                     imagesElement.GetArrayLength() > 0)
            {
                imageBase64 = imagesElement[0].GetString();
            }

            if (string.IsNullOrEmpty(imageBase64))
            {
                throw new InvalidOperationException("La respuesta de Stable Diffusion no contiene imagen");
            }

            // Guardar la imagen en la sala
            room.ImageId = null; // Indicar que es generada por IA
            room.ImageBase64 = imageBase64;

            // Notificar cambios
            PropertyEditor.SetObject(room);
            MapPanel.InvalidateVisual();
            PushUndoSnapshot();
            SetDirty(true);

            HidePlayLoading();

            // Mostrar la imagen generada en el diálogo
            try
            {
                byte[]? imgBytes = null;
                if (imageBase64.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
                {
                    var commaIndex = imageBase64.IndexOf(',');
                    if (commaIndex >= 0)
                        imgBytes = Convert.FromBase64String(imageBase64[(commaIndex + 1)..]);
                }
                else
                {
                    imgBytes = Convert.FromBase64String(imageBase64);
                }

                if (imgBytes != null)
                {
                    using var ms = new MemoryStream(imgBytes);
                    var bmp = new System.Windows.Media.Imaging.BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bmp.StreamSource = ms;
                    bmp.EndInit();
                    bmp.Freeze();

                    var img = new Image
                    {
                        Source = bmp,
                        Width = 896,
                        Height = 256,
                        Stretch = Stretch.Uniform
                    };

                    var dlg = new AlertWindow("", "Generación completada")
                    {
                        Owner = this
                    };
                    dlg.SetCustomContent(img);
                    dlg.ShowDialog();
                }
                else
                {
                    AlertWindow.Show("Imagen generada correctamente.", this);
                }
            }
            catch
            {
                AlertWindow.Show("Imagen generada correctamente.", this);
            }
        }
        catch (Exception ex)
        {
            HidePlayLoading();
            DarkErrorDialog.Show("Error de generación", $"Error al generar la imagen:\n\n{ex.Message}", this);
        }
    }

    /// <summary>
    /// Construye el prompt para Stable Diffusion basado en la descripción de la sala y el tema del mundo.
    /// </summary>
    private async Task<string> GenerateImagePromptAsync(Room room)
    {
        var contextParts = new List<string>();

        if (!string.IsNullOrEmpty(_world.Game.Theme))
            contextParts.Add($"Theme: {_world.Game.Theme}");

        contextParts.Add($"Room name: {room.Name}");
        contextParts.Add(room.IsInterior ? "Interior scene" : "Exterior/outdoor scene");

        if (!room.IsIlluminated)
            contextParts.Add("Dark/dim lighting");

        if (!string.IsNullOrEmpty(room.Description))
            contextParts.Add($"Description: {room.Description}");

        var context = string.Join(". ", contextParts);

        var ollamaPrompt = $@"Translate the following to English and create a Stable Diffusion image prompt for 16-bit style. Output ONLY the prompt, nothing else.

{context}

STRICT RULES:
- TRANSLATE everything to English first (theme, room name, description)
- Output ONLY in English, absolutely no Spanish words
- Maximum 40 words
- Comma-separated descriptive terms
- MUST include: 16-bit videogame
- Include: detailed, panoramic view
- Focus on: colors, lighting, mood, architecture
- NO explanations, NO prefixes like 'Here is', just raw prompt text";

        try
        {
            var requestBody = new
            {
                model = "llama3",
                prompt = ollamaPrompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseJson);

            if (doc.RootElement.TryGetProperty("response", out var respElement))
            {
                var result = respElement.GetString()?.Trim();
                if (!string.IsNullOrEmpty(result))
                {
                    // Clean up common prefixes that Ollama might add
                    var prefixesToRemove = new[]
                    {
                        "Here is a concise image prompt:",
                        "Here is the prompt:",
                        "Here's the prompt:",
                        "Image prompt:",
                        "Prompt:",
                        "Here it is:"
                    };

                    foreach (var prefix in prefixesToRemove)
                    {
                        if (result.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                        {
                            result = result[prefix.Length..].TrimStart();
                            break;
                        }
                    }

                    // Clean up quotes and formatting
                    result = result.Trim('"', '\'', '\n', '\r');
                    result = result.Replace("\n", " ").Replace("\r", " ");
                    while (result.Contains("  "))
                        result = result.Replace("  ", " ");

                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error generating prompt with Ollama: {ex.Message}");
        }

        // Fallback to basic prompt if Ollama fails
        return $"16-bit, {_world.Game.Theme ?? "fantasy"}, {room.Name}, atmospheric, detailed, panoramic view";
    }

    /// <summary>
    /// Genera una descripción con IA para una sala usando Ollama.
    /// </summary>
    private async void PropertyEditor_RequestAiDescriptionGeneration(Room room)
    {
        if (room == null) return;

        try
        {
            // Verificar que el mundo tenga un tema definido
            if (string.IsNullOrWhiteSpace(_world.Game.Theme))
            {
                AlertWindow.Show("Tema requerido",
                    "El mundo necesita tener un tema/ambientación definido para generar descripciones coherentes.\n\n" +
                    "Añade un tema en las propiedades del mundo (ej: 'fantasía medieval', 'ciencia ficción', 'horror gótico').",
                    this);
                return;
            }

            // Verificar que Ollama esté disponible
            ShowPlayLoading("Verificando servidor de IA...");

            try
            {
                var healthCheck = await _httpClient.GetAsync("api/tags");
                if (!healthCheck.IsSuccessStatusCode)
                {
                    throw new HttpRequestException("Ollama no está disponible");
                }
            }
            catch (HttpRequestException)
            {
                HidePlayLoading();

                var link = new TextBlock
                {
                    Margin = new Thickness(0, 4, 0, 0)
                };
                var hyperlink = new Hyperlink
                {
                    NavigateUri = new Uri("https://docs.docker.com/desktop/setup/install/windows-install/")
                };
                hyperlink.Inlines.Add("Instala Docker Desktop");
                hyperlink.RequestNavigate += (_, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = e.Uri.AbsoluteUri,
                            UseShellExecute = true
                        });
                        e.Handled = true;
                    }
                    catch { }
                };
                link.Inlines.Add(hyperlink);

                var confirmDialog = new DarkConfirmDialog(
                    "Iniciar Ollama",
                    "El servidor de IA (Ollama) no está iniciado.\n\n" +
                    "¿Deseas iniciarlo ahora?\n\n" +
                    "Instala Docker Desktop si no lo has hecho ya.",
                    this);
                confirmDialog.SetCustomContent(link);

                if (confirmDialog.ShowDialog() != true)
                    return;

                // Iniciar Docker con Ollama
                var progressWindow = new DockerProgressWindow
                {
                    Owner = this,
                    IncludeTts = false,
                    IncludeStableDiffusion = false,
                    IncludeOllama = true
                };

                var dockerResult = await progressWindow.RunAsync();
                if (!dockerResult.Success)
                {
                    DarkErrorDialog.Show("Error", "No se pudo iniciar el servidor de IA.", this);
                    return;
                }
            }

            ShowPlayLoading("Generando descripción...");

            // Construir contexto para la descripción
            var contextInfo = new StringBuilder();
            contextInfo.Append(room.IsInterior ? "Es un interior. " : "Es un exterior. ");
            if (!room.IsIlluminated)
                contextInfo.Append("Está poco iluminado/oscuro. ");

            // Objetos en la sala
            var roomObjects = _world.Objects.Where(o => o.RoomId == room.Id).ToList();
            if (roomObjects.Count > 0)
            {
                var objNames = string.Join(", ", roomObjects.Select(o => o.Name));
                contextInfo.Append($"Contiene: {objNames}. ");
            }

            // NPCs en la sala
            var roomNpcs = _world.Npcs.Where(n => n.RoomId == room.Id).ToList();
            if (roomNpcs.Count > 0)
            {
                var npcNames = string.Join(", ", roomNpcs.Select(n => n.Name));
                contextInfo.Append($"NPCs presentes: {npcNames}. ");
            }

            var prompt = $@"Eres un escritor de aventuras de texto. Escribe una descripción breve y atmosférica para una sala de un juego.

Tema del mundo: {_world.Game.Theme}
Nombre de la sala: {room.Name}
Contexto: {contextInfo}

Escribe SOLO la descripción en español, 2-3 frases, sin incluir el nombre de la sala. Debe ser evocadora y ayudar al jugador a visualizar el lugar.";

            var requestBody = new
            {
                model = "llama3",
                prompt = prompt,
                stream = false
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content);
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);

            string? description = null;
            if (doc.RootElement.TryGetProperty("response", out var respElement))
            {
                description = respElement.GetString()?.Trim();
            }

            if (string.IsNullOrEmpty(description))
            {
                throw new InvalidOperationException("La respuesta de Ollama no contiene descripción");
            }

            // Guardar la descripción en la sala
            room.Description = description;

            // Notificar cambios
            PropertyEditor.SetObject(room);
            PushUndoSnapshot();
            SetDirty(true);

            HidePlayLoading();
            AlertWindow.Show("Descripción generada correctamente.", this);
        }
        catch (Exception ex)
        {
            HidePlayLoading();
            DarkErrorDialog.Show("Error de generación", $"Error al generar la descripción:\n\n{ex.Message}", this);
        }
    }

    private void DeleteDoor(Door door)
    {
        if (door is null) return;

        var dlg = new ConfirmWindow($"¿Eliminar la puerta '{door.Name}'?", "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        WorldEditorHelpers.DeleteDoor(_world, door);

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

    }

    private void DeleteNpc(Npc npc)
    {
        if (npc is null) return;

        var dlg = new ConfirmWindow($"¿Eliminar el NPC '{npc.Name}'?", "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        _world.Npcs.Remove(npc);

        foreach (var room in _world.Rooms)
        {
            room.NpcIds.Remove(npc.Id);
        }

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

    }

    private void DeleteQuest(QuestDefinition quest)
    {
        if (quest is null) return;

        var dlg = new ConfirmWindow($"¿Eliminar la misión '{quest.Name}'?", "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        _world.Quests.Remove(quest);

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

    }

    private void AddDoorToTreeNode(Door door)
    {
        var salasRoot = WorldTree.Items.OfType<TreeViewItem>().FirstOrDefault(i => i.Header?.ToString()?.StartsWith("Salas") == true);
        if (salasRoot == null)
        {
            BuildTree();
            return;
        }

        // Añadir la puerta a las salas que conecta (RoomIdA y RoomIdB)
        foreach (TreeViewItem roomNode in salasRoot.Items.OfType<TreeViewItem>())
        {
            if (roomNode.Tag is Room room && (room.Id == door.RoomIdA || room.Id == door.RoomIdB))
            {
                if (!roomNode.Items.OfType<TreeViewItem>().Any(i => ReferenceEquals(i.Tag, door)))
                {
                    roomNode.Items.Add(new TreeViewItem { Header = door.Name, Tag = door, Foreground = Brushes.White });
                }
            }
        }
    }

    private void AddObjectToTreeNode(GameObject obj)
    {
        var root = WorldTree.Items.OfType<TreeViewItem>().FirstOrDefault(i => i.Header?.ToString()?.StartsWith("Objetos") == true);
        if (root == null)
        {
            BuildTree();
            return;
        }

        if (!root.Items.OfType<TreeViewItem>().Any(i => ReferenceEquals(i.Tag, obj)))
        {
            root.Items.Add(new TreeViewItem { Header = obj.Name, Tag = obj, Foreground = Brushes.White });
        }
    }

    private void SelectDoorInTree(Door door)
    {
        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("Salas") == true)
            {
                root.IsExpanded = true;
                // Buscar la puerta dentro de cualquier sala que la contenga
                foreach (TreeViewItem roomNode in root.Items.OfType<TreeViewItem>())
                {
                    foreach (TreeViewItem doorNode in roomNode.Items.OfType<TreeViewItem>())
                    {
                        if (doorNode.Tag == door)
                        {
                            roomNode.IsExpanded = true;
                            WorldTree.Focus();
                            doorNode.IsSelected = true;
                            doorNode.BringIntoView();
                            doorNode.Focus();
                            return;
                        }
                    }
                }
            }
        }
    }

    private void SelectRoomInTree(Room room)
    {
        if (room is null) return;

        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("Salas") == true)
            {
                root.IsExpanded = true;
                // Buscar en zonas
                foreach (TreeViewItem zoneNode in root.Items.OfType<TreeViewItem>())
                {
                    if (zoneNode.Tag is ZoneNodeTag)
                    {
                        foreach (TreeViewItem roomNode in zoneNode.Items.OfType<TreeViewItem>())
                        {
                            if (roomNode.Tag == room)
                            {
                                zoneNode.IsExpanded = true;
                                WorldTree.Focus();
                                roomNode.IsSelected = true;
                                roomNode.BringIntoView();
                                roomNode.Focus();
                                return;
                            }
                        }
                    }
                }
            }
        }
    }

    private void SelectZoneInTreeForEditing(string zoneName)
    {
        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("Salas") == true)
            {
                root.IsExpanded = true;
                foreach (TreeViewItem zoneNode in root.Items.OfType<TreeViewItem>())
                {
                    if (zoneNode.Tag is ZoneNodeTag tag && tag.ZoneName == zoneName)
                    {
                        zoneNode.IsExpanded = true;
                        WorldTree.Focus();
                        zoneNode.IsSelected = true;
                        zoneNode.BringIntoView();
                        zoneNode.Focus();

                        // Iniciar edición inline del nombre de la zona
                        StartZoneRename(zoneNode, zoneName);
                        return;
                    }
                }
            }
        }
    }

    private void StartZoneRename(TreeViewItem zoneNode, string currentName)
    {
        // Guardar el header original
        var originalHeader = zoneNode.Header;

        // Crear TextBox para edición inline
        var textBox = new TextBox
        {
            Text = currentName,
            Width = 150,
            Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(100, 150, 255)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(2),
            SelectionBrush = new SolidColorBrush(Color.FromRgb(100, 150, 255))
        };

        textBox.SelectAll();
        zoneNode.Header = textBox;
        textBox.Focus();

        bool committed = false;
        void CommitRename()
        {
            if (committed) return;
            committed = true;

            var newName = textBox.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                newName = currentName;
            }

            // Verificar que el nombre no exista ya
            var existingZones = _world.Rooms
                .Select(r => r.Zone ?? "")
                .Where(z => !string.IsNullOrEmpty(z) && z != currentName)
                .Distinct()
                .ToList();

            if (existingZones.Contains(newName))
            {
                AlertWindow.Show($"Ya existe una zona con el nombre '{newName}'.", this);
                newName = currentName;
            }

            // Actualizar todas las salas de esta zona
            foreach (var room in _world.Rooms.Where(r => r.Zone == currentName))
            {
                room.Zone = newName;
            }

            // Actualizar el tag del nodo
            if (zoneNode.Tag is ZoneNodeTag tag)
            {
                tag.ZoneName = newName;
            }

            BuildTree();
            UpdateZoneFilter();
            PushUndoSnapshot();
            SetDirty(true);
        }

        textBox.LostFocus += (s, e) => CommitRename();
        textBox.KeyDown += (s, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitRename();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                zoneNode.Header = originalHeader;
                e.Handled = true;
            }
        };
    }

    /// <summary>
    /// Busca recursivamente un objeto en el árbol y lo selecciona, expandiendo todos los nodos padre.
    /// </summary>
    private bool FindAndSelectInTree(TreeViewItem node, object target)
    {
        // Verificar si este nodo contiene el objeto buscado
        if (node.Tag == target)
        {
            WorldTree.Focus();
            node.IsSelected = true;
            node.BringIntoView();
            node.Focus();
            return true;
        }

        // Buscar recursivamente en los hijos
        foreach (TreeViewItem child in node.Items.OfType<TreeViewItem>())
        {
            if (FindAndSelectInTree(child, target))
            {
                // Expandir este nodo ya que contiene el objeto buscado
                node.IsExpanded = true;
                return true;
            }
        }

        return false;
    }

    private void SelectObjectInTree(GameObject obj)
    {
        if (obj is null) return;

        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("Objetos") == true)
            {
                root.IsExpanded = true;
                FindAndSelectInTree(root, obj);
                return;
            }
        }
    }

    private void SelectNpcInTree(Npc npc)
    {
        if (npc is null) return;

        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("NPCs") == true)
            {
                root.IsExpanded = true;
                foreach (TreeViewItem child in root.Items.OfType<TreeViewItem>())
                {
                    if (child.Tag == npc)
                    {
                        WorldTree.Focus();
                        child.IsSelected = true;
                        child.BringIntoView();
                        child.Focus();
                        return;
                    }
                }
            }
        }
    }

    private void SelectQuestInTree(QuestDefinition quest)
    {
        if (quest is null) return;

        foreach (TreeViewItem root in WorldTree.Items)
        {
            if (root.Header?.ToString()?.StartsWith("Misiones") == true)
            {
                root.IsExpanded = true;
                foreach (TreeViewItem child in root.Items.OfType<TreeViewItem>())
                {
                    if (child.Tag == quest)
                    {
                        WorldTree.Focus();
                        child.IsSelected = true;
                        child.BringIntoView();
                        child.Focus();
                        return;
                    }
                }
            }
        }
    }

    private void CutCopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        if (e.Command != ApplicationCommands.Cut && e.Command != ApplicationCommands.Copy)
            return;

        if (MapPanel == null)
        {
            e.CanExecute = false;
            e.Handled = true;
            return;
        }

        var selected = MapPanel.GetSelectedRooms();
        e.CanExecute = selected != null && selected.Count > 0;
        e.Handled = true;
    }

    private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = _roomsClipboard != null && _roomsClipboard.Count > 0;
        e.Handled = true;
    }

    private void CutCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selected = MapPanel.GetSelectedRooms();
        if (selected == null || selected.Count == 0 || _world == null)
            return;

        _roomsClipboard = CloneRoomsForClipboard(selected);
        _roomsClipboardIsCut = true;

        // Guardamos también las posiciones actuales de las salas copiadas
        _roomsClipboardPositions = MapPanel.GetRoomPositions(selected.Select(r => r.Id));
        _lastClipboardIdMap = null;

        foreach (var room in selected)
        {
            DeleteRoomWithoutConfirmation(room);
        }

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

        MapPanel.ClearSelection();
        PushUndoSnapshot();
    }

    private void CopyCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        var selected = MapPanel.GetSelectedRooms();
        if (selected == null || selected.Count == 0)
            return;

        _roomsClipboard = CloneRoomsForClipboard(selected);
        _roomsClipboardIsCut = false;

        // Guardamos también las posiciones actuales de las salas copiadas
        _roomsClipboardPositions = MapPanel.GetRoomPositions(selected.Select(r => r.Id));
        _lastClipboardIdMap = null;
    }

    private void PasteCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        if (_world == null || _roomsClipboard == null || _roomsClipboard.Count == 0)
            return;

        var newRooms = CreateRoomsFromClipboard(_roomsClipboard);

        foreach (var room in newRooms)
        {
            _world.Rooms.Add(room);
        }

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
        PushUndoSnapshot();
        SetDirty(true);

        // Si tenemos posiciones de las salas originales y un mapa de Ids,
        // recolocamos las salas nuevas en las mismas coordenadas pero
        // desplazadas ligeramente a la derecha y hacia abajo.
        if (_roomsClipboardPositions != null && _lastClipboardIdMap != null)
        {
            var newPositions = new Dictionary<string, Point>();
            const double offsetX = 40;
            const double offsetY = 40;

            foreach (var kv in _roomsClipboardPositions)
            {
                var originalId = kv.Key;
                var originalPos = kv.Value;

                if (_lastClipboardIdMap.TryGetValue(originalId, out var newId))
                {
                    newPositions[newId] = new Point(originalPos.X + offsetX, originalPos.Y + offsetY);
                }
            }

            if (newPositions.Count > 0)
            {
                MapPanel.SetRoomsPositions(newPositions);
            }
            else
            {
                MapPanel.PlaceRoomsAtTopLeft(newRooms);
            }
        }
        else
        {
            MapPanel.PlaceRoomsAtTopLeft(newRooms);
        }

        MapPanel.SetSelectedRooms(newRooms);

        if (_roomsClipboardIsCut)
        {
            _roomsClipboard = null;
            _roomsClipboardIsCut = false;
            _roomsClipboardPositions = null;
            _lastClipboardIdMap = null;
        }

        PushUndoSnapshot();
    }

    private static List<Room> CloneRoomsForClipboard(IEnumerable<Room> rooms)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        var json = JsonSerializer.Serialize(rooms, options);
        return JsonSerializer.Deserialize<List<Room>>(json, options) ?? new List<Room>();
    }

    private List<Room> CreateRoomsFromClipboard(List<Room> sourceRooms)
    {
        var result = new List<Room>();
        if (_world == null || sourceRooms == null || sourceRooms.Count == 0)
            return result;

        var existingIds = new HashSet<string>(_world.Rooms.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var options = new JsonSerializerOptions
        {
            WriteIndented = false
        };

        foreach (var room in sourceRooms)
        {
            var originalId = room.Id;
            var newId = GenerateUniqueRoomId(originalId, existingIds);
            existingIds.Add(newId);
            idMap[originalId] = newId;

            // Clonamos la sala para no modificar el contenido del portapapeles
            var json = JsonSerializer.Serialize(room, options);
            var cloned = JsonSerializer.Deserialize<Room>(json, options);
            if (cloned == null)
                continue;

            cloned.Id = newId;
            result.Add(cloned);
        }

        _lastClipboardIdMap = idMap;

        // Ajustar las salidas internas que apunten entre las salas pegadas
        foreach (var room in result)
        {
            foreach (var ex in room.Exits)
            {
                if (!string.IsNullOrEmpty(ex.TargetRoomId) && idMap.TryGetValue(ex.TargetRoomId, out var mapped))
                {
                    ex.TargetRoomId = mapped;
                }
            }
        }

        return result;
    }

    private static string GenerateUniqueRoomId(string baseId, HashSet<string> existingIds)
    {
        if (!existingIds.Contains(baseId))
            return baseId;

        int i = 1;
        while (true)
        {
            var candidate = $"{baseId}_{i}";
            if (!existingIds.Contains(candidate))
                return candidate;
            i++;
        }
    }

    private void DeleteRoomWithoutConfirmation(Room room)
    {
        if (_world == null || room is null)
            return;

        // Quitar salidas que apunten a esta sala
        foreach (var r in _world.Rooms)
        {
            r.Exits.RemoveAll(ex => string.Equals(ex.TargetRoomId, room.Id, StringComparison.OrdinalIgnoreCase));
        }

        // Desasociar objetos y NPCs de esta sala
        foreach (var obj in _world.Objects.Where(o => string.Equals(o.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
        {
            obj.RoomId = null;
        }

        foreach (var npc in _world.Npcs.Where(n => string.Equals(n.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
        {
            npc.RoomId = null;
        }

        // Si era la sala inicial, limpiamos ese valor
        if (!string.IsNullOrEmpty(_world.Game.StartRoomId) &&
            string.Equals(_world.Game.StartRoomId, room.Id, StringComparison.OrdinalIgnoreCase))
        {
            _world.Game.StartRoomId = string.Empty;
        }

        _world.Rooms.Remove(room);
    }




    private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private sealed class EditorSnapshot
    {
        public string WorldJson { get; set; } = string.Empty;
    }

    private sealed class UndoRedoManager
    {
        private readonly Stack<EditorSnapshot> _undoStack = new();
        private readonly Stack<EditorSnapshot> _redoStack = new();

        public bool CanUndo => _undoStack.Count > 1;
        public bool CanRedo => _redoStack.Count > 0;

        public void Reset(EditorSnapshot initialState)
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _undoStack.Push(initialState);
        }

        public void Push(EditorSnapshot snapshot)
        {
            _undoStack.Push(snapshot);
            _redoStack.Clear();
        }

        public EditorSnapshot? Undo()
        {
            if (!CanUndo)
                return null;

            var current = _undoStack.Pop();
            _redoStack.Push(current);

            return _undoStack.Peek();
        }

        public EditorSnapshot? Redo()
        {
            if (!CanRedo)
                return null;

            var snapshot = _redoStack.Pop();
            _undoStack.Push(snapshot);
            return snapshot;
        }
    }

    private EditorSnapshot CreateSnapshot()
    {
        // Sincronizamos las posiciones del mapa con el modelo antes de capturar el estado.
        if (_world != null)
        {
            var roomIds = _world.Rooms.Select(r => r.Id);
            var positions = MapPanel.GetRoomPositions(roomIds);

            _world.RoomPositions ??= new Dictionary<string, MapPosition>();
            _world.RoomPositions.Clear();

            foreach (var kv in positions)
            {
                _world.RoomPositions[kv.Key] = new MapPosition
                {
                    X = kv.Value.X,
                    Y = kv.Value.Y
                };
            }
        }

        var json = JsonSerializer.Serialize(_world, SnapshotJsonOptions);
        return new EditorSnapshot { WorldJson = json };
    }

    private void ResetUndoRedo()
    {
        var initial = CreateSnapshot();
        _undoRedo.Reset(initial);
        CommandManager.InvalidateRequerySuggested();
        SetDirty(false);
    }

    private void PushUndoSnapshot()
    {
        var snapshot = CreateSnapshot();
        _undoRedo.Push(snapshot);
        CommandManager.InvalidateRequerySuggested();
        SetDirty(true);
    }

    private void SetDirty(bool dirty)
    {
        _isDirty = dirty;
        UpdateWindowTitle();
        UpdateSaveButtonAppearance();

        // Auto-sincronizar con el panel de prueba si está visible
        if (dirty && TestPanel.Visibility == Visibility.Visible && _testEngine != null)
        {
            if (_skipNextAutoSync)
            {
                _skipNextAutoSync = false;
            }
            else
            {
                ScheduleAutoSync();
            }
        }
    }

    /// <summary>
    /// Programa una sincronización automática con la ventana de prueba.
    /// Usa debounce para evitar recargar con cada cambio.
    /// </summary>
    private void ScheduleAutoSync()
    {
        if (_autoSyncTimer == null)
        {
            _autoSyncTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(800)
            };
            _autoSyncTimer.Tick += async (_, _) =>
            {
                _autoSyncTimer.Stop();
                if (TestPanel.Visibility == Visibility.Visible && _testEngine != null)
                {
                    await PerformAutoSyncAsync();
                }
            };
        }

        // Reiniciar el timer (debounce)
        _autoSyncTimer.Stop();
        _autoSyncTimer.Start();
    }

    /// <summary>
    /// Realiza la sincronización automática sin mostrar overlay.
    /// </summary>
    private async Task PerformAutoSyncAsync()
    {
        if (TestPanel.Visibility != Visibility.Visible || _testEngine == null || _world == null)
            return;

        try
        {
            // Guardar el mundo actual
            SyncMapPositionsToWorld();
            WorldLoader.SaveWorldModel(_world, _currentPath!);
            SetDirty(false);

            // Preservar posición e inventario del jugador
            var currentRoomId = _testEngine.State.CurrentRoomId;
            var currentInventory = _testEngine.State.InventoryObjectIds.ToList();

            // Cargar y recargar
            _testWorld = WorldLoader.LoadWorldModel(_currentPath!);
            Parser.SetWorldDictionary(_testWorld.Game.ParserDictionaryJson);
            var state = WorldLoader.CreateInitialState(_testWorld);

            // Restaurar posición si la sala sigue existiendo
            if (_testWorld.Rooms.Any(r => r.Id.Equals(currentRoomId, StringComparison.OrdinalIgnoreCase)))
            {
                state.CurrentRoomId = currentRoomId;
            }

            // Restaurar inventario
            state.InventoryObjectIds = currentInventory
                .Where(id => _testWorld.Objects.Any(o => o.Id.Equals(id, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            _testEngine = new GameEngine(_testWorld, state, _testSoundManager!, isDebugMode: true);
            _testEngine.ScriptMessage += msg => Dispatcher.Invoke(() => HandleTestScriptMessage(msg));
            _testEngine.AdventureCompleted += () => Dispatcher.Invoke(ShowEndingWindow);
            _testEngine.CombatStarted += npcId => Dispatcher.Invoke(() => HandleTestCombatStarted(npcId));
            _testEngine.TradeOpened += npc => Dispatcher.Invoke(() => HandleTestTradeOpened(npc));
            _testEngine.ConversationDialogue += msg => Dispatcher.Invoke(() => HandleTestConversationDialogue(msg));
            _testEngine.ConversationOptions += options => Dispatcher.Invoke(() => HandleTestConversationOptions(options));
            _testEngine.ConversationEnded += () => Dispatcher.Invoke(HandleTestConversationEnded);
            _testEngine.HelpRequested += () => Dispatcher.Invoke(HandleTestHelpRequested);

            // Aplicar fuente por defecto del juego (puede haber cambiado)
            var fontFamily = new FontFamily(_testWorld.Game.DefaultFontFamily);
            TestOutputTextBox.FontFamily = fontFamily;
            TestInputTextBox.FontFamily = fontFamily;
            TestRoomTitle.FontFamily = fontFamily;
            TestRoomDescription.FontFamily = fontFamily;

            AppendTestSystemMessage("⟳ Mundo recargado");
            UpdateTestDisplay();
        }
        catch
        {
            // Ignorar errores silenciosamente en auto-sync
        }

        await Task.CompletedTask;
    }

    private void SyncMapPositionsToWorld()
    {
        if (_world == null) return;
        var roomIds = _world.Rooms.Select(r => r.Id);
        var positions = MapPanel.GetRoomPositions(roomIds);
        _world.RoomPositions ??= new Dictionary<string, MapPosition>();
        _world.RoomPositions.Clear();
        foreach (var kv in positions)
        {
            _world.RoomPositions[kv.Key] = new MapPosition { X = kv.Value.X, Y = kv.Value.Y };
        }
    }

    private void UpdateSaveButtonAppearance()
    {
        // Cuando NO hay cambios: botón se ve "pulsado" (más oscuro)
        // Cuando HAY cambios: botón se ve normal (más claro)
        if (_isDirty)
        {
            // Hay cambios: estilo normal
            SaveButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A3A3A"));
            SaveButton.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#4A4A4A"));
        }
        else
        {
            // No hay cambios: estilo "pulsado" (más oscuro/hundido)
            SaveButton.Background = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2A2A2A"));
            SaveButton.BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#1A1A1A"));
        }
    }

    private void UpdateWindowTitle()
    {
        var worldLabel = string.IsNullOrEmpty(_currentPath) ? "Nuevo mundo" : Path.GetFileName(_currentPath);
        var dirtySuffix = _isDirty ? " *" : string.Empty;
        Title = $"{_baseTitle} - {worldLabel}{dirtySuffix}";
    }

    private Style ResolveCommandButtonStyle()
    {
        var style = TryFindResource("CommandButtonStyle") as Style
                    ?? Application.Current?.TryFindResource("CommandButtonStyle") as Style;
        return style ?? new Style(typeof(Button));
    }

    private void RestoreSnapshot(EditorSnapshot snapshot)
    {
        var world = JsonSerializer.Deserialize<WorldModel>(snapshot.WorldJson, SnapshotJsonOptions);
        if (world == null)
            return;

        _world = world;
        MapPanel.SetWorld(_world);
        BuildTree();
        CommandManager.InvalidateRequerySuggested();
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

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            if (e.Key == Key.S)
            {
                // Guardar mundo con Ctrl+S
                SaveMenu_Click(this, new RoutedEventArgs());
                e.Handled = true;
                return;
            }

            if (e.Key == Key.A)
            {
                // Seleccionar todas las salas (de la zona seleccionada o todas)
                var selectedZone = ZoneFilterComboBox.SelectedItem as string;
                IEnumerable<Room> roomsToSelect;

                if (string.IsNullOrEmpty(selectedZone) || selectedZone == "(Todas las zonas)")
                {
                    roomsToSelect = _world.Rooms;
                }
                else
                {
                    roomsToSelect = _world.Rooms.Where(r => r.Zone == selectedZone);
                }

                MapPanel.SetSelectedRooms(roomsToSelect);
                e.Handled = true;
                return;
            }
        }

        // Hotkeys sin modificador: ignorar si el foco está en un control de entrada de texto
        var focused = Keyboard.FocusedElement;
        if (focused is TextBox || focused is PasswordBox || focused is System.Windows.Controls.Primitives.TextBoxBase)
            return;
        if (focused is ComboBox combo && combo.IsEditable)
            return;

        switch (e.Key)
        {
            case Key.G:
                ToggleGridButton.IsChecked = !ToggleGridButton.IsChecked;
                MapPanel.SetGridVisibility(ToggleGridButton.IsChecked ?? false);
                e.Handled = true;
                break;
            case Key.H:
                ToggleSnapButton.IsChecked = !ToggleSnapButton.IsChecked;
                MapPanel.SetSnapToGrid(ToggleSnapButton.IsChecked ?? false);
                e.Handled = true;
                break;
        }
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (_isDirty)
        {
            var dlg = new SaveChangesWindow("Hay cambios sin guardar. ¿Quieres guardarlos antes de cerrar el editor?")
            {
                Owner = this
            };
            dlg.ShowDialog();

            if (dlg.Result == SaveChangesResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (dlg.Result == SaveChangesResult.Save)
            {
                if (!ValidateEncryptionKey())
                {
                    e.Cancel = true;
                    return;
                }

                PerformSave();
                if (_isDirty)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        // Si el modo pruebas está activo, detener sonido y voz
        if (_testSoundManager != null)
        {
            try
            {
                _testSoundManager.StopMusic();
                _testSoundManager.Dispose();
            }
            catch
            {
                // Ignorar errores al detener el sonido
            }
            _testSoundManager = null;
        }

        // Al cerrar el editor intentamos también cerrar Docker Desktop.
        try
        {
            DockerShutdownHelper.TryShutdownDockerDesktop();
        }
        catch
        {
            // Ignoramos errores al cerrar Docker; no deben bloquear el cierre del editor.
        }

        base.OnClosing(e);
    }





    private void MapPanel_AddObjectToRoomRequested(Room room)
    {
        var index = _world.Objects.Count + 1;
        var obj = new GameObject
        {
            Id = $"obj_{index}",
            Name = $"Objeto {index}",
            Description = "Nuevo objeto.",
            CanTake = true,
            Visible = true,
            RoomId = room.Id
        };

        _world.Objects.Add(obj);
        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void MapPanel_AddNpcToRoomRequested(Room room)
    {
        var index = _world.Npcs.Count + 1;
        var npc = new Npc
        {
            Id = $"npc_{index}",
            Name = $"PNJ {index}",
            Description = "Nuevo personaje.",
            Visible = true,
            RoomId = room.Id
        };

        _world.Npcs.Add(npc);
        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void MapPanel_TeleportToRoomRequested(Room room)
    {
        if (_testEngine == null || _testWorld == null) return;

        // Cambiar la sala del jugador
        _testEngine.State.CurrentRoomId = room.Id;

        // Limpiar texto y actualizar (descripción ya visible en el panel)
        TestOutputTextBox.Document.Blocks.Clear();
        UpdateTestDisplay(showRoomDescription: false);
    }

    private void MapPanel_EmptyMapDoubleClicked(Point logicalPos)
    {
        // Determinar la zona de la nueva sala basándose en el filtro seleccionado
        string? targetZone = null;
        var selectedFilter = ZoneFilterComboBox.SelectedItem as string;
        if (!string.IsNullOrEmpty(selectedFilter) && selectedFilter != "(Todas las zonas)")
        {
            targetZone = selectedFilter;
        }
        else
        {
            // Si no hay zona específica seleccionada, usar la primera disponible o "Inicial"
            targetZone = _world.Rooms
                .Select(r => r.Zone)
                .FirstOrDefault(z => !string.IsNullOrEmpty(z)) ?? "Inicial";
        }

        var index = _world.Rooms.Count + 1;
        var room = new Room
        {
            Id = $"sala_{index}",
            Name = $"Sala {index}",
            Description = "Nueva sala.",
            Zone = targetZone
        };
        _world.Rooms.Add(room);

        // Encontrar la posición libre más cercana, considerando snap-to-grid si está activado
        Point freePosition = MapPanel.FindNearestFreePosition(logicalPos);

        // Establecer la posición de la sala en el mapa
        MapPanel.SetRoomPosition(room.Id, freePosition);

        MapPanel.SetWorld(_world);
        BuildTree();
        SelectRoomInTree(room);

        PushUndoSnapshot();
        SetDirty(true);
    }

    private void MapPanel_RoomsDeleteRequested(List<string> roomIds)
    {
        if (roomIds == null || roomIds.Count == 0)
            return;

        // Buscar las salas correspondientes
        var roomsToDelete = new List<Room>();
        foreach (var roomId in roomIds)
        {
            var room = _world.Rooms.FirstOrDefault(r => string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
            if (room != null)
                roomsToDelete.Add(room);
        }

        if (roomsToDelete.Count == 0)
            return;

        // Mostrar confirmación
        string message = roomsToDelete.Count == 1
            ? $"¿Eliminar la sala '{roomsToDelete[0].Name}'?"
            : $"¿Eliminar {roomsToDelete.Count} salas seleccionadas?";

        var dlg = new ConfirmWindow(message, "Confirmar eliminación")
        {
            Owner = this
        };

        if (dlg.ShowDialog() != true)
            return;

        // Eliminar cada sala
        foreach (var room in roomsToDelete)
        {
            // Quitar salidas que apunten a esta sala
            foreach (var r in _world.Rooms)
            {
                r.Exits.RemoveAll(ex => string.Equals(ex.TargetRoomId, room.Id, StringComparison.OrdinalIgnoreCase));
            }

            // Quitar puertas que conecten con esta sala
            if (_world.Doors != null)
            {
                _world.Doors.RemoveAll(d =>
                    string.Equals(d.RoomIdA, room.Id, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(d.RoomIdB, room.Id, StringComparison.OrdinalIgnoreCase));
            }

            // Desasociar objetos y NPCs de esta sala
            foreach (var obj in _world.Objects.Where(o => string.Equals(o.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
            {
                obj.RoomId = null;
            }

            foreach (var npc in _world.Npcs.Where(n => string.Equals(n.RoomId, room.Id, StringComparison.OrdinalIgnoreCase)))
            {
                npc.RoomId = null;
            }

            // Limpiar sala de inicio si era esta
            if (string.Equals(_world.Game.StartRoomId, room.Id, StringComparison.OrdinalIgnoreCase))
            {
                _world.Game.StartRoomId = string.Empty;
            }

            _world.Rooms.Remove(room);
        }

        BuildTree();
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);
    }

    private void ReorderMap_Click(object sender, RoutedEventArgs e)
    {
        if (_world.Rooms.Count == 0)
        {
            AlertWindow.Show("No hay salas que reordenar.", this);
            return;
        }

        var confirmDlg = new AlertWindow(
            "¿Reordenar el mapa?\n\nEsto reorganizará todas las salas según sus conexiones y separará las zonas para que no se superpongan.",
            "Confirmar reordenación");
        confirmDlg.ShowCancelButton(true);
        confirmDlg.SetOkButtonText("Reordenar");
        confirmDlg.Owner = this;

        if (confirmDlg.ShowDialog() != true)
            return;

        // Reordenar el mapa
        WorldLoader.ReorderRoomPositions(_world);

        // Actualizar el mapa
        MapPanel.SetWorld(_world);
        PushUndoSnapshot();
        SetDirty(true);

        // Centrar en la sala inicial
        var startRoom = _world.Rooms.FirstOrDefault(r => r.Id == _world.Game.StartRoomId);
        if (startRoom != null)
        {
            MapPanel.CenterOnRoom(startRoom);
        }
    }

    private void ToggleGridButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            bool isChecked = toggleButton.IsChecked ?? false;
            MapPanel.SetGridVisibility(isChecked);
        }
    }

    private void ToggleSnapButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton toggleButton)
        {
            bool isChecked = toggleButton.IsChecked ?? false;
            MapPanel.SetSnapToGrid(isChecked);
        }
    }

    private void ToggleGrid_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ToggleGridButton.IsChecked = !ToggleGridButton.IsChecked;
        MapPanel.SetGridVisibility(ToggleGridButton.IsChecked ?? false);
    }

    private void ToggleSnap_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ToggleSnapButton.IsChecked = !ToggleSnapButton.IsChecked;
        MapPanel.SetSnapToGrid(ToggleSnapButton.IsChecked ?? false);
    }

    private bool IsDotNet8SdkInstalled()
    {
        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "--list-sdks",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
                return false;

            process.WaitForExit(5000);
            var output = process.StandardOutput.ReadToEnd();

            // Buscar SDK 8.x o superior
            return output.Split('\n').Any(line =>
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed))
                    return false;

                var parts = trimmed.Split('.');
                if (parts.Length > 0 && int.TryParse(parts[0], out var majorVersion))
                {
                    return majorVersion >= 8;
                }
                return false;
            });
        }
        catch
        {
            return false;
        }
    }

    private async void ExportMenu_Click(object sender, RoutedEventArgs e)
    {
        // Verificar que el SDK de .NET 8 esté instalado
        if (!IsDotNet8SdkInstalled())
        {
            var alert = new AlertWindow(
                "Para exportar a ejecutable necesitas tener instalado el SDK de .NET 8.",
                "SDK de .NET 8 requerido")
            {
                Owner = this
            };

            // Crear un panel con el link de descarga
            var panel = new System.Windows.Controls.StackPanel();
            var link = new System.Windows.Documents.Hyperlink(new System.Windows.Documents.Run("Descarga el SDK"))
            {
                NavigateUri = new Uri("https://dotnet.microsoft.com/es-es/download/dotnet/thank-you/sdk-8.0.416-windows-x64-installer")
            };
            link.RequestNavigate += (s, args) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = args.Uri.ToString(),
                    UseShellExecute = true
                });
            };

            var linkTextBlock = new System.Windows.Controls.TextBlock
            {
                FontSize = 16,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(100, 180, 255))
            };
            linkTextBlock.Inlines.Add(link);
            panel.Children.Add(linkTextBlock);

            alert.SetCustomContent(panel);
            alert.ShowDialog();
            return;
        }

        // Verificar que el mundo esté guardado
        if (_isDirty)
        {
            var confirmDlg = new ConfirmWindow(
                "Debes guardar el mundo antes de exportar. ¿Quieres guardarlo ahora?",
                "Guardar antes de exportar")
            {
                Owner = this
            };

            if (confirmDlg.ShowDialog() == true)
            {
                await PerformSaveAsync();
                if (_isDirty) // Si sigue dirty, es que el usuario canceló o hubo error
                    return;
            }
            else
            {
                return;
            }
        }

        if (string.IsNullOrEmpty(_currentPath))
        {
            new AlertWindow(
                "Debes guardar el mundo en un archivo antes de exportar.",
                "Error")
            {
                Owner = this
            }.ShowDialog();
            return;
        }

        // Preguntar si quiere usar un icono personalizado
        string? customIconPath = null;
        var iconConfirmDlg = new ConfirmWindow(
            "¿Deseas usar un icono personalizado para el ejecutable?\n\nArchivo .ico de resolución recomendada 256x256.\n\nSi eliges 'No', se usará el icono predeterminado de XiloAdventures.",
            "Icono personalizado")
        {
            Owner = this
        };

        if (iconConfirmDlg.ShowDialog() == true)
        {
            var iconDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Iconos (*.ico)|*.ico",
                Title = "Selecciona el icono para tu aventura"
            };

            if (iconDialog.ShowDialog() == true)
            {
                customIconPath = iconDialog.FileName;
            }
        }

        // Seleccionar ubicación de salida
        var saveDialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "Ejecutable (*.exe)|*.exe",
            DefaultExt = ".exe",
            FileName = $"{_world.Game.Title}.exe"
        };

        if (saveDialog.ShowDialog() != true)
            return;

        var outputPath = saveDialog.FileName;

        // Mostrar indicador de progreso
        ShowPlayLoading("Exportando ejecutable...");

        try
        {
            await System.Threading.Tasks.Task.Run(() => ExportStandaloneExecutable(_currentPath, outputPath, customIconPath));

            HidePlayLoading();

            new AlertWindow(
                $"Ejecutable creado exitosamente en:\n{outputPath}\n\nEl jugador no necesitará .NET instalado para ejecutarlo.",
                "Exportación completada")
            {
                Owner = this
            }.ShowDialog();

            // Preguntar si quiere abrir la carpeta
            var confirmDlg = new ConfirmWindow(
                "¿Deseas abrir la carpeta donde se guardó el ejecutable?",
                "Abrir carpeta")
            {
                Owner = this
            };

            if (confirmDlg.ShowDialog() == true)
            {
                var folderPath = System.IO.Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", folderPath);
                }
            }
        }
        catch (Exception ex)
        {
            HidePlayLoading();
            new AlertWindow(
                $"Error al exportar el ejecutable:\n\n{ex.Message}",
                "Error de exportación")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void ExportStandaloneExecutable(string worldPath, string outputPath, string? customIconPath = null)
    {
        // Buscar el proyecto player
        var baseDir = AppContext.BaseDirectory;
        string? playerProjectPath = null;
        string? extractedSourceDir = null; // Para limpiar al final si se extrajo del ZIP

        // 1. Primero buscar en modo desarrollo (navegando hacia arriba hasta encontrar .sln)
        var currentDir = new System.IO.DirectoryInfo(baseDir);
        while (currentDir != null && !System.IO.File.Exists(System.IO.Path.Combine(currentDir.FullName, "XiloAdventures.sln")))
        {
            currentDir = currentDir.Parent;
        }

        if (currentDir != null)
        {
            // Modo desarrollo: usar la carpeta de la solución
            playerProjectPath = System.IO.Path.Combine(currentDir.FullName, "XiloAdventures.Wpf.Player");
        }
        else
        {
            // 2. Buscar SourceToExportEXE.zip en la instalación
            var sourceZipPath = System.IO.Path.Combine(baseDir, "SourceToExportEXE.zip");
            if (System.IO.File.Exists(sourceZipPath))
            {
                // Extraer el ZIP a una carpeta temporal
                extractedSourceDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"XiloAdventures_Source_{Guid.NewGuid():N}");
                System.IO.Compression.ZipFile.ExtractToDirectory(sourceZipPath, extractedSourceDir);
                playerProjectPath = System.IO.Path.Combine(extractedSourceDir, "XiloAdventures.Wpf.Player");
            }
        }

        if (string.IsNullOrEmpty(playerProjectPath) || !System.IO.Directory.Exists(playerProjectPath))
        {
            throw new InvalidOperationException(
                "No se encontró el código fuente del Player.\n\n" +
                "Asegúrate de tener instalado el .NET 8 SDK y que el código fuente esté disponible.");
        }

        // Verificar que existe el proyecto player
        var playerCsproj = System.IO.Path.Combine(playerProjectPath, "XiloAdventures.Wpf.Player.csproj");
        if (!System.IO.File.Exists(playerCsproj))
        {
            throw new InvalidOperationException(
                $"No se encontró el proyecto del player en:\n{playerProjectPath}\n\n" +
                "Asegúrate de que el proyecto XiloAdventures.Wpf.Player existe en la solución.");
        }

        // Copiar el mundo al proyecto player
        var worldDestPath = System.IO.Path.Combine(playerProjectPath, "world.xaw");
        System.IO.File.Copy(worldPath, worldDestPath, true);

        // Guardar el contenido original del .csproj para restaurarlo después
        string? originalCsprojContent = null;
        string? customIconDestPath = null;

        try
        {
            // Si hay un icono personalizado, modificar el .csproj temporalmente
            if (!string.IsNullOrEmpty(customIconPath) && System.IO.File.Exists(customIconPath))
            {
                // Copiar el icono al proyecto
                customIconDestPath = System.IO.Path.Combine(playerProjectPath, "custom_icon.ico");
                System.IO.File.Copy(customIconPath, customIconDestPath, true);

                // Leer y modificar el .csproj
                originalCsprojContent = System.IO.File.ReadAllText(playerCsproj);
                var modifiedCsproj = originalCsprojContent.Replace(
                    "<ApplicationIcon>..\\XiloAdventures.Wpf.Common\\appicon.ico</ApplicationIcon>",
                    "<ApplicationIcon>custom_icon.ico</ApplicationIcon>");
                System.IO.File.WriteAllText(playerCsproj, modifiedCsproj);
            }

            // Compilar con dotnet publish
            var publishDir = System.IO.Path.Combine(playerProjectPath, "bin", "publish");
            if (System.IO.Directory.Exists(publishDir))
            {
                System.IO.Directory.Delete(publishDir, true);
            }

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{playerCsproj}\" -c Release -r win-x64 --self-contained true -o \"{publishDir}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                throw new InvalidOperationException("No se pudo iniciar el proceso de compilación.");
            }

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                throw new InvalidOperationException(
                    $"Error al compilar el ejecutable (código {process.ExitCode}):\n\n{error}");
            }

            // Copiar el ejecutable resultante a la ubicación final
            var compiledExePath = System.IO.Path.Combine(publishDir, "XiloAdventures.Wpf.Player.exe");
            if (!System.IO.File.Exists(compiledExePath))
            {
                throw new InvalidOperationException(
                    $"No se encontró el ejecutable compilado en:\n{compiledExePath}");
            }

            try
            {
                System.IO.File.Copy(compiledExePath, outputPath, true);
            }
            catch (System.IO.IOException)
            {
                throw new InvalidOperationException(
                    "No se puede guardar el archivo porque está en uso.\n\n" +
                    "Cierra el ejecutable antes de volver a exportar.");
            }
        }
        finally
        {
            // Solo restaurar/limpiar si NO estamos usando una carpeta extraída del ZIP
            // (la carpeta extraída se borra completa al final)
            if (extractedSourceDir == null)
            {
                // Restaurar el .csproj original si fue modificado
                if (originalCsprojContent != null)
                {
                    try { System.IO.File.WriteAllText(playerCsproj, originalCsprojContent); } catch { }
                }

                // Limpiar el icono temporal
                if (customIconDestPath != null && System.IO.File.Exists(customIconDestPath))
                {
                    try { System.IO.File.Delete(customIconDestPath); } catch { }
                }

                // Limpiar el archivo temporal del mundo
                if (System.IO.File.Exists(worldDestPath))
                {
                    try { System.IO.File.Delete(worldDestPath); } catch { }
                }
            }

            // Limpiar la carpeta extraída del ZIP
            if (extractedSourceDir != null && System.IO.Directory.Exists(extractedSourceDir))
            {
                try { System.IO.Directory.Delete(extractedSourceDir, true); } catch { }
            }
        }
    }

    public class SelectRoomWindow : Window
    {
        private readonly ComboBox _combo;
        public Room? SelectedRoom { get; private set; }

        public SelectRoomWindow(IEnumerable<Room> rooms, string title)
        {
            Title = title;
            Width = 420;
            Height = 180;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(Color.FromRgb(34, 34, 34));
            Foreground = Brushes.White;
            WindowStyle = WindowStyle.ToolWindow;

            var panel = new StackPanel { Margin = new Thickness(10) };

            var text = new TextBlock
            {
                Text = "Elige la sala:",
                Margin = new Thickness(0, 0, 0, 8)
            };

            _combo = new ComboBox
            {
                Margin = new Thickness(0, 0, 0, 10),
                DisplayMemberPath = "Name",
                ItemsSource = rooms.ToList()
            };
            if (_combo.Items.Count > 0)
                _combo.SelectedIndex = 0;

            var buttonsPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            var okButton = new Button
            {
                Content = "Aceptar",
                Width = 80,
                Margin = new Thickness(0, 0, 8, 0)
            };
            okButton.Click += (s, e) =>
            {
                SelectedRoom = _combo.SelectedItem as Room;
                DialogResult = true;
            };

            var cancelButton = new Button
            {
                Content = "Cancelar",
                Width = 80
            };
            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
            };

            buttonsPanel.Children.Add(okButton);
            buttonsPanel.Children.Add(cancelButton);

            panel.Children.Add(text);
            panel.Children.Add(_combo);
            panel.Children.Add(buttonsPanel);

            Content = panel;
        }
    }

    #region LLM/IA para géneros gramaticales

    /// <summary>
    /// Llama al LLM para determinar el género gramatical y número (singular/plural) de objetos y puertas que no tienen valores manuales.
    /// </summary>
    private async Task ApplyLlmGendersAsync()
    {
        if (_world == null) return;

        // Recopilar nombres de objetos y puertas que NO tienen género/plural manual
        var itemsToAnalyze = new List<(string id, string name, string type)>();

        foreach (var obj in _world.Objects.Where(o => !o.GenderAndPluralSetManually))
        {
            itemsToAnalyze.Add((obj.Id, obj.Name, "objeto"));
        }

        foreach (var door in _world.Doors.Where(d => !d.GenderAndPluralSetManually))
        {
            itemsToAnalyze.Add((door.Id, door.Name, "puerta"));
        }

        if (itemsToAnalyze.Count == 0)
            return;

        ShowPlayLoading("Analizando géneros con IA...");

        try
        {
            // Construir prompt
            var namesListBuilder = new StringBuilder();
            for (int i = 0; i < itemsToAnalyze.Count; i++)
            {
                namesListBuilder.AppendLine($"{i + 1}. {itemsToAnalyze[i].name}");
            }

            var prompt = $@"Eres un experto en gramática española. Analiza los siguientes nombres de objetos/elementos de un juego de aventuras y determina su género gramatical (masculino o femenino) y si son singulares o plurales.

NOMBRES A ANALIZAR:
{namesListBuilder}

INSTRUCCIONES:
1. Para cada nombre, determina:
   - Género: masculino (M) o femenino (F)
   - Número: singular (S) o plural (P)
2. Responde SOLO con una lista numerada con género y número separados por espacio, ejemplo:
1. M S
2. F S
3. M P
4. F P
...

NO añadas explicaciones, solo el número, género (M/F) y número gramatical (S/P).";

            var requestBody = new
            {
                model = "llama3",
                prompt = prompt,
                stream = false,
                options = new { temperature = 0.1 }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("api/generate", content);
            if (!response.IsSuccessStatusCode)
            {
                // Si falla, simplemente no actualizamos los géneros
                return;
            }

            var responseText = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseText);
            var llmResponse = doc.RootElement.GetProperty("response").GetString() ?? "";

            // Parsear respuesta
            var lines = llmResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // Formato esperado: "1. M S" o "1. F P"
                var parts = trimmed.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 2) continue;

                if (!int.TryParse(parts[0].Trim(), out var index)) continue;
                if (index < 1 || index > itemsToAnalyze.Count) continue;

                var valuePart = parts[1].Trim().ToUpperInvariant();
                var tokens = valuePart.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                // Extraer género (M/F)
                var gender = tokens.Length > 0 && tokens[0].StartsWith("M")
                    ? GrammaticalGender.Masculine
                    : GrammaticalGender.Feminine;

                // Extraer número (S/P) - por defecto singular
                var isPlural = tokens.Length > 1 && tokens[1].StartsWith("P");

                var item = itemsToAnalyze[index - 1];

                // Aplicar género y plural
                if (item.type == "objeto")
                {
                    var obj = _world.Objects.FirstOrDefault(o => o.Id == item.id);
                    if (obj != null && !obj.GenderAndPluralSetManually)
                    {
                        obj.Gender = gender;
                        obj.IsPlural = isPlural;
                    }
                }
                else if (item.type == "puerta")
                {
                    var door = _world.Doors.FirstOrDefault(d => d.Id == item.id);
                    if (door != null && !door.GenderAndPluralSetManually)
                    {
                        door.Gender = gender;
                        door.IsPlural = isPlural;
                    }
                }
            }
        }
        catch
        {
            // Si hay error, simplemente no actualizamos los géneros
        }
        finally
        {
            HidePlayLoading();
        }
    }

    /// <summary>
    /// Inicia Docker silenciosamente si la IA estaba activada en el mundo.
    /// Si falla, desactiva la IA y muestra un mensaje.
    /// </summary>
    private async Task EnsureDockerStartedForAiAsync()
    {
        var progressWindow = new DockerProgressWindow
        {
            Owner = this,
            IncludeTts = false,
            IncludeStableDiffusion = true // En el editor usamos Ollama + SD
        };

        var result = await progressWindow.RunAsync().ConfigureAwait(true);

        if (result.Canceled || !result.Success)
        {
            // Desactivar la IA si Docker no se pudo iniciar
            _useLlmForGenders = false;
            PropertyEditor.IsAiEnabled = false;

            if (!result.Canceled)
            {
                new AlertWindow(
                    "No se han podido iniciar los servicios de IA. La IA ha sido desactivada.\n\n" +
                    "Comprueba que Docker Desktop está instalado y en ejecución.",
                    "Error")
                {
                    Owner = this
                }.ShowDialog();
            }
        }
    }

    private void RefreshPropertyEditor()
    {
        // Guardar el objeto actual y refrescarlo para actualizar la visibilidad del checkbox
        var currentObject = PropertyEditor.GetCurrentObject();
        if (currentObject != null)
        {
            PropertyEditor.SetObject(currentObject);
        }
    }

    #endregion

    #region Zone Filter

    private string? _currentZoneFilter = null;

    private void UpdateZoneFilter()
    {
        var zones = WorldLoader.GetZones(_world);

        // Siempre mostrar el filtro
        ZoneFilterPanel.Visibility = Visibility.Visible;

        // Guardar selección actual
        var previousSelection = ZoneFilterComboBox.SelectedItem as string;

        // Actualizar items
        ZoneFilterComboBox.Items.Clear();
        ZoneFilterComboBox.Items.Add("(Todas las zonas)");
        foreach (var zone in zones)
        {
            ZoneFilterComboBox.Items.Add(zone);
        }

        // Restaurar selección o seleccionar zona por defecto
        if (previousSelection != null && ZoneFilterComboBox.Items.Contains(previousSelection))
        {
            ZoneFilterComboBox.SelectedItem = previousSelection;
        }
        else if (zones.Contains("Inicial"))
        {
            // Si existe la zona "Inicial", seleccionarla por defecto
            ZoneFilterComboBox.SelectedItem = "Inicial";
        }
        else if (zones.Count > 0)
        {
            // Si hay zonas, seleccionar la primera
            ZoneFilterComboBox.SelectedItem = zones[0];
        }
        else
        {
            ZoneFilterComboBox.SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Actualiza solo los items del filtro de zonas sin disparar eventos de selección.
    /// Se usa cuando se renombra una zona para no perder el foco del campo de edición.
    /// </summary>
    private void UpdateZoneFilterItems()
    {
        var zones = WorldLoader.GetZones(_world);
        var currentIndex = ZoneFilterComboBox.SelectedIndex;

        // Desuscribir temporalmente el evento para evitar efectos secundarios
        ZoneFilterComboBox.SelectionChanged -= ZoneFilterComboBox_SelectionChanged;

        try
        {
            ZoneFilterComboBox.Items.Clear();
            ZoneFilterComboBox.Items.Add("(Todas las zonas)");
            foreach (var zone in zones)
            {
                ZoneFilterComboBox.Items.Add(zone);
            }

            // Restaurar el índice de selección (que ahora apunta al nombre actualizado)
            if (currentIndex >= 0 && currentIndex < ZoneFilterComboBox.Items.Count)
            {
                ZoneFilterComboBox.SelectedIndex = currentIndex;
            }
            else if (ZoneFilterComboBox.Items.Count > 0)
            {
                ZoneFilterComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            // Re-suscribir el evento
            ZoneFilterComboBox.SelectionChanged += ZoneFilterComboBox_SelectionChanged;
        }
    }

    private void ZoneFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (ZoneFilterComboBox.SelectedItem == null) return;

        var selected = ZoneFilterComboBox.SelectedItem as string;
        if (selected == "(Todas las zonas)")
        {
            _currentZoneFilter = null;
            MapPanel.SetZoneFilter(null);
        }
        else
        {
            _currentZoneFilter = selected;
            MapPanel.SetZoneFilter(selected);
        }
    }

    #endregion
}














