namespace FaceRecognitionApp;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage, SettingsPage settingsPage)
    {
        InitializeComponent();

        // Build the tab bar using DI-resolved page instances
        var tabBar = new TabBar();

        tabBar.Items.Add(new ShellContent
        {
            Title = "Recognize",
            Content = mainPage,
        });

        tabBar.Items.Add(new ShellContent
        {
            Title = "Settings",
            Content = settingsPage,
        });

        Items.Add(tabBar);
    }
}
