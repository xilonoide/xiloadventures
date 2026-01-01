using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

namespace XiloAdventures.Wpf.Common.Windows;

public partial class AboutWindow : Window
{
    private const string DonateUrl = "https://www.paypal.me/xmasmusicsoft";
    private const string GitHubUrl = "https://github.com/xilonoide/XiloAdventures";

    public AboutWindow()
    {
        InitializeComponent();

        var version = Assembly.GetEntryAssembly()?.GetName().Version
                   ?? Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = $"Version {version?.Major}.{version?.Minor}.{version?.Build}";
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void DonateLink_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = DonateUrl,
            UseShellExecute = true
        });
    }

    private void GitHubLink_Click(object sender, MouseButtonEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = GitHubUrl,
            UseShellExecute = true
        });
    }
}
