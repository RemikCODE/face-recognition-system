using FaceRecognitionApp.Services;

namespace FaceRecognitionApp;

public partial class MainPage : ContentPage
{
    private readonly ApiService _apiService;
    private readonly bool _isDesktop;

    private byte[]? _photoBytes;
    private string _photoFileName = "photo.jpg";

    public MainPage(ApiService apiService)
    {
        InitializeComponent();
        _apiService = apiService;

        // Desktop (Windows/Mac): file picker only.
        // Mobile (Android/iOS): camera only.
        _isDesktop = DeviceInfo.Idiom == DeviceIdiom.Desktop;
        SelectFileButton.IsVisible = _isDesktop;
        TakePhotoButton.IsVisible = !_isDesktop;
        InstructionLabel.Text = _isDesktop
            ? "Select an image file from your computer to identify a person."
            : "Take a photo with the camera to identify a person.";
    }

    // ── File picker (desktop only) ───────────────────────────────────────────

    private async void OnSelectFileClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a face image",
                FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI,       new[] { ".jpg", ".jpeg", ".png", ".bmp" } },
                    { DevicePlatform.MacCatalyst, new[] { "public.image" } },
                    // Fallback for other platforms (shouldn't be reached)
                    { DevicePlatform.iOS,         new[] { "public.image" } },
                    { DevicePlatform.Android,     new[] { "image/*" } },
                })
            });

            if (result == null) return;

            _photoFileName = result.FileName;
            using var raw = await result.OpenReadAsync();
            using var ms = new MemoryStream();
            await raw.CopyToAsync(ms);
            _photoBytes = ms.ToArray();
            ShowPhotoPreview(_photoBytes);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    // ── Camera capture (mobile only) ────────────────────────────────────────

    private async void OnTakePhotoClicked(object sender, EventArgs e)
    {
        try
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await DisplayAlert("Unavailable", "Camera capture is not supported on this device.", "OK");
                return;
            }

            var photo = await MediaPicker.Default.CapturePhotoAsync();
            if (photo == null) return;

            _photoFileName = photo.FileName;
            using var raw = await photo.OpenReadAsync();
            using var ms = new MemoryStream();
            await raw.CopyToAsync(ms);
            _photoBytes = ms.ToArray();
            ShowPhotoPreview(_photoBytes);
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    private void ShowPhotoPreview(byte[] bytes)
    {
        PhotoPreview.Source = ImageSource.FromStream(() => new MemoryStream(bytes));
        RecognizeButton.IsEnabled = true;
        ResultCard.IsVisible = false;
    }

    // ── Recognition ─────────────────────────────────────────────────────────

    private async void OnRecognizeClicked(object sender, EventArgs e)
    {
        if (_photoBytes == null) return;

        SetLoading(true);
        ResultCard.IsVisible = false;

        try
        {
            using var imageStream = new MemoryStream(_photoBytes);
            var result = await _apiService.RecognizeAsync(imageStream, _photoFileName);

            if (result == null)
            {
                await DisplayAlert("Error", "No response received from the server.", "OK");
                return;
            }

            ResultTitleLabel.Text = result.Found ? "Face Recognized" : "Face Not Recognized";
            ResultTitleLabel.TextColor = result.Found ? Color.FromArgb("#2e7d32") : Color.FromArgb("#f57f17");
            ResultNameLabel.Text = result.Person?.Name ?? "—";
            ResultConfidenceLabel.Text = result.Found ? $"{result.Confidence * 100:F1}%" : "—";
            ResultMessageLabel.Text = result.Message;
            ResultCard.IsVisible = true;
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Connection Error",
                $"Cannot connect to the backend.\n\n{ex.Message}\n\nCheck the API URL in Settings.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void SetLoading(bool loading)
    {
        LoadingIndicator.IsRunning = loading;
        LoadingIndicator.IsVisible = loading;
        RecognizeButton.IsEnabled = !loading && _photoBytes != null;
    }
}
