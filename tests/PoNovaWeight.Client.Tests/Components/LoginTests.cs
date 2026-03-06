using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using PoNovaWeight.Client.Pages;
using PoNovaWeight.Client.Services;
using System.Security.Claims;

namespace PoNovaWeight.Client.Tests.Components;

public class LoginTests : BunitContext
{
    public LoginTests()
    {
        // Register NavigationManager for the Login page
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
        
        // Register mock auth state provider for OIDC
        var mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        mockAuthStateProvider
            .Setup(x => x.GetAuthenticationStateAsync())
            .ReturnsAsync(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
        Services.AddSingleton<AuthenticationStateProvider>(mockAuthStateProvider.Object);
        
        // Register mock configuration with no OAuth configured
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Google:ClientId"]).Returns((string?)null);
        mockConfiguration.Setup(c => c["Microsoft:ClientId"]).Returns((string?)null);
        Services.AddSingleton<IConfiguration>(mockConfiguration.Object);
        
        // Register AuthService
        Services.AddSingleton(sp => new AuthService(
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<IJSRuntime>()));
    }

    [Fact]
    public void Login_ShowsNoAuthMessage_WhenNoProviderConfigured()
    {
        // Act
        var cut = Render<Login>();

        // Assert - No OAuth configured, shows error message
        cut.Markup.Should().Contain("No authentication method configured");
    }

    [Fact]
    public void Login_DisplaysAppTitle()
    {
        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("PoNovaWeight");
        cut.Markup.Should().Contain("Your personal OMAD food journal");
    }

    [Fact]
    public void Login_ShowsGoogleButton_WhenGoogleConfigured()
    {
        // Arrange - Configure Google ClientId
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Google:ClientId"]).Returns("test-google-client-id");
        mockConfig.Setup(c => c["Microsoft:ClientId"]).Returns((string?)null);
        Services.AddSingleton<IConfiguration>(mockConfig.Object);

        // Re-register AuthService with new config
        Services.AddSingleton(sp => new AuthService(
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<IJSRuntime>()));

        // Act
        var cut = Render<Login>();

        // Assert
        cut.Markup.Should().Contain("Sign in with Google");
    }

    private class FakeNavigationManager : NavigationManager
    {
        public FakeNavigationManager()
        {
            Initialize("http://localhost/", "http://localhost/login");
        }

        public void SetUri(string uri)
        {
            Uri = uri;
        }

        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = uri;
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            Uri = uri;
        }
    }
}
