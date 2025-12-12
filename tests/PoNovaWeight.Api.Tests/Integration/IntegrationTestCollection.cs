using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace PoNovaWeight.Api.Tests.Integration;

/// <summary>
/// Collection definition for integration tests that share a WebApplicationFactory.
/// This ensures tests run sequentially to avoid Serilog bootstrap logger conflicts.
/// </summary>
[CollectionDefinition("Integration Tests")]
public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
}
