using System.Windows;

namespace XiloAdventures.Wpf.Windows;

public enum ChoiceResult
{
    Cancel,
    Option1,
    Option2
}

public partial class DarkChoiceDialog : Window
{
    public ChoiceResult Result { get; private set; } = ChoiceResult.Cancel;

    public DarkChoiceDialog(string titleBold, string titleNormal, string message, string option1Text, string option2Text, Window? owner = null)
    {
        InitializeComponent();

        TitleBold.Text = titleBold;
        TitleNormal.Text = titleNormal;
        MessageText.Text = message;
        Option1Button.Content = option1Text;
        Option2Button.Content = option2Text;

        if (owner != null)
        {
            Owner = owner;
        }
    }

    public static ChoiceResult Show(string titleBold, string titleNormal, string message, string option1Text, string option2Text, Window? owner = null)
    {
        var dialog = new DarkChoiceDialog(titleBold, titleNormal, message, option1Text, option2Text, owner);
        dialog.ShowDialog();
        return dialog.Result;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = ChoiceResult.Cancel;
        Close();
    }

    private void Option1Button_Click(object sender, RoutedEventArgs e)
    {
        Result = ChoiceResult.Option1;
        Close();
    }

    private void Option2Button_Click(object sender, RoutedEventArgs e)
    {
        Result = ChoiceResult.Option2;
        Close();
    }
}
