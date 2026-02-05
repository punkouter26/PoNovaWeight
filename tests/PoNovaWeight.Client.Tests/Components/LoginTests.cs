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
        
        // Register mock configuration (dev mode - no Google ClientId)
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Google:ClientId"]).Returns((string?)null);
        Services.AddSingleton<IConfiguration>(mockConfiguration.Object);
        
        // Register AuthService
        Services.AddSingleton(sp => new AuthService(
            sp.GetRequiredService<AuthenticationStateProvider>(),
            sp.GetRequiredService<NavigationManager>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IJSRuntime>()));
    }

    [Fact]
    public void Login_RendersDevLoginButton()
    {
        // Act
        var cut = Render<Login>();

        // Assert - In dev mode (no ClientId), shows Dev Login
        cut.Markup.Should().Contain("Dev Login");
        cut.Markup.Should().Contain("Development mode");
    }

    [Fact]
    public void Login_ContainsLoginButton()
    {
        // Act
        var cut = Render<Login>();

        // Assert - Now it's a button, not a link
        var button = cut.Find("button");
        button.Should().NotBeNull();
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
    public void Login_HasEmailInput_InDevMode()
    {
        // Act
        var cut = Render<Login>();

        // Assert - Dev mode shows email input
        var input = cut.Find("input[type='email']");
        input.Should().NotBeNull();
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
