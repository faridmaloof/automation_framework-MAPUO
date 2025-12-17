# 📚 Ejemplos de Uso - MAPUO

Este documento contiene ejemplos prácticos de cómo usar el framework MAPUO para diferentes escenarios de automatización.

---

## 📋 Índice

1. [Ejemplo 1: Login Simple](#ejemplo-1-login-simple)
2. [Ejemplo 2: Búsqueda con Validación](#ejemplo-2-búsqueda-con-validación)
3. [Ejemplo 3: Formulario Complejo](#ejemplo-3-formulario-complejo)
4. [Ejemplo 4: Navegación Multi-Página](#ejemplo-4-navegación-multi-página)
5. [Ejemplo 5: Data-Driven Testing](#ejemplo-5-data-driven-testing)
6. [Ejemplo 6: API + Web Combinado](#ejemplo-6-api--web-combinado)
7. [Ejemplo 7: Pruebas con Esperas Dinámicas](#ejemplo-7-pruebas-con-esperas-dinámicas)
8. [Ejemplo 8: Screenshot en Puntos Específicos](#ejemplo-8-screenshot-en-puntos-específicos)

---

## Ejemplo 1: Login Simple

### Feature
```gherkin
# language: es
Característica: Autenticación de Usuario

  @smoke @login
  Escenario: Login exitoso con credenciales válidas
    Dado que el usuario está en la página de login
    Cuando ingresa las credenciales válidas
    Entonces debe ver el mensaje de bienvenida
    Y debe ser redirigido al dashboard
```

### Task
```csharp
using MAPUO.Core.Tasks;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;

namespace MAPUO.Infrastructure.Web.Tasks;

public class LoginWithCredentials : ITask
{
    private readonly string _username;
    private readonly string _password;

    public string Description => $"Login como '{_username}'";

    public LoginWithCredentials(string username, string password)
    {
        _username = username;
        _password = password;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        await web.WaitForSelectorAsync("#username", 5000);
        await web.FillAsync("#username", _username);
        await web.FillAsync("#password", _password);
        await web.ClickAsync("button[type='submit']");
    }
}
```

### Question
```csharp
using MAPUO.Core.Questions;
using MAPUO.Core.Actors;
using MAPUO.Core.Abilities;

namespace MAPUO.Infrastructure.Web.Questions;

public class TheWelcomeMessageIsVisible : IQuestion<bool>
{
    public string Description => "¿El mensaje de bienvenida es visible?";

    public async Task<bool> AnswerAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        try
        {
            await web.WaitForSelectorAsync(".welcome-message", 3000);
            return await web.IsVisibleAsync(".welcome-message");
        }
        catch
        {
            return false;
        }
    }
}
```

### Step Definition
```csharp
[Binding]
public class LoginStepDefinitions
{
    private readonly IActor _actor;

    public LoginStepDefinitions(ScenarioContext context)
    {
        _actor = context.Get<IActor>("Actor");
    }

    [Given(@"que el usuario está en la página de login")]
    public async Task GivenUserIsOnLoginPage()
    {
        var web = _actor.GetAbility<IWebAbility>();
        await web.NavigateToAsync("https://myapp.com/login");
    }

    [When(@"ingresa las credenciales válidas")]
    public async Task WhenEntersValidCredentials()
    {
        await _actor.ExecuteAsync(
            new LoginWithCredentials("admin@test.com", "Admin123!")
        );
    }

    [Then(@"debe ver el mensaje de bienvenida")]
    public async Task ThenShouldSeeWelcomeMessage()
    {
        var isVisible = await _actor.AsksForAsync(new TheWelcomeMessageIsVisible());
        Assert.That(isVisible, Is.True, "Mensaje de bienvenida no visible");
    }

    [Then(@"debe ser redirigido al dashboard")]
    public async Task ThenShouldBeRedirectedToDashboard()
    {
        var web = _actor.GetAbility<IWebAbility>();
        var currentUrl = await web.GetCurrentUrlAsync();
        Assert.That(currentUrl, Does.Contain("/dashboard"));
    }
}
```

---

## Ejemplo 2: Búsqueda con Validación

### Feature
```gherkin
# language: es
Característica: Búsqueda de Productos

  @smoke @search
  Escenario: Búsqueda exitosa de producto existente
    Dado que el usuario está en la página principal
    Cuando busca el producto "Laptop Dell XPS 13"
    Entonces debe ver al menos 1 resultado
    Y el primer resultado debe contener "Dell XPS"
```

### Task
```csharp
public class SearchForProduct : ITask
{
    private readonly string _productName;

    public string Description => $"Buscar producto '{_productName}'";

    public SearchForProduct(string productName)
    {
        _productName = productName;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        await web.FillAsync("input[name='search']", _productName);
        await web.PressKeyAsync("input[name='search']", "Enter");
        
        // Esperar a que se carguen los resultados
        await web.WaitForSelectorAsync(".search-results", 10000);
    }
}
```

### Question
```csharp
public class TheNumberOfSearchResults : IQuestion<int>
{
    public string Description => "El número de resultados de búsqueda";

    public async Task<int> AnswerAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        // Contar elementos con clase .product-card
        var resultsText = await web.GetTextAsync(".results-count");
        
        // Extraer número del texto "Mostrando 15 resultados"
        var match = System.Text.RegularExpressions.Regex.Match(resultsText, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }
}

public class TheFirstSearchResult : IQuestion<string>
{
    public string Description => "El texto del primer resultado";

    public async Task<string> AnswerAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        return await web.GetTextAsync(".product-card:first-child h3");
    }
}
```

---

## Ejemplo 3: Formulario Complejo

### Feature
```gherkin
# language: es
Característica: Registro de Usuario

  @regression @forms
  Escenario: Registro exitoso con todos los campos
    Dado que el usuario está en la página de registro
    Cuando completa el formulario con:
      | Campo          | Valor                  |
      | Nombre         | Juan                   |
      | Apellido       | Pérez                  |
      | Email          | juan.perez@email.com   |
      | Contraseña     | SecurePass123!         |
      | Confirmar      | SecurePass123!         |
      | País           | Argentina              |
      | Acepta términos| Sí                     |
    Y hace clic en "Registrarse"
    Entonces debe ver "Registro exitoso"
```

### Task
```csharp
public class FillRegistrationForm : ITask
{
    private readonly Dictionary<string, string> _formData;

    public string Description => "Completar formulario de registro";

    public FillRegistrationForm(Dictionary<string, string> formData)
    {
        _formData = formData;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        await web.FillAsync("#firstName", _formData["Nombre"]);
        await web.FillAsync("#lastName", _formData["Apellido"]);
        await web.FillAsync("#email", _formData["Email"]);
        await web.FillAsync("#password", _formData["Contraseña"]);
        await web.FillAsync("#confirmPassword", _formData["Confirmar"]);
        
        // Seleccionar dropdown
        await web.ClickAsync("#country");
        await web.ClickAsync($"text={_formData["País"]}");
        
        // Checkbox
        if (_formData["Acepta términos"] == "Sí")
        {
            await web.ClickAsync("#terms");
        }
    }
}
```

### Step Definition
```csharp
[When(@"completa el formulario con:")]
public async Task WhenCompletesFormWith(Table table)
{
    var formData = new Dictionary<string, string>();
    
    foreach (var row in table.Rows)
    {
        formData[row["Campo"]] = row["Valor"];
    }
    
    await _actor.ExecuteAsync(new FillRegistrationForm(formData));
}
```

---

## Ejemplo 4: Navegación Multi-Página

### Feature
```gherkin
# language: es
Característica: Flujo de Compra

  @e2e @checkout
  Escenario: Compra completa de producto
    Dado que el usuario tiene una sesión iniciada
    Cuando navega a la categoría "Electrónica"
    Y selecciona el producto "Laptop HP"
    Y agrega el producto al carrito
    Y procede al checkout
    Y completa la dirección de envío
    Y selecciona método de pago "Tarjeta de crédito"
    Y confirma la compra
    Entonces debe ver "Pedido confirmado"
    Y debe recibir un número de orden
```

### Tareas Compuestas
```csharp
// Tarea principal que combina múltiples tareas
public class CompletePurchaseFlow : ITask
{
    private readonly string _category;
    private readonly string _product;

    public string Description => "Completar flujo de compra";

    public CompletePurchaseFlow(string category, string product)
    {
        _category = category;
        _product = product;
    }

    public async Task ExecuteAsync(IActor actor)
    {
        // Ejecutar tareas secuencialmente
        await actor.ExecuteAsync(new NavigateToCategory(_category));
        await actor.ExecuteAsync(new SelectProduct(_product));
        await actor.ExecuteAsync(new AddToCart());
        await actor.ExecuteAsync(new ProceedToCheckout());
        await actor.ExecuteAsync(new FillShippingAddress());
        await actor.ExecuteAsync(new SelectPaymentMethod("Tarjeta de crédito"));
        await actor.ExecuteAsync(new ConfirmPurchase());
    }
}

// Tareas individuales
public class NavigateToCategory : ITask
{
    private readonly string _category;
    
    public string Description => $"Navegar a categoría '{_category}'";
    
    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        await web.ClickAsync($"text={_category}");
        await web.WaitForSelectorAsync(".product-list");
    }
}

public class AddToCart : ITask
{
    public string Description => "Agregar al carrito";
    
    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        await web.ClickAsync("button.add-to-cart");
        await web.WaitForSelectorAsync(".cart-notification");
    }
}
```

---

## Ejemplo 5: Data-Driven Testing

### Feature
```gherkin
# language: es
Característica: Validación de Login con Múltiples Usuarios

  @data-driven
  Esquema del escenario: Login con diferentes credenciales
    Dado que el usuario está en la página de login
    Cuando ingresa username "<usuario>" y password "<contraseña>"
    Entonces el resultado debe ser "<resultado>"

    Ejemplos:
      | usuario          | contraseña   | resultado |
      | admin@test.com   | Admin123!    | exitoso   |
      | user@test.com    | User123!     | exitoso   |
      | guest@test.com   | Guest123!    | exitoso   |
      | invalid@test.com | WrongPass    | fallido   |
      | admin@test.com   | WrongPass    | fallido   |
```

### Step Definition
```csharp
[When(@"ingresa username ""(.*)"" y password ""(.*)""")]
public async Task WhenEntersCredentials(string username, string password)
{
    await _actor.ExecuteAsync(new LoginWithCredentials(username, password));
    
    // Esperar a que procese (exitoso o fallido)
    await Task.Delay(2000);
}

[Then(@"el resultado debe ser ""(.*)""")]
public async Task ThenResultShouldBe(string expectedResult)
{
    var web = _actor.GetAbility<IWebAbility>();
    
    if (expectedResult == "exitoso")
    {
        var isLoggedIn = await web.IsVisibleAsync(".user-profile");
        Assert.That(isLoggedIn, Is.True, "Login no fue exitoso");
    }
    else
    {
        var errorVisible = await web.IsVisibleAsync(".error-message");
        Assert.That(errorVisible, Is.True, "Mensaje de error no visible");
    }
}
```

---

## Ejemplo 6: API + Web Combinado

### Tarea que combina API y Web
```csharp
public class CreateUserViaApiAndVerifyUI : ITask
{
    private readonly string _email;
    private readonly string _name;

    public string Description => $"Crear usuario '{_email}' vía API y verificar en UI";

    public async Task ExecuteAsync(IActor actor)
    {
        // 1. Crear usuario vía API
        var api = actor.GetAbility<IApiAbility>();
        var response = await api.PostAsync<CreateUserRequest, CreateUserResponse>(
            "/api/users",
            new CreateUserRequest { Email = _email, Name = _name }
        );

        // 2. Verificar en UI que el usuario existe
        var web = actor.GetAbility<IWebAbility>();
        await web.NavigateToAsync($"https://app.com/users/{response.Id}");
        
        await web.WaitForSelectorAsync(".user-details");
    }
}
```

---

## Ejemplo 7: Pruebas con Esperas Dinámicas

### Tarea con espera inteligente
```csharp
public class WaitForAjaxToComplete : ITask
{
    public string Description => "Esperar a que finalicen las peticiones AJAX";

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        // Esperar a que desaparezca el spinner
        for (int i = 0; i < 30; i++)
        {
            var spinnerVisible = await web.IsVisibleAsync(".loading-spinner");
            if (!spinnerVisible) break;
            
            await Task.Delay(100);
        }
        
        // Esperar estabilización adicional
        await Task.Delay(500);
    }
}
```

---

## Ejemplo 8: Screenshot en Puntos Específicos

### Tarea con captura de pantalla
```csharp
public class CaptureOrderConfirmation : ITask
{
    private readonly string _orderNumber;

    public string Description => "Capturar confirmación de orden";

    public async Task ExecuteAsync(IActor actor)
    {
        var web = actor.GetAbility<IWebAbility>();
        
        // Tomar screenshot del recibo
        var screenshotPath = Path.Combine(
            "TestResults",
            "Screenshots",
            $"Order_{_orderNumber}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        );
        
        Directory.CreateDirectory(Path.GetDirectoryName(screenshotPath)!);
        await web.TakeScreenshotAsync(screenshotPath);
        
        Console.WriteLine($"📸 Screenshot guardado: {screenshotPath}");
    }
}
```

---

## 🎯 Buenas Prácticas Demostradas

### ✅ DO (Hacer)

1. **Tareas descriptivas y específicas**
   ```csharp
   new LoginWithCredentials("admin@test.com", "Pass123");  // ✅
   ```

2. **Preguntas que retornan valores tipados**
   ```csharp
   public class TheNumberOfResults : IQuestion<int>  // ✅
   ```

3. **Esperas explícitas con timeouts**
   ```csharp
   await web.WaitForSelectorAsync(".results", 10000);  // ✅
   ```

4. **Manejo de errores en Questions**
   ```csharp
   try { return await web.IsVisibleAsync(selector); }
   catch { return false; }  // ✅
   ```

### ❌ DON'T (No hacer)

1. **Lógica de UI en Step Definitions**
   ```csharp
   [When("hace login")]
   public async Task Login()
   {
       await page.FillAsync("#user", "admin");  // ❌ NO!
   }
   ```

2. **Tareas genéricas sin contexto**
   ```csharp
   new ClickButton("#btn");  // ❌ Muy genérico
   ```

3. **Hardcodear esperas fijas**
   ```csharp
   await Task.Delay(5000);  // ❌ Usar WaitForSelector en su lugar
   ```

---

## 📚 Recursos Adicionales

- [Playwright Selectors](https://playwright.dev/dotnet/docs/selectors)
- [SpecFlow Table Parameters](https://docs.specflow.org/projects/specflow/en/latest/Bindings/Step-Argument-Conversions.html)
- [NUnit Assertions](https://docs.nunit.org/articles/nunit/writing-tests/assertions/assertion-models/constraint.html)

---

¡Feliz automatización! 🚀
