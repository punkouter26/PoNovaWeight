using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration.Auth;

/// <summary>
/// Tests for auth endpoints.
/// Note: With client-side OIDC authentication, /api/auth/login and /api/auth/logout
/// endpoints no longer exist. Login/logout is handled entirely by the Blazor client.
/// </summary>
[Collection("Integration Tests")]
public class AuthEndpointsTests
{
    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task GetMe_Unauthenticated_ReturnsUnauthenticatedStatus()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var authStatus = await response.Content.ReadFromJsonAsync<AuthStatus>();
        authStatus.Should().NotBeNull();
        authStatus!.IsAuthenticated.Should().BeFalse();
        authStatus.User.Should().BeNull();
    }
}
