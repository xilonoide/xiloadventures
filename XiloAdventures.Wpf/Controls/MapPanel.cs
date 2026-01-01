using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Windows;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Controls;

public partial class MapPanel : Control
{
    private const double RoomBoxWidth = 160;
    private const double RoomBoxHeight = 90;
    private const double RoomSpacingMargin = 50.0;

    private WorldModel? _world;
    private string? _zoneFilter;

    // Estado del grid y snap-to-grid
    private bool _showGrid = false;
    private bool _snapToGrid = true;

    // Posiciones lógicas (coordenadas de mapa, no píxeles) del centro de cada sala
    private readonly Dictionary<string, Point> _roomPositions = new();
    // Rectángulos en pantalla (tras zoom + pan) para hit testing
    private readonly Dictionary<string, Rect> _roomRects = new();
    // Salas seleccionadas (por Id)
    private readonly HashSet<string> _selectedRoomIds = new();
    // Sala donde está el jugador durante la prueba (para mostrar resplandor)
    private string? _testPlayerRoomId;
    // Puertas del GameState para mostrar estado actualizado en modo pruebas
    private List<Door>? _testDoors;
    // Rectángulos de los puertos de salida (roomId, dirección)
    private readonly Dictionary<(string roomId, string direction), Rect> _portRects = new();
    // Rectángulos de hit testing para las salidas (exits), indexadas por sala + índice de salida
    private readonly Dictionary<(string roomId, int exitIndex), Rect> _exitHitRects = new();
    // Salidas seleccionadas (por sala + índice)
    private readonly HashSet<(string roomId, int exitIndex)> _selectedExits = new();
    // Rectángulos de iconos de objetos y NPC por sala
    private readonly Dictionary<string, Rect> _roomObjectIconRects = new();
    private readonly Dictionary<string, Rect> _roomNpcIconRects = new();
    private readonly Dictionary<string, Rect> _roomStartIconRects = new();
    private readonly Dictionary<string, Rect> _doorIconRects = new();
    private readonly Dictionary<string, Rect> _keyIconRects = new();


    // Tooltip para iconos de objetos/NPCs
    private ToolTip? _iconToolTip;
    private string? _iconTooltipRoomId;
    private bool _iconTooltipIsObjectIcon;

    // Tooltip para imagen de salas
    private ToolTip? _roomImageToolTip;
    private string? _roomImageTooltipRoomId;

    private string? _pendingPortDirection;

    // Transformación de vista
    private double _zoom = 1.0;
    private Point _offset = new(0, 0); // offset lógico

    // Pan con botón central
    private bool _isPanning;
    private Point _lastMiddleDown;

    // Arrastre de salas
    private bool _isDraggingRooms;
    private Point _dragStartMouseScreen;
    private readonly Dictionary<string, Point> _dragStartLogicalPositions = new();

    // Selección por rectángulo
    private bool _isDragSelecting;
    private Point _selectionStartScreen;
    private Point _selectionEndScreen;

    // Creación de conexiones (Ctrl + arrastrar de una sala a otra)
    private Room? _connectionStart;
    private Point _connectionCurrentMouseScreen;

    // Reedición de salidas existentes (arrastrar desde puerto con salida)
    private bool _isEditingExit;
    private Room? _editingExitRoom;
    private int _editingExitIndex;
    private string? _editingExitOriginalTarget;
    private bool _editingExitIsOrigin; // true = editando origen, false = editando destino

    // Para distinguir click de arrastre
    private Point _mouseDownScreen;
    private Room? _mouseDownRoom;

    // Animación de respiración para destacar una sala
    private string? _breathingRoomId;
    private DispatcherTimer? _breathingTimer;
    private double _breathingPhase; // 0 a 2*PI para un ciclo completo
    private int _breathingCyclesRemaining;
    private const int BreathingTotalCycles = 5;
    private const double BreathingCycleDuration = 800.0; // ms por ciclo (igual que DiceControl)

    public event Action<Room>? RoomClicked;
    public event Action<Door>? DoorClicked;
    public event Action<Door, GameObject?>? DoorCreated;
    public event Action<Door>? DoorDoubleClicked;
    public event Action<Room>? RoomDoubleClicked;
    public event Action<Point>? EmptyMapDoubleClicked;
    public event Action? SelectionCleared;
    public event Action? MapEdited;

    public event Action<Room>? AddObjectToRoomRequested;
    public event Action<Room>? AddNpcToRoomRequested;
    public event Action<List<string>>? RoomsDeleteRequested;
    public event Action<GameObject>? KeyIconClicked;
    public event Action<Room>? TeleportToRoomRequested;

    static MapPanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(
            typeof(MapPanel),
            new FrameworkPropertyMetadata(typeof(MapPanel)));
    }

    public MapPanel()
    {
        Focusable = true;
        Background = Brushes.Transparent;
        AllowDrop = true;

        // Configurar eventos de drag and drop (solo cursor feedback, el drop lo maneja WorldEditorWindow)
        DragEnter += MapPanel_DragEnter;
        DragOver += MapPanel_DragOver;

        // Centrar en sala inicial al cambiar tamaño
        SizeChanged += MapPanel_SizeChanged;
    }

    private void MapPanel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        CenterOnStartRoom();
    }

    public void CenterOnStartRoom()
    {
        if (_world?.Game?.StartRoomId == null)
            return;

        var startRoom = _world.Rooms.FirstOrDefault(r =>
            string.Equals(r.Id, _world.Game.StartRoomId, StringComparison.OrdinalIgnoreCase));

        if (startRoom != null)
        {
            CenterOnRoom(startRoom);
        }
    }

    public void SetWorld(WorldModel world)
    {
        // Si la instancia de mundo cambia (abrir archivo, mundo nuevo), reseteamos totalmente
        // las posiciones. Si es el mismo objeto, conservamos las posiciones existentes y solo
        // limpiamos las de salas eliminadas.
        if (!ReferenceEquals(_world, world))
        {
            _world = world;
            _roomPositions.Clear();

            // Si el mundo trae posiciones guardadas, las aplicamos.
            if (_world.RoomPositions != null)
            {
                foreach (var kv in _world.RoomPositions)
                {
                    _roomPositions[kv.Key] = new Point(kv.Value.X, kv.Value.Y);
                }
            }
        }
        else if (_world != null)
        {
            var existingIds = new HashSet<string>(_world.Rooms.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
            foreach (var key in _roomPositions.Keys.ToList())
            {
                if (!existingIds.Contains(key))
                    _roomPositions.Remove(key);
            }

            // Sincronizar posiciones desde world.RoomPositions (para reordenación)
            if (_world.RoomPositions != null)
            {
                foreach (var kv in _world.RoomPositions)
                {
                    _roomPositions[kv.Key] = new Point(kv.Value.X, kv.Value.Y);
                }
            }
        }

        _roomRects.Clear();
        _portRects.Clear();
        _exitHitRects.Clear();
        _roomObjectIconRects.Clear();
        _roomNpcIconRects.Clear();
        _selectedRoomIds.Clear();
        _selectedExits.Clear();
        _connectionStart = null;
        _isEditingExit = false;
        _editingExitRoom = null;
        _editingExitIndex = -1;
        _editingExitOriginalTarget = null;
        _editingExitIsOrigin = false;
        _keyIconRects.Clear();
        HideIconTooltip();
        HideRoomImageTooltip();
        EnsureLayout();
        InvalidateVisual();
    }

    public void SetZoneFilter(string? zone)
    {
        _zoneFilter = zone;
        InvalidateVisual();
    }

    public void SetSelectedRoom(Room? room)
    {
        _selectedRoomIds.Clear();
        if (room != null)
        {
            _selectedRoomIds.Add(room.Id);
        }
        InvalidateVisual();
    }

    public void CenterOnRoom(Room room)
    {
        if (_world == null || room == null)
            return;

        if (!_roomPositions.ContainsKey(room.Id))
        {
            // Nos aseguramos de tener posiciones calculadas
            EnsureLayout();
        }

        if (!_roomPositions.TryGetValue(room.Id, out var logicalCenter))
            return;

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        // Calculamos el offset lógico necesario para centrar la sala en pantalla
        _offset = new Point(
            width / (2.0 * _zoom) - logicalCenter.X,
            height / (2.0 * _zoom) - logicalCenter.Y);

        InvalidateVisual();
    }

    /// <summary>
    /// Inicia una animación de respiración (5 pulsos) para destacar una sala.
    /// Útil cuando se hace click en un objeto o NPC para resaltar su ubicación.
    /// </summary>
    public void HighlightRoomWithBreathing(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            return;

        // Detener animación anterior si existe
        StopBreathingAnimation();

        _breathingRoomId = roomId;
        _breathingPhase = 0;
        _breathingCyclesRemaining = BreathingTotalCycles;

        _breathingTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _breathingTimer.Tick += BreathingTimer_Tick;
        _breathingTimer.Start();
    }

    private void BreathingTimer_Tick(object? sender, EventArgs e)
    {
        // Incrementar fase basándose en el tiempo transcurrido
        // Un ciclo completo (0 a 2*PI) toma BreathingCycleDuration ms
        double phaseIncrement = (2 * Math.PI) / (BreathingCycleDuration / 16.0);
        _breathingPhase += phaseIncrement;

        // Si completamos un ciclo (la fase pasa de 2*PI)
        if (_breathingPhase >= 2 * Math.PI)
        {
            _breathingPhase -= 2 * Math.PI;
            _breathingCyclesRemaining--;

            if (_breathingCyclesRemaining <= 0)
            {
                StopBreathingAnimation();
                return;
            }
        }

        InvalidateVisual();
    }

    private void StopBreathingAnimation()
    {
        if (_breathingTimer != null)
        {
            _breathingTimer.Stop();
            _breathingTimer.Tick -= BreathingTimer_Tick;
            _breathingTimer = null;
        }

        _breathingRoomId = null;
        _breathingPhase = 0;
        _breathingCyclesRemaining = 0;
        InvalidateVisual();
    }

    /// <summary>
    /// Obtiene la intensidad actual de la animación de respiración (0.0 a 1.0).
    /// Usa una función seno para el efecto suave de respiración.
    /// </summary>
    public double GetBreathingIntensity()
    {
        if (_breathingRoomId == null)
            return 0;

        // Sin wave: de 0.4 a 1.0 (igual que DiceControl)
        // (1 + sin(phase - PI/2)) / 2 da valores de 0 a 1
        // Luego escalamos a 0.4-1.0
        double normalized = (1 + Math.Sin(_breathingPhase - Math.PI / 2)) / 2.0;
        return 0.4 + normalized * 0.6;
    }

    public void CenterOnDoor(Door door)
    {
        if (_world == null || door == null)
            return;

        EnsureLayout();

        // Obtener posiciones de las dos salas que conecta la puerta
        Point? posA = null, posB = null;

        if (!string.IsNullOrEmpty(door.RoomIdA) && _roomPositions.TryGetValue(door.RoomIdA, out var pA))
            posA = pA;
        if (!string.IsNullOrEmpty(door.RoomIdB) && _roomPositions.TryGetValue(door.RoomIdB, out var pB))
            posB = pB;

        Point logicalCenter;
        if (posA.HasValue && posB.HasValue)
        {
            // Punto medio entre las dos salas
            logicalCenter = new Point(
                (posA.Value.X + posB.Value.X) / 2,
                (posA.Value.Y + posB.Value.Y) / 2);
        }
        else if (posA.HasValue)
        {
            logicalCenter = posA.Value;
        }
        else if (posB.HasValue)
        {
            logicalCenter = posB.Value;
        }
        else
        {
            return;
        }

        double width = ActualWidth;
        double height = ActualHeight;
        if (width <= 0 || height <= 0)
            return;

        _offset = new Point(
            width / (2.0 * _zoom) - logicalCenter.X,
            height / (2.0 * _zoom) - logicalCenter.Y);

        InvalidateVisual();
    }

    /// <summary>
    /// Devuelve las salas actualmente seleccionadas.
    /// </summary>
    public IReadOnlyList<Room> GetSelectedRooms()
    {
        if (_world == null)
            return Array.Empty<Room>();

        return _world.Rooms
            .Where(r => _selectedRoomIds.Contains(r.Id))
            .ToList();
    }

    /// <summary>
    /// Marca como seleccionadas las salas indicadas (si pertenecen al mundo actual).
    /// </summary>
    public void SetSelectedRooms(IEnumerable<Room> rooms)
    {
        _selectedRoomIds.Clear();

        if (_world == null)
        {
            InvalidateVisual();
            return;
        }

        var validIds = new HashSet<string>(_world.Rooms.Select(r => r.Id));

        foreach (var room in rooms)
        {
            if (validIds.Contains(room.Id))
            {
                _selectedRoomIds.Add(room.Id);
            }
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Establece la sala donde está el jugador durante la prueba (para mostrar resplandor).
    /// Pasar null para quitar el resplandor.
    /// </summary>
    public void SetTestPlayerRoom(string? roomId)
    {
        if (_testPlayerRoomId != roomId)
        {
            _testPlayerRoomId = roomId;
            InvalidateVisual();
        }
    }

    /// <summary>
    /// Establece las puertas del GameState para mostrar su estado actualizado en modo pruebas.
    /// Pasar null para usar las puertas del WorldModel.
    /// </summary>
    public void SetTestDoors(List<Door>? doors)
    {
        _testDoors = doors;
        InvalidateVisual();
    }

    public void ClearSelection()
    {
        _selectedRoomIds.Clear();
        _selectedExits.Clear();
        InvalidateVisual();
    }

    /// <summary>
    /// Devuelve las posiciones lógicas actuales de las salas indicadas.
    /// </summary>
    public IReadOnlyDictionary<string, Point> GetRoomPositions(IEnumerable<string> roomIds)
    {
        var result = new Dictionary<string, Point>();
        foreach (var id in roomIds)
        {
            if (_roomPositions.TryGetValue(id, out var p))
            {
                result[id] = p;
            }
        }
        return result;
    }

    /// <summary>
    /// Establece la posición lógica de una sala específica.
    /// </summary>
    public void SetRoomPosition(string roomId, Point position)
    {
        if (_world == null)
            return;

        _roomPositions[roomId] = position;
        UpdateRoomRects();
        InvalidateVisual();
    }

    /// <summary>
    /// Establece las posiciones lógicas de varias salas a la vez.
    /// </summary>
    public void SetRoomsPositions(IDictionary<string, Point> positions)
    {
        if (_world == null)
            return;

        foreach (var kv in positions)
        {
            _roomPositions[kv.Key] = kv.Value;
        }

        UpdateRoomRects();
        InvalidateVisual();
    }

    /// <summary>
    /// Coloca las salas indicadas en la esquina superior izquierda del mapa, en fila.
    /// Útil para "Pegar".
    /// </summary>
    public void PlaceRoomsAtTopLeft(IEnumerable<Room> rooms)
    {
        double x = RoomBoxWidth / 2 + 20;
        double y = RoomBoxHeight / 2 + 20;
        double stepX = RoomBoxWidth + RoomSpacingMargin;

        double width = ActualWidth;
        double height = ActualHeight;
        bool hasSize = width > 0 && height > 0;

        var placedCenters = new Dictionary<string, Point>();

        foreach (var room in rooms)
        {
            Point candidate = new(x, y);

            if (hasSize)
            {
                candidate = ClampRoomCenterToMap(candidate);

                int safetyCounter = 0;
                while (HasCollisionWithOtherRooms(room.Id, candidate, RoomSpacingMargin, null) ||
                       placedCenters.Values.Any(p =>
                           GetRoomRectFromCenter(candidate, RoomSpacingMargin)
                               .IntersectsWith(GetRoomRectFromCenter(p, RoomSpacingMargin))))
                {
                    // Desplazamos hacia la derecha hasta encontrar un hueco libre
                    x += stepX;
                    candidate = ClampRoomCenterToMap(new Point(x, y));

                    safetyCounter++;
                    if (safetyCounter > 50)
                        break;
                }
            }

            _roomPositions[room.Id] = candidate;
            placedCenters[room.Id] = candidate;
            x += stepX;
        }

        InvalidateVisual();
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

    public bool IsGridVisible => _showGrid;
    public bool IsSnapToGridEnabled => _snapToGrid;

    public void SetGridVisibility(bool visible)
    {
        _showGrid = visible;
        InvalidateVisual();
    }

    public void SetSnapToGrid(bool enabled)
    {
        _snapToGrid = enabled;
    }

    #region Patrol Route Editing

    // Estado de edición de ruta de patrulla
    private bool _isEditingPatrolRoute;
    private Npc? _patrolRouteNpc;

    // NPCs cuyas rutas de patrulla deben mostrarse (cuando no se está editando)
    private HashSet<string> _visiblePatrolRouteNpcIds = new(StringComparer.OrdinalIgnoreCase);
    private bool _showAllPatrolRoutes;

    /// <summary>
    /// Evento cuando la ruta de patrulla ha sido modificada.
    /// </summary>
    public event Action<Npc>? PatrolRouteEdited;

    /// <summary>
    /// Indica si se está editando una ruta de patrulla.
    /// </summary>
    public bool IsEditingPatrolRoute => _isEditingPatrolRoute;

    /// <summary>
    /// NPC cuya ruta de patrulla se está editando.
    /// </summary>
    public Npc? PatrolRouteNpc => _patrolRouteNpc;

    /// <summary>
    /// Inicia el modo de edición de ruta de patrulla para un NPC.
    /// </summary>
    public void StartEditingPatrolRoute(Npc npc)
    {
        _isEditingPatrolRoute = true;
        _patrolRouteNpc = npc;
        _selectedRoomIds.Clear();
        InvalidateVisual();
    }

    /// <summary>
    /// Finaliza el modo de edición de ruta de patrulla.
    /// </summary>
    public void StopEditingPatrolRoute()
    {
        if (_isEditingPatrolRoute && _patrolRouteNpc != null)
        {
            PatrolRouteEdited?.Invoke(_patrolRouteNpc);
        }
        _isEditingPatrolRoute = false;
        _patrolRouteNpc = null;
        InvalidateVisual();
    }

    /// <summary>
    /// Establece qué NPCs deben mostrar sus rutas de patrulla.
    /// Si npcId es null, no se muestra ninguna ruta.
    /// Si showAll es true, se muestran todas las rutas de NPCs.
    /// </summary>
    public void SetVisiblePatrolRoutes(string? npcId, bool showAll = false)
    {
        _visiblePatrolRouteNpcIds.Clear();
        _showAllPatrolRoutes = showAll;

        if (npcId != null)
        {
            _visiblePatrolRouteNpcIds.Add(npcId);
        }

        InvalidateVisual();
    }

    /// <summary>
    /// Añade o quita una sala de la ruta de patrulla del NPC.
    /// </summary>
    private void ToggleRoomInPatrolRoute(string roomId)
    {
        if (_patrolRouteNpc == null || _world == null) return;

        var index = _patrolRouteNpc.PatrolRoute.FindIndex(id =>
            string.Equals(id, roomId, StringComparison.OrdinalIgnoreCase));

        if (index >= 0)
        {
            // Solo se puede quitar la última sala de la ruta
            if (index != _patrolRouteNpc.PatrolRoute.Count - 1)
            {
                AlertWindow.Show(
                    "No se puede quitar",
                    "Solo puedes quitar la última sala de la ruta.\n\n" +
                    "Las salas deben quitarse en orden inverso a como se añadieron.",
                    Window.GetWindow(this));
                return;
            }

            // Preguntar antes de quitar de la ruta
            var room = _world.Rooms.FirstOrDefault(r =>
                string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
            var roomName = room?.Name ?? roomId;

            var confirmWindow = new AlertWindow(
                $"¿Quitar '{roomName}' de la ruta de patrulla?",
                "Quitar de la ruta");
            confirmWindow.ShowCancelButton(true);
            confirmWindow.SetOkButtonText("Quitar");
            confirmWindow.Owner = Window.GetWindow(this);

            if (confirmWindow.ShowDialog() == true)
            {
                _patrolRouteNpc.PatrolRoute.RemoveAt(index);
            }
            else
            {
                return; // No hacer nada si cancela
            }
        }
        else
        {
            // Si es la primera sala de la ruta, verificar si coincide con la ubicación actual del NPC
            if (_patrolRouteNpc.PatrolRoute.Count == 0)
            {
                if (!string.Equals(_patrolRouteNpc.RoomId, roomId, StringComparison.OrdinalIgnoreCase))
                {
                    var room = _world.Rooms.FirstOrDefault(r =>
                        string.Equals(r.Id, roomId, StringComparison.OrdinalIgnoreCase));
                    var roomName = room?.Name ?? roomId;

                    var confirmWindow = new AlertWindow(
                        $"El NPC no está en esta sala.\n\n" +
                        $"Si seleccionas '{roomName}' como inicio de la ruta, " +
                        $"el NPC será movido a esta sala.\n\n" +
                        $"¿Continuar?",
                        "Mover NPC");
                    confirmWindow.ShowCancelButton(true);
                    confirmWindow.SetOkButtonText("Mover y añadir");
                    confirmWindow.Owner = Window.GetWindow(this);

                    if (confirmWindow.ShowDialog() != true)
                    {
                        return; // Cancelado
                    }

                    // Mover el NPC a la nueva sala
                    _patrolRouteNpc.RoomId = roomId;
                }
            }

            // Añadir al final de la ruta - validar conexiones
            if (_patrolRouteNpc.PatrolRoute.Count > 0)
            {
                var lastRoomId = _patrolRouteNpc.PatrolRoute[^1];

                // Validar que la última sala tenga salida hacia la nueva sala
                if (!AreRoomsConnected(lastRoomId, roomId))
                {
                    AlertWindow.Show(
                        "Ruta de patrulla",
                        $"No hay salida desde la última sala de la ruta hacia esta sala.\n\n" +
                        $"Necesitas crear una salida que conecte las salas.",
                        Window.GetWindow(this));
                    return;
                }

            }
            _patrolRouteNpc.PatrolRoute.Add(roomId);
        }

        MapEdited?.Invoke();
        InvalidateVisual();
    }

    /// <summary>
    /// Verifica si dos salas están conectadas directamente (si existe una salida de A hacia B).
    /// </summary>
    private bool AreRoomsConnected(string roomIdA, string roomIdB)
    {
        if (_world == null) return false;
        if (string.IsNullOrEmpty(roomIdA) || string.IsNullOrEmpty(roomIdB)) return false;

        var roomA = _world.Rooms.FirstOrDefault(r =>
            string.Equals(r.Id, roomIdA, StringComparison.OrdinalIgnoreCase));

        if (roomA == null) return false;
        if (roomA.Exits == null || roomA.Exits.Count == 0) return false;

        // Buscar si existe una salida desde roomA hacia roomB
        foreach (var exit in roomA.Exits)
        {
            if (exit != null &&
                !string.IsNullOrEmpty(exit.TargetRoomId) &&
                string.Equals(exit.TargetRoomId, roomIdB, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    #endregion

    /// <summary>
    /// Encuentra la posición libre más cercana para una nueva sala, considerando el snap-to-grid si está activado.
    /// </summary>
    /// <param name="desiredPosition">La posición deseada inicial</param>
    /// <returns>La posición libre más cercana donde se puede colocar la sala</returns>
    public Point FindNearestFreePosition(Point desiredPosition)
    {
        // Si snap-to-grid está activado, ajustar primero a la celda más cercana
        Point candidate = _snapToGrid ? SnapToGridCell(desiredPosition) : desiredPosition;

        // Asegurar que está dentro del mapa
        candidate = ClampRoomCenterToMap(candidate);

        // Si no hay colisión con ninguna sala existente, usar esta posición
        if (!HasCollisionWithOtherRooms(string.Empty, candidate, RoomSpacingMargin, null))
            return candidate;

        // Si hay colisión, buscar la posición libre más cercana
        return FindNearestNonCollidingPosition(string.Empty, candidate, RoomSpacingMargin);
    }

}
