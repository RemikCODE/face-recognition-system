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
    ///   – http://localhost:5233 when running the Windows desktop build.
    ///   – 10.0.2.2  resolves to the host machine's localhost inside the Android emulator.
    ///   – Use the host machine's LAN IP when running on a real physical device.
    /// </summary>
#if WINDOWS || MACCATALYST
    private const string DefaultBaseUrl = "http://localhost:5233";
#else
    private const string DefaultBaseUrl = "http://10.0.2.2:5233";
#endif

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
        // Strip any path the user may have included (e.g. ".../recognize") so that
        // the relative path "api/faces/recognize" is always resolved correctly.
        var baseOnly = NormalizeBaseUrl(BaseUrl);
        if (_httpClient.BaseAddress?.ToString() != baseOnly)
            _httpClient.BaseAddress = new Uri(baseOnly);
    }

    /// <summary>
    /// Returns only the scheme + authority (host + port) part of <paramref name="url"/>,
    /// with a trailing slash. Any path component is intentionally discarded because
    /// <see cref="RecognizeAsync"/> always appends the full API path itself.
    /// </summary>
    internal static string NormalizeBaseUrl(string url)
    {
        if (Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed))
            return parsed.GetLeftPart(UriPartial.Authority) + "/";

        // Fallback: keep whatever was stored (will likely fail later with a clear error)
        return url.TrimEnd('/') + "/";
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
