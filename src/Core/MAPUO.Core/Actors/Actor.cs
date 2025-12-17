using MAPUO.Core.Abilities;
using MAPUO.Core.Questions;
using MAPUO.Core.Tasks;
using System;

namespace MAPUO.Core.Actors;

/// <summary>
/// Implementación concreta de un Actor en el patrón Screenplay.
/// El Actor utiliza un Service Provider para obtener las habilidades mediante Dependency Injection.
/// </summary>
public class Actor : IActor
{
    private readonly IServiceProvider _serviceProvider;

    public string Name { get; }

    /// <summary>
    /// Constructor del Actor.
    /// </summary>
    /// <param name="name">Nombre del actor</param>
    /// <param name="serviceProvider">Service provider para resolver dependencias</param>
    public Actor(string name, IServiceProvider serviceProvider)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public T GetAbility<T>() where T : class, IAbility
    {
        var ability = _serviceProvider.GetService(typeof(T)) as T;
        
        if (ability == null)
        {
            throw new InvalidOperationException(
                $"El actor '{Name}' no posee la habilidad '{typeof(T).Name}'. " +
                $"Asegúrese de que la habilidad esté registrada en el contenedor de DI.");
        }

        return ability;
    }

    /// <inheritdoc/>
    public bool HasAbility<T>() where T : class, IAbility
    {
        return _serviceProvider.GetService(typeof(T)) is T;
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(ITask task)
    {
        if (task == null)
            throw new ArgumentNullException(nameof(task));

        Console.WriteLine($"[{Name}] Ejecutando: {task.Description}");
        await task.ExecuteAsync(this);
    }

    /// <inheritdoc/>
    public async Task<T> AsksForAsync<T>(IQuestion<T> question)
    {
        if (question == null)
            throw new ArgumentNullException(nameof(question));

        Console.WriteLine($"[{Name}] Preguntando: {question.Description}");
        return await question.AnswerAsync(this);
    }
}
