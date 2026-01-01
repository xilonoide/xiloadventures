using System.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class DirectionPickerWindow : Window
{
    public string? SelectedDirection { get; private set; }

    public DirectionPickerWindow()
    {
        InitializeComponent();
        DirectionCombo.SelectedIndex = 0;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (DirectionCombo.SelectedItem is System.Windows.Controls.ComboBoxItem item)
            SelectedDirection = item.Content?.ToString();

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
