using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Windows;

public partial class AbilityManagerWindow : Window
{
    private readonly WorldModel _world;
    private readonly ObservableCollection<AbilityViewModel> _abilities;
    private AbilityViewModel? _selectedAbility;
    private bool _isUpdating;

    public AbilityManagerWindow(WorldModel world)
    {
        InitializeComponent();

        _world = world;
        _abilities = new ObservableCollection<AbilityViewModel>(
            _world.Abilities.Select(a => new AbilityViewModel(a)));

        AbilityListBox.ItemsSource = _abilities;

        if (_abilities.Any())
            AbilityListBox.SelectedIndex = 0;
    }

    private void AddAbilityButton_Click(object sender, RoutedEventArgs e)
    {
        var newId = GenerateUniqueId("habilidad");
        var ability = new CombatAbility
        {
            Id = newId,
            Name = "Nueva Habilidad",
            Description = "",
            AbilityType = AbilityType.Attack,
            ManaCost = 10,
            AttackValue = 5,
            DefenseValue = 0,
            Damage = 0,
            Healing = 0
        };

        _world.Abilities.Add(ability);
        var vm = new AbilityViewModel(ability);
        _abilities.Add(vm);
        AbilityListBox.SelectedItem = vm;
    }

    private string GenerateUniqueId(string prefix)
    {
        var index = 1;
        string id;
        do
        {
            id = $"{prefix}_{index:D3}";
            index++;
        } while (_world.Abilities.Any(a => a.Id.Equals(id, StringComparison.OrdinalIgnoreCase)));
        return id;
    }

    private void AbilityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedAbility = AbilityListBox.SelectedItem as AbilityViewModel;

        if (_selectedAbility != null)
        {
            EditorPanel.Visibility = Visibility.Visible;
            NoSelectionText.Visibility = Visibility.Collapsed;
            LoadAbilityToEditor(_selectedAbility.Ability);
        }
        else
        {
            EditorPanel.Visibility = Visibility.Collapsed;
            NoSelectionText.Visibility = Visibility.Visible;
        }
    }

    private void LoadAbilityToEditor(CombatAbility ability)
    {
        _isUpdating = true;

        IdTextBox.Text = ability.Id;
        NameTextBox.Text = ability.Name;
        DescriptionTextBox.Text = ability.Description;

        TypeComboBox.SelectedIndex = ability.AbilityType == AbilityType.Attack ? 0 : 1;

        ManaCostTextBox.Text = ability.ManaCost.ToString();
        AttackValueTextBox.Text = ability.AttackValue.ToString();
        DefenseValueTextBox.Text = ability.DefenseValue.ToString();
        DamageTextBox.Text = ability.Damage.ToString();
        HealingTextBox.Text = ability.Healing.ToString();

        _isUpdating = false;
    }

    private void Field_Changed(object sender, EventArgs e)
    {
        if (_isUpdating || _selectedAbility == null) return;

        var ability = _selectedAbility.Ability;

        // Update ID (with validation)
        var newId = IdTextBox.Text.Trim();
        if (!string.IsNullOrEmpty(newId) && newId != ability.Id)
        {
            // Check for duplicates
            if (!_world.Abilities.Any(a => a != ability && a.Id.Equals(newId, StringComparison.OrdinalIgnoreCase)))
            {
                ability.Id = newId;
            }
        }

        ability.Name = NameTextBox.Text;
        ability.Description = DescriptionTextBox.Text;

        if (int.TryParse(ManaCostTextBox.Text, out var manaCost))
            ability.ManaCost = Math.Max(0, manaCost);

        if (int.TryParse(AttackValueTextBox.Text, out var attackValue))
            ability.AttackValue = attackValue;

        if (int.TryParse(DefenseValueTextBox.Text, out var defenseValue))
            ability.DefenseValue = defenseValue;

        if (int.TryParse(DamageTextBox.Text, out var damage))
            ability.Damage = Math.Max(0, damage);

        if (int.TryParse(HealingTextBox.Text, out var healing))
            ability.Healing = Math.Max(0, healing);

        // Refresh the list item
        _selectedAbility.Refresh();
    }

    private void TypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || _selectedAbility == null) return;

        var ability = _selectedAbility.Ability;

        if (TypeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string typeTag)
        {
            var previousType = ability.AbilityType;
            var newType = typeTag == "Attack" ? AbilityType.Attack : AbilityType.Defense;

            if (previousType != newType)
            {
                ability.AbilityType = newType;

                // Set default values based on type
                _isUpdating = true;
                if (newType == AbilityType.Attack)
                {
                    ability.AttackValue = 5;
                    ability.DefenseValue = 0;
                    AttackValueTextBox.Text = "5";
                    DefenseValueTextBox.Text = "0";
                }
                else
                {
                    ability.AttackValue = 0;
                    ability.DefenseValue = 5;
                    AttackValueTextBox.Text = "0";
                    DefenseValueTextBox.Text = "5";
                }
                _isUpdating = false;

                // Refresh the list item
                _selectedAbility.Refresh();
            }
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedAbility == null) return;

        var result = MessageBox.Show(
            $"¿Eliminar la habilidad '{_selectedAbility.Name}'?\n\nEsta acción no se puede deshacer.",
            "Confirmar eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        _world.Abilities.Remove(_selectedAbility.Ability);
        _abilities.Remove(_selectedAbility);

        if (_abilities.Any())
            AbilityListBox.SelectedIndex = 0;
    }
}

public class AbilityViewModel : System.ComponentModel.INotifyPropertyChanged
{
    public CombatAbility Ability { get; }

    public string Id => Ability.Id;
    public string Name => Ability.Name;
    public string TypeIcon => Ability.AbilityType == AbilityType.Attack ? "⚔" : "🛡";
    public string TypeAndCost => $"{(Ability.AbilityType == AbilityType.Attack ? "Ataque" : "Defensa")} - {Ability.ManaCost} maná";

    public AbilityViewModel(CombatAbility ability)
    {
        Ability = ability;
    }

    public void Refresh()
    {
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Name)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TypeIcon)));
        PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(TypeAndCost)));
    }

    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
}
