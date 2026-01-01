using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.Windows.Documents;
using System.Diagnostics;
using XiloAdventures.Wpf.Common.Ui;
using XiloAdventures.Wpf.Common.Services;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class OptionsWindow : Window
{
    private readonly UiSettings _settings;
    private readonly Action<UiSettings> _onChanged;
    private readonly string _worldId;
    private readonly string _defaultFontFamily;
    private readonly bool _saveToFile;

    public OptionsWindow(UiSettings settings, Action<UiSettings> onChanged, string worldId, string defaultFontFamily = "Segoe UI", bool saveToFile = true)
    {
        _settings = new UiSettings
        {
            SoundEnabled = settings.SoundEnabled,
            FontSize = settings.FontSize,
            FontFamily = settings.FontFamily,
            UseLlmForUnknownCommands = settings.UseLlmForUnknownCommands,
            MusicVolume = settings.MusicVolume,
            EffectsVolume = settings.EffectsVolume,
            MasterVolume = settings.MasterVolume,
            VoiceVolume = settings.VoiceVolume,
            MapEnabled = settings.MapEnabled
        };
        _onChanged = onChanged;
        _worldId = worldId;
        _defaultFontFamily = defaultFontFamily;
        _saveToFile = saveToFile;

        InitializeComponent();

        Loaded += OptionsWindow_Loaded;
    }

    private void OptionsWindow_Loaded(object sender, RoutedEventArgs e)
    {
        SoundCheckBox.IsChecked = _settings.SoundEnabled;

        FontSizeSlider.Value = _settings.FontSize;
        FontSizeLabel.Text = _settings.FontSize.ToString("0");

        MusicVolumeSlider.Value = _settings.MusicVolume;
        EffectsVolumeSlider.Value = _settings.EffectsVolume;
        MasterVolumeSlider.Value = _settings.MasterVolume;
        VoiceVolumeSlider.Value = _settings.VoiceVolume;

        MusicVolumeLabel.Text = _settings.MusicVolume.ToString("0");
        EffectsVolumeLabel.Text = _settings.EffectsVolume.ToString("0");
        MasterVolumeLabel.Text = _settings.MasterVolume.ToString("0");
        VoiceVolumeLabel.Text = _settings.VoiceVolume.ToString("0");

        var soundEnabled = _settings.SoundEnabled;
        MusicVolumeSlider.IsEnabled = soundEnabled;
        EffectsVolumeSlider.IsEnabled = soundEnabled;
        MasterVolumeSlider.IsEnabled = soundEnabled;
        VoiceVolumeSlider.IsEnabled = soundEnabled;

        SoundCheckBox.Checked += SoundCheckBox_Changed;
        SoundCheckBox.Unchecked += SoundCheckBox_Changed;

        // Poblar el ComboBox de fuentes
        var fonts = Fonts.SystemFontFamilies.OrderBy(f => f.Source).ToList();
        FontFamilyComboBox.ItemsSource = fonts;
        FontFamilyComboBox.DisplayMemberPath = "Source";

        // Seleccionar la fuente actual
        var currentFont = fonts.FirstOrDefault(f => f.Source == _settings.FontFamily);
        if (currentFont != null)
            FontFamilyComboBox.SelectedItem = currentFont;
        else
            FontFamilyComboBox.SelectedIndex = 0;
    }

    private void SoundCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        var enabled = SoundCheckBox.IsChecked == true;
        _settings.SoundEnabled = enabled;

        MusicVolumeSlider.IsEnabled = enabled;
        EffectsVolumeSlider.IsEnabled = enabled;
        MasterVolumeSlider.IsEnabled = enabled;
        VoiceVolumeSlider.IsEnabled = enabled;

        ApplyChanges();
    }

    private void MusicVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MusicVolumeLabel != null)
        {
            _settings.MusicVolume = e.NewValue;
            MusicVolumeLabel.Text = _settings.MusicVolume.ToString("0");
            ApplyChanges();
        }
    }

    private void EffectsVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (EffectsVolumeLabel != null)
        {
            _settings.EffectsVolume = e.NewValue;
            EffectsVolumeLabel.Text = _settings.EffectsVolume.ToString("0");
            ApplyChanges();
        }
    }

    private void VoiceVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (VoiceVolumeLabel != null)
        {
            _settings.VoiceVolume = e.NewValue;
            VoiceVolumeLabel.Text = _settings.VoiceVolume.ToString("0");
            ApplyChanges();
        }
    }

    private void MasterVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (MasterVolumeLabel != null)
        {
            _settings.MasterVolume = e.NewValue;
            MasterVolumeLabel.Text = _settings.MasterVolume.ToString("0");
            ApplyChanges();
        }
    }

    private void FontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (FontSizeLabel != null)
        {
            _settings.FontSize = e.NewValue;
            FontSizeLabel.Text = _settings.FontSize.ToString("0");
            ApplyChanges();
        }
    }

    private void FontFamilyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FontFamilyComboBox.SelectedItem is FontFamily selectedFont)
        {
            _settings.FontFamily = selectedFont.Source;
            ApplyChanges();
        }
    }

    private void ResetFontButton_Click(object sender, RoutedEventArgs e)
    {
        var fonts = FontFamilyComboBox.ItemsSource as System.Collections.Generic.List<FontFamily>;
        var defaultFont = fonts?.FirstOrDefault(f => f.Source == _defaultFontFamily);
        if (defaultFont != null)
        {
            FontFamilyComboBox.SelectedItem = defaultFont;
        }
    }

    private void ApplyChanges()
    {
        if (_saveToFile)
        {
            UiSettingsManager.SaveForWorld(_worldId, _settings);
        }
        _onChanged?.Invoke(_settings);
    }
}
