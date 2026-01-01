using System.Windows;

namespace XiloAdventures.Wpf.Common.Windows;

public enum SaveChangesResult
{
    Save,
    DontSave,
    Cancel
}

public partial class SaveChangesWindow : Window
{
    public SaveChangesResult Result { get; private set; }

    public SaveChangesWindow(string message, string? saveButtonText = null, string? dontSaveButtonText = null, string? cancelButtonText = null)
    {
        InitializeComponent();
        MessageText.Text = message;
        Result = SaveChangesResult.Cancel;

        if (!string.IsNullOrEmpty(saveButtonText))
            SaveButton.Content = saveButtonText;
        if (!string.IsNullOrEmpty(dontSaveButtonText))
            DontSaveButton.Content = dontSaveButtonText;
        if (!string.IsNullOrEmpty(cancelButtonText))
            CancelButton.Content = cancelButtonText;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Result = SaveChangesResult.Save;
        DialogResult = true;
        Close();
    }

    private void DontSaveButton_Click(object sender, RoutedEventArgs e)
    {
        Result = SaveChangesResult.DontSave;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Result = SaveChangesResult.Cancel;
        DialogResult = false;
        Close();
    }
}
