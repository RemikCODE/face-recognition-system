namespace FaceRecognitionApi.Models;

/// <summary>
/// Persisted record of a single face-recognition attempt.
/// Written by FaceRecognitionService after every /api/faces/recognize call.
/// Displayed on the web results dashboard (/).
/// </summary>
public class RecognitionLog
{
    public int Id { get; set; }
    public DateTime RecognizedAt { get; set; } = DateTime.UtcNow;
    public bool Found { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ImageFileName { get; set; } = string.Empty;
}
