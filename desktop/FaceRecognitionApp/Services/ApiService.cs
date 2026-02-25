using System.Net.Http.Headers;
using System.Text.Json;
using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

/// <summary>
/// HTTP client that communicates with the ASP.NET backend.
/// The base URL is persisted in device Preferences so users can change it
/// from the Settings page without rebuilding the app.
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;

    private const string BaseUrlKey = "ApiBaseUrl";

    /// <summary>
    /// Default base URL:
    ///   – 10.0.2.2  resolves to the host machine's localhost inside the Android emulator.
    ///   – Use the host machine's LAN IP when running on a real physical device.
    ///   – Use http://localhost:5233 when running the Windows desktop build.
    /// </summary>
    private const string DefaultBaseUrl = "http://10.0.2.2:5233";

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        EnsureBaseAddress();
    }

    /// <summary>Gets or sets the backend base URL, persisted across app restarts.</summary>
    public string BaseUrl
    {
        get => Preferences.Default.Get(BaseUrlKey, DefaultBaseUrl);
        set
        {
            Preferences.Default.Set(BaseUrlKey, value);
            EnsureBaseAddress();
        }
    }

    private void EnsureBaseAddress()
    {
        var url = BaseUrl.TrimEnd('/') + "/";
        if (_httpClient.BaseAddress?.ToString() != url)
            _httpClient.BaseAddress = new Uri(url);
    }

    /// <summary>
    /// Uploads <paramref name="imageStream"/> to POST /api/faces/recognize and returns the result.
    /// </summary>
    public async Task<RecognitionResult?> RecognizeAsync(Stream imageStream, string fileName)
    {
        EnsureBaseAddress();

        using var form = new MultipartFormDataContent();
        var imgContent = new StreamContent(imageStream);
        imgContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
        form.Add(imgContent, "image", fileName);

        var response = await _httpClient.PostAsync("api/faces/recognize", form);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<RecognitionResult>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".bmp"            => "image/bmp",
            _                 => "image/jpeg",
        };
}
