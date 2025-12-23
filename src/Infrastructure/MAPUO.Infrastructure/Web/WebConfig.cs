namespace MAPUO.Infrastructure.Web;

/// <summary>
/// Configuración para la automatización web.
/// </summary>
public class WebConfig
{
    /// <summary>
    /// Tiempo máximo de espera para la ejecución completa de una tarea (en milisegundos).
    /// </summary>
    public int ExecutionTimeoutMs { get; set; } = 30000; // 30 segundos por defecto

    /// <summary>
    /// Tiempo máximo de espera para encontrar un elemento en la página (en milisegundos).
    /// </summary>
    public int ElementWaitTimeoutMs { get; set; } = 10000; // 10 segundos por defecto

    /// <summary>
    /// Tipo de navegador: "chromium", "firefox", "webkit".
    /// </summary>
    public string BrowserType { get; set; } = "chromium";

    /// <summary>
    /// Si ejecutar en modo headless (sin interfaz gráfica).
    /// </summary>
    public bool Headless { get; set; } = false;

    /// <summary>
    /// Lista de navegadores para ejecutar pruebas en paralelo.
    /// Si está vacío, usa solo BrowserType.
    /// </summary>
    public List<string> Browsers { get; set; } = new List<string>();

    /// <summary>
    /// Si grabar video de la ejecución.
    /// </summary>
    public bool RecordVideo { get; set; } = false;

    /// <summary>
    /// Si tomar screenshot antes de cada paso.
    /// </summary>
    public bool ScreenshotsBeforeStep { get; set; } = false;

    /// <summary>
    /// Si tomar screenshot después de cada paso.
    /// </summary>
    public bool ScreenshotsAfterStep { get; set; } = false;

    /// <summary>
    /// Si tomar screenshot solo en fallos (siempre activo además de otros).
    /// </summary>
    public bool ScreenshotsOnFailure { get; set; } = true;

    /// <summary>
    /// Directorio base para guardar evidencias (videos, screenshots).
    /// </summary>
    public string EvidenceBasePath { get; set; } = "TestResults/Evidence";

    /// <summary>
    /// Si se debe generar el reporte (Allure u otro) durante la ejecución.
    /// </summary>
    public bool GenerateReport { get; set; } = true;

    /// <summary>
    /// Tags a ejecutar; si está vacío se ejecutan todos.
    /// </summary>
    public List<string> Tags { get; set; } = new();
}