using FaceRecognitionApp.Services;

namespace FaceRecognitionApp;

public partial class SettingsPage : ContentPage
{
    private readonly ApiService _apiService;

    public SettingsPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;
        ApiUrlEntry.Text = _apiService.BaseUrl;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        var url = ApiUrlEntry.Text?.Trim();

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var parsed))
        {
            StatusLabel.Text = "⚠ Please enter a valid HTTP/HTTPS URL.";
            StatusLabel.TextColor = Colors.OrangeRed;
            StatusLabel.IsVisible = true;
            return;
        }

        // Strip any path the user may have typed (e.g. http://localhost:5001/recognize).
        // The app always calls /api/faces/recognize on its own – a path in the base URL
        // would result in a double-path like /recognize/api/faces/recognize (404).
        var sanitized = ApiService.NormalizeBaseUrl(url).TrimEnd('/');
        var hasExtraPath = parsed.AbsolutePath is not ("" or "/");

        _apiService.BaseUrl = sanitized;
        ApiUrlEntry.Text = sanitized;

        if (hasExtraPath)
        {
            StatusLabel.Text = $"⚠ Path removed – saved as: {sanitized}\n   (the app adds /api/faces/recognize automatically)";
            StatusLabel.TextColor = Colors.OrangeRed;
        }
        else
        {
            StatusLabel.Text = "✅ Settings saved.";
            StatusLabel.TextColor = Color.FromArgb("#2e7d32");
        }
        StatusLabel.IsVisible = true;
    }
}
