using System.Windows;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class InputWindow : Window
{
    public string InputText { get; private set; }

    public InputWindow(string message, string title = "Entrada", string defaultValue = "")
    {
        InitializeComponent();
        MessageText.Text = message;
        Title = title;
        InputTextBox.Text = defaultValue;
        InputText = defaultValue;

        Loaded += (s, e) =>
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        };
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        InputText = InputTextBox.Text;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
