namespace MAPUO.Core.Abilities;

/// <summary>
/// Habilidad para interactuar con aplicaciones web.
/// Define el contrato para operaciones comunes de automatización web.
/// </summary>
public interface IWebAbility : IAbility
{
    /// <summary>
    /// Navega a una URL específica.
    /// </summary>
    /// <param name="url">URL de destino</param>
    Task NavigateToAsync(string url);

    /// <summary>
    /// Hace clic en un elemento identificado por selector.
    /// </summary>
    /// <param name="selector">Selector del elemento (CSS, XPath, etc.)</param>
    Task ClickAsync(string selector);

    /// <summary>
    /// Llena un campo de entrada con texto.
    /// </summary>
    /// <param name="selector">Selector del campo de entrada</param>
    /// <param name="text">Texto a ingresar</param>
    Task FillAsync(string selector, string text);

    /// <summary>
    /// Obtiene el texto de un elemento.
    /// </summary>
    /// <param name="selector">Selector del elemento</param>
    /// <returns>Texto del elemento</returns>
    Task<string> GetTextAsync(string selector);

    /// <summary>
    /// Obtiene la URL actual de la página.
    /// </summary>
    /// <returns>URL actual</returns>
    Task<string> GetCurrentUrlAsync();

    /// <summary>
    /// Verifica si un elemento es visible.
    /// </summary>
    /// <param name="selector">Selector del elemento</param>
    /// <returns>True si el elemento es visible</returns>
    Task<bool> IsVisibleAsync(string selector);

    /// <summary>
    /// Espera a que un elemento sea visible.
    /// </summary>
    /// <param name="selector">Selector del elemento</param>
    /// <param name="timeoutMs">Timeout en milisegundos</param>
    Task WaitForSelectorAsync(string selector, int timeoutMs = 30000);

    /// <summary>
    /// Toma una captura de pantalla.
    /// </summary>
    /// <param name="path">Ruta donde guardar la captura</param>
    Task TakeScreenshotAsync(string path);

    /// <summary>
    /// Presiona una tecla específica.
    /// </summary>
    /// <param name="selector">Selector del elemento donde presionar la tecla</param>
    /// <param name="key">Tecla a presionar (ej: "Enter", "Escape")</param>
    Task PressKeyAsync(string selector, string key);

    /// <summary>
    /// Cierra el navegador y libera recursos.
    /// </summary>
    Task CloseAsync();
}
