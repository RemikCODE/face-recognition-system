using FaceRecognitionApi.Data;
using FaceRecognitionApi.Models;
using FaceRecognitionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionApi.Pages.Persons;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly CsvImportService _csvImport;

    public IndexModel(AppDbContext db, CsvImportService csvImport)
    {
        _db = db;
        _csvImport = csvImport;
    }

    public List<Person> Persons { get; set; } = [];
    public int Total { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);
    public string? Search { get; set; }

    [BindProperty]
    public IFormFile? CsvFile { get; set; }

    public string? SeedMessage { get; set; }
    public bool SeedSuccess { get; set; }

    public async Task OnGetAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        CurrentPage = Math.Max(1, page);
        PageSize = pageSize is >= 1 and <= 200 ? pageSize : 50;
        Search = search;

        await LoadPersonsAsync();
    }

    public async Task<IActionResult> OnPostSeedAsync()
    {
        if (CsvFile is null || CsvFile.Length == 0)
        {
            SeedMessage = "Wybierz plik CSV przed kliknięciem Załaduj.";
            SeedSuccess = false;
        }
        else
        {
            using var stream = CsvFile.OpenReadStream();
            var count = await _csvImport.ImportFromStreamAsync(stream);
            if (count > 0)
            {
                SeedMessage = $"✅ Załadowano {count} rekordów z pliku \"{CsvFile.FileName}\".";
                SeedSuccess = true;
            }
            else
            {
                SeedMessage = $"⚠ Plik \"{CsvFile.FileName}\" nie zawiera rekordów. Sprawdź format CSV (kolumny: id, label).";
                SeedSuccess = false;
            }
        }

        await LoadPersonsAsync();
        return Page();
    }

    private async Task LoadPersonsAsync()
    {
        var query = _db.Persons.AsQueryable();
        if (!string.IsNullOrWhiteSpace(Search))
        {
            query = query.Where(p => p.Name.Contains(Search));
        }

        Total = await query.CountAsync();
        Persons = await query
            .OrderBy(p => p.Id)
            .Skip((CurrentPage - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }
}
