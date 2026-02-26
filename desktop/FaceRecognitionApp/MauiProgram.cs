using FaceRecognitionApp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace FaceRecognitionApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMaui()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // HTTP client and app services
        builder.Services.AddHttpClient<ApiService>();
        builder.Services.AddSingleton<ApiService>();

        // Pages (registered for DI constructor injection)
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
