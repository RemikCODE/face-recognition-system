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

        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            StatusLabel.Text = "⚠ Please enter a valid HTTP/HTTPS URL.";
            StatusLabel.TextColor = Colors.OrangeRed;
            StatusLabel.IsVisible = true;
            return;
        }

        _apiService.BaseUrl = url;
        StatusLabel.Text = "✅ Settings saved.";
        StatusLabel.TextColor = Color.FromArgb("#2e7d32");
        StatusLabel.IsVisible = true;
    }
}
