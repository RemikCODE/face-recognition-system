using FaceRecognitionApi.Models;
using FaceRecognitionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FaceRecognitionApi.Pages;

public class IndexModel : PageModel
{
    private readonly IFaceRecognitionService _recognitionService;

    public IndexModel(IFaceRecognitionService recognitionService)
    {
        _recognitionService = recognitionService;
    }

    [BindProperty]
    public IFormFile? ImageFile { get; set; }

    public RecognitionResult? Result { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ImageFile == null || ImageFile.Length == 0)
        {
            ModelState.AddModelError(nameof(ImageFile), "Please select an image file.");
            return Page();
        }

        await using var stream = ImageFile.OpenReadStream();
        Result = await _recognitionService.RecognizeAsync(stream, ImageFile.FileName);
        return Page();
    }
}
