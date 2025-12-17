# Guía de Inicio Rápido - MAPUO

## ⚡ Setup Inicial (5 minutos)

### 1. Prerrequisitos

Asegúrate de tener instalado:

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/downloads)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) o [VS Code](https://code.visualstudio.com/)

### 2. Clonar y Configurar

```powershell
# Clonar el repositorio
git clone https://github.com/yourorg/MAPUO.git
cd MAPUO

# Cargar scripts de ejecución
. .\RunTests.ps1

# Ejecutar setup automático
Setup-Project
```

Esto:
- ✅ Restaura dependencias NuGet
- ✅ Compila la solución
- ✅ Instala navegadores Playwright (Chromium, Firefox, WebKit)

### 3. Primera Ejecución

```powershell
# Ejecutar pruebas E2E con navegador visible
Run-E2E-Visible
```

¡Listo! Deberías ver el navegador Chrome abrirse automáticamente.

---

## 🎯 Comandos Esenciales

| Comando | Descripción |
|---------|-------------|
| `Run-E2E-Visible` | Pruebas E2E con navegador visible (debugging) |
| `Run-E2E-Headless` | Pruebas E2E modo headless (CI/CD) |
| `Run-Smoke-Tests` | Solo pruebas críticas (smoke) |
| `Run-All-Browsers` | Pruebas en Chromium + Firefox + WebKit |
| `Clean-Build` | Limpia y recompila el proyecto |

---

## 📝 Crear Tu Primera Prueba (10 minutos)

### Paso 1: Crear Feature File

Crea `tests/E2E/MAPUO.Tests.E2E/Login.feature`:

```gherkin
# language: es
Característica: Autenticación de Usuario
  Como usuario registrado
  Quiero poder autenticarme
  Para acceder a mi cuenta

  @smoke @web
  Escenario: Login exitoso
    Dado que el usuario navega a "https://myapp.com/login"
    Cuando ingresa usuario "test@mail.com" y contraseña "123456"
    Entonces debe ver el mensaje "Bienvenido"
```

### Paso 2: Crear Tarea de Login

Crea `src/Infrastructure/MAPUO.Infrastructure/Web/Tasks/LoginTask.cs`:

```csharp
using MAPUO.Core.Tasks;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;

namespace MAPUO.Infrastructure.Web.Tasks;

public class LoginTask : ITask
{
    private readonly string _email;
    private readonly string _password;

    public string Description => $"Login con email '{_email}'";

    public LoginTask(string email, string password)
    {
        _email = email;
        _password = password;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        await web.FillAsync("#email", _email);
        await web.FillAsync("#password", _password);
        await web.ClickAsync("#login-button");
    }
}
```

### Paso 3: Crear Step Definitions

Crea `tests/E2E/MAPUO.Tests.E2E/LoginStepDefinitions.cs`:

```csharp
using TechTalk.SpecFlow;
using NUnit.Framework;
using MAPUO.Core.Actors;
using MAPUO.Infrastructure.Web.Tasks;

namespace MAPUO.StepDefinitions;

[Binding]
public class LoginStepDefinitions
{
    private readonly IActor _actor;

    public LoginStepDefinitions(ScenarioContext context)
    {
        _actor = context.Get<IActor>("Actor");
    }

    [Given(@"que el usuario navega a ""(.*)""")]
    public async Task GivenQueElUsuarioNavegaA(string url)
    {
        var web = _actor.GetAbility<IWebAbility>();
        await web.NavigateToAsync(url);
    }

    [When(@"ingresa usuario ""(.*)"" y contraseña ""(.*)""")]
    public async Task WhenIngresaUsuarioYContrasena(string email, string password)
    {
        await _actor.ExecuteAsync(new LoginTask(email, password));
    }

    [Then(@"debe ver el mensaje ""(.*)""")]
    public async Task ThenDebeVerElMensaje(string expectedMessage)
    {
        var web = _actor.GetAbility<IWebAbility>();
        var actualMessage = await web.GetTextAsync(".welcome-message");
        Assert.That(actualMessage, Does.Contain(expectedMessage));
    }
}
```

### Paso 4: Compilar y Ejecutar

```powershell
# Compilar
dotnet build

# Ejecutar solo la nueva prueba
Run-By-Category -Category "smoke"
```

---

## 🏗️ Patrones y Buenas Prácticas

### ✅ DO (Hacer)

```csharp
// ✅ Tareas descriptivas y reutilizables
public class SearchProductTask : ITask
{
    private readonly string _productName;
    
    public string Description => $"Buscar producto '{_productName}'";
    
    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        await web.FillAsync("#search", _productName);
        await web.ClickAsync("#search-btn");
    }
}

// ✅ Preguntas claras y específicas
public class TheProductIsInCart : IQuestion<bool>
{
    private readonly string _productName;
    
    public string Description => $"¿El producto '{_productName}' está en el carrito?";
    
    public async Task<bool> AnswerAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        return await web.IsVisibleAsync($"text={_productName}");
    }
}
```

### ❌ DON'T (No hacer)

```csharp
// ❌ Lógica de UI en step definitions
[When("busca un producto")]
public async Task WhenBuscaProducto()
{
    // MAL: Lógica de navegación aquí
    var page = GetPage();
    await page.FillAsync("#search", "laptop");
    await page.ClickAsync("#btn");
}

// ❌ Tareas genéricas sin contexto
public class ClickButtonTask : ITask { } // Muy genérico
```

---

## 🐛 Debugging Tips

### Ver navegador durante ejecución

```powershell
$env:HEADLESS = "false"
dotnet test
```

### Captura de pantalla manual

```csharp
var web = actor.GetAbility<IWebAbility>();
await web.TakeScreenshotAsync("debug.png");
```

### Logs detallados

```powershell
dotnet test --logger "console;verbosity=detailed"
```

---

## 📊 Generación de Reportes

### Allure Report

```powershell
# Ejecutar pruebas y generar reporte
Run-With-Allure

# Solo generar reporte de ejecuciones anteriores
Open-Allure-Report
```

El reporte se abrirá automáticamente en el navegador.

---

## 🔧 Configuración Avanzada

### Cambiar navegador

```powershell
$env:BROWSER = "firefox"  # o "webkit"
Run-E2E-Visible
```

### Ejecución paralela

```powershell
dotnet test --parallel
```

### Filtros personalizados

```powershell
# Solo pruebas de login
dotnet test --filter "FullyQualifiedName~Login"

# Excluir pruebas lentas
dotnet test --filter "Category!=slow"
```

---

## ❓ Troubleshooting

### "Playwright browsers not found"

```powershell
& ".\tests\E2E\MAPUO.Tests.E2E\bin\Debug\net9.0\playwright.ps1" install
```

### "Could not find ability"

Verificar que la habilidad esté registrada en `ContainerBootstrapper.cs`:

```csharp
services.AddScoped<IWebAbility>(sp => new PlaywrightWebAbility());
```

### Pruebas fallan en Google

**Esperado**: Google bloquea automatización. Usa sitios de prueba como:
- https://demo.playwright.dev/todomvc
- https://the-internet.herokuapp.com/

---

## 📚 Siguientes Pasos

1. ✅ [Leer arquitectura completa](../README.md#arquitectura)
2. ✅ [Aprender patrones SOLID aplicados](../README.md#principios-solid)
3. ✅ [Explorar ejemplos avanzados](../docs/EXAMPLES.md)
4. ✅ [Configurar CI/CD](../.github/workflows/ci.yml)

---

## 🎓 Recursos de Aprendizaje

- [Playwright Documentation](https://playwright.dev/dotnet/)
- [SpecFlow Guide](https://docs.specflow.org/)
- [Screenplay Pattern](https://serenity-js.org/handbook/design/screenplay-pattern/)
- [Clean Architecture](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)

---

## 💬 Obtener Ayuda

- 📖 [FAQ](./FAQ.md)
- 🐛 [Reportar bug](https://github.com/yourorg/MAPUO/issues)
- 💡 [Solicitar feature](https://github.com/yourorg/MAPUO/issues/new?template=feature_request.md)

---

¡Feliz automatización! 🚀
