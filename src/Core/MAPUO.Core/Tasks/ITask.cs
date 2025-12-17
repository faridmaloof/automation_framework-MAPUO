namespace MAPUO.Core.Tasks;

/// <summary>
/// Interfaz base para una tarea en el patrón Screenplay.
/// Las tareas representan flujos de negocio complejos compuestos por múltiples acciones.
/// </summary>
public interface ITask
{
    /// <summary>
    /// Descripción de la tarea para reportes.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Ejecuta la tarea utilizando las habilidades del actor.
    /// </summary>
    /// <param name="actor">Actor que ejecuta la tarea</param>
    Task ExecuteAsync(Actors.IActor actor);
}
