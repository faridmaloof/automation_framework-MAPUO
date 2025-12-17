using MAPUO.Core.Abilities;
using Microsoft.Playwright;

namespace MAPUO.Infrastructure.Web;

/// <summary>
/// Implementación de IWebAbility utilizando Playwright.
/// Proporciona capacidades de automatización web usando Playwright como motor de navegación.
/// </summary>
public class PlaywrightWebAbility : IWebAbility
{
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IPage? _page;
    private readonly string _browserType;
    private readonly bool _headless;

    public string Name => "Playwright Web Automation";

    /// <summary>
    /// Constructor de PlaywrightWebAbility.
    /// </summary>
    /// <param name="browserType">Tipo de navegador: "chromium", "firefox", "webkit"</param>
    /// <param name="headless">Si se ejecuta en modo headless (sin interfaz gráfica)</param>
    public PlaywrightWebAbility(string browserType = "chromium", bool headless = false)
    {
        _browserType = browserType;
        _headless = headless;
    }

    /// <summary>
    /// Inicializa Playwright y lanza el navegador.
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();
            
            _browser = _browserType.ToLower() switch
            {
                "firefox" => await _playwright.Firefox.LaunchAsync(new BrowserTypeLaunchOptions { Headless = _headless }),
                "webkit" => await _playwright.Webkit.LaunchAsync(new BrowserTypeLaunchOptions { Headless = _headless }),
                _ => await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = _headless })
            };

            _page = await _browser.NewPageAsync();
        }
    }

    /// <summary>
    /// Obtiene la instancia de la página, inicializándola si es necesario.
    /// </summary>
    private async Task<IPage> GetPageAsync()
    {
        if (_page == null)
        {
            await InitializeAsync();
        }
        return _page!;
    }

    public async Task NavigateToAsync(string url)
    {
        var page = await GetPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
    }

    public async Task ClickAsync(string selector)
    {
        var page = await GetPageAsync();
        await page.ClickAsync(selector);
    }

    public async Task FillAsync(string selector, string text)
    {
        var page = await GetPageAsync();
        await page.FillAsync(selector, text);
    }

    public async Task<string> GetTextAsync(string selector)
    {
        var page = await GetPageAsync();
        var element = await page.QuerySelectorAsync(selector);
        
        if (element == null)
            throw new InvalidOperationException($"Elemento no encontrado: {selector}");

        return await element.TextContentAsync() ?? string.Empty;
    }

    public async Task<string> GetCurrentUrlAsync()
    {
        var page = await GetPageAsync();
        return page.Url;
    }

    public async Task<bool> IsVisibleAsync(string selector)
    {
        var page = await GetPageAsync();
        var element = await page.QuerySelectorAsync(selector);
        return element != null && await element.IsVisibleAsync();
    }

    public async Task WaitForSelectorAsync(string selector, int timeoutMs = 30000)
    {
        var page = await GetPageAsync();
        await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions 
        { 
            Timeout = timeoutMs 
        });
    }

    public async Task TakeScreenshotAsync(string path)
    {
        var page = await GetPageAsync();
        await page.ScreenshotAsync(new PageScreenshotOptions { Path = path });
    }

    public async Task PressKeyAsync(string selector, string key)
    {
        var page = await GetPageAsync();
        await page.PressAsync(selector, key);
    }

    public async Task CloseAsync()
    {
        if (_page != null)
        {
            await _page.CloseAsync();
            _page = null;
        }

        if (_browser != null)
        {
            await _browser.CloseAsync();
            _browser = null;
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
        }
    }
}
