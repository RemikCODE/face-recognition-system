namespace FaceRecognitionApp;

public partial class AppShell : Shell
{
    public AppShell(MainPage mainPage, HistoryPage historyPage, AddPersonPage addPersonPage)
    {
        InitializeComponent();

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

        tabBar.Items.Add(new ShellContent
        {
            Title = "Add Person",
            Content = addPersonPage,
        });

        Items.Add(tabBar);
    }
}
