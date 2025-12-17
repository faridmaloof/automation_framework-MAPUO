namespace MAPUO.Core.Questions;

/// <summary>
/// Interfaz base para una pregunta en el patrón Screenplay.
/// Las preguntas permiten al Actor consultar el estado actual de la aplicación.
/// </summary>
/// <typeparam name="T">Tipo de respuesta que devuelve la pregunta</typeparam>
public interface IQuestion<T>
{
    /// <summary>
    /// Descripción de la pregunta para reportes.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Obtiene la respuesta a la pregunta utilizando las habilidades del actor.
    /// </summary>
    /// <param name="actor">Actor que realiza la pregunta</param>
    /// <returns>Respuesta de tipo T</returns>
    Task<T> AnswerAsync(Actors.IActor actor);
}
