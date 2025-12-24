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
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;
using XiloAdventures.Wpf.Common.Windows;
using XiloAdventures.Wpf.Windows;

namespace XiloAdventures.Wpf.Controls;

public partial class PropertyEditor : UserControl
{
    private PasswordBox? _encryptionPasswordBox;
    private object? _currentObject;

    // Diccionario para rastrear elementos que deben mostrarse/ocultarse dinámicamente
    private readonly Dictionary<string, UIElement> _propertyElements = new();
    // Diccionario para rastrear las condiciones de visibilidad de cada propiedad
    private readonly Dictionary<string, Func<bool>> _visibilityConditions = new();

    // Contador de secciones para determinar cuál debe estar expandida (la primera)
    private int _sectionIndex;

    // Lista de Expanders creados para expandir/contraer todo
    private readonly List<Expander> _expanders = new();

    // Diccionario para búsqueda: nombre de propiedad traducido → (elemento UI, Expander padre)
    private readonly Dictionary<string, (UIElement Element, Expander? ParentExpander)> _searchableElements = new();

    // TextBlock para mostrar el total de puntos de características del jugador
    private TextBlock? _attributesTotalLabel;

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

    public Func<IEnumerable<MusicAsset>>? GetMusics { get; set; }

    public Func<IEnumerable<GameObject>>? GetObjects { get; set; }

    public Func<IEnumerable<CombatAbility>>? GetAbilities { get; set; }

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


    public PropertyEditor()
    {
        InitializeComponent();
    }

    public object? GetCurrentObject()
    {
        return _currentObject;
    }

    public void SetObject(object? obj)
    {
        _currentObject = obj;
        _encryptionPasswordBox = null;
        _attributesTotalLabel = null;
        _sectionIndex = 0;
        RootPanel.Children.Clear();
        _propertyElements.Clear();
        _visibilityConditions.Clear();
        _expanders.Clear();
        _searchableElements.Clear();

        if (obj == null)
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
        var otrosGroup = groups.FirstOrDefault(g => g.Name == "🏷️ Otros");
        var mainGroups = groups.Where(g => g.Name != "🏷️ Otros").ToList();

        foreach (var group in mainGroups)
        {
            if (!group.Properties.Any())
                continue;

            // Crear contenido del acordeón
            var contentPanel = new StackPanel();

            // Si es PlayerDefinition y grupo Características, crear header especial con total de puntos
            object headerContent;
            if (obj is PlayerDefinition playerDef && group.Name == "⚔️ Características")
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

            // Propiedades del grupo
            foreach (var prop in group.Properties)
            {
                AddPropertyControlToPanel(obj, prop, contentPanel);
            }

            // Crear acordeón
            AddAccordionSection(headerContent, contentPanel, obj, group.Name);
        }

        // Añadir sección de estadísticas de combate para NPCs
        AddNpcStatsSection(obj);

        // Añadir sección de Sistemas para NPCs (Magia)
        AddNpcSystemsSection(obj);

        // Añadir sección de habilidades para PlayerDefinition
        AddPlayerAbilitiesSection(obj);

        // Añadir grupo "Otros" al final (si tiene propiedades)
        if (otrosGroup != null && otrosGroup.Properties.Any())
        {
            var contentPanel = new StackPanel();
            foreach (var prop in otrosGroup.Properties)
            {
                AddPropertyControlToPanel(obj, prop, contentPanel);
            }
            AddAccordionSection(otrosGroup.Name.ToUpper(), contentPanel, obj, otrosGroup.Name);
        }

        // Añadir botón de sinónimos para objetos, NPCs y puertas
        AddSynonymEditorButton(obj);
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
        bool isExpanded = ShouldSectionBeExpanded(obj, sectionName, _sectionIndex);

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
    /// Añade un botón para editar sinónimos del parser (solo para objetos, NPCs y puertas).
    /// </summary>
    private void AddSynonymEditorButton(object obj)
    {
        // Solo para objetos, NPCs y puertas
        if (obj is not GameObject and not Npc and not Door)
            return;

        // Verificar que tenemos acceso al diccionario
        if (GetParserDictionary == null || SetParserDictionary == null)
            return;

        // Crear contenido del acordeón
        var contentPanel = new StackPanel();

        // Descripción
        var description = new TextBlock
        {
            Text = "Define sinónimos para que el parser reconozca diferentes nombres para este elemento.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            Margin = new Thickness(0, 0, 0, 8)
        };
        contentPanel.Children.Add(description);

        // Botón
        var currentJson = GetParserDictionary();
        var hasContent = !string.IsNullOrWhiteSpace(currentJson);

        var panel = new StackPanel { Orientation = Orientation.Horizontal };

        var editButton = new Button
        {
            Content = "📖 Editar sinónimos...",
            Padding = new Thickness(12, 6, 12, 6),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Cursor = Cursors.Hand,
            Margin = new Thickness(0, 2, 0, 0)
        };

        var statusText = new TextBlock
        {
            Text = hasContent ? " ✓ Diccionario configurado" : "",
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
                statusText.Text = newHasContent ? " ✓ Diccionario configurado" : "";
            }
        };

        panel.Children.Add(editButton);
        panel.Children.Add(statusText);
        contentPanel.Children.Add(panel);

        // Crear acordeón
        AddAccordionSection("📖 SINÓNIMOS DEL PARSER", contentPanel);
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
            ("Level", "Nivel", 1, 100),
            ("Strength", "Fuerza", 1, 100),
            ("Dexterity", "Destreza", 1, 100),
            ("Intelligence", "Inteligencia", 1, 100),
            ("MaxHealth", "Vida máxima", 1, 1000),
            ("CurrentHealth", "Vida actual", 0, 1000),
            ("Gold", "Oro", 0, 100000)
        };

        foreach (var (propName, displayName, minVal, maxVal) in statsProperties)
        {
            AddNpcStatControlToPanel(npc, propName, displayName, minVal, maxVal, contentPanel);
        }

        // Crear acordeón
        AddAccordionSection("📊 ESTADÍSTICAS DE COMBATE", contentPanel);
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
        AddAccordionSection("🎮 SISTEMAS", contentPanel);
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
        AddAccordionSection("✨ HABILIDADES MÁGICAS", contentPanel);
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
        var groups = new Dictionary<string, List<PropertyInfo>>(StringComparer.OrdinalIgnoreCase)
        {
            ["🔖 Identificación"] = new(),
            ["📝 Descripción"] = new(),
            ["🎮 Sistemas"] = new(),
            ["🎵 Multimedia"] = new(),
            ["⚙️ Comportamiento"] = new(),
            ["⚔️ Combate"] = new(),
            ["📊 Estadísticas"] = new(),
            ["⚔️ Características"] = new(),
            ["🔒 Seguridad"] = new(),
            ["🏷️ Otros"] = new()
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
                groups["🏷️ Otros"].Add(prop);
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
            "🔖 Identificación",
            "📝 Descripción",
            "🎮 Sistemas",
            "🎵 Multimedia",
            "⚙️ Comportamiento",
            "⚔️ Combate",
            "📊 Estadísticas",
            "⚔️ Características",
            "🔒 Seguridad",
            "🏷️ Otros"
        };

        return orderedCategories
            .Where(cat => groups[cat].Any())
            .Select(cat => new PropertyGroup(cat, groups[cat]))
            .ToList();
    }

    private static string GetPropertyCategory(object obj, PropertyInfo prop)
    {
        var name = prop.Name;

        // Identificación
        if (name is "Id" or "Name" or "Title" or "Theme")
            return "🔖 Identificación";

        // Descripción
        if (name is "Description" or "Dialogue" or "TextContent")
            return "📝 Descripción";

        // Sistemas (Combate y Necesidades básicas)
        if (name is "CombatEnabled" or "MagicEnabled" or "BasicNeedsEnabled"
            or "HungerRate" or "ThirstRate" or "SleepRate"
            or "HungerDeathText" or "ThirstDeathText" or "SleepDeathText")
            return "🎮 Sistemas";

        // Multimedia
        if (name.Contains("Image") || name.Contains("Music"))
            return "🎵 Multimedia";

        // Salas (al final de Identificación)
        if (name is "RoomId" or "RoomIdA" or "RoomIdB" or "StartRoomId" or "TargetRoomId" or "Direction")
            return "🔖 Identificación";

        // Comportamiento (incluyendo propiedades de contenedor de GameObject que irán con sangría)
        if (name is "Visible" or "CanTake" or "Type" or "Gender" or "IsPlural" or "IsContainer" or "IsOpenable" or "IsOpen"
            or "IsLocked" or "ContentsVisible" or "IsIlluminated"
            or "IsInterior" or "StartHour" or "StartWeather" or "MinutesPerGameHour"
            or "RequiredQuestId" or "RequiredQuestStatus" or "OpenFromSide" or "EndingText"
            or "IsLightSource" or "IsLit" or "LightTurnsRemaining" or "CanExtinguish" or "CanIgnite" or "IgniterObjectId")
            return "⚙️ Comportamiento";

        // Propiedades de llave de Door
        if (obj is Door && name is "KeyObjectId")
            return "⚙️ Comportamiento";

        // Propiedades de conversación, comercio, patrulla y seguimiento de NPC
        if (obj is Npc && name is "ConversationId" or "IsShopkeeper" or "ShopInventory" or "BuyPriceMultiplier" or "SellPriceMultiplier"
            or "IsPatrolling" or "PatrolMovementMode" or "PatrolSpeed" or "PatrolTimeInterval"
            or "IsFollowingPlayer" or "FollowMovementMode" or "FollowSpeed" or "FollowTimeInterval")
            return "⚙️ Comportamiento";

        // Propiedades de contenedor de GameObject (se mostrarán con sangría dentro de Comportamiento)
        if (obj is GameObject && name is "ContainedObjectIds" or "KeyId" or "MaxCapacity")
            return "⚙️ Comportamiento";

        // Propiedades de combate de GameObject (armas y armaduras)
        if (obj is GameObject && name is "AttackBonus" or "DefenseBonus" or "DamageType"
            or "MaxDurability" or "CurrentDurability" or "InitiativeBonus")
            return "⚔️ Combate";

        // Otras propiedades de contenido que no son de GameObject
        if (name is "InventoryObjectIds" or "Objectives" or "KeyObjectId" or "DoorId" or "ObjectId")
            return "🏷️ Otros";

        // PlayerDefinition: propiedades físicas y económicas
        if (obj is PlayerDefinition && name is "Age" or "Weight" or "Height" or "InitialGold")
            return "📊 Estadísticas";

        // PlayerDefinition: características
        if (obj is PlayerDefinition && name is "Strength" or "Constitution" or "Intelligence" or "Dexterity" or "Charisma")
            return "⚔️ Características";

        // Estadísticas
        if (name is "Level" or "Strength" or "Dexterity" or "Intelligence" or "MaxHealth"
            or "CurrentHealth" or "Gold" or "Stats" or "Volume" or "Weight" or "Price")
            return "📊 Estadísticas";

        // Seguridad
        if (name is "EncryptionKey")
            return "🔒 Seguridad";

        // Tags
        if (name is "Tags")
            return "🏷️ Otros";

        // Parser Dictionary al final de Otros
        if (name is "ParserDictionaryJson")
            return "🏷️ Otros";

        return "🏷️ Otros";
    }

    private static int GetPropertyOrder(PropertyInfo prop)
    {
        // Orden de prioridad para propiedades dentro de su grupo
        return prop.Name switch
        {
            "Id" => 0,
            "Name" => 1,
            "Theme" => 1,
            "Title" => 2,
            "Description" => 0,
            "TextContent" => 1,
            "Dialogue" => 2,
            "ImageId" => 0,
            "ImageBase64" => 1,
            "MusicId" => 2,
            "WorldMusicId" => 3,
            // Propiedades de sala al final de Identificación
            "StartRoomId" => 100,
            "RoomId" => 101,
            "RoomIdA" => 102,
            "RoomIdB" => 103,
            "Direction" => 104,
            "TargetRoomId" => 105,

            // Texto de finalización al final de Comportamiento
            "EndingText" => 200,

            // Sistemas (Combate y Necesidades básicas)
            "CombatEnabled" => 0,
            "MagicEnabled" => 1,
            "BasicNeedsEnabled" => 10,
            "HungerRate" => 11,
            "ThirstRate" => 12,
            "SleepRate" => 13,
            "HungerDeathText" => 14,
            "ThirstDeathText" => 15,
            "SleepDeathText" => 16,

            // Parser Dictionary al final de Otros
            "ParserDictionaryJson" => 999,

            // Orden para propiedades de contenedor (GameObject)
            "Type" => 10,
            "CanTake" => 11,
            "Visible" => 12,
            "Gender" => 13,
            "IsPlural" => 14,
            "IsContainer" => 20,
            "IsOpenable" => 21,
            "IsOpen" => 22,
            "IsLocked" => 23,
            "KeyId" => 24,
            "ContentsVisible" => 25,
            "MaxCapacity" => 26,
            "ContainedObjectIds" => 27,

            // Orden para propiedades de iluminación (GameObject)
            "IsLightSource" => 50,
            "IsLit" => 51,
            "LightTurnsRemaining" => 52,
            "CanExtinguish" => 53,
            "CanIgnite" => 54,
            "IgniterObjectId" => 55,

            // Orden para propiedades de patrulla/seguimiento de NPC
            "IsPatrolling" => 30,
            "PatrolMovementMode" => 31,
            "PatrolSpeed" => 32,
            "PatrolTimeInterval" => 33,
            "IsFollowingPlayer" => 35,
            "FollowMovementMode" => 36,
            "FollowSpeed" => 37,
            "FollowTimeInterval" => 38,

            // Estadísticas
            "Volume" => 40,
            "Weight" => 41,
            "Price" => 42,

            _ => 99
        };
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
                    if (target is Npc npc && prop.Name == "IsFollowingPlayer" && npc.PatrolRoute.Count > 0)
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
                    if (target is Door door && prop.Name == "IsLocked" && !string.IsNullOrEmpty(door.KeyObjectId))
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
                    if (target is Door && prop.Name == "IsLocked")
                    {
                        SetObject(target);
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

            // Registrar el elemento para control de visibilidad
            var visibilityCondition = GetVisibilityCondition(obj, prop);
            if (visibilityCondition != null)
            {
                _propertyElements[prop.Name] = elementToAdd;
                _visibilityConditions[prop.Name] = visibilityCondition;

                // Aplicar visibilidad inicial
                elementToAdd.Visibility = visibilityCondition() ? Visibility.Visible : Visibility.Collapsed;
            }

            // Si es IsPatrolling de un NPC, añadir botón de editar ruta justo después
            if (obj is Npc npcForPatrolButton && prop.Name == "IsPatrolling")
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

                // Aplicar visibilidad inicial
                patrolButton.Visibility = npcForPatrolButton.IsPatrolling && !npcForPatrolButton.IsFollowingPlayer
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            // Si es MagicEnabled de GameInfo, añadir descripción y botón de habilidades
            if (obj is GameInfo gameInfoForMagic && prop.Name == "MagicEnabled")
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

        if (obj is Room && prop.Name == "MusicId" && prop.PropertyType == typeof(string))
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
        else if (obj is Room && prop.Name == "Description" && prop.PropertyType == typeof(string))
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
            // Selectores de sala (StartRoomId, RoomId, RoomIdA, RoomIdB)
            if (prop.PropertyType == typeof(string) && GetRooms != null &&
                (prop.Name == "StartRoomId" || prop.Name == "RoomId" || prop.Name == "RoomIdA" || prop.Name == "RoomIdB"))
            {
                var rooms = GetRooms().ToList();
                var combo = new ComboBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    DisplayMemberPath = "Name",
                    SelectedValuePath = "Id",
                    ItemsSource = rooms
                };

                if (obj is Door && (prop.Name == "RoomIdA" || prop.Name == "RoomIdB"))
                {
                    combo.IsEnabled = false;
                }

                // Si es un GameObject con RoomId, verificar si está contenido en otro objeto
                if (obj is GameObject gameObj && prop.Name == "RoomId")
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
                            if (target is GameObject containerObj && prop.Name == "RoomId")
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
                if (obj is Door door && prop.Name == "OpenFromSide" && GetRooms != null)
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
                else if ((obj is GameObject || obj is Door) && prop.Name == "Gender" && prop.PropertyType == typeof(GrammaticalGender))
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
                                if (prop.Name == "Type")
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
            else if (obj is GameInfo && prop.Name == "MinutesPerGameHour" && prop.PropertyType == typeof(int))
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
            // PlayerDefinition: Age (10-90 de 1 en 1)
            else if (obj is PlayerDefinition && prop.Name == "Age" && prop.PropertyType == typeof(int))
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
            else if (obj is PlayerDefinition && prop.Name == "Weight" && prop.PropertyType == typeof(int))
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
            else if (obj is PlayerDefinition && prop.Name == "Height" && prop.PropertyType == typeof(int))
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
            // PlayerDefinition: InitialGold (dinero inicial, mínimo 0, sin decimales)
            else if (obj is PlayerDefinition && prop.Name == "InitialGold" && prop.PropertyType == typeof(int))
            {
                var currentGold = prop.GetValue(obj) is int g ? g : 0;
                if (currentGold < 0) currentGold = 0;

                var tbGold = new TextBox
                {
                    Margin = new Thickness(0, 2, 0, 0),
                    Text = currentGold.ToString()
                };

                tbGold.PreviewTextInput += (_, e) =>
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

                tbGold.LostFocus += (_, _) =>
                {
                    try
                    {
                        if (_currentObject is not { } target) return;
                        if (int.TryParse(tbGold.Text, out var value))
                        {
                            if (value < 0) value = 0;
                            prop.SetValue(target, value);
                            tbGold.Text = value.ToString();
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                        else
                        {
                            // Si no es válido, restaurar a 0
                            prop.SetValue(target, 0);
                            tbGold.Text = "0";
                            PropertyEdited?.Invoke(target, prop.Name);
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                };

                editor = tbGold;
            }
            // PlayerDefinition: Características (Strength, Constitution, Intelligence, Dexterity, Charisma)
            else if (obj is PlayerDefinition playerDef &&
                     prop.Name is "Strength" or "Constitution" or "Intelligence" or "Dexterity" or "Charisma" &&
                     prop.PropertyType == typeof(int))
            {
                editor = CreateAttributeEditor(playerDef, prop);
            }
            // NPC: Velocidad de patrulla (1-3) con RadioButtons
            else if (obj is Npc npcForPatrolSpeed && prop.Name == "PatrolSpeed" && prop.PropertyType == typeof(int))
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
            else if (obj is Npc npcForFollowSpeed && prop.Name == "FollowSpeed" && prop.PropertyType == typeof(int))
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
            else if (obj is Npc npcForPatrolMode && prop.Name == "PatrolMovementMode")
            {
                editor = CreateMovementModeComboBox(npcForPatrolMode, prop, isPatrol: true);
            }
            // NPC: Intervalo de tiempo de patrulla con RadioButtons
            else if (obj is Npc npcForPatrolTime && prop.Name == "PatrolTimeInterval" && prop.PropertyType == typeof(float))
            {
                editor = CreateTimeIntervalRadioButtons(npcForPatrolTime, prop);
            }
            // NPC: Modo de movimiento de seguimiento (Turns/Time)
            else if (obj is Npc npcForFollowMode && prop.Name == "FollowMovementMode")
            {
                editor = CreateMovementModeComboBox(npcForFollowMode, prop, isPatrol: false);
            }
            // NPC: Intervalo de tiempo de seguimiento con RadioButtons
            else if (obj is Npc npcForFollowTime && prop.Name == "FollowTimeInterval" && prop.PropertyType == typeof(float))
            {
                editor = CreateTimeIntervalRadioButtons(npcForFollowTime, prop);
            }
            else if (obj is GameInfo gameInfoForParser && prop.Name == "ParserDictionaryJson" && prop.PropertyType == typeof(string))
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


            else if (prop.Name == "WorldMusicId" && prop.PropertyType == typeof(string))
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


            else if (prop.Name == "EndingMusicId" && prop.PropertyType == typeof(string))
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
            else if (prop.Name == "ImageId" && prop.PropertyType == typeof(string))
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


            // MusicId de Room: ComboBox con las músicas disponibles
            else if (prop.Name == "MusicId" && prop.PropertyType == typeof(string))
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
            // Selector múltiple de objetos para ShopInventory e InventoryObjectIds de NPC
            else if (obj is Npc npcForObjectList &&
                     prop.PropertyType == typeof(List<string>) &&
                     (prop.Name == "ShopInventory" || prop.Name == "InventoryObjectIds") &&
                     GetObjects != null)
            {
                editor = CreateMultiSelectObjectPicker(npcForObjectList, prop);
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
                    (prop.PropertyType == typeof(string) &&
                    string.Equals(prop.Name, "Description", StringComparison.OrdinalIgnoreCase) &&
                    (obj is Room || obj is GameObject || obj is Npc)) ||
                    (prop.PropertyType == typeof(string) &&
                    string.Equals(prop.Name, "TextContent", StringComparison.OrdinalIgnoreCase) &&
                    obj is GameObject);

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
                else
                {
                    var tb = new TextBox
                {
                    Text = text,
                    Margin = new Thickness(0, 2, 0, 0),
                    AcceptsReturn = isMultilineDescription,
                    TextWrapping = isMultilineDescription ? TextWrapping.Wrap : TextWrapping.NoWrap,
                    VerticalScrollBarVisibility = isMultilineDescription ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden,
                    MinHeight = isMultilineDescription ? 80 : 0
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

            // Registrar el elemento para control de visibilidad
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
        ["EncryptionKey"] = "Clave de cifrado",
        ["ImageBase64"] = "Imagen (Base64)",
        ["ImageId"] = "Imagen (id)",
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
        ["Tags"] = "Etiquetas",
        ["StartHour"] = "Hora inicial",
        ["StartWeather"] = "Clima inicial",
        ["RequiredQuestId"] = "Misión requerida",
        ["RequiredQuestStatus"] = "Estado de misión requerido",
        ["Visible"] = "Visible",
        ["CanTake"] = "Se puede coger",
        ["Type"] = "Tipo de objeto",
        ["TextContent"] = "Contenido de texto",
        ["Gender"] = "Género gramatical",
        ["IsPlural"] = "Es plural",
        ["IsContainer"] = "Es contenedor",
        ["IsOpenable"] = "Se puede abrir/cerrar",
        ["IsOpen"] = "Está abierto",
        ["IsLocked"] = "Está bloqueado",
        ["OpenFromSide"] = "Cerradura desde",
        ["ContentsVisible"] = "Contenido visible",
        ["MaxCapacity"] = "Capacidad máxima (cm³)",
        ["IsLightSource"] = "Es luminoso",
        ["IsLit"] = "Está encendido",
        ["LightTurnsRemaining"] = "Turnos de luz (-1 = infinito)",
        ["CanExtinguish"] = "Se puede apagar",
        ["CanIgnite"] = "Se puede encender",
        ["IgniterObjectId"] = "Objeto encendedor",
        ["Volume"] = "Volumen (cm³)",
        ["Weight"] = "Peso (g)",
        ["Price"] = "Precio",
        ["ContainedObjectIds"] = "Objetos contenidos",
        ["InventoryObjectIds"] = "Objetos en inventario",
        ["Dialogue"] = "Diálogo",
        ["Stats"] = "Estadísticas",
        ["Level"] = "Nivel",
        ["Strength"] = "Fuerza",
        ["Dexterity"] = "Destreza",
        ["Intelligence"] = "Inteligencia",
        ["MaxHealth"] = "Salud máxima",
        ["CurrentHealth"] = "Salud actual",
        ["Gold"] = "Oro",
        ["Objectives"] = "Objetivos",

        // Juego
        ["GameInfo.Title"] = "Título",
        ["GameInfo.Theme"] = "Temática",
        ["GameInfo.StartRoomId"] = "Sala inicial",
        ["GameInfo.MinutesPerGameHour"] = "Minutos por hora de juego",
        ["GameInfo.ParserDictionaryJson"] = "Diccionario del parser",
        ["GameInfo.StartHour"] = "Hora inicial",
        ["GameInfo.StartWeather"] = "Clima inicial",
        ["GameInfo.WorldMusicId"] = "Música global",
        ["GameInfo.EncryptionKey"] = "Clave de cifrado",
        ["GameInfo.EndingText"] = "Texto de finalización",
        ["GameInfo.EndingMusicId"] = "Música de finalización",
        ["GameInfo.TestModeAiEnabled"] = "IA en modo pruebas",
        ["GameInfo.TestModeSoundEnabled"] = "Sonido en modo pruebas",
        ["GameInfo.CombatEnabled"] = "Combate activo",
        ["GameInfo.MagicEnabled"] = "Magia activa",
        ["GameInfo.BasicNeedsEnabled"] = "Necesidades básicas activas",
        ["GameInfo.HungerRate"] = "Velocidad de hambre",
        ["GameInfo.ThirstRate"] = "Velocidad de sed",
        ["GameInfo.SleepRate"] = "Velocidad de sueño",
        ["GameInfo.HungerDeathText"] = "Texto de muerte por hambre",
        ["GameInfo.ThirstDeathText"] = "Texto de muerte por sed",
        ["GameInfo.SleepDeathText"] = "Texto de muerte por agotamiento",

        // Sala
        ["Room.Name"] = "Nombre",
        ["Room.Description"] = "Descripción",
        ["Room.ImageBase64"] = "Imagen (Base64)",
        ["Room.MusicId"] = "Música",
        ["Room.ImageId"] = "Imagen (id)",
        ["Room.RequiredQuestId"] = "Misión requerida",
        ["Room.RequiredQuestStatus"] = "Estado de misión requerido",
        ["Room.Tags"] = "Etiquetas",

        // Objeto
        ["GameObject.RoomId"] = "Sala",
        ["GameObject.CanTake"] = "Se puede coger",
        ["GameObject.Type"] = "Tipo",
        ["GameObject.IsContainer"] = "Es contenedor",
        ["GameObject.IsOpenable"] = "Se puede abrir/cerrar",
        ["GameObject.IsOpen"] = "Está abierto",
        ["GameObject.IsLocked"] = "Está bloqueado",
        ["GameObject.ContentsVisible"] = "Contenido visible",
        ["GameObject.MaxCapacity"] = "Capacidad máxima (cm³)",
        ["GameObject.Volume"] = "Volumen (cm³)",
        ["GameObject.Weight"] = "Peso (g)",
        ["GameObject.Price"] = "Precio",
        ["GameObject.ContainedObjectIds"] = "Objetos contenidos",
        ["GameObject.KeyId"] = "Llave necesaria",
        ["GameObject.Tags"] = "Etiquetas",
        ["GameObject.Visible"] = "Visible",
        ["GameObject.IsLightSource"] = "Es luminoso",
        ["GameObject.IsLit"] = "Está encendido",
        ["GameObject.LightTurnsRemaining"] = "Turnos de luz (-1 = infinito)",
        ["GameObject.CanExtinguish"] = "Se puede apagar",
        ["GameObject.CanIgnite"] = "Se puede encender",
        ["GameObject.IgniterObjectId"] = "Objeto encendedor",

        // NPC
        ["Npc.RoomId"] = "Sala",
        ["Npc.Dialogue"] = "Diálogo",
        ["Npc.InventoryObjectIds"] = "Objetos en inventario",
        ["Npc.Tags"] = "Etiquetas",
        ["Npc.Visible"] = "Visible",
        ["Npc.Stats"] = "Estadísticas",
        ["Npc.ConversationId"] = "Conversación",
        ["Npc.IsShopkeeper"] = "Es comerciante",
        ["Npc.ShopInventory"] = "Inventario de tienda",
        ["Npc.BuyPriceMultiplier"] = "Multiplicador compra",
        ["Npc.SellPriceMultiplier"] = "Multiplicador venta",
        ["Npc.IsPatrolling"] = "Está patrullando",
        ["Npc.PatrolSpeed"] = "Velocidad patrulla",
        ["Npc.IsFollowingPlayer"] = "Sigue al jugador",
        ["Npc.FollowSpeed"] = "Velocidad seguimiento",
        ["ConversationId"] = "Conversación",
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

        // Puerta
        ["Door.RoomIdA"] = "Sala A",
        ["Door.RoomIdB"] = "Sala B",
        ["Door.IsOpen"] = "Está abierta",
        ["Door.IsLocked"] = "Cerradura",
        ["Door.RequiredQuestId"] = "Misión requerida",
        ["Door.RequiredQuestStatus"] = "Estado de misión requerido",
        ["Door.Tags"] = "Etiquetas",

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
        ["PlayerDefinition.Age"] = "Edad (años)",
        ["PlayerDefinition.Weight"] = "Peso (kg)",
        ["PlayerDefinition.Height"] = "Altura (cm)",
        ["PlayerDefinition.Strength"] = "Fuerza",
        ["PlayerDefinition.Constitution"] = "Constitución",
        ["PlayerDefinition.Intelligence"] = "Inteligencia",
        ["PlayerDefinition.Dexterity"] = "Destreza",
        ["PlayerDefinition.Charisma"] = "Carisma",
        ["PlayerDefinition.InitialGold"] = "Dinero inicial",
        ["Constitution"] = "Constitución",
        ["Charisma"] = "Carisma",
        ["Age"] = "Edad",
        ["Height"] = "Altura",
        ["InitialGold"] = "Dinero inicial",
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
            // Validar y corregir InitialGold si es negativo
            if (playerDef.InitialGold < 0)
            {
                playerDef.InitialGold = 0;
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
    private void RootPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // Obtener el ScrollViewer padre
        var scrollViewer = MainScrollViewer;
        if (scrollViewer == null) return;

        // Calcular el nuevo offset (e.Delta es positivo cuando se hace scroll hacia arriba)
        var delta = e.Delta > 0 ? -50 : 50;
        scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta);

        e.Handled = true;
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
        }

        // Propiedades de patrulla/seguimiento de NPC (subpropiedades)
        if (obj is Npc)
        {
            if (name is "PatrolSpeed" or "FollowSpeed")
                return true;

            // Propiedades de tienda (subpropiedades de IsShopkeeper)
            if (name is "ShopInventory" or "BuyPriceMultiplier" or "SellPriceMultiplier")
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
            return name switch
            {
                // TextContent solo visible si Type = Texto
                "TextContent" => () => gameObject.Type == ObjectType.Texto,

                // IsOpen solo visible si IsContainer = true Y IsOpenable = true
                "IsOpen" => () => gameObject.IsContainer && gameObject.IsOpenable,

                // KeyId solo visible si IsContainer = true Y IsLocked = true
                "KeyId" => () => gameObject.IsContainer && gameObject.IsLocked,

                // Propiedades de contenedor solo visibles si IsContainer = true
                "IsOpenable" or "IsLocked" or "ContentsVisible" or "MaxCapacity" or "ContainedObjectIds"
                    => () => gameObject.IsContainer,

                // === PROPIEDADES DE COMBATE ===
                // AttackBonus solo visible si Type = Arma
                "AttackBonus" => () => gameObject.Type == ObjectType.Arma,

                // DefenseBonus solo visible si Type = Armadura
                "DefenseBonus" => () => gameObject.Type == ObjectType.Armadura,

                // DamageType solo visible si Type = Arma
                "DamageType" => () => gameObject.Type == ObjectType.Arma,

                // MaxDurability y CurrentDurability visibles si Type = Arma o Armadura
                "MaxDurability" or "CurrentDurability"
                    => () => gameObject.Type == ObjectType.Arma || gameObject.Type == ObjectType.Armadura,

                // InitiativeBonus visible si Type = Arma o Armadura
                "InitiativeBonus"
                    => () => gameObject.Type == ObjectType.Arma || gameObject.Type == ObjectType.Armadura,

                // === PROPIEDADES DE ILUMINACIÓN ===
                // IsLit solo visible si IsLightSource = true
                "IsLit" => () => gameObject.IsLightSource,

                // LightTurnsRemaining solo visible si IsLightSource = true
                "LightTurnsRemaining" => () => gameObject.IsLightSource,

                // CanExtinguish solo visible si IsLightSource = true
                "CanExtinguish" => () => gameObject.IsLightSource,

                // CanIgnite solo visible si IsLightSource = true
                "CanIgnite" => () => gameObject.IsLightSource,

                // IgniterObjectId solo visible si IsLightSource = true Y CanIgnite = true
                "IgniterObjectId" => () => gameObject.IsLightSource && gameObject.CanIgnite,

                _ => null
            };
        }

        // Condiciones para Room
        if (obj is Room room)
        {
            return name switch
            {
                // IsIlluminated solo visible si IsInterior = true
                "IsIlluminated" => () => room.IsInterior,

                _ => null
            };
        }

        // Condiciones para GameInfo
        if (obj is GameInfo gameInfo)
        {
            return name switch
            {
                // MagicEnabled solo visible si CombatEnabled = true
                "MagicEnabled" => () => gameInfo.CombatEnabled,

                // Propiedades de necesidades básicas solo visibles si BasicNeedsEnabled = true
                "HungerRate" or "ThirstRate" or "SleepRate"
                or "HungerDeathText" or "ThirstDeathText" or "SleepDeathText"
                    => () => gameInfo.BasicNeedsEnabled,

                _ => null
            };
        }

        // Condiciones para NPC
        if (obj is Npc npc)
        {
            return name switch
            {
                // IsPatrolling solo visible si NO está siguiendo al jugador
                "IsPatrolling" => () => !npc.IsFollowingPlayer,

                // PatrolMovementMode solo visible si está patrullando Y NO siguiendo
                "PatrolMovementMode" => () => npc.IsPatrolling && !npc.IsFollowingPlayer,

                // PatrolSpeed solo visible si está patrullando Y NO siguiendo Y modo Turns
                "PatrolSpeed" => () => npc.IsPatrolling && !npc.IsFollowingPlayer && npc.PatrolMovementMode == MovementMode.Turns,

                // PatrolTimeInterval solo visible si está patrullando Y NO siguiendo Y modo Time
                "PatrolTimeInterval" => () => npc.IsPatrolling && !npc.IsFollowingPlayer && npc.PatrolMovementMode == MovementMode.Time,

                // IsFollowingPlayer solo visible si NO está patrullando
                "IsFollowingPlayer" => () => !npc.IsPatrolling,

                // FollowMovementMode solo visible si está siguiendo Y NO patrullando
                "FollowMovementMode" => () => npc.IsFollowingPlayer && !npc.IsPatrolling,

                // FollowSpeed solo visible si está siguiendo Y NO patrullando Y modo Turns
                "FollowSpeed" => () => npc.IsFollowingPlayer && !npc.IsPatrolling && npc.FollowMovementMode == MovementMode.Turns,

                // FollowTimeInterval solo visible si está siguiendo Y NO patrullando Y modo Time
                "FollowTimeInterval" => () => npc.IsFollowingPlayer && !npc.IsPatrolling && npc.FollowMovementMode == MovementMode.Time,

                // Propiedades de tienda solo visibles si IsShopkeeper = true
                "ShopInventory" or "BuyPriceMultiplier" or "SellPriceMultiplier"
                    => () => npc.IsShopkeeper,

                _ => null
            };
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
    /// Cada característica tiene un mínimo de 10 y un máximo de 100.
    /// </summary>
    private FrameworkElement CreateAttributeEditor(PlayerDefinition playerDef, PropertyInfo prop)
    {
        const int MinValue = 10;
        const int MaxValue = 100;

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
            LargeChange = prop.Name == "FollowSpeed" ? 10 : 1,
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
                    checkLabel.Text = $"{obj.Name} ({obj.Price} 🪙)";
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
