using FaceRecognitionApi.Data;
using FaceRecognitionApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace FaceRecognitionApi.Tests;

public class FaceRecognitionServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RecognizeAsync_MlServiceNotConfigured_ReturnsNotFoundResult()
    {
        using var db = CreateDb($"FRS_{Guid.NewGuid()}");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["MlService:Url"] = "" })
            .Build();

        var httpClient = new System.Net.Http.HttpClient();
        var service = new FaceRecognitionService(db, httpClient, config,
            NullLogger<FaceRecognitionService>.Instance);

        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await service.RecognizeAsync(stream, "test.jpg");

        Assert.False(result.Found);
        Assert.Contains("not configured", result.Message, StringComparison.OrdinalIgnoreCase);
    }
}
