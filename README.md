# MAPUO - Marco de Automatización de Pruebas Unificado y Observables

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Playwright](https://img.shields.io/badge/Playwright-1.57-green)
![SpecFlow](https://img.shields.io/badge/SpecFlow-3.9-orange)
![License](https://img.shields.io/badge/license-MIT-green)

## 📋 Descripción

MAPUO es un framework de automatización de pruebas profesional basado en **Clean Architecture** y el **Patrón Screenplay**, diseñado para soportar pruebas Web, API y móviles con integración nativa en pipelines CI/CD.

### ✨ Características Principales

- 🎯 **Patrón Screenplay**: Actor → Tareas → Preguntas para pruebas altamente mantenibles
- 🏗️ **Clean Architecture**: Separación clara entre lógica de negocio e infraestructura
- 🔄 **Dependency Injection**: Gestión de dependencias con Microsoft.Extensions.DependencyInjection
- 🌐 **Multi-navegador**: Soporte para Chromium, Firefox y WebKit vía Playwright
- 📝 **BDD/Gherkin**: Pruebas legibles con SpecFlow en español
- 📊 **Allure Reports**: Reportes interactivos y profesionales
- 🔧 **CI/CD Ready**: Configuración lista para GitHub Actions y Azure DevOps
- 📸 **Screenshots automáticos**: Captura de pantalla en fallos

---

## 🏛️ Arquitectura del Proyecto

```
MAPUO/
├── src/
│   ├── Core/                          # Lógica de negocio (independiente de herramientas)
│   │   ├── Actors/                    # Definición de actores
│   │   ├── Tasks/                     # Tareas de negocio
│   │   ├── Questions/                 # Preguntas para validaciones
│   │   └── Abilities/                 # Contratos de habilidades
│   │
│   └── Infrastructure/                # Implementación concreta
│       ├── Web/                       # Playwright + habilidades Web
│       ├── API/                       # HttpClient + habilidades API
│       └── DI/                        # Configuración de Dependency Injection
│
├── tests/
│   ├── Unit/                          # Pruebas unitarias
│   ├── Integration/                   # Pruebas de integración
│   └── E2E/                          # Pruebas end-to-end (SpecFlow + Playwright)
│
└── .github/workflows/                 # Pipelines CI/CD
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

- **.NET SDK 9.0** o superior
- **PowerShell** 7+ (para scripts)
- **Git** (para clonar el repositorio)

### Instalación

```powershell
# Clonar el repositorio
git clone https://github.com/yourorg/MAPUO.git
cd MAPUO

# Restaurar dependencias
dotnet restore

# Compilar la solución
dotnet build

# Instalar navegadores de Playwright
& ".\tests\E2E\MAPUO.Tests.E2E\bin\Debug\net9.0\playwright.ps1" install
```

### Ejecutar Pruebas

#### Ejecutar todas las pruebas E2E

```powershell
dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
```

#### Ejecutar con navegador visible (útil para debugging)

```powershell
$env:HEADLESS="false"
dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
```

#### Ejecutar en modo headless (CI/CD)

```powershell
$env:HEADLESS="true"
dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
```

#### Seleccionar navegador

```powershell
$env:BROWSER="firefox"  # Opciones: chromium, firefox, webkit
dotnet test tests/E2E/MAPUO.Tests.E2E/MAPUO.Tests.E2E.csproj
```

#### Filtrar pruebas por categoría

```powershell
# Ejecutar solo pruebas smoke
dotnet test --filter "Category=smoke"

# Ejecutar solo pruebas web
dotnet test --filter "Category=web"
```

---

## 📝 Crear Nuevas Pruebas

### 1. Definir Feature (Gherkin)

Crear archivo `.feature` en `tests/E2E/MAPUO.Tests.E2E/`:

```gherkin
# language: es
Característica: Login de Usuario
  Como usuario registrado
  Quiero poder autenticarme en la aplicación
  Para acceder a mi cuenta

  @smoke @web
  Escenario: Login exitoso con credenciales válidas
    Dado que el usuario navega a la página de login
    Cuando ingresa credenciales válidas
    Entonces debe ser redirigido al dashboard
```

### 2. Crear Tareas (Tasks)

```csharp
using MAPUO.Core.Tasks;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;

public class LoginTask : ITask
{
    private readonly string _username;
    private readonly string _password;

    public string Description => $"Login con usuario '{_username}'";

    public LoginTask(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        await webAbility.FillAsync("#username", _username);
        await webAbility.FillAsync("#password", _password);
        await webAbility.ClickAsync("#login-btn");
    }
}
```

### 3. Crear Preguntas (Questions)

```csharp
using MAPUO.Core.Questions;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;

public class TheUserIsLoggedIn : IQuestion<bool>
{
    public string Description => "¿El usuario está autenticado?";

    public async Task<bool> AnswerAsync(IActor actor)
    {
        var webAbility = actor.GetAbility<IWebAbility>();
        return await webAbility.IsVisibleAsync("#user-profile");
    }
}
```

### 4. Implementar Step Definitions

```csharp
[Binding]
public class LoginStepDefinitions
{
    private readonly IActor _actor;

    public LoginStepDefinitions(ScenarioContext scenarioContext)
    {
        _actor = scenarioContext.Get<IActor>("Actor");
    }

    [When(@"ingresa credenciales válidas")]
    public async Task WhenIngresaCredencialesValidas()
    {
        await _actor.ExecuteAsync(new LoginTask("user@test.com", "password123"));
    }

    [Then(@"debe ser redirigido al dashboard")]
    public async Task ThenDebeSerRedirigidoAlDashboard()
    {
        var isLoggedIn = await _actor.AsksForAsync(new TheUserIsLoggedIn());
        Assert.That(isLoggedIn, Is.True);
    }
}
```

---

## 🔧 Configuración

### Variables de Entorno

| Variable | Descripción | Valores | Default |
|----------|-------------|---------|---------|
| `HEADLESS` | Modo headless del navegador | `true`, `false` | `false` |
| `BROWSER` | Tipo de navegador | `chromium`, `firefox`, `webkit` | `chromium` |

### Configuración de SpecFlow

Ver [specflow.json](tests/E2E/MAPUO.Tests.E2E/specflow.json) para personalizar:
- Lenguaje de los features
- Manejo de errores
- Configuración de trace

---

## 📊 Reportes

### Allure Reports

Los reportes de Allure se generan automáticamente en `allure-results/`.

Para visualizar:

```powershell
# Instalar Allure (solo una vez)
npm install -g allure-commandline

# Generar y abrir reporte
allure serve allure-results
```

### Screenshots

Los screenshots de fallos se guardan en `TestResults/Screenshots/`.

---

## 🧪 Principios SOLID Aplicados

1. **SRP (Single Responsibility)**: Cada clase tiene una única responsabilidad
2. **OCP (Open/Closed)**: Abierto para extensión, cerrado para modificación
3. **LSP (Liskov Substitution)**: Las abstracciones son intercambiables
4. **ISP (Interface Segregation)**: Interfaces específicas y cohesivas
5. **DIP (Dependency Inversion)**: Dependencias inyectadas, no instanciadas

---

## 📚 Documentación Adicional

- [Guía de Contribución](docs/CONTRIBUTING.md)
- [Arquitectura Detallada](docs/ARCHITECTURE.md)
- [Troubleshooting](docs/TROUBLESHOOTING.md)
- [Mejores Prácticas](docs/BEST_PRACTICES.md)

---

## 🤝 Contribuir

1. Fork el proyecto
2. Crear feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit cambios (`git commit -m 'Add AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Abrir Pull Request

---

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver [LICENSE](LICENSE) para más detalles.

---

## 👥 Autores

- **Equipo MAPUO** - *Desarrollo inicial*

---

## 🙏 Agradecimientos

- Microsoft Playwright Team
- SpecFlow Contributors
- Clean Architecture Community
- Screenplay Pattern Advocates

---

## 📞 Soporte

Para reportar problemas o sugerencias, por favor crear un [issue](https://github.com/yourorg/MAPUO/issues).
