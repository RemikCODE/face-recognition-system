namespace FaceRecognitionApp;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage, HistoryPage historyPage)
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
            Title = "History",
            Content = historyPage,
        });

        Items.Add(tabBar);
    }
}
