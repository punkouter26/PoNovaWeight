using Azure;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using FluentAssertions;
using Moq;
using PoNovaWeight.Api.Infrastructure.TableStorage;

namespace PoNovaWeight.Api.Tests.Unit.Auth;

public class UserRepositoryTests
{
    private readonly Mock<TableServiceClient> _tableServiceClientMock;
    private readonly Mock<TableClient> _tableClientMock;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _tableServiceClientMock = new Mock<TableServiceClient>();
        _tableClientMock = new Mock<TableClient>();

        _tableServiceClientMock
            .Setup(c => c.GetTableClient("Users"))
            .Returns(_tableClientMock.Object);

        _repository = new UserRepository(_tableServiceClientMock.Object);
    }

    [Fact]
    public async Task GetAsync_UserExists_ReturnsUser()
    {
        // Arrange
        var email = "test@example.com";
        var expectedEntity = new UserEntity
        {
            PartitionKey = email.ToLowerInvariant(),
            RowKey = "profile",
            DisplayName = "Test User",
            PictureUrl = "https://example.com/pic.jpg",
            FirstLoginUtc = DateTimeOffset.UtcNow.AddDays(-30),
            LastLoginUtc = DateTimeOffset.UtcNow
        };

        _tableClientMock
            .Setup(c => c.GetEntityAsync<UserEntity>(
                email.ToLowerInvariant(),
                "profile",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(expectedEntity, Mock.Of<Response>()));

        // Act
        var result = await _repository.GetAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.PartitionKey.Should().Be(email.ToLowerInvariant());
        result.DisplayName.Should().Be("Test User");
        result.PictureUrl.Should().Be("https://example.com/pic.jpg");
    }

    [Fact]
    public async Task GetAsync_UserNotFound_ReturnsNull()
    {
        // Arrange
        var email = "nonexistent@example.com";

        _tableClientMock
            .Setup(c => c.GetEntityAsync<UserEntity>(
                email.ToLowerInvariant(),
                "profile",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not Found"));

        // Act
        var result = await _repository.GetAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_NormalizesEmail_ToLowercase()
    {
        // Arrange
        var email = "Test.User@EXAMPLE.COM";
        var normalizedEmail = email.ToLowerInvariant();

        _tableClientMock
            .Setup(c => c.GetEntityAsync<UserEntity>(
                normalizedEmail,
                "profile",
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new RequestFailedException(404, "Not Found"));

        // Act
        await _repository.GetAsync(email);

        // Assert
        _tableClientMock.Verify(c => c.GetEntityAsync<UserEntity>(
            normalizedEmail,
            "profile",
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpsertAsync_CallsTableClientUpsert()
    {
        // Arrange
        var entity = new UserEntity
        {
            PartitionKey = "test@example.com",
            RowKey = "profile",
            DisplayName = "Test User",
            FirstLoginUtc = DateTimeOffset.UtcNow,
            LastLoginUtc = DateTimeOffset.UtcNow
        };

        _tableClientMock
            .Setup(c => c.UpsertEntityAsync(
                entity,
                TableUpdateMode.Replace,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response>());

        // Act
        await _repository.UpsertAsync(entity);

        // Assert
        _tableClientMock.Verify(c => c.UpsertEntityAsync(
            entity,
            TableUpdateMode.Replace,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTableIfNotExists()
    {
        // Arrange
        _tableClientMock
            .Setup(c => c.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(Response.FromValue(new TableItem("Users"), Mock.Of<Response>())));

        // Act
        await _repository.InitializeAsync();

        // Assert
        _tableClientMock.Verify(c => c.CreateIfNotExistsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
