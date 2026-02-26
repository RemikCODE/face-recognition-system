using System.Net.Http.Headers;
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

    private async Task<RecognitionResult> CallMlServiceAsync(Stream imageStream, string fileName, string mlServiceUrl)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var imageContent = new StreamContent(imageStream);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(GetMimeType(fileName));
            content.Add(imageContent, "image", fileName);

            var response = await _httpClient.PostAsync(mlServiceUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("ML service returned {Status}", response.StatusCode);
                return new RecognitionResult
                {
                    Found = false,
                    Message = $"ML service error: {response.StatusCode}",
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
                return new RecognitionResult
                {
                    Found = false,
                    Confidence = mlResult.Confidence,
                    Message = $"Recognized as '{recognizedName}' but not found in database.",
                };
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
            _logger.LogError(ex, "Error calling ML service");
            return new RecognitionResult
            {
                Found = false,
                Message = $"Error communicating with ML service: {ex.Message}",
            };
        }
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

    private class MlServiceResponse
    {
        public string Label { get; set; } = string.Empty;
        public double Confidence { get; set; }
    }
}
