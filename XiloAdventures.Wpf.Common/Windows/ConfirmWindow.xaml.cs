using System.Windows;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class ConfirmWindow : Window
{
    public ConfirmWindow(string message, string title)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
