# Documento de Arquitectura MAPUO

## 📐 Visión General de la Arquitectura

MAPUO implementa **Clean Architecture** combinada con el **Patrón Screenplay** para crear un framework de automatización de pruebas altamente mantenible, escalable y profesional.

---

## 🏗️ Capas de la Arquitectura

### 1. Capa Core (Dominio)

**Responsabilidad**: Lógica de negocio y contratos (interfaces) independientes de cualquier framework o herramienta externa.

```
src/Core/MAPUO.Core/
├── Actors/
│   ├── IActor.cs           # Interfaz del actor
│   └── Actor.cs            # Implementación del actor
├── Tasks/
│   └── ITask.cs            # Contrato para tareas
├── Questions/
│   └── IQuestion.cs        # Contrato para preguntas
├── Abilities/
│   ├── IAbility.cs         # Interfaz base de habilidades
│   ├── IWebAbility.cs      # Contrato para habilidades web
│   └── IApiAbility.cs      # Contrato para habilidades API
└── Models/                 # Modelos de dominio (si es necesario)
```

**Principios aplicados**:
- ✅ **DIP (Dependency Inversion)**: Solo interfaces, sin implementaciones concretas
- ✅ **SRP (Single Responsibility)**: Cada interfaz tiene una única responsabilidad
- ✅ **ISP (Interface Segregation)**: Interfaces específicas y cohesivas

---

### 2. Capa Infrastructure (Implementación)

**Responsabilidad**: Implementaciones concretas de las interfaces del Core utilizando herramientas específicas (Playwright, HttpClient, etc.).

```
src/Infrastructure/MAPUO.Infrastructure/
├── Web/
│   ├── PlaywrightWebAbility.cs    # Implementación con Playwright
│   ├── Tasks/
│   │   └── GoogleTasks.cs          # Tareas específicas de Google
│   └── Questions/
│       └── GoogleQuestions.cs      # Preguntas específicas de Google
├── API/
│   └── RestSharpApiAbility.cs      # Implementación con RestSharp (futuro)
└── DI/
    └── ContainerBootstrapper.cs    # Configuración de Dependency Injection
```

**Principios aplicados**:
- ✅ **OCP (Open/Closed)**: Abierto para extensión (nuevas abilities), cerrado para modificación
- ✅ **LSP (Liskov Substitution)**: Implementaciones intercambiables sin romper contrato
- ✅ **DIP**: Depende de abstracciones (IWebAbility), no de concreciones

---

### 3. Capa de Pruebas

**Responsabilidad**: Definición y ejecución de pruebas usando SpecFlow/BDD.

```
tests/
├── Unit/                          # Pruebas unitarias
│   └── MAPUO.Tests.Unit/
├── Integration/                   # Pruebas de integración
│   └── MAPUO.Tests.Integration/
└── E2E/                          # Pruebas End-to-End
    └── MAPUO.Tests.E2E/
        ├── GoogleSearch.feature   # Definiciones Gherkin
        ├── GoogleSearchStepDefinitions.cs
        └── Hooks.cs               # Setup/Teardown de escenarios
```

**Principios aplicados**:
- ✅ **SRP**: Cada feature file tiene una única funcionalidad
- ✅ **DIP**: Step definitions dependen de abstracciones (IActor)

---

## 🎭 Patrón Screenplay

El patrón Screenplay organiza las pruebas alrededor de **Actores** que realizan **Tareas** y hacen **Preguntas** utilizando **Habilidades**.

### Componentes del Screenplay

#### 1. **Actor (IActor)**

Representa un usuario o sistema que interactúa con la aplicación.

```csharp
var actor = new Actor("TestUser", serviceProvider);
```

**Responsabilidades**:
- Obtener habilidades (`GetAbility<T>()`)
- Ejecutar tareas (`ExecuteAsync()`)
- Hacer preguntas (`AsksForAsync()`)

#### 2. **Abilities (IAbility)**

Capacidades que un Actor puede tener (Web, API, Database, etc.).

```csharp
public interface IWebAbility : IAbility
{
    Task NavigateToAsync(string url);
    Task ClickAsync(string selector);
    Task FillAsync(string selector, string text);
    // ...
}
```

**Implementaciones**:
- `PlaywrightWebAbility`: Automatización web con Playwright
- `RestSharpApiAbility`: (Futuro) Automatización de APIs

#### 3. **Tasks (ITask)**

Flujos de negocio complejos compuestos por múltiples acciones.

```csharp
public class LoginTask : ITask
{
    public string Description => "Autenticarse en la aplicación";
    
    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        await web.FillAsync("#username", _username);
        await web.FillAsync("#password", _password);
        await web.ClickAsync("#login-btn");
    }
}
```

**Ventajas**:
- ✅ Reutilizables
- ✅ Auto-documentadas
- ✅ Fáciles de mantener

#### 4. **Questions (IQuestion<T>)**

Consultas sobre el estado actual de la aplicación.

```csharp
public class TheUserIsLoggedIn : IQuestion<bool>
{
    public string Description => "¿El usuario está autenticado?";
    
    public async Task<bool> AnswerAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        return await web.IsVisibleAsync("#user-profile");
    }
}
```

**Uso**:
```csharp
var isLoggedIn = await actor.AsksForAsync(new TheUserIsLoggedIn());
Assert.That(isLoggedIn, Is.True);
```

---

## 🔄 Flujo de Ejecución

### Diagrama de Secuencia

```
Usuario → SpecFlow Feature → Step Definition → Actor → Task → Ability → Playwright → Browser
```

### Ejemplo Completo

```gherkin
# Feature
Escenario: Login exitoso
  Dado que el usuario navega a la página de login
  Cuando ingresa credenciales válidas
  Entonces debe ver el dashboard
```

```csharp
// Step Definition
[When(@"ingresa credenciales válidas")]
public async Task WhenIngresaCredencialesValidas()
{
    await _actor.ExecuteAsync(new LoginTask("user@test.com", "pass123"));
}

// Task
public class LoginTask : ITask
{
    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        await web.FillAsync("#email", _email);
        await web.FillAsync("#password", _password);
        await web.ClickAsync("#login-btn");
    }
}

// Ability (PlaywrightWebAbility)
public async Task FillAsync(string selector, string text)
{
    var page = await GetPageAsync();
    await page.FillAsync(selector, text);
}
```

---

## 🔌 Dependency Injection

### Configuración (ContainerBootstrapper)

```csharp
public static IServiceProvider Build(string browserType, bool headless)
{
    var services = new ServiceCollection();
    
    // Registrar habilidades
    services.AddScoped<IWebAbility>(sp => 
        new PlaywrightWebAbility(browserType, headless)
    );
    
    // Registrar factory de actores
    services.AddScoped<Func<string, IActor>>(sp => 
        (actorName) => new Actor(actorName, sp)
    );
    
    return services.BuildServiceProvider();
}
```

### Ciclo de Vida

- **Scoped**: Una instancia por escenario de prueba
- **Singleton**: (Futuro) Para configuraciones compartidas
- **Transient**: (No recomendado) Para objetos desechables

---

## 🧪 Estrategia de Pruebas (Pirámide)

```
        /\
       /  \      E2E (Pocas, lentas, UI completa)
      /____\
     /      \    Integración (Medias, BD real, servicios reales)
    /________\
   /          \  Unitarias (Muchas, rápidas, aisladas)
  /____________\
```

### Distribución Recomendada

- **70%** Unitarias (< 1ms cada una)
- **20%** Integración (< 1s cada una)
- **10%** E2E (< 30s cada una)

---

## 🔒 Principios SOLID Aplicados

### 1. SRP (Single Responsibility Principle)

❌ **Mal**:
```csharp
public class TestHelper
{
    public void Login() { }
    public void Search() { }
    public void Logout() { }
    public void GenerateReport() { }
}
```

✅ **Bien**:
```csharp
public class LoginTask : ITask { }
public class SearchTask : ITask { }
public class LogoutTask : ITask { }
public class ReportGenerator { }
```

### 2. OCP (Open/Closed Principle)

✅ Nueva habilidad sin modificar código existente:

```csharp
// Nueva habilidad: Database
public class DatabaseAbility : IAbility
{
    public string Name => "Database Access";
    public async Task<User> GetUserAsync(int id) { }
}

// Registrar en DI (único cambio necesario)
services.AddScoped<IDatabaseAbility, DatabaseAbility>();
```

### 3. LSP (Liskov Substitution Principle)

✅ Implementaciones intercambiables:

```csharp
// Usar Playwright
services.AddScoped<IWebAbility, PlaywrightWebAbility>();

// Cambiar a Selenium sin romper nada
services.AddScoped<IWebAbility, SeleniumWebAbility>();
```

### 4. ISP (Interface Segregation Principle)

✅ Interfaces específicas:

```csharp
// En lugar de una interfaz gigante:
// interface ITestAbility { /* 50 métodos */ }

// Interfaces segregadas:
interface IWebAbility { }
interface IApiAbility { }
interface IDatabaseAbility { }
```

### 5. DIP (Dependency Inversion Principle)

✅ Depender de abstracciones:

```csharp
// Task depende de IWebAbility (abstracción)
public async Task ExecuteAsync(IActor actor)
{
    var web = actor.GetAbility<IWebAbility>();  // ← Abstracción
    await web.ClickAsync("#btn");
}
```

---

## 📊 Diagrama de Componentes

```
┌─────────────────────────────────────────────────────────┐
│                    SpecFlow Features                     │
│                    (Gherkin/BDD)                         │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│                  Step Definitions                        │
│              (Bindings de SpecFlow)                      │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│                    Core Layer                            │
│  ┌─────────┐  ┌─────────┐  ┌───────────┐  ┌─────────┐ │
│  │ IActor  │  │ ITask   │  │ IQuestion │  │IAbility │ │
│  └─────────┘  └─────────┘  └───────────┘  └─────────┘ │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│               Infrastructure Layer                       │
│  ┌──────────────────┐  ┌───────────────────┐           │
│  │ Playwright       │  │ RestSharp API     │           │
│  │ WebAbility       │  │ Ability (Future)  │           │
│  └──────────────────┘  └───────────────────┘           │
│                                                          │
│  ┌──────────────────────────────────────────┐          │
│  │    Dependency Injection Container        │          │
│  │       (Microsoft.Extensions.DI)          │          │
│  └──────────────────────────────────────────┘          │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
              ┌────────────────┐
              │   Playwright   │
              │   Browsers     │
              └────────────────┘
```

---

## 🚀 Extensibilidad

### Agregar Nueva Habilidad (ej: Database)

1. **Definir interfaz** en Core:
```csharp
public interface IDatabaseAbility : IAbility
{
    Task<User> GetUserAsync(int id);
}
```

2. **Implementar** en Infrastructure:
```csharp
public class SqlServerDatabaseAbility : IDatabaseAbility
{
    public async Task<User> GetUserAsync(int id)
    {
        // Lógica con Entity Framework o Dapper
    }
}
```

3. **Registrar** en DI:
```csharp
services.AddScoped<IDatabaseAbility, SqlServerDatabaseAbility>();
```

4. **Usar** en Tasks:
```csharp
var db = actor.GetAbility<IDatabaseAbility>();
var user = await db.GetUserAsync(123);
```

---

## 📈 Métricas de Calidad

| Métrica | Objetivo | Actual |
|---------|----------|--------|
| Cobertura de Código | > 80% | TBD |
| Tiempo de Ejecución (Suite Completa) | < 5 min | ~2 min |
| Tasa de Flaky Tests | < 5% | 0% |
| Tiempo de Mantenimiento por Test | < 10 min | ~5 min |

---

## 🔮 Roadmap Futuro

- [ ] **API Ability**: Implementar habilidad para pruebas de API con RestSharp
- [ ] **Database Ability**: Soporte para validaciones de BD con Entity Framework
- [ ] **TestContainers**: Integración para pruebas de integración aisladas
- [ ] **Parallel Execution**: Optimizar para ejecución paralela masiva
- [ ] **Visual Regression**: Integración con Percy o Applitools
- [ ] **Mobile Support**: Appium para pruebas nativas iOS/Android

---

## 📚 Referencias

- [Clean Architecture - Uncle Bob](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Screenplay Pattern - Serenity BDD](https://serenity-js.org/handbook/design/screenplay-pattern/)
- [SOLID Principles](https://www.digitalocean.com/community/conceptual_articles/s-o-l-i-d-the-first-five-principles-of-object-oriented-design)
- [Playwright Best Practices](https://playwright.dev/dotnet/docs/best-practices)

---

**Última actualización**: Diciembre 2025  
**Versión**: 1.0.0
