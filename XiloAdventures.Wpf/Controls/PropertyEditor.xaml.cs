using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Windows;
using XiloAdventures.Wpf.Helpers;
using XiloAdventures.Wpf.Windows;
using PN = XiloAdventures.Wpf.Helpers.PropertyNames;

namespace XiloAdventures.Wpf.Controls;

public partial class PropertyEditor : UserControl
{
    private PasswordBox? _encryptionPasswordBox;
    private object? _currentObject;

    // Diccionario para rastrear elementos que deben mostrarse/ocultarse dinámicamente
    private readonly Dictionary<string, UIElement> _propertyElements = new();
    // Diccionario para rastrear las condiciones de visibilidad de cada propiedad
    private readonly Dictionary<string, Func<bool>> _visibilityConditions = new();
    // Diccionario para rastrear a qué sección pertenece cada propiedad (por nombre de sección)
    private readonly Dictionary<string, string> _propertyToSectionName = new();
    // Nombre de la sección que se está construyendo actualmente
    private string? _currentBuildingSectionName;

    // Contador de secciones para determinar cuál debe estar expandida (la primera)
    private int _sectionIndex;

    // Lista de Expanders creados para expandir/contraer todo
    private readonly List<Expander> _expanders = new();

    // Diccionario que asocia cada Expander con su nombre de sección
    private readonly Dictionary<Expander, string> _expanderSectionNames = new();

    // Diccionario para búsqueda: nombre de propiedad traducido → (elemento UI, Expander padre)
    private readonly Dictionary<string, (UIElement Element, Expander? ParentExpander)> _searchableElements = new();

    // TextBlock para mostrar el total de puntos de características del jugador
    private TextBlock? _attributesTotalLabel;

    // Diccionario para persistir el estado de expansión de las secciones por tipo de objeto
    // Clave: tipo de objeto (ej: "Room", "Npc"), Valor: diccionario de sección -> estado expandido
    private static readonly Dictionary<string, Dictionary<string, bool>> _expanderStates = new();

    public event Action<object?, string>? PropertyEdited;

    /// <summary>
    /// Evento para solicitar la eliminación de un objeto por su ID.
    /// </summary>
    public event Action<string>? RequestDeleteObject;

    /// <summary>
    /// Evento para solicitar la generación de imagen con IA para una sala.
    /// </summary>
    public event Action<Room>? RequestAiImageGeneration;

    /// <summary>
    /// Evento para solicitar la generación de descripción con IA para una sala.
    /// </summary>
    public event Action<Room>? RequestAiDescriptionGeneration;

    /// <summary>
    /// Evento para solicitar la apertura del gestor de habilidades mágicas.
    /// </summary>
    public event Action? RequestManageAbilities;

    public PasswordBox? EncryptionPasswordBox => _encryptionPasswordBox;

    public Func<IEnumerable<Room>>? GetRooms { get; set; }

    public Func<IEnumerable<string>>? GetZones { get; set; }

    public Func<IEnumerable<MusicAsset>>? GetMusics { get; set; }

    public Func<IEnumerable<GameObject>>? GetObjects { get; set; }

    public Func<IEnumerable<CombatAbility>>? GetAbilities { get; set; }

    public Func<IEnumerable<QuestDefinition>>? GetQuests { get; set; }

    public Func<IEnumerable<Npc>>? GetNpcs { get; set; }

    public Func<IEnumerable<Door>>? GetDoors { get; set; }

    public Func<PlayerDefinition?>? GetPlayerDefinition { get; set; }

    /// <summary>
    /// Obtiene la información del juego actual.
    /// </summary>
    public Func<GameInfo?>? GetGameInfo { get; set; }

    /// <summary>
    /// Obtiene el diccionario del parser del juego actual (JSON).
    /// </summary>
    public Func<string?>? GetParserDictionary { get; set; }

    /// <summary>
    /// Establece el diccionario del parser del juego actual (JSON).
    /// </summary>
    public Action<string?>? SetParserDictionary { get; set; }

    /// <summary>
    /// Indica si la IA está activada en el editor. Controla la visibilidad del checkbox de género/plural manual.
    /// </summary>
    public bool IsAiEnabled { get; set; }

    /// <summary>
    /// Expone el ScrollViewer interno para scroll programático desde el exterior.
    /// </summary>
    public ScrollViewer InternalScrollViewer => MainScrollViewer;

    public PropertyEditor()
    {
        InitializeComponent();
    }

    public object? GetCurrentObject()
    {
        return _currentObject;
    }

    /// <summary>
    /// Guarda el estado de expansión actual de todas las secciones para el objeto actual.
    /// </summary>
    private void SaveExpanderStates()
    {
        if (_currentObject == null || _expanders.Count == 0)
            return;

        var objectType = _currentObject.GetType().Name;

        if (!_expanderStates.ContainsKey(objectType))
            _expanderStates[objectType] = new Dictionary<string, bool>();

        foreach (var expander in _expanders)
        {
            if (_expanderSectionNames.TryGetValue(expander, out var sectionName) && !string.IsNullOrEmpty(sectionName))
            {
                _expanderStates[objectType][sectionName] = expander.IsExpanded;
            }
        }
    }

    /// <summary>
    /// Obtiene el estado de expansión guardado para una sección, si existe.
    /// </summary>
    private bool? GetSavedExpanderState(object? obj, string? sectionName)
    {
        if (obj == null || string.IsNullOrEmpty(sectionName))
            return null;

        var objectType = obj.GetType().Name;

        if (_expanderStates.TryGetValue(objectType, out var sectionStates))
        {
            if (sectionStates.TryGetValue(sectionName, out var isExpanded))
                return isExpanded;
        }

        return null;
    }

    public void SetObject(object? obj)
    {
        // Guardar el estado de expansión del objeto anterior antes de limpiar
        SaveExpanderStates();

        _currentObject = obj;
        _encryptionPasswordBox = null;
        _attributesTotalLabel = null;
        _sectionIndex = 0;
        RootPanel.Children.Clear();
        _propertyElements.Clear();
        _visibilityConditions.Clear();
        _propertyToSectionName.Clear();
        _currentBuildingSectionName = null;
        _expanders.Clear();
        _expanderSectionNames.Clear();
        _searchableElements.Clear();

        if (obj == null)
            return;

        // Las carpetas del editor no tienen propiedades visibles (se editan con F2 en el árbol)
        if (obj is EditorFolder)
            return;

        var props = obj.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p =>
            {
                var browsable = p.GetCustomAttribute<BrowsableAttribute>();
                return browsable == null || browsable.Browsable;
            })
            .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() == null) // Excluir propiedades runtime
            .Where(p => p.Name != "Exits" && p.Name != "GenderAndPluralSetManually" && p.Name != "PatrolRoute" && p.Name != "Stats" && p.Name != "AbilityIds")
            .Where(p => !(obj is Npc && p.Name == "MagicEnabled")) // NPC MagicEnabled se maneja en AddNpcSystemsSection
            .ToList();

        // Agrupar propiedades por categoría
        var groups = GroupProperties(obj, props);

        // Separar el grupo "Otros" para renderizarlo al final
        var otrosDisplayName = PropertyCategory.Otros.ToDisplayString();
        var otrosGroup = groups.FirstOrDefault(g => g.Name == otrosDisplayName);
        var mainGroups = groups.Where(g => g.Name != otrosDisplayName).ToList();

        foreach (var group in mainGroups)
        {
            if (!group.Properties.Any())
                continue;

            // Crear contenido del acordeón
            var contentPanel = new StackPanel();

            // Si es PlayerDefinition y grupo Características, crear header especial con total de puntos
            object headerContent;
            if (obj is PlayerDefinition playerDef && group.Name == PropertyCategory.Caracteristicas.ToDisplayString())
            {
                var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };

                var headerText = new TextBlock
                {
                    Text = group.Name.ToUpper(),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0xFF))
                };
                headerPanel.Children.Add(headerText);

                var total = playerDef.TotalAttributePoints;
                _attributesTotalLabel = new TextBlock
                {
                    Text = $" ({total}/100)",
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = GetAttributesTotalColor(total),
                    VerticalAlignment = VerticalAlignment.Center
                };
                headerPanel.Children.Add(_attributesTotalLabel);
                headerContent = headerPanel;
            }
            else
            {
                headerContent = group.Name.ToUpper();
            }

            // Trackear la sección actual para asociar propiedades
            _currentBuildingSectionName = group.Name;

            // Propiedades del grupo
            foreach (var prop in group.Properties)
            {
                AddPropertyControlToPanel(obj, prop, contentPanel);
            }

            // Crear acordeón
            AddAccordionSection(headerContent, contentPanel, obj, group.Name);
            _currentBuildingSectionName = null;
        }

        // Añadir sección de Estadísticas de combate para NPCs (solo si combate está habilitado)
        var gameInfoForSections = GetGameInfo?.Invoke();
        if (gameInfoForSections?.CombatEnabled == true)
        {
            AddNpcStatsSection(obj);
        }

        // Añadir sección de Sistemas para NPCs (Magia) - solo si combate está habilitado
        if (gameInfoForSections?.CombatEnabled == true)
        {
            AddNpcSystemsSection(obj);
        }

        // Añadir sección de habilidades para PlayerDefinition
        AddPlayerAbilitiesSection(obj);

        // Añadir grupo "Otros" al final (si tiene propiedades o sinónimos)
        var synonymPanel = CreateSynonymEditorPanel(obj);
        var hasOtrosProperties = otrosGroup != null && otrosGroup.Properties.Any();

        if (hasOtrosProperties || synonymPanel != null)
        {
            var contentPanel = new StackPanel();

            // Trackear la sección actual
            _currentBuildingSectionName = otrosDisplayName;

            // Añadir propiedades de "Otros"
            if (hasOtrosProperties)
            {
                foreach (var prop in otrosGroup!.Properties)
                {
                    AddPropertyControlToPanel(obj, prop, contentPanel);
                }
            }

            // Añadir editor de sinónimos al final
            if (synonymPanel != null)
            {
                contentPanel.Children.Add(synonymPanel);
            }

            AddAccordionSection(otrosDisplayName.ToUpper(), contentPanel, obj, otrosDisplayName);
            _currentBuildingSectionName = null;
        }

        // Aplicar visibilidad inicial de secciones
        UpdateSectionVisibility();
    }

    /// <summary>
    /// Crea una sección de acordeón (Expander) con el estilo del editor.
    /// La expansión por defecto depende del tipo de objeto y la sección.
    /// </summary>
    private void AddAccordionSection(object headerContent, UIElement content, object? obj = null, string? sectionName = null)
    {
        // Crear el header con el estilo azul
        var headerElement = headerContent is string headerText
            ? new TextBlock
            {
                Text = headerText,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0xFF))
            }
            : (UIElement)headerContent;

        // Determinar si la sección debe estar expandida
        // Primero intentar usar el estado guardado, si no existe usar el comportamiento por defecto
        var savedState = GetSavedExpanderState(obj, sectionName);
        bool isExpanded = savedState ?? ShouldSectionBeExpanded(obj, sectionName, _sectionIndex);

        var expander = new Expander
        {
            Header = headerElement,
            Content = content,
            IsExpanded = isExpanded,
            Margin = new Thickness(0, _sectionIndex == 0 ? 0 : 4, 0, 0),
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = Brushes.White
        };

        // Estilo para el expander
        expander.Style = CreateExpanderStyle();

        RootPanel.Children.Add(expander);
        _expanders.Add(expander);

        // Registrar el nombre de sección para persistencia del estado de expansión
        if (!string.IsNullOrEmpty(sectionName))
            _expanderSectionNames[expander] = sectionName;

        // Registrar elementos searchables del contenido
        RegisterSearchableElements(content, expander);

        _sectionIndex++;
    }

    /// <summary>
    /// Determina si una sección debe estar expandida por defecto según el tipo de objeto.
    /// </summary>
    private bool ShouldSectionBeExpanded(object? obj, string? sectionName, int sectionIndex)
    {
        // Si no hay información del objeto, usar el comportamiento por defecto (primera sección expandida)
        if (obj == null || sectionName == null)
            return sectionIndex == 0;

        // Player: todos los apartados expandidos
        if (obj is PlayerDefinition)
            return true;

        // NPC: Comportamiento expandido, además de la primera sección (Identificación)
        if (obj is Npc)
            return sectionIndex == 0 || sectionName.Contains("Comportamiento");

        // Room: Descripción y Comportamiento expandidos, además de la primera sección
        if (obj is Room)
            return sectionIndex == 0 || sectionName.Contains("Descripción") || sectionName.Contains("Comportamiento");

        // GameObject: Comportamiento y Estadísticas expandidos, además de la primera sección
        if (obj is GameObject)
            return sectionIndex == 0 || sectionName.Contains("Comportamiento") || sectionName.Contains("Estadísticas");

        // Por defecto: solo la primera sección expandida
        return sectionIndex == 0;
    }

    /// <summary>
    /// Crea el estilo para los Expanders del acordeón.
    /// </summary>
    private Style CreateExpanderStyle()
    {
        var style = new Style(typeof(Expander));

        // Fondo y borde
        style.Setters.Add(new Setter(Expander.BackgroundProperty, Brushes.Transparent));
        style.Setters.Add(new Setter(Expander.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))));
        style.Setters.Add(new Setter(Expander.BorderThicknessProperty, new Thickness(0, 0, 0, 1)));
        style.Setters.Add(new Setter(Expander.PaddingProperty, new Thickness(0, 4, 0, 8)));
        style.Setters.Add(new Setter(Expander.ForegroundProperty, Brushes.White));

        return style;
    }

    /// <summary>
    /// Registra los elementos de un panel como searchables, asociándolos con su Expander padre.
    /// </summary>
    private void RegisterSearchableElements(UIElement content, Expander parentExpander)
    {
        if (content is not Panel panel) return;

        foreach (UIElement child in panel.Children)
        {
            // Buscar TextBlocks que son labels de propiedades
            if (child is TextBlock textBlock && !string.IsNullOrWhiteSpace(textBlock.Text))
            {
                var text = textBlock.Text.Trim();
                if (!_searchableElements.ContainsKey(text))
                {
                    _searchableElements[text] = (child, parentExpander);
                }
            }
            // Recursivamente buscar en paneles hijos
            else if (child is Panel childPanel)
            {
                RegisterSearchableElementsInPanel(childPanel, parentExpander);
            }
        }
    }

    /// <summary>
    /// Registra elementos searchables recursivamente en un panel.
    /// </summary>
    private void RegisterSearchableElementsInPanel(Panel panel, Expander parentExpander)
    {
        foreach (UIElement child in panel.Children)
        {
            if (child is TextBlock textBlock && !string.IsNullOrWhiteSpace(textBlock.Text))
            {
                var text = textBlock.Text.Trim();
                if (!_searchableElements.ContainsKey(text))
                {
                    _searchableElements[text] = (child, parentExpander);
                }
            }
            else if (child is Panel childPanel)
            {
                RegisterSearchableElementsInPanel(childPanel, parentExpander);
            }
        }
    }

    /// <summary>
    /// Expande todas las secciones acordeón.
    /// </summary>
    public void ExpandAll()
    {
        foreach (var expander in _expanders)
        {
            expander.IsExpanded = true;
        }
    }

    /// <summary>
    /// Contrae todas las secciones acordeón.
    /// </summary>
    public void CollapseAll()
    {
        for (int i = 0; i < _expanders.Count; i++)
        {
            // La primera sección siempre queda expandida
            _expanders[i].IsExpanded = (i == 0);
        }
    }

    /// <summary>
    /// Busca una propiedad por texto y devuelve el elemento encontrado junto con su Expander padre.
    /// Si el texto está vacío, devuelve null.
    /// </summary>
    public (UIElement? Element, Expander? ParentExpander) SearchProperty(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return (null, null);

        searchText = searchText.Trim().ToLowerInvariant();

        // Buscar coincidencia parcial (contains) en los nombres de propiedades
        foreach (var kvp in _searchableElements)
        {
            if (kvp.Key.ToLowerInvariant().Contains(searchText))
            {
                return kvp.Value;
            }
        }

        return (null, null);
    }

    /// <summary>
    /// Añade un control de propiedad a un panel específico (para uso con acordeones).
    /// </summary>
    private void AddPropertyControlToPanel(object obj, PropertyInfo prop, Panel targetPanel)
    {
        // Guardar referencia al panel original
        var originalPanel = RootPanel;

        // Crear un panel temporal como wrapper
        var wrapperPanel = new StackPanel();

        // Usar reflexión para redirigir temporalmente RootPanel al wrapper
        // En lugar de eso, extraemos los controles creados
        var childCountBefore = RootPanel.Children.Count;
        AddPropertyControl(obj, prop);
        var childCountAfter = RootPanel.Children.Count;

        // Mover los elementos recién creados al panel de destino
        var elementsToMove = new List<UIElement>();
        for (int i = childCountBefore; i < childCountAfter; i++)
        {
            elementsToMove.Add(RootPanel.Children[i]);
        }

        foreach (var element in elementsToMove)
        {
            RootPanel.Children.Remove(element);
            targetPanel.Children.Add(element);
        }
    }

    /// <summary>
    /// Crea un panel con label y botón para editar sinónimos del parser.
    /// Retorna null si el objeto no soporta sinónimos o no hay acceso al diccionario.
    /// </summary>
    private StackPanel? CreateSynonymEditorPanel(object obj)
    {
        // Solo para objetos, NPCs y puertas
        if (obj is not GameObject and not Npc and not Door)
            return null;

        // Verificar que tenemos acceso al diccionario
        if (GetParserDictionary == null || SetParserDictionary == null)
            return null;

        // Crear panel contenedor con label
        var container = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };

        // Label
        var label = new TextBlock
        {
            Text = "Sinónimos del parser",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            Margin = new Thickness(0, 0, 0, 4)
        };
        container.Children.Add(label);

        // Botón y estado
        var currentJson = GetParserDictionary();
        var hasContent = !string.IsNullOrWhiteSpace(currentJson);

        var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal };

        var editButton = new Button
        {
            Content = "📖 Editar sinónimos...",
            Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Cursor = Cursors.Hand
        };

        var statusText = new TextBlock
        {
            Text = hasContent ? " ✓ Configurado" : "",
            Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0xD4, 0x7E)),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 12
        };

        editButton.Click += (_, _) =>
        {
            var currentValue = GetParserDictionary?.Invoke() ?? string.Empty;
            // showVerbs = false: solo sustantivos, adjetivos y stopwords
            var editorWindow = new ParserDictionaryEditorWindow(currentValue, showVerbs: false)
            {
                Owner = Window.GetWindow(this)
            };

            if (editorWindow.ShowDialog() == true)
            {
                SetParserDictionary?.Invoke(editorWindow.ResultJson);

                // Update status
                var newHasContent = !string.IsNullOrWhiteSpace(editorWindow.ResultJson);
                statusText.Text = newHasContent ? " ✓ Configurado" : "";
            }
        };

        buttonPanel.Children.Add(editButton);
        buttonPanel.Children.Add(statusText);
        container.Children.Add(buttonPanel);

        return container;
    }

    /// <summary>
    /// Añade una sección de estadísticas de combate para NPCs.
    /// </summary>
    private void AddNpcStatsSection(object obj)
    {
        if (obj is not Npc npc)
            return;

        // Asegurar que Stats no sea null
        npc.Stats ??= new CombatStats();

        // Crear contenido del acordeón
        var contentPanel = new StackPanel();

        // Propiedades de estadísticas
        var statsProperties = new[]
        {
            ("Strength", "💪 Fuerza", 1, 100),
            ("Dexterity", "🏃 Destreza", 1, 100),
            ("Intelligence", "🧠 Inteligencia", 1, 100),
            ("MaxHealth", "❤️ Vida máxima", 1, 1000),
            ("CurrentHealth", "❤️ Vida actual", 0, 1000),
            ("Money", "💰 Dinero", 0, 100000)
        };

        foreach (var (propName, displayName, minVal, maxVal) in statsProperties)
        {
            AddNpcStatControlToPanel(npc, propName, displayName, minVal, maxVal, contentPanel);
        }

        // Crear acordeón
        var combatStatsDisplay = PropertyCategory.EstadisticasCombate.ToDisplayString();
        AddAccordionSection(combatStatsDisplay.ToUpper(), contentPanel, null, combatStatsDisplay);
    }

    /// <summary>
    /// Añade una sección de Sistemas para NPCs (Magia).
    /// </summary>
    private void AddNpcSystemsSection(object obj)
    {
        if (obj is not Npc npc)
            return;

        // Crear contenido del acordeón
        var contentPanel = new StackPanel();

        // Checkbox de Magia
        var magicCheckBox = new CheckBox
        {
            Content = "Puede usar habilidades mágicas",
            IsChecked = npc.MagicEnabled,
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            Margin = new Thickness(0, 0, 0, 0)
        };

        // Panel contenedor para las habilidades (se muestra bajo el checkbox)
        var npcAbilitiesPanel = CreateMultiSelectAbilityPicker(npc, "NpcAbilities");
        npcAbilitiesPanel.Margin = new Thickness(16, 8, 0, 0);
        npcAbilitiesPanel.Visibility = npc.MagicEnabled ? Visibility.Visible : Visibility.Collapsed;

        magicCheckBox.Checked += (_, _) =>
        {
            if (_currentObject is Npc targetNpc)
            {
                targetNpc.MagicEnabled = true;
                PropertyEdited?.Invoke(targetNpc, "MagicEnabled");
                npcAbilitiesPanel.Visibility = Visibility.Visible;
            }
        };
        magicCheckBox.Unchecked += (_, _) =>
        {
            if (_currentObject is Npc targetNpc)
            {
                targetNpc.MagicEnabled = false;
                PropertyEdited?.Invoke(targetNpc, "MagicEnabled");
                npcAbilitiesPanel.Visibility = Visibility.Collapsed;
            }
        };
        contentPanel.Children.Add(magicCheckBox);
        contentPanel.Children.Add(npcAbilitiesPanel);

        // Crear acordeón
        AddAccordionSection("🎮 SISTEMAS", contentPanel, null, "🎮 SISTEMAS");
    }

    /// <summary>
    /// Añade una sección de habilidades mágicas para PlayerDefinition.
    /// </summary>
    private void AddPlayerAbilitiesSection(object obj)
    {
        if (obj is not PlayerDefinition player)
            return;

        // Crear contenido del acordeón
        var contentPanel = new StackPanel();

        // Descripción
        var description = new TextBlock
        {
            Text = "Selecciona las habilidades mágicas que el jugador tendrá disponibles al iniciar la partida.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        contentPanel.Children.Add(description);

        // Selector de habilidades
        var abilitiesSelector = CreateMultiSelectAbilityPicker(player, "PlayerAbilities");
        contentPanel.Children.Add(abilitiesSelector);

        // Crear acordeón
        AddAccordionSection("✨ HABILIDADES MÁGICAS", contentPanel, null, "✨ HABILIDADES MÁGICAS");
    }

    /// <summary>
    /// Añade un control para una propiedad de estadística de NPC a un panel específico.
    /// </summary>
    private void AddNpcStatControlToPanel(Npc npc, string propertyName, string displayName, int minValue, int maxValue, Panel targetPanel)
    {
        var prop = typeof(CombatStats).GetProperty(propertyName);
        if (prop == null) return;

        // Label
        var label = new TextBlock
        {
            Text = displayName,
            Margin = new Thickness(0, 8, 0, 3),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))
        };
        targetPanel.Children.Add(label);

        // Panel con slider y valor
        var panel = new Grid
        {
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var currentValue = prop.GetValue(npc.Stats) is int v ? v : minValue;
        if (currentValue < minValue) currentValue = minValue;
        if (currentValue > maxValue) currentValue = maxValue;

        var slider = new Slider
        {
            Minimum = minValue,
            Maximum = maxValue,
            Value = currentValue,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = maxValue > 100 ? 10 : 5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(slider, 0);

        var valueLabel = new TextBlock
        {
            Text = currentValue.ToString(),
            Width = 50,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0xFF))
        };
        Grid.SetColumn(valueLabel, 1);

        slider.ValueChanged += (_, args) =>
        {
            try
            {
                if (_currentObject is not Npc targetNpc) return;
                targetNpc.Stats ??= new CombatStats();

                var newValue = (int)args.NewValue;
                if (newValue < minValue) newValue = minValue;
                if (newValue > maxValue) newValue = maxValue;

                prop.SetValue(targetNpc.Stats, newValue);
                valueLabel.Text = newValue.ToString();
                PropertyEdited?.Invoke(targetNpc, $"Stats.{propertyName}");
            }
            catch
            {
                // Ignorar errores
            }
        };

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);
        targetPanel.Children.Add(panel);
    }

    /// <summary>
    /// Evento para solicitar la edición de la ruta de patrulla de un NPC en el mapa.
    /// </summary>
    public event Action<Npc>? RequestEditPatrolRoute;

    private record PropertyGroup(string Name, List<PropertyInfo> Properties);

    private List<PropertyGroup> GroupProperties(object obj, List<PropertyInfo> props)
    {
        var groups = new Dictionary<PropertyCategory, List<PropertyInfo>>
        {
            [PropertyCategory.Identificacion] = new(),
            [PropertyCategory.Descripcion] = new(),
            [PropertyCategory.Objetos] = new(),
            [PropertyCategory.Sistemas] = new(),
            [PropertyCategory.Multimedia] = new(),
            [PropertyCategory.Comportamiento] = new(),
            [PropertyCategory.Combate] = new(),
            [PropertyCategory.Estadisticas] = new(),
            [PropertyCategory.Caracteristicas] = new(),
            [PropertyCategory.Seguridad] = new(),
            [PropertyCategory.Fabricacion] = new(),
            [PropertyCategory.Otros] = new()
        };

        foreach (var prop in props)
        {
            var category = GetPropertyCategory(obj, prop);
            if (groups.ContainsKey(category))
            {
                groups[category].Add(prop);
            }
            else
            {
                groups[PropertyCategory.Otros].Add(prop);
            }
        }

        // Ordenar propiedades dentro de cada grupo
        foreach (var group in groups.Values)
        {
            group.Sort((a, b) => GetPropertyOrder(a).CompareTo(GetPropertyOrder(b)));
        }

        // Retornar solo los grupos que tienen propiedades, en orden
        var orderedCategories = new[]
        {
            PropertyCategory.Identificacion,
            PropertyCategory.Multimedia,
            PropertyCategory.Descripcion,
            PropertyCategory.Objetos,
            PropertyCategory.Sistemas,
            PropertyCategory.Comportamiento,
            PropertyCategory.Combate,
            PropertyCategory.Fabricacion,
            PropertyCategory.Estadisticas,
            PropertyCategory.Caracteristicas,
            PropertyCategory.Seguridad,
            PropertyCategory.Otros
        };

        // Obtener configuración del juego para filtrar categorías
        var gameInfo = GetGameInfo?.Invoke();
        var craftingEnabled = gameInfo?.CraftingEnabled ?? false;
        var combatEnabled = gameInfo?.CombatEnabled ?? false;

        return orderedCategories
            .Where(cat => groups[cat].Any())
            .Where(cat => ShouldShowCategory(cat, obj, craftingEnabled, combatEnabled))
            .Select(cat => new PropertyGroup(cat.ToDisplayString(), groups[cat]))
            .ToList();
    }

    /// <summary>
    /// Determina si una categoría debe mostrarse según la configuración del juego.
    /// </summary>
    private static bool ShouldShowCategory(PropertyCategory category, object obj, bool craftingEnabled, bool combatEnabled)
    {
        // Fabricación solo visible en objetos si está habilitada
        if (category == PropertyCategory.Fabricacion && obj is GameObject && !craftingEnabled)
            return false;

        // Combate solo visible en objetos y NPCs si está habilitado
        if (category == PropertyCategory.Combate && (obj is GameObject || obj is Npc) && !combatEnabled)
            return false;

        return true;
    }

    private static PropertyCategory GetPropertyCategory(object obj, PropertyInfo prop)
    {
        var name = prop.Name;

        // QuestDefinition: todas las propiedades en Identificación
        if (obj is QuestDefinition)
            return PropertyCategory.Identificacion;

        // Identificación
        if (name is var n && n == PN.Id || n == PN.Name || n == PN.Title || n == PN.Theme || n == "ZoneName")
            return PropertyCategory.Identificacion;

        // Descripción
        if (name == PN.Description || name == PN.Dialogue)
            return PropertyCategory.Descripcion;

        // Sistemas (Combate, Necesidades básicas y Fabricación)
        if (name == PN.CombatEnabled || name == PN.MagicEnabled || name == PN.BasicNeedsEnabled
            || name == PN.HungerRate || name == PN.ThirstRate || name == PN.SleepRate
            || name == PN.HungerDeathText || name == PN.ThirstDeathText || name == PN.SleepDeathText
            || name == PN.HealthDeathText || name == PN.SanityDeathText
            || name == PN.CraftingEnabled)
            return PropertyCategory.Sistemas;

        // Multimedia
        if (name.Contains("Image") || name.Contains("Music"))
            return PropertyCategory.Multimedia;

        // Salas y zona (al final de Identificación)
        if (name == PN.RoomId || name == PN.RoomIdA || name == PN.RoomIdB
            || name == PN.StartRoomId || name == PN.TargetRoomId || name == PN.Direction
            || name == "Zone")
            return PropertyCategory.Identificacion;

        // NPC: Dinero en Identificación (después de Sala)
        if (obj is Npc && name == PN.Money)
            return PropertyCategory.Identificacion;

        // Comportamiento (incluyendo propiedades de contenedor de GameObject que irán con sangría)
        if (name == PN.Visible || name == PN.CanTake || name == PN.Type || name == PN.Gender
            || name == PN.IsPlural || name == PN.IsContainer || name == PN.IsOpenable || name == PN.IsOpen
            || name == PN.IsLocked || name == PN.ContentsVisible || name == PN.IsIlluminated
            || name == PN.IsInterior || name == PN.StartHour || name == PN.StartWeather || name == PN.MinutesPerGameHour
            || name == PN.RequiredQuests || name == PN.OpenFromSide || name == PN.IntroText || name == PN.EndingText
            || name == PN.IsLightSource || name == PN.IsLit || name == PN.LightTurnsRemaining
            || name == PN.CanExtinguish || name == PN.CanIgnite || name == PN.IgniterObjectId
            || name == PN.CanRead || name == PN.TextContent || name == PN.DefaultFontFamily)
            return PropertyCategory.Comportamiento;

        // Propiedades de llave, visibilidad y misiones de Door
        if (obj is Door && (name == PN.KeyObjectId || name == PN.Visible || name == PN.RequiredQuests))
            return PropertyCategory.Comportamiento;

        // Propiedades de comercio, patrulla, seguimiento y estado de NPC
        if (obj is Npc && (name == PN.IsShopkeeper || name == PN.ShopInventory
            || name == PN.BuyPriceMultiplier || name == PN.SellPriceMultiplier
            || name == PN.IsPatrolling || name == PN.PatrolMovementMode || name == PN.PatrolSpeed || name == PN.PatrolTimeInterval
            || name == PN.IsFollowingPlayer || name == PN.FollowMovementMode || name == PN.FollowSpeed || name == PN.FollowTimeInterval
            || name == PN.IsCorpse))
            return PropertyCategory.Comportamiento;

        // Fabricación (GameObject)
        if (obj is GameObject && name == PN.CraftingRecipe)
            return PropertyCategory.Fabricacion;

        // Propiedades de contenedor de GameObject (se mostrarán con sangría dentro de Comportamiento)
        if (obj is GameObject && (name == PN.ContainedObjectIds || name == PN.KeyId || name == PN.MaxCapacity))
            return PropertyCategory.Comportamiento;

        // Propiedades de combate de GameObject (armas y armaduras)
        if (obj is GameObject && (name == PN.AttackBonus || name == PN.HandsRequired || name == PN.DefenseBonus || name == PN.DamageType
            || name == PN.MaxDurability || name == PN.CurrentDurability || name == PN.InitiativeBonus))
            return PropertyCategory.Combate;

        // PlayerDefinition: Inventario y equipamiento inicial
        if (obj is PlayerDefinition && (name == PN.InitialInventory || name == PN.InitialRightHandId
            || name == PN.InitialLeftHandId || name == PN.InitialTorsoId || name == PN.InitialHeadId))
            return PropertyCategory.Objetos;

        // NPC: Inventario y equipamiento
        if (obj is Npc && (name == PN.Inventory || name == PN.EquippedRightHandId
            || name == PN.EquippedLeftHandId || name == PN.EquippedTorsoId || name == PN.EquippedHeadId))
            return PropertyCategory.Objetos;

        // Otras propiedades de contenido que no son de GameObject
        if (name == PN.Objectives || name == PN.KeyObjectId
            || name == PN.DoorId || name == PN.ObjectId)
            return PropertyCategory.Otros;

        // PlayerDefinition: propiedades físicas y económicas
        if (obj is PlayerDefinition && (name == PN.Age || name == PN.Weight || name == PN.Height || name == PN.InitialMoney
            || name == PN.MaxInventoryWeight || name == PN.MaxInventoryVolume))
            return PropertyCategory.Estadisticas;

        // PlayerDefinition: características
        if (obj is PlayerDefinition && (name == PN.Strength || name == PN.Constitution
            || name == PN.Intelligence || name == PN.Dexterity || name == PN.Charisma))
            return PropertyCategory.Caracteristicas;

        // Estadísticas
        if (name == PN.Strength || name == PN.Dexterity || name == PN.Intelligence
            || name == PN.MaxHealth || name == PN.CurrentHealth || name == PN.Money || name == PN.Stats
            || name == PN.Volume || name == PN.Weight || name == PN.Price)
            return PropertyCategory.Estadisticas;

        // Seguridad
        if (name == PN.EncryptionKey)
            return PropertyCategory.Seguridad;

        // Parser Dictionary al final de Otros
        if (name == PN.ParserDictionaryJson)
            return PropertyCategory.Otros;

        return PropertyCategory.Otros;
    }

    private static int GetPropertyOrder(PropertyInfo prop)
    {
        // Orden de prioridad para propiedades dentro de su grupo
        var name = prop.Name;
        if (name == PN.Id) return 0;
        if (name == PN.Name) return 1;
        if (name == PN.Theme) return 1;
        if (name == "Zone") return 2; // Zona después de Nombre
        if (name == PN.IsMainQuest) return 2;
        if (name == PN.Title) return 3;
        if (name == PN.Description) return 3;
        if (name == PN.Dialogue) return 5;
        if (name == PN.ImageId) return 10;
        if (name == PN.ImageBase64) return 11;
        if (name == PN.AsciiImage) return 12;
        if (name == PN.MusicId) return 13;
        if (name == PN.WorldMusicId) return 13;

        // QuestDefinition: orden específico (Id, Nombre, Misión principal, Descripción, Objetivos)
        if (name == PN.Objectives) return 4;

        // Propiedades de sala al final de Identificación
        if (name == PN.StartRoomId) return 100;
        if (name == PN.RoomId) return 101;
        if (name == PN.Money) return 102; // NPC: Dinero después de Sala
        if (name == PN.RoomIdA) return 103;
        if (name == PN.RoomIdB) return 104;
        if (name == PN.Direction) return 105;
        if (name == PN.TargetRoomId) return 106;

        // DefaultFontFamily primero en Comportamiento (solo para GameInfo)
        if (name == PN.DefaultFontFamily) return -1;

        // Texto de introducción y finalización al final de Comportamiento
        if (name == PN.IntroText) return 199;
        if (name == PN.EndingText) return 200;

        // Sistemas (Fabricación, Combate, Necesidades básicas)
        if (name == PN.CraftingEnabled) return 0;
        if (name == PN.CombatEnabled) return 1;
        if (name == PN.MagicEnabled) return 2;
        if (name == PN.HealthDeathText) return 3;
        if (name == PN.SanityDeathText) return 4;
        if (name == PN.BasicNeedsEnabled) return 10;
        if (name == PN.HungerRate) return 11;
        if (name == PN.ThirstRate) return 12;
        if (name == PN.SleepRate) return 13;
        if (name == PN.HungerDeathText) return 14;
        if (name == PN.ThirstDeathText) return 15;
        if (name == PN.SleepDeathText) return 16;

        // Orden para propiedades de equipamiento (categoría Objetos)
        if (name == PN.InitialRightHandId || name == PN.EquippedRightHandId) return 0;
        if (name == PN.InitialLeftHandId || name == PN.EquippedLeftHandId) return 1;
        if (name == PN.InitialTorsoId || name == PN.EquippedTorsoId) return 2;
        if (name == PN.InitialHeadId || name == PN.EquippedHeadId) return 3;
        if (name == PN.InitialInventory || name == PN.Inventory) return 10;

        // Parser Dictionary al final de Otros
        if (name == PN.ParserDictionaryJson) return 999;

        // Orden para propiedades de contenedor (GameObject) y NPC
        if (name == PN.Type) return 10;
        if (name == PN.CanTake) return 11;
        if (name == PN.Visible) return 12;
        if (name == PN.IsCorpse) return 13;
        if (name == PN.Gender) return 14;
        if (name == PN.IsPlural) return 15;
        if (name == PN.CanRead) return 16;
        if (name == PN.TextContent) return 17;
        if (name == PN.IsContainer) return 20;
        if (name == PN.ContentsVisible) return 21;
        if (name == PN.IsOpenable) return 22;
        if (name == PN.IsOpen) return 23;
        if (name == PN.IsLocked) return 24;
        if (name == PN.KeyId) return 25;
        if (name == PN.MaxCapacity) return 26;
        if (name == PN.ContainedObjectIds) return 27;

        // Orden para propiedades de iluminación (GameObject)
        if (name == PN.IsLightSource) return 50;
        if (name == PN.IsLit) return 51;
        if (name == PN.CanExtinguish) return 52;
        if (name == PN.CanIgnite) return 53;
        if (name == PN.IgniterObjectId) return 54;
        if (name == PN.LightTurnsRemaining) return 55;

        // Orden para propiedades de patrulla/seguimiento de NPC
        if (name == PN.IsPatrolling) return 30;
        if (name == PN.PatrolMovementMode) return 31;
        if (name == PN.PatrolSpeed) return 32;
        if (name == PN.PatrolTimeInterval) return 33;
        if (name == PN.IsFollowingPlayer) return 35;
        if (name == PN.FollowMovementMode) return 36;
        if (name == PN.FollowSpeed) return 37;
        if (name == PN.FollowTimeInterval) return 38;

        // Estadísticas de combate (NPC)
        if (name == PN.MaxHealth) return 0;
        if (name == PN.CurrentHealth) return 1;

        // Estadísticas de objetos
        if (name == PN.Volume) return 40;
        if (name == PN.Weight) return 41;
        if (name == PN.Price) return 42;
        if (name == PN.NutritionAmount) return 43;

        return 99;
    }

    private void AddPropertyControl(object obj, PropertyInfo prop)
    {
        // Determinar si necesita sangría
        bool needsIndent = ShouldIndentProperty(obj, prop);
        double leftMargin = needsIndent ? 20 : 0;

        // Crear contenedor para la propiedad (puede tener borde si es un grupo)
        Border? propertyContainer = null;
        StackPanel containerPanel;

        // Si es una propiedad agrupada (contenedor), crear un borde visual
        if (needsIndent)
        {
            propertyContainer = new Border
            {
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
                BorderThickness = new Thickness(2, 0, 0, 0),
                Padding = new Thickness(10, 0, 0, 0),
                Margin = new Thickness(8, 0, 0, 0)
            };

            containerPanel = new StackPanel();
            propertyContainer.Child = containerPanel;
        }
        else
        {
            containerPanel = new StackPanel
            {
                Margin = new Thickness(leftMargin, 0, 0, 0)
            };
        }

        // Booleans: label y checkbox en la misma fila, centrados verticalmente.
        if (prop.PropertyType == typeof(bool))
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 6, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var chk = new CheckBox
            {
                IsChecked = (bool?)prop.GetValue(obj),
                VerticalAlignment = VerticalAlignment.Center
            };

            chk.Checked += (_, _) =>
            {
                try
                {
                    if (_currentObject is not { } target) return;

                    // Si es NPC y se activa IsFollowingPlayer, verificar si tiene ruta de patrulla
                    if (target is Npc npc && prop.Name == PN.IsFollowingPlayer && npc.PatrolRoute.Count > 0)
                    {
                        var confirmWindow = new ConfirmWindow(
                            "Este NPC tiene una ruta de patrulla definida.\n¿Deseas eliminarla para activar el seguimiento?",
                            "Eliminar ruta de patrulla");

                        if (confirmWindow.ShowDialog() != true)
                        {
                            // Usuario canceló, desmarcar el checkbox
                            chk.IsChecked = false;
                            return;
                        }

                        // Limpiar la ruta de patrulla
                        npc.PatrolRoute.Clear();
                        npc.IsPatrolling = false;
                        PropertyEdited?.Invoke(target, "PatrolRoute");
                        PropertyEdited?.Invoke(target, "IsPatrolling");
                    }

                    prop.SetValue(target, true);

                    PropertyEdited?.Invoke(target, prop.Name);

                    // Actualizar visibilidad de propiedades dependientes
                    UpdatePropertyVisibility();
                }
                catch
                {
                    // Ignorar errores
                }
            };

            chk.Unchecked += (_, _) =>
            {
                try
                {
                    if (_currentObject is not { } target) return;

                    // Si es una puerta y se desmarca IsLocked (Cerradura), limpiar llave y preguntar si eliminarla
                    if (target is Door door && prop.Name == PN.IsLocked && !string.IsNullOrEmpty(door.KeyObjectId))
                    {
                        var keyId = door.KeyObjectId;
                        var keyObject = GetObjects?.Invoke().FirstOrDefault(o => o.Id == keyId);
                        var keyName = keyObject?.Name ?? keyId;

                        // Limpiar la asignación de llave
                        door.KeyObjectId = null;
                        PropertyEdited?.Invoke(target, "KeyObjectId");

                        // Preguntar si desea eliminar el objeto llave (ventana oscura)
                        var confirmWindow = new ConfirmWindow(
                            $"¿Deseas eliminar también el objeto llave \"{keyName}\"?",
                            "Eliminar llave");

                        if (confirmWindow.ShowDialog() == true)
                        {
                            RequestDeleteObject?.Invoke(keyId);
                        }
                    }

                    prop.SetValue(target, false);

                    PropertyEdited?.Invoke(target, prop.Name);

                    // Actualizar visibilidad de propiedades dependientes
                    UpdatePropertyVisibility();

                    // Si es una puerta, refrescar el PropertyEditor para mostrar el combo de llave en "(ninguna)"
                    if (target is Door && prop.Name == PN.IsLocked)
                    {
                        SetObject(target);
                    }

                    // Si se desmarca CombatEnabled, desmarcar también MagicEnabled
                    if (target is GameInfo gameInfo && prop.Name == PN.CombatEnabled && gameInfo.MagicEnabled)
                    {
                        gameInfo.MagicEnabled = false;
                        PropertyEdited?.Invoke(target, PN.MagicEnabled);
                        SetObject(target); // Refrescar para actualizar el checkbox de Magia
                    }
                }
                catch
                {
                    // Ignorar errores
                }
            };

            panel.Children.Add(chk);

            var boolLabel = new TextBlock
            {
                Text = GetDisplayLabel(prop),
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            boolLabel.MouseLeftButtonDown += (_, _) => chk.IsChecked = !chk.IsChecked;
            panel.Children.Add(boolLabel);

            containerPanel.Children.Add(panel);

            // Agregar el contenedor al panel raíz
            var elementToAdd = propertyContainer ?? (UIElement)containerPanel;
            RootPanel.Children.Add(elementToAdd);

            // Registrar la propiedad en su sección (para control de visibilidad de secciones)
            if (_currentBuildingSectionName != null)
                _propertyToSectionName[prop.Name] = _currentBuildingSectionName;

            // Registrar el elemento para control de visibilidad condicional
            var visibilityCondition = GetVisibilityCondition(obj, prop);
            if (visibilityCondition != null)
            {
                _propertyElements[prop.Name] = elementToAdd;
                _visibilityConditions[prop.Name] = visibilityCondition;

                // Aplicar visibilidad inicial
                elementToAdd.Visibility = visibilityCondition() ? Visibility.Visible : Visibility.Collapsed;
            }

            // Si es IsPatrolling de un NPC, añadir botón de editar ruta justo después
            if (obj is Npc npcForPatrolButton && prop.Name == PN.IsPatrolling)
            {
                var patrolButton = new Button
                {
                    Content = "📍 Editar ruta de patrulla...",
                    Padding = new Thickness(10, 6, 10, 6),
                    Margin = new Thickness(20, 4, 0, 0),
                    Background = new SolidColorBrush(Color.FromRgb(160, 100, 60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    FontWeight = FontWeights.Normal,
                    FontSize = 12
                };

                patrolButton.Click += (_, _) =>
                {
                    RequestEditPatrolRoute?.Invoke(npcForPatrolButton);
                };

                RootPanel.Children.Add(patrolButton);

                // Registrar visibilidad: solo visible si IsPatrolling está activado
                _propertyElements["_PatrolRouteButton"] = patrolButton;
                _visibilityConditions["_PatrolRouteButton"] = () => npcForPatrolButton.IsPatrolling && !npcForPatrolButton.IsFollowingPlayer;
                if (_currentBuildingSectionName != null)
                    _propertyToSectionName["_PatrolRouteButton"] = _currentBuildingSectionName;

                // Aplicar visibilidad inicial
                patrolButton.Visibility = npcForPatrolButton.IsPatrolling && !npcForPatrolButton.IsFollowingPlayer
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Si es MagicEnabled de GameInfo, añadir descripción y botón de habilidades
            if (obj is GameInfo gameInfoForMagic && prop.Name == PN.MagicEnabled)
            {
                var abilitiesPanel = new StackPanel
                {
                    Margin = new Thickness(20, 8, 0, 0)
                };

                var description = new TextBlock
                {
                    Text = "Las habilidades mágicas son ataques y defensas especiales que consumen maná.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    Margin = new Thickness(0, 0, 0, 6)
                };
                abilitiesPanel.Children.Add(description);

                var manageButton = new Button
                {
                    Content = "✨ Gestionar Habilidades...",
                    Padding = new Thickness(12, 6, 12, 6),
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
                    Cursor = Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                manageButton.Click += (_, _) =>
                {
                    RequestManageAbilities?.Invoke();
                };

                abilitiesPanel.Children.Add(manageButton);
                RootPanel.Children.Add(abilitiesPanel);

                // Registrar visibilidad: solo visible si MagicEnabled está activado
                _propertyElements["_AbilitiesPanel"] = abilitiesPanel;
                _visibilityConditions["_AbilitiesPanel"] = () => gameInfoForMagic.MagicEnabled;
                if (_currentBuildingSectionName != null)
                    _propertyToSectionName["_AbilitiesPanel"] = _currentBuildingSectionName;

                // Aplicar visibilidad inicial
                abilitiesPanel.Visibility = gameInfoForMagic.MagicEnabled
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return;
        }

        FrameworkElement editor;
        var label = new TextBlock
        {
            Text = GetDisplayLabel(prop),
            Margin = new Thickness(0, 10, 0, 3),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC))
        };

        if (obj is Room && prop.Name == PN.MusicId && prop.PropertyType == typeof(string))
        {
            var labelPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = label.Margin
            };
            label.Margin = new Thickness(0);
            labelPanel.Children.Add(label);

            var helpIcon = new TextBlock
            {
                Text = "?",
                Foreground = Brushes.Yellow,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(6, 0, 0, 0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Ayuda sobre música de sala"
            };
            helpIcon.MouseLeftButtonUp += (_, _) => ShowMusicIdHelp();
            labelPanel.Children.Add(helpIcon);

            containerPanel.Children.Add(labelPanel);
        }
        else if (obj is Room && prop.Name == PN.Description && prop.PropertyType == typeof(string))
        {
            // Description de Room: label + botón 🤖 para generar con IA
            var labelPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = label.Margin
            };
            label.Margin = new Thickness(0);
            labelPanel.Children.Add(label);

            var aiDescBtn = new Button
            {
                Content = "🤖",
                Width = 24,
                Height = 20,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Generar descripción con IA"
            };
            aiDescBtn.Click += (_, _) =>
            {
                try
                {
                    if (_currentObject is not Room room) return;
                    RequestAiDescriptionGeneration?.Invoke(room);
                }
                catch
                {
                    // Ignorar errores
                }
            };
            labelPanel.Children.Add(aiDescBtn);

            containerPanel.Children.Add(labelPanel);
        }
        else
        {
            containerPanel.Children.Add(label);
        }

        {
            // StartRoomId: selector de sala
            // Selectores de sala (StartRoomId, RoomId, RoomIdA, RoomIdB, TargetRoomId)
            if (prop.PropertyType == typeof(string) && GetRooms != null &&
                (prop.Name == PN.StartRoomId || prop.Name == PN.RoomId || prop.Name == PN.RoomIdA || prop.Name == PN.RoomIdB || prop.Name == PN.TargetRoomId))
            {
                var rooms = GetRooms().ToList();
                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    DisplayMemberPath = "Name",
                    SelectedValuePath = "Id",
                    ItemsSource = rooms
                };

                if (obj is Door && (prop.Name == PN.RoomIdA || prop.Name == PN.RoomIdB))
                {
                    combo.IsEnabled = false;
                }

                // Si es un GameObject con RoomId, verificar si está contenido en otro objeto
                if (obj is GameObject gameObj && prop.Name == PN.RoomId)
                {
                    if (IsObjectContainedInAnother(gameObj))
                    {
                        // Deshabilitar el campo porque está contenido en otro objeto
                        combo.IsEnabled = false;

                        // Sincronizar con la sala del contenedor
                        var container = FindContainerObject(gameObj);
                        if (container != null)
                        {
                            gameObj.RoomId = container.RoomId;
                        }
                    }
                }

                var currentId = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;
                combo.SelectedValue = currentId;

                combo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (combo.SelectedValue is string id)
                        {
                            prop.SetValue(target, id);
                            PropertyEdited?.Invoke(target, prop.Name);

                            // Si es un contenedor que cambió de sala, mover sus objetos contenidos
                            if (target is GameObject containerObj && prop.Name == PN.RoomId)
                            {
                                UpdateContainedObjectsRoom(containerObj, id);
                            }
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = combo;
            }
            else if (prop.PropertyType.IsEnum && prop.Name != "PatrolMovementMode" && prop.Name != "FollowMovementMode")
            {
                // Caso especial para OpenFromSide de Door: mostrar nombres de salas
                if (obj is Door door && prop.Name == PN.OpenFromSide && GetRooms != null)
                {
                    var rooms = GetRooms().ToList();
                    var roomA = rooms.FirstOrDefault(r => string.Equals(r.Id, door.RoomIdA, StringComparison.OrdinalIgnoreCase));
                    var roomB = rooms.FirstOrDefault(r => string.Equals(r.Id, door.RoomIdB, StringComparison.OrdinalIgnoreCase));

                    var openSideOptions = new List<OpenFromSideComboItem>
                    {
                        new OpenFromSideComboItem
                        {
                            Value = DoorOpenSide.Both,
                            DisplayName = "Ambas"
                        },
                        new OpenFromSideComboItem
                        {
                            Value = DoorOpenSide.FromAOnly,
                            DisplayName = roomA != null ? roomA.Name : "Sala A"
                        },
                        new OpenFromSideComboItem
                        {
                            Value = DoorOpenSide.FromBOnly,
                            DisplayName = roomB != null ? roomB.Name : "Sala B"
                        }
                    };

                    var comboOpenSide = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        DisplayMemberPath = nameof(OpenFromSideComboItem.DisplayName),
                        SelectedValuePath = nameof(OpenFromSideComboItem.Value),
                        ItemsSource = openSideOptions
                    };

                    var currentValue = (DoorOpenSide)(prop.GetValue(obj) ?? DoorOpenSide.Both);
                    comboOpenSide.SelectedValue = currentValue;

                    comboOpenSide.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (comboOpenSide.SelectedValue is DoorOpenSide value)
                            {
                                prop.SetValue(target, value);
                                PropertyEdited?.Invoke(target, prop.Name);
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    editor = comboOpenSide;
                }
                // Caso especial para GrammaticalGender de GameObject o Door: mostrar en español
                else if ((obj is GameObject || obj is Door) && prop.Name == PN.Gender && prop.PropertyType == typeof(GrammaticalGender))
                {
                    var genderOptions = new List<GenderComboItem>
                    {
                        new GenderComboItem { Value = GrammaticalGender.Masculine, DisplayName = "Masculino (el/un)" },
                        new GenderComboItem { Value = GrammaticalGender.Feminine, DisplayName = "Femenino (la/una)" }
                    };

                    var comboGender = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        DisplayMemberPath = nameof(GenderComboItem.DisplayName),
                        SelectedValuePath = nameof(GenderComboItem.Value),
                        ItemsSource = genderOptions
                    };

                    var currentGender = (GrammaticalGender)(prop.GetValue(obj) ?? GrammaticalGender.Masculine);
                    comboGender.SelectedValue = currentGender;

                    // Crear un panel que contenga el combo y el checkbox de "no sobrescribir con IA"
                    var genderPanel = new StackPanel();
                    genderPanel.Children.Add(comboGender);

                    // Checkbox para GenderAndPluralSetManually (solo visible si IA está activa)
                    var manualCheckPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 6, 0, 0),
                        Visibility = IsAiEnabled ? Visibility.Visible : Visibility.Collapsed
                    };

                    var manualCheck = new CheckBox
                    {
                        VerticalAlignment = VerticalAlignment.Center
                    };

                    // Obtener valor inicial
                    var isManual = obj is GameObject go ? go.GenderAndPluralSetManually
                        : obj is Door d && d.GenderAndPluralSetManually;
                    manualCheck.IsChecked = isManual;

                    // Handler del combo
                    comboGender.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (comboGender.SelectedValue is GrammaticalGender value)
                            {
                                prop.SetValue(target, value);
                                PropertyEdited?.Invoke(target, prop.Name);

                                // Al cambiar manualmente el género, activar "no sobrescribir con IA"
                                if (IsAiEnabled && manualCheck.IsChecked != true)
                                {
                                    manualCheck.IsChecked = true;
                                }
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    manualCheck.Checked += (_, _) =>
                    {
                        if (_currentObject is GameObject gameObj)
                            gameObj.GenderAndPluralSetManually = true;
                        else if (_currentObject is Door door)
                            door.GenderAndPluralSetManually = true;
                        PropertyEdited?.Invoke(_currentObject, "GenderAndPluralSetManually");
                    };

                    manualCheck.Unchecked += (_, _) =>
                    {
                        if (_currentObject is GameObject gameObj)
                            gameObj.GenderAndPluralSetManually = false;
                        else if (_currentObject is Door door)
                            door.GenderAndPluralSetManually = false;
                        PropertyEdited?.Invoke(_currentObject, "GenderAndPluralSetManually");
                    };

                    manualCheckPanel.Children.Add(manualCheck);
                    var manualCheckLabel = new TextBlock
                    {
                        Text = "No sobrescribir con IA",
                        Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(6, 0, 0, 0),
                        FontSize = 12,
                        Cursor = Cursors.Hand
                    };
                    manualCheckLabel.MouseLeftButtonDown += (_, _) => manualCheck.IsChecked = !manualCheck.IsChecked;
                    manualCheckPanel.Children.Add(manualCheckLabel);

                    genderPanel.Children.Add(manualCheckPanel);

                    // Registrar el panel del checkbox para control de visibilidad basado en IA
                    _propertyElements["GenderAndPluralSetManually"] = manualCheckPanel;
                    _visibilityConditions["GenderAndPluralSetManually"] = () => IsAiEnabled;
                    if (_currentBuildingSectionName != null)
                        _propertyToSectionName["GenderAndPluralSetManually"] = _currentBuildingSectionName;

                    editor = genderPanel;
                }
                // Caso especial para NeedRate: mostrar radio buttons en español
                else if (prop.PropertyType == typeof(NeedRate))
                {
                    var currentRate = (NeedRate)(prop.GetValue(obj) ?? NeedRate.Normal);

                    var radioPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Margin = new Thickness(0, 2, 0, 0)
                    };

                    var rbLow = new RadioButton
                    {
                        Content = "Lento",
                        GroupName = $"NeedRate_{prop.Name}_{obj.GetHashCode()}",
                        IsChecked = currentRate == NeedRate.Low,
                        Margin = new Thickness(0, 0, 12, 0),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White
                    };

                    var rbNormal = new RadioButton
                    {
                        Content = "Normal",
                        GroupName = $"NeedRate_{prop.Name}_{obj.GetHashCode()}",
                        IsChecked = currentRate == NeedRate.Normal,
                        Margin = new Thickness(0, 0, 12, 0),
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White
                    };

                    var rbHigh = new RadioButton
                    {
                        Content = "Rápido",
                        GroupName = $"NeedRate_{prop.Name}_{obj.GetHashCode()}",
                        IsChecked = currentRate == NeedRate.High,
                        VerticalContentAlignment = VerticalAlignment.Center,
                        Foreground = Brushes.White
                    };

                    rbLow.Checked += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            prop.SetValue(target, NeedRate.Low);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        catch { }
                    };

                    rbNormal.Checked += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            prop.SetValue(target, NeedRate.Normal);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        catch { }
                    };

                    rbHigh.Checked += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            prop.SetValue(target, NeedRate.High);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        catch { }
                    };

                    radioPanel.Children.Add(rbLow);
                    radioPanel.Children.Add(rbNormal);
                    radioPanel.Children.Add(rbHigh);

                    editor = radioPanel;
                }
                else
                {
                    // Enum normal
                    var comboEnum = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        ItemsSource = Enum.GetValues(prop.PropertyType)
                    };

                    comboEnum.SelectedItem = prop.GetValue(obj);

                    comboEnum.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (comboEnum.SelectedItem != null)
                            {
                                prop.SetValue(target, comboEnum.SelectedItem);
                                PropertyEdited?.Invoke(target, prop.Name);

                                // Si es ObjectType.Type, actualizar visibilidad de propiedades dependientes
                                if (prop.Name == PN.Type)
                                {
                                    UpdatePropertyVisibility();
                                }
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    editor = comboEnum;
                }
            }
            else if (obj is GameInfo && prop.Name == PN.MinutesPerGameHour && prop.PropertyType == typeof(int))
            {
                var comboMinutes = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEditable = false
                };

                for (int i = 1; i <= 10; i++)
                    comboMinutes.Items.Add(i);

                var current = prop.GetValue(obj) is int v ? v : 6;
                if (current < 1 || current > 10) current = 6;
                comboMinutes.SelectedItem = current;

                comboMinutes.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (comboMinutes.SelectedItem is int value)
                        {
                            prop.SetValue(target, value);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = comboMinutes;
            }
            // GameInfo: DefaultFontFamily (desplegable de fuentes del sistema)
            else if (obj is GameInfo && prop.Name == PN.DefaultFontFamily && prop.PropertyType == typeof(string))
            {
                var comboFont = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEditable = false,
                    MaxDropDownHeight = 300
                };

                var fonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
                comboFont.ItemsSource = fonts;
                comboFont.DisplayMemberPath = "Source";

                var currentFontName = prop.GetValue(obj) as string ?? "Segoe UI";
                var currentFont = fonts.FirstOrDefault(f => f.Source == currentFontName);
                if (currentFont != null)
                    comboFont.SelectedItem = currentFont;
                else
                    comboFont.SelectedIndex = 0;

                comboFont.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (comboFont.SelectedItem is FontFamily selectedFont)
                        {
                            prop.SetValue(target, selectedFont.Source);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = comboFont;
            }
            // PlayerDefinition: Age (10-90 de 1 en 1)
            else if (obj is PlayerDefinition && prop.Name == PN.Age && prop.PropertyType == typeof(int))
            {
                var comboAge = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEditable = false
                };

                for (int i = 10; i <= 90; i++)
                    comboAge.Items.Add(i);

                var currentAge = prop.GetValue(obj) is int a ? a : 25;
                if (currentAge < 10 || currentAge > 90) currentAge = 25;
                comboAge.SelectedItem = currentAge;

                comboAge.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (comboAge.SelectedItem is int value)
                        {
                            prop.SetValue(target, value);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = comboAge;
            }
            // PlayerDefinition: Weight (50-150 de 5 en 5)
            else if (obj is PlayerDefinition && prop.Name == PN.Weight && prop.PropertyType == typeof(int))
            {
                var comboWeight = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEditable = false
                };

                for (int i = 50; i <= 150; i += 5)
                    comboWeight.Items.Add(i);

                var currentWeight = prop.GetValue(obj) is int w ? w : 70;
                if (currentWeight < 50 || currentWeight > 150) currentWeight = 70;
                // Ajustar al múltiplo de 5 más cercano
                currentWeight = (currentWeight / 5) * 5;
                comboWeight.SelectedItem = currentWeight;

                comboWeight.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (comboWeight.SelectedItem is int value)
                        {
                            prop.SetValue(target, value);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = comboWeight;
            }
            // PlayerDefinition: Height (50-220 de 5 en 5)
            else if (obj is PlayerDefinition && prop.Name == PN.Height && prop.PropertyType == typeof(int))
            {
                var comboHeight = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    IsEditable = false
                };

                for (int i = 50; i <= 220; i += 5)
                    comboHeight.Items.Add(i);

                var currentHeight = prop.GetValue(obj) is int h ? h : 170;
                if (currentHeight < 50 || currentHeight > 220) currentHeight = 170;
                // Ajustar al múltiplo de 5 más cercano
                currentHeight = (currentHeight / 5) * 5;
                comboHeight.SelectedItem = currentHeight;

                comboHeight.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (comboHeight.SelectedItem is int value)
                        {
                            prop.SetValue(target, value);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = comboHeight;
            }
            // PlayerDefinition: InitialMoney (dinero inicial, mínimo 0, sin decimales)
            else if (obj is PlayerDefinition && prop.Name == PN.InitialMoney && prop.PropertyType == typeof(int))
            {
                var currentMoney = prop.GetValue(obj) is int g ? g : 0;
                if (currentMoney < 0) currentMoney = 0;

                var tbMoney = new TextBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    Text = currentMoney.ToString()
                };

                tbMoney.PreviewTextInput += (_, e) =>
                {
                    // Solo permitir dígitos
                    foreach (char c in e.Text)
                    {
                        if (!char.IsDigit(c))
                        {
                            e.Handled = true;
                            return;
                        }
                    }
                };

                tbMoney.LostFocus += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (int.TryParse(tbMoney.Text, out var value))
                        {
                            if (value < 0) value = 0;
                            prop.SetValue(target, value);
                            tbMoney.Text = value.ToString();
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        else
                        {
                            // Si no es válido, restaurar a 0
                            prop.SetValue(target, 0);
                            tbMoney.Text = "0";
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = tbMoney;
            }
            // PlayerDefinition: Características (Strength, Constitution, Intelligence, Dexterity, Charisma)
            else if (obj is PlayerDefinition playerDef &&
                     prop.Name is "Strength" or "Constitution" or "Intelligence" or "Dexterity" or "Charisma" &&
                     prop.PropertyType == typeof(int))
            {
                editor = CreateAttributeEditor(playerDef, prop);
            }
            // NPC: Velocidad de patrulla (1-3) con RadioButtons
            else if (obj is Npc npcForPatrolSpeed && prop.Name == PN.PatrolSpeed && prop.PropertyType == typeof(int))
            {
                var currentValue = prop.GetValue(npcForPatrolSpeed) is int v ? v : 1;
                if (currentValue < 1) currentValue = 1;
                if (currentValue > 3) currentValue = 3;

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
                var options = new[] { (1, "Camina (1)"), (2, "Lento (2)"), (3, "Muy lento (3)") };

                foreach (var (val, text) in options)
                {
                    var rb = new RadioButton
                    {
                        Content = text,
                        IsChecked = currentValue == val,
                        Tag = val,
                        Margin = new Thickness(0, 0, 12, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00))
                    };
                    rb.Checked += (_, _) =>
                    {
                        if (_currentObject is Npc target && rb.Tag is int newVal)
                        {
                            prop.SetValue(target, newVal);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    };
                    panel.Children.Add(rb);
                }
                editor = panel;
            }
            // NPC: Velocidad de seguimiento (1-3) con RadioButtons igual que patrulla
            else if (obj is Npc npcForFollowSpeed && prop.Name == PN.FollowSpeed && prop.PropertyType == typeof(int))
            {
                // Convertir valor porcentual antiguo a nuevo sistema 1-3
                var currentValue = prop.GetValue(npcForFollowSpeed) is int v ? v : 1;
                // Si viene del sistema antiguo (10-100), convertir a 1-3
                if (currentValue >= 10) currentValue = currentValue >= 80 ? 1 : (currentValue >= 40 ? 2 : 3);
                if (currentValue < 1) currentValue = 1;
                if (currentValue > 3) currentValue = 3;

                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
                var options = new[] { (1, "Camina (1)"), (2, "Lento (2)"), (3, "Muy lento (3)") };

                foreach (var (val, text) in options)
                {
                    var rb = new RadioButton
                    {
                        Content = text,
                        IsChecked = currentValue == val,
                        Tag = val,
                        Margin = new Thickness(0, 0, 12, 0),
                        Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00))
                    };
                    rb.Checked += (_, _) =>
                    {
                        if (_currentObject is Npc target && rb.Tag is int newVal)
                        {
                            prop.SetValue(target, newVal);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    };
                    panel.Children.Add(rb);
                }
                editor = panel;
            }
            // NPC: Modo de movimiento de patrulla (Turns/Time)
            else if (obj is Npc npcForPatrolMode && prop.Name == PN.PatrolMovementMode)
            {
                editor = CreateMovementModeComboBox(npcForPatrolMode, prop, isPatrol: true);
            }
            // NPC: Intervalo de tiempo de patrulla con RadioButtons
            else if (obj is Npc npcForPatrolTime && prop.Name == PN.PatrolTimeInterval && prop.PropertyType == typeof(float))
            {
                editor = CreateTimeIntervalRadioButtons(npcForPatrolTime, prop);
            }
            // NPC: Modo de movimiento de seguimiento (Turns/Time)
            else if (obj is Npc npcForFollowMode && prop.Name == PN.FollowMovementMode)
            {
                editor = CreateMovementModeComboBox(npcForFollowMode, prop, isPatrol: false);
            }
            // NPC: Intervalo de tiempo de seguimiento con RadioButtons
            else if (obj is Npc npcForFollowTime && prop.Name == PN.FollowTimeInterval && prop.PropertyType == typeof(float))
            {
                editor = CreateTimeIntervalRadioButtons(npcForFollowTime, prop);
            }
            else if (obj is GameInfo gameInfoForParser && prop.Name == PN.ParserDictionaryJson && prop.PropertyType == typeof(string))
            {
                var currentJson = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;
                var hasContent = !string.IsNullOrWhiteSpace(currentJson);

                var panel = new StackPanel { Orientation = Orientation.Horizontal };

                var editButton = new Button
                {
                    Content = "📖 Editar diccionario...",
                    Padding = new Thickness(12, 6, 12, 6),
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(0, 2, 0, 0)
                };

                var statusText = new TextBlock
                {
                    Text = hasContent ? " ✓ Configurado" : "",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x8C, 0xD4, 0x7E)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0),
                    FontSize = 12
                };

                editButton.Click += (_, _) =>
                {
                    var currentValue = Convert.ToString(prop.GetValue(gameInfoForParser)) ?? string.Empty;
                    var editorWindow = new ParserDictionaryEditorWindow(currentValue)
                    {
                        Owner = Window.GetWindow(this)
                    };

                    if (editorWindow.ShowDialog() == true)
                    {
                        prop.SetValue(gameInfoForParser, editorWindow.ResultJson);
                        PropertyEdited?.Invoke(gameInfoForParser, prop.Name);

                        // Update status
                        var newHasContent = !string.IsNullOrWhiteSpace(editorWindow.ResultJson);
                        statusText.Text = newHasContent ? " ✓ Configurado" : "";
                    }
                };

                panel.Children.Add(editButton);
                panel.Children.Add(statusText);

                editor = panel;
            }


            else if (prop.Name == PN.WorldMusicId && prop.PropertyType == typeof(string))
            {
                var valueObj = prop.GetValue(obj);
                var currentMusicId = Convert.ToString(valueObj) ?? string.Empty;

                var musics = GetMusics?.Invoke()?.ToList() ?? new List<MusicAsset>();

                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0)
                };

                // Añadir opción vacía para "sin música"
                var items = new List<MusicComboItem>
                {
                    new MusicComboItem { Id = string.Empty, DisplayName = "(Sin música)" }
                };

                // Añadir las músicas disponibles
                items.AddRange(musics.Select(m => new MusicComboItem { Id = m.Id, DisplayName = m.Id }));

                combo.ItemsSource = items;
                combo.DisplayMemberPath = nameof(MusicComboItem.DisplayName);
                combo.SelectedValuePath = nameof(MusicComboItem.Id);
                combo.SelectedValue = currentMusicId;

                // Si no encuentra el valor actual (puede ser una música antigua que ya no existe),
                // seleccionar la opción vacía
                if (combo.SelectedItem == null)
                {
                    combo.SelectedIndex = 0;
                }

                combo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not GameInfo game) return;
                        if (combo.SelectedValue is not string selectedId) return;

                        game.WorldMusicId = selectedId;
                        PropertyEdited?.Invoke(game, nameof(GameInfo.WorldMusicId));
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = combo;
            }


            else if (prop.Name == PN.EndingMusicId && prop.PropertyType == typeof(string))
            {
                var valueObj = prop.GetValue(obj);
                var currentMusicId = Convert.ToString(valueObj) ?? string.Empty;

                var musics = GetMusics?.Invoke()?.ToList() ?? new List<MusicAsset>();

                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    ItemsSource = new[] { "" }.Concat(musics.Select(m => m.Id)).ToList(),
                    SelectedValue = currentMusicId,
                    DisplayMemberPath = null
                };

                if (!string.IsNullOrEmpty(currentMusicId) && !musics.Any(m => m.Id == currentMusicId))
                {
                    combo.SelectedIndex = 0;
                }

                combo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not GameInfo game) return;
                        if (combo.SelectedValue is not string selectedId) return;

                        game.EndingMusicId = selectedId;
                        PropertyEdited?.Invoke(game, nameof(GameInfo.EndingMusicId));
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = combo;
            }


            // ImageId de Room: textbox + botón 🤖 (IA) + botón ... para imagen de sala
            else if (prop.Name == PN.ImageId && prop.PropertyType == typeof(string))
            {
                var valueObj = prop.GetValue(obj);
                string text = Convert.ToString(valueObj) ?? string.Empty;

                // Detectar si es una imagen generada por IA (ImageBase64 tiene contenido pero ImageId está vacío)
                bool isAiGenerated = false;
                if (obj is Room roomCheck)
                {
                    isAiGenerated = string.IsNullOrEmpty(roomCheck.ImageId) && !string.IsNullOrEmpty(roomCheck.ImageBase64);
                    if (isAiGenerated)
                    {
                        text = "[Generada por IA]";
                    }
                }

                var panel = new Grid
                {
                    Margin = new Thickness(0, 2, 0, 0)
                };
                panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Botón IA
                panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Botón ...

                var tb = new TextBox
                {
                    Text = text,
                    Margin = new Thickness(0, 0, 4, 0),
                    IsReadOnly = isAiGenerated,
                    Background = isAiGenerated ? new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)) : null,
                    Foreground = isAiGenerated ? new SolidColorBrush(Color.FromRgb(0x88, 0xCC, 0xFF)) : Brushes.White
                };
                Grid.SetColumn(tb, 0);

                // Botón para generar imagen con IA
                var aiBtn = new Button
                {
                    Content = "🤖",
                    Width = 28,
                    Margin = new Thickness(0, 0, 2, 0),
                    Padding = new Thickness(0, 0, 0, 0),
                    ToolTip = "Generar imagen con IA"
                };
                Grid.SetColumn(aiBtn, 1);

                var btn = new Button
                {
                    Content = "...",
                    Width = 28,
                    Padding = new Thickness(0, 0, 0, 0),
                    ToolTip = "Seleccionar imagen de archivo"
                };
                Grid.SetColumn(btn, 2);

                // Si el usuario edita manualmente el texto, solo cambiamos el nombre de archivo
                tb.LostFocus += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        prop.SetValue(target, tb.Text);
                        PropertyEdited?.Invoke(target, prop.Name);
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                // Botón IA: solicitar generación de imagen
                aiBtn.Click += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Room room) return;

                        // Validar que la sala tenga descripción
                        if (string.IsNullOrWhiteSpace(room.Description))
                        {
                            MessageBox.Show(
                                "La sala debe tener una descripción para generar la imagen con IA.",
                                "Descripción requerida",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }

                        // Disparar evento para que WorldEditorWindow genere la imagen
                        RequestAiImageGeneration?.Invoke(room);
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                // Botón ...: seleccionar imagen de archivo
                btn.Click += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Room room) return;

                        var dlg = new OpenFileDialog
                        {
                            Filter = "Imágenes (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg|Todos los archivos (*.*)|*.*",
                            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
                        };

                        if (dlg.ShowDialog() == true)
                        {
                            // Guardamos el nombre de archivo (con extensión) y la imagen en Base64 dentro del mundo
                            var fileName = Path.GetFileName(dlg.FileName);
                            var bytes = File.ReadAllBytes(dlg.FileName);
                            var base64 = Convert.ToBase64String(bytes);

                            tb.Text = fileName;
                            tb.IsReadOnly = false;
                            tb.Background = null;
                            tb.Foreground = Brushes.White;

                            room.ImageId = fileName;
                            room.ImageBase64 = base64;

                            PropertyEdited?.Invoke(room, nameof(Room.ImageId));
                            PropertyEdited?.Invoke(room, nameof(Room.ImageBase64));
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                panel.Children.Add(tb);
                panel.Children.Add(aiBtn);
                panel.Children.Add(btn);
                editor = panel;
            }

            // AsciiImage de Room: checkbox que genera ASCII desde ImageBase64
            else if (prop.Name == PN.AsciiImage && prop.PropertyType == typeof(string) && obj is Room roomCheck)
            {
                bool hasAsciiImage = !string.IsNullOrEmpty(roomCheck.AsciiImage);
                bool hasPngImage = !string.IsNullOrEmpty(roomCheck.ImageBase64);

                var panel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 6, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };

                var chk = new CheckBox
                {
                    IsChecked = hasAsciiImage,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsEnabled = hasPngImage || hasAsciiImage  // Solo habilitado si hay imagen PNG o ya tiene ASCII
                };

                var lbl = new TextBlock
                {
                    Text = !hasPngImage && !hasAsciiImage
                        ? "(requiere imagen PNG)"
                        : hasAsciiImage ? "(generada)" : "(pendiente)",
                    Foreground = hasAsciiImage
                        ? new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88))
                        : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    Margin = new Thickness(8, 0, 0, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    FontStyle = FontStyles.Italic
                };

                // Función para crear tooltip con estilo terminal
                ToolTip CreateAsciiTooltip(string asciiArt)
                {
                    var tooltipText = new TextBlock
                    {
                        Text = asciiArt,
                        FontFamily = new FontFamily("Consolas"),
                        FontSize = 6,
                        Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xFF, 0x00)),
                        Background = Brushes.Black
                    };

                    return new ToolTip
                    {
                        Content = tooltipText,
                        Background = Brushes.Black,
                        BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x88, 0x00)),
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(4),
                        MaxWidth = 1200
                    };
                }

                // Establecer tooltip inicial si hay imagen ASCII
                if (hasAsciiImage)
                {
                    var tooltip = CreateAsciiTooltip(roomCheck.AsciiImage!);
                    chk.ToolTip = tooltip;
                    lbl.ToolTip = tooltip;
                }

                chk.Checked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is Room targetRoom && !string.IsNullOrEmpty(targetRoom.ImageBase64))
                        {
                            // Generar ASCII desde la imagen PNG
                            var asciiArt = AsciiConverter.ConvertFromBase64(targetRoom.ImageBase64, 160);
                            targetRoom.AsciiImage = asciiArt;
                            lbl.Text = "(generada)";
                            lbl.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0xFF, 0x88));

                            // Actualizar tooltip
                            var tooltip = CreateAsciiTooltip(asciiArt);
                            chk.ToolTip = tooltip;
                            lbl.ToolTip = tooltip;

                            PropertyEdited?.Invoke(targetRoom, PN.AsciiImage);
                        }
                    }
                    catch (Exception ex)
                    {
                        DarkErrorDialog.Show("Error al generar ASCII",
                            $"No se pudo convertir la imagen a ASCII:\n{ex.Message}",
                            Window.GetWindow(this));
                        chk.IsChecked = false;
                    }
                };

                chk.Unchecked += (_, _) =>
                {
                    if (_currentObject is Room targetRoom)
                    {
                        targetRoom.AsciiImage = null;
                        lbl.Text = !string.IsNullOrEmpty(targetRoom.ImageBase64) ? "(pendiente)" : "(requiere imagen PNG)";
                        lbl.Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));

                        // Quitar tooltip
                        chk.ToolTip = null;
                        lbl.ToolTip = null;

                        PropertyEdited?.Invoke(targetRoom, PN.AsciiImage);
                    }
                };

                panel.Children.Add(chk);
                panel.Children.Add(lbl);
                editor = panel;
            }


            // MusicId de Room: ComboBox con las músicas disponibles
            else if (prop.Name == PN.MusicId && prop.PropertyType == typeof(string))
            {
                var valueObj = prop.GetValue(obj);
                var currentMusicId = Convert.ToString(valueObj) ?? string.Empty;

                var musics = GetMusics?.Invoke()?.ToList() ?? new List<MusicAsset>();

                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0)
                };

                // Añadir opción vacía para "sin música"
                var items = new List<MusicComboItem>
                {
                    new MusicComboItem { Id = string.Empty, DisplayName = "(Sin música)" }
                };

                // Añadir las músicas disponibles
                items.AddRange(musics.Select(m => new MusicComboItem { Id = m.Id, DisplayName = m.Id }));

                combo.ItemsSource = items;
                combo.DisplayMemberPath = nameof(MusicComboItem.DisplayName);
                combo.SelectedValuePath = nameof(MusicComboItem.Id);
                combo.SelectedValue = currentMusicId;

                // Si no encuentra el valor actual (puede ser una música antigua que ya no existe),
                // seleccionar la opción vacía
                if (combo.SelectedItem == null)
                {
                    combo.SelectedIndex = 0;
                }

                combo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Room room) return;
                        if (combo.SelectedValue is not string selectedId) return;

                        room.MusicId = selectedId;
                        PropertyEdited?.Invoke(room, nameof(Room.MusicId));
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = combo;
            }
            // Zone de Room: ComboBox con las zonas disponibles
            else if (obj is Room && prop.Name == "Zone" && prop.PropertyType == typeof(string))
            {
                var valueObj = prop.GetValue(obj);
                var currentZone = Convert.ToString(valueObj) ?? string.Empty;

                var zones = GetZones?.Invoke()?.ToList() ?? new List<string>();

                // Si no hay zonas, añadir "Inicial" como zona por defecto
                if (zones.Count == 0)
                {
                    zones.Add("Inicial");
                }

                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0)
                };

                // Aplicar estilos oscuros desde recursos
                if (Application.Current.TryFindResource("DarkComboBoxStyle") is Style comboStyle)
                {
                    combo.Style = comboStyle;
                }
                if (Application.Current.TryFindResource("DarkComboBoxItemStyle") is Style itemStyle)
                {
                    combo.ItemContainerStyle = itemStyle;
                }

                // Añadir las zonas disponibles
                foreach (var zone in zones)
                {
                    combo.Items.Add(zone);
                }

                combo.SelectedItem = currentZone;

                // Si no encuentra el valor actual, seleccionar el primero
                if (combo.SelectedItem == null && combo.Items.Count > 0)
                {
                    combo.SelectedIndex = 0;
                    // Asignar la zona al objeto inmediatamente
                    if (obj is Room roomObj)
                    {
                        roomObj.Zone = combo.Items[0] as string;
                    }
                }

                combo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Room room) return;
                        var selectedZone = combo.SelectedItem as string;
                        if (selectedZone == null) return;

                        room.Zone = selectedZone;
                        PropertyEdited?.Invoke(room, nameof(Room.Zone));
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = combo;
            }
            else if (prop.PropertyType == typeof(string) &&
                     obj is GameInfo &&
                     string.Equals(prop.Name, "EncryptionKey", StringComparison.OrdinalIgnoreCase))
            {
                // Clave de cifrado: se muestra como password
                var valueObj = prop.GetValue(obj);
                var text = Convert.ToString(valueObj) ?? string.Empty;

                var pb = new PasswordBox
                {
                    Password = text,
                    Margin = new Thickness(0, 2, 0, 0)
                };
                _encryptionPasswordBox = pb;

                pb.LostFocus += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;

                        var trimmed = (pb.Password ?? string.Empty).Trim();

                        // Pedir confirmación si la contraseña ha cambiado y es válida
                        if (!string.IsNullOrEmpty(trimmed) && trimmed.Length == 8)
                        {
                            var currentVal = prop.GetValue(target) as string ?? string.Empty;
                            if (trimmed != currentVal)
                            {
                                // Usamos InputWindow para pedir confirmación
                                // Nota: InputWindow es un cuadro de texto normal, no password, 
                                // pero servirá para que el usuario escriba la clave otra vez y verifique.
                                var confirmDlg = new XiloAdventures.Wpf.Common.Windows.InputWindow(
                                    "Por seguridad, confirme la nueva clave de cifrado:",
                                    "Confirmar clave");

                                if (confirmDlg.ShowDialog() != true || confirmDlg.InputText != trimmed)
                                {
                                    new AlertWindow("La confirmación de la clave no coincide.", "Error")
                                    {
                                        Owner = Window.GetWindow(this)
                                    }.ShowDialog();

                                    // Restaurar valor anterior en el UI
                                    pb.Password = currentVal;
                                    return;
                                }
                            }
                        }

                        prop.SetValue(target, trimmed);
                        PropertyEdited?.Invoke(target, prop.Name);
                    }
                    catch
                    {
                        // Ignorar errores de conversion
                    }
                };

                editor = pb;
            }
            // Editor de inventario con cantidades para Inventory de NPC o InitialInventory de PlayerDefinition
            else if ((obj is Npc || obj is PlayerDefinition) &&
                     prop.PropertyType == typeof(List<InventoryItem>) &&
                     (prop.Name == PN.Inventory || prop.Name == PN.InitialInventory) &&
                     GetObjects != null)
            {
                editor = CreateInventoryEditor(obj, prop);
            }
            // Editor de inventario de tienda con cantidades para ShopInventory de NPC
            else if (obj is Npc npcForShop &&
                     prop.Name == PN.ShopInventory &&
                     prop.PropertyType == typeof(List<ShopItem>) &&
                     GetObjects != null)
            {
                editor = CreateShopInventoryEditor(npcForShop, prop);
            }
            // Editor de receta de fabricación para GameObject
            else if (obj is GameObject gameObjForCrafting &&
                     prop.Name == PN.CraftingRecipe &&
                     prop.PropertyType == typeof(List<CraftingIngredient>) &&
                     GetObjects != null)
            {
                editor = CreateCraftingRecipeEditor(gameObjForCrafting, prop);
            }
            // Editor de requisitos de misiones para Room y Exit
            else if (prop.Name == PN.RequiredQuests &&
                     prop.PropertyType == typeof(List<QuestRequirement>) &&
                     GetQuests != null)
            {
                editor = CreateRequiredQuestsEditor(obj, prop);
            }
            else
            {
                // Texto normal / listas
                var valueObj = prop.GetValue(obj);
                string text;
                if (prop.PropertyType == typeof(List<string>) && valueObj is List<string> list)
                {
                    text = string.Join(", ", list);
                }
                else
                {
                    text = Convert.ToString(valueObj) ?? string.Empty;
                }

                bool isMultilineDescription =
                    prop.PropertyType == typeof(string) &&
                    string.Equals(prop.Name, "Description", StringComparison.OrdinalIgnoreCase) &&
                    (obj is Room || obj is GameObject || obj is Npc || obj is QuestDefinition);

                // TextContent: multilinea con 6 líneas de altura
                bool isTextContent =
                    prop.PropertyType == typeof(string) &&
                    string.Equals(prop.Name, "TextContent", StringComparison.OrdinalIgnoreCase) &&
                    obj is GameObject;

                // Objetivos de misión también multilínea
                bool isQuestObjectives =
                    prop.PropertyType == typeof(List<string>) &&
                    string.Equals(prop.Name, "Objectives", StringComparison.OrdinalIgnoreCase) &&
                    obj is QuestDefinition;

                // IntroText y EndingText del nodo Juego también multilínea
                bool isGameInfoText =
                    prop.PropertyType == typeof(string) &&
                    (string.Equals(prop.Name, "IntroText", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prop.Name, "EndingText", StringComparison.OrdinalIgnoreCase)) &&
                    obj is GameInfo;

                bool isLargeMultiline = isMultilineDescription || isQuestObjectives || isTextContent || isGameInfoText;

                // Si es una propiedad de llave (KeyObjectId o KeyId), crear un ComboBox con objetos de tipo Llave
                bool isKeyProperty =
                    prop.PropertyType == typeof(string) &&
                    (string.Equals(prop.Name, "KeyObjectId", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(prop.Name, "KeyId", StringComparison.OrdinalIgnoreCase));

                // Si es una propiedad de objeto encendedor (IgniterObjectId), crear un ComboBox con todos los objetos
                bool isIgniterProperty =
                    prop.PropertyType == typeof(string) &&
                    string.Equals(prop.Name, "IgniterObjectId", StringComparison.OrdinalIgnoreCase);

                if (isIgniterProperty && GetObjects != null)
                {
                    var allObjects = GetObjects().ToList();

                    // Crear lista de opciones con "Sin objeto" al principio
                    var igniterOptions = new List<KeyComboItem> { new KeyComboItem { Id = "", DisplayName = "(Sin objeto)" } };
                    igniterOptions.AddRange(allObjects.Select(o => new KeyComboItem { Id = o.Id, DisplayName = o.Name }));

                    var combo = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        DisplayMemberPath = nameof(KeyComboItem.DisplayName),
                        SelectedValuePath = nameof(KeyComboItem.Id),
                        ItemsSource = igniterOptions
                    };

                    var currentIgniterId = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;
                    combo.SelectedValue = currentIgniterId;

                    combo.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (combo.SelectedValue is string igniterId)
                            {
                                // Si es cadena vacía, guardar como null
                                prop.SetValue(target, string.IsNullOrEmpty(igniterId) ? null : igniterId);
                                PropertyEdited?.Invoke(target, prop.Name);
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    editor = combo;
                }
                else if (isKeyProperty && GetObjects != null)
                {
                    var keyObjects = GetObjects().Where(o => o.Type == ObjectType.Llave).ToList();

                    // Crear lista de opciones con "(ninguna)" al principio
                    var keyOptions = new List<KeyComboItem> { new KeyComboItem { Id = "", DisplayName = "(ninguna)" } };
                    keyOptions.AddRange(keyObjects.Select(k => new KeyComboItem { Id = k.Id, DisplayName = k.Name }));

                    var combo = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        DisplayMemberPath = nameof(KeyComboItem.DisplayName),
                        SelectedValuePath = nameof(KeyComboItem.Id),
                        ItemsSource = keyOptions
                    };

                    var currentKeyId = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;
                    combo.SelectedValue = currentKeyId;

                    combo.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (combo.SelectedValue is string keyId)
                            {
                                // Si es cadena vacía, guardar como null
                                prop.SetValue(target, string.IsNullOrEmpty(keyId) ? null : keyId);
                                PropertyEdited?.Invoke(target, prop.Name);

                                // Si es una puerta y se selecciona una llave, activar IsLocked automáticamente
                                if (target is Door door && !string.IsNullOrEmpty(keyId) && !door.IsLocked)
                                {
                                    door.IsLocked = true;
                                    PropertyEdited?.Invoke(target, "IsLocked");
                                    // Refrescar el PropertyEditor para mostrar el cambio
                                    SetObject(target);
                                }
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    editor = combo;
                }
                // Selector de puerta para Exit.DoorId
                else if (obj is Exit && prop.Name == PN.DoorId && GetDoors != null)
                {
                    var doors = GetDoors().ToList();

                    // Crear lista de opciones con "(ninguna)" al principio
                    var doorOptions = new List<DoorComboItem> { new DoorComboItem { Id = "", DisplayName = "(ninguna)" } };
                    doorOptions.AddRange(doors.Select(d => new DoorComboItem { Id = d.Id, DisplayName = d.Name ?? d.Id }));

                    var combo = new ComboBox
                    {
                        Margin = new Thickness(0, 2, 0, 0),
                        DisplayMemberPath = nameof(DoorComboItem.DisplayName),
                        SelectedValuePath = nameof(DoorComboItem.Id),
                        ItemsSource = doorOptions
                    };

                    var currentDoorId = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;
                    combo.SelectedValue = currentDoorId;

                    combo.SelectionChanged += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;
                            if (combo.SelectedValue is string doorId)
                            {
                                // Si es cadena vacía, guardar como null
                                prop.SetValue(target, string.IsNullOrEmpty(doorId) ? null : doorId);
                                PropertyEdited?.Invoke(target, prop.Name);
                            }
                        }
                        catch
                        {
                            // Ignorar errores
                        }
                    };

                    editor = combo;
                }
                // Selector de equipamiento para mano derecha (Arma o Armadura)
                else if ((obj is PlayerDefinition || obj is Npc) &&
                         prop.PropertyType == typeof(string) &&
                         (prop.Name == PN.InitialRightHandId || prop.Name == PN.EquippedRightHandId) &&
                         GetObjects != null)
                {
                    editor = CreateEquipmentSlotEditor(obj, prop, EquipmentSlot.RightHand);
                }
                // Selector de equipamiento para mano izquierda (Arma 1 mano o Armadura)
                else if ((obj is PlayerDefinition || obj is Npc) &&
                         prop.PropertyType == typeof(string) &&
                         (prop.Name == PN.InitialLeftHandId || prop.Name == PN.EquippedLeftHandId) &&
                         GetObjects != null)
                {
                    editor = CreateEquipmentSlotEditor(obj, prop, EquipmentSlot.LeftHand);
                }
                // Selector de equipamiento para torso (solo Armadura)
                else if ((obj is PlayerDefinition || obj is Npc) &&
                         prop.PropertyType == typeof(string) &&
                         (prop.Name == PN.InitialTorsoId || prop.Name == PN.EquippedTorsoId) &&
                         GetObjects != null)
                {
                    editor = CreateEquipmentSlotEditor(obj, prop, EquipmentSlot.Torso);
                }
                // Selector de equipamiento para cabeza (solo Casco)
                else if ((obj is PlayerDefinition || obj is Npc) &&
                         prop.PropertyType == typeof(string) &&
                         (prop.Name == PN.InitialHeadId || prop.Name == PN.EquippedHeadId) &&
                         GetObjects != null)
                {
                    editor = CreateEquipmentSlotEditor(obj, prop, EquipmentSlot.Head);
                }
                else
                {
                    // TextContent usa 120px (6 líneas), Description usa 160px
                    var multilineHeight = isTextContent ? 120 : (isLargeMultiline ? 160 : 0);
                    var tb = new TextBox
                    {
                        Text = text,
                        Margin = new Thickness(0, 2, 0, 0),
                        AcceptsReturn = isLargeMultiline,
                        TextWrapping = isLargeMultiline ? TextWrapping.Wrap : TextWrapping.NoWrap,
                        VerticalScrollBarVisibility = isLargeMultiline ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden,
                        VerticalContentAlignment = isLargeMultiline ? VerticalAlignment.Top : VerticalAlignment.Center,
                        MinHeight = multilineHeight
                    };
                    var originalText = text;
                    tb.LostFocus += (_, _) =>
                    {
                        try
                        {
                            if (_currentObject is not { } target) return;

                            object? value = tb.Text;
                            if (prop.PropertyType == typeof(string) &&
                                obj is GameInfo &&
                                string.Equals(prop.Name, "EncryptionKey", StringComparison.OrdinalIgnoreCase))
                            {
                                var trimmed = (tb.Text ?? string.Empty).Trim();
                                if (!string.IsNullOrEmpty(trimmed))
                                {
                                    var length = Encoding.UTF8.GetByteCount(trimmed);
                                    if (length != 8 && length != 32)
                                    {
                                        new AlertWindow("La clave de cifrado debe ser de 8 caracteres", "Clave inválida")
                                        {
                                            Owner = Window.GetWindow(this)
                                        }.ShowDialog();
                                        tb.Text = originalText;
                                        return;
                                    }
                                }

                                value = trimmed;
                            }
                            if (prop.PropertyType == typeof(int))
                            {
                                if (int.TryParse(tb.Text, out var i)) value = i;
                            }
                            else if (prop.PropertyType == typeof(double))
                            {
                                if (double.TryParse(tb.Text, out var d)) value = d;
                            }
                            else if (prop.PropertyType == typeof(List<string>))
                            {
                                var parts = (tb.Text ?? string.Empty).Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                                var listVal = new List<string>();
                                foreach (var p in parts)
                                {
                                    var s = p.Trim();
                                    if (!string.IsNullOrEmpty(s))
                                        listVal.Add(s);
                                }
                                value = listVal;
                            }

                            prop.SetValue(target, value);
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        catch
                        {
                            // Ignorar errores de conversion
                        }
                    };
                    // Para propiedades de texto (como Name), actualizamos en vivo al teclear
                    if (prop.PropertyType == typeof(string))
                    {
                        tb.TextChanged += (_, _) =>
                        {
                            try
                            {
                                if (_currentObject is not { } target) return;
                                prop.SetValue(target, tb.Text);
                                PropertyEdited?.Invoke(target, prop.Name);
                            }
                            catch
                            {
                                // Ignorar errores
                            }
                        };
                    }

                    editor = tb;
                }
            }

            containerPanel.Children.Add(editor);

            // Agregar el contenedor al panel raíz
            var finalElement = propertyContainer ?? (UIElement)containerPanel;
            RootPanel.Children.Add(finalElement);

            // Registrar la propiedad en su sección (para control de visibilidad de secciones)
            if (_currentBuildingSectionName != null)
                _propertyToSectionName[prop.Name] = _currentBuildingSectionName;

            // Registrar el elemento para control de visibilidad condicional
            var condition = GetVisibilityCondition(obj, prop);
            if (condition != null)
            {
                _propertyElements[prop.Name] = finalElement;
                _visibilityConditions[prop.Name] = condition;

                // Aplicar visibilidad inicial
                finalElement.Visibility = condition() ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private static readonly Dictionary<string, string> DisplayNameMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Genericos
        ["Id"] = "Id",
        ["Name"] = "Nombre",
        ["Description"] = "Descripción",
        ["Title"] = "Título",
        ["Theme"] = "Tema/Ambientación",
        ["MusicId"] = "Música",
        ["WorldMusicId"] = "Música global",
        ["EncryptionKey"] = "Clave de cifrado de las partidas",
        ["ImageBase64"] = "Imagen (Base64)",
        ["ImageId"] = "Imagen (id)",
        ["AsciiImage"] = "Imagen ASCII (Linux)",
        ["RoomId"] = "Sala",
        ["RoomIdA"] = "Sala A",
        ["RoomIdB"] = "Sala B",
        ["TargetRoomId"] = "Sala destino",
        ["Direction"] = "Dirección",
        ["IsIlluminated"] = "Iluminada",
        ["IsInterior"] = "Interior",
        ["KeyId"] = "Llave (ID objeto)",
        ["KeyObjectId"] = "Llave (ID objeto)",
        ["ObjectId"] = "Objeto",
        ["DoorId"] = "Puerta",
        ["StartHour"] = "Hora inicial",
        ["StartWeather"] = "Clima inicial",
        ["RequiredQuests"] = "Requisitos de misión",
        ["Visible"] = "Visible",
        ["CanTake"] = "Se puede coger",
        ["CanRead"] = "Se puede leer",
        ["Type"] = "Tipo de objeto",
        ["TextContent"] = "Texto",
        ["Gender"] = "Género gramatical",
        ["IsPlural"] = "Es plural",
        ["IsContainer"] = "Es contenedor",
        ["IsOpenable"] = "Se puede abrir/cerrar",
        ["IsOpen"] = "Está abierto",
        ["IsLocked"] = "Está bloqueado",
        ["OpenFromSide"] = "Cerradura desde",
        ["ContentsVisible"] = "Contenido visible",
        ["MaxCapacity"] = "📦 Capacidad máxima (cm³)",
        ["IsLightSource"] = "Es luminoso",
        ["IsLit"] = "Está encendido",
        ["LightTurnsRemaining"] = "Turnos de luz (-1 = infinito)",
        ["CanExtinguish"] = "Se puede apagar",
        ["CanIgnite"] = "Se puede encender",
        ["IgniterObjectId"] = "Objeto encendedor",
        ["Volume"] = "📐 Volumen (cm³)",
        ["Weight"] = "⚖️ Peso (g)",
        ["Price"] = "💰 Precio",
        ["NutritionAmount"] = "🍽️ Cantidad (nutrición)",
        ["ContainedObjectIds"] = "📦 Objetos contenidos",
        ["InventoryObjectIds"] = "Objetos en inventario",
        ["Dialogue"] = "Diálogo",
        ["Stats"] = "Estadísticas",
        ["Strength"] = "💪 Fuerza",
        ["Dexterity"] = "🏃 Destreza",
        ["Intelligence"] = "🧠 Inteligencia",
        ["MaxHealth"] = "❤️ Salud máxima",
        ["CurrentHealth"] = "❤️ Salud actual",
        ["Money"] = "💰 Dinero",
        ["Objectives"] = "Objetivos",

        // Juego
        ["GameInfo.Title"] = "Título",
        ["GameInfo.Theme"] = "Temática",
        ["GameInfo.DefaultFontFamily"] = "Fuente",
        ["GameInfo.StartRoomId"] = "Sala inicial",
        ["GameInfo.MinutesPerGameHour"] = "Minutos por hora de juego",
        ["GameInfo.ParserDictionaryJson"] = "Diccionario del parser",
        ["GameInfo.StartHour"] = "Hora inicial",
        ["GameInfo.StartWeather"] = "Clima inicial",
        ["GameInfo.WorldMusicId"] = "Música global",
        ["GameInfo.EncryptionKey"] = "Clave de cifrado de las partidas",
        ["GameInfo.IntroText"] = "Texto de introducción",
        ["GameInfo.EndingText"] = "Texto de finalización",
        ["GameInfo.EndingMusicId"] = "Música de finalización",
        ["GameInfo.TestModeAiEnabled"] = "IA en modo pruebas",
        ["GameInfo.TestModeSoundEnabled"] = "Sonido en modo pruebas",
        ["GameInfo.CraftingEnabled"] = "Fabricación",
        ["GameInfo.CombatEnabled"] = "Combate",
        ["GameInfo.MagicEnabled"] = "Magia",
        ["GameInfo.BasicNeedsEnabled"] = "Necesidades básicas",
        ["GameInfo.HungerRate"] = "🍖 Velocidad de hambre",
        ["GameInfo.ThirstRate"] = "💧 Velocidad de sed",
        ["GameInfo.SleepRate"] = "😴 Velocidad de sueño",
        ["GameInfo.HungerDeathText"] = "🍖 Texto de muerte por hambre",
        ["GameInfo.ThirstDeathText"] = "💧 Texto de muerte por sed",
        ["GameInfo.SleepDeathText"] = "😴 Texto de muerte por agotamiento",
        ["GameInfo.HealthDeathText"] = "❤️ Texto de muerte por heridas",
        ["GameInfo.SanityDeathText"] = "🧠 Texto de muerte por locura",

        // Zona (nodo de zona en el árbol)
        ["ZoneName"] = "Nombre de la zona",

        // Sala
        ["Room.Name"] = "Nombre",
        ["Room.Zone"] = "Zona",
        ["Room.Description"] = "Descripción",
        ["Room.ImageBase64"] = "Imagen (Base64)",
        ["Room.ImageId"] = "Imagen (id)",
        ["Room.AsciiImage"] = "Imagen ASCII (Linux)",
        ["Room.MusicId"] = "Música",
        ["Room.RequiredQuests"] = "Requisitos de misión",

        // Objeto
        ["GameObject.RoomId"] = "Sala",
        ["GameObject.CanTake"] = "Se puede coger",
        ["GameObject.Type"] = "Tipo",
        ["GameObject.IsContainer"] = "Es contenedor",
        ["GameObject.IsOpenable"] = "Se puede abrir/cerrar",
        ["GameObject.IsOpen"] = "Está abierto",
        ["GameObject.IsLocked"] = "Está bloqueado",
        ["GameObject.ContentsVisible"] = "Contenido visible",
        ["GameObject.MaxCapacity"] = "📦 Capacidad máxima (cm³)",
        ["GameObject.Volume"] = "📐 Volumen (cm³)",
        ["GameObject.Weight"] = "⚖️ Peso (g)",
        ["GameObject.Price"] = "💰 Precio",
        ["GameObject.NutritionAmount"] = "🍽️ Cantidad (nutrición)",
        ["GameObject.ContainedObjectIds"] = "📦 Objetos contenidos",
        ["GameObject.KeyId"] = "Llave necesaria",
        ["GameObject.Visible"] = "Visible",
        ["GameObject.IsLightSource"] = "Es luminoso",
        ["GameObject.IsLit"] = "Está encendido",
        ["GameObject.LightTurnsRemaining"] = "Turnos de luz (-1 = infinito)",
        ["GameObject.CanExtinguish"] = "Se puede apagar",
        ["GameObject.CanIgnite"] = "Se puede encender",
        ["GameObject.IgniterObjectId"] = "Objeto encendedor",
        ["GameObject.CraftingRecipe"] = "Se fabrica con",
        ["GameObject.HandsRequired"] = "✋ Manos requeridas",
        ["HandsRequired"] = "✋ Manos requeridas",
        ["AttackBonus"] = "⚔️ Bonus de ataque",
        ["DefenseBonus"] = "🛡️ Bonus de defensa",
        ["DamageType"] = "💥 Tipo de daño",
        ["MaxDurability"] = "🔧 Durabilidad máxima",
        ["CurrentDurability"] = "🔧 Durabilidad actual",
        ["InitiativeBonus"] = "⚡ Bonus de iniciativa",

        // NPC
        ["Npc.RoomId"] = "Sala",
        ["Npc.Dialogue"] = "Diálogo",
        ["Npc.InventoryObjectIds"] = "Objetos en inventario",
        ["Npc.Inventory"] = "🎒 Inventario",
        ["Npc.EquippedRightHandId"] = "✋ Mano derecha",
        ["Npc.EquippedLeftHandId"] = "🤚 Mano izquierda",
        ["Npc.EquippedTorsoId"] = "👕 Torso",
        ["Npc.EquippedHeadId"] = "🪖 Cabeza",
        ["Npc.Visible"] = "Visible",
        ["Npc.Stats"] = "Estadísticas",
        ["Npc.IsShopkeeper"] = "Es comerciante",
        ["Npc.ShopInventory"] = "Inventario de tienda",
        ["Npc.BuyPriceMultiplier"] = "Multiplicador compra",
        ["Npc.SellPriceMultiplier"] = "Multiplicador venta",
        ["Npc.IsPatrolling"] = "Está patrullando",
        ["Npc.PatrolSpeed"] = "Velocidad patrulla",
        ["Npc.IsFollowingPlayer"] = "Sigue al jugador",
        ["Npc.FollowSpeed"] = "Velocidad seguimiento",
        ["Npc.IsCorpse"] = "Es un cadáver",
        ["IsShopkeeper"] = "Es comerciante",
        ["ShopInventory"] = "Inventario de tienda",
        ["BuyPriceMultiplier"] = "Multiplicador compra",
        ["SellPriceMultiplier"] = "Multiplicador venta",
        ["IsPatrolling"] = "Está patrullando",
        ["PatrolMovementMode"] = "Modo de movimiento",
        ["PatrolSpeed"] = "Velocidad",
        ["PatrolTimeInterval"] = "Intervalo",
        ["IsFollowingPlayer"] = "Sigue al jugador",
        ["FollowMovementMode"] = "Modo de movimiento",
        ["FollowSpeed"] = "Velocidad",
        ["FollowTimeInterval"] = "Intervalo",
        ["IsCorpse"] = "Es un cadáver",

        // Puerta
        ["Door.RoomIdA"] = "Sala A",
        ["Door.RoomIdB"] = "Sala B",
        ["Door.IsOpen"] = "Está abierta",
        ["Door.IsLocked"] = "Cerradura",
        ["Door.Visible"] = "Visible",
        ["Door.RequiredQuests"] = "Requisitos de misión",

        // Quest
        ["QuestDefinition.Name"] = "Nombre",
        ["QuestDefinition.Description"] = "Descripción",
        ["QuestDefinition.IsMainQuest"] = "Misión principal",
        ["QuestDefinition.Objectives"] = "Objetivos",

        // Llave
        ["KeyDefinition.ObjectId"] = "Objeto",
        ["KeyDefinition.LockIds"] = "Cerraduras",

        // Jugador
        ["PlayerDefinition.Name"] = "Nombre",
        ["PlayerDefinition.Age"] = "🎂 Edad (años)",
        ["PlayerDefinition.Weight"] = "⚖️ Peso (kg)",
        ["PlayerDefinition.Height"] = "📏 Altura (cm)",
        ["PlayerDefinition.Strength"] = "💪 Fuerza",
        ["PlayerDefinition.Constitution"] = "🛡️ Constitución",
        ["PlayerDefinition.Intelligence"] = "🧠 Inteligencia",
        ["PlayerDefinition.Dexterity"] = "🏃 Destreza",
        ["PlayerDefinition.Charisma"] = "✨ Carisma",
        ["PlayerDefinition.InitialMoney"] = "💰 Dinero inicial",
        ["PlayerDefinition.MaxInventoryWeight"] = "⚖️ Peso máx. inventario (g)",
        ["PlayerDefinition.MaxInventoryVolume"] = "📐 Volumen máx. inventario (cm³)",
        ["PlayerDefinition.InitialInventory"] = "🎒 Inventario inicial",
        ["PlayerDefinition.InitialRightHandId"] = "✋ Mano derecha",
        ["PlayerDefinition.InitialLeftHandId"] = "🤚 Mano izquierda",
        ["PlayerDefinition.InitialTorsoId"] = "👕 Torso",
        ["PlayerDefinition.InitialHeadId"] = "🪖 Cabeza",
        ["InitialInventory"] = "🎒 Inventario inicial",
        ["InitialRightHandId"] = "✋ Mano derecha",
        ["InitialLeftHandId"] = "🤚 Mano izquierda",
        ["InitialTorsoId"] = "👕 Torso",
        ["InitialHeadId"] = "🪖 Cabeza",
        ["Inventory"] = "🎒 Inventario",
        ["EquippedRightHandId"] = "✋ Mano derecha",
        ["EquippedLeftHandId"] = "🤚 Mano izquierda",
        ["EquippedTorsoId"] = "👕 Torso",
        ["EquippedHeadId"] = "🪖 Cabeza",
        ["Constitution"] = "🛡️ Constitución",
        ["Charisma"] = "✨ Carisma",
        ["Age"] = "🎂 Edad",
        ["Height"] = "📏 Altura",
        ["InitialMoney"] = "💰 Dinero inicial",
    };

    private static string GetDisplayLabel(PropertyInfo prop)
    {
        var keyByType = $"{prop.DeclaringType?.Name}.{prop.Name}";
        if (DisplayNameMap.TryGetValue(keyByType, out var typedLabel))
            return typedLabel;

        if (DisplayNameMap.TryGetValue(prop.Name, out var label))
            return label;

        return SplitCamelCase(prop.Name);
    }

    private static string SplitCamelCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sb = new StringBuilder();
        char? prev = null;
        foreach (var c in input)
        {
            if (prev.HasValue && char.IsUpper(c) && (char.IsLower(prev.Value) || char.IsDigit(prev.Value)))
            {
                sb.Append(' ');
            }
            sb.Append(c);
            prev = c;
        }

        return sb.ToString();
    }

    private void ShowMusicIdHelp()
    {
        var message =
            "Recomendaciones para la imagen de sala:\n\n" +
            "• Relación de aspecto recomendada: 3.5:1 (panorámica horizontal)\n" +
            "• Resolución recomendada: 1400x400 píxeles\n\n" +
            "Esto asegurará que la imagen se vea correctamente en el visor de la sala.";

        var owner = Window.GetWindow(this);
        new AlertWindow(message, "Imagen de sala")
        {
            Owner = owner
        }.ShowDialog();
    }

    /// <summary>
    /// Fuerza la actualización de los bindings pendientes del control con foco.
    /// Necesario para que los TextBox y PasswordBox actualicen su valor antes de validar.
    /// </summary>
    public void UpdateBindings()
    {
        // Para el PasswordBox de clave de encriptación, actualizamos directamente el valor
        // ya que no usa bindings sino eventos LostFocus que pueden no haberse disparado aún
        if (_encryptionPasswordBox != null && _currentObject is GameInfo gameInfo)
        {
            var trimmed = (_encryptionPasswordBox.Password ?? string.Empty).Trim();
            gameInfo.EncryptionKey = trimmed;
        }

        // Obtener el elemento con foco actual para TextBox
        var focusedElement = FocusManager.GetFocusedElement(this);
        if (focusedElement is TextBox textBox)
        {
            var binding = textBox.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
        }
    }

    /// <summary>
    /// Aplica las validaciones pendientes a todos los campos del objeto actual.
    /// Debe llamarse antes de guardar el mundo para asegurar que los valores
    /// que no han disparado LostFocus sean validados y aplicados correctamente.
    /// </summary>
    public void ApplyPendingValidations()
    {
        if (_currentObject is PlayerDefinition playerDef)
        {
            // Validar y corregir InitialMoney si es negativo
            if (playerDef.InitialMoney < 0)
            {
                playerDef.InitialMoney = 0;
            }
        }

        // También actualizar bindings pendientes
        UpdateBindings();
    }

    /// <summary>
    /// Actualiza el valor de la clave de encriptación desde el PasswordBox al GameInfo proporcionado.
    /// Útil cuando el usuario puede tener otro objeto seleccionado pero se está validando la clave.
    /// </summary>
    public void UpdateEncryptionKey(GameInfo gameInfo)
    {
        if (_encryptionPasswordBox != null && gameInfo != null)
        {
            var trimmed = (_encryptionPasswordBox.Password ?? string.Empty).Trim();
            gameInfo.EncryptionKey = trimmed;
        }
    }

    /// <summary>
    /// Maneja el scroll con la rueda del ratón sobre el panel de propiedades
    /// </summary>
    private void MainScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scrollViewer = sender as ScrollViewer;
        if (scrollViewer == null) return;

        // Verificar si hay algún ComboBox con el dropdown abierto
        if (HasOpenComboBox(RootPanel))
        {
            // No manejar el evento para que el ComboBox pueda hacer scroll
            return;
        }

        // Calcular el nuevo offset (e.Delta es positivo cuando se hace scroll hacia arriba)
        var delta = e.Delta > 0 ? -50 : 50;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);

        e.Handled = true;
    }

    /// <summary>
    /// Verifica recursivamente si hay algún ComboBox con el dropdown abierto
    /// </summary>
    private static bool HasOpenComboBox(DependencyObject parent)
    {
        if (parent == null) return false;

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is ComboBox combo && combo.IsDropDownOpen)
                return true;

            if (HasOpenComboBox(child))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Actualiza la visibilidad de las propiedades según las condiciones definidas
    /// </summary>
    private void UpdatePropertyVisibility()
    {
        foreach (var kvp in _visibilityConditions)
        {
            if (_propertyElements.TryGetValue(kvp.Key, out var element))
            {
                var shouldBeVisible = kvp.Value();
                element.Visibility = shouldBeVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        // Actualizar visibilidad de secciones basado en si tienen propiedades visibles
        UpdateSectionVisibility();
    }

    /// <summary>
    /// Oculta secciones (Expanders) que no tienen ninguna propiedad visible.
    /// Una sección se oculta solo si TODAS sus propiedades con condiciones de visibilidad están ocultas.
    /// </summary>
    private void UpdateSectionVisibility()
    {
        // Crear diccionario inverso: sectionName -> expander
        var sectionToExpander = _expanderSectionNames.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        // Agrupar propiedades por sección
        var propertiesBySection = _propertyToSectionName
            .GroupBy(kvp => kvp.Value)
            .ToDictionary(g => g.Key, g => g.Select(kvp => kvp.Key).ToList());

        foreach (var section in propertiesBySection)
        {
            if (!sectionToExpander.TryGetValue(section.Key, out var expander))
                continue;

            // Contar propiedades con condiciones y cuántas están visibles
            var conditionalCount = 0;
            var visibleConditionalCount = 0;

            foreach (var propName in section.Value)
            {
                if (_visibilityConditions.TryGetValue(propName, out var condition))
                {
                    conditionalCount++;
                    if (condition())
                        visibleConditionalCount++;
                }
            }

            // Si todas las propiedades de la sección tienen condiciones y ninguna es visible, ocultar
            // (Si hay propiedades sin condiciones, siempre serán visibles, así que la sección se muestra)
            var allPropertiesAreConditional = conditionalCount == section.Value.Count;
            var shouldHideSection = allPropertiesAreConditional && visibleConditionalCount == 0;

            expander.Visibility = shouldHideSection ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    /// <summary>
    /// Determina si una propiedad debe tener sangría (es una subpropiedad de otra)
    /// </summary>
    private bool ShouldIndentProperty(object obj, PropertyInfo prop)
    {
        var name = prop.Name;

        // Propiedades de contenedor (subpropiedades de IsContainer)
        if (obj is GameObject)
        {
            if (name is "IsOpenable" or "IsOpen" or "IsLocked" or "ContentsVisible"
                or "MaxCapacity" or "ContainedObjectIds" or "KeyId")
                return true;

            // Propiedades de iluminación (subpropiedades de IsLightSource)
            if (name is "IsLit" or "LightTurnsRemaining" or "CanExtinguish" or "CanIgnite" or "IgniterObjectId")
                return true;

            // TextContent es subpropiedad de CanRead
            if (name == PN.TextContent)
                return true;

            // HandsRequired es subpropiedad de Type (solo para armas)
            if (name == PN.HandsRequired)
                return true;
        }

        // Propiedades de patrulla/seguimiento de NPC (subpropiedades)
        if (obj is Npc)
        {
            // Subpropiedades de IsPatrolling
            if (name == PN.PatrolMovementMode || name == PN.PatrolSpeed || name == PN.PatrolTimeInterval)
                return true;

            // Subpropiedades de IsFollowingPlayer
            if (name == PN.FollowMovementMode || name == PN.FollowSpeed || name == PN.FollowTimeInterval)
                return true;

            // Propiedades de tienda (subpropiedades de IsShopkeeper)
            if (name == PN.ShopInventory || name == PN.BuyPriceMultiplier || name == PN.SellPriceMultiplier)
                return true;
        }

        // Propiedades de necesidades básicas (subpropiedades de BasicNeedsEnabled)
        // y MagicEnabled (subpropiedad de CombatEnabled)
        if (obj is GameInfo)
        {
            if (name is "MagicEnabled")
                return true;
            if (name is "HungerRate" or "ThirstRate" or "SleepRate"
                or "HungerDeathText" or "ThirstDeathText" or "SleepDeathText")
                return true;
        }

        // Propiedades de visibilidad de Door (subpropiedades de Visible)
        if (obj is Door)
        {
            if (name == PN.RequiredQuests)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Obtiene la condición de visibilidad para una propiedad
    /// </summary>
    private Func<bool>? GetVisibilityCondition(object obj, PropertyInfo prop)
    {
        var name = prop.Name;

        // Condiciones para GameObject
        if (obj is GameObject gameObject)
        {
            // TextContent solo visible si CanRead = true
            if (name == PN.TextContent)
                return () => gameObject.CanRead;

            // IsOpen solo visible si IsContainer = true Y IsOpenable = true
            if (name == PN.IsOpen)
                return () => gameObject.IsContainer && gameObject.IsOpenable;

            // KeyId solo visible si IsContainer = true Y IsLocked = true
            if (name == PN.KeyId)
                return () => gameObject.IsContainer && gameObject.IsLocked;

            // Propiedades de contenedor solo visibles si IsContainer = true
            if (name == PN.IsOpenable || name == PN.IsLocked || name == PN.ContentsVisible
                || name == PN.MaxCapacity || name == PN.ContainedObjectIds)
                return () => gameObject.IsContainer;

            // === PROPIEDADES DE CONSUMIBLES ===
            // NutritionAmount solo visible si Type = Comida o Bebida
            if (name == PN.NutritionAmount)
                return () => gameObject.Type == ObjectType.Comida || gameObject.Type == ObjectType.Bebida;

            // === PROPIEDADES DE COMBATE ===
            // AttackBonus solo visible si Type = Arma
            if (name == PN.AttackBonus)
                return () => gameObject.Type == ObjectType.Arma;

            // HandsRequired solo visible si Type = Arma
            if (name == PN.HandsRequired)
                return () => gameObject.Type == ObjectType.Arma;

            // DefenseBonus solo visible si Type = Armadura o Casco
            if (name == PN.DefenseBonus)
                return () => gameObject.Type == ObjectType.Armadura || gameObject.Type == ObjectType.Casco;

            // DamageType solo visible si Type = Arma
            if (name == PN.DamageType)
                return () => gameObject.Type == ObjectType.Arma;

            // MaxDurability y CurrentDurability visibles si Type = Arma, Armadura o Casco
            if (name == PN.MaxDurability || name == PN.CurrentDurability)
                return () => gameObject.Type == ObjectType.Arma || gameObject.Type == ObjectType.Armadura || gameObject.Type == ObjectType.Casco;

            // InitiativeBonus visible si Type = Arma, Armadura o Casco
            if (name == PN.InitiativeBonus)
                return () => gameObject.Type == ObjectType.Arma || gameObject.Type == ObjectType.Armadura || gameObject.Type == ObjectType.Casco;

            // === PROPIEDADES DE ILUMINACIÓN ===
            // IsLit solo visible si IsLightSource = true
            if (name == PN.IsLit)
                return () => gameObject.IsLightSource;

            // LightTurnsRemaining solo visible si IsLightSource = true
            if (name == PN.LightTurnsRemaining)
                return () => gameObject.IsLightSource;

            // CanExtinguish solo visible si IsLightSource = true
            if (name == PN.CanExtinguish)
                return () => gameObject.IsLightSource;

            // CanIgnite solo visible si IsLightSource = true
            if (name == PN.CanIgnite)
                return () => gameObject.IsLightSource;

            // IgniterObjectId solo visible si IsLightSource = true Y CanIgnite = true
            if (name == PN.IgniterObjectId)
                return () => gameObject.IsLightSource && gameObject.CanIgnite;

            // CraftingRecipe solo visible si CraftingEnabled = true en GameInfo
            if (name == PN.CraftingRecipe)
                return () => GetGameInfo?.Invoke()?.CraftingEnabled == true;

            return null;
        }

        // Condiciones para Room
        if (obj is Room room)
        {
            // IsIlluminated solo visible si IsInterior = true
            if (name == PN.IsIlluminated)
                return () => room.IsInterior;

            return null;
        }

        // Condiciones para GameInfo
        if (obj is GameInfo gameInfo)
        {
            // MagicEnabled solo visible si CombatEnabled = true
            if (name == PN.MagicEnabled)
                return () => gameInfo.CombatEnabled;

            // Propiedades de necesidades básicas solo visibles si BasicNeedsEnabled = true
            if (name == PN.HungerRate || name == PN.ThirstRate || name == PN.SleepRate
                || name == PN.HungerDeathText || name == PN.ThirstDeathText || name == PN.SleepDeathText
                || name == PN.HealthDeathText || name == PN.SanityDeathText)
                return () => gameInfo.BasicNeedsEnabled;

            return null;
        }

        // Condiciones para NPC
        if (obj is Npc npc)
        {
            // IsPatrolling solo visible si NO está siguiendo al jugador
            if (name == PN.IsPatrolling)
                return () => !npc.IsFollowingPlayer;

            // PatrolMovementMode solo visible si está patrullando Y NO siguiendo
            if (name == PN.PatrolMovementMode)
                return () => npc.IsPatrolling && !npc.IsFollowingPlayer;

            // PatrolSpeed solo visible si está patrullando Y NO siguiendo Y modo Turns
            if (name == PN.PatrolSpeed)
                return () => npc.IsPatrolling && !npc.IsFollowingPlayer && npc.PatrolMovementMode == MovementMode.Turns;

            // PatrolTimeInterval solo visible si está patrullando Y NO siguiendo Y modo Time
            if (name == PN.PatrolTimeInterval)
                return () => npc.IsPatrolling && !npc.IsFollowingPlayer && npc.PatrolMovementMode == MovementMode.Time;

            // IsFollowingPlayer solo visible si NO está patrullando
            if (name == PN.IsFollowingPlayer)
                return () => !npc.IsPatrolling;

            // FollowMovementMode solo visible si está siguiendo Y NO patrullando
            if (name == PN.FollowMovementMode)
                return () => npc.IsFollowingPlayer && !npc.IsPatrolling;

            // FollowSpeed solo visible si está siguiendo Y NO patrullando Y modo Turns
            if (name == PN.FollowSpeed)
                return () => npc.IsFollowingPlayer && !npc.IsPatrolling && npc.FollowMovementMode == MovementMode.Turns;

            // FollowTimeInterval solo visible si está siguiendo Y NO patrullando Y modo Time
            if (name == PN.FollowTimeInterval)
                return () => npc.IsFollowingPlayer && !npc.IsPatrolling && npc.FollowMovementMode == MovementMode.Time;

            // Propiedades de tienda solo visibles si IsShopkeeper = true
            if (name == PN.ShopInventory || name == PN.BuyPriceMultiplier || name == PN.SellPriceMultiplier)
                return () => npc.IsShopkeeper;

            return null;
        }

        // Condiciones para PlayerDefinition
        if (obj is PlayerDefinition)
        {
            // MaxInventoryWeight y MaxInventoryVolume solo visibles si CraftingEnabled = true
            if (name == PN.MaxInventoryWeight || name == PN.MaxInventoryVolume)
                return () => GetGameInfo?.Invoke()?.CraftingEnabled == true;

            return null;
        }

        // Condiciones para Door
        if (obj is Door door)
        {
            // RequiredQuests solo visible si Visible = true
            if (name == PN.RequiredQuests)
                return () => door.Visible;

            return null;
        }

        return null;
    }

    /// <summary>
    /// Determina si un objeto está contenido dentro de otro objeto
    /// </summary>
    private bool IsObjectContainedInAnother(GameObject obj)
    {
        if (GetObjects == null) return false;

        var allObjects = GetObjects().ToList();
        return allObjects.Any(container =>
            container.IsContainer &&
            container.ContainedObjectIds.Contains(obj.Id, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Encuentra el objeto contenedor de un objeto dado
    /// </summary>
    private GameObject? FindContainerObject(GameObject obj)
    {
        if (GetObjects == null) return null;

        var allObjects = GetObjects().ToList();
        return allObjects.FirstOrDefault(container =>
            container.IsContainer &&
            container.ContainedObjectIds.Contains(obj.Id, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Actualiza la sala de todos los objetos contenidos dentro de un contenedor
    /// </summary>
    private void UpdateContainedObjectsRoom(GameObject container, string? newRoomId)
    {
        if (!container.IsContainer || GetObjects == null) return;

        var allObjects = GetObjects().ToList();
        foreach (var containedId in container.ContainedObjectIds)
        {
            var containedObj = allObjects.FirstOrDefault(o =>
                string.Equals(o.Id, containedId, StringComparison.OrdinalIgnoreCase));

            if (containedObj != null)
            {
                containedObj.RoomId = newRoomId;
                PropertyEdited?.Invoke(containedObj, nameof(GameObject.RoomId));
            }
        }
    }

    /// <summary>
    /// Crea el editor para una característica de PlayerDefinition con slider y validación.
    /// Cada característica tiene un mínimo de 10 y un máximo de 60.
    /// </summary>
    private FrameworkElement CreateAttributeEditor(PlayerDefinition playerDef, PropertyInfo prop)
    {
        const int MinValue = 10;
        const int MaxValue = 60;

        var panel = new Grid
        {
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var currentValue = prop.GetValue(playerDef) is int v ? v : 20;
        if (currentValue < MinValue) currentValue = MinValue;
        if (currentValue > MaxValue) currentValue = MaxValue;

        var slider = new Slider
        {
            Minimum = MinValue,
            Maximum = MaxValue,
            Value = currentValue,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(slider, 0);

        var valueLabel = new TextBlock
        {
            Text = currentValue.ToString(),
            Width = 30,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0xBB, 0xFF))
        };
        Grid.SetColumn(valueLabel, 1);

        slider.ValueChanged += (_, args) =>
        {
            try
            {
                if (_currentObject is not PlayerDefinition target) return;

                var newValue = (int)args.NewValue;
                if (newValue < MinValue) newValue = MinValue;
                if (newValue > MaxValue) newValue = MaxValue;

                prop.SetValue(target, newValue);
                valueLabel.Text = newValue.ToString();
                PropertyEdited?.Invoke(target, prop.Name);

                // Actualizar el total de características en el header
                UpdateAttributesTotalLabel(target);
            }
            catch
            {
                // Ignorar errores
            }
        };

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);

        return panel;
    }

    /// <summary>
    /// Crea un slider para velocidad de NPC (patrulla o seguimiento).
    /// </summary>
    private FrameworkElement CreateNpcSpeedSlider(Npc npc, PropertyInfo prop, int minValue, int maxValue, string? suffix = null, Dictionary<int, string>? customLabels = null, double? sliderMaxWidth = null)
    {
        var panel = new Grid
        {
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var currentValue = prop.GetValue(npc) is int v ? v : minValue;
        if (currentValue < minValue) currentValue = minValue;
        if (currentValue > maxValue) currentValue = maxValue;

        string GetLabel(int val) => customLabels != null && customLabels.TryGetValue(val, out var lbl) ? lbl : val + (suffix ?? "");

        var slider = new Slider
        {
            Minimum = minValue,
            Maximum = maxValue,
            Value = currentValue,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = prop.Name == PN.FollowSpeed ? 10 : 1,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (sliderMaxWidth.HasValue)
            slider.MaxWidth = sliderMaxWidth.Value;
        Grid.SetColumn(slider, 0);

        var valueLabel = new TextBlock
        {
            Text = GetLabel(currentValue),
            Width = 80,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0xA5, 0x00)) // Naranja para patrulla
        };
        Grid.SetColumn(valueLabel, 1);

        slider.ValueChanged += (_, args) =>
        {
            try
            {
                if (_currentObject is not Npc target) return;

                var newValue = (int)args.NewValue;
                if (newValue < minValue) newValue = minValue;
                if (newValue > maxValue) newValue = maxValue;

                prop.SetValue(target, newValue);
                valueLabel.Text = GetLabel(newValue);
                PropertyEdited?.Invoke(target, prop.Name);
            }
            catch
            {
                // Ignorar errores
            }
        };

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);

        return panel;
    }

    /// <summary>
    /// Crea un ComboBox para seleccionar el modo de movimiento (Turns/Time).
    /// </summary>
    private FrameworkElement CreateMovementModeComboBox(Npc npc, PropertyInfo prop, bool isPatrol)
    {
        var currentValue = prop.GetValue(npc) is MovementMode m ? m : MovementMode.Turns;

        var options = new List<MovementModeComboItem>
        {
            new MovementModeComboItem { Value = MovementMode.Turns, DisplayName = "Turnos" },
            new MovementModeComboItem { Value = MovementMode.Time, DisplayName = "Tiempo" }
        };

        var combo = new ComboBox
        {
            Margin = new Thickness(0, 2, 0, 0),
            DisplayMemberPath = nameof(MovementModeComboItem.DisplayName),
            SelectedValuePath = nameof(MovementModeComboItem.Value),
            ItemsSource = options
        };

        combo.SelectedValue = currentValue;

        combo.SelectionChanged += (_, _) =>
        {
            if (_currentObject is not Npc target) return;
            if (combo.SelectedValue is not MovementMode newMode) return;

            prop.SetValue(target, newMode);
            PropertyEdited?.Invoke(target, prop.Name);

            // Actualizar visibilidad de controles dependientes
            UpdatePropertyVisibility();
        };

        return combo;
    }

    /// <summary>
    /// Crea un slider para intervalo de tiempo (en segundos).
    /// </summary>
    private FrameworkElement CreateTimeIntervalSlider(Npc npc, PropertyInfo prop, int minValue, int maxValue)
    {
        var panel = new Grid
        {
            Margin = new Thickness(0, 2, 0, 0)
        };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var currentValue = prop.GetValue(npc) is float v ? v : minValue;
        if (currentValue < minValue) currentValue = minValue;
        if (currentValue > maxValue) currentValue = maxValue;

        var slider = new Slider
        {
            Minimum = minValue,
            Maximum = maxValue,
            Value = currentValue,
            IsSnapToTickEnabled = true,
            TickFrequency = 1,
            SmallChange = 1,
            LargeChange = 5,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(slider, 0);

        var valueLabel = new TextBlock
        {
            Text = $"{(int)currentValue} seg",
            Width = 80,
            TextAlignment = TextAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0, 0, 0),
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xBF, 0xFF)) // Azul para tiempo
        };
        Grid.SetColumn(valueLabel, 1);

        slider.ValueChanged += (_, args) =>
        {
            try
            {
                if (_currentObject is not Npc target) return;

                var newValue = (float)args.NewValue;
                if (newValue < minValue) newValue = minValue;
                if (newValue > maxValue) newValue = maxValue;

                prop.SetValue(target, newValue);
                valueLabel.Text = $"{(int)newValue} seg";
                PropertyEdited?.Invoke(target, prop.Name);
            }
            catch
            {
                // Ignorar errores
            }
        };

        panel.Children.Add(slider);
        panel.Children.Add(valueLabel);

        return panel;
    }

    /// <summary>
    /// Crea radio buttons para intervalo de tiempo (Camina=3", Lento=6", Muy lento=10").
    /// </summary>
    private FrameworkElement CreateTimeIntervalRadioButtons(Npc npc, PropertyInfo prop)
    {
        var currentValue = prop.GetValue(npc) is float v ? v : 3f;

        // Mapear valor actual al más cercano
        int selectedOption;
        if (currentValue <= 4.5f) selectedOption = 3;
        else if (currentValue <= 8f) selectedOption = 6;
        else selectedOption = 10;

        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
        var options = new[] { (3f, "Camina (3\")"), (6f, "Lento (6\")"), (10f, "Muy lento (10\")") };

        foreach (var (val, text) in options)
        {
            var rb = new RadioButton
            {
                Content = text,
                IsChecked = (int)val == selectedOption,
                Tag = val,
                Margin = new Thickness(0, 0, 12, 0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x00, 0xBF, 0xFF)) // Azul para tiempo
            };
            rb.Checked += (_, _) =>
            {
                if (_currentObject is Npc target && rb.Tag is float newVal)
                {
                    prop.SetValue(target, newVal);
                    PropertyEdited?.Invoke(target, prop.Name);
                }
            };
            panel.Children.Add(rb);
        }

        return panel;
    }

    /// <summary>
    /// Actualiza el label del total de características.
    /// </summary>
    private void UpdateAttributesTotalLabel(PlayerDefinition player)
    {
        if (_attributesTotalLabel == null) return;

        var total = player.TotalAttributePoints;
        _attributesTotalLabel.Text = $" ({total}/100)";
        _attributesTotalLabel.Foreground = GetAttributesTotalColor(total);
    }

    /// <summary>
    /// Obtiene el color del total de características según si es correcto, por encima o por debajo.
    /// </summary>
    private static Brush GetAttributesTotalColor(int total)
    {
        if (total == 100)
            return Brushes.LimeGreen; // Verde - correcto
        else if (total > 100)
            return Brushes.Red; // Rojo - exceso
        else
            return Brushes.Yellow; // Amarillo - faltan puntos
    }

    /// <summary>
    /// Crea un selector múltiple de objetos para propiedades List&lt;string&gt; como ShopInventory o InventoryObjectIds.
    /// </summary>
    private FrameworkElement CreateMultiSelectObjectPicker(Npc npc, PropertyInfo prop)
    {
        var currentList = prop.GetValue(npc) as List<string> ?? new List<string>();
        var allObjects = GetObjects?.Invoke()?.ToList() ?? new List<GameObject>();

        var mainPanel = new StackPanel();

        // Etiqueta que muestra el resumen de objetos seleccionados
        var summaryText = new TextBlock
        {
            Text = GetObjectsSummary(currentList, allObjects),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con los checkboxes
        var expander = new Expander
        {
            Header = $"Seleccionar objetos ({currentList.Count})",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var checkboxPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        if (!allObjects.Any())
        {
            checkboxPanel.Children.Add(new TextBlock
            {
                Text = "(No hay objetos en el mundo)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontStyle = FontStyles.Italic
            });
        }
        else
        {
            foreach (var obj in allObjects.OrderBy(o => o.Name))
            {
                var isSelected = currentList.Contains(obj.Id, StringComparer.OrdinalIgnoreCase);

                var checkPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var checkbox = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = obj.Id
                };

                var checkLabel = new TextBlock
                {
                    Text = $"{obj.Name}",
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0),
                    Cursor = Cursors.Hand
                };

                // Si el objeto tiene precio, mostrarlo
                if (obj.Price > 0)
                {
                    checkLabel.Text = $"{obj.Name} ({obj.Price})";
                }

                checkLabel.MouseLeftButtonDown += (_, _) => checkbox.IsChecked = !checkbox.IsChecked;

                checkbox.Checked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Npc targetNpc) return;
                        var list = prop.GetValue(targetNpc) as List<string> ?? new List<string>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId) && !list.Contains(objId, StringComparer.OrdinalIgnoreCase))
                        {
                            list.Add(objId);
                            prop.SetValue(targetNpc, list);
                            PropertyEdited?.Invoke(targetNpc, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Seleccionar objetos ({list.Count})";
                            summaryText.Text = GetObjectsSummary(list, allObjects);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                checkbox.Unchecked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Npc targetNpc) return;
                        var list = prop.GetValue(targetNpc) as List<string> ?? new List<string>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId))
                        {
                            list.RemoveAll(id => string.Equals(id, objId, StringComparison.OrdinalIgnoreCase));
                            prop.SetValue(targetNpc, list);
                            PropertyEdited?.Invoke(targetNpc, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Seleccionar objetos ({list.Count})";
                            summaryText.Text = GetObjectsSummary(list, allObjects);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                checkPanel.Children.Add(checkbox);
                checkPanel.Children.Add(checkLabel);
                checkboxPanel.Children.Add(checkPanel);
            }
        }

        // Wrap en ScrollViewer si hay muchos objetos
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = checkboxPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    /// <summary>
    /// Crea un editor de inventario de tienda con objetos y cantidades (-1 = infinito).
    /// </summary>
    private FrameworkElement CreateShopInventoryEditor(Npc npc, PropertyInfo prop)
    {
        var currentInventory = prop.GetValue(npc) as List<ShopItem> ?? new List<ShopItem>();
        var allObjects = GetObjects?.Invoke()?.ToList() ?? new List<GameObject>();

        var mainPanel = new StackPanel();

        // Etiqueta de resumen
        var summaryText = new TextBlock
        {
            Text = GetShopInventorySummary(currentInventory, allObjects),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con objetos y cantidades
        var expander = new Expander
        {
            Header = $"Objetos en tienda ({currentInventory.Count})",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var itemsPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        if (!allObjects.Any())
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = "(No hay objetos en el mundo)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontStyle = FontStyles.Italic
            });
        }
        else
        {
            foreach (var obj in allObjects.OrderBy(o => o.Name))
            {
                var shopItem = currentInventory.FirstOrDefault(si =>
                    si.ObjectId.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));
                var isSelected = shopItem != null;
                var quantity = shopItem?.Quantity ?? -1;

                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var checkbox = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = obj.Id
                };

                var checkLabel = new TextBlock
                {
                    Text = obj.Price > 0 ? $"{obj.Name} ({obj.Price})" : obj.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Width = 150
                };

                var qtyLabel = new TextBlock
                {
                    Text = "Cant:",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                var qtyInput = new TextBox
                {
                    Text = quantity.ToString(),
                    Width = 50,
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = obj.Id,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                var infiniteHint = new TextBlock
                {
                    Text = "(-1 = ∞)",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    FontSize = 10,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                checkLabel.MouseLeftButtonDown += (_, _) => checkbox.IsChecked = !checkbox.IsChecked;

                checkbox.Checked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Npc targetNpc) return;
                        var inventory = prop.GetValue(targetNpc) as List<ShopItem> ?? new List<ShopItem>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId) && !inventory.Any(si =>
                            si.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase)))
                        {
                            inventory.Add(new ShopItem { ObjectId = objId, Quantity = -1 });
                            prop.SetValue(targetNpc, inventory);
                            PropertyEdited?.Invoke(targetNpc, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Objetos en tienda ({inventory.Count})";
                            summaryText.Text = GetShopInventorySummary(inventory, allObjects);
                            qtyInput.Text = "-1";
                            qtyLabel.Visibility = Visibility.Visible;
                            qtyInput.Visibility = Visibility.Visible;
                            infiniteHint.Visibility = Visibility.Visible;
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                checkbox.Unchecked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Npc targetNpc) return;
                        var inventory = prop.GetValue(targetNpc) as List<ShopItem> ?? new List<ShopItem>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId))
                        {
                            inventory.RemoveAll(si =>
                                si.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase));
                            prop.SetValue(targetNpc, inventory);
                            PropertyEdited?.Invoke(targetNpc, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Objetos en tienda ({inventory.Count})";
                            summaryText.Text = GetShopInventorySummary(inventory, allObjects);
                            qtyLabel.Visibility = Visibility.Collapsed;
                            qtyInput.Visibility = Visibility.Collapsed;
                            infiniteHint.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                qtyInput.LostFocus += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not Npc targetNpc) return;
                        var inventory = prop.GetValue(targetNpc) as List<ShopItem> ?? new List<ShopItem>();
                        var objId = qtyInput.Tag as string;
                        var item = inventory.FirstOrDefault(si =>
                            si.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase));
                        if (item != null && int.TryParse(qtyInput.Text, out var newQty))
                        {
                            item.Quantity = newQty;
                            PropertyEdited?.Invoke(targetNpc, prop.Name);
                            summaryText.Text = GetShopInventorySummary(inventory, allObjects);
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                itemPanel.Children.Add(checkbox);
                itemPanel.Children.Add(checkLabel);
                itemPanel.Children.Add(qtyLabel);
                itemPanel.Children.Add(qtyInput);
                itemPanel.Children.Add(infiniteHint);
                itemsPanel.Children.Add(itemPanel);
            }
        }

        // Wrap en ScrollViewer
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 250,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = itemsPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    /// <summary>
    /// Genera un resumen del inventario de tienda.
    /// </summary>
    private string GetShopInventorySummary(List<ShopItem> inventory, List<GameObject> allObjects)
    {
        if (inventory == null || !inventory.Any())
            return "(Vacío)";

        var names = inventory
            .Select(si =>
            {
                var obj = allObjects.FirstOrDefault(o =>
                    o.Id.Equals(si.ObjectId, StringComparison.OrdinalIgnoreCase));
                var qtyText = si.Quantity < 0 ? "∞" : si.Quantity.ToString();
                return obj != null ? $"{obj.Name} x{qtyText}" : $"{si.ObjectId} x{qtyText}";
            })
            .ToList();

        return string.Join(", ", names);
    }

    /// <summary>
    /// Crea un editor de inventario con selección de objetos y cantidades.
    /// Funciona para Npc.Inventory y PlayerDefinition.InitialInventory.
    /// </summary>
    private FrameworkElement CreateInventoryEditor(object obj, PropertyInfo prop)
    {
        var currentInventory = prop.GetValue(obj) as List<InventoryItem> ?? new List<InventoryItem>();
        var allObjects = GetObjects?.Invoke()?.ToList() ?? new List<GameObject>();

        // Obtener IDs de objetos equipados (excluir inventario para esta verificación)
        var equippedObjectIds = GetUsedObjectIds(obj, excludeInventory: true);

        var mainPanel = new StackPanel();

        // Etiqueta de resumen
        var summaryText = new TextBlock
        {
            Text = GetInventorySummary(currentInventory, allObjects),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con objetos y cantidades
        var headerText = prop.Name == PN.InitialInventory ? "Inventario inicial" : "Inventario";
        var expander = new Expander
        {
            Header = $"{headerText} ({currentInventory.Count})",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var itemsPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        if (!allObjects.Any())
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = "(No hay objetos en el mundo)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontStyle = FontStyles.Italic
            });
        }
        else
        {
            foreach (var gameObj in allObjects.OrderBy(o => o.Name))
            {
                var invItem = currentInventory.FirstOrDefault(i =>
                    i.ObjectId.Equals(gameObj.Id, StringComparison.OrdinalIgnoreCase));
                var isSelected = invItem != null;
                var quantity = invItem?.Quantity ?? 1;

                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                // Verificar si el objeto está equipado
                var isEquipped = equippedObjectIds.Contains(gameObj.Id);

                var checkbox = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = gameObj.Id,
                    IsEnabled = !isEquipped
                };

                var checkLabel = new TextBlock
                {
                    Text = isEquipped ? $"{gameObj.Name} (equipado)" : gameObj.Name,
                    Foreground = isEquipped ? new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)) : Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 8, 0),
                    Cursor = isEquipped ? Cursors.Arrow : Cursors.Hand,
                    Width = 150,
                    ToolTip = isEquipped ? "Este objeto está equipado" : null
                };

                var qtyLabel = new TextBlock
                {
                    Text = "Cant:",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                var qtyInput = new TextBox
                {
                    Text = quantity.ToString(),
                    Width = 50,
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = gameObj.Id,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                var infiniteHint = new TextBlock
                {
                    Text = "(-1 = ∞)",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0),
                    FontSize = 10,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                // Solo permitir click en la etiqueta si no está equipado
                if (!isEquipped)
                    checkLabel.MouseLeftButtonDown += (_, _) => checkbox.IsChecked = !checkbox.IsChecked;

                // Capture variables for closures
                var capturedProp = prop;
                var capturedHeaderText = headerText;

                checkbox.Checked += (_, _) =>
                {
                    try
                    {
                        var inventory = capturedProp.GetValue(_currentObject) as List<InventoryItem> ?? new List<InventoryItem>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId) && !inventory.Any(i =>
                            i.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase)))
                        {
                            inventory.Add(new InventoryItem { ObjectId = objId, Quantity = 1 });
                            capturedProp.SetValue(_currentObject, inventory);
                            PropertyEdited?.Invoke(_currentObject!, capturedProp.Name);

                            // Refrescar el editor para actualizar listas de equipamiento
                            if (_currentObject != null)
                                SetObject(_currentObject);
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                checkbox.Unchecked += (_, _) =>
                {
                    try
                    {
                        var inventory = capturedProp.GetValue(_currentObject) as List<InventoryItem> ?? new List<InventoryItem>();
                        var objId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(objId))
                        {
                            inventory.RemoveAll(i =>
                                i.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase));
                            capturedProp.SetValue(_currentObject, inventory);
                            PropertyEdited?.Invoke(_currentObject!, capturedProp.Name);

                            // Refrescar el editor para actualizar listas de equipamiento
                            if (_currentObject != null)
                                SetObject(_currentObject);
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                qtyInput.LostFocus += (_, _) =>
                {
                    try
                    {
                        var inventory = capturedProp.GetValue(_currentObject) as List<InventoryItem> ?? new List<InventoryItem>();
                        var objId = qtyInput.Tag as string;
                        if (!string.IsNullOrEmpty(objId) && int.TryParse(qtyInput.Text, out var qty))
                        {
                            var item = inventory.FirstOrDefault(i =>
                                i.ObjectId.Equals(objId, StringComparison.OrdinalIgnoreCase));
                            if (item != null && item.Quantity != qty)
                            {
                                item.Quantity = qty;
                                PropertyEdited?.Invoke(_currentObject!, capturedProp.Name);
                                summaryText.Text = GetInventorySummary(inventory, allObjects);
                            }
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                itemPanel.Children.Add(checkbox);
                itemPanel.Children.Add(checkLabel);
                itemPanel.Children.Add(qtyLabel);
                itemPanel.Children.Add(qtyInput);
                itemPanel.Children.Add(infiniteHint);
                itemsPanel.Children.Add(itemPanel);
            }
        }

        // Wrap en ScrollViewer
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = itemsPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    /// <summary>
    /// Genera un resumen de texto del inventario.
    /// </summary>
    private string GetInventorySummary(List<InventoryItem> inventory, List<GameObject> allObjects)
    {
        if (inventory == null || !inventory.Any())
            return "(Vacío)";

        var names = inventory
            .Select(item =>
            {
                var obj = allObjects.FirstOrDefault(o =>
                    o.Id.Equals(item.ObjectId, StringComparison.OrdinalIgnoreCase));
                var qtyText = item.Quantity < 0 ? "∞" : item.Quantity.ToString();
                return obj != null ? $"{obj.Name} x{qtyText}" : $"{item.ObjectId} x{qtyText}";
            })
            .ToList();

        return string.Join(", ", names);
    }

    /// <summary>
    /// Crea un editor de slot de equipamiento con filtrado según el tipo de slot.
    /// - Mano derecha: Arma o Armadura
    /// - Mano izquierda: Arma (1 mano) o Armadura
    /// - Torso: solo Armadura
    /// </summary>
    private FrameworkElement CreateEquipmentSlotEditor(object obj, PropertyInfo prop, EquipmentSlot slot)
    {
        var allObjects = GetObjects?.Invoke()?.ToList() ?? new List<GameObject>();

        // Obtener objetos ya usados (en inventario u otros slots)
        var usedObjectIds = GetUsedObjectIds(obj, excludeSlot: slot);

        // Obtener el ID actualmente seleccionado para incluirlo siempre
        var currentId = Convert.ToString(prop.GetValue(obj)) ?? string.Empty;

        // Filtrar objetos según el slot y excluir los ya usados
        List<GameObject> validObjects;
        string emptyOption;

        switch (slot)
        {
            case EquipmentSlot.RightHand:
                // Armas (cualquier número de manos)
                validObjects = allObjects
                    .Where(o => o.Type == ObjectType.Arma)
                    .Where(o => !usedObjectIds.Contains(o.Id) || o.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                emptyOption = "(Sin equipar)";
                break;
            case EquipmentSlot.LeftHand:
                // Armas de 1 mano o Escudos
                validObjects = allObjects
                    .Where(o => (o.Type == ObjectType.Arma && o.HandsRequired == 1) || o.Type == ObjectType.Escudo)
                    .Where(o => !usedObjectIds.Contains(o.Id) || o.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                emptyOption = "(Sin equipar)";
                break;
            case EquipmentSlot.Torso:
                // Solo armaduras de cuerpo
                validObjects = allObjects
                    .Where(o => o.Type == ObjectType.Armadura)
                    .Where(o => !usedObjectIds.Contains(o.Id) || o.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                emptyOption = "(Sin equipar)";
                break;
            case EquipmentSlot.Head:
                // Solo cascos
                validObjects = allObjects
                    .Where(o => o.Type == ObjectType.Casco)
                    .Where(o => !usedObjectIds.Contains(o.Id) || o.Id.Equals(currentId, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                emptyOption = "(Sin equipar)";
                break;
            default:
                validObjects = new List<GameObject>();
                emptyOption = "(Sin equipar)";
                break;
        }

        // Crear lista de opciones
        var options = new List<KeyComboItem> { new KeyComboItem { Id = "", DisplayName = emptyOption } };
        options.AddRange(validObjects
            .OrderBy(o => o.Name)
            .Select(o => new KeyComboItem
            {
                Id = o.Id,
                DisplayName = o.Type == ObjectType.Arma
                    ? $"{o.Name} ({o.HandsRequired} mano{(o.HandsRequired > 1 ? "s" : "")})"
                    : o.Name
            }));

        var combo = new ComboBox
        {
            Margin = new Thickness(0, 2, 0, 0),
            DisplayMemberPath = nameof(KeyComboItem.DisplayName),
            SelectedValuePath = nameof(KeyComboItem.Id),
            ItemsSource = options
        };

        combo.SelectedValue = currentId;

        combo.SelectionChanged += (_, _) =>
        {
            try
            {
                if (_currentObject is not { } target) return;
                if (combo.SelectedValue is string selectedId)
                {
                    prop.SetValue(target, string.IsNullOrEmpty(selectedId) ? null : selectedId);
                    PropertyEdited?.Invoke(target, prop.Name);

                    // Si seleccionamos un arma de 2 manos en mano derecha, actualizar mano izquierda
                    if (slot == EquipmentSlot.RightHand && !string.IsNullOrEmpty(selectedId))
                    {
                        var selectedObj = allObjects.FirstOrDefault(o =>
                            o.Id.Equals(selectedId, StringComparison.OrdinalIgnoreCase));
                        if (selectedObj?.Type == ObjectType.Arma && selectedObj.HandsRequired == 2)
                        {
                            // Establecer el mismo ID en mano izquierda
                            string leftHandPropName = target is PlayerDefinition
                                ? PN.InitialLeftHandId
                                : PN.EquippedLeftHandId;
                            var leftHandProp = target.GetType().GetProperty(leftHandPropName);
                            if (leftHandProp != null)
                            {
                                leftHandProp.SetValue(target, selectedId);
                                PropertyEdited?.Invoke(target, leftHandPropName);
                            }
                        }
                    }

                    // Refrescar para actualizar listas de equipamiento e inventario
                    SetObject(target);
                }
            }
            catch
            {
                // Ignorar errores
            }
        };

        // Si es mano izquierda, verificar si está bloqueada por arma de 2 manos
        if (slot == EquipmentSlot.LeftHand)
        {
            string rightHandPropName = obj is PlayerDefinition
                ? PN.InitialRightHandId
                : PN.EquippedRightHandId;
            var rightHandProp = obj.GetType().GetProperty(rightHandPropName);
            var rightHandId = rightHandProp?.GetValue(obj) as string;

            if (!string.IsNullOrEmpty(rightHandId))
            {
                var rightHandObj = allObjects.FirstOrDefault(o =>
                    o.Id.Equals(rightHandId, StringComparison.OrdinalIgnoreCase));
                if (rightHandObj?.Type == ObjectType.Arma && rightHandObj.HandsRequired == 2)
                {
                    // Bloquear el combo y mostrar mensaje
                    combo.IsEnabled = false;
                    combo.ToolTip = $"Ocupada por arma de 2 manos: {rightHandObj.Name}";
                }
            }
        }

        return combo;
    }

    /// <summary>
    /// Obtiene los IDs de objetos ya usados globalmente (en todas las entidades),
    /// excluyendo la entidad actual siendo editada.
    /// </summary>
    /// <param name="currentEntity">La entidad actual (PlayerDefinition o Npc) a excluir.</param>
    /// <param name="excludeSlot">Slot a excluir del conteo para la entidad actual.</param>
    /// <param name="excludeInventory">Si es true, no incluir objetos del inventario de la entidad actual.</param>
    private HashSet<string> GetUsedObjectIds(object currentEntity, EquipmentSlot? excludeSlot = null, bool excludeInventory = false)
    {
        var usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Obtener objetos usados por el jugador
        var playerDef = GetPlayerDefinition?.Invoke();
        if (playerDef != null)
        {
            bool isCurrentEntity = ReferenceEquals(currentEntity, playerDef);
            AddPlayerUsedObjects(usedIds, playerDef, isCurrentEntity ? excludeSlot : null, isCurrentEntity && excludeInventory);
        }

        // Obtener objetos usados por todos los NPCs
        var npcs = GetNpcs?.Invoke()?.ToList() ?? new List<Npc>();
        foreach (var npc in npcs)
        {
            bool isCurrentEntity = ReferenceEquals(currentEntity, npc);
            AddNpcUsedObjects(usedIds, npc, isCurrentEntity ? excludeSlot : null, isCurrentEntity && excludeInventory);
        }

        return usedIds;
    }

    /// <summary>
    /// Añade los IDs de objetos usados por un jugador al conjunto.
    /// </summary>
    private void AddPlayerUsedObjects(HashSet<string> usedIds, PlayerDefinition player, EquipmentSlot? excludeSlot, bool excludeInventory)
    {
        // Inventario
        if (!excludeInventory && player.InitialInventory != null)
        {
            foreach (var item in player.InitialInventory)
                usedIds.Add(item.ObjectId);
        }

        // Equipamiento (excluyendo el slot actual si corresponde)
        if (excludeSlot != EquipmentSlot.RightHand && !string.IsNullOrEmpty(player.InitialRightHandId))
            usedIds.Add(player.InitialRightHandId);
        if (excludeSlot != EquipmentSlot.LeftHand && !string.IsNullOrEmpty(player.InitialLeftHandId))
        {
            // Si mano izquierda es igual a mano derecha (arma 2 manos), no duplicar
            if (player.InitialLeftHandId != player.InitialRightHandId)
                usedIds.Add(player.InitialLeftHandId);
        }
        if (excludeSlot != EquipmentSlot.Torso && !string.IsNullOrEmpty(player.InitialTorsoId))
            usedIds.Add(player.InitialTorsoId);
        if (excludeSlot != EquipmentSlot.Head && !string.IsNullOrEmpty(player.InitialHeadId))
            usedIds.Add(player.InitialHeadId);
    }

    /// <summary>
    /// Añade los IDs de objetos usados por un NPC al conjunto.
    /// </summary>
    private void AddNpcUsedObjects(HashSet<string> usedIds, Npc npc, EquipmentSlot? excludeSlot, bool excludeInventory)
    {
        // Inventario
        if (!excludeInventory && npc.Inventory != null)
        {
            foreach (var item in npc.Inventory)
                usedIds.Add(item.ObjectId);
        }

        // Equipamiento (excluyendo el slot actual si corresponde)
        if (excludeSlot != EquipmentSlot.RightHand && !string.IsNullOrEmpty(npc.EquippedRightHandId))
            usedIds.Add(npc.EquippedRightHandId);
        if (excludeSlot != EquipmentSlot.LeftHand && !string.IsNullOrEmpty(npc.EquippedLeftHandId))
        {
            // Si mano izquierda es igual a mano derecha (arma 2 manos), no duplicar
            if (npc.EquippedLeftHandId != npc.EquippedRightHandId)
                usedIds.Add(npc.EquippedLeftHandId);
        }
        if (excludeSlot != EquipmentSlot.Torso && !string.IsNullOrEmpty(npc.EquippedTorsoId))
            usedIds.Add(npc.EquippedTorsoId);
        if (excludeSlot != EquipmentSlot.Head && !string.IsNullOrEmpty(npc.EquippedHeadId))
            usedIds.Add(npc.EquippedHeadId);
    }

    /// <summary>
    /// Diccionario de traducciones de estados de misión.
    /// </summary>
    private static readonly Dictionary<QuestStatus, string> QuestStatusTranslations = new()
    {
        [QuestStatus.NotStarted] = "No iniciada",
        [QuestStatus.InProgress] = "En progreso",
        [QuestStatus.Completed] = "Completada",
        [QuestStatus.Failed] = "Fallida"
    };

    /// <summary>
    /// Crea un editor de requisitos de misiones con selección de misiones y estados.
    /// </summary>
    private FrameworkElement CreateRequiredQuestsEditor(object obj, PropertyInfo prop)
    {
        var currentRequirements = prop.GetValue(obj) as List<QuestRequirement> ?? new List<QuestRequirement>();
        var allQuests = GetQuests?.Invoke()?.ToList() ?? new List<QuestDefinition>();

        var mainPanel = new StackPanel();

        // Etiqueta de resumen
        var summaryText = new TextBlock
        {
            Text = GetRequiredQuestsSummary(currentRequirements, allQuests),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con misiones y estados
        var expander = new Expander
        {
            Header = $"Requisitos de misión ({currentRequirements.Count})",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var itemsPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        if (!allQuests.Any())
        {
            itemsPanel.Children.Add(new TextBlock
            {
                Text = "(No hay misiones definidas)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontStyle = FontStyles.Italic
            });
        }
        else
        {
            foreach (var quest in allQuests.OrderBy(q => q.Name))
            {
                var requirement = currentRequirements.FirstOrDefault(r =>
                    r.QuestId.Equals(quest.Id, StringComparison.OrdinalIgnoreCase));
                var isSelected = requirement != null;
                var currentStatus = requirement?.RequiredStatus ?? QuestStatus.Completed;

                var itemPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var checkbox = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = quest.Id
                };

                var checkLabel = new TextBlock
                {
                    Text = quest.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 8, 0),
                    Cursor = Cursors.Hand,
                    Width = 150
                };

                var statusLabel = new TextBlock
                {
                    Text = "Estado:",
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 4, 0),
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                var statusCombo = new ComboBox
                {
                    Width = 120,
                    Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                    Foreground = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x5A, 0x5A, 0x5A)),
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = quest.Id,
                    Visibility = isSelected ? Visibility.Visible : Visibility.Collapsed
                };

                // Poblar combo con estados traducidos
                foreach (var status in Enum.GetValues<QuestStatus>())
                {
                    var item = new ComboBoxItem
                    {
                        Content = QuestStatusTranslations[status],
                        Tag = status
                    };
                    statusCombo.Items.Add(item);
                    if (status == currentStatus)
                        statusCombo.SelectedItem = item;
                }

                checkLabel.MouseLeftButtonDown += (_, _) => checkbox.IsChecked = !checkbox.IsChecked;

                checkbox.Checked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject == null) return;
                        var requirements = prop.GetValue(_currentObject) as List<QuestRequirement> ?? new List<QuestRequirement>();
                        var questId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(questId) && !requirements.Any(r =>
                            r.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase)))
                        {
                            requirements.Add(new QuestRequirement { QuestId = questId, RequiredStatus = QuestStatus.Completed });
                            prop.SetValue(_currentObject, requirements);
                            PropertyEdited?.Invoke(_currentObject, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Requisitos de misión ({requirements.Count})";
                            summaryText.Text = GetRequiredQuestsSummary(requirements, allQuests);
                            statusCombo.SelectedIndex = (int)QuestStatus.Completed;
                            statusLabel.Visibility = Visibility.Visible;
                            statusCombo.Visibility = Visibility.Visible;
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                checkbox.Unchecked += (_, _) =>
                {
                    try
                    {
                        if (_currentObject == null) return;
                        var requirements = prop.GetValue(_currentObject) as List<QuestRequirement> ?? new List<QuestRequirement>();
                        var questId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(questId))
                        {
                            requirements.RemoveAll(r =>
                                r.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
                            prop.SetValue(_currentObject, requirements);
                            PropertyEdited?.Invoke(_currentObject, prop.Name);

                            // Actualizar UI
                            expander.Header = $"Requisitos de misión ({requirements.Count})";
                            summaryText.Text = GetRequiredQuestsSummary(requirements, allQuests);
                            statusLabel.Visibility = Visibility.Collapsed;
                            statusCombo.Visibility = Visibility.Collapsed;
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                statusCombo.SelectionChanged += (_, _) =>
                {
                    try
                    {
                        if (_currentObject == null) return;
                        var requirements = prop.GetValue(_currentObject) as List<QuestRequirement> ?? new List<QuestRequirement>();
                        var questId = statusCombo.Tag as string;
                        var req = requirements.FirstOrDefault(r =>
                            r.QuestId.Equals(questId, StringComparison.OrdinalIgnoreCase));
                        if (req != null && statusCombo.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is QuestStatus newStatus)
                        {
                            req.RequiredStatus = newStatus;
                            PropertyEdited?.Invoke(_currentObject, prop.Name);
                            summaryText.Text = GetRequiredQuestsSummary(requirements, allQuests);
                        }
                    }
                    catch { /* Ignorar errores */ }
                };

                itemPanel.Children.Add(checkbox);
                itemPanel.Children.Add(checkLabel);
                itemPanel.Children.Add(statusLabel);
                itemPanel.Children.Add(statusCombo);
                itemsPanel.Children.Add(itemPanel);
            }
        }

        // Wrap en ScrollViewer
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 250,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = itemsPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    /// <summary>
    /// Genera un resumen de los requisitos de misiones.
    /// </summary>
    private string GetRequiredQuestsSummary(List<QuestRequirement> requirements, List<QuestDefinition> allQuests)
    {
        if (requirements == null || !requirements.Any())
            return "(Sin requisitos)";

        var descriptions = requirements
            .Select(r =>
            {
                var quest = allQuests.FirstOrDefault(q =>
                    q.Id.Equals(r.QuestId, StringComparison.OrdinalIgnoreCase));
                var questName = quest?.Name ?? r.QuestId;
                var statusText = QuestStatusTranslations.GetValueOrDefault(r.RequiredStatus, r.RequiredStatus.ToString());
                return $"{questName}: {statusText}";
            })
            .ToList();

        return string.Join(", ", descriptions);
    }

    /// <summary>
    /// Crea un editor de receta de fabricación con selección de objetos y cantidades.
    /// </summary>
    private FrameworkElement CreateCraftingRecipeEditor(GameObject gameObject, PropertyInfo prop)
    {
        var currentRecipe = prop.GetValue(gameObject) as List<CraftingIngredient> ?? new List<CraftingIngredient>();
        var allObjects = GetObjects?.Invoke()?.ToList() ?? new List<GameObject>();

        var mainPanel = new StackPanel();

        // Etiqueta de resumen
        var summaryText = new TextBlock
        {
            Text = GetRecipeSummary(currentRecipe, allObjects),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con los ingredientes
        var expander = new Expander
        {
            Header = $"Se fabrica con ({currentRecipe.Count} ingredientes)",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var ingredientPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        foreach (var obj in allObjects.OrderBy(o => o.Name))
        {
            // No permitir que un objeto sea ingrediente de si mismo
            if (obj.Id.Equals(gameObject.Id, StringComparison.OrdinalIgnoreCase))
                continue;

            var existingIngredient = currentRecipe.FirstOrDefault(i =>
                i.ObjectId.Equals(obj.Id, StringComparison.OrdinalIgnoreCase));

            var rowPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            var checkbox = new CheckBox
            {
                IsChecked = existingIngredient != null,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = obj.Id
            };

            var label = new TextBlock
            {
                Text = obj.Name,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(6, 0, 8, 0),
                Width = 150
            };

            var quantityLabel = new TextBlock
            {
                Text = "x",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 4, 0)
            };

            var quantityBox = new TextBox
            {
                Text = existingIngredient?.Quantity.ToString() ?? "1",
                Width = 40,
                IsEnabled = existingIngredient != null,
                Tag = obj.Id,
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55))
            };

            // Eventos para checkbox
            checkbox.Checked += (_, _) =>
            {
                quantityBox.IsEnabled = true;
                if (!int.TryParse(quantityBox.Text, out int qty) || qty <= 0)
                {
                    quantityBox.Text = "1";
                    qty = 1;
                }
                UpdateCraftingRecipe(prop, gameObject, obj.Id, qty, true, summaryText, expander, allObjects);
            };

            checkbox.Unchecked += (_, _) =>
            {
                quantityBox.IsEnabled = false;
                UpdateCraftingRecipe(prop, gameObject, obj.Id, 0, false, summaryText, expander, allObjects);
            };

            // Evento para cambio de cantidad
            quantityBox.TextChanged += (_, _) =>
            {
                if (checkbox.IsChecked == true && int.TryParse(quantityBox.Text, out int qty) && qty > 0)
                {
                    UpdateCraftingRecipe(prop, gameObject, obj.Id, qty, true, summaryText, expander, allObjects);
                }
            };

            rowPanel.Children.Add(checkbox);
            rowPanel.Children.Add(label);
            rowPanel.Children.Add(quantityLabel);
            rowPanel.Children.Add(quantityBox);
            ingredientPanel.Children.Add(rowPanel);
        }

        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = ingredientPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    private void UpdateCraftingRecipe(PropertyInfo prop, GameObject gameObject, string objectId,
        int quantity, bool add, TextBlock summaryText, Expander expander, List<GameObject> allObjects)
    {
        if (_currentObject is not GameObject target) return;
        var recipe = prop.GetValue(target) as List<CraftingIngredient> ?? new List<CraftingIngredient>();

        // Eliminar ingrediente existente
        recipe.RemoveAll(i => i.ObjectId.Equals(objectId, StringComparison.OrdinalIgnoreCase));

        // Añadir si es necesario
        if (add && quantity > 0)
        {
            recipe.Add(new CraftingIngredient { ObjectId = objectId, Quantity = quantity });
        }

        prop.SetValue(target, recipe);
        PropertyEdited?.Invoke(target, prop.Name);

        expander.Header = $"Se fabrica con ({recipe.Count} ingredientes)";
        summaryText.Text = GetRecipeSummary(recipe, allObjects);
    }

    private static string GetRecipeSummary(List<CraftingIngredient> recipe, List<GameObject> allObjects)
    {
        if (!recipe.Any())
            return "(Sin receta - no se puede fabricar)";

        var parts = recipe.Select(i =>
        {
            var obj = allObjects.FirstOrDefault(o => o.Id.Equals(i.ObjectId, StringComparison.OrdinalIgnoreCase));
            var name = obj?.Name ?? i.ObjectId;
            return i.Quantity > 1 ? $"{i.Quantity}x {name}" : name;
        });

        return string.Join(" + ", parts);
    }

    /// <summary>
    /// Obtiene un resumen de los objetos seleccionados para mostrar en la etiqueta.
    /// </summary>
    private static string GetObjectsSummary(List<string> selectedIds, List<GameObject> allObjects)
    {
        if (!selectedIds.Any())
            return "(Ningún objeto seleccionado)";

        var names = selectedIds
            .Select(id => allObjects.FirstOrDefault(o => string.Equals(o.Id, id, StringComparison.OrdinalIgnoreCase)))
            .Where(o => o != null)
            .Select(o => o!.Name)
            .ToList();

        if (!names.Any())
            return "(Ningún objeto seleccionado)";

        return string.Join(", ", names);
    }

    /// <summary>
    /// Crea un selector múltiple de habilidades mágicas para NPC o PlayerDefinition.
    /// </summary>
    private FrameworkElement CreateMultiSelectAbilityPicker(object target, string elementKey)
    {
        List<string> currentList;
        Action<List<string>> setList;
        string propertyName = "AbilityIds";

        if (target is Npc npc)
        {
            currentList = npc.AbilityIds ?? new List<string>();
            setList = list => npc.AbilityIds = list;
        }
        else if (target is PlayerDefinition player)
        {
            currentList = player.AbilityIds ?? new List<string>();
            setList = list => player.AbilityIds = list;
        }
        else
        {
            return new TextBlock { Text = "(Tipo no soportado)" };
        }

        var allAbilities = GetAbilities?.Invoke()?.ToList() ?? new List<CombatAbility>();

        var mainPanel = new StackPanel();

        // Etiqueta descriptiva
        var descLabel = new TextBlock
        {
            Text = "Habilidades asignadas:",
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(descLabel);

        // Etiqueta que muestra el resumen de habilidades seleccionadas
        var summaryText = new TextBlock
        {
            Text = GetAbilitiesSummary(currentList, allAbilities),
            Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xAA, 0xAA)),
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 4)
        };
        mainPanel.Children.Add(summaryText);

        // Expander con los checkboxes
        var expander = new Expander
        {
            Header = $"Seleccionar habilidades ({currentList.Count})",
            IsExpanded = false,
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4)
        };

        var checkboxPanel = new StackPanel { Margin = new Thickness(8, 4, 4, 4) };

        if (!allAbilities.Any())
        {
            checkboxPanel.Children.Add(new TextBlock
            {
                Text = "(No hay habilidades definidas en el mundo)",
                Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                FontStyle = FontStyles.Italic
            });
        }
        else
        {
            foreach (var ability in allAbilities.OrderBy(a => a.Name))
            {
                var isSelected = currentList.Contains(ability.Id, StringComparer.OrdinalIgnoreCase);

                var checkPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 2, 0, 2)
                };

                var checkbox = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = ability.Id
                };

                var typeIcon = ability.AbilityType == AbilityType.Attack ? "⚔" : "🛡";
                var checkLabel = new TextBlock
                {
                    Text = $"{typeIcon} {ability.Name} ({ability.ManaCost} maná)",
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(6, 0, 0, 0),
                    Cursor = Cursors.Hand
                };

                checkLabel.MouseLeftButtonDown += (_, _) => checkbox.IsChecked = !checkbox.IsChecked;

                checkbox.Checked += (_, _) =>
                {
                    try
                    {
                        var list = target is Npc n ? (n.AbilityIds ?? new List<string>()) :
                                   target is PlayerDefinition p ? (p.AbilityIds ?? new List<string>()) :
                                   new List<string>();

                        var abilityId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(abilityId) && !list.Contains(abilityId, StringComparer.OrdinalIgnoreCase))
                        {
                            list.Add(abilityId);
                            setList(list);
                            PropertyEdited?.Invoke(target, propertyName);

                            // Actualizar UI
                            expander.Header = $"Seleccionar habilidades ({list.Count})";
                            summaryText.Text = GetAbilitiesSummary(list, allAbilities);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                checkbox.Unchecked += (_, _) =>
                {
                    try
                    {
                        var list = target is Npc n ? (n.AbilityIds ?? new List<string>()) :
                                   target is PlayerDefinition p ? (p.AbilityIds ?? new List<string>()) :
                                   new List<string>();

                        var abilityId = checkbox.Tag as string;
                        if (!string.IsNullOrEmpty(abilityId))
                        {
                            list.RemoveAll(id => string.Equals(id, abilityId, StringComparison.OrdinalIgnoreCase));
                            setList(list);
                            PropertyEdited?.Invoke(target, propertyName);

                            // Actualizar UI
                            expander.Header = $"Seleccionar habilidades ({list.Count})";
                            summaryText.Text = GetAbilitiesSummary(list, allAbilities);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                checkPanel.Children.Add(checkbox);
                checkPanel.Children.Add(checkLabel);
                checkboxPanel.Children.Add(checkPanel);
            }
        }

        // Wrap en ScrollViewer si hay muchas habilidades
        var scrollViewer = new ScrollViewer
        {
            MaxHeight = 200,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = checkboxPanel
        };

        expander.Content = scrollViewer;
        mainPanel.Children.Add(expander);

        return mainPanel;
    }

    /// <summary>
    /// Obtiene un resumen de las habilidades seleccionadas para mostrar en la etiqueta.
    /// </summary>
    private static string GetAbilitiesSummary(List<string> selectedIds, List<CombatAbility> allAbilities)
    {
        if (!selectedIds.Any())
            return "(Ninguna habilidad asignada)";

        var names = selectedIds
            .Select(id => allAbilities.FirstOrDefault(a => string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)))
            .Where(a => a != null)
            .Select(a => a!.Name)
            .ToList();

        if (!names.Any())
            return "(Ninguna habilidad asignada)";

        return string.Join(", ", names);
    }
}

/// <summary>
/// Item para el ComboBox de selección de llaves.
/// </summary>
internal class KeyComboItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de puertas.
/// </summary>
internal class DoorComboItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de música.
/// </summary>
internal class MusicComboItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de lado de apertura de puerta.
/// </summary>
internal class OpenFromSideComboItem
{
    public DoorOpenSide Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de género gramatical.
/// </summary>
internal class GenderComboItem
{
    public GrammaticalGender Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de modo de movimiento.
/// </summary>
internal class MovementModeComboItem
{
    public MovementMode Value { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Item para el ComboBox de selección de conversación.
/// </summary>
internal class ConversationComboItem
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// Tipo de slot de equipamiento.
/// </summary>
internal enum EquipmentSlot
{
    RightHand,
    LeftHand,
    Torso,
    Head
}
