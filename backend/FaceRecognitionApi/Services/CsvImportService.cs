using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using FaceRecognitionApi.Data;
using FaceRecognitionApi.Models;

namespace FaceRecognitionApi.Services;

public class CsvImportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CsvImportService> _logger;

    public CsvImportService(AppDbContext db, ILogger<CsvImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Seeds the Persons table from a CSV file.
    /// Expected CSV format (with header): id,label
    /// where label is a filename like "Robert Downey Jr_87.jpg"
    /// The person name is extracted by removing the trailing "_N.jpg" suffix.
    /// </summary>
    public async Task<int> ImportAsync(string csvFilePath)
    {
        if (!File.Exists(csvFilePath))
        {
            _logger.LogWarning("CSV file not found: {Path}", csvFilePath);
            return 0;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            HeaderValidated = null,
            PrepareHeaderForMatch = args => args.Header.ToLowerInvariant(),
        };

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, config);

        var records = new List<Person>();
        await foreach (var record in csv.GetRecordsAsync<CsvRecord>())
        {
            var name = ExtractName(record.Label);
            records.Add(new Person
            {
                Id = record.Id,
                Name = name,
                ImageFileName = record.Label,
            });
        }

        if (records.Count == 0)
        {
            _logger.LogWarning("No records found in CSV file: {Path}", csvFilePath);
            return 0;
        }

        _db.Persons.RemoveRange(_db.Persons);
        await _db.Persons.AddRangeAsync(records);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Imported {Count} records from {Path}", records.Count, csvFilePath);
        return records.Count;
    }

    /// <summary>
    /// Extracts the person name from a filename like "Robert Downey Jr_87.jpg".
    /// Returns "Robert Downey Jr" by removing the last "_N" part and the extension.
    /// </summary>
    public static string ExtractName(string label)
    {
        var nameWithoutExt = Path.GetFileNameWithoutExtension(label);
        var lastUnderscore = nameWithoutExt.LastIndexOf('_');
        if (lastUnderscore > 0)
        {
            return nameWithoutExt[..lastUnderscore].Trim();
        }
        return nameWithoutExt.Trim();
    }

    private class CsvRecord
    {
        [CsvHelper.Configuration.Attributes.Name("id")]
        public int Id { get; set; }
        [CsvHelper.Configuration.Attributes.Name("label")]
        public string Label { get; set; } = string.Empty;
    }
}
