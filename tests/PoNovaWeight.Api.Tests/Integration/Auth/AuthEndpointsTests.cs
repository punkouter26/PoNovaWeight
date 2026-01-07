using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using PoNovaWeight.Shared.DTOs;

namespace PoNovaWeight.Api.Tests.Integration.Auth;

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
    public async Task GetLogin_ReturnsRedirectToGoogle()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/login");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("accounts.google.com");
    }

    [Fact]
    public async Task GetLogin_WithReturnUrl_IncludesInState()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/login?returnUrl=/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("accounts.google.com");
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

    [Fact]
    public async Task GetLogout_ReturnsRedirectToLogin()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/logout");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be("/login");
    }
}
