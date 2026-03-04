using System.Net.Http.Headers;
using System.Text.Json;
using FaceRecognitionApp.Models;

namespace FaceRecognitionApp.Services;

/// <summary>
/// HTTP client that communicates with the ASP.NET backend.
/// The backend URL is fixed in code. To use a different host (e.g. a physical device
/// on the same network), change <see cref="BackendBaseUrl"/> and rebuild.
/// </summary>
public class ApiService
{
    private readonly HttpClient _httpClient;

    // Fixed backend URL – no runtime Settings page.
    // Windows / macOS desktop: backend runs on the same machine.
    // Android emulator:        10.0.2.2 maps to the host's localhost.
    // Real physical device:    change to your PC's LAN IP and rebuild, e.g. "http://192.168.1.42:5233/"
#if WINDOWS || MACCATALYST
    private const string BackendBaseUrl = "http://localhost:5233/";
#else
    private const string BackendBaseUrl = "http://10.0.2.2:5233/";
#endif

    // DeepFace inference can be slow – allow up to 10 minutes.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(10);

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(BackendBaseUrl);
        _httpClient.Timeout = RequestTimeout;
    }

    /// <summary>
    /// Uploads <paramref name="imageStream"/> to POST /api/faces/recognize and returns the result.
    /// </summary>
    public async Task<RecognitionResult?> RecognizeAsync(Stream imageStream, string fileName)
    {
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

    /// <summary>
    /// Returns recent recognition log entries from GET /api/recognitions.
    /// This is the same data shown on the backend web dashboard.
    /// </summary>
    public async Task<List<RecognitionLog>> GetRecentLogsAsync(int limit = 20)
    {
        var json = await _httpClient.GetStringAsync($"api/recognitions?limit={limit}");
        return JsonSerializer.Deserialize<List<RecognitionLog>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
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
