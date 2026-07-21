using Microsoft.Playwright;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Accessories.E2ETests;

public class ProductApiTests
{
    private IPlaywright _playwright = null!;
    private IAPIRequestContext _request = null!;

    [SetUp]
    public async Task Setup()
    {
        _playwright = await Playwright.CreateAsync();
        
        _request = await _playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            // Explicitly using IPv4 to avoid the ECONNREFUSED ::1 error
            BaseURL = "http://127.0.0.1:8080",
            IgnoreHTTPSErrors = true
        });
        // Start capturing network requests and sources
        await _request.Tracing.StartAsync(new TracingStartOptions
        {
            Snapshots = true,
            Sources = true,
            Title = TestContext.CurrentContext.Test.Name
        });
    }

    [TearDown]
    public async Task TearDown()
    {
        // 1. Determine test outcome
        var passed = TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Passed;
        var testName = TestContext.CurrentContext.Test.Name;
        
        // 2. Create a specific directory for the trace files
        var traceDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "playwright-traces");
        Directory.CreateDirectory(traceDir);
        
        var traceFilePath = Path.Combine(traceDir, $"{testName}_{(passed ? "PASS" : "FAIL")}.zip");

        // 3. Stop tracing and save the .zip file
        if (_request != null)
        {
            await _request.Tracing.StopAsync(new TracingStopOptions
            {
                Path = traceFilePath
            });
        }

        // 4. Attach the .zip file directly to the NUnit test results
        TestContext.AddTestAttachment(traceFilePath, "Playwright Trace Log");
        if (_request != null) await _request.DisposeAsync();
        if (_playwright != null) _playwright.Dispose();
    }

    [Test]
    public async Task GetProducts_ReturnsSuccessAndValidJson()
    {
        // Act
        var response = await _request.GetAsync("/api/products");

        // Assert network-level success
        Assert.That(response.Ok, Is.True, $"API returned {response.Status}: {response.StatusText}");

        // Assert JSON payload
        var json = await response.JsonAsync();
        Assert.That(json, Is.Not.Null);
    }

    [Test]
    public async Task CreateProduct_SuccessfullyCreatesAndReturnsProduct()
    {
        // Arrange
        var newProduct = new
        {
            name = "Logitech MX Master 3S",
            price = 99.99,
            category = "Mice"
        };

        // Act
        var response = await _request.PostAsync("/api/products", new APIRequestContextOptions
        {
            DataObject = newProduct
        });

        // Assert
        Assert.That(response.Status, Is.EqualTo(201));
        
        var responseBody = await response.JsonAsync();
        Assert.That(responseBody?.GetProperty("name").GetString(), Is.EqualTo("Logitech MX Master 3S"));
    }
    [Test]
    public async Task DeleteProduct_SuccessfullyRemovesProduct()
    {
        // Arrange: Create a temporary product so we have a valid ID to delete
        var temporaryProduct = new
        {
            name = "Disposable Keyboard",
            price = 29.99,
            category = "Keyboards"
        };

        var createResponse = await _request.PostAsync("/api/products", new APIRequestContextOptions
        {
            DataObject = temporaryProduct
        });

        Assert.That(createResponse.Status, Is.EqualTo(201), "Setup failed: Unable to create temporary product.");

        var createdJson = await createResponse.JsonAsync();
        var productId = createdJson?.GetProperty("id").GetInt32();

        // Act: Send DELETE request for the created product
        var deleteResponse = await _request.DeleteAsync($"/api/products/{productId}");

        // Assert: Verify DELETE response status (200 OK or 204 No Content)
        Assert.That(deleteResponse.Status, Is.AnyOf(200, 204), $"Expected 200 or 204 on DELETE, but got {deleteResponse.Status}");

        // Verify: Confirm the product no longer exists via GET
        var getResponse = await _request.GetAsync($"/api/products/{productId}");
        Assert.That(getResponse.Status, Is.EqualTo(404), $"Expected 404 Not Found, but product {productId} still exists.");
    }
}