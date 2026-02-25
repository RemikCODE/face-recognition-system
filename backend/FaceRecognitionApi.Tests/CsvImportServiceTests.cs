using FaceRecognitionApi.Data;
using FaceRecognitionApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FaceRecognitionApi.Tests;

public class CsvImportServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    private static ILogger<T> CreateLogger<T>() =>
        LoggerFactory.Create(_ => { }).CreateLogger<T>();

    [Theory]
    [InlineData("Robert Downey Jr_87.jpg", "Robert Downey Jr")]
    [InlineData("Scarlett Johansson_123.jpg", "Scarlett Johansson")]
    [InlineData("Unknown_1.png", "Unknown")]
    [InlineData("SingleName.jpg", "SingleName")]
    [InlineData("John_Smith_42.jpg", "John_Smith")]
    public void ExtractName_ReturnsCorrectName(string label, string expected)
    {
        var result = CsvImportService.ExtractName(label);
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ImportAsync_FileNotFound_ReturnsZero()
    {
        using var db = CreateDb("CsvImport_FileNotFound");
        var service = new CsvImportService(db, CreateLogger<CsvImportService>());

        var count = await service.ImportAsync("/tmp/nonexistent_file.csv");

        Assert.Equal(0, count);
    }

    [Fact]
    public async Task ImportAsync_ValidCsv_ImportsAllRecords()
    {
        var csvPath = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(csvPath,
                "id,label\n" +
                "1,Robert Downey Jr_87.jpg\n" +
                "2,Scarlett Johansson_12.jpg\n" +
                "3,Chris Evans_5.jpg\n");

            using var db = CreateDb($"CsvImport_Valid_{Guid.NewGuid()}");
            var service = new CsvImportService(db, CreateLogger<CsvImportService>());

            var count = await service.ImportAsync(csvPath);

            Assert.Equal(3, count);
            Assert.Equal(3, db.Persons.Count());
            Assert.Contains(db.Persons, p => p.Name == "Robert Downey Jr");
            Assert.Contains(db.Persons, p => p.Name == "Scarlett Johansson");
            Assert.Contains(db.Persons, p => p.Name == "Chris Evans");
        }
        finally
        {
            File.Delete(csvPath);
        }
    }

    [Fact]
    public async Task ImportAsync_ReplacesExistingRecords()
    {
        var csvPath1 = Path.GetTempFileName();
        var csvPath2 = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(csvPath1, "id,label\n1,Person A_1.jpg\n");
            await File.WriteAllTextAsync(csvPath2, "id,label\n1,Person B_1.jpg\n2,Person C_2.jpg\n");

            using var db = CreateDb($"CsvImport_Replace_{Guid.NewGuid()}");
            var service = new CsvImportService(db, CreateLogger<CsvImportService>());

            await service.ImportAsync(csvPath1);
            var count = await service.ImportAsync(csvPath2);

            Assert.Equal(2, count);
            Assert.Equal(2, db.Persons.Count());
            Assert.DoesNotContain(db.Persons, p => p.Name == "Person A");
        }
        finally
        {
            File.Delete(csvPath1);
            File.Delete(csvPath2);
        }
    }
}
