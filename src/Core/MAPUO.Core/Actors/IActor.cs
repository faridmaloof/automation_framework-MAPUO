using MAPUO.Core.Abilities;
using MAPUO.Core.Tasks;
using MAPUO.Core.Questions;

namespace MAPUO.Core.Actors;

/// <summary>
/// Interfaz para un Actor en el patrón Screenplay.
/// Los Actores representan usuarios o sistemas que interactúan con la aplicación bajo prueba.
/// </summary>
public interface IActor
{
    /// <summary>
    /// Nombre del actor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Obtiene una habilidad específica que posee el actor.
    /// </summary>
    /// <typeparam name="T">Tipo de habilidad</typeparam>
    /// <returns>Instancia de la habilidad</returns>
    /// <exception cref="InvalidOperationException">Si el actor no posee la habilidad solicitada</exception>
    T GetAbility<T>() where T : class, IAbility;

    /// <summary>
    /// Verifica si el actor posee una habilidad específica.
    /// </summary>
    /// <typeparam name="T">Tipo de habilidad</typeparam>
    /// <returns>True si el actor posee la habilidad</returns>
    bool HasAbility<T>() where T : class, IAbility;

    /// <summary>
    /// Ejecuta una tarea.
    /// </summary>
    /// <param name="task">Tarea a ejecutar</param>
    Task ExecuteAsync(ITask task);

    /// <summary>
    /// Realiza una pregunta y obtiene la respuesta.
    /// </summary>
    /// <typeparam name="T">Tipo de respuesta esperada</typeparam>
    /// <param name="question">Pregunta a realizar</param>
    /// <returns>Respuesta de la pregunta</returns>
    Task<T> AsksForAsync<T>(IQuestion<T> question);
}
