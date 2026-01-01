using System.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class RenameRoomWindow : Window
{
    public string RoomName => NameTextBox.Text;

    public RenameRoomWindow(string currentName)
    {
        InitializeComponent();
        NameTextBox.Text = currentName ?? string.Empty;
        NameTextBox.SelectAll();
        Loaded += (_, _) => NameTextBox.Focus();
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
