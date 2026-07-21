using System.Net;
using System.Net.Http.Json;
using Accessories.Api.DTOs;
using Accessories.Api.Models;
using AutoFixture.Xunit2;
using FluentAssertions;

namespace Accessories.Api.Tests.Integration;

public class ProductsIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ProductsIntegrationTests(CustomWebApplicationFactory factory)
    {
        // This client is automatically configured to talk to the in-memory test server
        _client = factory.CreateClient();
    }

    [Theory, AutoData]
    public async Task CreateProduct_SavesToDatabase_AndCanBeRetrieved(ProductDto newProduct)
    {       

        // Act 1: Send the API POST request to create the product
        var postResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
        
        // Assert 1: Verify the API returned 201 Created
        postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        
        // Extract the created ID from the response body
        var createdProduct = await postResponse.Content.ReadFromJsonAsync<Product>();
        createdProduct.Should().NotBeNull();
        createdProduct!.Id.Should().BeGreaterThan(0);

        // Act 2: Send a GET request to the Get endpoint to fetch the newly created item
        var getResponse = await _client.GetAsync($"/api/products/{createdProduct.Id}");
        
        // Assert 2: Verify it was actually saved and returned correctly
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedProduct = await getResponse.Content.ReadFromJsonAsync<Product>();
        
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Name.Should().Be(newProduct.Name);
        retrievedProduct.Price.Should().Be(newProduct.Price);
    }

    [Theory, AutoData]
    public async Task CreateMultipleProducts_SavesToDatabase_AndCanBeRetrieved(List<ProductDto> newProducts)
    {
        foreach (var newProduct in newProducts)
        {
            // Act 1: Send the API POST request to create the product
            var postResponse = await _client.PostAsJsonAsync("/api/products", newProduct);
            
            // Assert 1: Verify the API returned 201 Created
            postResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            
            // Extract the created ID from the response body
            var createdProduct = await postResponse.Content.ReadFromJsonAsync<Product>();
            createdProduct.Should().NotBeNull();
            createdProduct!.Id.Should().BeGreaterThan(0);
        }        

        // Act 2: Send a GET request to the Get endpoint to fetch the newly created item
        var getResponse = await _client.GetAsync($"/api/products");
        
        // Assert 2: Verify it was actually saved and returned correctly
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedProducts = await getResponse.Content.ReadFromJsonAsync<IEnumerable<Product>>();
        
        retrievedProducts.Should().NotBeNull();
        retrievedProducts.Count().Should().Be(newProducts.Count);
        foreach (var expectedProduct in newProducts)
        {
            retrievedProducts.Should().ContainEquivalentOf(expectedProduct, options => options.ExcludingMissingMembers());
        }
    }

    [Theory, AutoData]
    public async Task DeleteProduct_WhenIdExists_ReturnsNoContent_AndRemovesFromDatabase(ProductDto productToCreate)
    {
        // Arrange: First, we MUST create a product so we have a valid, existing ID to delete
        var postResponse = await _client.PostAsJsonAsync("/api/products", productToCreate);
        var createdProduct = await postResponse.Content.ReadFromJsonAsync<Product>();
        var validId = createdProduct!.Id;

        // Act: Send the DELETE request using the valid ID
        var deleteResponse = await _client.DeleteAsync($"/api/products/{validId}");

        // Assert: A successful delete should return HTTP 204 No Content
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verification: Try to fetch it again to prove it was actually removed from the database
        var getResponse = await _client.GetAsync($"/api/products/{validId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
    
    [Theory, AutoData]
    public async Task UpdateProduct_WhenIdExists_ReturnsNoContent_AndUpdatesInDatabase(ProductDto productToCreate, ProductDto productToUpdate)
    {
        // Arrange: First, we MUST create a product so we have a valid, existing ID to update
        var postResponse = await _client.PostAsJsonAsync("/api/products", productToCreate);
        var createdProduct = await postResponse.Content.ReadFromJsonAsync<Product>();
        var validId = createdProduct!.Id;

        // Act: Send the PUT request using the valid ID
        var updateResponse = await _client.PutAsJsonAsync($"/api/products/{validId}", productToUpdate);

        // Assert: A successful update should return HTTP 204 No Content
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verification: Try to fetch it again to prove it was actually updated in the database
        var getResponse = await _client.GetAsync($"/api/products/{validId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedProduct = await getResponse.Content.ReadFromJsonAsync<Product>();
        
        retrievedProduct.Should().NotBeNull();
        retrievedProduct!.Name.Should().Be(productToUpdate.Name);
        retrievedProduct.Price.Should().Be(productToUpdate.Price);
    }

    [Fact]
    public async Task DeleteProduct_WhenIdDoesNotExist_ReturnsNotFound()
    {
        // Arrange: Pick an ID that is practically guaranteed not to exist in your test DB
        int nonExistentId = 999999;

        // Act: Attempt to delete the fake ID
        var deleteResponse = await _client.DeleteAsync($"/api/products/{nonExistentId}");

        // Assert: The controller should handle the null result and return HTTP 404 Not Found
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory, AutoData]
    public async Task UpdateProduct_WhenIdDoesNotExist_ReturnsNotFound(ProductDto updateDto)
    {
        // Arrange: Pick an ID guaranteed not to exist in the test database
        int nonExistentId = 999999;

        // Act: Send a PUT request with the valid payload but invalid ID
        var putResponse = await _client.PutAsJsonAsync($"/api/products/{nonExistentId}", updateDto);

        // Assert: Verify the pipeline correctly translates the missing entity to a 404
        putResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}