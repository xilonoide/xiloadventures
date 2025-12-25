using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using XiloAdventures.Engine;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class ParserDictionaryEditorWindow : Window
{
    public class DictionaryEntry
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    private readonly ObservableCollection<DictionaryEntry> _verbs = new();
    private readonly ObservableCollection<DictionaryEntry> _nouns = new();
    private readonly ObservableCollection<DictionaryEntry> _adjectives = new();
    private readonly ObservableCollection<string> _stopwords = new();
    private readonly bool _showVerbs;

    public string? ResultJson { get; private set; }

    /// <summary>
    /// Creates a parser dictionary editor.
    /// </summary>
    /// <param name="json">Current JSON dictionary value</param>
    /// <param name="showVerbs">If false, hides the Verbs tab (for object/NPC/door context)</param>
    public ParserDictionaryEditorWindow(string? json, bool showVerbs = true)
    {
        InitializeComponent();

        _showVerbs = showVerbs;

        VerbsDataGrid.ItemsSource = _verbs;
        NounsDataGrid.ItemsSource = _nouns;
        AdjectivesDataGrid.ItemsSource = _adjectives;
        StopwordsListBox.ItemsSource = _stopwords;

        LoadFromJson(json);
        LoadDefaultParserInfo();

        // Hide verbs tab if not needed (for object/NPC/door context)
        if (!showVerbs)
        {
            if (MainTabControl.Items[0] is TabItem verbsTab)
            {
                verbsTab.Visibility = Visibility.Collapsed;
                MainTabControl.SelectedIndex = 1; // Select nouns tab by default
            }
        }
    }

    private void LoadDefaultParserInfo()
    {
        // Cargar verbos por defecto
        var defaultVerbs = Parser.GetDefaultVerbs();
        var verbsBuilder = new StringBuilder();
        foreach (var kvp in defaultVerbs.OrderBy(v => v.Key))
        {
            var synonyms = kvp.Value.Where(s => !s.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
            if (synonyms.Count > 0)
            {
                verbsBuilder.Append(kvp.Key);
                verbsBuilder.Append(" → ");
                verbsBuilder.Append(string.Join(", ", synonyms.Take(8)));
                if (synonyms.Count > 8)
                    verbsBuilder.Append("...");
                verbsBuilder.AppendLine();
            }
        }
        DefaultVerbsText.Text = verbsBuilder.ToString().TrimEnd();

        // Cargar sustantivos por defecto
        var defaultNouns = Parser.GetDefaultNouns();
        var nounsBuilder = new StringBuilder();
        foreach (var kvp in defaultNouns.OrderBy(n => n.Key))
        {
            var synonyms = kvp.Value.Where(s => !s.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)).Distinct().ToList();
            if (synonyms.Count > 0)
            {
                nounsBuilder.Append(kvp.Key);
                nounsBuilder.Append(" → ");
                nounsBuilder.Append(string.Join(", ", synonyms));
                nounsBuilder.AppendLine();
            }
        }
        DefaultNounsText.Text = nounsBuilder.ToString().TrimEnd();

        // Cargar palabras ignoradas por defecto
        var ignoredWords = Parser.GetDefaultIgnoredWords();
        DefaultIgnoredText.Text = string.Join(", ", ignoredWords.OrderBy(w => w));
    }

    private void LoadFromJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Load verbs
            if (root.TryGetProperty("verbs", out var verbs))
            {
                foreach (var prop in verbs.EnumerateObject())
                {
                    var synonyms = prop.Value.EnumerateArray()
                        .Select(v => v.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s));
                    _verbs.Add(new DictionaryEntry
                    {
                        Key = prop.Name,
                        Value = string.Join(", ", synonyms)
                    });
                }
            }

            // Load nouns
            if (root.TryGetProperty("nouns", out var nouns))
            {
                foreach (var prop in nouns.EnumerateObject())
                {
                    var synonyms = prop.Value.EnumerateArray()
                        .Select(v => v.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s));
                    _nouns.Add(new DictionaryEntry
                    {
                        Key = prop.Name,
                        Value = string.Join(", ", synonyms)
                    });
                }
            }

            // Load adjectives
            if (root.TryGetProperty("adjectives", out var adjectives))
            {
                foreach (var prop in adjectives.EnumerateObject())
                {
                    var synonyms = prop.Value.EnumerateArray()
                        .Select(v => v.GetString() ?? "")
                        .Where(s => !string.IsNullOrEmpty(s));
                    _adjectives.Add(new DictionaryEntry
                    {
                        Key = prop.Name,
                        Value = string.Join(", ", synonyms)
                    });
                }
            }

            // Load stopwords
            if (root.TryGetProperty("stopwords", out var stopwords))
            {
                foreach (var word in stopwords.EnumerateArray())
                {
                    var w = word.GetString();
                    if (!string.IsNullOrEmpty(w))
                        _stopwords.Add(w);
                }
            }
        }
        catch
        {
            // Invalid JSON, start fresh
        }
    }

    private string BuildJson()
    {
        var options = new JsonWriterOptions { Indented = true };
        using var stream = new System.IO.MemoryStream();
        using var writer = new Utf8JsonWriter(stream, options);

        writer.WriteStartObject();

        // Write verbs
        if (_verbs.Count > 0)
        {
            writer.WriteStartObject("verbs");
            foreach (var entry in _verbs)
            {
                writer.WriteStartArray(entry.Key.Trim().ToLowerInvariant());
                foreach (var synonym in ParseSynonyms(entry.Value))
                {
                    writer.WriteStringValue(synonym);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        // Write nouns
        if (_nouns.Count > 0)
        {
            writer.WriteStartObject("nouns");
            foreach (var entry in _nouns)
            {
                writer.WriteStartArray(entry.Key.Trim().ToLowerInvariant());
                foreach (var synonym in ParseSynonyms(entry.Value))
                {
                    writer.WriteStringValue(synonym);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        // Write adjectives
        if (_adjectives.Count > 0)
        {
            writer.WriteStartObject("adjectives");
            foreach (var entry in _adjectives)
            {
                writer.WriteStartArray(entry.Key.Trim().ToLowerInvariant());
                foreach (var synonym in ParseSynonyms(entry.Value))
                {
                    writer.WriteStringValue(synonym);
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }

        // Write stopwords
        if (_stopwords.Count > 0)
        {
            writer.WriteStartArray("stopwords");
            foreach (var word in _stopwords)
            {
                writer.WriteStringValue(word.Trim().ToLowerInvariant());
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
        writer.Flush();

        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    private static IEnumerable<string> ParseSynonyms(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Enumerable.Empty<string>();

        return value.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrEmpty(s));
    }

    #region Entry Edit Dialog

    private (string? key, string? value) ShowEntryDialog(string title, string keyLabel, string valueLabel, string? currentKey = null, string? currentValue = null)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 450,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Foreground = Brushes.White,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow
        };

        var border = new Border
        {
            Margin = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Key label
        var keyLabelTb = new TextBlock
        {
            Text = keyLabel,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(keyLabelTb, 0);
        grid.Children.Add(keyLabelTb);

        // Key input
        var keyInput = new TextBox
        {
            Text = currentKey ?? "",
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(keyInput, 1);
        grid.Children.Add(keyInput);

        // Value label
        var valueLabelTb = new TextBlock
        {
            Text = valueLabel,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(valueLabelTb, 2);
        grid.Children.Add(valueLabelTb);

        // Value input
        var valueInput = new TextBox
        {
            Text = currentValue ?? "",
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(valueInput, 3);
        grid.Children.Add(valueInput);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(buttonPanel, 5);

        string? resultKey = null;
        string? resultValue = null;

        var okButton = new Button
        {
            Content = "✓ Aceptar",
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(keyInput.Text))
            {
                new AlertWindow("Debes introducir una palabra base.", "Error") { Owner = dialog }.ShowDialog();
                return;
            }
            resultKey = keyInput.Text.Trim();
            resultValue = valueInput.Text;
            dialog.DialogResult = true;
        };

        var cancelButton = new Button
        {
            Content = "✕ Cancelar",
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Cursor = Cursors.Hand,
            IsCancel = true
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        grid.Children.Add(buttonPanel);

        border.Child = grid;
        dialog.Content = border;

        keyInput.Focus();
        keyInput.SelectAll();

        if (dialog.ShowDialog() == true)
            return (resultKey, resultValue);

        return (null, null);
    }

    private string? ShowSingleInputDialog(string title, string label, string? currentValue = null)
    {
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
            Foreground = Brushes.White,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow
        };

        var border = new Border
        {
            Margin = new Thickness(12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            BorderThickness = new Thickness(1)
        };

        var grid = new Grid { Margin = new Thickness(16) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Label
        var labelTb = new TextBlock
        {
            Text = label,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        };
        Grid.SetRow(labelTb, 0);
        grid.Children.Add(labelTb);

        // Input
        var input = new TextBox
        {
            Text = currentValue ?? "",
            Background = new SolidColorBrush(Color.FromRgb(0x25, 0x25, 0x25)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
            Padding = new Thickness(8, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 8)
        };
        Grid.SetRow(input, 1);
        grid.Children.Add(input);

        // Buttons
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 8, 0, 0)
        };
        Grid.SetRow(buttonPanel, 3);

        string? result = null;

        var okButton = new Button
        {
            Content = "✓ Aceptar",
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Margin = new Thickness(0, 0, 8, 0),
            Cursor = Cursors.Hand
        };
        okButton.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text))
            {
                new AlertWindow("Debes introducir un valor.", "Error") { Owner = dialog }.ShowDialog();
                return;
            }
            result = input.Text.Trim();
            dialog.DialogResult = true;
        };

        var cancelButton = new Button
        {
            Content = "✕ Cancelar",
            Padding = new Thickness(16, 8, 16, 8),
            Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x3A, 0x3A)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x4A, 0x4A, 0x4A)),
            Cursor = Cursors.Hand,
            IsCancel = true
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;

        buttonPanel.Children.Add(okButton);
        buttonPanel.Children.Add(cancelButton);
        grid.Children.Add(buttonPanel);

        border.Child = grid;
        dialog.Content = border;

        input.Focus();
        input.SelectAll();

        if (dialog.ShowDialog() == true)
            return result;

        return null;
    }

    #endregion

    #region Verbs

    private void AddVerb_Click(object sender, RoutedEventArgs e)
    {
        var (key, value) = ShowEntryDialog("Añadir verbo", "Verbo base:", "Sinónimos (separados por comas):");
        if (key != null)
        {
            if (_verbs.Any(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El verbo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            _verbs.Add(new DictionaryEntry { Key = key, Value = value ?? "" });
        }
    }

    private void EditVerb_Click(object sender, RoutedEventArgs e)
    {
        if (VerbsDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un verbo para editar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var (key, value) = ShowEntryDialog("Editar verbo", "Verbo base:", "Sinónimos (separados por comas):", entry.Key, entry.Value);
        if (key != null)
        {
            if (!key.Equals(entry.Key, StringComparison.OrdinalIgnoreCase) &&
                _verbs.Any(v => v.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El verbo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            entry.Key = key;
            entry.Value = value ?? "";
            VerbsDataGrid.Items.Refresh();
        }
    }

    private void DeleteVerb_Click(object sender, RoutedEventArgs e)
    {
        if (VerbsDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un verbo para eliminar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var confirm = new ConfirmWindow($"¿Eliminar el verbo '{entry.Key}'?", "Confirmar") { Owner = this };
        if (confirm.ShowDialog() == true)
        {
            _verbs.Remove(entry);
        }
    }

    #endregion

    #region Nouns

    private void AddNoun_Click(object sender, RoutedEventArgs e)
    {
        var (key, value) = ShowEntryDialog("Añadir sustantivo", "Sustantivo base:", "Sinónimos (separados por comas):");
        if (key != null)
        {
            if (_nouns.Any(n => n.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El sustantivo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            _nouns.Add(new DictionaryEntry { Key = key, Value = value ?? "" });
        }
    }

    private void EditNoun_Click(object sender, RoutedEventArgs e)
    {
        if (NounsDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un sustantivo para editar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var (key, value) = ShowEntryDialog("Editar sustantivo", "Sustantivo base:", "Sinónimos (separados por comas):", entry.Key, entry.Value);
        if (key != null)
        {
            if (!key.Equals(entry.Key, StringComparison.OrdinalIgnoreCase) &&
                _nouns.Any(n => n.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El sustantivo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            entry.Key = key;
            entry.Value = value ?? "";
            NounsDataGrid.Items.Refresh();
        }
    }

    private void DeleteNoun_Click(object sender, RoutedEventArgs e)
    {
        if (NounsDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un sustantivo para eliminar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var confirm = new ConfirmWindow($"¿Eliminar el sustantivo '{entry.Key}'?", "Confirmar") { Owner = this };
        if (confirm.ShowDialog() == true)
        {
            _nouns.Remove(entry);
        }
    }

    #endregion

    #region Adjectives

    private void AddAdjective_Click(object sender, RoutedEventArgs e)
    {
        var (key, value) = ShowEntryDialog("Añadir adjetivo", "Adjetivo base:", "Sinónimos (separados por comas):");
        if (key != null)
        {
            if (_adjectives.Any(a => a.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El adjetivo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            _adjectives.Add(new DictionaryEntry { Key = key, Value = value ?? "" });
        }
    }

    private void EditAdjective_Click(object sender, RoutedEventArgs e)
    {
        if (AdjectivesDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un adjetivo para editar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var (key, value) = ShowEntryDialog("Editar adjetivo", "Adjetivo base:", "Sinónimos (separados por comas):", entry.Key, entry.Value);
        if (key != null)
        {
            if (!key.Equals(entry.Key, StringComparison.OrdinalIgnoreCase) &&
                _adjectives.Any(a => a.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"El adjetivo '{key}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            entry.Key = key;
            entry.Value = value ?? "";
            AdjectivesDataGrid.Items.Refresh();
        }
    }

    private void DeleteAdjective_Click(object sender, RoutedEventArgs e)
    {
        if (AdjectivesDataGrid.SelectedItem is not DictionaryEntry entry)
        {
            new AlertWindow("Selecciona un adjetivo para eliminar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var confirm = new ConfirmWindow($"¿Eliminar el adjetivo '{entry.Key}'?", "Confirmar") { Owner = this };
        if (confirm.ShowDialog() == true)
        {
            _adjectives.Remove(entry);
        }
    }

    #endregion

    #region Stopwords

    private void AddStopword_Click(object sender, RoutedEventArgs e)
    {
        var word = ShowSingleInputDialog("Añadir palabra a ignorar", "Palabra:");
        if (word != null)
        {
            if (_stopwords.Any(s => s.Equals(word, StringComparison.OrdinalIgnoreCase)))
            {
                new AlertWindow($"La palabra '{word}' ya existe.", "Error") { Owner = this }.ShowDialog();
                return;
            }
            _stopwords.Add(word.ToLowerInvariant());
        }
    }

    private void DeleteStopword_Click(object sender, RoutedEventArgs e)
    {
        if (StopwordsListBox.SelectedItem is not string word)
        {
            new AlertWindow("Selecciona una palabra para eliminar.", "Aviso") { Owner = this }.ShowDialog();
            return;
        }

        var confirm = new ConfirmWindow($"¿Eliminar '{word}' de las palabras ignoradas?", "Confirmar") { Owner = this };
        if (confirm.ShowDialog() == true)
        {
            _stopwords.Remove(word);
        }
    }

    #endregion

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        // Only generate JSON if there's actual content
        if (_verbs.Count == 0 && _nouns.Count == 0 && _adjectives.Count == 0 && _stopwords.Count == 0)
        {
            ResultJson = null;
        }
        else
        {
            ResultJson = BuildJson();
        }
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
