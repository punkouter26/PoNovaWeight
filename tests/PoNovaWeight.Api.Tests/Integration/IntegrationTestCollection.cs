using Xunit;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Collection definition for integration tests that share a WebApplicationFactory.
/// This ensures tests run sequentially to avoid Serilog bootstrap logger conflicts.
/// Uses CustomWebApplicationFactory to properly configure test dependencies.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
