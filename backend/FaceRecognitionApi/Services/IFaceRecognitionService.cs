using FaceRecognitionApi.Models;

namespace FaceRecognitionApi.Services;

public interface IFaceRecognitionService
{
    /// <summary>
    /// Recognizes a face from the uploaded image stream.
    /// Returns the best matching person from the database, or null if no match found.
    /// </summary>
    Task<RecognitionResult> RecognizeAsync(Stream imageStream, string fileName);
}
