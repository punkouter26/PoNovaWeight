using Microsoft.Playwright;

namespace PoNovaWeight.E2E;

/// <summary>
/// End-to-end tests for Google OAuth authentication flow.
/// Note: Full OAuth flow cannot be tested without mocking Google's consent screen.
/// These tests verify the application's redirect behavior and UI elements.
/// REQUIRES: The application must be running at localhost:5000 before running these tests.
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
    public async Task LoginPage_DisplaysCorrectUIElements()
    {
        // Act - Navigate to login page
        await _page!.GotoAsync($"{BaseUrl}/login");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Assert - All UI elements in one test
        var signInButton = await _page.GetByText("Sign in with Google").CountAsync();
        Assert.Equal(1, signInButton);

        var title = await _page.TitleAsync();
        Assert.Contains("NovaWeight", title);

        var svgCount = await _page.Locator("svg").CountAsync();
        Assert.True(svgCount >= 1);

        var content = await _page.ContentAsync();
        Assert.Contains("terms of service", content.ToLowerInvariant());
    }

    [Fact]
    public async Task AuthFlow_WorksCorrectly()
    {
        // Test homepage redirect
        await _page!.GotoAsync(BaseUrl);
        await _page.WaitForURLAsync($"{BaseUrl}/login**", new PageWaitForURLOptions { Timeout = 5000 });
        Assert.Contains("/login", _page.Url);

        // Test auth/me returns unauthenticated
        var response = await _page.APIRequest.GetAsync($"{BaseUrl}/api/auth/me");
        Assert.True(response.Ok);
        var body = await response.TextAsync();
        Assert.Contains("false", body.ToLowerInvariant());

        // Test logout redirects
        await _page.GotoAsync($"{BaseUrl}/api/auth/logout");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.Contains("/login", _page.Url);
    }
}
