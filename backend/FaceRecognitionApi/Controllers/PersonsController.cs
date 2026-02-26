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
    /// Seeds the database from a CSV file.
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
}
