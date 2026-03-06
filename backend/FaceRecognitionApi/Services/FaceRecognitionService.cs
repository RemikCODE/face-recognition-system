using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FaceRecognitionApi.Data;
using FaceRecognitionApi.Models;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionApi.Services;

/// <summary>
/// Delegates face recognition to a Python ML microservice.
/// The ML service is expected to accept a multipart/form-data POST with the image
/// and return JSON: { "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }
/// If the ML service URL is not configured, the service returns a not-configured result.
/// Every result is persisted to RecognitionLogs for the web results dashboard.
/// </summary>
public class FaceRecognitionService : IFaceRecognitionService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<FaceRecognitionService> _logger;

    public FaceRecognitionService(
        AppDbContext db,
        HttpClient httpClient,
        IConfiguration config,
        ILogger<FaceRecognitionService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _config = config;
        _logger = logger;
    }

    public async Task<RecognitionResult> RecognizeAsync(Stream imageStream, string fileName)
    {
        RecognitionResult result;
        var mlServiceUrl = _config["MlService:Url"];

        if (string.IsNullOrWhiteSpace(mlServiceUrl))
        {
            _logger.LogWarning("ML service URL is not configured. Set 'MlService:Url' in appsettings.");
            result = new RecognitionResult
            {
                Found = false,
                Message = "Face recognition ML service is not configured.",
            };
        }
        else
        {
            result = await CallMlServiceAsync(imageStream, fileName, mlServiceUrl);
        }

        await SaveLogAsync(result, fileName);
        return result;
    }

    // Number of times to retry when the ML service responds with 503 (model still loading).
    // Warmup can take several minutes on the first run (model download + building .pkl embeddings
    // for the whole dataset). 30 retries × 10 s = up to 5 minutes of patient waiting, after
    // which the user sees a clear error instead of having to press the button repeatedly.
    private const int MlServiceMaxRetries = 30;

    // Delay between retries (seconds).
    private const int MlServiceRetryDelaySeconds = 10;

    private async Task<RecognitionResult> CallMlServiceAsync(Stream imageStream, string fileName, string mlServiceUrl)
    {
        // Buffer the entire image into memory so the request body can be
        // resent on each retry attempt (a Stream can only be read once).
        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await imageStream.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        for (int attempt = 1; attempt <= MlServiceMaxRetries; attempt++)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                // MemoryStream is owned and disposed by imageContent -> content (MultipartFormDataContent).
                var imageContent = new StreamContent(new MemoryStream(imageBytes));
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
                content.Add(imageContent, "image", fileName);

                var response = await _httpClient.PostAsync(mlServiceUrl, content);

                // 503 means the ML service is still warming up (loading model weights).
                // Retry after a short delay so the user doesn't have to press the button again.
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable && attempt < MlServiceMaxRetries)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation(
                        "ML service not ready (attempt {Attempt}/{Max}), retrying in {Delay}s. Response: {Body}",
                        attempt, MlServiceMaxRetries, MlServiceRetryDelaySeconds, body);
                    await Task.Delay(TimeSpan.FromSeconds(MlServiceRetryDelaySeconds));
                    continue;
                }

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    var errorMessage = ExtractMlErrorMessage(body) ?? response.StatusCode.ToString();
                    _logger.LogWarning("ML service returned {Status}. Body: {Body}", response.StatusCode, body);
                    return new RecognitionResult
                    {
                        Found = false,
                        Message = $"ML service error: {errorMessage}",
                    };
                }

                var mlResult = await response.Content.ReadFromJsonAsync<MlServiceResponse>();

                if (mlResult == null || string.IsNullOrWhiteSpace(mlResult.Label))
                {
                    return new RecognitionResult { Found = false, Message = "No face recognized." };
                }

                var recognizedName = CsvImportService.ExtractName(mlResult.Label);

                var person = await _db.Persons
                    .Where(p => p.ImageFileName == mlResult.Label || p.Name == recognizedName)
                    .FirstOrDefaultAsync();

                if (person == null)
                {
                    // ML recognized the person but they are not yet in the database
                    // (e.g. the Persons table was never seeded from CSV).
                    // Auto-insert so future lookups succeed without manual seeding.
                    _logger.LogInformation(
                        "Person '{Name}' (label: {Label}) not found in DB – auto-inserting.",
                        recognizedName, mlResult.Label);

                    person = new Person
                    {
                        Name = recognizedName,
                        ImageFileName = mlResult.Label,
                    };
                    _db.Persons.Add(person);
                    await _db.SaveChangesAsync();
                }

                return new RecognitionResult
                {
                    Found = true,
                    Person = person,
                    Confidence = mlResult.Confidence,
                    Message = "Face recognized successfully.",
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling ML service (attempt {Attempt}/{Max})", attempt, MlServiceMaxRetries);
                if (attempt < MlServiceMaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(MlServiceRetryDelaySeconds));
                    continue;
                }

                return new RecognitionResult
                {
                    Found = false,
                    Message = $"Error communicating with ML service: {ex.Message}",
                };
            }
        }

        // Should never be reached: the loop always returns or throws on the final attempt.
        throw new InvalidOperationException("Unexpected exit from ML service retry loop.");
    }

    /// <summary>
    /// Tries to extract the "error" field from an ML service JSON error body.
    /// Returns <c>null</c> if the body cannot be parsed.
    /// </summary>
    private static string? ExtractMlErrorMessage(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var prop))
                return prop.GetString();
        }
        catch (JsonException)
        {
            // Body was not valid JSON – return null so the caller falls back to the status code.
        }
        return null;
    }

    private async Task SaveLogAsync(RecognitionResult result, string sourceFileName)
    {
        _db.RecognitionLogs.Add(new RecognitionLog
        {
            RecognizedAt = DateTime.UtcNow,
            Found = result.Found,
            PersonName = result.Person?.Name ?? string.Empty,
            Confidence = result.Confidence,
            Message = result.Message,
            ImageFileName = sourceFileName,
        });
        await _db.SaveChangesAsync();
    }

    private static string GetMimeType(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".gif" => "image/gif",
            _ => "application/octet-stream",
        };
    }

    /// <summary>
    /// Derives the ML service base URL (scheme + host + port) from the configured
    /// recognize URL, e.g. "http://localhost:5001/recognize" → "http://localhost:5001".
    /// </summary>
    private string? GetMlServiceBaseUrl()
    {
        var url = _config["MlService:Url"];
        if (string.IsNullOrWhiteSpace(url)) return null;
        return new Uri(url).GetLeftPart(UriPartial.Authority);
    }

    /// <inheritdoc/>
    public async Task<string?> AddPersonAsync(string name, Stream imageStream, string fileName)
    {
        var baseUrl = GetMlServiceBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("ML service URL is not configured. Set 'MlService:Url' in appsettings.");
            return null;
        }

        byte[] imageBytes;
        using (var ms = new MemoryStream())
        {
            await imageStream.CopyToAsync(ms);
            imageBytes = ms.ToArray();
        }

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(name), "name");
        var imageContent = new StreamContent(new MemoryStream(imageBytes));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
        content.Add(imageContent, "image", fileName);

        var response = await _httpClient.PostAsync($"{baseUrl}/add-person", content);
        response.EnsureSuccessStatusCode();

        var doc = await response.Content.ReadFromJsonAsync<AddPersonResponse>();
        return doc?.Filename;
    }

    private class MlServiceResponse
    {
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }

    private class AddPersonResponse
    {
        public string Filename { get; set; } = string.Empty;
    }
}
