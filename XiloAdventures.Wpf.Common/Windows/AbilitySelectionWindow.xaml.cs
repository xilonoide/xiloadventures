using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class AbilitySelectionWindow : Window
{
    public CombatAbility? SelectedAbility { get; private set; }

    public AbilitySelectionWindow(List<CombatAbility> abilities, int currentMana)
    {
        InitializeComponent();

        var attackAbilities = abilities.Where(a => a.AbilityType == AbilityType.Attack).ToList();
        var defenseAbilities = abilities.Where(a => a.AbilityType == AbilityType.Defense).ToList();

        PopulateAbilities(AttackAbilitiesPanel, attackAbilities, currentMana);
        PopulateAbilities(DefenseAbilitiesPanel, defenseAbilities, currentMana);

        if (!attackAbilities.Any())
        {
            AttackAbilitiesPanel.Children.Add(new TextBlock
            {
                Text = "No tienes habilidades de ataque",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(4)
            });
        }

        if (!defenseAbilities.Any())
        {
            DefenseAbilitiesPanel.Children.Add(new TextBlock
            {
                Text = "No tienes habilidades de defensa",
                Foreground = System.Windows.Media.Brushes.Gray,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(4)
            });
        }
    }

    private void PopulateAbilities(StackPanel panel, List<CombatAbility> abilities, int currentMana)
    {
        foreach (var ability in abilities)
        {
            var canUse = currentMana >= ability.ManaCost;

            var button = new Button
            {
                Tag = ability,
                IsEnabled = canUse,
                Style = (Style)FindResource("AbilityButtonStyle")
            };

            var content = new StackPanel();

            // Nombre y coste de mana
            var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
            headerPanel.Children.Add(new TextBlock
            {
                Text = ability.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = canUse
                    ? (ability.AbilityType == AbilityType.Attack
                        ? System.Windows.Media.Brushes.LightCoral
                        : System.Windows.Media.Brushes.LightGreen)
                    : System.Windows.Media.Brushes.Gray
            });
            headerPanel.Children.Add(new TextBlock
            {
                Text = $"  ({ability.ManaCost} maná)",
                FontSize = 12,
                Foreground = canUse ? System.Windows.Media.Brushes.CornflowerBlue : System.Windows.Media.Brushes.Gray,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 1)
            });
            content.Children.Add(headerPanel);

            // Stats de la habilidad
            var statsText = ability.AbilityType == AbilityType.Attack
                ? $"+{ability.AttackValue} Ataque"
                : $"+{ability.DefenseValue} Defensa";

            if (ability.Damage > 0)
                statsText += $", {ability.Damage} dano base";
            if (ability.Healing > 0)
                statsText += $", {ability.Healing} curacion";

            content.Children.Add(new TextBlock
            {
                Text = statsText,
                FontSize = 11,
                Foreground = System.Windows.Media.Brushes.Gold,
                Margin = new Thickness(0, 2, 0, 0)
            });

            // Descripcion
            if (!string.IsNullOrEmpty(ability.Description))
            {
                content.Children.Add(new TextBlock
                {
                    Text = ability.Description,
                    FontSize = 11,
                    Foreground = System.Windows.Media.Brushes.LightGray,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 2, 0, 0)
                });
            }

            button.Content = content;
            button.Click += Ability_Click;
            panel.Children.Add(button);
        }
    }

    private void Ability_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is CombatAbility ability)
        {
            SelectedAbility = ability;
            DialogResult = true;
            Close();
        }
    }
}
