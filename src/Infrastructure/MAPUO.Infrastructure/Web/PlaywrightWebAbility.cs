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
    private readonly WebConfig _config;
    private string? _videoPath;
    private string? _scenarioName;
    private int _stepCounter;

    public string Name => "Playwright Web Automation";

    /// <summary>
    /// Constructor de PlaywrightWebAbility.
    /// </summary>
    /// <param name="config">Configuración web</param>
    /// <param name="browserType">Tipo de navegador: "chromium", "firefox", "webkit"</param>
    /// <param name="headless">Si se ejecuta en modo headless (sin interfaz gráfica)</param>
    /// <param name="scenarioName">Nombre del escenario para organizar evidencias</param>
    public PlaywrightWebAbility(WebConfig config, string browserType = "chromium", bool headless = false, string? scenarioName = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _browserType = browserType;
        _headless = headless;
        _scenarioName = scenarioName;
        _stepCounter = 0;
    }

    /// <summary>
    /// Inicializa Playwright y lanza el navegador.
    /// </summary>
    private async Task InitializeAsync()
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();
            
            var launchOptions = new BrowserTypeLaunchOptions { Headless = _headless };

            _browser = _browserType.ToLower() switch
            {
                "firefox" => await _playwright.Firefox.LaunchAsync(launchOptions),
                "webkit" => await _playwright.Webkit.LaunchAsync(launchOptions),
                _ => await _playwright.Chromium.LaunchAsync(launchOptions)
            };

            // Crear contexto con video si está habilitado
            if (_config.RecordVideo && !_headless)
            {
                var videoDir = Path.Combine(_config.EvidenceBasePath, "Videos", _scenarioName ?? "Unknown", _browserType);
                Directory.CreateDirectory(videoDir);
                _videoPath = Path.Combine(videoDir, $"{DateTime.Now:yyyyMMdd_HHmmss}.webm");
                
                var contextOptions = new BrowserNewContextOptions
                {
                    RecordVideoDir = videoDir,
                    RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 }
                };
                
                var context = await _browser.NewContextAsync(contextOptions);
                _page = await context.NewPageAsync();
            }
            else
            {
                _page = await _browser.NewPageAsync();
            }
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

    /// <summary>
    /// Toma un screenshot si está configurado.
    /// </summary>
    private async Task TakeScreenshotIfConfiguredAsync(string type, string? customName = null)
    {
        if (_config.ScreenshotsBeforeStep || _config.ScreenshotsAfterStep || _config.ScreenshotsOnFailure)
        {
            try
            {
                var screenshotDir = Path.Combine(_config.EvidenceBasePath, "Screenshots", _scenarioName ?? "Unknown", _browserType, type);
                Directory.CreateDirectory(screenshotDir);
                
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var stepInfo = customName ?? $"step_{_stepCounter:00}";
                var screenshotPath = Path.Combine(screenshotDir, $"{stepInfo}_{timestamp}.png");
                
                await TakeScreenshotAsync(screenshotPath);
                Console.WriteLine($"Screenshot guardado: {screenshotPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al tomar screenshot: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Incrementa el contador de pasos para screenshots.
    /// </summary>
    public void IncrementStepCounter()
    {
        _stepCounter++;
    }

    public async Task NavigateToAsync(string url)
    {
        if (_config.ScreenshotsBeforeStep) await TakeScreenshotIfConfiguredAsync("before_navigate");
        
        var page = await GetPageAsync();
        await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
        
        if (_config.ScreenshotsAfterStep) await TakeScreenshotIfConfiguredAsync("after_navigate");
        _stepCounter++;
    }

    public async Task ClickAsync(string selector)
    {
        if (_config.ScreenshotsBeforeStep) await TakeScreenshotIfConfiguredAsync("before_click");
        
        var page = await GetPageAsync();
        await page.ClickAsync(selector);
        
        if (_config.ScreenshotsAfterStep) await TakeScreenshotIfConfiguredAsync("after_click");
        _stepCounter++;
    }

    public async Task FillAsync(string selector, string text)
    {
        if (_config.ScreenshotsBeforeStep) await TakeScreenshotIfConfiguredAsync("before_fill");
        
        var page = await GetPageAsync();
        await page.FillAsync(selector, text);
        
        if (_config.ScreenshotsAfterStep) await TakeScreenshotIfConfiguredAsync("after_fill");
        _stepCounter++;
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

    public async Task WaitForSelectorAsync(string selector, int? timeoutMs = null)
    {
        var page = await GetPageAsync();
        await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions 
        { 
            Timeout = timeoutMs ?? _config.ElementWaitTimeoutMs 
        });
    }

    public async Task WaitForUrlAsync(Func<string, bool> urlPredicate, int? timeoutMs = null)
    {
        var page = await GetPageAsync();
        await page.WaitForURLAsync(urlPredicate, new PageWaitForURLOptions 
        { 
            Timeout = timeoutMs ?? _config.ElementWaitTimeoutMs 
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
            // Detener grabación de video si está activa
            if (_config.RecordVideo && !_headless && _videoPath != null)
            {
                try
                {
                    await _page.CloseAsync();
                    Console.WriteLine($"Video guardado en: {_videoPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al cerrar página con video: {ex.Message}");
                }
            }
            else
            {
                await _page.CloseAsync();
            }
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
