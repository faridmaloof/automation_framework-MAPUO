namespace MAPUO.Core.Abilities;

/// <summary>
/// Habilidad para interactuar con APIs REST.
/// Define el contrato para operaciones HTTP comunes.
/// </summary>
public interface IApiAbility : IAbility
{
    /// <summary>
    /// Realiza una petición GET.
    /// </summary>
    /// <typeparam name="T">Tipo de respuesta esperada</typeparam>
    /// <param name="endpoint">Endpoint de la API</param>
    /// <returns>Respuesta deserializada</returns>
    Task<T> GetAsync<T>(string endpoint);

    /// <summary>
    /// Realiza una petición POST.
    /// </summary>
    /// <typeparam name="TRequest">Tipo del cuerpo de la petición</typeparam>
    /// <typeparam name="TResponse">Tipo de respuesta esperada</typeparam>
    /// <param name="endpoint">Endpoint de la API</param>
    /// <param name="body">Cuerpo de la petición</param>
    /// <returns>Respuesta deserializada</returns>
    Task<TResponse> PostAsync<TRequest, TResponse>(string endpoint, TRequest body);

    /// <summary>
    /// Realiza una petición PUT.
    /// </summary>
    /// <typeparam name="TRequest">Tipo del cuerpo de la petición</typeparam>
    /// <typeparam name="TResponse">Tipo de respuesta esperada</typeparam>
    /// <param name="endpoint">Endpoint de la API</param>
    /// <param name="body">Cuerpo de la petición</param>
    /// <returns>Respuesta deserializada</returns>
    Task<TResponse> PutAsync<TRequest, TResponse>(string endpoint, TRequest body);

    /// <summary>
    /// Realiza una petición DELETE.
    /// </summary>
    /// <param name="endpoint">Endpoint de la API</param>
    Task DeleteAsync(string endpoint);

    /// <summary>
    /// Establece un encabezado HTTP para todas las peticiones.
    /// </summary>
    /// <param name="name">Nombre del encabezado</param>
    /// <param name="value">Valor del encabezado</param>
    void SetHeader(string name, string value);

    /// <summary>
    /// Obtiene el código de estado HTTP de la última respuesta.
    /// </summary>
    /// <returns>Código de estado HTTP</returns>
    int GetLastStatusCode();

    /// <summary>
    /// Obtiene la URL final utilizada en la última petición.
    /// </summary>
    string? GetLastRequestUrl();

    /// <summary>
    /// Obtiene el método HTTP de la última petición.
    /// </summary>
    string? GetLastMethod();

    /// <summary>
    /// Obtiene el cuerpo enviado en la última petición (si aplica).
    /// </summary>
    string? GetLastRequestBody();

    /// <summary>
    /// Obtiene el contenido crudo de la última respuesta.
    /// </summary>
    string? GetLastResponseContent();
}
