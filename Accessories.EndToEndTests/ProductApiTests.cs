using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;

namespace Accessories.EndToEndTests;

public class ProductApiTests : IAsyncLifetime
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;

    // This runs once before the tests in this class execute
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        
        _request = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            // Point this directly at your Docker container's exposed port
            BaseURL = "http://127.0.0.1:8080",
            IgnoreHTTPSErrors = true
        });
    }

    // This runs after all tests in this class have finished
    public async Task DisposeAsync()
    {
        if (_request != null) await _request.DisposeAsync();
        if (_playwright != null) _playwright.Dispose();
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccessAndValidJson()
    {
        // Act: Fire a real HTTP GET request to the Docker container
        var response = await _request.GetAsync("/api/products");

        // Assert: Verify network-level success
        Assert.True(response.Ok, $"API returned {response.Status}: {response.StatusText}");

        // Assert: Parse and verify the JSON payload
        var json = await response.JsonAsync();
        Assert.NotNull(json);
    }

    [Fact]
    public async Task CreateProduct_SuccessfullyCreatesAndReturnsProduct()
    {
        // Arrange
        var newProduct = new
        {
            name = "Logitech MX Master 3S",
            price = 99.99,
            category = "Mice"
        };

        // Act: Fire a POST request with the JSON body
        var response = await _request.PostAsync("/api/products", new APIRequestContextOptions
        {
            DataObject = newProduct
        });

        // Assert
        Assert.Equal(201, response.Status);
        
        var responseBody = await response.JsonAsync();
        Assert.Equal("Logitech MX Master 3S", responseBody?.GetProperty("name").GetString());
    }
}