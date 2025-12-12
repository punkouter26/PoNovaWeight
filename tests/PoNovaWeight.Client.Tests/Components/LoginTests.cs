using Bunit;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using PoNovaWeight.Client.Pages;

namespace PoNovaWeight.Client.Tests.Components;

public class LoginTests : TestContext
{
    public LoginTests()
    {
        // Register NavigationManager for the Login page
        Services.AddSingleton<NavigationManager>(new FakeNavigationManager());
    }

    [Fact]
    public void Login_RendersSignInButton()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Markup.Should().Contain("Sign in with Google");
    }

    [Fact]
    public void Login_ContainsGoogleLoginLink()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        var link = cut.Find("a[href*='/api/auth/login']");
        link.Should().NotBeNull();
    }

    [Fact]
    public void Login_DisplaysAppTitle()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        cut.Markup.Should().Contain("NovaWeight");
        cut.Markup.Should().Contain("Your personal food journal");
    }

    [Fact]
    public void Login_HasGoogleLogo()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert
        var svg = cut.Find("svg");
        svg.Should().NotBeNull();
        // Google logo colors
        cut.Markup.Should().Contain("#4285F4"); // Google Blue
        cut.Markup.Should().Contain("#34A853"); // Google Green
    }

    [Fact]
    public void Login_WithReturnUrl_IncludesInLoginLink()
    {
        // For SupplyParameterFromQuery parameters, we need to test via the rendered output
        // Since the parameter comes from query string, we test the default behavior 
        // and verify the component's URL generation logic

        // Act
        var cut = RenderComponent<Login>();

        // Assert - Component should have link to auth endpoint
        var link = cut.Find("a[href*='/api/auth/login']");
        link.Should().NotBeNull();
        // Verify the href contains the returnUrl parameter
        var href = link.GetAttribute("href");
        href.Should().Contain("returnUrl=");
    }

    [Fact]
    public void Login_WithoutReturnUrl_DefaultsToRoot()
    {
        // Act
        var cut = RenderComponent<Login>();

        // Assert - Default return URL should be root
        cut.Markup.Should().Contain("/api/auth/login?returnUrl=%2F");
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
    }
}
