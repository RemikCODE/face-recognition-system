using FaceRecognitionApi.Data;
using FaceRecognitionApi.Models;
using FaceRecognitionApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FaceRecognitionApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly CsvImportService _csvImport;

    public PersonsController(AppDbContext db, CsvImportService csvImport)
    {
        _db = db;
        _csvImport = csvImport;
    }

    /// <summary>Gets a paginated list of all persons in the database.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Person>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 200) pageSize = 50;

        var query = _db.Persons.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Name.Contains(search));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(new { total, page, pageSize, items });
    }

    /// <summary>Gets a single person by ID.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Person), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var person = await _db.Persons.FindAsync(id);
        return person is null ? NotFound() : Ok(person);
    }

    /// <summary>
    /// Seeds the database from a CSV file path on the server.
    /// The CSV must have columns: id, label  (e.g. "Robert Downey Jr_87.jpg").
    /// </summary>
    [HttpPost("seed")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SeedFromCsv([FromBody] SeedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CsvFilePath))
        {
            return BadRequest(new { message = "csvFilePath is required." });
        }

        var count = await _csvImport.ImportAsync(request.CsvFilePath);
        return Ok(new { imported = count, message = $"Successfully imported {count} records." });
    }

    /// <summary>
    /// Seeds the database by uploading a CSV file directly.
    /// The CSV must have columns: id, label  (e.g. "Robert Downey Jr_87.jpg").
    /// </summary>
    [HttpPost("seed-upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SeedFromCsvUpload(IFormFile csv)
    {
        if (csv == null || csv.Length == 0)
        {
            return BadRequest(new { message = "A CSV file is required." });
        }

        await using var stream = csv.OpenReadStream();
        var count = await _csvImport.ImportFromStreamAsync(stream);
        return Ok(new { imported = count, message = $"Successfully imported {count} records." });
    }

    /// <summary>
    /// Scans a dataset directory for image files (jpg, jpeg, png, bmp) and seeds
    /// the Persons table from the filenames found.  The file must be named in the
    /// format "Person Name_N.ext" (e.g. "Robert Downey Jr_87.jpg").
    /// Existing records are replaced.
    /// </summary>
    [HttpPost("scan-dataset")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScanDataset([FromBody] ScanDatasetRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DatasetPath))
        {
            return BadRequest(new { message = "datasetPath is required." });
        }

        if (!Directory.Exists(request.DatasetPath))
        {
            return BadRequest(new { message = $"Directory not found: {request.DatasetPath}" });
        }

        var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".jpg", ".jpeg", ".png", ".bmp" };

        var files = Directory.EnumerateFiles(request.DatasetPath, "*", SearchOption.TopDirectoryOnly)
            .Where(f => imageExtensions.Contains(Path.GetExtension(f)))
            .OrderBy(f => f)
            .ToList();

        if (files.Count == 0)
        {
            return BadRequest(new { message = "No image files found in the specified directory." });
        }

        var records = files.Select(f => new Person
        {
            Name = CsvImportService.ExtractName(Path.GetFileName(f)),
            ImageFileName = Path.GetFileName(f),
        }).ToList();

        await _db.Persons.ExecuteDeleteAsync();
        await _db.Persons.AddRangeAsync(records);
        await _db.SaveChangesAsync();

        return Ok(new { imported = records.Count, message = $"Successfully imported {records.Count} records from dataset." });
    }

    /// <summary>Deletes all persons from the database.</summary>
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAll()
    {
        _db.Persons.RemoveRange(_db.Persons);
        await _db.SaveChangesAsync();
        return NoContent();
    }

    public class SeedRequest
    {
        public string CsvFilePath { get; set; } = string.Empty;
    }

    public class ScanDatasetRequest
    {
        public string DatasetPath { get; set; } = string.Empty;
    }
}
