using Microsoft.Playwright;

namespace PoNovaWeight.E2E;

/// <summary>
/// End-to-end tests for Google OAuth authentication flow.
/// Note: Full OAuth flow cannot be tested without mocking Google's consent screen.
/// These tests verify the application's redirect behavior and UI elements.
/// </summary>
public class GoogleAuthTests : IAsyncLifetime
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;

    private const string BaseUrl = "http://localhost:5000";

    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });
        _context = await _browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        if (_page is not null) await _page.CloseAsync();
        if (_context is not null) await _context.DisposeAsync();
        if (_browser is not null) await _browser.DisposeAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task Homepage_RedirectsToLogin_WhenUnauthenticated()
    {
        // Act
        await _page!.GotoAsync(BaseUrl);

        // Assert - Should redirect to login page
        await _page.WaitForURLAsync($"{BaseUrl}/login**");
        var url = _page.Url;
        Assert.Contains("/login", url);
    }

    [Fact]
    public async Task LoginPage_DisplaysGoogleSignInButton()
    {
        // Act
        await _page!.GotoAsync($"{BaseUrl}/login");
        // Wait for Blazor WASM to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForSelectorAsync("text=Sign in with Google", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var signInButton = await _page.GetByText("Sign in with Google").CountAsync();
        Assert.Equal(1, signInButton);
    }

    [Fact]
    public async Task LoginPage_HasCorrectTitle()
    {
        // Act
        await _page!.GotoAsync($"{BaseUrl}/login");
        // Wait for Blazor WASM to load and set title
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForFunctionAsync("() => document.title.includes('Sign In') || document.title.includes('NovaWeight')", 
            new PageWaitForFunctionOptions { Timeout = 10000 });

        // Assert
        var title = await _page.TitleAsync();
        Assert.Contains("NovaWeight", title);
    }

    [Fact]
    public async Task SignInButton_RedirectsToGoogle()
    {
        // Arrange
        await _page!.GotoAsync($"{BaseUrl}/login");

        // Act - Click sign in and catch the redirect
        var response = await _page.GotoAsync($"{BaseUrl}/api/auth/login");

        // Assert - Should redirect to Google
        var url = _page.Url;
        Assert.Contains("accounts.google.com", url);
    }

    [Fact]
    public async Task AuthMe_ReturnsUnauthenticated_WithoutSession()
    {
        // Act
        var response = await _page!.APIRequest.GetAsync($"{BaseUrl}/api/auth/me");

        // Assert
        Assert.True(response.Ok);
        var body = await response.TextAsync();
        // JSON property names are camelCase
        Assert.Contains("isauthenticated", body.ToLowerInvariant());
        Assert.Contains("false", body.ToLowerInvariant());
    }

    [Fact]
    public async Task Logout_RedirectsToLogin()
    {
        // Act
        await _page!.GotoAsync($"{BaseUrl}/api/auth/logout");

        // Assert - Should redirect to login page
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var url = _page.Url;
        Assert.Contains("/login", url);
    }

    [Fact]
    public async Task LoginPage_ContainsGoogleLogo()
    {
        // Act
        await _page!.GotoAsync($"{BaseUrl}/login");
        // Wait for Blazor WASM to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForSelectorAsync("svg", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert - Check for Google logo SVG
        var svgCount = await _page.Locator("svg").CountAsync();
        Assert.True(svgCount >= 1);
    }

    [Fact]
    public async Task LoginPage_HasPrivacyDisclaimer()
    {
        // Act
        await _page!.GotoAsync($"{BaseUrl}/login");
        // Wait for Blazor WASM to load
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        await _page.WaitForSelectorAsync("text=terms of service", new PageWaitForSelectorOptions { Timeout = 10000 });

        // Assert
        var content = await _page.ContentAsync();
        Assert.Contains("terms of service", content.ToLowerInvariant());
        Assert.Contains("privacy policy", content.ToLowerInvariant());
    }
}
