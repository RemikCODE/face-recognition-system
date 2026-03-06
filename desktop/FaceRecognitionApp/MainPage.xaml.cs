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

        _isDesktop = DeviceInfo.Idiom == DeviceIdiom.Desktop;
        SelectFileButton.IsVisible = _isDesktop;
        TakePhotoButton.IsVisible = !_isDesktop;
        InstructionLabel.Text = _isDesktop
            ? "Select an image file to identify a person"
            : "Take a photo with the camera to identify a person";
    }

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
        PhotoPreview.IsVisible = true;
        PhotoPlaceholder.IsVisible = false;
        RecognizeButton.IsEnabled = true;
        ResultCard.IsVisible = false;
    }

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

            ResultStatusLabel.Text = result.Found ? "Face Recognized" : "Not Recognized";
            ResultStatusLabel.TextColor = result.Found
                ? Color.FromArgb("#3FB950")
                : Color.FromArgb("#F85149");
            ResultNameLabel.Text = result.Person?.Name ?? "—";
            ResultConfidenceLabel.Text = result.Found ? $"{result.Confidence * 100:F1}%" : "—";
            ResultMessageLabel.Text = result.Message;
            ResultCard.IsVisible = true;
        }
        catch (HttpRequestException ex)
        {
            await DisplayAlert("Connection Error",
                $"Cannot connect to the backend.\n\n{ex.Message}\n\nMake sure the backend server is running on port 5233.",
                "OK");
        }
        catch (TaskCanceledException)
        {
            await DisplayAlert("Timeout",
                "Face recognition timed out.\n\n" +
                "On the very first run the ML service needs to download model weights (~93 MB) " +
                "and build a face-embedding index for every photo in the dataset – " +
                "this can take several minutes depending on the size of the dataset and your hardware.\n\n" +
                "Please wait a moment and try again. Subsequent requests will be much faster.",
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
        LoadingPanel.IsVisible = loading;
        PhotoButtonsRow.IsVisible = !loading;
        RecognizeButton.IsEnabled = !loading && _photoBytes != null;
        RecognizeButton.Text = loading ? "Recognizing…" : "Identify Face";
    }
}
