namespace MAPUO.Core.Abilities;

/// <summary>
/// Interfaz base para todas las habilidades (Abilities) en el patrón Screenplay.
/// Las habilidades representan capacidades que un Actor puede tener, como interactuar con una aplicación web, API, o base de datos.
/// </summary>
public interface IAbility
{
    /// <summary>
    /// Nombre descriptivo de la habilidad.
    /// </summary>
    string Name { get; }
}
