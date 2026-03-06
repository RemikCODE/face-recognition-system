using FaceRecognitionApi.Models;

namespace FaceRecognitionApi.Services;

public interface IFaceRecognitionService
{
    /// <summary>
    /// Recognizes a face from the uploaded image stream.
    /// Returns the best matching person from the database, or null if no match found.
    /// </summary>
    Task<RecognitionResult> RecognizeAsync(Stream imageStream, string fileName);

    /// <summary>
    /// Forwards the image to the ML service's /add-person endpoint so it is stored
    /// in the dataset folder. Returns the filename chosen by the ML service,
    /// or null when the ML service is not configured.
    /// </summary>
    Task<string?> AddPersonAsync(string name, Stream imageStream, string fileName);
}
