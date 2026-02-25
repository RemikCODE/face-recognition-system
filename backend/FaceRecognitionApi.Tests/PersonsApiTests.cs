using FaceRecognitionApi.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace FaceRecognitionApi.Tests;

public class PersonsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public PersonsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                // Replace SQLite with in-memory DB for tests
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<AppDbContext>(options =>
                    options.UseInMemoryDatabase("PersonsApiTests"));
            });
        });
    }

    [Fact]
    public async Task GetPersons_ReturnsOkWithPaginatedResult()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/persons");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"items\"", json);
        Assert.Contains("\"total\"", json);
    }

    [Fact]
    public async Task GetPersonById_NotFound_Returns404()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/persons/999999");

        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SeedFromCsv_MissingPath_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/persons/seed",
            new { csvFilePath = "" });

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Recognize_NoFile_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        using var content = new System.Net.Http.MultipartFormDataContent();
        var response = await client.PostAsync("/api/faces/recognize", content);

        Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
    }
}
