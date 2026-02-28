namespace FaceRecognitionApp.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();

        // Surface unhandled exceptions as a dialog instead of a silent crash.
        this.UnhandledException += OnUnhandledException;
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

    private static async void OnUnhandledException(object sender,
        Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        var xamlRoot = (Microsoft.Maui.Controls.Application.Current?.Windows.FirstOrDefault()?.Handler?.PlatformView as Microsoft.UI.Xaml.Window)?.Content?.XamlRoot;
        if (xamlRoot is null) return;

        try
        {
            await new Microsoft.UI.Xaml.Controls.ContentDialog
            {
                Title = "Unexpected Error",
                Content = e.Exception?.Message ?? e.Message,
                CloseButtonText = "OK",
                XamlRoot = xamlRoot,
            }.ShowAsync();
        }
        catch { /* dialog itself failed; nothing more we can do */ }
    }
}
