using Accessories.Api.Controllers;
using Accessories.Api.DTOs;
using Accessories.Api.Models;
using Accessories.Api.Repositories;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Accessories.Api.Tests.Unit;

public class ProductsControllerTests
{
    [Theory, AutoMoqData]
    public async Task CreateProduct_WithValidDto_ReturnsCreatedAtActionResult(
        [Frozen] Mock<IProductRepository> repositoryMock,
        ProductsController sut,
        ProductDto productDto,
        Product createdProduct)
    {
        // Arrange
        repositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<Product>()))
            .ReturnsAsync(createdProduct);

        // Act
        var result = await sut.CreateProduct(productDto);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        
        createdResult.ActionName.Should().Be(nameof(ProductsController.GetProduct));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(createdProduct.Id);
        createdResult.Value.Should().BeEquivalentTo(createdProduct);
    }
    
    [Theory, AutoMoqData]
    public async Task CreateProduct_WithInvalidDto_ReturnsBadRequest(
        [Frozen] Mock<IProductRepository> repositoryMock,
        ProductsController sut,
        ProductDto productDto)
    {
        // Arrange
       _ = repositoryMock.Setup(repo => repo.CreateAsync(It.IsAny<Product>()))
            .Returns(Task.FromResult<Product>(null!)); // Bypasses ReturnsAsync nullability resolution

        // Act
        var result = await sut.CreateProduct(productDto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Theory, AutoMoqData]
    public async Task GetProduct_WhenProductExists_ReturnsOkResult(
        [Frozen] Mock<IProductRepository> repositoryMock,
        ProductsController sut,
        Product product)
    {
        // Arrange: Tell the mock repository what to return
        repositoryMock.Setup(repo => repo.GetByIdAsync(product.Id))
            .ReturnsAsync(product);

        // Act: Call the controller
        var result = await sut.GetProduct(product.Id);

        // Assert: Use FluentAssertions to verify the HTTP response and data
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(product);
    }
    
    [Theory, AutoMoqData]
    public async Task GetProduct_WhenProductDoesNotExist_ReturnsNotFound(
        [Frozen] Mock<IProductRepository> repositoryMock,
        ProductsController sut, // Prevents the BindingInfo error
        int nonExistentId)
    {
        // Arrange: Tell the mock to explicitly return null for this random ID
        repositoryMock.Setup(repo => repo.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Product?)null);

        // Act: Call the controller endpoint
        var result = await sut.GetProduct(nonExistentId);

        // Assert: Verify the controller correctly translates a null database result into a 404 Not Found
        result.Should().BeOfType<NotFoundResult>();
    }
    [Theory, AutoMoqData]
    public async Task GetProducts_WhenNoProductsExist_ReturnsOkWithEmptyList(
        [Frozen] Mock<IProductRepository> repositoryMock,
        [NoAutoProperties] ProductsController sut)
    {
        // Arrange: Tell the mock to return an empty list
        repositoryMock.Setup(repo => repo.GetAllAsync())
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await sut.GetProducts();

        // Assert: Verify we get a 200 OK and the list is actually empty
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        
        var returnedProducts = okResult.Value.Should().BeAssignableTo<IEnumerable<Product>>().Subject;
        returnedProducts.Should().BeEmpty();
    }

    [Theory, AutoMoqData]
    public async Task GetProducts_WhenRepositoryThrowsException_BubblesUpException(
        [Frozen] Mock<IProductRepository> repositoryMock,
        [NoAutoProperties] ProductsController sut,
        Exception expectedException) // AutoFixture generates a random exception
    {
        // Arrange: Tell the mock to simulate a database crash
        repositoryMock.Setup(repo => repo.GetAllAsync())
            .ThrowsAsync(expectedException);

        // Act: Capture the action without executing it immediately
        Func<Task> act = async () => await sut.GetProducts();

        // Assert: Verify that calling the controller throws the exact same exception
        await act.Should().ThrowAsync<Exception>()
            .WithMessage(expectedException.Message);
    }
    [Theory, AutoMoqData]
    public async Task UpdateProduct_WhenProductDoesNotExist_ReturnsNotFound(
        [Frozen] Mock<IProductRepository> repositoryMock,
        [NoAutoProperties] ProductsController sut, // Prevents the AutoFixture BindingInfo crash
        int nonExistentId,
        ProductDto updateDto)
    {
        // Arrange: Tell the mock repository that this ID does not exist
        repositoryMock.Setup(repo => repo.GetByIdAsync(nonExistentId))
            .ReturnsAsync((Product?)null);

        // Act: Attempt to update the product
        var result = await sut.UpdateProduct(nonExistentId, updateDto);

        // Assert: Verify the controller returns a 404 Not Found
        // Notice we don't use .Result here because the method returns IActionResult
        result.Should().BeOfType<NotFoundResult>();

        // Verify that the controller never attempted to save the invalid update
        repositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
    }

}